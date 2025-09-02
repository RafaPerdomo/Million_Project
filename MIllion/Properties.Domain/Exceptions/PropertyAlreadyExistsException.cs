using System;

namespace Properties.Domain.Exceptions
{
    public class PropertyAlreadyExistsException : Exception
    {
        public PropertyAlreadyExistsException() : base("Property already exists")
        {
        }

        public PropertyAlreadyExistsException(string identifier, string value) 
            : base($"Property with {identifier} '{value}' already exists")
        {
        }

        public PropertyAlreadyExistsException(string message) : base(message)
        {
        }

        public PropertyAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
