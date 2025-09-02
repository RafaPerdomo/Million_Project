using System;

namespace Properties.Domain.Exceptions
{
    public class OwnerNotFoundException : Exception
    {
        public OwnerNotFoundException() : base("Owner not found")
        {
        }

        public OwnerNotFoundException(int id) : base($"Owner with ID {id} was not found")
        {
        }

        public OwnerNotFoundException(string message) : base(message)
        {
        }

        public OwnerNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
