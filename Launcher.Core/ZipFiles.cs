using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Launcher.Core
{
    internal static class ZipFiles
    {
        internal static void ExtractZipFile(string archiveFile, string outputDirectory, List<string> excludeList)
        {
            bool IsExcluded(string fullName)
            {
                foreach (var s in excludeList)
                {
                    if (fullName.StartsWith(s)) return true;
                }
                return false;
            }

            using (var zipFile = ZipFile.OpenRead(archiveFile))
            {
                var entries = from entry in zipFile.Entries
                              where !string.IsNullOrEmpty(entry.Name)
                              where !entry.FullName.StartsWith("META-INF/")
                              where !entry.Name.EndsWith(".git")
                              where !entry.Name.EndsWith(".sha1")
                              where !IsExcluded(entry.FullName)
                              select entry;

                foreach (var entry in entries)
                {
                    string outFileName = Path.Combine(outputDirectory, entry.FullName);

                    Directory.CreateDirectory(Path.GetDirectoryName(outFileName));

                    if (!File.Exists(outFileName))
                        entry.ExtractToFile(outFileName, overwrite: false);
                }
            }
        }
    }
}
