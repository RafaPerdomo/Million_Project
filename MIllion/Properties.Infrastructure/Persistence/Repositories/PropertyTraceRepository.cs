using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using Properties.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Infrastructure.Persistence.Repositories
{
    public class PropertyTraceRepository : Repository<PropertyTrace>, IPropertyTraceRepository
    {
        private readonly ApplicationDbContext _context;

        public PropertyTraceRepository(ApplicationDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyList<PropertyTrace>> GetByPropertyIdAsync(
            int propertyId, 
            bool includeInactive = false, 
            CancellationToken cancellationToken = default)
        {
            var query = _context.PropertyTraces
                .Where(pt => pt.PropertyId == propertyId);

            if (!includeInactive)
            {
                query = query.Where(pt => pt.IsActive);
            }

            return await query
                .OrderByDescending(pt => pt.DateSale)
                .ThenByDescending(pt => pt.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PropertyTrace>> GetAsync(
            Expression<Func<PropertyTrace, bool>> predicate = null,
            Func<IQueryable<PropertyTrace>, IOrderedQueryable<PropertyTrace>> orderBy = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            IQueryable<PropertyTrace> query = _context.PropertyTraces;

            if (!includeInactive)
            {
                query = query.Where(pt => pt.IsActive);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                return await orderBy(query)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }

            return await query
                .OrderByDescending(pt => pt.DateSale)
                .ThenByDescending(pt => pt.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountByPropertyIdAsync(
            int propertyId, 
            bool includeInactive = false, 
            CancellationToken cancellationToken = default)
        {
            var query = _context.PropertyTraces
                .Where(pt => pt.PropertyId == propertyId);

            if (!includeInactive)
            {
                query = query.Where(pt => pt.IsActive);
            }

            return await query.CountAsync(cancellationToken);
        }
    }
}