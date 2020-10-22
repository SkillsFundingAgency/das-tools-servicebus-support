using System;
using System.Net;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Exceptions
{
    public class CosmosDeleteException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public CosmosDeleteException(string message, HttpStatusCode statusCode) : this(message)
        {
            StatusCode = statusCode;
        }

        public CosmosDeleteException(string message) : base(message)
        {
        }

        public CosmosDeleteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
