using Microsoft.EntityFrameworkCore;
using Properties.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Properties.Domain.Common;

namespace Properties.Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Owner> Owners => Set<Owner>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<PropertyTrace> PropertyTraces => Set<PropertyTrace>();
        
        public DbSet<Domain.Entities.Auth.User> Users => Set<Domain.Entities.Auth.User>();
        public DbSet<Domain.Entities.Auth.Role> Roles => Set<Domain.Entities.Auth.Role>();
        public DbSet<Domain.Entities.Auth.UserRole> UserRoles => Set<Domain.Entities.Auth.UserRole>();
        public DbSet<Domain.Entities.Auth.RefreshToken> RefreshTokens => Set<Domain.Entities.Auth.RefreshToken>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.IsActive = true;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            ConfigureOwners(modelBuilder);
            ConfigureProperties(modelBuilder);
            ConfigurePropertyImages(modelBuilder);
            ConfigureAuthEntities(modelBuilder);
            ConfigurePropertyTraces(modelBuilder);
        }

        private static void ConfigureOwners(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Address)
                    .HasMaxLength(200);
                
                entity.Property(e => e.Photo)
                    .HasMaxLength(500);
                
                entity.Property(e => e.Birthday)
                    .IsRequired();
            });
        }

        private static void ConfigureProperties(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Property>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();
                
                entity.Property(e => e.CodeInternal)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Year)
                    .IsRequired();
                
                entity.HasOne(p => p.Owner)
                    .WithMany(o => o.Properties)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigurePropertyImages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PropertyImage>(entity =>
            {
                entity.HasOne(pi => pi.Property)
                    .WithMany(p => p.PropertyImages)
                    .HasForeignKey(pi => pi.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAuthEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.Entities.Auth.User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.FirstName).HasMaxLength(200);
                entity.Property(u => u.LastName).HasMaxLength(200);
                entity.Property(u => u.CreatedAt).IsRequired();
                
                entity.HasMany(u => u.UserRoles)
                    .WithOne(ur => ur.User)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.RefreshTokens)
                    .WithOne(rt => rt.User)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Domain.Entities.Auth.Role>(entity =>
            {
                entity.HasIndex(r => r.Name).IsUnique();
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).HasMaxLength(500);
                
                entity.HasMany(r => r.UserRoles)
                    .WithOne(ur => ur.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Domain.Entities.Auth.UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);
                    
                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<Domain.Entities.Auth.RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.Expires).IsRequired();
                
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigurePropertyTraces(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PropertyTrace>(entity =>
            {
                entity.Property(e => e.DateSale)
                    .IsRequired();
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Value)
                    .HasPrecision(18, 2)
                    .IsRequired();
                
                entity.Property(e => e.Tax)
                    .HasPrecision(18, 2)
                    .IsRequired();
                
                entity.HasOne(pt => pt.Property)
                    .WithMany(p => p.PropertyTraces)
                    .HasForeignKey(pt => pt.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}