using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Properties.Domain.Entities.Auth;
using Properties.Domain.Helpers;
using Properties.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Properties.Infrastructure.Persistence.Seeders
{
    public class DataSeeder : IDisposable
    {
        private bool _disposed = false;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SeedInitialDataAsync()
        {
            try
            {
                await SeedRolesAsync();
                await SeedAdminUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            if (await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Roles already exist in the database");
                return;
            }

            var roles = new[]
            {
                new Role { Name = "Admin", Description = "Administrator with full access" },
                new Role { Name = "User", Description = "Standard user with basic access" },
                new Role { Name = "Agent", Description = "Real estate agent with property management access" }
            };

            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Seeded default roles");
        }

        private async Task SeedAdminUserAsync()
        {
            if (await _context.Users.AnyAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin")))
            {
                _logger.LogInformation("Admin user already exists");
                return;
            }

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                _logger.LogError("Admin role not found. Please seed roles first.");
                return;
            }

            var passwordHash = PasswordHasher.Hash("Admin123");
            var segments = passwordHash.Split(':');
            var passwordBytes = Convert.FromBase64String(segments[0]);
            var saltBytes = Convert.FromBase64String(segments[1]);
            
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@properties.com",
                PasswordHash = passwordBytes,
                PasswordSalt = saltBytes,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            adminUser.UserRoles.Add(new UserRole { Role = adminRole });
            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Seeded admin user with username: {Username}", adminUser.Username);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DataSeeder()
        {
            Dispose(false);
        }
    }
}
