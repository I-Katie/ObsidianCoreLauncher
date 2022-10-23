using Launcher.Core.Mods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Launcher.Core
{
    public static class GameLauncher
    {
        public const string VMDefaults = @"-Xmx2G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M";

        public static async Task LaunchAsync(IPageControl pageControl, Settings settings, LoginData loginData, bool offline)
        {
            try
            {
                await Task.Run(() => PrepareThenLaunchAsync(pageControl, settings, loginData, offline));
            }
            catch (ProfilesException ex)
            {
                throw new LaunchException("Profiles file error", ex);
            }
            catch (FabricException ex)
            {
                throw new LaunchException("Fabric install error", ex);
            }
            catch (ForgeException ex)
            {
                throw new LaunchException("Forge install error", ex);
            }
            catch (VersionsManifestException ex)
            {
                throw new LaunchException("Versions manifest error", ex);
            }
            catch (VersionFileException ex)
            {
                throw new LaunchException("Version file error", ex);
            }
            catch (AssetsException ex)
            {
                throw new LaunchException("Assets error", ex);
            }
            catch (LibrariesException ex)
            {
                throw new LaunchException("Libraries error", ex);
            }
            catch (LaunchException ex)
            {
                throw new LaunchException("Launch error", ex);
            }
            catch (Exception ex)
            {
                throw new LaunchException("Internal error", ex);
            }
        }

        private static async Task PrepareThenLaunchAsync(IPageControl pageControl, Settings settings, LoginData loginData, bool offline)
        {
            string localDataDir = Path.GetFullPath(Path.Combine(Launcher.BaseDir, "data"));
            var gamePaths = new GamePaths
            {
                DataDir = localDataDir,
                GameDir = Path.Combine(Launcher.BaseDir, "game"),
                LibrariesDir = Path.Combine(localDataDir, "libraries"),
                VersionsDir = Path.Combine(localDataDir, "versions"),
                JavaLauncherLibrary = Path.Combine(Launcher.BaseDir, "Launcher.Java.jar")
            };
            ProfilesFile.SetPaths(gamePaths);

            // set up Java information
            Java.Info javaInfo;
            try
            {
                string jvm_bin = settings.JVMBinary;
                if (jvm_bin.Length == 0)
                {
                    if (Launcher.GameConfig.JavaBin != null)
                    {
                        jvm_bin = Launcher.GameConfig.JavaBin;
                        switch (Platform.OperatingSystem)
                        {
                            case Platform.OS.Windows:
                                jvm_bin = jvm_bin.Replace("${os}", "windows");
                                break;
                            case Platform.OS.Linux:
                                jvm_bin = jvm_bin.Replace("${os}", "linux");
                                break;
                            case Platform.OS.OSX:
                                jvm_bin = jvm_bin.Replace("${os}", "osx");
                                break;
                            default:
                                throw new PlatformNotSupportedException();
                        }
                        switch (Platform.Architecture)
                        {
                            case Platform.Arch.X86:
                                jvm_bin = jvm_bin.Replace("${arch}", "x86");
                                break;
                            case Platform.Arch.X64:
                                jvm_bin = jvm_bin.Replace("${arch}", "x64");
                                break;
                            case Platform.Arch.Arm:
                                jvm_bin = jvm_bin.Replace("${arch}", "arm");
                                break;
                            case Platform.Arch.AArch64:
                                jvm_bin = jvm_bin.Replace("${arch}", "aarch64");
                                break;
                            default:
                                throw new PlatformNotSupportedException();
                        }
                    }
                    else
                    {
                        jvm_bin = "java";
                    }
                }
                javaInfo = Java.GetInfo(gamePaths, jvm_bin).Value;
            }
            catch (Exception)
            {
                throw new LaunchException($"Can't find a valid Java JRE.");
            }

            VersionsManifest versionsManifest = null;
            if (Launcher.GameConfig.LaunchArguments == null)
            {
                versionsManifest = new VersionsManifest(gamePaths, offline);
                await versionsManifest.LoadManifestAsync();
            }

            if (Launcher.GameConfig.LaunchForge != null) // forge
            {
                Forge forge = new Forge(gamePaths, javaInfo, versionsManifest, Launcher.GameConfig.LaunchForge);
                string forgeProfile = forge.GetForgeVersionId();
                if (forgeProfile != null)
                {
                    Launcher.GameConfig.LaunchVersion = forgeProfile;
                }
                else
                {
                    pageControl.SetPageWait("Installing Forge...");
                    await forge.InstallAsync();
                    Launcher.GameConfig.LaunchVersion = forge.GetForgeVersionId();
                }
                forge.CopyFMLLibsIfRequired();
            }
            else if (Launcher.GameConfig.LaunchFabric != null) // fabric
            {
                string versionId = Fabric.ReadVersionIdFromProfile(gamePaths, Launcher.GameConfig.LaunchFabric);
                if (versionId != null)
                {
                    Launcher.GameConfig.LaunchVersion = versionId;
                }
                else
                {
                    pageControl.SetPageWait("Installing Fabric...");
                    Fabric.Install(gamePaths, javaInfo, Launcher.GameConfig.LaunchFabric);
                    Launcher.GameConfig.LaunchVersion = Fabric.ReadVersionIdFromProfile(gamePaths, Launcher.GameConfig.LaunchFabric);
                }
            }

            // version file
            VersionFile versionFile = null;
            if (Launcher.GameConfig.LaunchVersion != null)
            {
                pageControl.SetPageWait("Verifying game version data...");
                await versionsManifest.DownloadIfMissing(Launcher.GameConfig.LaunchVersion);

                versionFile = VersionFile.Load(gamePaths, javaInfo, Launcher.GameConfig.LaunchVersion);
                Launcher.GameConfig.Assets = versionFile.AssetConfig;
                var (vmArgs, mainClass, gameArgs) = versionFile.GetArguments();
                Launcher.GameConfig.LaunchArguments = new GameConfig.LaunchArgs
                {
                    VMArgs = vmArgs,
                    MainClass = mainClass,
                    GameArgs = gameArgs
                };
            }
            else if (versionFile == null) //<launch-args>
            {
                string CleanUserArgs(string input) =>
                    Regex.Split(input, @"\r\n?|\n")
                        .Select((arg) => arg.Length > 0 ? arg.Trim() : arg)
                        .Where(arg => arg.Length > 0)
                        .Aggregate((left, right) => $"{left} {right}");

                Launcher.GameConfig.LaunchArguments.VMArgs = CleanUserArgs(Launcher.GameConfig.LaunchArguments.VMArgs)
                    .Replace(";", Path.PathSeparator + ""); //';' is converted to Path.PathSeparator when <launch-args> is used to make the xml file easier to read

                Launcher.GameConfig.LaunchArguments.GameArgs = CleanUserArgs(Launcher.GameConfig.LaunchArguments.GameArgs);
            }

            // java version
            // I'm running everything either in Java 8 or Java 18 and it works fine.
            if ((versionFile?.JavaVersionMajor ?? -1) > javaInfo.DescriptiveJavaVersion)
                throw new LaunchException($"The game requires Java {versionFile.JavaVersionMajor} but found Java {javaInfo.JavaVersion} instead.");

            // assets
            Assets.AssetInfo assetInfo = null;
            if (Launcher.GameConfig.Assets != null)
            {
                assetInfo = await Assets.VerifyAndDownloadAsync(pageControl, gamePaths);
            }

            // libraries and natives
            string nativesDir = Path.Combine(gamePaths.DataDir, "natives");
            if (Launcher.GameConfig.LaunchVersion != null)
            {
                nativesDir = Path.Combine(nativesDir, versionFile.ID);

                await Libraries.VerifyAndDownloadAsync(pageControl, gamePaths, versionFile);

                //extract native libraries
                foreach (var lib in versionFile.Libraries)
                {
                    if (lib.native != null)
                    {
                        gamePaths.NativesDir = nativesDir; //we have native libraries so set the path

                        try
                        {
                            ZipFiles.ExtractZipFile(
                                Path.Combine(gamePaths.LibrariesDir, lib.native.path),
                                nativesDir,
                                lib.extractExcludeList ?? new List<string>()
                                );
                        }
                        catch (IOException)
                        {
                            throw new LibrariesException("Error while extracting native libraries.");
                        }
                    }
                }
            }
            if (gamePaths.NativesDir == null) gamePaths.NativesDir = ".";

            // proceed with launching the game
            pageControl.SetPageWait("Running the game...");
            Launch(gamePaths, loginData, javaInfo, settings, assetInfo, versionFile);
        }

        private static void Launch(GamePaths gamePaths, LoginData loginData, Java.Info javaInfo, Settings settings, Assets.AssetInfo assetInfo, VersionFile versionFile)
        {
            Directory.CreateDirectory(gamePaths.GameDir);

            var vmArgs = FillArgsWithValues(gamePaths, loginData, versionFile, assetInfo, Launcher.GameConfig.LaunchArguments.VMArgs);
            if (settings.JREArguments.Length > 0) vmArgs += " " + settings.JREArguments;

            var mainClass = Launcher.GameConfig.LaunchArguments.MainClass;

            var gameArgs = FillArgsWithValues(gamePaths, loginData, versionFile, assetInfo, Launcher.GameConfig.LaunchArguments.GameArgs);

            try
            {
                string Encode(string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input)); //so that it's sent as a single argument

                string arguments = "";
                arguments += $" -cp \"{gamePaths.JavaLauncherLibrary}\"";
                arguments += $" -Dobsidiancore.launcher.name={Encode(Launcher.GameConfig.Name)}";
                arguments += $" -Dobsidiancore.launcher.java={Encode(javaInfo.JavaBinary)}";
                arguments += $" -Dobsidiancore.launcher.lock={Encode(Launcher.GameConfig.LockFile ?? "\"\"")}";
                arguments += " -Dobsidiancore.launcher.closeOnExit=" + (settings.CloseOnExit ? "true" : "false");
                arguments += $" obsidiancore.launcher.console.Console";
                arguments += $" {vmArgs} {mainClass} {gameArgs}";

                var procInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = javaInfo.JavaBinary,
                    Arguments = arguments,
                    WorkingDirectory = gamePaths.GameDir
                };

                LockFile.Unlock();

                Process.Start(procInfo).Dispose();
            }
            catch (Win32Exception)
            {
                throw new LaunchException("Could not run the game.\nIs \"console.exe\" missing?");
            }
        }

        private static string FillArgsWithValues(GamePaths gamePaths, LoginData loginData, VersionFile versionFile, Assets.AssetInfo assetInfo, string args)
        {
            StringBuilder argsBuilder = new StringBuilder();

            int lastIndex = 0;
            Match match = Regex.Match(args, @"\${(.*?)}", RegexOptions.Compiled);
            while (match.Success)
            {
                int start = match.Index;
                int length = match.Length;
                string key = match.Groups[1].Value;

                argsBuilder.Append(args.Substring(lastIndex, start - lastIndex));
                lastIndex = start + length;

                string value;
                switch (key)
                {
                    //launcher data
                    case "launcher_name":
                        value = Launcher.MinecraftLauncherName;
                        break;
                    case "launcher_version":
                        {
                            var version = Assembly.GetExecutingAssembly().GetName().Version;
                            value = $"{version.Major}.{version.Minor}.{version.Build}";
                            break;
                        }

                    //user (required)
                    case "auth_player_name":
                        value = loginData.PlayerName;
                        break;
                    case "auth_uuid":
                        value = loginData.Uuid;
                        break;
                    case "auth_access_token":
                        value = loginData.AccessToken;
                        break;
                    case "user_type":
                        value = loginData.UserType;
                        break;

                    //user older
                    case "user_properties":
                        value = "{}";
                        break;
                    case "auth_session":
                        value = $"token:{loginData.AccessToken}";
                        break;

                    //user (telemetry only)
                    case "auth_xuid":
                        value = loginData.Xuid;
                        break;
                    case "clientid":
                        value = Convert.ToBase64String(Encoding.UTF8.GetBytes(Launcher.ClientId));
                        break;

                    //classpath
                    case "classpath_separator":
                        value = Path.PathSeparator + "";
                        break;
                    case "classpath":
                        if (versionFile == null) throw new LaunchException($"${{{key}}}' can only be used with <launch-version>.");
                        value = versionFile.GetClassPath();
                        break;

                    //assets
                    case "assets_root":
                        if (assetInfo == null)
                            throw new LaunchException("Referencing ${assets_root} but neither <assets> nor <launch-version> have been set.");
                        else if (assetInfo.Legacy)
                            throw new LaunchException("Referencing ${assets_root} but assets are legacy.");
                        value = assetInfo.AssetsRoot;
                        break;
                    case "assets_index_name":
                        if (Launcher.GameConfig.Assets?.Id == null)
                            throw new LaunchException("Referencing ${assets_index_name} but neither <assets> nor <launch-version> have been set.");
                        value = Launcher.GameConfig.Assets.Id;
                        break;
                    //legacy assets
                    case "game_assets":
                        if (assetInfo == null)
                            throw new LaunchException("Referencing ${game_assets} but neither <assets> nor <launch-version> have been set.");
                        else if (!assetInfo.Legacy)
                            throw new LaunchException("Referencing ${game_assets} but assets aren't legacy.");
                        value = assetInfo.LegacyAssetsDir;
                        if (assetInfo.MapToResources)
                            value = Path.Combine(gamePaths.DataDir, "this_does_not_exist"); //to make old versions work you have to do what the official launcher does and cause an exception by passing an assets dir that doesn't exist
                        break;

                    //game info
                    case "version_name":
                        if (versionFile == null) throw new LaunchException($"${{{key}}}' can only be used with <launch-version>.");
                        value = versionFile.ID;
                        break;
                    case "version_type":
                        if (versionFile == null) throw new LaunchException($"${{{key}}}' can only be used with <launch-version>.");
                        value = versionFile.GameType;
                        break;

                    //directories
                    case "game_directory":
                        value = gamePaths.GameDir;
                        break;
                    case "natives_directory":
                        if (versionFile == null) throw new LaunchException($"${{{key}}}' can only be used with <launch-version>.");
                        value = gamePaths.NativesDir;
                        break;
                    case "library_directory":
                        if (versionFile == null) throw new LaunchException($"${{{key}}}' can only be used with <launch-version>.");
                        value = gamePaths.LibrariesDir;
                        break;

                    //custom resolution (normally this one must be set and enabled in the launcher to appear in the config)
                    case "resolution_width":
                    case "resolution_height":
                        throw new InternalException();

                    //keys specific to this launcher (apart from ;). usefull for <launch-args>
                    case "data_dir":
                        value = gamePaths.DataDir;
                        break;

                    default:
                        throw new LaunchException($"Unknown placeholder ${{{key}}}' in launch arguments.");
                }
                if (value.Length == 0)
                    argsBuilder.Append("\"\"");
                else if (value.IndexOf(" ") >= 0)
                    argsBuilder.Append($"\"{value}\"");
                else
                    argsBuilder.Append(value);

                match = match.NextMatch();
            }
            argsBuilder.Append(args.Substring(lastIndex));

            return argsBuilder.ToString();
        }
    }

    public class LaunchException : ApplicationException
    {
        public LaunchException(string message) : base(message) { }

        public LaunchException(string title, Exception ex) : base(ex.Message)
        {
            Title = title;
        }

        public string Title { get; } = "";
    }
}
