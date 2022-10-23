using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Launcher.Core.Mods
{
    internal class ByteCodePatcher
    {
        // TODO: add other versions that require patching to the list (and hope the same patch works)
        // older 1.7.10 versions supposedly (that I didn't check and don't know if it makes sense supporting)
        // down to at least 1.6.4, don't know more (though my tests show that the latest 1.6.4 works out of the box)
        private static readonly string[] SortPatchRequired = new string[] {
            "1.7.2-10.12.2.1161"
        };

        // TODO: add other versions that require patching to the list, they should be around those that are already added
        // or maybe it doesn't make sense to support older builds of the same version?
        private static readonly string[] SunPatchRequired = new string[]
        {
            "1.16.3-34.1.42", "1.16.4-35.1.4", "1.16.4-35.1.37"
        };

        internal static void PatchIfRequired(GamePaths gamePaths, Java.Info javaInfo, Forge.ForgeVersion forgeVersion, string forgeVersionId, string forgeVersionFilePath)
        {
            string patchToApply;
            string libPath;
            if (SortPatchRequired.Contains(forgeVersion.Full))
            {
                patchToApply = "CoreModManager_Sort_Patch";
                string name = forgeVersion.Full + forgeVersionId.Substring(forgeVersionId.LastIndexOf('-'));
                libPath = Path.Combine(gamePaths.LibrariesDir, "net", "minecraftforge", "forge", name, $"forge-{name}.jar");
            }
            else if (SunPatchRequired.Contains(forgeVersion.Full))
            {
                patchToApply = "SecureJarHandler_ManifestEntryVerifier_Patch";

                // load the version.json and json match it. name is the capture
                string versionJson = File.ReadAllText(forgeVersionFilePath);
                var m = Regex.Match(versionJson, @"modlauncher-(\d+\.\d+\.\d+).jar");
                if (!m.Success) throw new ForgeException("Patching failed.");

                string name = m.Groups[1].Value;
                libPath = Path.Combine(gamePaths.LibrariesDir, "cpw", "mods", "modlauncher", name, $"modlauncher-{name}.jar");
            }
            else
            {
                return;
            }

            if (javaInfo.DescriptiveJavaVersion < 7)
                throw new ForgeException("Running the bytecode patcher requires at least Java 7.");

            if (!File.Exists(libPath)) throw new ForgeException("A patch is required for this version of Forge but the launcher can't find the file that needs patching.");

            using (Process proc = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = javaInfo.JavaBinary,
                Arguments = $"-cp commons-lang3-3.12.0.jar{Path.PathSeparator}bcel-6.6.0.jar{Path.PathSeparator}\"{gamePaths.JavaLauncherLibrary}\" obsidiancore.launcher.bcp.Patcher {patchToApply} \"{libPath}\""
            }))
            {
                proc.WaitForExit();
                if (proc.ExitCode != 0) throw new ForgeException("Patching the bytecode failed.");
            }
        }
    }
}
