using Properties.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IPropertyTraceRepository : IRepository<PropertyTrace>
    {
        Task<IReadOnlyList<PropertyTrace>> GetByPropertyIdAsync(
            int propertyId, 
            bool includeInactive = false, 
            CancellationToken cancellationToken = default);
            
        Task<IReadOnlyList<PropertyTrace>> GetAsync(
            Expression<Func<PropertyTrace, bool>> predicate = null,
            Func<IQueryable<PropertyTrace>, IOrderedQueryable<PropertyTrace>> orderBy = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);
            
        Task<int> GetCountByPropertyIdAsync(
            int propertyId, 
            bool includeInactive = false, 
            CancellationToken cancellationToken = default);
    }
}