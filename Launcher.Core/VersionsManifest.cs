using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Launcher.Core
{
    internal class VersionsManifest
    {
        private const string VersionManifestUrl = @"https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

        private readonly GamePaths gamePaths;
        private readonly bool offline;
        private readonly string manifestFileName;
        private readonly string manifestDir;
        private readonly string manifestFullPath;

        private readonly Dictionary<string, VersionsManifestJson.VersionJson> versions = new Dictionary<string, VersionsManifestJson.VersionJson>();

        internal VersionsManifest(GamePaths gamePaths, bool offline)
        {
            this.gamePaths = gamePaths;
            this.offline = offline;

            manifestFileName = VersionManifestUrl.Substring(VersionManifestUrl.LastIndexOf("/") + 1);
            manifestDir = gamePaths.VersionsDir;
            manifestFullPath = Path.Combine(manifestDir, manifestFileName);
        }

        //this must be called after the constructor
        internal async Task LoadManifestAsync()
        {
            async Task DownloadAsync()
            {
                try
                {
                    var (result, date) = await Http.GetStringWithDateAsync(VersionManifestUrl);

                    Directory.CreateDirectory(manifestDir);
                    File.WriteAllText(manifestFullPath, result);
                    if (date != null)
                        File.SetLastWriteTimeUtc(manifestFullPath, date.Value);
                }
                catch (Exception)
                {
                    throw new VersionsManifestException("Failed to download versions manifest.");
                }
            }

            if (!File.Exists(manifestFullPath))
            {
                // download it
                await DownloadAsync();
            }
            else if (!offline)
            {
                // check if a newer one exists
                var createdTime = File.GetLastWriteTimeUtc(manifestFullPath);

                var headers = new Dictionary<string, string>
                    {
                        {"If-Modified-Since", createdTime.ToUniversalTime().ToString("r") } //I'm keeping ToUniversalTime() just because if I didn't read it from the file with a function that returns Utc time it would be required
                    };
                var res = await Http.HeadAsync(VersionManifestUrl, headers); //vrne 200 če je nov, 304 če ni novega
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    //server has newer version, download it
                    await DownloadAsync();
                }
                else if (res.StatusCode != HttpStatusCode.NotModified) //if not 200 but also not 304 we have an error
                {
                    throw new VersionsManifestException("Failed to download versions manifest.");
                }
            }

            try
            {
                var json = JsonSerializer.Deserialize<VersionsManifestJson>(File.ReadAllText(manifestFullPath));

                foreach (var version in json.Versions)
                {
                    versions[version.Id] = version;
                }
            }
            catch (Exception)
            {
                throw new VersionsManifestException("Failed to read versions manifest.");
            }
        }

        internal async Task DownloadIfMissing(string versionId)
        {
            List<string> verified = new List<string>();

            using (var tempFile = new TempFile())
            {
                while (versionId != null)
                {
                    if (verified.Contains(versionId))
                        throw new VersionsManifestException("The versions are referencing each other in a loop.");
                    verified.Add(versionId);

                    var versionFilePath = VersionFile.GetVersionFilePathFor(gamePaths, versionId);
                    string outputDir = Path.GetDirectoryName(versionFilePath);

                    if (!File.Exists(versionFilePath))
                    {
                        // download it
                        try
                        {
                            var version = versions[versionId];

                            await Http.GetFileAsync(version.Url, tempFile);

                            //verify the file hash
                            if (!string.Equals(version.Sha1, Hash.CalculateSha1ForFile(tempFile), StringComparison.OrdinalIgnoreCase))
                            {
                                throw new VersionsManifestException($"Hash for file \"{versionId}.json\" doesn't match.");
                            }

                            Directory.CreateDirectory(outputDir);
                            File.Copy(tempFile, versionFilePath);
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new VersionsManifestException($"The versions manifest doesn't contain version \"{versionId}.json\"");
                        }
                        catch (Exception)
                        {
                            new VersionsManifestException($"Failed to download \"{versionId}.json\".");
                        }
                    }

                    try
                    {
                        versionId = null; //if it's null the loop will end, otherwise it will repeat for the new versionId

                        //load the file and check if it inherits from another file
                        var doc = JsonDocument.Parse(File.ReadAllText(versionFilePath));
                        if (doc.RootElement.TryGetProperty("inheritsFrom", out var inheritsFrom))
                        {
                            versionId = inheritsFrom.GetString();
                        }
                    }
                    catch (Exception)
                    {
                        throw new VersionsManifestException($"Error reading \"{versionId}.json\".");
                    }
                }
            }
        }

#pragma warning disable CS0649
        public class VersionsManifestJson
        {
            [JsonPropertyName("latest")]
            public LatestJson Latest { get; set; }

            [JsonPropertyName("versions")]
            public VersionJson[] Versions { get; set; }

            public class LatestJson
            {
                [JsonPropertyName("release")]
                public string Release { get; set; }

                [JsonPropertyName("snapshot")]
                public string Snapshot { get; set; }
            }

            public class VersionJson
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("type")]
                public string Type { get; set; }

                [JsonPropertyName("url")]
                public string Url { get; set; }

                [JsonPropertyName("time")]
                public DateTime Time { get; set; }

                [JsonPropertyName("releaseTime")]
                public DateTime ReleaseTime { get; set; }

                [JsonPropertyName("sha1")]
                public string Sha1 { get; set; }

                [JsonPropertyName("complianceLevel")]
                public int ComplianceLevel { get; set; }
            }
        }
#pragma warning restore CS0649
    }

    public class VersionsManifestException : ApplicationException
    {
        internal VersionsManifestException(string message) : base(message) { }
    }
}
