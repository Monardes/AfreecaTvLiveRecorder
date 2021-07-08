using System;
using System.Runtime.InteropServices;

public class ClearRecycle
{
    [DllImport("shell32.dll")]
    private static extern int SHEmptyRecycleBin(IntPtr handle, string root, int falgs);

    const int SHERB_NOCONFIRMATION = 0x000001;

    const int SHERB_NOPROGRESSUI = 0x000002;

    const int SHERB_NOSOUND = 0x000004;

    /// <summary>
    /// 清空回收站
    /// </summary>
    /// <param name="form"></param>
    public static void Clear(System.Windows.Forms.Form form)
    {
        SHEmptyRecycleBin(form.Handle, "", SHERB_NOCONFIRMATION + SHERB_NOPROGRESSUI + SHERB_NOSOUND);
    }
}
