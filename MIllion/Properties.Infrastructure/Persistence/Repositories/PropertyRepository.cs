using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using Properties.Infrastructure.Persistence.Context;

namespace Properties.Infrastructure.Persistence.Repositories
{
    public class PropertyRepository : Repository<Property>, IPropertyRepository
    {
        private readonly ApplicationDbContext _context;

        public PropertyRepository(ApplicationDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IQueryable<Property> GetQueryable(bool includeInactive = false)
        {
            var query = _context.Properties.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            return query
                .Include(p => p.Owner)
                .Include(p => p.PropertyImages.Where(pi => pi.IsActive))
                .Include(p => p.PropertyTraces.Where(pt => pt.IsActive))
                .AsQueryable();
        }

        public async Task<IReadOnlyList<Property>> GetPropertiesWithDetailsAsync(
            Expression<Func<Property, bool>> predicate = null,
            Func<IQueryable<Property>, IOrderedQueryable<Property>> orderBy = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Property> query = _context.Properties;

            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            query = query
                .Include(p => p.Owner)
                .Include(p => p.PropertyImages.Where(pi => pi.IsActive))
                .Include(p => p.PropertyTraces.Where(pt => pt.IsActive));

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync(cancellationToken);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<Property> GetByIdWithDetailsAsync(int id, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Properties.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            return await query
                .Include(p => p.Owner)
                .Include(p => p.PropertyImages.Where(pi => pi.IsActive))
                .Include(p => p.PropertyTraces.Where(pt => pt.IsActive))
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Property> GetByCodeInternalAsync(string codeInternal, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeInternal))
            {
                throw new ArgumentException("Code internal cannot be null or whitespace.", nameof(codeInternal));
            }

            return await _context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodeInternal == codeInternal, cancellationToken);
        }
        public new async Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Properties
                .Where(p => p.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        public new async Task<Property> AddAsync(Property entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            await _context.Properties.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }
        public async Task<Property> UpdateAsync(Property property, CancellationToken cancellationToken = default)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            _context.Entry(property).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
            return property;
        }
        public async Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (property == null)
                return false;
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<PropertyTrace> AddTraceAsync(PropertyTrace propertyTrace, CancellationToken cancellationToken = default)
        {
            if (propertyTrace == null)
                throw new ArgumentNullException(nameof(propertyTrace));
            await _context.PropertyTraces.AddAsync(propertyTrace, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return propertyTrace;
        }
        public new async Task<Property> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<bool> PropertyExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Properties
                .AnyAsync(p => p.Id == id && p.IsActive, cancellationToken);
        }

        public async Task<bool> PropertyWithCodeExistsAsync(string codeInternal, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeInternal))
                return false;
                
            return await _context.Properties
                .AnyAsync(p => p.CodeInternal == codeInternal && p.IsActive, cancellationToken);
        }
    }
}