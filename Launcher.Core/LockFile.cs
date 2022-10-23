using System;
using System.IO;

namespace Launcher.Core
{
    static class LockFile
    {
        private static object lockObj = new object();

        private static FileStream file;

        public static bool Lock(string fileName)
        {
            lock (lockObj)
            {
                try
                {
                    if (file != null) return false; //Already using a lock file
                    file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static void Unlock()
        {
            lock (lockObj)
            {
                if (file != null)
                {
                    file.Dispose();
                    file = null;
                }
            }
        }
    }
}
