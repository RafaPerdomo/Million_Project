using Properties.Domain.Entities.Auth;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role> GetByNameAsync(string name);
    }
}
