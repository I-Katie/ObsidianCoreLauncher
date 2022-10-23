using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// https://wiki.vg/Game_files#Assets

namespace Launcher.Core
{
    internal static class Assets
    {
        private const string AssetsUrl = @"http://resources.download.minecraft.net/";

        internal static async Task<AssetInfo> VerifyAndDownloadAsync(IPageControl pageControl, GamePaths gamePaths)
        {
            bool share = Launcher.GameConfig.ShareAssets;
            string assetsDir = Path.Combine(share ? Minecraft.GetMinecraftDataDir() : gamePaths.DataDir, "assets");
            string indexesDir = Path.Combine(assetsDir, "indexes");

            Directory.CreateDirectory(assetsDir);
            Directory.CreateDirectory(indexesDir);

            string assetsIndexJson;
            string indexFileName = Path.GetFullPath(Path.Combine(indexesDir, $"{Launcher.GameConfig.Assets.Id}.json"));
            if (!File.Exists(indexFileName))
            {
                if (share) throw new AssetsException($"Can't download missing asset index \"{Launcher.GameConfig.Assets.Id}\" as sharing is enabled.");

                //we need to download the index file
                try
                {
                    pageControl.SetPageWait("Fetching assets index");

                    assetsIndexJson = await Http.GetStringAsync(Launcher.GameConfig.Assets.Url);

                    if (Launcher.GameConfig.Assets.Sha1 != null)
                    {
                        //verify the file hash
                        if (!string.Equals(Launcher.GameConfig.Assets.Sha1, Hash.CalculateSha1ForString(assetsIndexJson), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new AssetsException($"Hash for index \"{Launcher.GameConfig.Assets.Id}\" doesn't match.");
                        }
                    }

                    File.WriteAllText(indexFileName, assetsIndexJson);
                }
                catch (Exception)
                {
                    throw new AssetsException("Failed to download asset index.");
                }
            }
            else
            {
                assetsIndexJson = File.ReadAllText(indexFileName);
            }

            AssetsJson assetsData;
            try
            {
                assetsData = JsonSerializer.Deserialize<AssetsJson>(assetsIndexJson);
            }
            catch (Exception)
            {
                throw new AssetsException("Failed to parse asset index.");
            }

            string legacyAssetsDir = null;
            string objectsDir;
            if (assetsData.MapToResources || assetsData.Virtual)
            {
                //virtual
                objectsDir = Path.Combine(assetsDir, $"virtual{Path.DirectorySeparatorChar}{Launcher.GameConfig.Assets.Id}");
                legacyAssetsDir = objectsDir;
            }
            else
            {
                //current
                objectsDir = Path.Combine(assetsDir, "objects");
            }

            var pageProgress = pageControl.SetPageDownloadProgress("Fetching assets", assetsData.Objects.Count);

            using (var tempFile = new TempFile())
            {
                foreach (var kv in assetsData.Objects)
                {
                    string name = kv.Key;
                    var data = kv.Value;
                    var firstTwoLettersOfHash = data.Hash.Substring(0, 2);

                    pageProgress.CurrentFileName = name;

                    string subDir, fileName;
                    if (assetsData.MapToResources || assetsData.Virtual)
                    {
                        //virtual
                        fileName = Path.Combine(objectsDir, name);
                        subDir = Path.GetDirectoryName(fileName);
                    }
                    else
                    {
                        subDir = Path.Combine(objectsDir, firstTwoLettersOfHash);
                        fileName = Path.Combine(subDir, data.Hash);
                    }

                    if (!File.Exists(fileName))
                    {
                        if (share) throw new AssetsException($"Can't download missing asset \"{name}\" as sharing is enabled.");

                        //download it
                        try
                        {
                            await Http.GetFileAsync($"{AssetsUrl}{firstTwoLettersOfHash}/{data.Hash}", tempFile);

                            //verify the file hash
                            if (!string.Equals(data.Hash, Hash.CalculateSha1ForFile(tempFile), StringComparison.OrdinalIgnoreCase))
                            {
                                throw new AssetsException($"Hash for asset \"{name}\" doesn't match.");
                            }

                            Directory.CreateDirectory(subDir);
                            File.Copy(tempFile, fileName);
                        }
                        catch (Exception)
                        {
                            throw new AssetsException($"Failed to download asset \"{name}\".");
                        }
                    }

                    pageProgress.Value += 1;
                }
            }

            if (assetsData.MapToResources)
            {
                if (share) throw new AssetsException("Can't share assets because they must be mapped to resources.");

                //copy to {game}/resources
                string resourcesDir = Path.Combine(gamePaths.GameDir, "resources");
                pageControl.SetPageWait("Copying assets...");
                //maybe this could be solved with File.CreateSymbolicLink() if it were .NET 6 and Windows 11 or Linux or Mac
                Files.DeepCopy(legacyAssetsDir, resourcesDir);
            }

            return new AssetInfo
            {
                AssetsRoot = legacyAssetsDir == null ? assetsDir : null,
                LegacyAssetsDir = legacyAssetsDir,
                Legacy = legacyAssetsDir != null,
                MapToResources = assetsData.MapToResources
            };
        }

        internal class AssetInfo
        {
            internal string AssetsRoot { get; set; }
            internal string LegacyAssetsDir { get; set; }
            internal bool Legacy { get; set; }
            internal bool MapToResources { get; set; }
        }

#pragma warning disable CS0649
        public class AssetsJson
        {
            [JsonPropertyName("objects")]
            public Dictionary<string, AssetData> Objects { get; set; }

            [JsonPropertyName("virtual")]
            public bool Virtual { get; set; } //(1.6, 1.7.2]]

            [JsonPropertyName("map_to_resources")]
            public bool MapToResources { get; set; } //[rd-132211", 1.6]

            public class AssetData
            {
                [JsonPropertyName("hash")]
                public string Hash { get; set; }

                [JsonPropertyName("size")]
                public long Size { get; set; }
            }
        }
#pragma warning restore CS0649
    }

    public class AssetsException : ApplicationException
    {
        internal AssetsException(string message) : base(message) { }
    }
}
