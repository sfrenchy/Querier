using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Antlr4.StringTemplate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Database.Generators;
using Querier.Api.Infrastructure.Database.Parameters;
using Querier.Api.Infrastructure.Database.Models;
using System.Security.Cryptography;
using QDBConnection = Querier.Api.Domain.Entities.QDBConnection.QDBConnection;
using EndpointParameterInfo = Querier.Api.Application.DTOs.Responses.DBConnection.ParameterInfo;

namespace Querier.Api.Domain.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
    public class SummaryAttribute : Attribute
    {
        public string Summary { get; }

        public SummaryAttribute(string summary)
        {
            Summary = summary;
        }
    }

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
                SuppressConnectionStringWarning = true,
                SuppressOnConfiguring = true
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
                List<Querier.Api.Domain.Entities.QDBConnection.StoredProcedure> storedProcedures = DatabaseToCSharpConverter.ToProcedureList(connection.ConnectionString);
                procedureDescription = JsonSerializer.Serialize(storedProcedures);

                var procedureModel = new StoredProcedureTemplateModel
                {
                    NameSpace = connectionNamespace,
                    ContextNameSpace = contextName,
                    ContextRoute = connection.ContextApiRoute,
                    ProcedureList = ExtractStoredProcedureMetadata(storedProcedures)
                };

                var procedureParamsTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureParameters.st")
                ), '$', '$');
                procedureParamsTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureParamsTemplate.Add("procedureList", procedureModel.ProcedureList);
                string procedureParamsContent = procedureParamsTemplate.Render();
                srcZipContent.Add("ProcedureParameters\\ProcedureParameters.cs", procedureParamsContent);
                sourceFiles.Add(procedureParamsContent);

                var procedureResultTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureResultSet.st")
                ), '$', '$');
                procedureResultTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureResultTemplate.Add("procedureList", procedureModel.ProcedureList.Where(s => s.HasOutput).ToList());
                string procedureResultContent = procedureResultTemplate.Render();
                srcZipContent.Add("ProcedureResultSet\\ProcedureResultSet.cs", procedureResultContent);
                sourceFiles.Add(procedureResultContent);

                var procedureReportRequestTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureReportRequests.st")
                ), '$', '$');
                procedureReportRequestTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureReportRequestTemplate.Add("procedureList", procedureModel.ProcedureList);
                string procedureReportRequestContent = procedureReportRequestTemplate.Render();
                srcZipContent.Add("ProcedureReportRequests\\ProcedureReportRequests.cs", procedureReportRequestContent);
                sourceFiles.Add(procedureReportRequestContent);

                var procedureContextTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureContext.st")
                ), '$', '$');
                procedureContextTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureContextTemplate.Add("contextNameSpace", procedureModel.ContextNameSpace);
                procedureContextTemplate.Add("procedureList", procedureModel.ProcedureList);
                string procedureContextContent = procedureContextTemplate.Render();
                srcZipContent.Add("ProcedureContext\\ProcedureContext.cs", procedureContextContent);
                sourceFiles.Add(procedureContextContent);

                var procedureServiceTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureService.st")
                ), '$', '$');
                procedureServiceTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureServiceTemplate.Add("contextNameSpace", procedureModel.ContextNameSpace);
                procedureServiceTemplate.Add("procedureList", procedureModel.ProcedureList);
                string procedureServiceContent = procedureServiceTemplate.Render();
                srcZipContent.Add("ProcedureService\\ProcedureService.cs", procedureServiceContent);
                sourceFiles.Add(procedureServiceContent);

                var procedureServiceResolverTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureServiceResolver.st")
                ), '$', '$');
                procedureServiceResolverTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureServiceResolverTemplate.Add("contextNameSpace", procedureModel.ContextNameSpace);
                procedureServiceResolverTemplate.Add("procedureList", procedureModel.ProcedureList);
                string procedureServiceResolverContent = procedureServiceResolverTemplate.Render();
                srcZipContent.Add("ProcedureServiceResolver\\ProcedureServiceResolver.cs", procedureServiceResolverContent);
                sourceFiles.Add(procedureServiceResolverContent);

                var procedureControllerTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureController.st")
                ), '$', '$');
                procedureControllerTemplate.Add("nameSpace", procedureModel.NameSpace);
                procedureControllerTemplate.Add("contextNameSpace", procedureModel.ContextNameSpace);
                procedureControllerTemplate.Add("procedureList", procedureModel.ProcedureList);
                procedureControllerTemplate.Add("contextRoute", procedureModel.ContextRoute);
                string procedureControllerContent = procedureControllerTemplate.Render();
                srcZipContent.Add("ProcedureController\\ProcedureController.cs", procedureControllerContent);
                sourceFiles.Add(procedureControllerContent);
            }

            // Extract entity metadata from scaffolded model
            var entityModel = new TemplateModel
            {
                NameSpace = connectionNamespace,
                ContextNameSpace = contextName,
                ContextRoute = connection.ContextApiRoute,
                EntityList = ExtractEntityMetadata(scaffoldedModelSources)
            };

            // Generate CRUD code using templates
            var entityDtoTemplate = new Template(File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "EntityDto.st")
            ), '$', '$');
            entityDtoTemplate.Add("nameSpace", entityModel.NameSpace);
            entityDtoTemplate.Add("entityList", entityModel.EntityList);
            string entityDtoContent = entityDtoTemplate.Render();
            srcZipContent.Add("DTOs\\EntityDtos.cs", entityDtoContent);
            sourceFiles.Add(entityDtoContent);

            var entityServiceTemplate = new Template(File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "EntityService.st")
            ), '$', '$');
            entityServiceTemplate.Add("nameSpace", entityModel.NameSpace);
            entityServiceTemplate.Add("contextNameSpace", entityModel.ContextNameSpace);
            entityServiceTemplate.Add("entityList", entityModel.EntityList);
            string entityServiceContent = entityServiceTemplate.Render();
            srcZipContent.Add("Services\\EntityServices.cs", entityServiceContent);
            sourceFiles.Add(entityServiceContent);

            var entityControllerTemplate = new Template(File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "EntityController.st")
            ), '$', '$');
            entityControllerTemplate.Add("nameSpace", entityModel.NameSpace);
            entityControllerTemplate.Add("contextRoute", entityModel.ContextRoute);
            entityControllerTemplate.Add("entityList", entityModel.EntityList);
            string entityControllerContent = entityControllerTemplate.Render();
            srcZipContent.Add("Controllers\\EntityControllers.cs", entityControllerContent);
            sourceFiles.Add(entityControllerContent);

            // Generate entity services resolver
            var entityServiceResolverTemplate = new Template(File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "EntityServiceResolver.st")
            ), '$', '$');
            entityServiceResolverTemplate.Add("nameSpace", entityModel.NameSpace);
            entityServiceResolverTemplate.Add("contextNameSpace", entityModel.ContextNameSpace);
            entityServiceResolverTemplate.Add("entityList", entityModel.EntityList);
            string entityServiceResolverContent = entityServiceResolverTemplate.Render();
            srcZipContent.Add("Services\\EntityServiceResolver.cs", entityServiceResolverContent);
            sourceFiles.Add(entityServiceResolverContent);

            // Créer le zip des sources une seule fois
            byte[] sourceZipBytes;
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
                sourceZipBytes = sourceStream.ToArray();
            }

            if (!Directory.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");
            string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
            File.WriteAllBytes(srcPath, sourceZipBytes);

            // Templating done, we are now compiling generated sources
            MemoryStream peStream = new MemoryStream();
            MemoryStream pdbStream = new MemoryStream();
            EmitResult emitResult = GenerateCode(connection.Name, sourceFiles).Emit(peStream, pdbStream);

            if (!emitResult.Success)
            {
                var compilationErrors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => new
                    {
                        Location = d.Location.GetLineSpan().StartLinePosition,
                        Message = d.GetMessage(),
                        ErrorCode = d.Id
                    })
                    .ToList();

                var errorMessage = string.Join("\n", compilationErrors.Select(e => 
                    $"Error {e.ErrorCode} at line {e.Location.Line + 1}: {e.Message}"));
                
                _logger.LogError("Code compilation failed:\n{Errors}", errorMessage);
                return new AddDBConnectionResponse 
                { 
                    State = QDBConnectionState.CompilationError,
                    Messages = new List<string> { errorMessage }
                };
            }

            // Sauvegarder les bytes des assemblies
            peStream.Seek(0, SeekOrigin.Begin);
            var assemblyBytes = peStream.ToArray();
            pdbStream.Seek(0, SeekOrigin.Begin);
            var pdbBytes = pdbStream.ToArray();

            // Calculer le hash de l'assembly
            var hash = ComputeAssemblyHash(assemblyBytes);

            // Charger l'assembly depuis une nouvelle copie des bytes
            var assemblyLoadContext = new AssemblyLoadContext("DbContext", false);
            var loadedAssembly = assemblyLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));

            // Créer la nouvelle connexion
            var endpoints = ExtractEndpointDescriptions(loadedAssembly);
            var newConnection = new QDBConnection
            {
                Name = connection.Name,
                ConnectionString = connection.ConnectionString,
                ConnectionType = connection.ConnectionType,
                Description = procedureDescription,
                AssemblyHash = hash,
                AssemblyDll = assemblyBytes,
                AssemblyPdb = pdbBytes,
                AssemblySourceZip = sourceZipBytes,
                ContextName = connectionNamespace + "." + contextName,
                ApiRoute = connection.ContextApiRoute
            };
            newConnection.Endpoints = endpoints;
            
            using (var apiDbContext = _apiDbContextFactory.CreateDbContext())
            {
                apiDbContext.QDBConnections.Add(newConnection);
                await apiDbContext.SaveChangesAsync();
            }

            result.State = QDBConnectionState.Available;
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
                return await apiDbContext.QDBConnections.Select(c => new QDBConnectionResponse()
                {
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

        private string ComputeAssemblyHash(byte[] assemblyBytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(assemblyBytes);
                return Convert.ToBase64String(hash);
            }
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

        public async Task<DatabaseSchemaResponse> GetDatabaseSchema(int connectionId)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
            var connection = await apiDbContext.QDBConnections.FindAsync(connectionId);
            
            if (connection == null)
                throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

            var response = new DatabaseSchemaResponse();

            try
            {
                switch (connection.ConnectionType)
                {
                    case QDBConnectionType.SqlServer:
                        await GetSqlServerSchema(connection.ConnectionString, response);
                        break;
                    case QDBConnectionType.MySQL:
                        await GetMySqlSchema(connection.ConnectionString, response);
                        break;
                    case QDBConnectionType.PgSQL:
                        await GetPgSqlSchema(connection.ConnectionString, response);
                        break;
                    default:
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database schema for connection {ConnectionId}", connectionId);
                throw;
            }

            return response;
        }

        private async Task GetSqlServerSchema(string connectionString, DatabaseSchemaResponse response)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get Tables and Columns
            var tableQuery = @"
                SELECT 
                    t.TABLE_SCHEMA,
                    t.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY,
                    CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_FOREIGN_KEY,
                    fk.REFERENCED_TABLE_NAME,
                    fk.REFERENCED_COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLES t
                INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                LEFT JOIN (
                    SELECT ku.TABLE_CATALOG,ku.TABLE_SCHEMA,ku.TABLE_NAME,ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
                        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN (
                    SELECT 
                        cu.TABLE_CATALOG,cu.TABLE_SCHEMA,cu.TABLE_NAME,cu.COLUMN_NAME,
                        cu2.TABLE_NAME as REFERENCED_TABLE_NAME, cu2.COLUMN_NAME as REFERENCED_COLUMN_NAME
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu ON rc.CONSTRAINT_NAME = cu.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu2 ON rc.UNIQUE_CONSTRAINT_NAME = cu2.CONSTRAINT_NAME
                ) fk ON c.TABLE_NAME = fk.TABLE_NAME AND c.COLUMN_NAME = fk.COLUMN_NAME
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

            using (var command = new SqlCommand(tableQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                TableDescription currentTable = null;
                string currentTableName = null;
                string currentSchema = null;

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var tableName = reader.GetString(1);

                    if (currentTableName != tableName || currentSchema != schema)
                    {
                        currentTable = new TableDescription
                        {
                            Name = tableName,
                            Schema = schema
                        };
                        response.Tables.Add(currentTable);
                        currentTableName = tableName;
                        currentSchema = schema;
                    }

                    currentTable.Columns.Add(new ColumnDescription
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES",
                        IsPrimaryKey = reader.GetInt32(5) == 1,
                        IsForeignKey = reader.GetInt32(6) == 1,
                        ForeignKeyTable = !reader.IsDBNull(7) ? reader.GetString(7) : null,
                        ForeignKeyColumn = !reader.IsDBNull(8) ? reader.GetString(8) : null
                    });
                }
            }

            // Get Views
            var viewQuery = @"
                SELECT 
                    v.TABLE_SCHEMA,
                    v.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE
                FROM INFORMATION_SCHEMA.VIEWS v
                INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON v.TABLE_NAME = c.TABLE_NAME AND v.TABLE_SCHEMA = c.TABLE_SCHEMA
                ORDER BY v.TABLE_SCHEMA, v.TABLE_NAME, c.ORDINAL_POSITION";

            using (var command = new SqlCommand(viewQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                ViewDescription currentView = null;
                string currentViewName = null;
                string currentSchema = null;

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var viewName = reader.GetString(1);

                    if (currentViewName != viewName || currentSchema != schema)
                    {
                        currentView = new ViewDescription
                        {
                            Name = viewName,
                            Schema = schema
                        };
                        response.Views.Add(currentView);
                        currentViewName = viewName;
                        currentSchema = schema;
                    }

                    currentView.Columns.Add(new ColumnDescription
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES"
                    });
                }
            }

            // Get Stored Procedures
            var spQuery = @"
                SELECT 
                    SPECIFIC_SCHEMA,
                    SPECIFIC_NAME,
                    PARAMETER_NAME,
                    DATA_TYPE,
                    PARAMETER_MODE
                FROM INFORMATION_SCHEMA.PARAMETERS
                WHERE SPECIFIC_SCHEMA != 'sys'
                ORDER BY SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION";

            using (var command = new SqlCommand(spQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                StoredProcedureDescription currentSp = null;
                string currentSpName = null;
                string currentSchema = null;

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var spName = reader.GetString(1);

                    if (currentSpName != spName || currentSchema != schema)
                    {
                        currentSp = new StoredProcedureDescription
                        {
                            Name = spName,
                            Schema = schema
                        };
                        response.StoredProcedures.Add(currentSp);
                        currentSpName = spName;
                        currentSchema = schema;
                    }

                    if (!reader.IsDBNull(2)) // Skip return value parameter
                    {
                        currentSp.Parameters.Add(new ParameterDescription
                        {
                            Name = reader.GetString(2),
                            DataType = reader.GetString(3),
                            Mode = reader.GetString(4)
                        });
                    }
                }
            }

            // Get User Functions
            var functionQuery = @"
                SELECT 
                    SCHEMA_NAME(SCHEMA_ID) as SPECIFIC_SCHEMA,
                    o.name as SPECIFIC_NAME,
                    p.name as PARAMETER_NAME,
                    TYPE_NAME(p.user_type_id) as DATA_TYPE,
                    CASE 
                        WHEN p.is_output = 1 THEN 'OUT'
                        ELSE 'IN'
                    END as PARAMETER_MODE
                FROM sys.objects o
                LEFT JOIN sys.parameters p ON o.object_id = p.object_id
                WHERE o.type IN ('FN', 'IF', 'TF')  -- FN: Scalar Function, IF: Inline Table Function, TF: Table Function
                AND SCHEMA_NAME(SCHEMA_ID) != 'sys'
                ORDER BY SCHEMA_NAME(SCHEMA_ID), o.name, p.parameter_id";

            using (var command = new SqlCommand(functionQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                UserFunctionDescription currentFunc = null;
                string currentFuncName = null;
                string currentSchema = null;

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var funcName = reader.GetString(1);

                    if (currentFuncName != funcName || currentSchema != schema)
                    {
                        currentFunc = new UserFunctionDescription
                        {
                            Name = funcName,
                            Schema = schema
                        };
                        response.UserFunctions.Add(currentFunc);
                        currentFuncName = funcName;
                        currentSchema = schema;
                    }

                    if (!reader.IsDBNull(2)) // Skip return value parameter
                    {
                        currentFunc.Parameters.Add(new ParameterDescription
                        {
                            Name = reader.GetString(2),
                            DataType = reader.GetString(3),
                            Mode = reader.GetString(4)
                        });
                    }
                }
            }
        }

        // Implémentez des méthodes similaires pour MySQL et PostgreSQL
        private Task GetMySqlSchema(string connectionString, DatabaseSchemaResponse response)
        {
            // TODO: Implémenter la logique spécifique à MySQL
            throw new NotImplementedException("MySQL schema extraction not yet implemented");
        }

        private Task GetPgSqlSchema(string connectionString, DatabaseSchemaResponse response)
        {
            // TODO: Implémenter la logique spécifique à PostgreSQL
            throw new NotImplementedException("PostgreSQL schema extraction not yet implemented");
        }

        public async Task<QueryAnalysisResponse> GetQueryObjects(int connectionId, string query)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
            var connection = await apiDbContext.QDBConnections.FindAsync(connectionId);
            
            if (connection == null)
                throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

            try
            {
                switch (connection.ConnectionType)
                {
                    case QDBConnectionType.SqlServer:
                        using (var sqlConnection = new SqlConnection(connection.ConnectionString))
                        {
                            await sqlConnection.OpenAsync();
                            
                            var response = new QueryAnalysisResponse();
                            var tempProcName = $"TempQuery_{Guid.NewGuid():N}";
                            
                            try
                            {
                                // Créer la procédure temporaire
                                using var createCmd = new SqlCommand($@"
                                    CREATE PROCEDURE [dbo].[{tempProcName}] AS
                                    BEGIN
                                        SET QUOTED_IDENTIFIER ON;
                                        SET NOCOUNT ON;
                                        {query}
                                    END", sqlConnection);
                                await createCmd.ExecuteNonQueryAsync();

                                // Analyser les dépendances avec leur type
                                using var analyzeCmd = new SqlCommand($@"
                                    SELECT DISTINCT 
                                        SCHEMA_NAME(o.schema_id) + '.' + referenced_entity_name as full_name,
                                        CASE o.type
                                            WHEN 'U' THEN 'Table'
                                            WHEN 'V' THEN 'View'
                                            WHEN 'P' THEN 'StoredProcedure'
                                            WHEN 'FN' THEN 'Function'
                                            WHEN 'IF' THEN 'Function'
                                            WHEN 'TF' THEN 'Function'
                                            ELSE o.type
                                        END as object_type
                                    FROM sys.dm_sql_referenced_entities('dbo.{tempProcName}', 'OBJECT') r
                                    JOIN sys.objects o ON o.name = r.referenced_entity_name
                                    WHERE referenced_minor_name IS NULL
                                    AND o.name NOT IN ('fn_diagramobjects')", sqlConnection);
                                
                                using var reader = await analyzeCmd.ExecuteReaderAsync();
                                while (await reader.ReadAsync())
                                {
                                    var objectName = reader.GetString(0);
                                    var objectType = reader.GetString(1);
                                    
                                    switch (objectType)
                                    {
                                        case "Table":
                                            response.Tables.Add(objectName);
                                            break;
                                        case "View":
                                            response.Views.Add(objectName);
                                            break;
                                        case "StoredProcedure":
                                            response.StoredProcedures.Add(objectName);
                                            break;
                                        case "Function":
                                            response.UserFunctions.Add(objectName);
                                            break;
                                    }
                                }
                            }
                            finally
                            {
                                using var dropCmd = new SqlCommand($"DROP PROCEDURE IF EXISTS [{tempProcName}]", sqlConnection);
                                await dropCmd.ExecuteNonQueryAsync();
                            }

                            return response;
                        }
                        
                    // Ajouter des cas similaires pour MySQL et PostgreSQL
                    default:
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query objects for connection {ConnectionId}", connectionId);
                throw;
            }
        }
        
        private async Task<List<IPAddress>> GetActiveHostsInNetwork()
        {
            var activeHosts = new List<IPAddress>();
            try
            {
                // Utiliser ARP pour trouver les hôtes actifs
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Parser la sortie de ARP pour extraire les adresses IP
                var ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                var matches = System.Text.RegularExpressions.Regex.Matches(output, ipPattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (IPAddress.TryParse(match.Value, out IPAddress ip))
                    {
                        activeHosts.Add(ip);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active hosts from ARP cache");
            }

            // Ajouter aussi les IPs locales
            var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address);
            activeHosts.AddRange(localIps);

            return activeHosts.Distinct().ToList();
        }

        private async Task<List<DatabaseServerInfo>> EnumerateServersWithPort(int port)
        {
            var servers = new List<DatabaseServerInfo>();

            try
            {
                var activeHosts = await GetActiveHostsInNetwork();

                foreach (var ip in activeHosts)
                {
                    try
                    {
                        using var client = new TcpClient();
                        var connectTask = client.ConnectAsync(ip, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(200)) == connectTask)
                        {
                            servers.Add(new DatabaseServerInfo
                            {
                                ServerName = ip.ToString(),
                                NetworkProtocol = "TCP",
                                Port = port
                            });
                        }
                    }
                    catch
                    {
                        // Ignore connection errors
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for servers on port {Port}", port);
            }

            return servers;
        }

        private Task<List<DatabaseServerInfo>> EnumerateSqlServers()
        {
            return EnumerateServersWithPort(1433);
        }

        private Task<List<DatabaseServerInfo>> EnumerateMySqlServers()
        {
            return EnumerateServersWithPort(3306);
        }

        private Task<List<DatabaseServerInfo>> EnumeratePostgresServers()
        {
            return EnumerateServersWithPort(5432);
        }
        
        public async Task<List<DatabaseServerInfo>> EnumerateServersAsync(string databaseType)
        {
            try
            {
                switch (databaseType)
                {
                    case "SQLServer":
                        return await EnumerateSqlServers();
                    case "MySQL":
                        return await EnumerateMySqlServers();
                    case "PostgreSQL":
                        return await EnumeratePostgresServers();
                    default:
                        throw new NotSupportedException($"Database type {databaseType} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating {DatabaseType} servers", databaseType);
                throw;
            }
        }

        private List<TemplateEntityMetadata> ExtractEntityMetadata(ScaffoldedModel scaffoldedModel)
        {
            var entities = new List<TemplateEntityMetadata>();
            var entityFiles = scaffoldedModel.AdditionalFiles.Where(f => !f.Path.EndsWith("Context.cs"));
            var pluralizer = new Bricelam.EntityFrameworkCore.Design.Pluralizer();

            foreach (var entityFile in entityFiles)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(entityFile.Code);
                var root = syntaxTree.GetRoot();
                var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                
                var entityName = classDeclaration.Identifier.Text;
                var entity = new TemplateEntityMetadata
                {
                    Name = entityName,
                    PluralName = pluralizer.Pluralize(entityName),
                    Properties = new List<TemplateProperty>()
                };

                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var attributes = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Select(a => a.Name.ToString())
                        .ToList();

                    // Check for [Key] attribute or if property name contains "Id" or ends with "Id"
                    var isKey = attributes.Contains("Key") || 
                               property.Identifier.Text.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                               property.Identifier.Text.EndsWith("Id", StringComparison.OrdinalIgnoreCase);

                    var isRequired = attributes.Contains("Required");
                    var isAutoGenerated = attributes.Contains("DatabaseGenerated") && 
                        property.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Any(a => a.ArgumentList?.Arguments
                                .Any(arg => arg.ToString().Contains("DatabaseGeneratedOption.Identity")) ?? false);

                    var prop = new TemplateProperty
                    {
                        Name = property.Identifier.Text,
                        CSName = property.Identifier.Text,
                        CSType = property.Type.ToString(),
                        IsKey = isKey,
                        IsRequired = isRequired,
                        IsAutoGenerated = isAutoGenerated
                    };

                    entity.Properties.Add(prop);

                    if (isKey)
                    {
                        entity.KeyType = property.Type.ToString();
                        entity.KeyName = property.Identifier.Text;
                    }
                }

                // Si aucune clé n'a été trouvée, on prend la première propriété qui se termine par "Id"
                if (string.IsNullOrEmpty(entity.KeyType))
                {
                    var idProperty = entity.Properties.FirstOrDefault(p => 
                        p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                        p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

                    if (idProperty != null)
                    {
                        idProperty.IsKey = true;
                        entity.KeyType = idProperty.CSType;
                        entity.KeyName = idProperty.Name;
                    }
                    else
                    {
                        // Fallback: utiliser la première propriété comme clé
                        var firstProperty = entity.Properties.First();
                        firstProperty.IsKey = true;
                        entity.KeyType = firstProperty.CSType;
                        entity.KeyName = firstProperty.Name;
                    }
                }

                entities.Add(entity);
            }

            return entities;
        }

        private List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(List<Querier.Api.Domain.Entities.QDBConnection.StoredProcedure> procedures)
        {
            return procedures.Select(p => new StoredProcedureMetadata
            {
                Name = p.Name,
                CSName = p.CSName,
                CSReturnSignature = p.CSReturnSignature,
                CSParameterSignature = p.CSParameterSignature,
                InlineParameters = p.InlineParameters,
                HasOutput = p.HasOutput,
                HasParameters = p.HasParameters,
                Parameters = p.Parameters.Select(param => new TemplateProperty
                {
                    Name = param.Name,
                    CSName = param.CSName,
                    CSType = param.CSType,
                    SqlParameterType = param.SqlParameterType
                }).ToList(),
                OutputSet = p.OutputSet.Select(col => new TemplateProperty
                {
                    Name = col.Name,
                    CSName = col.CSName,
                    CSType = col.CSType
                }).ToList(),
                SummableOutputColumns = p.SummableOutputColumns
            }).ToList();
        }

        public class SourceDownload
        {
            public byte[] Content { get; set; }
            public string FileName { get; set; }
        }

        public async Task<SourceDownload> GetConnectionSourcesAsync(int connectionId)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                var connection = await apiDbContext.QDBConnections.FindAsync(connectionId);
                if (connection == null)
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

                if (connection.AssemblySourceZip == null)
                    throw new InvalidOperationException($"No source code available for connection {connectionId}");

                return new SourceDownload
                {
                    Content = connection.AssemblySourceZip,
                    FileName = $"{connection.Name}.DynamicContext.Sources.zip"
                };
            }
        }

        private List<EndpointDescription> ExtractEndpointDescriptions(Assembly assembly)
        {
            var endpoints = new List<EndpointDescription>();
            
            foreach (var controller in assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t)))
            {
                var controllerRoute = controller.GetCustomAttributes<RouteAttribute>()
                    .FirstOrDefault()?.Template ?? string.Empty;

                foreach (var action in controller.GetMethods())
                {
                    var httpMethods = action.GetCustomAttributes()
                        .Where(a => a is HttpGetAttribute || 
                                  a is HttpPostAttribute || 
                                  a is HttpPutAttribute || 
                                  a is HttpDeleteAttribute)
                        .Select(a => a switch
                        {
                            HttpGetAttribute _ => "GET",
                            HttpPostAttribute _ => "POST",
                            HttpPutAttribute _ => "PUT",
                            HttpDeleteAttribute _ => "DELETE",
                            _ => "GET"
                        })
                        .ToList();

                    if (!httpMethods.Any()) continue;

                    var actionRoute = action.GetCustomAttributes<RouteAttribute>()
                        .FirstOrDefault()?.Template ?? string.Empty;

                    var description = new EndpointDescription
                    {
                        Controller = controller.Name,
                        Action = action.Name,
                        HttpMethod = string.Join(", ", httpMethods),
                        Route = CombineRoutes(controllerRoute, actionRoute),
                        Description = action.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                        Parameters = ExtractParameters(action).ToList(),
                        Responses = ExtractResponses(action).ToList()
                    };

                    endpoints.Add(description);
                }
            }

            return endpoints;
        }

        private IEnumerable<EndpointParameter> ExtractParameters(MethodInfo method)
        {
            foreach (var param in method.GetParameters())
            {
                var fromBody = param.GetCustomAttribute<FromBodyAttribute>();
                var fromQuery = param.GetCustomAttribute<FromQueryAttribute>();
                var fromRoute = param.GetCustomAttribute<FromRouteAttribute>();
                var required = param.GetCustomAttribute<RequiredAttribute>();

                var jsonSchema = GenerateJsonSchema(param.ParameterType);

                yield return new EndpointParameter
                {
                    Name = param.Name,
                    Type = param.ParameterType.Name,
                    Description = param.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                    IsRequired = required != null || !param.IsOptional,
                    Source = fromBody != null ? "FromBody" :
                            fromQuery != null ? "FromQuery" :
                            fromRoute != null ? "FromRoute" : "FromQuery",
                    JsonSchema = jsonSchema
                };
            }
        }

        private IEnumerable<EndpointResponse> ExtractResponses(MethodInfo method)
        {
            var produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(attr => attr.StatusCode == 200)
                .ToList();
            
            foreach (var response in produces)
            {
                var jsonSchema = response.Type != null ? GenerateJsonSchema(response.Type) : GenerateErrorSchema(response.StatusCode);

                yield return new EndpointResponse
                {
                    StatusCode = response.StatusCode,
                    Type = response.Type?.Name ?? "void",
                    Description = "Success",
                    JsonSchema = jsonSchema
                };
            }
        }

        private string GenerateErrorSchema(int statusCode)
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    code = new
                    {
                        type = "string",
                        description = "Code d'erreur"
                    },
                    message = new
                    {
                        type = "string",
                        description = "Message d'erreur"
                    },
                    details = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                field = new
                                {
                                    type = "string",
                                    description = "Champ concerné par l'erreur"
                                },
                                message = new
                                {
                                    type = "string",
                                    description = "Description de l'erreur"
                                }
                            }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = false });
        }

        private string CombineRoutes(params string[] routes)
        {
            return string.Join("/", routes
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => r.Trim('/'))
            );
        }

        private string GenerateJsonSchema(Type type)
        {
            if (type == null) return null;

            // Gérer les types génériques
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                // Cas spécial pour PagedResult<T>
                if (genericTypeDef == typeof(PagedResult<>))
                {
                    var itemType = genericArgs[0];
                    var schema = new
                    {
                        type = "object",
                        description = "Paginated result list",
                        properties = new
                        {
                            items = new
                            {
                                type = "array",
                                description = "List of items",
                                items = JsonSerializer.Deserialize<object>(GenerateJsonSchema(itemType))
                            },
                            total = new
                            {
                                type = "integer",
                                format = "int32",
                                description = "Total number of items"
                            }
                        }
                    };
                    return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = false });
                }

                // Cas spécial pour IEnumerable<T>, List<T>, etc.
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var itemType = genericArgs[0];
                    var schema = new
                    {
                        type = "array",
                        description = $"List of {itemType.Name}",
                        items = JsonSerializer.Deserialize<object>(GenerateJsonSchema(itemType))
                    };
                    return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = false });
                }

                // Cas spécial pour Task<T>
                if (genericTypeDef == typeof(Task<>))
                {
                    return GenerateJsonSchema(genericArgs[0]);
                }

                // Cas spécial pour les types Nullable<T>
                if (genericTypeDef == typeof(Nullable<>))
                {
                    var schema = JsonSerializer.Deserialize<object>(GenerateJsonSchema(genericArgs[0]));
                    // Ajouter null comme valeur possible
                    var schemaDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(schema));
                    schemaDict["nullable"] = true;
                    return JsonSerializer.Serialize(schemaDict, new JsonSerializerOptions { WriteIndented = false });
                }
            }

            // Pour les types non génériques, utiliser la logique existante
            var baseSchema = new
            {
                type = GetJsonType(type),
                format = GetJsonFormat(type),
                description = type.GetCustomAttribute<SummaryAttribute>()?.Summary,
                required = GetRequiredProperties(type),
                properties = GetJsonProperties(type),
                @enum = type.IsEnum ? Enum.GetNames(type) : null,
                minimum = GetMinValue(type),
                maximum = GetMaxValue(type),
                minLength = GetMinLength(type),
                maxLength = GetMaxLength(type),
                pattern = GetPattern(type)
            };

            return JsonSerializer.Serialize(baseSchema, new JsonSerializerOptions { WriteIndented = false });
        }

        private string[] GetRequiredProperties(Type type)
        {
            if (!type.IsClass || type == typeof(string)) return null;

            return type.GetProperties()
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                .Select(p => p.Name)
                .ToArray();
        }

        private object GetMinValue(Type type)
        {
            var rangeAttr = type.GetCustomAttribute<RangeAttribute>();
            return rangeAttr?.Minimum;
        }

        private object GetMaxValue(Type type)
        {
            var rangeAttr = type.GetCustomAttribute<RangeAttribute>();
            return rangeAttr?.Maximum;
        }

        private int? GetMinLength(Type type)
        {
            var strLengthAttr = type.GetCustomAttribute<StringLengthAttribute>();
            return strLengthAttr?.MinimumLength;
        }

        private int? GetMaxLength(Type type)
        {
            var strLengthAttr = type.GetCustomAttribute<StringLengthAttribute>();
            return strLengthAttr?.MaximumLength;
        }

        private string GetPattern(Type type)
        {
            var regexAttr = type.GetCustomAttribute<RegularExpressionAttribute>();
            return regexAttr?.Pattern;
        }

        private string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(DateTime)) return "string";
            if (type.IsArray || typeof(IEnumerable<>).IsAssignableFrom(type)) return "array";
            if (type.IsEnum) return "string";
            if (type.IsClass && type != typeof(string)) return "object";
            return "string";
        }

        private string GetJsonFormat(Type type)
        {
            if (type == typeof(DateTime)) return "date-time";
            if (type == typeof(int)) return "int32";
            if (type == typeof(long)) return "int64";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(string))
            {
                var emailAttr = type.GetCustomAttribute<EmailAddressAttribute>();
                if (emailAttr != null) return "email";
                
                var phoneAttr = type.GetCustomAttribute<PhoneAttribute>();
                if (phoneAttr != null) return "phone";
                
                var urlAttr = type.GetCustomAttribute<UrlAttribute>();
                if (urlAttr != null) return "uri";
            }
            return null;
        }

        private object GetJsonProperties(Type type)
        {
            if (!type.IsClass || type == typeof(string)) return null;

            var properties = type.GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToDictionary(
                    p => p.Name,
                    p => new
                    {
                        type = GetJsonType(p.PropertyType),
                        format = GetJsonFormat(p.PropertyType),
                        description = p.GetCustomAttribute<SummaryAttribute>()?.Summary,
                        required = p.GetCustomAttribute<RequiredAttribute>() != null,
                        @enum = p.PropertyType.IsEnum ? Enum.GetNames(p.PropertyType) : null,
                        minimum = GetMinValue(p.PropertyType),
                        maximum = GetMaxValue(p.PropertyType),
                        minLength = GetMinLength(p.PropertyType),
                        maxLength = GetMaxLength(p.PropertyType),
                        pattern = GetPattern(p.PropertyType)
                    }
                );

            return properties.Count > 0 ? properties : null;
        }

        public async Task<List<EndpointInfoResponse>> GetEndpointsAsync(int connectionId)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                var connection = await apiDbContext.QDBConnections
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Parameters)
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Responses)
                    .FirstOrDefaultAsync(c => c.Id == connectionId);

                if (connection == null)
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

                return connection.Endpoints.Select(e => new EndpointInfoResponse
                {
                    Controller = e.Controller,
                    Action = e.Action,
                    Route = e.Route,
                    HttpMethod = e.HttpMethod,
                    Description = e.Description,
                    Parameters = e.Parameters.Select(p => new Querier.Api.Application.DTOs.Responses.DBConnection.ParameterInfo
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Description = p.Description,
                        IsRequired = p.IsRequired,
                        Source = p.Source,
                        JsonSchema = p.JsonSchema
                    }).ToList(),
                    Responses = e.Responses.Select(r => new ResponseInfo
                    {
                        StatusCode = r.StatusCode,
                        Type = r.Type,
                        Description = r.Description,
                        JsonSchema = r.JsonSchema
                    }).ToList()
                }).ToList();
            }
        }
    }
}