using System.Runtime.InteropServices;
using ints = System.Runtime.InteropServices;

namespace Launcher.Core
{
    internal static class Platform
    {
        internal static OS OperatingSystem
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OS.Windows;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OS.Linux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OS.OSX;
                }
                else
                {
                    return OS.Unknown;
                }
            }
        }

        internal static Arch Architecture
        {
            get
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case ints.Architecture.X86:
                        return Arch.X86;
                    case ints.Architecture.X64:
                        return Arch.X64;
                    case ints.Architecture.Arm:
                        return Arch.Arm;
                    case ints.Architecture.Arm64:
                        return Arch.AArch64;
                    default:
                        return Arch.Unknown;
                }
            }
        }

        internal enum OS
        {
            Unknown,
            Windows,
            Linux,
            OSX
        }

        internal enum Arch
        {
            Unknown,
            X86,
            X64,
            Arm,
            AArch64 //Arm64
        }
    }
}
