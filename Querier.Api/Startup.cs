using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Services;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Extensions;
using Querier.Api.Infrastructure.Services;

namespace Querier.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<Startup>>();

            try
            {
                _logger.LogInformation("Starting services configuration");

                // Services de base (requis pour la configuration)
                _logger.LogInformation("Configuring core services");
                services.AddCoreServices();
                services.AddStackExchangeRedisCache(options => options.Configuration = "localhost:6379");

                // Configuration de la base de données et des services
                _logger.LogInformation("Configuring database and authentication services");
                services.AddCustomDatabase(_configuration);
                services.AddCustomAuthentication(_configuration);
                services.AddCustomIdentity();
                services.AddCustomLogging();
                services.AddApplicationServices();
                services.AddCustomSwagger();

                // Configuration des contrôleurs et CORS
                _logger.LogInformation("Configuring controllers and CORS");

                // Enregistrer notre activateur de contrôleur dynamique
                services.AddDynamicControllerActivator();

                services.AddControllers(options =>
                {
                    // Configurer MVC pour utiliser notre activateur
                    var activator = services.BuildServiceProvider().GetRequiredService<IControllerActivator>();
                    options.EnableEndpointRouting = true;
                })
                       .AddNewtonsoftJson(options => {
                           options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
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
                _logger.LogInformation("Configuring additional services");
                services.AddHealthChecks();
                services.AddSignalR();
                services.AddSingleton<IDynamicContextList, DynamicContextList>(_ => DynamicContextList.Instance);

                // Enregistrement du service d'encryption
                services.AddScoped<IEncryptionService, AesEncryptionService>();

                // Chargement dynamique des assemblies depuis la base de données
                _logger.LogInformation("Loading dynamic assemblies");
                //await services.AddDynamicAssemblies(_configuration);

                _logger.LogInformation("Services configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure services");
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApiDbContext dbContext)
        {
            try
            {
                _logger.LogInformation("Starting application configuration");

                ServiceActivator.Configure(app.ApplicationServices);

                if (env.IsDevelopment())
                {
                    _logger.LogInformation("Development environment detected, enabling developer exception page");
                    app.UseDeveloperExceptionPage();
                }

                _logger.LogInformation("Configuring Swagger");
                app.UseCustomSwagger();

                _logger.LogInformation("Configuring middleware pipeline");
                app.UseRouting();
                app.UseCustomCors();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseConfigurationCheck();

                // Configuration de l'identité après l'initialisation des services
                _logger.LogInformation("Configuring identity options");
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    try
                    {
                        var identityConfig = scope.ServiceProvider.GetRequiredService<IAspnetIdentityConfigurationService>();
                        // On attend explicitement la configuration pour s'assurer qu'elle est terminée avant de continuer
                        identityConfig.ConfigureIdentityOptions().Wait();
                        identityConfig.ConfigureTokenProviderOptions().Wait();
                        _logger.LogInformation("Identity configuration completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to configure identity options");
                        throw;
                    }
                }

                _logger.LogInformation("Configuring endpoints");
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapControllerRoute(
                        name: "public",
                        pattern: "api/v1/settings/configured",
                        defaults: new { controller = "PublicSettings", action = "GetApiIsConfigured" }
                    ).WithMetadata(new AllowAnonymousAttribute());
                });

                _logger.LogInformation("Configuring health checks and static files");
                app.UseHealthChecks("/healthcheck")
                   .UseStaticFiles();

                app.UseDynamicAssemblies();
                _logger.LogInformation("Application configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure application");
                throw;
            }
        }
    }
}
