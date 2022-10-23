using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Launcher.Core
{
    static class Hash
    {
        internal static string CalculateSha1ForFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder sb = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        sb.AppendFormat("{0:X2}", b);
                    }
                    return sb.ToString();
                }
            }
        }

        internal static string CalculateSha1ForString(string value)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder sb = new StringBuilder(2 * hash.Length);
                foreach (byte b in hash)
                {
                    sb.AppendFormat("{0:X2}", b);
                }
                return sb.ToString();
            }
        }
    }
}
