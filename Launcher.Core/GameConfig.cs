using Launcher.Core.Mods;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Launcher.Core
{
    [XmlRoot(ElementName = "game")]
    public class GameConfig
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "lock-file")]
        public string LockFile { get; set; }

        //if this one is set and the one in Settings is empty this one will be used
        [XmlElement(ElementName = "java-bin")]
        public string JavaBin { get; set; }

        //this one can either be loaded here or set from the value in VersionFile
        [XmlElement(ElementName = "assets")]
        public AssetConfig Assets { get; set; }

        [XmlElement(ElementName = "share-assets")]
        public bool ShareAssets { get; set; }

        //this one can either be loaded here or set from <launch-version>
        [XmlElement(ElementName = "launch-args")]
        public LaunchArgs LaunchArguments { get; set; }

        //this one can either be loaded here or set from <launch-forge>
        [XmlElement(ElementName = "launch-version")]
        public string LaunchVersion { get; set; }

        //for forge-1.19.2-43.1.27-installer.jar the value is "1.19.2-43.1.27"
        [XmlElement(ElementName = "launch-forge")]
        public string LaunchForge { get; set; }

        [XmlElement(ElementName = "launch-fabric")]
        public string LaunchFabric { get; set; }

        public class LaunchArgs
        {
            [XmlElement(ElementName = "vm-args")]
            public string VMArgs { get; set; } = "";

            [XmlElement(ElementName = "main-class")]
            public string MainClass { get; set; } = "";

            [XmlElement(ElementName = "game-args")]
            public string GameArgs { get; set; } = "";
        }

        private const string FileName = "game.xml";

        internal static GameConfig Load()
        {
            var xs = new XmlSerializer(typeof(GameConfig));
            using (var sr = new StreamReader(Path.Combine(Launcher.BaseDir, FileName)))
            {
                var cfg = (GameConfig)xs.Deserialize(sr);

                cfg.Name = cfg.Name?.Trim();

                cfg.LockFile = cfg.LockFile?.Trim();
                if (string.IsNullOrWhiteSpace(cfg.LockFile))
                    cfg.LockFile = null;

                cfg.JavaBin = cfg.JavaBin?.Trim();
                if (string.IsNullOrWhiteSpace(cfg.JavaBin))
                    cfg.JavaBin = null;

                if (cfg.Assets != null)
                {
                    bool clear = false;
                    if (string.IsNullOrWhiteSpace(cfg.Assets.Id)) clear = true;
                    if (string.IsNullOrWhiteSpace(cfg.Assets.Url)) clear = true;
                    if (clear)
                    {
                        cfg.Assets = null;
                    }
                    else
                    {
                        cfg.Assets.Id = cfg.Assets.Id.Trim();
                        cfg.Assets.Url = cfg.Assets.Url.Trim();
                    }
                }

                if (cfg.LaunchArguments != null)
                {
                    cfg.LaunchArguments.VMArgs = cfg.LaunchArguments.VMArgs.Trim();
                    cfg.LaunchArguments.MainClass = cfg.LaunchArguments.MainClass.Trim();
                    cfg.LaunchArguments.GameArgs = cfg.LaunchArguments.GameArgs.Trim();
                }

                cfg.LaunchVersion = cfg.LaunchVersion?.Trim();

                cfg.LaunchForge = cfg.LaunchForge?.Trim();
                if (cfg.LaunchForge != null)
                {
                    if (!Regex.IsMatch(cfg.LaunchForge, Forge.VersionRegex))
                        throw new ApplicationException("Incorrect <launch-forge> format.");
                }

                cfg.LaunchFabric = cfg.LaunchFabric?.Trim();

                return cfg;
            }
        }

        public class AssetConfig
        {
            [XmlElement(ElementName = "index")]
            public string Id { get; set; }

            [XmlElement(ElementName = "url")]
            public string Url { get; set; }

            public string Sha1 { get; set; } //not used when loaded from config, but used when loaded from VersionFile
        }
    }
}
