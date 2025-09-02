using Properties.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IPropertyRepository : IRepository<Property>
    {
        IQueryable<Property> GetQueryable(bool includeInactive = false);
        
        Task<IReadOnlyList<Property>> GetPropertiesWithDetailsAsync(
            Expression<Func<Property, bool>> predicate = null,
            Func<IQueryable<Property>, IOrderedQueryable<Property>> orderBy = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);
            
        Task<Property> GetByIdWithDetailsAsync(
            int id, 
            bool includeInactive = false, 
            CancellationToken cancellationToken = default);
            
        Task<Property> GetByCodeInternalAsync(
            string codeInternal, 
            CancellationToken cancellationToken = default);
            
        Task<bool> PropertyExistsAsync(
            int id, 
            CancellationToken cancellationToken = default);
            
        Task<bool> PropertyWithCodeExistsAsync(
            string codeInternal, 
            CancellationToken cancellationToken = default);
            
        Task<PropertyTrace> AddTraceAsync(
            PropertyTrace propertyTrace, 
            CancellationToken cancellationToken = default);
    }
}