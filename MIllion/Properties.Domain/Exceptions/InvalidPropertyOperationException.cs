using System;

namespace Properties.Domain.Exceptions
{
    public class InvalidPropertyOperationException : Exception
    {
        public InvalidPropertyOperationException() : base("Invalid operation on property")
        {
        }

        public InvalidPropertyOperationException(string operation) 
            : base($"Invalid operation on property: {operation}")
        {
        }

        public InvalidPropertyOperationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
