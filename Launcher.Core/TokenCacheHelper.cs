using Microsoft.Identity.Client;
using System.IO;
using System.Security.Cryptography;

// https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-net-token-cache-serialization?tabs=desktop

namespace Launcher.Core
{
    internal class TokenCacheHelper
    {
        private static readonly object FileLock = new object();

        private readonly string filePath;

        internal TokenCacheHelper(string filePath)
        {
            this.filePath = filePath;
        }

        internal void RegisterCache(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                byte[] data = File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
                try
                {
                    if (data != null) data = Cryptography.Decrypt(data);
                }
                catch (CryptographicException)
                {
                    data = null;
                }
                args.TokenCache.DeserializeMsalV3(data);
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(filePath, Cryptography.Encrypt(args.TokenCache.SerializeMsalV3()));
                }
            }
        }
    }
}
