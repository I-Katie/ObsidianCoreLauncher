using System;
using System.IO;

namespace Launcher.Core
{
    public static class Launcher
    {
        public const string Title = "ObsidianCore Launcher";

        //the name representing this launcher that gets passed to Minecraft
        internal const string MinecraftLauncherName = "obsidiancore-launcher";

        //externally specified config
        public static GameConfig GameConfig { private set; get; }

        //not the Azure ID, just the client ID as passed to Minecraft
        internal static string ClientId { private set; get; }

        public static string BaseDir { private set; get; }

        public static void Init(string baseDir = null)
        {
            if (string.IsNullOrWhiteSpace(Secrets.AzureApp_ClientId))
                throw new StartupException(Title, "This launcher was compiled without an Azure Application key and can not run.");

            BaseDir = Path.GetFullPath(baseDir ?? Directory.GetCurrentDirectory());

            try
            {
                GameConfig = GameConfig.Load();
                if (GameConfig.Name.Length == 0) GameConfig.Name = Title;
            }
            catch (ApplicationException ex)
            {
                throw new StartupException(Title, ex.Message);
            }
            catch (Exception)
            {
                throw new StartupException(Title, $"Error loading game launch configuration.");
            }

            try
            {
                //verify game config
                if (string.IsNullOrEmpty(GameConfig.Name))
                    throw new ApplicationException("Property <name> must be set and not empty.");

                if (!ExactlyOne(GameConfig.LaunchArguments, GameConfig.LaunchVersion, GameConfig.LaunchForge, GameConfig.LaunchFabric))
                    throw new ApplicationException("Either <launch-args>, <launch-version>, <launch-forge> or <launch-fabric> must be set in the game config, but no more than one of those.");

                if (GameConfig.LaunchArguments != null) //<launch-args>
                {
                    if (string.IsNullOrEmpty(GameConfig.LaunchArguments.VMArgs))
                        throw new ApplicationException("The game settings <launch-args> requires a sub setting <vm-args>.");
                    if (string.IsNullOrEmpty(GameConfig.LaunchArguments.MainClass))
                        throw new ApplicationException("The game settings <launch-args> requires a sub setting <main-class>.");
                    if (string.IsNullOrEmpty(GameConfig.LaunchArguments.GameArgs))
                        throw new ApplicationException("The game settings <launch-args> requires a sub setting <game-args>.");
                }
                else if (GameConfig.LaunchVersion != null) //<launch-version>
                {
                    if (GameConfig.Assets != null)
                        throw new ApplicationException("The game settings <launch-version> and <assets> are mutually exclusive.");
                }
                else if (GameConfig.LaunchForge != null) //<launch-forge>
                {
                    if (GameConfig.Assets != null)
                        throw new ApplicationException("The game settings <launch-forge> and <assets> are mutually exclusive.");
                }
                else if (GameConfig.LaunchFabric != null) //<launch-fabric>
                {
                    if (GameConfig.Assets != null)
                        throw new ApplicationException("The game settings <launch-fabric> and <assets> are mutually exclusive.");
                }
            }
            catch (ApplicationException ex)
            {
                throw new StartupException(GameConfig.Name, ex.Message);
            }

            if (GameConfig.LockFile != null)
            {
                GameConfig.LockFile = Path.GetFullPath(Path.Combine(Launcher.BaseDir, GameConfig.LockFile));
                if (!GameConfig.LockFile.StartsWith(Launcher.BaseDir))
                {
                    throw new StartupException(GameConfig.Name, "The lock file is located outside of the current directory");
                }

                if (!LockFile.Lock(GameConfig.LockFile))
                {
                    throw new StartupException(GameConfig.Name, "The files used by this launcher are currently in use.");
                }
            }

            ClientId = Minecraft.GetClientId();

            Login.Init();
        }

        public static void Exit()
        {
            LockFile.Unlock();
        }

        private static bool ExactlyOne(params object[] values)
        {
            bool result = false;
            foreach (var val in values)
            {
                if (val != null)
                {
                    if (!result)
                        result = true;
                    else
                        return false;
                }
            }
            return result;
        }
    }

    public class StartupException : ApplicationException
    {
        public string Title { get; }

        public StartupException(string title, string message) : base(message)
        {
            Title = title;
        }
    }
}
