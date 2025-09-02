using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using properties.Api.Infrastructure.ServiceRegistrations;
using Properties.Infrastructure.Persistence.Context;
using Properties.Infrastructure.Persistence.Seeders;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory() + "/Configutarion")
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    builder.Configuration.Sources.Clear();
    builder.Configuration.AddConfiguration(configuration);
    ConfigureServices(builder.Services, builder.Configuration);
    var app = builder.Build();
    ConfigureMiddleware(app, builder.Environment);
    await InitializeDatabaseAsync(app.Services);
    app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddInfrastructureServices(configuration);

    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    services.AddEndpointsApiExplorer();
    services.AddSwaggerConfig();
    services.AddAuthorization();
}

static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment env)
{
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    
    app.UseRouting();
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    
    EnsureUploadsDirectoryExists(env);
}
static void EnsureUploadsDirectoryExists(IWebHostEnvironment env)
{
    var uploadsPath = Path.Combine(env.WebRootPath, "uploads");
    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }
}
static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database connection...");
        var context = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Verifying if database exists...");
        if (!await context.Database.CanConnectAsync())
        {
            logger.LogInformation("Database does not exist. Creating...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database created successfully.");
        }
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Seeding initial data...");
        using (var seedScope = serviceProvider.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedInitialDataAsync();
        }
        
        logger.LogInformation("Database setup and seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error setting up the database");
        logger.LogError("Error details: {ErrorMessage}", ex.Message);
        throw;
    }
}