using System;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class InvalidContextException : Exception
    {
        public InvalidContextException(string message)
            : base(message)
        {
        }
    }
}
