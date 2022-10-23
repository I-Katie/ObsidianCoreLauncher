using System.IO;

namespace Launcher.Core
{
    internal static class Files
    {
        internal static void DeepCopy(string src, string dest)
        {
            var source = new DirectoryInfo(src);

            foreach (string dir in Directory.GetDirectories(source.FullName, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(source.FullName, dest));
            }

            foreach (string srcFile in Directory.GetFiles(source.FullName, "*.*", SearchOption.AllDirectories))
            {
                string destPath = srcFile.Replace(source.FullName, dest);
                if (!File.Exists(destPath))
                    File.Copy(srcFile, destPath);
            }
        }
    }
}
