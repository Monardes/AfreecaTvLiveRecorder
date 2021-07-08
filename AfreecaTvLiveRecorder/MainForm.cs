using FluentScheduler;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/*
创建日期:2018年

作者QQ:5115147

简介:AfreecaTv直播录制小助手

如需更多功能或软件定制及开发请联系作者QQ:5115147 或 960596621

使用注意事项:

需手动获取Cookie,Cookie可不填写

19+需填入Cookie 才可以录制

Record.txt 填入需要录制主播的Id, 一行一个, # 为注释该行,比如:

#付费录制
gusdk2362

#徐雅
bjdyrksu

推荐录制主播

feel0100

rlrlvkvk123

gusdk2362

bjdyrksu

jeehyeoun

sol3712

fall0715

tprtl7

damikim
 */

namespace AfreecaTvLiveRecorder
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 日志
        /// </summary>
        public static Logger Log = null;

        /// <summary>
        /// 控件名,值
        /// </summary>
        public Dictionary<string, string> ControlsDic = new Dictionary<string, string>();

        /// <summary>
        /// 直播录制任务列表  主播,任务
        /// </summary>
        public Dictionary<string, Task> RecordTaskDic = new Dictionary<string, Task>();

        /// <summary>
        /// 模拟苹果电脑 才可以获取原画质视频
        /// </summary>
        public string UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36";

        /// <summary>
        /// Mp4标题
        /// </summary>
        public string Title { get; set; } = "微博:主播直播录播大师 直播录制客服QQ:960596621或1656395222 QQ交流群:812393257";

        /// <summary>
        /// Mp4注释
        /// </summary>
        public string Comment { get; set; } = "直播录制客服QQ:960596621或1656395222 QQ交流群:812393257 微博:主播直播录播大师";

        /// <summary>
        /// Cookie
        /// </summary>
        public string Cookie = string.Empty;

        /// <summary>
        /// mp4保存文件夹
        /// </summary>
        public string SaveDir = string.Empty;

        /// <summary>
        /// mp4转存文件夹
        /// </summary>
        public string SaveAsDir = string.Empty;

        private void MainForm_Load(object sender, EventArgs e)
        {
            //初始化日志
            if (null == Log)
            {
                Log = LogManager.GetCurrentClassLogger();
            }

            //设置图标
            this.Icon = Properties.Resources.AfreecaTv;

            string softVersion = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}";

            this.Text += " Ver:" + softVersion;

            //不捕获对错误线程的调用
            CheckForIllegalCrossThreadCalls = false;

            //Http请求最大并发
            ServicePointManager.DefaultConnectionLimit = 1000;

            ServicePointManager.Expect100Continue = false;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            //忽略https证书错误
            ServicePointManager.ServerCertificateValidationCallback += (s, certificate, chain, sslPolicyErrors) => true;

            LoadJsonData();

            LoadControlValue();

            ThreadPool.SetMinThreads(1000, 1000);

            ThreadPool.SetMaxThreads(2000, 2000);

            if (File.Exists("ffmpeg.exe"))
            {
                Log.Info("程序初始化完成,如需定制功能或代录请联系QQ 5115147 或 960596621");
            }
            else
            {
                Log.Info("缺少必要的FFmpeg组件,如需更多功能请联系QQ 5115147 或 960596621");
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveControlValue();

            SaveJsonData();

            KillChildrenProcess();
        }

        private void LoadJsonData()
        {
            string file = "ControlsDic.json";

            if (File.Exists(file))
            {
                ControlsDic = JsonHelper.DeSerialize<Dictionary<string, string>>(File.ReadAllText(file));
            }
        }

        private void SaveJsonData()
        {
            string file = "ControlsDic.json";

            string text = JsonHelper.Serialize<Dictionary<string, string>>(ControlsDic);

            File.WriteAllText(file, text);
        }

        /// <summary>
        /// 加载窗体控件的值
        /// </summary>
        private void LoadControlValue()
        {
            foreach (var control in this.Controls)
            {
                if (control is TextBox)
                {
                    TextBox textBox = (TextBox)control;

                    if (ControlsDic.ContainsKey(textBox.Name))
                    {
                        textBox.Text = ControlsDic[textBox.Name];
                    }
                }
            }
        }

        /// <summary>
        /// 保存窗体控件的值
        /// </summary>
        private void SaveControlValue()
        {
            foreach (var control in this.Controls)
            {
                if (control is TextBox)
                {
                    TextBox textBox = (TextBox)control;

                    ControlsDic[textBox.Name] = textBox.Text;
                }
            }
        }

        /// <summary>
        /// 设置控件状态
        /// </summary>
        /// <param name="enable"></param>
        private void EnableControl(bool enable)
        {
            txtSaveAsDir.Enabled = enable;

            txtSaveDir.Enabled = enable;

            txtCookie.Enabled = enable;
        }

        private void txtSaveDir_DoubleClick(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtSaveDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void txtSaveAsDir_DoubleClick(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtSaveAsDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            btnStartRecord.Enabled = false;

            if (string.IsNullOrWhiteSpace(txtSaveDir.Text))
            {
                MessageBox.Show("必须填写保存文件夹");

                btnStartRecord.Enabled = true;

                return;
            }

            Cookie = txtCookie.Text;

            SaveDir = txtSaveDir.Text;

            SaveAsDir = txtSaveAsDir.Text;

            //检测Cookie是否过期
            bool expired = false;

            string text = HttpHelper.Get("https://www.afreecatv.com/", UserAgent, Cookie);

            if (text.Contains("var _szUserId = '';"))
            {
                expired = true;
            }

            //如果Cookie已过期 清空Cookie
            if (expired)
            {
                txtCookie.Text = string.Empty;

                Cookie = string.Empty;

                Log.Info($"账户Cookie已过期");
            }
            else
            {
                Log.Info($"账户登陆成功");
            }

            EnableControl(false);

            SaveControlValue();

            SaveJsonData();

            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var registry = new Registry();

            //定时器不重入 每2分钟执行一次直播检测
            registry.Schedule(() => LiveTimer()).WithName("LiveTimer").NonReentrant().ToRunNow().AndEvery(2).Minutes();

            JobManager.JobException += JobManager_JobException;

            JobManager.Initialize(registry);
        }

        private static void JobManager_JobException(JobExceptionInfo obj)
        {
            Log.Info($"定时器 {obj.Name} 发生错误:{obj.Exception.Message}");
        }

        private void LiveTimer()
        {
            //读取需要录制的主播
            LoadRecordFile();

            List<string> list = new List<string>();

            list.AddRange(RecordTaskDic.Keys);

            int num = 0;

            foreach (var item in list)
            {
                if ((RecordTaskDic[item] != null) && (RecordTaskDic[item].Status == TaskStatus.Running))
                {
                    num++;
                }
            }

            Log.Info($"开始检测主播直播状态 {num}/{list.Count}");

            foreach (var item in list)
            {
                if ((RecordTaskDic[item] != null) && (RecordTaskDic[item].Status == TaskStatus.Running))
                {
                    //正在录制中
                }
                else
                {
                    RecordTaskDic[item] =
                    Task.Factory.StartNew(() =>
                     {
                         StartRecord(item);
                     }, TaskCreationOptions.LongRunning);

                    Thread.Sleep(200);
                }
            }
        }

        private void StartRecord(string author)
        {
            string roomUrl = $"https://play.afreecatv.com/{author}";

            string text = HttpHelper.Get(roomUrl, UserAgent, Cookie);

            //File.WriteAllText($"{author}_Room.txt", text);

            string BjId = author;

            string BroadNo = text.Substring("var nBroadNo = ", ";");

            //查看直播状态
            if (BroadNo == "null")
            {
                Log.Info($"主播直播状态:未开播 {author}");

                return;
            }
            else
            {
                Log.Info($"主播直播状态:直播中 {author}");
            }

            //提取直播视频地址
            string postData = $"bid={BjId}&bno={BroadNo}&type=live&pwd=&player_type=html5&stream_type=common&quality=original&mode=landing";

            string postUrl = $"https://live.afreecatv.com/afreeca/player_live_api.php?bjid={BjId}";

            string postText = HttpHelper.Post(postUrl, postData, UserAgent, Cookie);

            //File.WriteAllText($"{author}_Live.txt", postText);

            if (postText.Contains("\"BPWD\":\"Y\""))
            {
                Log.Info($"需要密码进入直播间:{author}");

                return;
            }

            postData = $"bid={BjId}&bno={BroadNo}&type=aid&pwd=&player_type=html5&stream_type=common&quality=original&mode=landing";

            postText = HttpHelper.Post(postUrl, postData, UserAgent, Cookie);

            //File.WriteAllText($"{author}_Stream.txt", postText);

            if (string.IsNullOrWhiteSpace(postText))
            {
                Log.Info($"获取直播源出错:{author}");

                return;
            }

            string aid = postText.Substring("\"AID\":\"", "\"");

            if (string.IsNullOrEmpty(aid))
            {
                Log.Info($"AID获取失败:{author}");

                return;
            }

            string viewUrl = HttpHelper.Get($"https://livestream-manager.afreecatv.com/broad_stream_assign.html?return_type=gcp_cdn&use_cors=true&cors_origin_url=play.afreecatv.com&broad_key={BroadNo}-common-original-hls", UserAgent, Cookie);

            //File.WriteAllText($"{author}_ViewUrl.txt", viewUrl);

            if (string.IsNullOrEmpty(viewUrl))
            {
                Log.Info($"获取直播源验证地址失败:{author}");

                return;
            }

            string url = viewUrl.Substring("\"view_url\":\"", "\"") + $"?aid={aid}";

            if (!url.ToLower().Contains(".m3u8"))
            {
                Log.Info($"获取直播M3U8地址失败:{author}");

                return;
            }

            DateTime recDateTime = DateTime.Now;

            string fileName = $"{author}_{recDateTime.ToString("yyyyMMddHHmmss")}.mp4";

            string saveName = Path.Combine(txtSaveDir.Text, author, DateTime.Now.ToString("yyyy-MM-dd"), fileName);

            FileInfo fileInfo = new FileInfo(saveName);

            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            string args = $"-seekable 0 -http_seekable 0 -rw_timeout 6000000 -multiple_requests 1 -icy 0 -referer \"{roomUrl}\" -user_agent \"{UserAgent}\" -i \"{url}\" -c copy -metadata title=\"{Title}\" -metadata comment=\"{Comment}\" \"{saveName}\"";

            Log.Info($"开始直播录制:{author}");

            //Log.Info($"FFmpeg参数:{args}");

            bool runOk = ProcessHelper.RunProgram("ffmpeg.exe", args);

            if (runOk)
            {
                Log.Info($"直播录制完成:{author}");
            }
            else
            {
                Log.Info($"直播录制出错:{author}");
            }

            fileInfo = new FileInfo(saveName);

            //大于50MB的视频进行转存
            if (fileInfo.Exists && (fileInfo.Length >= 1024 * 1024 * 50))
            {
                if (!string.IsNullOrEmpty(SaveAsDir))
                {
                    string dstName = saveName.Replace(SaveDir, SaveAsDir);

                    FileInfo dstFileInfo = new FileInfo(dstName);

                    if (!dstFileInfo.Directory.Exists)
                    {
                        dstFileInfo.Directory.Create();
                    }

                    try
                    {
                        File.Move(saveName, dstName);
                    }
                    catch (Exception exception)
                    {
                        Log.Info($"{saveName} 移动到 {dstName} 出错 Exception:{exception.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 清空回收站
        /// </summary>
        private void EmptyRecycleBinTimer()
        {
            ClearRecycle.Clear(this);
        }

        /// <summary>
        /// 加载主播录制设置
        /// </summary>
        private void LoadRecordFile()
        {
            Log.Info("开始加载主播录制列表");

            string file = "Record.txt";

            //读取录制列表
            if (File.Exists(file))
            {
                string[] strArr = File.ReadAllLines(file);

                foreach (var item in strArr)
                {
                    string id = item.Trim();

                    if (!string.IsNullOrEmpty(id))
                    {
                        if (!id.Contains("#"))
                        {
                            if (!RecordTaskDic.ContainsKey(id))
                            {
                                RecordTaskDic.Add(id, null);
                            }
                        }
                    }
                }
            }
            else
            {
                File.WriteAllText(file, "");
            }

            if (RecordTaskDic.Count == 0)
            {
                Log.Info($"请在{file}中填写要录制的主播,每行一个,以#开头为注释");
            }
        }

        #region 结束子进程

        // Enumerated type for the control messages sent to the handler routine
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll")]
        public static extern bool GenerateConsoleCtrlEvent(CtrlTypes CtrlType, int processGroupId);

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        public void CloseProcess(Process process)
        {
            if (AttachConsole(process.Id))
            {
                //Disable Ctrl-C handling for our program
                SetConsoleCtrlHandler(IntPtr.Zero, true);   //设置自己的ctrl+c处理，防止自己被终止

                //Sent Ctrl-C to the attached console
                GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0); // 发送ctrl+c（注意：这是向所有共享该console的进程发送）

                FreeConsole();

                //30秒超时退出
                process.WaitForExit(1000 * 30);

                //Re-enable Ctrl-C handling or any subsequently started programs will inherit the disabled state.
                SetConsoleCtrlHandler(IntPtr.Zero, false);  //重置此参数
            }
        }

        public void KillChildrenProcess()
        {
            int pid = Process.GetCurrentProcess().Id;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);

            ManagementObjectCollection moc = searcher.Get();

            foreach (ManagementObject mo in moc)
            {
                try
                {
                    Process process = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));

                    CloseProcess(process);
                }
                catch (Exception exception)
                {
                    Log.Info($"关闭进程失败:{exception.Message}");
                }
            }
        }

        #endregion
    }
}
