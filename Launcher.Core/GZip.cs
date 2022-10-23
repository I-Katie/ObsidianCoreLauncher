using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Launcher.Core
{
    internal static class GZip
    {
        internal static byte[] ReadBytesFromGZipFile(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                using (GZipStream gzs = new GZipStream(fs, CompressionMode.Decompress))
                {
                    List<byte> bytes = new List<byte>();
                    int b;
                    while ((b = gzs.ReadByte()) != -1)
                    {
                        bytes.Add((byte)b);
                    }
                    return bytes.ToArray();
                }
            }
        }

        internal static void WriteBytesToGZipFile(string fileName, byte[] data)
        {
            using (FileStream fs = File.OpenWrite(fileName))
            {
                fs.SetLength(0);
                using (GZipStream gzs = new GZipStream(fs, CompressionMode.Compress))
                {
                    gzs.Write(data, 0, data.Length);
                }
            }
        }
    }
}
