using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Launcher.Core
{
    static class Java
    {
        internal static Info? GetInfo(GamePaths gamePaths, string javaBinary)
        {
            var dict = GetProperties(gamePaths, javaBinary, new[] { "java.version", "os.version", "os.arch" });
            try
            {
                return new Info
                {
                    JavaBinary = javaBinary,
                    JavaVersion = dict["java.version"],
                    DescriptiveJavaVersion = GetDescriptiveVersion(dict["java.version"]),
                    OSVersion = dict["os.version"],
                    Is64Bit = dict["os.arch"].Contains("64")
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static int GetDescriptiveVersion(string version)
        {
            var match = Regex.Match(version, @"^(\d+)\.(\d+)");
            if (match.Success)
            {
                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);

                if (major > 1)
                    return major;
                else if (major == 1 && minor >= 1)
                    return minor;
                else
                    return 0;
            }
            else
            {
                throw new ApplicationException();
            }
        }

        private static Dictionary<string, string> GetProperties(GamePaths gamePaths, string jvm_bin, string[] keys)
        {
            string args = keys.Aggregate((left, right) => $"{left} {right}");

            var dict = new Dictionary<string, string>();
            try
            {
                string lines;

                using (Process proc = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = jvm_bin,
                    Arguments = $"-cp \"{gamePaths.JavaLauncherLibrary}\" obsidiancore.launcher.util.GetProperties " + args,
                    WorkingDirectory = Launcher.BaseDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    proc.WaitForExit();
                    lines = proc.StandardOutput.ReadToEnd();
                }

                var match = Regex.Match(lines, @"^(.*?)=(.*)$", RegexOptions.Multiline);
                while (match.Success)
                {
                    dict[match.Groups[1].Value] = match.Groups[2].Value;
                    match = match.NextMatch();
                }

                return dict;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        internal struct Info
        {
            internal string JavaBinary { get; set; }

            internal string JavaVersion { get; set; }
            internal int DescriptiveJavaVersion { get; set; }
            internal string OSVersion { get; set; }
            internal bool Is64Bit { get; set; }
        }
    }
}
