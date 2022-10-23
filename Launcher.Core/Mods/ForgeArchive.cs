using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Launcher.Core.Mods
{
    internal static class ForgeArchive
    {
        internal static string GetVanillaVersionFromZip(ZipArchive zipFile)
        {
            ZipArchiveEntry entry = zipFile.GetEntry("fmlversion.properties");
            if (entry == null) throw new ForgeException("Invalid Forge universal/client archive.");
            using (var stream = entry.Open())
            {
                try
                {
                    var prop = new PropertiesFile(stream);

                    var mcVersion = prop["fmlbuild.mcversion"];
                    if (mcVersion == null) mcVersion = prop["fmlbuild.mcclientversion"];
                    if (mcVersion == null) throw new ForgeException("Invalid Forge universal/client archive.");

                    return mcVersion;
                }
                catch (Exception)
                {
                    throw new ForgeException("Invalid Forge universal/client archive.");
                }
            }
        }

        internal static string GetForgeVersionFromUniversalZip(string zipPath)
        {
            using (var zipFile = ZipFile.OpenRead(zipPath))
            {
                ZipArchiveEntry entry = zipFile.GetEntry("forgeversion.properties");
                if (entry == null) throw new ForgeException("Invalid Forge universal archive.");
                using (var stream = entry.Open())
                {
                    try
                    {
                        var prop = new PropertiesFile(stream);

                        string major = prop["forge.major.number"];
                        if (major == null) throw new ForgeException("Invalid Forge universal archive.");
                        string minor = prop["forge.minor.number"];
                        if (minor == null) throw new ForgeException("Invalid Forge universal archive.");
                        string revision = prop["forge.revision.number"];
                        if (revision == null) throw new ForgeException("Invalid Forge universal archive.");
                        string build = prop["forge.build.number"];
                        if (build == null) throw new ForgeException("Invalid Forge universal archive.");

                        return $"{major}.{minor}.{revision}.{build}";
                    }
                    catch (Exception)
                    {
                        throw new ForgeException("Invalid Forge universal archive.");
                    }
                }
            }
        }

        internal static async Task InstallByPatchingAsync(GamePaths gamePaths, Java.Info javaInfo, Forge.ForgeVersion forgeVersion, string forgeVersionId, string forgeZipPath, VersionsManifest versionsManifest)
        {
            //fetch the VersionFile for the needed Minecraft version
            await versionsManifest.DownloadIfMissing(forgeVersion.MinecraftFull);
            var vanillaVersionFile = VersionFile.Load(gamePaths, javaInfo, forgeVersion.MinecraftFull);

            using (var tempFile = new TempFile())
            {
                string inJarFile = vanillaVersionFile.ClientJarFilePath;

                // download the vanilla jar file if we don't have it already
                if (!File.Exists(inJarFile))
                {
                    await Libraries.DownloadClientAsync(vanillaVersionFile, tempFile);
                }

                string outJarFile = Path.ChangeExtension(VersionFile.GetVersionFilePathFor(gamePaths, forgeVersionId), ".jar");

                Directory.CreateDirectory(Path.GetDirectoryName(outJarFile));

                // merge the files

                using (ZipArchive inPatch = new ZipArchive(File.OpenRead(forgeZipPath), ZipArchiveMode.Read))
                using (ZipArchive inJar = new ZipArchive(File.OpenRead(inJarFile), ZipArchiveMode.Read))
                using (ZipArchive outJar = new ZipArchive(File.Create(tempFile), ZipArchiveMode.Create))
                {
                    var entries = new List<ZipArchiveEntry>();

                    // add all files from inPatch to the list
                    inPatch.Entries.Where(entry => !entry.FullName.StartsWith("META-INF/")).ToList();

                    // add all files from inJar to the list that aren't already in and skip META-INF/
                    foreach (var entry in inJar.Entries)
                    {
                        if (entry.FullName.StartsWith("META-INF/")) continue;

                        bool alreadyPresent = entries.Where(e => e.FullName == entry.FullName).FirstOrDefault() != null;
                        if (!alreadyPresent)
                        {
                            entries.Add(entry);
                        }
                    }

                    //write the listed entries to the new file
                    foreach (var inEntry in entries)
                    {
                        var outEntry = outJar.CreateEntry(inEntry.FullName);
                        using (var inStream = inEntry.Open())
                        using (var outStream = outEntry.Open())
                        {
                            inStream.CopyTo(outStream);
                        }
                    }
                }
                File.Copy(tempFile, outJarFile, true);
            }

            // create <version>.json
            try
            {
                string vanillaVersionFilePath = VersionFile.GetVersionFilePathFor(gamePaths, vanillaVersionFile.ID);
                var json = (JsonObject)JsonNode.Parse(File.ReadAllText(vanillaVersionFilePath));
                json["id"] = forgeVersionId;
                string forgeVersionFilePath = VersionFile.GetVersionFilePathFor(gamePaths, forgeVersionId);
                File.WriteAllText(forgeVersionFilePath, json.ToJsonString());
            }
            catch (Exception)
            {
                throw new ForgeException("Error creating version file.");
            }

            // update luncher_profiles.json
            try
            {
                var forgeProfileKey = Forge.GetProfileKey(forgeVersion);

                ProfilesFile.CreateFileIfNotPresent();
                var profiles = ProfilesFile.LoadProfiles();
                profiles[forgeProfileKey] = new LauncherProfiles.Profile
                {
                    Name = forgeProfileKey,
                    LastVersionId = forgeVersionId
                };

                ProfilesFile.StoreProfiles(profiles);
            }
            catch (Exception)
            {
                throw new ForgeException("Error updating profiles.");
            }
        }
    }
}
