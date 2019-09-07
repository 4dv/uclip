using System;

namespace uclip
{
    internal class CommandLineException : Exception
    {
        public CommandLineException(string message) : base(message)
        {
        }
    }
}