using System;
using System.IO;

namespace Launcher.Core
{
    public class TempFile : IDisposable
    {
        private string path;

        public void Dispose()
        {
            if (path == null) return;
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception) { }
            path = null;
        }

        public static implicit operator string(TempFile tf)
        {
            if (tf.path == null)
                tf.path = Path.GetTempFileName();
            return tf.path;
        }
    }
}
