using Properties.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IPropertyImageRepository : IRepository<PropertyImage>
    {
        Task<PropertyImage> GetByIdAsync(int id, bool includeInactive = false, CancellationToken cancellationToken = default);
        
        Task<PropertyImage> GetByIdWithPropertyAsync(int id, CancellationToken cancellationToken = default);
        
        Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(int propertyId, bool includeInactive = false, CancellationToken cancellationToken = default);
        
        Task AddRangeAsync(IEnumerable<PropertyImage> images, CancellationToken cancellationToken = default);
        
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        
        Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default);
        
        Task<int> GetCountByPropertyIdAsync(int propertyId, bool includeInactive = false, CancellationToken cancellationToken = default);
    }
}