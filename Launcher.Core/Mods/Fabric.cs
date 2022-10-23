using System;
using System.Diagnostics;
using System.IO;

// https://fabricmc.net/wiki/install

namespace Launcher.Core.Mods
{
    internal static class Fabric
    {
        internal static void Install(GamePaths gamePaths, Java.Info javaInfo, string minecraftVersion)
        {
            var fabricDir = Path.Combine(Launcher.BaseDir, "fabric");

            //find the installer
            var files = Directory.GetFiles(fabricDir, "fabric-installer-*.jar", SearchOption.TopDirectoryOnly);
            if (files.Length == 0) throw new FabricException("Couldn't find the Fabric installer.");
            if (files.Length > 1) throw new FabricException("Too many Fabric installers. Only provide one.");
            var installerJarFile = files[0];

            ProfilesFile.CreateFileIfNotPresent(); //the Fabric installer requires this file to be present

            using (Process proc = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = javaInfo.JavaBinary,
                Arguments = $"-jar \"{Path.GetFileName(installerJarFile)}\" client -dir \"{gamePaths.DataDir}\" -mcversion \"{minecraftVersion}\"",
                WorkingDirectory = "./fabric"
            }))
            {
                proc.WaitForExit();

                if (proc.ExitCode != 0) throw new FabricException("Installation failed");

                var versionId = ReadVersionIdFromProfile(gamePaths, minecraftVersion);
                if (versionId == null) throw new FabricException("Installation failed");
            }
        }

        internal static string ReadVersionIdFromProfile(GamePaths gamePaths, string minecraftVersion)
        {
            try
            {
                // profile name: fabric-loader-{mcVersion}
                string profileId = $"fabric-loader-{minecraftVersion}";

                var profiles = ProfilesFile.LoadProfiles();
                return profiles[profileId]?.LastVersionId;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class FabricException : ApplicationException
    {
        internal FabricException(string message) : base(message) { }
    }
}
