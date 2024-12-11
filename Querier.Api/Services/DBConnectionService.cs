using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Diagnostics.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Scaffolding.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Pomelo.EntityFrameworkCore.MySql.Diagnostics.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;
using Querier.Api.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Builder;

namespace Querier.Api.Services
{
    public class DBConnectionService : IDBConnectionService
    {
        private readonly IDbContextFactory<ApiDbContext> _apiDbContextFactory;
        private readonly IDynamicContextList _dynamicContextList;
        private readonly ILogger<DBConnectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationPartManager _partManager;
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionProvider;
        private readonly ISchemaGenerator _schemaGenerator;
        private readonly ISwaggerProvider _swaggerProvider;

        public DBConnectionService(
            IDynamicContextList dynamicContextList,
            IDbContextFactory<ApiDbContext> apiDbContextFactory,
            IServiceProvider serviceProvider,
            ILogger<DBConnectionService> logger,
            ApplicationPartManager partManager,
            IApiDescriptionGroupCollectionProvider apiDescriptionProvider,
            ISchemaGenerator schemaGenerator,
            ISwaggerProvider swaggerProvider)
        {
            _logger = logger;
            _apiDbContextFactory = apiDbContextFactory;
            _serviceProvider = serviceProvider;
            _dynamicContextList = dynamicContextList;
            _partManager = partManager;
            _apiDescriptionProvider = apiDescriptionProvider;
            _schemaGenerator = schemaGenerator;
            _swaggerProvider = swaggerProvider;
        }

        public async Task<AddDBConnectionResponse> AddConnectionAsync(AddDBConnectionRequest connection)
        {
            AddDBConnectionResponse result = new AddDBConnectionResponse();
            string connectionNamespace = "";
            string contextName = "";
            string procedureDescription = "";
            try
            {
                switch (connection.ConnectionType)
                {
                    case QDBConnectionType.SqlServer:
                        using (SqlConnection c = new SqlConnection(connection.ConnectionString))
                        {
                            c.Open();
                            connectionNamespace = $"{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = QDBConnectionState.Connected;
                        }
                        break;
                    case QDBConnectionType.MySQL:
                        using (MySqlConnection c = new MySqlConnection(connection.ConnectionString))
                        {
                            c.Open();
                            connectionNamespace = $"{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = QDBConnectionState.Connected;
                        }
                        break;
                    case QDBConnectionType.PgSQL:
                        using (NpgsqlConnection c = new NpgsqlConnection(connection.ConnectionString))
                        {
                            c.Open();
                            connectionNamespace = $"{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = QDBConnectionState.Connected;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                result.State = QDBConnectionState.ConnectionError;
                result.Messages.Add(ex.Message);
                return result;
            }

            // if acces to db OK => scaffolding context
            IReverseEngineerScaffolder scaffolder = null;
            switch (connection.ConnectionType)
            {
                case QDBConnectionType.SqlServer:
                    scaffolder = CreateMssqlScaffolder();
                    break;
                case QDBConnectionType.MySQL:
                    scaffolder = CreateMySQLScaffolder();
                    break;
                case QDBConnectionType.PgSQL:
                    scaffolder = CreatePgSQLScaffolder();
                    break;
            }

            var dbOpts = new DatabaseModelFactoryOptions();
            var modelOpts = new ModelReverseEngineerOptions();
            var codeGenOpts = new ModelCodeGenerationOptions()
            {
                RootNamespace = connectionNamespace,
                ContextName = contextName,
                ContextNamespace = connectionNamespace,
                ModelNamespace = connectionNamespace,
                SuppressConnectionStringWarning = true
            };

            var scaffoldedModelSources = scaffolder.ScaffoldModel(connection.ConnectionString, dbOpts, modelOpts, codeGenOpts);

            var contextFile = "";
            if (connection.ConnectionType == QDBConnectionType.SqlServer)
                contextFile = scaffoldedModelSources.ContextFile.Code.Replace(".UseSqlServer", ".UseLazyLoadingProxies().UseSqlServer");
            else if (connection.ConnectionType == QDBConnectionType.MySQL)    
                contextFile = scaffoldedModelSources.ContextFile.Code.Replace(".UseMySql", ".UseLazyLoadingProxies().UseMySql");
            else if (connection.ConnectionType == QDBConnectionType.PgSQL)
                contextFile = scaffoldedModelSources.ContextFile.Code.Replace(".UseNpgsql", ".UseLazyLoadingProxies().UseNpgsql");
            else
                throw new Exception("Unsupported SGBD");
            var sourceFiles = new List<string> { contextFile };
            sourceFiles.AddRange(scaffoldedModelSources.AdditionalFiles.Select(f => f.Code));
            string sourceZipPath = Path.GetTempFileName() + ".src.zip";
            Dictionary<string, string> srcZipContent = new Dictionary<string, string>();
            srcZipContent.Add(scaffoldedModelSources.ContextFile.Path, scaffoldedModelSources.ContextFile.Code);
            foreach (var addFile in scaffoldedModelSources.AdditionalFiles)
            {
                srcZipContent.Add(addFile.Path, addFile.Code);
            }
            // if scaffolding OK => Generate a common DB Schema representation for stored procedure
            if (connection.GenerateProcedureControllersAndServices && connection.ConnectionType == QDBConnectionType.SqlServer)
            {
                List<StoredProcedure> storedProcedures = DatabaseToCSharpConverter.ToProcedureList(connection.ConnectionString);
                procedureDescription = JsonConvert.SerializeObject(storedProcedures);
                var templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(), "Resources", "DBTemplating", "ProcedureParameters.st"
                );
                var procedureParamsTemplate = new Template(File.ReadAllText(templatePath), '$', '$');
                procedureParamsTemplate.Add("nameSpace", connectionNamespace);
                procedureParamsTemplate.Add("procedureList", storedProcedures);
                string procedureParamsContent = procedureParamsTemplate.Render();
                srcZipContent.Add("ProcedureParameters\\ProcedureParameters.cs", procedureParamsContent);
                sourceFiles.Add(procedureParamsContent);

                var procedureResultTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "DBTemplating", "ProcedureResultSet.st")
                ), '$', '$');
                procedureResultTemplate.Add("nameSpace", connectionNamespace);
                procedureResultTemplate.Add("procedureList", storedProcedures.Where(s => s.HasOutput).ToList());
                string procedureResultContent = procedureResultTemplate.Render();
                srcZipContent.Add("ProcedureResultSet\\ProcedureResultSet.cs", procedureResultContent);
                sourceFiles.Add(procedureResultContent);

                var procedureReportRequestTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "DBTemplating", "ProcedureReportRequests.st")
                ), '$', '$');
                procedureReportRequestTemplate.Add("nameSpace", connectionNamespace);
                procedureReportRequestTemplate.Add("procedureList", storedProcedures);
                string procedureReportRequestContent = procedureReportRequestTemplate.Render();
                srcZipContent.Add("ProcedureReportRequests\\ProcedureReportRequests.cs", procedureReportRequestContent);
                sourceFiles.Add(procedureReportRequestContent);

                var procedureContextTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "DBTemplating", "ProcedureContext.st")
                ), '$', '$');
                procedureContextTemplate.Add("nameSpace", connectionNamespace);
                procedureContextTemplate.Add("contextNameSpace", contextName);
                procedureContextTemplate.Add("procedureList", storedProcedures);
                string procedureContextContent = procedureContextTemplate.Render();
                srcZipContent.Add("ProcedureContext\\ProcedureContext.cs", procedureContextContent);
                sourceFiles.Add(procedureContextContent);
                
                var procedureServiceTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "DBTemplating", "ProcedureService.st")
                ), '$', '$');
                procedureServiceTemplate.Add("nameSpace", connectionNamespace);
                procedureServiceTemplate.Add("contextNameSpace", contextName);
                procedureServiceTemplate.Add("procedureList", storedProcedures);
                string procedureServiceContent = procedureServiceTemplate.Render();
                srcZipContent.Add("ProcedureService\\ProcedureService.cs", procedureServiceContent);
                sourceFiles.Add(procedureServiceContent);

                var procedureServiceResolverTemplate = new Template(File.ReadAllText("./Resources/DBTemplating/ProcedureServiceResolver.st"), '$', '$');
                procedureServiceResolverTemplate.Add("nameSpace", connectionNamespace);
                procedureServiceResolverTemplate.Add("contextNameSpace", contextName);
                procedureServiceResolverTemplate.Add("procedureList", storedProcedures);
                string procedureServiceResolverContent = procedureServiceResolverTemplate.Render();
                srcZipContent.Add("ProcedureServiceResolver\\ProcedureServiceResolver.cs", procedureServiceResolverContent);
                sourceFiles.Add(procedureServiceResolverContent);

                var procedureControllerTemplate = new Template(File.ReadAllText("./Resources/DBTemplating/ProcedureController.st"), '$', '$');
                procedureControllerTemplate.Add("nameSpace", connectionNamespace);
                procedureControllerTemplate.Add("contextNameSpace", contextName);
                procedureControllerTemplate.Add("procedureList", storedProcedures);
                procedureControllerTemplate.Add("contextRoute", connection.ContextApiRoute);
                string procedureControllerContent = procedureControllerTemplate.Render();
                srcZipContent.Add("ProcedureController\\ProcedureController.cs", procedureControllerContent);
                sourceFiles.Add(procedureControllerContent);
            }
            
            using (var sourceStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(sourceStream, ZipArchiveMode.Create))
                {
                    foreach (var item in srcZipContent)
                    {
                        var entry = archive.CreateEntry(item.Key);
                        using (var entryStream = entry.Open())
                        using (var streamWriter = new BinaryWriter(entryStream))
                        {
                            streamWriter.Write(Encoding.UTF8.GetBytes(item.Value), 0, Encoding.UTF8.GetBytes(item.Value).Length);
                        }                    
                    }
                }
                File.WriteAllBytes(sourceZipPath, sourceStream.ToArray());
            }
            string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
            using(FileStream srcFileStream = new FileStream(srcPath, FileMode.Create)) 
            {
                using (FileStream zipFileStream = new FileStream(sourceZipPath, FileMode.Open)) {
                    zipFileStream.CopyTo(srcFileStream);
                }
            }
            // Templating done, we are now compiling generated sources
            MemoryStream peStream = new MemoryStream();
            MemoryStream pdbStream = new MemoryStream();
            EmitResult emitResult = GenerateCode(connection.Name, sourceFiles).Emit(peStream, pdbStream);

            if (!emitResult.Success)
            {
                var sb = new StringBuilder();
                foreach (var diag in emitResult.Diagnostics)
                {
                    sb.AppendLine(diag.ToString());
                }
                string errorMessage = sb.ToString();
                Console.WriteLine(errorMessage);
                result.State = QDBConnectionState.CompilationError;
                result.Messages = new List<string>();
                result.Messages.Add(errorMessage);
                return result;
            }

            // Compiling done, we load the context
            var assemblyLoadContext = new AssemblyLoadContext("DbContext", false);
            peStream.Seek(0, SeekOrigin.Begin);
            var assembly = assemblyLoadContext.LoadFromStream(peStream);
            peStream.Seek(0, SeekOrigin.Begin);
            pdbStream.Seek(0, SeekOrigin.Begin);
            // Store connection to database
            QDBConnection newConnection = new QDBConnection();
            newConnection.ApiRoute = connection.ContextApiRoute;
            
            if (!Path.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");
            
            string dllPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.dll");
            string pdbPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.pdb");
            
            using(FileStream dllFileStream = new FileStream(dllPath, FileMode.Create)) 
            {
                peStream.CopyTo(dllFileStream);
            }
            using(FileStream pdbFileStream = new FileStream(pdbPath, FileMode.Create)) 
            {
                pdbStream.CopyTo(pdbFileStream);
            }
            

            newConnection.Name = connection.Name;
            newConnection.ConnectionString = connection.ConnectionString;
            newConnection.ConnectionType = connection.ConnectionType;
            newConnection.Description = procedureDescription;
            using (var apiDbContext = _apiDbContextFactory.CreateDbContext())
            {
                apiDbContext.QDBConnections.Add(newConnection);
                await apiDbContext.SaveChangesAsync();
            }
            result.State = QDBConnectionState.Available;
            File.Delete(sourceZipPath);
            await AssemblyLoader.LoadAssemblyFromQDBConnection(newConnection, _serviceProvider, _partManager, _logger);

            AssemblyLoader.RegenerateSwagger(_swaggerProvider, _logger);

            return result;
        }

        public async Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                QDBConnection toDelete = apiDbContext.QDBConnections.Find(request.DBConnectionId);
                int toDeleteId = toDelete.Id;

                apiDbContext.QDBConnections.Remove(toDelete);
                apiDbContext.SaveChanges();

                return new DeleteDBConnectionResponse()
                {
                   DeletedDBConnectionId = toDeleteId 
                };
            }
        }

        public async Task<List<QDBConnectionResponse>> GetAll()
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                return await apiDbContext.QDBConnections.Select(c => new QDBConnectionResponse() {
                        ApiRoute = c.ApiRoute,
                        ConnectionString = c.ConnectionString,
                        ConnectionType = c.ConnectionType.ToString(),
                        Id = c.Id,
                        Name = c.Name
                    }).ToListAsync();
            }
        }

        private CSharpCompilation GenerateCode(string contextName, List<string> sourceFiles)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
            var parsedSyntaxTrees = sourceFiles
                .Select(f => SyntaxFactory.ParseSyntaxTree(f, options));

            return CSharpCompilation.Create($"{contextName}_DataContext.dll",
                parsedSyntaxTrees,
                references: GetCompilationReferences(),
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug));
        }

        private List<MetadataReference> GetCompilationReferences()
        {
            var refs = new List<MetadataReference>();

            // Reference all assemblies referenced by this program 
            var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            refs.AddRange(referencedAssemblies.Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            // Add the missing ones needed to compile the assembly:
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(DisplayNameAttribute).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Task).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(OutputParameter<>).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(IDynamicContextProcedureWithParamsAndResult).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(IDistributedCache).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(MemoryStream).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(StreamReader).Assembly.Location));
            
            // If we decided to use LazyLoading, we need to add one more assembly:
            refs.Add(MetadataReference.CreateFromFile(typeof(ProxiesExtensions).Assembly.Location));
            
            return refs;
        }

        static IReverseEngineerScaffolder CreateMssqlScaffolder() =>
            new ServiceCollection()
               .AddEntityFrameworkSqlServer()
               .AddLogging()
               .AddEntityFrameworkDesignTimeServices()
               .AddSingleton<LoggingDefinitions, SqlServerLoggingDefinitions>()
               .AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
               .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
               .AddSingleton<IDatabaseModelFactory, SqlServerDatabaseModelFactory>()
               .AddSingleton<IProviderConfigurationCodeGenerator, SqlServerCodeGenerator>()
               .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
               .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
               .AddSingleton<ProviderCodeGeneratorDependencies>()
               .AddSingleton<AnnotationCodeGeneratorDependencies>()
               .BuildServiceProvider()
               .GetRequiredService<IReverseEngineerScaffolder>();

        static IReverseEngineerScaffolder CreateMySQLScaffolder() =>
            new ServiceCollection()
               .AddEntityFrameworkMySql()
               .AddLogging()
               .AddEntityFrameworkDesignTimeServices()
               .AddSingleton<LoggingDefinitions, MySqlLoggingDefinitions>()
               .AddSingleton<IRelationalTypeMappingSource, MySqlTypeMappingSource>()
               .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
               .AddSingleton<IDatabaseModelFactory, MySqlDatabaseModelFactory>()
               .AddSingleton<IProviderConfigurationCodeGenerator, MySqlCodeGenerator>()
               .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
               .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
               .AddSingleton<ProviderCodeGeneratorDependencies>()
               .AddSingleton<AnnotationCodeGeneratorDependencies>()
               .BuildServiceProvider()
               .GetRequiredService<IReverseEngineerScaffolder>();

        static IReverseEngineerScaffolder CreatePgSQLScaffolder() =>
            new ServiceCollection()
               .AddEntityFrameworkNpgsql()
               .AddLogging()
               .AddEntityFrameworkDesignTimeServices()
               .AddSingleton<LoggingDefinitions, NpgsqlLoggingDefinitions>()
               .AddSingleton<IRelationalTypeMappingSource, NpgsqlTypeMappingSource>()
               .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
               .AddSingleton<IDatabaseModelFactory, NpgsqlDatabaseModelFactory>()
               .AddSingleton<IProviderConfigurationCodeGenerator, NpgsqlCodeGenerator>()
               .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
               .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
               .AddSingleton<ProviderCodeGeneratorDependencies>()
               .AddSingleton<AnnotationCodeGeneratorDependencies>()
               .BuildServiceProvider()
               .GetRequiredService<IReverseEngineerScaffolder>();
    }
}