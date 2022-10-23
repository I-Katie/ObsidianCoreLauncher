using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Launcher.Core
{
    // reads the official launcher's .json files that describe how to download and launch the game
    internal class VersionFile
    {
        private const int SupportedLauncherVersion = 21;
        private const int SupportedComplianceLevel = 1;

        private const string BaseLibraryUrl = @"https://libraries.minecraft.net/";

        private readonly GamePaths gamePaths;
        private readonly Java.Info javaInfo;

        internal GameConfig.AssetConfig AssetConfig { private set; get; } //info on the assets the game uses

        internal string ID { private set; get; } //example: 1.29.2
        internal string GameType { private set; get; } //examples: release, snapshot, old_alpha, old_beta

        internal string MainClass { private set; get; } //example: net.minecraft.client.main.Main

        internal int? JavaVersionMajor { private set; get; } //even the oldest version has it, but I'm making it nullable because theoretically we could do without

        private List<string> jvmArgs; //list of arguments to pass to the java virtual machine
        private List<string> gameArgs; //list of arguments to pass to the game
        private string minecraftArguments;

        private LoggingInfo loggingInfo;

        private VersionFile(GamePaths gamePaths, Java.Info javaInfo)
        {
            this.gamePaths = gamePaths;
            this.javaInfo = javaInfo;
        }

        internal List<LibraryInfo> Libraries { private set; get; } = new List<LibraryInfo>(); //list of libraries required to launch the game

        internal ClientJarInfo ClientJarDownloadInfo { private set; get; } //info on the client.jar file download

        internal static VersionFile Load(GamePaths gamePaths, Java.Info javaInfo, string version)
        {
            var versionFile = LoadInternal(version, gamePaths, javaInfo);

            //Since the files can be loaded with dependencies and each single file doesn't need to provide all the information
            //required to start the game, we now check that we do in fact have all the required information.

            if (versionFile.AssetConfig == null)
                throw new VersionFileException("The version file didn't provide asset info.");
            if (versionFile.ID == null)
                throw new VersionFileException("The version file didn't provide the game ID.");
            if (versionFile.GameType == null)
                throw new VersionFileException("The version file didn't provide the game type.");
            if (versionFile.MainClass == null)
                throw new VersionFileException("The version file didn't provide the main class.");
            //jvmArgs don't need to be checked, they can in fact be empty
            if (versionFile.gameArgs == null && versionFile.minecraftArguments == null)
                throw new VersionFileException("The version file didn't provide the game arguments.");
            if (versionFile.Libraries.Count == 0)
                throw new VersionFileException("The version file didn't provide information on the required libraries.");

            return versionFile;
        }

        internal static string GetVersionFilePathFor(GamePaths gamePaths, string versionId)
        {
            return Path.Combine(gamePaths.VersionsDir, $"{versionId}{Path.DirectorySeparatorChar}{versionId}.json");
        }

        internal string ClientJarFilePath => Path.ChangeExtension(GetVersionFilePathFor(gamePaths, ID), ".jar");

        private static VersionFile LoadInternal(string versionId, GamePaths gamePaths, Java.Info javaInfo)
        {
            string fileName = GetVersionFilePathFor(gamePaths, versionId);
            try
            {
                JsonObject json;
                try
                {
                    json = (JsonObject)JsonNode.Parse(File.ReadAllText(fileName));
                }
                catch (Exception)
                {
                    if (!File.Exists(fileName))
                        throw new VersionFileException($"Missing file \"{versionId}.json\".");
                    else
                        throw new VersionFileException($"Error reading file \"{versionId}.json\".");
                }

                var minimumLauncherVersion = json["minimumLauncherVersion"]?.GetValue<int>() ?? 0;
                if (minimumLauncherVersion > SupportedLauncherVersion)
                    throw new VersionFileException($"Required launcher version too high ({minimumLauncherVersion}).");

                var complianceLevel = json["complianceLevel"]?.GetValue<int>() ?? 0;
                if (complianceLevel > SupportedComplianceLevel)
                    throw new VersionFileException($"Required compliance level too high ({complianceLevel}).");

                VersionFile cfg;
                if (json["inheritsFrom"] != null)
                {
                    string inheritsFrom = json["inheritsFrom"].GetValue<string>();
                    cfg = LoadInternal(inheritsFrom, gamePaths, javaInfo);
                }
                else
                {
                    cfg = new VersionFile(gamePaths, javaInfo);
                }

                if (json["assetIndex"] != null)
                {
                    var assetIndex = json["assetIndex"];
                    cfg.AssetConfig = new GameConfig.AssetConfig
                    {
                        Id = assetIndex["id"].GetValue<string>(),
                        Url = assetIndex["url"].GetValue<string>(),
                        Sha1 = assetIndex["sha1"].GetValue<string>()
                    };
                }

                if (json["id"] != null)
                    cfg.ID = json["id"].GetValue<string>();

                if (json["type"] != null)
                    cfg.GameType = json["type"].GetValue<string>();

                if (json["mainClass"] != null)
                    cfg.MainClass = json["mainClass"].GetValue<string>();

                if (json["javaVersion"]?["majorVersion"] != null)
                    cfg.JavaVersionMajor = json["javaVersion"]["majorVersion"].GetValue<int>();

                if (json["arguments"] != null)
                {
                    var arguments = json["arguments"];
                    if (arguments["jvm"] != null)
                    {
                        if (cfg.jvmArgs == null) cfg.jvmArgs = new List<string>();
                        ProcessArguments((JsonArray)arguments["jvm"], cfg.jvmArgs, javaInfo);
                    }

                    if (arguments["game"] != null)
                    {
                        if (cfg.gameArgs == null) cfg.gameArgs = new List<string>();
                        ProcessArguments((JsonArray)arguments["game"], cfg.gameArgs, javaInfo);
                    }
                }
                else if (json["minecraftArguments"] != null)
                {
                    //older versions just specify a string
                    cfg.minecraftArguments = json["minecraftArguments"].GetValue<string>();
                }

                if (json["libraries"] != null)
                {
                    var libraries = (JsonArray)json["libraries"];
                    var libs = new List<LibraryInfo>();

                    foreach (var lib in libraries)
                    {
                        if (lib["rules"] != null && !CheckRules((JsonArray)lib["rules"], javaInfo)) continue;

                        string libName = lib["name"]?.GetValue<string>();
                        if (libName == null) continue; //I've only seen this in Forge with a link to a Maven url for nothing specific

                        var match = Regex.Match(libName, @"(.+?):(.+?):(.+)"); //name: <package>:<name>:<version>
                        if (!match.Success) throw new VersionFileException("Wrong library name format.");

                        var li = new LibraryInfo
                        {
                            fullName = libName,
                            package = match.Groups[1].Value,
                            name = match.Groups[2].Value,
                            version = match.Groups[3].Value
                        };

                        if (lib["downloads"] != null)
                        {
                            var downloads = lib["downloads"];
                            if (downloads["artifact"] != null)
                            {
                                li.artifact = new LibraryInfo.ArtifactInfo
                                {
                                    path = downloads["artifact"]["path"].GetValue<string>(),
                                    sha1 = downloads["artifact"]["sha1"].GetValue<string>(),
                                    url = downloads["artifact"]["url"].GetValue<string>()
                                };
                            }

                            if (lib["natives"] != null)
                            {
                                var natives = lib["natives"];
                                li.artifact = null; //so far it seems that when natives are specified the artifact is duplicated in the preceding entry and this does nothing

                                string nativeClassifier = natives[GetOSName()]?.GetValue<string>();
                                if (nativeClassifier != null)
                                {
                                    nativeClassifier = nativeClassifier.Replace("${arch}", javaInfo.Is64Bit ? "64" : "32");

                                    var classifier = downloads["classifiers"][nativeClassifier];
                                    if (classifier != null) //it's possible for a native classifier for a given OS to be specified but for it to not be in the list of classifiers
                                    {
                                        li.native = new LibraryInfo.ArtifactInfo
                                        {
                                            path = classifier["path"].GetValue<string>(),
                                            sha1 = classifier["sha1"].GetValue<string>(),
                                            url = classifier["url"].GetValue<string>()
                                        };
                                    }
                                }

                                if (li.native == null)
                                    throw new InternalException();
                            }
                        }
                        else //lib.downloads == null
                        {
                            string baseUrl = BaseLibraryUrl;
                            if (lib["url"] != null) baseUrl = lib["url"].GetValue<string>();

                            if (lib["natives"] == null)
                            {
                                string url = $"{baseUrl}{li.package.Replace('.', '/')}/{li.name}/{li.version}/{li.name}-{li.version}.jar";
                                //the sha1 can supposedly be aquired by fetching the link: (url + ".sha1") but I haven't tried it
                                li.artifact = new LibraryInfo.ArtifactInfo
                                {
                                    path = url.Substring(baseUrl.Length),
                                    sha1 = null,
                                    url = url
                                };
                            }
                            else //natives != null
                            {
                                var natives = lib["natives"];

                                string nativeString = natives[GetOSName()]?.GetValue<string>();
                                if (nativeString != null)
                                {
                                    nativeString = nativeString.Replace("${arch}", javaInfo.Is64Bit ? "64" : "32");
                                    string url = $"{baseUrl}{li.package.Replace('.', '/')}/{li.name}/{li.version}/{li.name}-{li.version}-{nativeString}.jar";
                                    //the sha1 can supposedly be aquired by fetching the link: (url + ".sha1") but I haven't tried it
                                    li.native = new LibraryInfo.ArtifactInfo
                                    {
                                        path = url.Substring(baseUrl.Length),
                                        sha1 = null,
                                        url = url
                                    };
                                }
                            }
                        }

                        if (lib["extract"] != null)
                        {
                            var extract = lib["extract"];

                            List<string> excludeList = new List<string>();
                            foreach (JsonValue el in (JsonArray)extract["exclude"])
                            {
                                excludeList.Add(el.GetValue<string>());
                            }
                            li.extractExcludeList = excludeList;
                        }

                        libs.Add(li);
                    }

                    // remove old libraries that have been replaced with newer ones
                    var newNames = from l in libs
                                   select l.name;
                    cfg.Libraries = (from l in cfg.Libraries
                                     where !newNames.Contains(l.name)
                                     select l).ToList();
                    cfg.Libraries.InsertRange(0, libs);
                }

                if (json["downloads"]?["client"] != null)
                {
                    var client = json["downloads"]["client"];
                    cfg.ClientJarDownloadInfo = new ClientJarInfo
                    {
                        url = client["url"].GetValue<string>(),
                        sha1 = client["sha1"].GetValue<string>()
                    };
                }

                if (json["logging"] != null)
                {
                    var client = json["logging"]["client"];
                    if (client != null)
                    {
                        cfg.loggingInfo = new LoggingInfo
                        {
                            type = client["type"].GetValue<string>(),
                            argument = client["argument"].GetValue<string>()
                        };
                    }
                    else
                    {
                        // if there is an entry for logging but it's empty it removes the inherited logging settings
                        cfg.loggingInfo = null;
                    }
                }

                return cfg;
            }
            catch (VersionFileException)
            {
                throw;
            }
            catch (InternalException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new VersionFileException($"Error loading file \"{versionId}.json\".");
            }
        }

        internal static bool CheckRules(JsonArray rules, Java.Info javaInfo)
        {
            foreach (JsonObject rule in rules)
            {
                bool conditionsPassed = true;

                if (rule.ContainsKey("features")) conditionsPassed = false; //we don't have features in this launcher

                //from what I've seen
                //a rule can have an action without conditions
                //a rule can have an action with a condition
                //if the action is dissalow there will be an allow action before it without conditions

                if (rule["os"] != null)
                {
                    var os = rule["os"];

                    if (os["name"] != null)
                    {
                        if (os["name"].GetValue<string>() != GetOSName()) conditionsPassed = false;
                    }

                    if (os["version"] != null)
                    {
                        var osVersion = os["version"].GetValue<string>();
                        try
                        {
                            if (!Regex.Match(javaInfo.OSVersion, osVersion).Success)
                            {
                                conditionsPassed = false;
                            }
                        }
                        catch (Exception)
                        {
                            throw new VersionFileException("Value for rule os.version is not valid.");
                        }
                    }

                    if (os["arch"] != null)
                    {
                        var arch = os["arch"].GetValue<string>();
                        if (arch == "x86")
                        {
                            if (javaInfo.Is64Bit) conditionsPassed = false;
                        }
                        else
                        {
                            throw new VersionFileException($"Unexpected os.arch \"{arch}\".");
                        }
                    }
                }

                string action = rule["action"].GetValue<string>();
                if (action == "allow")
                {
                    if (!conditionsPassed) return false;
                }
                else if (action == "disallow")
                {
                    if (conditionsPassed) return false;
                }
                else
                {
                    throw new VersionFileException($"Unexpected rule action \"{action}\".");
                }
            }

            return true;
        }

        internal static void ProcessArguments(JsonArray json, List<string> args, Java.Info javaInfo)
        {
            foreach (var arg in json)
            {
                if (arg is JsonValue strVal) //string argument
                {
                    args.Add(strVal.GetValue<string>());
                }
                else if (arg is JsonObject) //conditional argument
                {
                    if (CheckRules((JsonArray)arg["rules"], javaInfo))
                    {
                        //there can be one string value or a list of them
                        if (arg["value"] is JsonValue val2)
                        {
                            args.Add(val2.GetValue<string>());
                        }
                        else if (arg["value"] is JsonArray arr)
                        {
                            foreach (JsonValue el in arr)
                            {
                                args.Add(el.GetValue<string>());
                            }
                        }
                        else
                        {
                            throw new VersionFileException("Argument is neither a string nor a list of strings.");
                        }
                    }
                }
            }
        }

        private static string GetOSName()
        {
            if (Platform.OperatingSystem == Platform.OS.Windows)
            {
                return "windows";
            }
            else if (Platform.OperatingSystem == Platform.OS.Linux)
            {
                return "linux";
            }
            else if (Platform.OperatingSystem == Platform.OS.OSX)
            {
                return "osx";
            }
            else
            {
                throw new InternalException($"Unexpected OS \"{Environment.OSVersion.Platform}\".");
            }
        }

        //VM arguments, MainClass, Minecraft arguments
        internal (string, string, string) GetArguments()
        {
            void EscapeListElementsIfNecessary(List<string> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var val = list[i];
                    if (val.IndexOf(" ") >= 0)
                    {
                        val = $"\"{val}\"";
                        list[i] = val;
                    }
                }
            }

            string GetLoggingInfoArgs()
            {
                if (!loggingInfo.type.Contains("log4j2")) throw new InternalException("Unsupported logger.");
                return ' ' + loggingInfo.argument.Replace("${path}", Path.Combine(Launcher.BaseDir, "log4j.xml"));
            }

            if (minecraftArguments != null)
            {
                StringBuilder sb = new StringBuilder();

                if (Platform.OperatingSystem == Platform.OS.Windows)
                    sb.Append("-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump").Append(' ');
                else if (Platform.OperatingSystem == Platform.OS.OSX)
                    sb.Append("-XstartOnFirstThread").Append(' ');

                if (!javaInfo.Is64Bit) //it should check if it's x86 but this is technically it
                    sb.Append("-Xss1M").Append(' ');

                string gameJar = ClientJarFilePath;
                if (gameJar.IndexOf(" ") >= 0)
                    gameJar = $"\"{gameJar}\"";

                sb.Append("-Djava.library.path=${natives_directory}").Append(' ');
                sb.Append("-Dminecraft.launcher.brand=${launcher_name}").Append(' ');
                sb.Append("-Dminecraft.launcher.version=${launcher_version}").Append(' ');
                sb.Append("-Dminecraft.client.jar=").Append(gameJar).Append(' ');
                sb.Append("-cp ${classpath}");

                if (loggingInfo != null)
                    sb.Append(GetLoggingInfoArgs());

                return (sb.ToString(), MainClass, minecraftArguments);
            }
            else
            {
                EscapeListElementsIfNecessary(jvmArgs);
                var jvm = jvmArgs.Aggregate((left, right) => $"{left} {right}");

                if (loggingInfo != null)
                    jvm += GetLoggingInfoArgs();

                EscapeListElementsIfNecessary(gameArgs);
                var game = gameArgs.Aggregate((left, right) => $"{left} {right}");

                return (jvm, MainClass, game);
            }
        }

        internal string GetClassPath()
        {
            StringBuilder res = new StringBuilder();

            foreach (var libInfo in Libraries)
            {
                if (libInfo.artifact != null)
                {
                    string libPath = Path.Combine(gamePaths.LibrariesDir, libInfo.artifact.path);
                    if (libPath.IndexOf(" ") >= 0)
                        libPath = $"\"{libPath}\"";

                    res.Append(libPath.Replace('/', Path.DirectorySeparatorChar)).Append(Path.PathSeparator);
                }
            }

            string gameJar = ClientJarFilePath;
            if (gameJar.IndexOf(" ") >= 0)
                gameJar = $"\"{gameJar}\"";
            res.Append(gameJar);

            return res.ToString();
        }

        internal class LibraryInfo
        {
            internal string fullName;
            internal string package, name, version; //fullName split into it's components

            internal ArtifactInfo artifact; //can be null
            internal ArtifactInfo native; //can be null
            internal List<string> extractExcludeList; //can be null

            internal class ArtifactInfo
            {
                //download
                internal string path;
                internal string sha1;
                internal string url;
            }
        }

        internal class ClientJarInfo
        {
            internal string url;
            internal string sha1;
        }

        internal class LoggingInfo
        {
            internal string type;
            internal string argument; //vsebuje: ${path}
        }
    }

    public class VersionFileException : ApplicationException
    {
        internal VersionFileException(string message) : base(message) { }
    }
}
