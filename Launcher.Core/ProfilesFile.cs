using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Core
{
    internal static class ProfilesFile
    {
        private static string profilesFile;

        internal static void SetPaths(GamePaths gamePaths)
        {
            profilesFile = Path.Combine(gamePaths.DataDir, "launcher_profiles.json");
        }

        private static string FileName => profilesFile ?? throw new NullReferenceException("Profiles paths not yet set.");

        internal static void CreateFileIfNotPresent()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileName));
            if (!File.Exists(FileName)) File.WriteAllText(FileName, "{ \"profiles\": {} }");
        }

        internal static Dictionary<string, LauncherProfiles.Profile> LoadProfiles()
        {
            try
            {
                LauncherProfiles lp = JsonSerializer.Deserialize<LauncherProfiles>(File.ReadAllText(ProfilesFile.FileName));
                return lp.Profiles;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                throw new ProfilesException("Corrupted \"launcher_profiles.json\".");
            }
        }

        internal static void StoreProfiles(Dictionary<string, LauncherProfiles.Profile> profiles)
        {
            LauncherProfiles lp = new LauncherProfiles
            {
                Profiles = profiles
            };
            File.WriteAllText(ProfilesFile.FileName, JsonSerializer.Serialize(lp));
        }
    }

#pragma warning disable CS0649
    public class LauncherProfiles
    {
        [JsonPropertyName("profiles")]
        public Dictionary<string, Profile> Profiles { get; set; }

        public class Profile
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("lastVersionId")]
            public string LastVersionId { get; set; }
        }
    }
#pragma warning restore CS0649

    public class ProfilesException : Exception
    {
        internal ProfilesException(string msg) : base(msg) { }
    }
}
