using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Querier.Api.Models.Common
{
    public class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
    {
        public ApiDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                                    .AddJsonFile(args[0])
                                                    .Build();
            
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            return new ApiDbContext(optionsBuilder.Options, configuration);
        }
    }
}