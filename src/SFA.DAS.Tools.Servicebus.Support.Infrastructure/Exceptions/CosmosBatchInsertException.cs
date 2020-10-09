using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Exceptions
{
    public class CosmosBatchInsertException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public CosmosBatchInsertException(string message, HttpStatusCode statusCode) : this(message)
        {
            StatusCode = statusCode;
        }

        public CosmosBatchInsertException(string message) : base(message)
        {
        }

        public CosmosBatchInsertException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
