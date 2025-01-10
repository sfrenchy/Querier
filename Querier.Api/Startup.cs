using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Querier.Api.Infrastructure.Extensions;
using Querier.Api.Infrastructure.Swagger.Helpers;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Utilities;
using Querier.Api.Infrastructure.Data;
using Querier.Api.Domain.Services;

namespace Querier.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async void ConfigureServices(IServiceCollection services)
        {
            // Services de base (requis pour la configuration)
            services.AddCoreServices();
            services.AddStackExchangeRedisCache(options => options.Configuration = "localhost:6379");

            // Configuration de la base de données et des services
            services.AddCustomDatabase(_configuration);
            services.AddCustomAuthentication(_configuration);
            services.AddCustomIdentity();
            services.AddCustomLogging();
            services.AddApplicationServices();
            services.AddCustomSwagger();

            // Configuration des contrôleurs et CORS
            services.AddControllers()
                   .AddNewtonsoftJson(options => {
                       options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                       options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                       options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                   });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Services additionnels
            services.AddHealthChecks();
            services.AddSignalR();
            services.AddSingleton<IDynamicContextList, DynamicContextList>(_ => DynamicContextList.Instance);

            // Chargement dynamique des assemblies depuis la base de données
            await services.AddDynamicAssemblies(_configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApiDbContext dbContext)
        {
            ServiceActivator.Configure(app.ApplicationServices);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger()
               .UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));

            app.UseRouting()
               .UseCustomCors()
               .UseAuthentication()
               .UseAuthorization()
               .UseConfigurationCheck();

            // Configuration de l'identité après l'initialisation des services
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var identityConfig = scope.ServiceProvider.GetRequiredService<IAspnetIdentityConfigurationService>();
                // On attend explicitement la configuration pour s'assurer qu'elle est terminée avant de continuer
                identityConfig.ConfigureIdentityOptions().Wait();
                identityConfig.ConfigureTokenProviderOptions().Wait();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "public",
                    pattern: "api/v1/settings/configured",
                    defaults: new { controller = "PublicSettings", action = "GetIsConfigured" }
                ).WithMetadata(new AllowAnonymousAttribute());
            });

            app.UseHealthChecks("/healthcheck")
               .UseStaticFiles();
        }
    }
}
