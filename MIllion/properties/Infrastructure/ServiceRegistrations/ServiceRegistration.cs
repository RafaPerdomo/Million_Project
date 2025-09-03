using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Properties.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Reflection;
using properties.Infrastructure.HealthChecks;
using Properties.Domain.Helpers;
using Properties.Domain.Interfaces;
using Properties.Domain.Interfaces.Services;
using Properties.Domain.Settings;
using Properties.Infrastructure.Persistence.Context;
using Properties.Infrastructure.Persistence.Repositories;
using Properties.Infrastructure.Persistence.Seeders;
using Properties.Infrastructure.Services;
using MediatR;
using System.Reflection;
using System;
using System.Text;
using properties.Api.Application.Commands.Propertys.CreatePropertyImages;
using properties.Api.Application.Commands.Owners.UpdateOwnerPhoto;
using Properties.Infrastructure.Persistence;
using properties.Api.Infrastructure.Cache;
using Scrutor;
using Microsoft.IdentityModel.Tokens;

namespace properties.Api.Infrastructure.ServiceRegistrations
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration["ConnectionStrings:DefaultConnection"];
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("The 'DefaultConnection' connection string was not found in the configuration. " +
                        "Please ensure it's set in appsettings.json or environment variables.");
                }
            }
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
                )
            );

            services.AddHealthChecks()
                .AddCheck<CacheHealthCheck>("cache_health_check");

            services.Scan(scan => scan
                .FromAssembliesOf(
                    typeof(Repository<>),
                    typeof(PropertyRepository),
                    typeof(OwnerRepository),
                    typeof(PropertyImageRepository),
                    typeof(PropertyTraceRepository),
                    typeof(UserRepository),
                    typeof(RoleRepository),
                    typeof(FileService))
                .AddClasses(classes => classes.AssignableToAny(
                    typeof(IRepository<>),
                    typeof(IPropertyRepository),
                    typeof(IOwnerRepository),
                    typeof(IPropertyImageRepository),
                    typeof(IPropertyTraceRepository),
                    typeof(IUserRepository),
                    typeof(IRoleRepository),
                    typeof(IFileService)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                
                .FromAssembliesOf(typeof(AuthService))
                .AddClasses(classes => classes.AssignableToAny(
                    typeof(IAuthService)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                
                .FromAssembliesOf(
                    typeof(CreatePropertyImagesCommandValidator),
                    typeof(UpdateOwnerPhotoCommandValidator))
                .AddClasses(classes => classes.AssignableToAny(
                    typeof(CreatePropertyImagesCommandValidator),
                    typeof(UpdateOwnerPhotoCommandValidator)))
                .AsSelf()
                .WithScopedLifetime()
            );

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<DataSeeder>();
            
            services.AddMediatR(cfg => 
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
                
            services.AddCachingServices();

            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            
            var jwtSettings = new JwtSettings();
            configuration.GetSection("Jwt").Bind(jwtSettings);
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}