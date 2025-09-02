using System;

namespace Properties.Domain.Exceptions
{
    public class PropertyNotFoundException : Exception
    {
        public PropertyNotFoundException() : base("Property not found")
        {
        }

        public PropertyNotFoundException(int id) : base($"Property with ID {id} was not found")
        {
        }

        public PropertyNotFoundException(string message) : base(message)
        {
        }

        public PropertyNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
