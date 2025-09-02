using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using Properties.Infrastructure.Persistence.Context;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Properties.Infrastructure.Persistence.Repositories
{
    public class OwnerRepository : Repository<Owner>, IOwnerRepository
    {
        private readonly ApplicationDbContext _context;

        public OwnerRepository(ApplicationDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Owner> GetByIdWithPropertiesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Owners
                .Include(o => o.Properties.Where(p => p.IsActive))
                    .ThenInclude(p => p.PropertyTraces)
                .Include(o => o.Properties)
                    .ThenInclude(p => p.PropertyImages)
                .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);
        }

        public async Task<IReadOnlyList<Owner>> GetAllWithPropertiesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Owners
                .Where(o => o.IsActive)
                .Include(o => o.Properties.Where(p => p.IsActive))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> OwnerExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Owners
                .AnyAsync(o => o.Id == id && o.IsActive, cancellationToken);
        }

        public async Task<Owner> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Owners
                .FirstOrDefaultAsync(o => 
                    o.Name.ToLower() == name.Trim().ToLower() && 
                    o.IsActive, 
                    cancellationToken);
        }
    }
}