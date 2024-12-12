using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;
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

            using (var context = new ApiDbContext(optionsBuilder.Options, configuration))
            {
                Console.WriteLine("Ensuring database exists...");
                context.Database.EnsureCreated();
            }
        }
    }
}
