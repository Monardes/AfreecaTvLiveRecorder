using System.Diagnostics;

public class ProcessHelper
{
    public static bool RunProgram(string fileName, string arguments = null, bool createNoWindow = true, string workingDirectory = null)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = fileName;

            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = createNoWindow;

            process.StartInfo.UseShellExecute = false;

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            process.Start();

            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }

    public static bool RunProgram(string fileName, out string output, out string error, string arguments = null, bool createNoWindow = true)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = fileName;

            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = createNoWindow;

            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.RedirectStandardError = true;

            process.Start();

            var tStandardOutput = process.StandardOutput.ReadToEndAsync();

            var tStandardError = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            output = tStandardOutput.Result;

            error = tStandardError.Result;

            return process.ExitCode == 0;
        }
    }
}

