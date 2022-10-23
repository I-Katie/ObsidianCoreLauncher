using System;
using System.IO;

namespace Launcher.Core
{
    static class Minecraft
    {
        internal static string GetMinecraftDataDir()
        {
            if (Platform.OperatingSystem == Platform.OS.Windows)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), ".minecraft");
            }
            else if (Platform.OperatingSystem == Platform.OS.Linux)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal, Environment.SpecialFolderOption.DoNotVerify), ".minecraft");
            }
            else if (Platform.OperatingSystem == Platform.OS.OSX)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal, Environment.SpecialFolderOption.DoNotVerify), "Library/Application Support/minecraft");
            }
            else
            {
                throw new ApplicationException($"Unsupported platform \"{Environment.OSVersion.Platform}\".");
            }
        }

        private const string ClientIdFileName = "clientId.txt";

        internal static string GetClientId()
        {
            //not the Azure ID, just the client ID as passed to Minecraft
            try
            {
                //try to use the one from the official launcher
                return File.ReadAllText(Path.Combine(GetMinecraftDataDir(), ClientIdFileName));
            }
            catch (Exception)
            {
                //if not possible use own

                string localClientIdFileName = Path.Combine(Launcher.BaseDir, ClientIdFileName);
                try
                {
                    return Guid.Parse(File.ReadAllText(localClientIdFileName)).ToString();
                }
                catch (Exception)
                {
                    //create a new one
                    string clientId = Guid.NewGuid().ToString();

                    File.WriteAllText(localClientIdFileName, clientId);

                    return clientId;
                }
            }
        }
    }
}
