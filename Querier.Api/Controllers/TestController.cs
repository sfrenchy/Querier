using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TestController> _logger;
        public TestController(IServiceProvider serviceProvider, ILogger<TestController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> Test()
        {
            var _sourceCodeService = new SourceCodeService(Domain.Common.Enums.DbConnectionType.SqlServer, "Server=localhost;Database=Northwind;User=arnaud;Password=arnaud;MultipleActiveResultSets=True;TrustServerCertificate=True", "Northwind", "northwind", _logger);
            await _sourceCodeService.GenerateDbConnectionSourcesAsync();
            var zipFile = await _sourceCodeService.CreateSourceZipAsync();

            if (!Directory.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");
            string srcPath = Path.Combine("Assemblies", $"Northwind.DynamicContext.Sources.zip");
            await System.IO.File.WriteAllBytesAsync(srcPath, zipFile);
            /*
            var scope = _serviceProvider.CreateScope();
            var roslynCompilerService = scope.ServiceProvider.GetRequiredService<IRoslynCompilerService>();
            
            var compilationResult = roslynCompilerService.CompileAssembly("Test", _sourceCodeService.GetGeneratedSyntaxTrees());
            */
            return Ok();
        }
    }
}
