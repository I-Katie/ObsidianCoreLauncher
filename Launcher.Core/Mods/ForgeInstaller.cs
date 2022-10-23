using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Launcher.Core.Mods.Forge;

namespace Launcher.Core.Mods
{
    internal class ForgeInstaller
    {
        internal static readonly string[] InstallersBlacklist = { "1.5.2" };

        //(minecraft, forge)
        internal static (string, string) GetVersionIdFromInstaller(string jarPath)
        {
            using (var zipFile = ZipFile.OpenRead(jarPath))
            {
                var entry = zipFile.GetEntry("install_profile.json");
                if (entry == null) throw new ForgeException("Invalid forge installer.");
                using (Stream stream = entry.Open())
                using (StreamReader sr = new StreamReader(stream))
                {
                    try
                    {
                        var ip = JsonSerializer.Deserialize<ForgeInstaller_InstallProfile>(sr.ReadToEnd());
                        if (ip.Version != null)
                        {
                            return (ip.Minecraft, ip.Version);
                        }
                        else if (ip.VersionInfo?.Id != null)
                        {
                            return (ip.Install.Minecraft, ip.VersionInfo.Id);
                        }
                    }
                    catch (Exception)
                    {
                        throw new ForgeException("Invalid forge installer.");
                    }
                }
            }
            throw new ForgeException("Invalid forge installer.");
        }

        internal static async Task InstallWithInstallerAsync(GamePaths gamePaths, Java.Info javaInfo, ForgeVersion forgeVersion, string forgeInstallerFile, string forgeVersionId, VersionsManifest versionsManifest)
        {
            if (InstallersBlacklist.Contains(forgeVersion.MinecraftFull))
                throw new ForgeException($"The Forge installer for Minecraft version {forgeVersion.MinecraftFull} is not supported. Please use the universal archive instead.");

            // The Forge installers use class version 52 which require Java 8. (Although some versions of Forge don't actually run on Java 8 without patching FML.)
            //The installer patch used by the launcher requires Java 6. The bytecode patcher requires Java 7.
            if (javaInfo.DescriptiveJavaVersion < 8)
                throw new ForgeException("Running the Forge installer requires at least Java 8.");

            ProfilesFile.CreateFileIfNotPresent(); //the Forge installer requires this file to be present

            try
            {
                using (Process proc = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = javaInfo.JavaBinary,
                    Arguments = $"-Dobsidiancore.launcher.path=\"{gamePaths.DataDir}\" -Dobsidiancore.launcher.choice=client -cp \"{gamePaths.JavaLauncherLibrary}\"{Path.PathSeparator}\"{forgeInstallerFile}\" obsidiancore.launcher.forge_installer_patch.Patcher",
                    WorkingDirectory = "./forge",
                    RedirectStandardError = true,
                }))
                {
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        if (proc.ExitCode == 2)
                            throw new ForgeException("Forge installer incompatible with launcher patch.");
                        else
                            throw new ForgeException("Error running Forge installer.");
                    }

                    // check if the installed version.json file is missing something important and add it from the vanilla version
                    string forgeVersionFilePath = VersionFile.GetVersionFilePathFor(gamePaths, forgeVersionId);
                    try
                    {
                        JsonObject jsonForge;
                        try
                        {
                            jsonForge = (JsonObject)JsonNode.Parse(File.ReadAllText(forgeVersionFilePath));
                        }
                        catch (JsonException)
                        {
                            //some version.json files provided by Forge are malformed, try to fix it

                            var lines = File.ReadAllLines(forgeVersionFilePath);
                            lines = lines.Where(l => l != ",").ToArray();
                            File.WriteAllLines(forgeVersionFilePath, lines);

                            jsonForge = (JsonObject)JsonNode.Parse(File.ReadAllText(forgeVersionFilePath));
                        }

                        bool versionModified = false;
                        if (!jsonForge.ContainsKey("assetIndex"))
                        {
                            // we are missing asset info (try and get it from the vanilla version)
                            await versionsManifest.DownloadIfMissing(forgeVersion.MinecraftFull);
                            string vanillaVersionPath = VersionFile.GetVersionFilePathFor(gamePaths, forgeVersion.MinecraftFull);

                            var jsonVanilla = (JsonObject)JsonNode.Parse(File.ReadAllText(vanillaVersionPath));

                            jsonForge["assetIndex"] = JsonNode.Parse(jsonVanilla["assetIndex"].ToJsonString());
                            versionModified = true;
                        }
                        if (versionModified)
                        {
                            File.WriteAllText(forgeVersionFilePath, jsonForge.ToJsonString());
                        }
                    }
                    catch (Exception)
                    {
                        throw new ForgeException("Error while adding missing version info.");
                    }

                    // patch the bytecode if patching is required
                    ByteCodePatcher.PatchIfRequired(gamePaths, javaInfo, forgeVersion, forgeVersionId, forgeVersionFilePath);

                    // update profiles

                    string profileKey = Forge.GetProfileKey(forgeVersion); //the profile key we want to use

                    // find the profile, if it's not there it wasn't installed successfully
                    Dictionary<string, LauncherProfiles.Profile> profiles = ProfilesFile.LoadProfiles();
                    string installerKey = null; //the profile key created by the installer
                    foreach (var kv in profiles)
                    {
                        if ((kv.Value.LastVersionId == forgeVersionId) && (kv.Key != profileKey))
                        {
                            installerKey = kv.Key;
                            break;
                        }
                    }
                    if (installerKey == null)
                    {
                        throw new ForgeException("The Forge installation didn't complete successfully.");
                    }
                    else
                    {
                        // rename the profile to our name
                        var profile = profiles[installerKey];
                        profiles.Remove(installerKey);
                        profile.Name = profileKey;
                        profiles[profileKey] = profile;

                        ProfilesFile.StoreProfiles(profiles);
                    }
                }
            }
            catch (Win32Exception)
            {
                throw new ForgeException("Error running Forge installer.");
            }
        }

#pragma warning disable CS0649
        public class ForgeInstaller_InstallProfile
        {
            //either these two are present
            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("minecraft")]
            public string Minecraft { get; set; }

            //or these two are
            [JsonPropertyName("install")]
            public InstallJson Install { get; set; }

            [JsonPropertyName("versionInfo")]
            public VersionInfoJson VersionInfo { get; set; }

            public class InstallJson
            {
                [JsonPropertyName("minecraft")]
                public string Minecraft { get; set; }
            }

            public class VersionInfoJson
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }
            }
        }
#pragma warning restore CS0649
    }
}
