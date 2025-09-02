using Properties.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IOwnerRepository : IRepository<Owner>
    {
        Task<Owner> GetByIdWithPropertiesAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Owner>> GetAllWithPropertiesAsync(CancellationToken cancellationToken = default);
        Task<bool> OwnerExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<Owner> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}