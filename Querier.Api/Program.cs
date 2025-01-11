using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Startup>>();

            try
            {
                logger.LogInformation("Starting application initialization");
                InitializeDatabase(logger);
                logger.LogInformation("Database initialization completed");

                logger.LogInformation("Running host");
                host.Run();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application startup failed");
                throw;
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                    
                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Information);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void InitializeDatabase(ILogger logger)
        {
            try
            {
                logger.LogInformation("Loading configuration");
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                logger.LogInformation("Configuring database context");
                var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
                optionsBuilder.UseSqlite(configuration.GetConnectionString("ApiDBConnection"));

                var serviceProvider = new ServiceCollection()
                    .AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    })
                    .BuildServiceProvider();

                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                var dbLogger = loggerFactory.CreateLogger<ApiDbContext>();

                logger.LogInformation("Ensuring database exists and is up to date");
                using (var context = new ApiDbContext(optionsBuilder.Options, configuration, dbLogger))
                {
                    context.Database.EnsureCreated();
                }
                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to initialize database");
                throw;
            }
        }
    }
}
