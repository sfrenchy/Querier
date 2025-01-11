using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

namespace Querier.Api.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();

                var allowedHosts = settingService.GetSettingValueAsync("api:allowedHosts", "*").Result?.Split(',');
                var allowedOrigins = settingService.GetSettingValueAsync("api:allowedOrigins", "*").Result?.Split(',');
                var allowedMethods = settingService.GetSettingValueAsync("api:allowedMethods", "GET,POST,DELETE,OPTIONS,PUT").Result?.Split(',');
                var allowedHeaders = settingService.GetSettingValueAsync("api:allowedHeaders", "X-Request-Token,Accept,Content-Type,Authorization").Result?.Split(',');
                var preflightMaxAge = settingService.GetSettingValueAsync("api:PreflightMaxAge", 10).Result;

                app.UseCors(builder =>
                {
                    if (allowedOrigins.Contains("*"))
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(allowedOrigins);
                    }

                    if (allowedHeaders.Contains("*"))
                    {
                        builder.AllowAnyHeader();
                    }
                    else
                    {
                        builder.WithHeaders(allowedHeaders);
                    }

                    if (allowedMethods.Contains("*"))
                    {
                        builder.AllowAnyMethod();
                    }
                    else
                    {
                        builder.WithMethods(allowedMethods);
                    }

                    builder.SetPreflightMaxAge(TimeSpan.FromMinutes(preflightMaxAge));
                });
            }

            return app;
        }

        public static IApplicationBuilder UseConfigurationCheck(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var endpoint = context.GetEndpoint();
                if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() == null)
                {
                    // Allow SMTP test endpoint during initial setup
                    var path = context.Request.Path.Value?.ToLower();
                    if (path != null && (
                        path.EndsWith("/api/v1/smtp/test")))
                    {
                        await next();
                        return;
                    }

                    using var scope = context.RequestServices.CreateScope();
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                    var isConfigured = await settingService.GetApiIsConfiguredAsync();
                    
                    if (!isConfigured)
                    {
                        context.Response.StatusCode = 503;
                        await context.Response.WriteAsJsonAsync(new { error = "Application not configured" });
                        return;
                    }
                }
                
                await next();
            });

            return app;
        }
    }
} 