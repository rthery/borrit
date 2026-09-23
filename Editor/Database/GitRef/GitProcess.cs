using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BorritEditor.Database.GitRef
{
    internal readonly struct GitResult
    {
        public readonly int ExitCode;
        public readonly string Output;
        public readonly string Error;

        public bool Success => ExitCode == 0;

        public GitResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }

    internal static class GitProcess
    {
        private const int TimeoutMilliseconds = 60000;

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        public static GitResult Run(string workingDirectory, string arguments, string input = null, IDictionary<string, string> environment = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Utf8,
                StandardErrorEncoding = Utf8
            };
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            if (environment != null)
            {
                foreach (KeyValuePair<string, string> variable in environment)
                {
                    startInfo.EnvironmentVariables[variable.Key] = variable.Value;
                }
            }

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                if (input != null)
                {
                    byte[] inputBytes = Utf8.GetBytes(input);
                    process.StandardInput.BaseStream.Write(inputBytes, 0, inputBytes.Length);
                }
                process.StandardInput.Close();

                if (process.WaitForExit(TimeoutMilliseconds) == false)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    return new GitResult(-1, string.Empty, $"git {arguments} timed out");
                }
                process.WaitForExit();

                return new GitResult(process.ExitCode, outputTask.Result, errorTask.Result);
            }
        }
    }
}
