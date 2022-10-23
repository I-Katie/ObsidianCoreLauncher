using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// https://files.minecraftforge.net/net/minecraftforge/forge/

namespace Launcher.Core.Mods
{
    internal class Forge
    {
        internal const string VersionRegex = @"^(([1-9]+)\.([0-9]+)(\.([0-9]+))?)-(([0-9]+)\.([0-9]+)\.([0-9]+)(\.([0-9]+))?)$";

        private readonly GamePaths gamePaths;
        private readonly Java.Info javaInfo;
        private readonly VersionsManifest versionsManifest;
        private readonly ForgeVersion forgeVersion; //version passed to the class (not the same as the versionId of the forge instalation)

        //installers start at version 1.6.1 (1.5.2 has an installer but it doesn't work -even in the official launcher- as it fails to download a file)
        //universal files are used for [1.3.2, 1.5.2]
        //client files are used for [1.1, 1.2.5] //we only support 1.2+

        internal Forge(GamePaths gamePaths, Java.Info javaInfo, VersionsManifest versionsManifest, string forgeVersion)
        {
            this.gamePaths = gamePaths;
            this.javaInfo = javaInfo;
            this.versionsManifest = versionsManifest;
            this.forgeVersion = ParseVersion(forgeVersion);
        }

        private static ForgeVersion ParseVersion(string versionString)
        {
            Match match = Regex.Match(versionString, VersionRegex);
            if (!match.Success) throw new ForgeException("Incorrect Forge version format.");
            var groups = match.Groups;

            ForgeVersion fv = new ForgeVersion();
            fv.Full = versionString;

            fv.MinecraftMajor = int.Parse(groups[2].Value);
            fv.MinecraftMinor = int.Parse(groups[3].Value);
            int.TryParse(groups[5].Value, out fv.MinecraftRevision);
            if (fv.MinecraftRevision == 0)
                fv.MinecraftFull = $"{fv.MinecraftMajor}.{fv.MinecraftMinor}";
            else
                fv.MinecraftFull = groups[1].Value;

            fv.ForgeFull = groups[6].Value;
            fv.ForgeMajor = int.Parse(groups[7].Value);
            fv.ForgeMinor = int.Parse(groups[8].Value);
            fv.ForgeRevision = int.Parse(groups[9].Value);
            int.TryParse(groups[11].Value, out fv.ForgeBuild);

            return fv;
        }

        internal static string GetProfileKey(ForgeVersion forgeVersion) => $"forge--{forgeVersion.Full}";

        internal string GetForgeVersionId()
        {
            string profileKey = GetProfileKey(forgeVersion);
            var profiles = ProfilesFile.LoadProfiles();

            if (profiles?.ContainsKey(profileKey) ?? false)
            {
                return profiles[profileKey].LastVersionId;
            }
            else
            {
                return null;
            }
        }

        internal Task InstallAsync()
        {
            string forgeDir = Path.Combine(Launcher.BaseDir, "forge");

            if (!Directory.Exists(forgeDir)) throw new ForgeException("The directory \"forge\" is missing.");

            //try to find the forge installer
            foreach (string file in Directory.GetFiles(forgeDir, "*-installer.jar", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (name.Contains(forgeVersion.Full))
                {
                    //found the -installer.jar
                    var (minecraftVersion, forgeVersionId) = ForgeInstaller.GetVersionIdFromInstaller(file);

                    if (minecraftVersion != forgeVersion.MinecraftFull)
                        throw new ForgeException("The installer version doesn't match its file name.");
                    //not fully accurate but it should work
                    if (!forgeVersionId.Contains(forgeVersion.ForgeFull))
                        throw new ForgeException("The installer version doesn't match its file name.");

                    return ForgeInstaller.InstallWithInstallerAsync(gamePaths, javaInfo, forgeVersion, file, forgeVersionId, versionsManifest);
                }
            }

            //try to find the universal .zip
            foreach (string file in Directory.GetFiles(forgeDir, "*-universal.zip", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (name.Contains(forgeVersion.Full))
                {
                    //found the -universal.zip
                    string minecraftVersion;
                    using (var zipFile = ZipFile.OpenRead(file))
                        minecraftVersion = ForgeArchive.GetVanillaVersionFromZip(zipFile);
                    var forgeVersion = ForgeArchive.GetForgeVersionFromUniversalZip(file);

                    if (minecraftVersion != this.forgeVersion.MinecraftFull)
                        throw new ForgeException("The universal archive version doesn't match its file name.");
                    if (forgeVersion != this.forgeVersion.ForgeFull)
                        throw new ForgeException("The universal archive version doesn't match its file name.");

                    return ForgeArchive.InstallByPatchingAsync(gamePaths, javaInfo, this.forgeVersion, $"{minecraftVersion}-forge-{forgeVersion}", file, versionsManifest);
                }
            }

            //try to find the client .zip
            foreach (string file in Directory.GetFiles(forgeDir, "*-client.zip", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (name.Contains(forgeVersion.Full))
                {
                    //found the -client.zip
                    if (forgeVersion.MinecraftFull == "1.2.5")
                    {
                        string mcVersion, forgeVersion;
                        using (var zipFile = ZipFile.OpenRead(file))
                        {
                            mcVersion = ForgeArchive.GetVanillaVersionFromZip(zipFile);

                            var entry = zipFile.GetEntry("mod_MinecraftForge.info");
                            if (entry == null) throw new ForgeException("Invalid Forge client archive.");
                            try
                            {
                                using (var stream = entry.Open())
                                using (StreamReader sr = new StreamReader(stream))
                                {
                                    JsonDocument doc = JsonDocument.Parse(sr.ReadToEnd());
                                    var item0 = doc.RootElement.EnumerateArray().First();
                                    forgeVersion = item0.GetProperty("version").GetString();
                                }
                            }
                            catch (Exception)
                            {
                                throw new ForgeException("Invalid Forge client archive.");
                            }
                        }

                        if (mcVersion != this.forgeVersion.MinecraftFull)
                            throw new ForgeException("The universal archive version doesn't match its file name.");
                        if (forgeVersion != this.forgeVersion.ForgeFull)
                            throw new ForgeException("The universal archive version doesn't match its file name.");

                        return ForgeArchive.InstallByPatchingAsync(gamePaths, javaInfo, this.forgeVersion, $"{mcVersion}-forge-{forgeVersion}", file, versionsManifest);
                    }

                    //the other ones don't have FML built in
                    throw new ForgeException($"Unsupported client archive version {forgeVersion.MinecraftFull}.");
                }
            }

            throw new ForgeException("Couldn't find the required Forge installer jar, universal zip archive or client zip archive file.");
        }

        internal void CopyFMLLibsIfRequired()
        {
            // versions for Minecraft [1.3.2, 1.5.2] need additional libraries that don't get automatically downloaded because the links don't work anymore
            // technically each of these versions only requires some of these files but if I just copy all of them it works too
            string[] requireFMLLibs = new[] { "1.3.2", "1.4", "1.4.1", "1.4.2", "1.4.3", "1.4.4", "1.4.5", "1.4.6", "1.4.7", "1.5", "1.5.1", "1.5.2" };
            if (requireFMLLibs.Contains(forgeVersion.MinecraftFull))
            {
                string libDir = Path.Combine(gamePaths.GameDir, "lib");
                Directory.CreateDirectory(libDir);
                Files.DeepCopy(Path.Combine(Launcher.BaseDir, "forge", "fmllibs"), libDir);
            }
        }

        internal struct ForgeVersion
        {
            internal string Full;

            internal string MinecraftFull;
            internal int MinecraftMajor, MinecraftMinor, MinecraftRevision;

            internal string ForgeFull;
            internal int ForgeMajor, ForgeMinor, ForgeRevision, ForgeBuild;
        }
    }

    public class ForgeException : ApplicationException
    {
        internal ForgeException(string message) : base(message) { }
    }
}
