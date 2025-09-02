using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
namespace properties.Infrastructure.AppBuilder
{
    public static class SwaggerConfig
    {
        public static IApplicationBuilder ConfigureSwagger(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices;
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Properties API V1");
                    c.DefaultModelsExpandDepth(-1);
                });
            }
            return app;
        }
    }
}