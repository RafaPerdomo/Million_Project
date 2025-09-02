using System;

namespace Properties.Domain.Exceptions
{
    public class InvalidOwnerOperationException : Exception
    {
        public InvalidOwnerOperationException() : base("Invalid operation on owner")
        {
        }

        public InvalidOwnerOperationException(string operation) 
            : base($"Invalid operation on owner: {operation}")
        {
        }

        public InvalidOwnerOperationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
