using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Core
{
    public class Settings
    {
        private const string FileName = "settings.json";

        [JsonPropertyName("jvm_bin")]
        public string JVMBinary { get; set; } = "";

        [JsonPropertyName("jre_args")]
        public string JREArguments { get; set; } = GameLauncher.VMDefaults;

        [JsonPropertyName("close_on_exit")]
        public bool CloseOnExit { get; set; } = true;

        public void Save()
        {
            string data = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(Launcher.BaseDir, FileName), data);
        }

        public static Settings Load()
        {
            try
            {
                var obj = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(Launcher.BaseDir, FileName)));
                obj.JVMBinary = obj.JVMBinary.Trim();
                obj.JREArguments = obj.JREArguments.Trim();
                return obj;
            }
            catch (Exception)
            {
                return new Settings();
            }
        }
    }
}
