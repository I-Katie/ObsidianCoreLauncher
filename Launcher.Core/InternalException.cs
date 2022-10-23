using System;

namespace Launcher.Core
{
    public class InternalException : ApplicationException
    {
        public InternalException() { }

        public InternalException(string message) : base(message) { }
    }
}
