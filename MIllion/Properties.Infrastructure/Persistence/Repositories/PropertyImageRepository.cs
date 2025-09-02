using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using Properties.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Infrastructure.Persistence.Repositories
{
    public class PropertyImageRepository : Repository<PropertyImage>, IPropertyImageRepository
    {
        private readonly ApplicationDbContext _context;

        public PropertyImageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public new async Task<PropertyImage> AddAsync(PropertyImage entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            
            await _context.PropertyImages.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<PropertyImage> images, CancellationToken cancellationToken = default)
        {
            if (images == null)
                throw new ArgumentNullException(nameof(images));

            var now = DateTime.UtcNow;
            foreach (var image in images)
            {
                image.CreatedAt = now;
                image.IsActive = true;
            }

            await _context.PropertyImages.AddRangeAsync(images, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public new async Task<IReadOnlyList<PropertyImage>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PropertyImages
                .Where(pi => pi.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<PropertyImage> GetByIdAsync(int id, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.PropertyImages.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(pi => pi.IsActive);
            }

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
        }

        public async Task<PropertyImage> GetByIdWithPropertyAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.PropertyImages
                .Include(pi => pi.Property)
                .FirstOrDefaultAsync(pi => pi.Id == id && pi.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<PropertyImage>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            return await _context.PropertyImages
                .AsNoTracking()
                .Where(pi => pi.PropertyId == propertyId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(int propertyId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.PropertyImages
                .Where(pi => pi.PropertyId == propertyId);

            if (!includeInactive)
            {
                query = query.Where(pi => pi.IsActive);
            }

            return await query
                .AsNoTracking()
                .OrderBy(pi => pi.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var image = await _context.PropertyImages
                .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
                
            if (image == null)
                return false;
                
            image.IsActive = false;
            image.UpdatedAt = DateTime.UtcNow;
            
            _context.PropertyImages.Update(image);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        
        public async Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            return await _context.Properties
                .AnyAsync(p => p.Id == propertyId, cancellationToken);
        }
        
        public async Task<int> GetCountByPropertyIdAsync(int propertyId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.PropertyImages
                .Where(pi => pi.PropertyId == propertyId);
                
            if (!includeInactive)
            {
                query = query.Where(pi => pi.IsActive);
            }
            
            return await query.CountAsync(cancellationToken);
        }
    }
}