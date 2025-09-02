using System;

namespace Properties.Domain.Exceptions
{
    public class OwnerAlreadyExistsException : Exception
    {
        public OwnerAlreadyExistsException() : base("Owner already exists")
        {
        }

        public OwnerAlreadyExistsException(string identifier, string value) 
            : base($"Owner with {identifier} '{value}' already exists")
        {
        }

        public OwnerAlreadyExistsException(string message) : base(message)
        {
        }

        public OwnerAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
