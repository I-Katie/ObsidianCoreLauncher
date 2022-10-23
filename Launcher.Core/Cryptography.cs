using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Launcher.Core
{
    static class Cryptography
    {
        internal static byte[] Encrypt(byte[] input)
        {
            if (Platform.OperatingSystem == Platform.OS.Windows)
            {
                return ProtectedData.Protect(input, default, DataProtectionScope.LocalMachine);
            }
            else if (Platform.OperatingSystem == Platform.OS.Linux)
            {
                return Encrypt(input, GetLinuxMachineKey());
            }
            else if (Platform.OperatingSystem == Platform.OS.OSX)
            {
                return Encrypt(input, GetMacMachineKey());
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal static byte[] Decrypt(byte[] input)
        {
            if (Platform.OperatingSystem == Platform.OS.Windows)
            {
                return ProtectedData.Unprotect(input, default, DataProtectionScope.LocalMachine);
            }
            else if (Platform.OperatingSystem == Platform.OS.Linux)
            {
                return Decrypt(input, GetLinuxMachineKey());
            }
            else if (Platform.OperatingSystem == Platform.OS.OSX)
            {
                return Decrypt(input, GetMacMachineKey());
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        //returns 128 bit machine id
        private static byte[] GetLinuxMachineKey()
        {
            try
            {
                // https://man7.org/linux/man-pages/man5/machine-id.5.html
                string text = File.ReadAllLines("/etc/machine-id")[0];
                byte[] output = new byte[16];
                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = byte.Parse(text.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                return output;
            }
            catch (Exception)
            {
                throw new CryptographicException("Missing machine ID.");
            }
        }

        //returns 128 bit platform uuid
        private static byte[] GetMacMachineKey()
        {
            try
            {
                using (Process process = Process.Start(new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    RedirectStandardOutput = true,
                }))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    var match = Regex.Match(output, "\"IOPlatformUUID\"\\s*=\\s*\"([A-Fa-f0-9-]{36})\"");
                    if (match.Success)
                    {
                        return Guid.Parse(match.Groups[1].Value).ToByteArray();
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            catch (Exception)
            {
                throw new CryptographicException("Missing machine ID.");
            }
        }

        //key must be 128 bits
        private static byte[] Encrypt(byte[] input, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[aes.KeySize / 8];

                using (var encrypt = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                        cs.Write(input, 0, input.Length);
                    return ms.ToArray();
                }
            }
        }

        //key must be 128 bits
        private static byte[] Decrypt(byte[] input, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[aes.KeySize / 8];

                using (var decrypt = aes.CreateDecryptor())
                using (var ms = new MemoryStream(input))
                using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Read))
                using (var ms2 = new MemoryStream())
                {
                    cs.CopyTo(ms2);
                    return ms2.ToArray();
                }
            }
        }
    }
}
