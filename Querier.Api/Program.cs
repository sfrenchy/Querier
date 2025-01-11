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
            InitializeDatabase();
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void InitializeDatabase()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("ApiDBConnection"));

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ApiDbContext>();

            using (var context = new ApiDbContext(optionsBuilder.Options, configuration, logger))
            {
                Console.WriteLine("Ensuring database exists...");
                context.Database.EnsureCreated();
            }
        }
    }
}
