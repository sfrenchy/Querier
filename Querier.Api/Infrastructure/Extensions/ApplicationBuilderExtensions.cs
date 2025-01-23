using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

namespace Querier.Api.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Configuring CORS settings");

            try
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();

                    var allowedHosts = settingService.GetSettingValueAsync("api:allowedHosts", "*").Result?.Split(',');
                    var allowedOrigins = settingService.GetSettingValueAsync("api:allowedOrigins", "*").Result?.Split(',');
                    var allowedMethods = settingService.GetSettingValueAsync("api:allowedMethods", "GET,POST,DELETE,OPTIONS,PUT").Result?.Split(',');
                    var allowedHeaders = settingService.GetSettingValueAsync("api:allowedHeaders", "X-Request-Token,Accept,Content-Type,Authorization").Result?.Split(',');
                    var preflightMaxAge = settingService.GetSettingValueAsync("api:PreflightMaxAge", 10).Result;

                    logger.LogDebug("CORS Configuration: Hosts: {Hosts}, Origins: {Origins}, Methods: {Methods}, Headers: {Headers}, MaxAge: {MaxAge}",
                        string.Join(",", allowedHosts),
                        string.Join(",", allowedOrigins),
                        string.Join(",", allowedMethods),
                        string.Join(",", allowedHeaders),
                        preflightMaxAge);

                    app.UseCors(builder =>
                    {
                        if (allowedOrigins.Contains("*"))
                        {
                            builder.AllowAnyOrigin();
                            logger.LogInformation("CORS configured to allow any origin");
                        }
                        else
                        {
                            builder.WithOrigins(allowedOrigins);
                            logger.LogInformation("CORS configured with specific origins: {Origins}", string.Join(",", allowedOrigins));
                        }

                        if (allowedHeaders.Contains("*"))
                        {
                            builder.AllowAnyHeader();
                            logger.LogInformation("CORS configured to allow any header");
                        }
                        else
                        {
                            builder.WithHeaders(allowedHeaders);
                            logger.LogInformation("CORS configured with specific headers: {Headers}", string.Join(",", allowedHeaders));
                        }

                        if (allowedMethods.Contains("*"))
                        {
                            builder.AllowAnyMethod();
                            logger.LogInformation("CORS configured to allow any method");
                        }
                        else
                        {
                            builder.WithMethods(allowedMethods);
                            logger.LogInformation("CORS configured with specific methods: {Methods}", string.Join(",", allowedMethods));
                        }

                        builder.SetPreflightMaxAge(TimeSpan.FromMinutes(preflightMaxAge));
                    });

                    logger.LogInformation("CORS configuration completed successfully");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring CORS settings. Using default permissive configuration");
                app.UseCors(builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            }

            return app;
        }

        public static IApplicationBuilder UseConfigurationCheck(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Setting up configuration check middleware");

            app.Use(async (context, next) =>
            {
                try
                {
                    var endpoint = context.GetEndpoint();
                    if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() == null)
                    {
                        // Allow SMTP test endpoint during initial setup
                        var path = context.Request.Path.Value?.ToLower();
                        if (path != null && path.EndsWith("/api/v1/smtp/test"))
                        {
                            logger.LogDebug("Allowing access to SMTP test endpoint: {Path}", path);
                            await next();
                            return;
                        }

                        using var scope = context.RequestServices.CreateScope();
                        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                        var isConfigured = await settingService.GetApiIsConfiguredAsync();
                        
                        if (!isConfigured)
                        {
                            logger.LogWarning("Application not configured. Blocking request to: {Path}", context.Request.Path);
                            context.Response.StatusCode = 503;
                            await context.Response.WriteAsJsonAsync(new { error = "Application not configured" });
                            return;
                        }

                        logger.LogDebug("Configuration check passed for: {Path}", context.Request.Path);
                    }
                    else
                    {
                        logger.LogDebug("Skipping configuration check for anonymous endpoint: {Path}", context.Request.Path);
                    }
                    
                    await next();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during configuration check for path: {Path}", context.Request.Path);
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Internal server error during configuration check" });
                }
            });

            logger.LogInformation("Configuration check middleware setup completed");
            return app;
        }
    }
} 