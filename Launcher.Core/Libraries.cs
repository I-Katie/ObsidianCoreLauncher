using System;
using System.IO;
using System.Threading.Tasks;

//https://wiki.vg/Game_files#Libraries

namespace Launcher.Core
{
    internal static class Libraries
    {
        internal static async Task VerifyAndDownloadAsync(IPageControl pageControl, GamePaths gamePaths, VersionFile versionFile)
        {
            string librariesDir = gamePaths.LibrariesDir;
            Directory.CreateDirectory(librariesDir);

            var pageProgress = pageControl.SetPageDownloadProgress("Fetching libraries", versionFile.Libraries.Count + 1);

            using (var tempFile = new TempFile())
            {
                //libraries
                foreach (var library in versionFile.Libraries)
                {
                    VersionFile.LibraryInfo.ArtifactInfo info;
                    if (library.artifact != null)
                    {
                        info = library.artifact;
                    }
                    else if (library.native != null)
                    {
                        info = library.native;
                    }
                    else
                    {
                        throw new InternalException();
                    }

                    string fullFileName = Path.Combine(librariesDir, info.path).Replace('/', Path.DirectorySeparatorChar);
                    string outputDir = Path.GetDirectoryName(fullFileName);
                    string fileName = Path.GetFileName(fullFileName);

                    pageProgress.CurrentFileName = fileName;

                    if (!File.Exists(fullFileName))
                    {
                        //download it
                        try
                        {
                            await Http.GetFileAsync(info.url, tempFile);

                            if (info.sha1 != null) //some files don't have a sha1 for their libraries (check VersionFile)
                            {
                                //verify the file hash
                                if (!string.Equals(info.sha1, Hash.CalculateSha1ForFile(tempFile), StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new LibrariesException($"Hash for library {library.fullName} doesn't match.");
                                }
                            }

                            Directory.CreateDirectory(outputDir);
                            File.Copy(tempFile, fullFileName);
                        }
                        catch (Exception)
                        {
                            throw new LibrariesException($"Failed to download library {library.fullName}.");
                        }
                    }

                    pageProgress.Value += 1;
                }

                //client.jar
                pageProgress.CurrentFileName = Path.GetFileName(versionFile.ClientJarFilePath);
                if (!File.Exists(versionFile.ClientJarFilePath) || new FileInfo(versionFile.ClientJarFilePath).Length == 0) //check the length because of how the Fabric installer works
                {
                    await DownloadClientAsync(versionFile, tempFile);
                }
                pageProgress.Value += 1;
            }
        }

        internal static async Task DownloadClientAsync(VersionFile versionFile, TempFile tempFile)
        {
            //maybe download it
            string fileName = Path.GetFileName(versionFile.ClientJarFilePath);
            string outputDir = Path.GetDirectoryName(versionFile.ClientJarFilePath);

            if (versionFile.ClientJarDownloadInfo == null)
            {
                //we don't have a download url
                throw new LibrariesException($"Missing \"{fileName}\".");
            }

            //download it
            try
            {
                await Http.GetFileAsync(versionFile.ClientJarDownloadInfo.url, tempFile);

                //verify the file hash
                if (!string.Equals(versionFile.ClientJarDownloadInfo.sha1, Hash.CalculateSha1ForFile(tempFile), StringComparison.OrdinalIgnoreCase))
                {
                    throw new LibrariesException($"Hash for \"{fileName}\" doesn't match.");
                }

                Directory.CreateDirectory(outputDir);
                File.Copy(tempFile, versionFile.ClientJarFilePath, true);
            }
            catch (Exception)
            {
                throw new LibrariesException($"Failed to download \"{fileName}\".");
            }
        }
    }

    public class LibrariesException : ApplicationException
    {
        internal LibrariesException(string message) : base(message) { }
    }
}