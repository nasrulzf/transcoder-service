using System.Diagnostics;

namespace TranscoderService.Console;

public class ShakaPackagerWrapper
{
    public string PackagerPath { get; set; } = "/app/third-party/packager-linux-x64"; // Default path in the Dockerfile

    public string Execute(string arguments)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = PackagerPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Shaka Packager error: {error}");
            }

            return output;
        }
    }
}