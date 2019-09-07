using System;

namespace Checkers
{
    public class CheckFailedException : Exception
    {
        public CheckFailedException(string message) : base(message)
        {
            
        }
    }
}