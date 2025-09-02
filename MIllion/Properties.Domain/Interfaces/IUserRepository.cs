using Properties.Domain.Entities.Auth;
using System.Threading.Tasks;

namespace Properties.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByRefreshTokenAsync(string token);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailOrUsernameAsync(string emailOrUsername);
        Task<Role> GetRoleByNameAsync(string name);
    }
}
