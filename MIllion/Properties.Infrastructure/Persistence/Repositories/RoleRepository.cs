using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities.Auth;
using Properties.Domain.Interfaces;
using Properties.Infrastructure.Persistence.Context;
using System.Threading.Tasks;

namespace Properties.Infrastructure.Persistence.Repositories
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Role> GetByNameAsync(string name)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == name);
        }
    }
}
