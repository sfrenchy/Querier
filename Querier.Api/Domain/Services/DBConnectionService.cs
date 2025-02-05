using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Database.Generators;
using Querier.Api.Infrastructure.Database.Templates;
using Querier.Api.Infrastructure.Services;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Concurrent;

namespace Querier.Api.Domain.Services
{
    public class DbConnectionService : IDbConnectionService
    {
        private readonly IDbConnectionRepository _dbConnectionRepository;
        private readonly ILogger<DbConnectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;
        private readonly ApplicationPartManager _partManager;
        private readonly EndpointExtractor _endpointExtractor;
        private readonly DatabaseServerDiscovery _serverDiscovery;
        private readonly DatabaseSchemaExtractor _schemaExtractor;
        private readonly IAssemblyManagerService _assemblyManager;
        private readonly DatabaseToCSharpConverter _databaseToCSharpConverter;
        private readonly IEncryptionService _encryptionService;
        private readonly ConcurrentDictionary<string, IDbContextFactory<DbContext>> _contextFactoryCache = new();
        private readonly ConcurrentDictionary<int, IDbContextFactory<DbContext>> _contextFactoryByIdCache = new();
        
        public DbConnectionService(
            IDbConnectionRepository dbConnectionRepository,
            IServiceProvider serviceProvider,
            IServiceCollection services,
            ILogger<DbConnectionService> logger,
            ApplicationPartManager partManager,
            ILogger<DatabaseServerDiscovery> serverDiscoveryLogger,
            ILogger<DatabaseSchemaExtractor> schemaExtractorLogger,
            ILogger<JsonSchemaGenerator> jsonSchemaGeneratorLogger,
            ILogger<EndpointExtractor> endpointExtractorLogger,
            ILogger<DatabaseToCSharpConverter> databaseToCSharpConverterLogger,
            IAssemblyManagerService assemblyManager,
            IEncryptionService encryptionService)
        {
            _logger = logger;
            _dbConnectionRepository = dbConnectionRepository;
            _serviceProvider = serviceProvider;
            _services = services;
            _partManager = partManager;
            _assemblyManager = assemblyManager;
            var jsonSchemaGenerator = new JsonSchemaGeneratorService(jsonSchemaGeneratorLogger);
            _endpointExtractor = new EndpointExtractor(jsonSchemaGenerator, endpointExtractorLogger, serviceProvider, services);
            _serverDiscovery = new DatabaseServerDiscovery(serverDiscoveryLogger);
            _schemaExtractor = new DatabaseSchemaExtractor(schemaExtractorLogger);
            _databaseToCSharpConverter = new DatabaseToCSharpConverter(databaseToCSharpConverterLogger);
            _encryptionService = encryptionService;
        }

        public async Task<DBConnectionCreateResultDto> AddConnectionAsync(DBConnectionCreateDto connection)
        {
            _logger.LogDebug("Adding new database connection: {Name}", connection.Name);
            DBConnectionCreateResultDto result = new DBConnectionCreateResultDto();
            string connectionNamespace = "";
            string contextName = "";
            string procedureDescription = "";

            // Construire la chaîne de connexion à partir des paramètres
            var connectionString = BuildConnectionString(connection.ConnectionType, connection.Parameters);
            try
            {
                

                // Tester la connexion
                switch (connection.ConnectionType)
                {
                    case DbConnectionType.SqlServer:
                        await using (SqlConnection c = new SqlConnection(connectionString))
                        {
                            _logger.LogDebug("Testing SQL Server connection for: {Name}", connection.Name);
                            c.Open();
                            connectionNamespace = $"{connection.Name}.{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = DBConnectionState.Connected;
                            _logger.LogInformation("Successfully connected to SQL Server for: {Name}", connection.Name);
                        }
                        break;
                    case DbConnectionType.MySql:
                        await using (MySqlConnection c = new MySqlConnection(connectionString))
                        {
                            _logger.LogDebug("Testing MySQL connection for: {Name}", connection.Name);
                            c.Open();
                            connectionNamespace = $"{connection.Name}.{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = DBConnectionState.Connected;
                            _logger.LogInformation("Successfully connected to MySQL for: {Name}", connection.Name);
                        }
                        break;
                    case DbConnectionType.PgSql:
                        await using (NpgsqlConnection c = new NpgsqlConnection(connectionString))
                        {
                            _logger.LogDebug("Testing PostgreSQL connection for: {Name}", connection.Name);
                            c.Open();
                            connectionNamespace = $"{connection.Name}.{c.Database}.Api.Models";
                            contextName = $"{c.Database}Context";
                            result.State = DBConnectionState.Connected;
                            _logger.LogInformation("Successfully connected to PostgreSQL for: {Name}", connection.Name);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error for database: {Name}", connection.Name);
                result.State = DBConnectionState.ConnectionError;
                result.Messages.Add(ex.Message);
                return result;
            }

            try
            {
                _logger.LogDebug("Creating scaffolder for database type: {Type}", connection.ConnectionType);
                IReverseEngineerScaffolder scaffolder = connection.ConnectionType switch
                {
                    DbConnectionType.SqlServer => DatabaseScaffolderFactory.CreateMssqlScaffolder(),
                    DbConnectionType.MySql => DatabaseScaffolderFactory.CreateMySQLScaffolder(),
                    DbConnectionType.PgSql => DatabaseScaffolderFactory.CreatePgSQLScaffolder(),
                    _ => throw new NotSupportedException($"Database type {connection.ConnectionType} not supported")
                };

                var dbOpts = new DatabaseModelFactoryOptions();
                var modelOpts = new ModelReverseEngineerOptions();
                var codeGenOpts = new ModelCodeGenerationOptions()
                {
                    RootNamespace = connectionNamespace,
                    ContextName = contextName,
                    ContextNamespace = connectionNamespace,
                    ModelNamespace = connectionNamespace,
                    SuppressConnectionStringWarning = true,
                    SuppressOnConfiguring = true,
                    UseDataAnnotations = true
                };

                var scaffoldedModelSources = scaffolder.ScaffoldModel(connectionString, dbOpts, modelOpts, codeGenOpts);

                var contextFile = connection.ConnectionType switch
                {
                    DbConnectionType.SqlServer => scaffoldedModelSources.ContextFile.Code.Replace(".UseSqlServer", ".UseLazyLoadingProxies().UseSqlServer"),
                    DbConnectionType.MySql => scaffoldedModelSources.ContextFile.Code.Replace(".UseMySql", ".UseLazyLoadingProxies().UseMySql"),
                    DbConnectionType.PgSql => scaffoldedModelSources.ContextFile.Code.Replace(".UseNpgsql", ".UseLazyLoadingProxies().UseNpgsql"),
                    _ => throw new NotSupportedException($"Database type {connection.ConnectionType} not supported")
                };

                var sourceFiles = new List<string> { contextFile };
                sourceFiles.AddRange(scaffoldedModelSources.AdditionalFiles.Select(f => f.Code));

                Dictionary<string, string> srcZipContent = new Dictionary<string, string> { { scaffoldedModelSources.ContextFile.Path, scaffoldedModelSources.ContextFile.Code } };
                foreach (var addFile in scaffoldedModelSources.AdditionalFiles)
                {
                    srcZipContent.Add(addFile.Path, addFile.Code);
                }
                
                List<Entities.QDBConnection.StoredProcedure> storedProcedures = _databaseToCSharpConverter.ToProcedureList(connectionString);
                procedureDescription = System.Text.Json.JsonSerializer.Serialize(storedProcedures);

                var procedureModel = new StoredProcedureTemplateModel
                {
                    NameSpace = connectionNamespace,
                    ContextNameSpace = contextName,
                    ContextRoute = connection.ApiRoute,
                    ProcedureList = ExtractStoredProcedureMetadata(storedProcedures)
                };
                
                // if scaffolding OK => Generate a common DB Schema representation for stored procedure
                if (connection.GenerateProcedureControllersAndServices && connection.ConnectionType == DbConnectionType.SqlServer)
                {
                    GenerateProcedureFiles(procedureModel, srcZipContent, sourceFiles);
                }

                // Extract entity metadata from scaffolded model
                var entityModel = new TemplateModel
                {
                    NameSpace = connectionNamespace,
                    ContextNameSpace = contextName,
                    ContextRoute = connection.ApiRoute,
                    EntityList = ExtractEntityMetadata(scaffoldedModelSources)
                };
                
                GenerateEntityFiles(procedureModel, entityModel, srcZipContent, sourceFiles);

                // Create source zip
                byte[] sourceZipBytes = CreateSourceZip(srcZipContent);

                if (!Directory.Exists("Assemblies"))
                    Directory.CreateDirectory("Assemblies");
                string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
                await File.WriteAllBytesAsync(srcPath, sourceZipBytes);

                // Compile generated sources
                var (assemblyBytes, pdbBytes) = CompileAssembly(connection.Name, sourceFiles);
                if (assemblyBytes == null)
                {
                    result.State = DBConnectionState.CompilationError;
                    return result;
                }

                // Calculate assembly hash
                var hash = ComputeAssemblyHash(assemblyBytes);

                // Load assembly and configure services
                var container = await _assemblyManager.LoadAssemblyAsync(
                    connection.Name,
                    connection.ConnectionType,
                    connectionString,
                    assemblyBytes);

                if (container == null)
                {
                    result.State = DBConnectionState.LoadError;
                    return result;
                }

                using DbContext newDbContext = Utils.GetDbContextFromTypeName(connectionNamespace + "." + contextName, connectionString, connection.ConnectionType);
                
                var endpoints = _endpointExtractor.ExtractFromAssembly(container.GetType().Assembly, newDbContext,connectionString, connection.ConnectionType);

                foreach (var p in connection.Parameters)
                {
                    if (p.IsEncrypted)
                        p.Value = await _encryptionService.EncryptAsync(p.Value);
                }
                
                var newConnection = new DBConnection
                {
                    Name = connection.Name,
                    ConnectionString = "",
                    ConnectionType = connection.ConnectionType,
                    Description = procedureDescription,
                    AssemblyHash = hash,
                    AssemblyDll = assemblyBytes,
                    AssemblyPdb = pdbBytes,
                    AssemblySourceZip = sourceZipBytes,
                    ContextName = connectionNamespace + "." + contextName,
                    ApiRoute = connection.ApiRoute,
                    Endpoints = endpoints,
                    Parameters = connection.Parameters.Select(p => new ConnectionStringParameter()
                    {
                        Key = p.Key,
                        IsEncrypted = p.IsEncrypted,
                        StoredValue = p.Value
                    }).ToList()
                };

                await _dbConnectionRepository.AddDbConnectionAsync(newConnection);

                result.State = DBConnectionState.Available;
                result.Id = newConnection.Id;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scaffolding for database: {Name}", connection.Name);
                throw;
            }
        }

        private void GenerateProcedureFiles(StoredProcedureTemplateModel model, Dictionary<string, string> srcZipContent, List<string> sourceFiles)
        {
            var templates = new[]
            {
                ("DynamicContextExceptions", "Exceptions\\DynamicContextExceptions.cs"),
                ("ProcedureContext", "Context\\ProcedureContext.cs"),
                ("ProcedureController", "Controllers\\ProcedureController.cs"),
                ("ProcedureDto", "DTOs\\ProcedureDtos.cs"),
                ("ProcedureInputDto", "DTOs\\ProcedureInputDtos.cs"),
                ("ProcedureReportRequests", "Reports\\ProcedureReportRequests.cs"),
                ("ProcedureRepository", "Repositories\\ProcedureRepository.cs"),
                ("ProcedureService", "Services\\ProcedureService.cs"),
            };

            foreach (var (templateName, outputPath) in templates)
            {
                _logger.LogDebug($"Processing template {templateName}");
                var template = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", $"{templateName}.st")
                ), '$', '$');

                template.Add("nameSpace", model.NameSpace);
                template.Add("contextNameSpace", model.ContextNameSpace);
                template.Add("procedureList", templateName == "ProcedureDto" 
                    ? model.ProcedureList.Where(s => s.HasOutput).ToList() 
                    : model.ProcedureList);

                if (templateName == "ProcedureController")
                    template.Add("contextRoute", model.ContextRoute);

                string content = template.Render();
                srcZipContent.Add(outputPath, content);
                sourceFiles.Add(content);
            }
        }

        private void GenerateEntityFiles(StoredProcedureTemplateModel procedureModel, TemplateModel model, Dictionary<string, string> srcZipContent, List<string> sourceFiles)
        {
            var templates = new[]
            {
                ("DynamicServiceContainer", "Services\\DynamicServiceContainer.cs"),
                ("EntityController","Controllers\\EntityController.cs"),
                ("EntityDto","Entities\\EntityDto.cs"),
                ("EntityRepository","Repositories\\EntityRepository.cs"),
                ("EntityService","Services\\EntityService.cs"),
            };

            foreach (var (templateName, outputPath) in templates)
            {
                _logger.LogDebug($"Processing template {templateName}");
                var template = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", $"{templateName}.st")
                ), '$', '$');

                template.Add("procedureList", templateName == "ProcedureDto" 
                    ? procedureModel.ProcedureList.Where(s => s.HasOutput).ToList() 
                    : procedureModel.ProcedureList);
                template.Add("nameSpace", model.NameSpace);
                template.Add("contextNameSpace", model.ContextNameSpace);
                template.Add("entityList", model.EntityList);

                if (templateName == "EntityController")
                    template.Add("contextRoute", model.ContextRoute);

                string content = template.Render();
                srcZipContent.Add(outputPath, content);
                sourceFiles.Add(content);
            }
        }

        private byte[] CreateSourceZip(Dictionary<string, string> srcZipContent)
        {
            using var sourceStream = new MemoryStream();
            using (var archive = new ZipArchive(sourceStream, ZipArchiveMode.Create))
            {
                foreach (var item in srcZipContent)
                {
                    var entry = archive.CreateEntry(item.Key);
                    using var entryStream = entry.Open();
                    using var streamWriter = new BinaryWriter(entryStream);
                    var bytes = Encoding.UTF8.GetBytes(item.Value);
                    streamWriter.Write(bytes, 0, bytes.Length);
                }
            }
            return sourceStream.ToArray();
        }

        private (byte[] assemblyBytes, byte[] pdbBytes) CompileAssembly(string contextName, List<string> sourceFiles)
        {
            var peStream = new MemoryStream();
            var pdbStream = new MemoryStream();

            var compilation = GenerateCode(contextName, sourceFiles);
            var emitResult = compilation.Emit(peStream, pdbStream);

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
                return (null, null);
            }

            peStream.Seek(0, SeekOrigin.Begin);
            pdbStream.Seek(0, SeekOrigin.Begin);
            return (peStream.ToArray(), pdbStream.ToArray());
        }

        private CSharpCompilation GenerateCode(string contextName, List<string> sourceFiles)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
            var parsedSyntaxTrees = sourceFiles.Select(f => SyntaxFactory.ParseSyntaxTree(f, options));

            return CSharpCompilation.Create($"{contextName}_DataContext.dll",
                parsedSyntaxTrees,
                references: GetCompilationReferences(),
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug));
        }

        private string ComputeAssemblyHash(byte[] assemblyBytes)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(assemblyBytes);
            return Convert.ToBase64String(hash);
        }

        private List<MetadataReference> GetCompilationReferences()
        {
            var refs = new List<MetadataReference>();

            // Reference all assemblies referenced by this program 
            var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            refs.AddRange(referencedAssemblies.Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            // Add the missing ones needed to compile the assembly
            var additionalAssemblies = new[]
            {
                typeof(object),
                typeof(DbConnection),
                typeof(System.Linq.Expressions.Expression),
                typeof(System.ComponentModel.DisplayNameAttribute),
                typeof(System.Threading.CancellationToken),
                typeof(Task),
                typeof(List<>),
                typeof(Infrastructure.Database.Parameters.OutputParameter<>),
                typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache),
                typeof(Enumerable),
                typeof(MemoryStream),
                typeof(StreamReader),
                typeof(System.Linq.Dynamic.Core.DynamicClassFactory),
                typeof(MySqlConnector.MySqlConnection)
            };

            refs.AddRange(additionalAssemblies.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)));
            refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));

            return refs;
        }

        public async Task DeleteDbConnectionAsync(int dbConnectionId)
        {
            try
            {
                _logger.LogDebug("Deleting database connection with ID: {Id}", dbConnectionId);
                var connection = await _dbConnectionRepository.FindByIdAsync(dbConnectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", dbConnectionId);
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");
                }

                // Décharger l'assembly si elle est chargée
                if (_assemblyManager.IsAssemblyLoaded(connection.Name))
                {
                    await _assemblyManager.UnloadAssemblyAsync(connection.Name);
                }

                await _dbConnectionRepository.DeleteDbConnectionAsync(dbConnectionId);
                _logger.LogInformation("Successfully deleted database connection with ID: {Id}", dbConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting database connection with ID: {Id}", dbConnectionId);
                throw;
            }
        }

        public async Task<List<DBConnectionDto>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all database connections");
                var connections = await _dbConnectionRepository.GetAllDbConnectionsAsync();
                _logger.LogInformation("Successfully retrieved {Count} database connections", connections.Count);
                return connections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all database connections");
                throw;
            }
        }

        public async Task<DBConnectionDatabaseSchemaDto> GetDatabaseSchemaAsync(int connectionId)
        {
            try
            {
                _logger.LogDebug("Retrieving database schema for connection ID: {Id}", connectionId);
                var connection = await _dbConnectionRepository.FindByIdAsync(connectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");
                }

                var connectionString = BuildConnectionString(connection.ConnectionType, connection.Parameters.Select(p => new ConnectionStringParameterCreateDto
                {
                    Key = p.Key,
                    Value = p.StoredValue,
                    IsEncrypted = p.IsEncrypted
                }));
                var schema = await _schemaExtractor.ExtractSchema(connection.ConnectionType, connectionString);
                _logger.LogInformation("Successfully retrieved schema for connection ID: {Id}", connectionId);
                return schema;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database schema for connection ID: {Id}", connectionId);
                throw;
            }
        }

        public async Task<List<DBConnectionDatabaseServerInfoDto>> EnumerateServersAsync(string databaseType)
        {
            try
            {
                _logger.LogDebug("Enumerating servers for database type: {Type}", databaseType);
                var servers = await _serverDiscovery.EnumerateServersAsync(databaseType);
                _logger.LogInformation("Successfully enumerated {Count} servers for type: {Type}", servers.Count, databaseType);
                return servers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating servers for database type: {Type}", databaseType);
                throw;
            }
        }

        public async Task<SourceDownload> GetConnectionSourcesAsync(int connectionId)
        {
            try
            {
                _logger.LogDebug("Retrieving sources for connection ID: {Id}", connectionId);
                var connection = await _dbConnectionRepository.FindByIdAsync(connectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");
                }

                if (connection.AssemblySourceZip == null)
                {
                    _logger.LogWarning("No source code available for connection ID: {Id}", connectionId);
                    throw new InvalidOperationException($"No source code available for connection {connectionId}");
                }

                _logger.LogInformation("Successfully retrieved sources for connection ID: {Id}", connectionId);
                return new SourceDownload
                {
                    Content = connection.AssemblySourceZip,
                    FileName = $"{connection.Name}.DynamicContext.Sources.zip"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sources for connection ID: {Id}", connectionId);
                throw;
            }
        }

        public async Task<List<DBConnectionEndpointInfoDto>> GetEndpointsAsync(int connectionId, string? targetTable, string? controller, string? action)
        {
            try
            {
                _logger.LogDebug("Retrieving endpoints for connection ID: {Id}", connectionId);
                var endpoints = await _dbConnectionRepository.FindEndpointsAsync(connectionId, controller, targetTable, action);
                
                var endpointDtos = endpoints.Select(e => new DBConnectionEndpointInfoDto
                {
                    Controller = e.Controller,
                    Action = e.Action,
                    Route = e.Route,
                    HttpMethod = e.HttpMethod,
                    Description = e.Description,
                    Parameters = e.Parameters.Select(p => new DBConnectionEndpointRequestInfoDto
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Description = p.Description,
                        IsRequired = p.IsRequired,
                        Source = p.Source,
                        JsonSchema = p.JsonSchema
                    }).ToList(),
                    Responses = e.Responses.Select(r => new DBConnectionEndpointResponseInfoDto
                    {
                        StatusCode = r.StatusCode,
                        Type = r.Type,
                        Description = r.Description,
                        JsonSchema = r.JsonSchema
                    }).ToList()
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} endpoints for connection ID: {Id}", endpointDtos.Count, connectionId);
                return endpointDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving endpoints for connection ID: {Id}", connectionId);
                throw;
            }
        }

        public async Task<List<DBConnectionControllerInfoDto>> GetControllersAsync(int connectionId)
        {
            try
            {
                _logger.LogDebug("Retrieving controllers for connection ID: {Id}", connectionId);
                var connection = await _dbConnectionRepository.FindByIdWithControllersAsync(connectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");
                }

                var tableControllers = connection.Endpoints
                    .Where(e => !string.IsNullOrEmpty(e.TargetTable))
                    .GroupBy(e => e.Controller)
                    .Select(g => new DBConnectionControllerInfoDto
                    {
                        Name = g.Key.Replace("Controller",""),
                        Route = g.First().Route.Replace("Controller",""),
                        ResponseEntityJsonSchema = g.FirstOrDefault(e => e.HttpMethod == "GET")
                            ?.EntitySubjectJsonSchema,
                        ParameterJsonSchema = ""
                    })
                    .ToList();
                var procedureControllers = connection.Endpoints
                    .Where(e => string.IsNullOrEmpty(e.TargetTable))
                    .GroupBy(e => e.Controller)
                    .Select(g => new DBConnectionControllerInfoDto
                    {
                        Name = g.Key.Replace("Controller",""),
                        Route = g.First().Route.Replace("Controller",""),
                        ResponseEntityJsonSchema = g.FirstOrDefault(e => e.HttpMethod == "POST")
                            ?.EntitySubjectJsonSchema,
                        ParameterJsonSchema = g.FirstOrDefault(e => e.HttpMethod == "POST")
                            ?.Parameters.FirstOrDefault()?.JsonSchema
                    })
                    .ToList();

                List<DBConnectionControllerInfoDto> controllerDtos = new List<DBConnectionControllerInfoDto>();
                controllerDtos.AddRange(tableControllers);
                controllerDtos.AddRange(procedureControllers);
                
                _logger.LogInformation("Successfully retrieved {Count} controllers for connection ID: {Id}", controllerDtos.Count, connectionId);
                
                return controllerDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving controllers for connection ID: {Id}", connectionId);
                throw;
            }
        }

        public async Task<DBConnectionDto> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving database connection with ID: {Id}", id);
                var connection = await _dbConnectionRepository.FindByIdAsync(id);

                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved database connection: {Id}", id);
                return DBConnectionDto.FromEntity(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database connection with ID: {Id}", id);
                throw;
            }
        }

        public async Task<DbContext> GetDbContextByContextTypeFullNameAsync(string contextTypeFullName)
        {
            DBConnection connection = await _dbConnectionRepository.FindByContextNameAsync(contextTypeFullName);
            return await GetDbContextByIdAsync(connection.Id);
        }
        public async Task<DbContext> GetDbContextByIdAsync(int id)
        {
            try
            {
                //_logger.LogDebug($"Get DbContext for DBConnection {id}");
                var connection = await _dbConnectionRepository.FindByIdAsync(id); 
                var contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == connection.ContextName)
                    .ToList();
                if (!contextTypes.Any())
                {
                    _logger.LogError("No DbContext found with type name: {ContextTypeName}", connection.ContextName);
                    throw new InvalidOperationException($"No DbContext found with type name {connection.ContextName}");
                }

                var contextType = contextTypes.First();
                var scope = ServiceActivator.GetScope();
                
                // Try to get from DI first
                _logger.LogTrace("Attempting to get context from DI container");
                if (scope.ServiceProvider.GetService(contextType) is DbContext context)
                {
                    _logger.LogDebug("Successfully retrieved context from DI container");
                    return context;
                }
                string connectionString = string.Join(';', connection.Parameters.Where(p => !p.IsEncrypted).Select(p => p.Key + "=" + p.StoredValue));
                foreach (var cryptedParameter in connection.Parameters.Where(p => p.IsEncrypted))
                {
                    string uncryptedParameterValue = _encryptionService.DecryptAsync(cryptedParameter.StoredValue).GetAwaiter().GetResult();
                    connectionString += $";{cryptedParameter.Key}={uncryptedParameterValue}";
                }

                // Create options with the correct connection string
                _logger.LogTrace("Creating context options with connection string");
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);

                switch (connection.ConnectionType)
                {
                    case DbConnectionType.SqlServer:
                        _logger.LogDebug("Configuring SQL Server connection");
                        if (optionsBuilder != null) optionsBuilder.UseSqlServer(connectionString);
                        break;
                    case DbConnectionType.MySql:
                        _logger.LogDebug("Configuring MySQL connection");
                        if (optionsBuilder != null)
                            optionsBuilder.UseMySql(connectionString,
                                ServerVersion.AutoDetect(connectionString));
                        break;
                    case DbConnectionType.PgSql:
                        _logger.LogDebug("Configuring PostgresSQL connection");
                        if (optionsBuilder != null) optionsBuilder.UseNpgsql(connectionString);
                        break;
                    default:
                        _logger.LogError("Unsupported database type: {ConnectionType}", connection.ConnectionType);
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
                }

                _logger.LogDebug("Creating new instance of context type: {ContextType}", contextType.Name);
                if (optionsBuilder != null)
                    return (DbContext)Activator.CreateInstance(contextType, optionsBuilder.Options);
                throw new Exception($"Unable to create optionBuilder for connection {id}");
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not NotSupportedException)
            {
                _logger.LogError(ex, $"Error creating DbContext for type connection: {id}");
                throw;
            }
        }

        private List<TemplateEntityMetadata> ExtractEntityMetadata(ScaffoldedModel scaffoldedModel)
        {
            var entityFiles = scaffoldedModel.AdditionalFiles.Where(f => !f.Path.EndsWith("Context.cs"));
            var pluralizer = new Bricelam.EntityFrameworkCore.Design.Pluralizer();

            // Première passe : extraire toutes les entités et leurs propriétés
            var entityMap = new Dictionary<string, TemplateEntityMetadata>();
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
                    Properties = new List<TemplateProperty>(),
                    ForeignKeys = new List<TemplateForeignKey>()
                };
                List<string> keys = new List<string>();
                List<string> foreignKeys = new List<string>();

                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var keyAttributes = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == "Key")
                        .Select(a => property.Identifier.Text)
                        .ToList();
                    
                    keys.AddRange(keyAttributes);
                }
                
                // Extraire les clés étrangères et leurs références
                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var foreignKeyAttributes = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == "ForeignKey")
                        .SelectMany(a => a.ArgumentList.Arguments)
                        .Select(a => a.Expression.ToString().Replace("\"", "")).ToList();
                    
                    foreach (var foreignKey in foreignKeyAttributes)
                        if (!keys.Contains(foreignKey))
                            foreignKeys.Add(foreignKey);

                    // Chercher les attributs de navigation
                    var navigationProperty = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => /*a.Name.ToString() == "InverseProperty" ||*/ a.Name.ToString() == "ForeignKey");

                    if (navigationProperty != null)
                    {
                        var inverseProperty = property.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .FirstOrDefault(a => a.Name.ToString() == "InverseProperty");
                        var foreignKeyIsCurrentPrimaryKey = navigationProperty.ArgumentList.Arguments.Any(a => keys.Contains(a.Expression.ToString().Replace("\"", "")));
                        if (inverseProperty != null && !foreignKeyIsCurrentPrimaryKey)
                        {
                            bool inverseTargetIsCurrent = inverseProperty.ArgumentList.Arguments.Any(a =>
                                a.Expression.ToString().Replace("\"", "") == pluralizer.Pluralize(entityName));
                            if (inverseTargetIsCurrent)
                            {
                                var referencedType = property.Type.ToString().TrimEnd('?');
                                if (referencedType != entityName) // Éviter les auto-références
                                {
                                    var isCollection = referencedType.StartsWith("ICollection<") ||
                                                       referencedType.StartsWith("List<");
                                    var actualType = isCollection
                                        ? referencedType.Substring(referencedType.IndexOf('<') + 1).TrimEnd('>')
                                        : referencedType;

                                    var fk = new TemplateForeignKey
                                    {
                                        Name = foreignKeys.LastOrDefault() ?? property.Identifier.Text,
                                        NamePlural = pluralizer.Pluralize(foreignKeys.LastOrDefault() ?? property.Identifier.Text),
                                        ReferencedEntityPlural = pluralizer.Pluralize(actualType),
                                        ReferencedEntitySingular = actualType,
                                        ReferencedColumn =
                                            "Id", // Par défaut, peut être mis à jour dans la deuxième passe
                                        IsCollection = isCollection
                                    };
                                    if (!entity.ForeignKeys.Any(f => f.Name == fk.Name))
                                        entity.ForeignKeys.Add(fk);
                                }
                            }
                        }
                    }
                }
                    
                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var attributes = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Select(a => a.Name.ToString())
                        .ToList();

                    var isKey = keys.Contains(property.Identifier.Text);
                    var isForeignKey = foreignKeys.Contains(property.Identifier.Text);
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
                        IsAutoGenerated = isAutoGenerated,
                        IsForeignKey = isForeignKey
                    };

                    entity.Properties.Add(prop);

                    if (isKey)
                    {
                        entity.KeyType = property.Type.ToString();
                        entity.KeyName = property.Identifier.Text;
                    }
                }

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
                        var firstProperty = entity.Properties.First();
                        firstProperty.IsKey = true;
                        entity.KeyType = firstProperty.CSType;
                        entity.KeyName = firstProperty.Name;
                    }
                }

                entityMap[entityName] = entity;
            }

            // Deuxième passe : mettre à jour les colonnes référencées des clés étrangères
            foreach (var entity in entityMap.Values)
            {
                foreach (var fk in entity.ForeignKeys)
                {
                    if (entityMap.TryGetValue(fk.ReferencedEntitySingular, out var referencedEntity))
                    {
                        fk.ReferencedColumn = referencedEntity.KeyName;
                    }
                }
            }

            return entityMap.Values.ToList();
        }

        private List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(List<Entities.QDBConnection.StoredProcedure> procedures)
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

        public async Task<DBConnectionQueryAnalysisDto> GetQueryObjectsAsync(int connectionId, string objectType)
        {
            try
            {
                _logger.LogDebug("Analyzing query objects for connection ID: {Id}, type: {Type}", connectionId, objectType);
                var connection = await _dbConnectionRepository.FindByIdAsync(connectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", connectionId);
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");
                }

                var schema = await _schemaExtractor.ExtractSchema(connection.ConnectionType, connection.ConnectionString);
                var response = new DBConnectionQueryAnalysisDto();

                switch (objectType.ToLowerInvariant())
                {
                    case "table":
                        response.Tables = schema.Tables.Select(t => $"{t.Schema}.{t.Name}").ToList();
                        break;
                    case "view":
                        response.Views = schema.Views.Select(v => $"{v.Schema}.{v.Name}").ToList();
                        break;
                    case "procedure":
                        response.StoredProcedures = schema.StoredProcedures.Select(p => $"{p.Schema}.{p.Name}").ToList();
                        break;
                    case "function":
                        response.UserFunctions = schema.UserFunctions.Select(f => $"{f.Schema}.{f.Name}").ToList();
                        break;
                    default:
                        _logger.LogWarning("Invalid object type requested: {Type}", objectType);
                        throw new ArgumentException($"Invalid object type: {objectType}");
                }

                _logger.LogInformation("Successfully analyzed query objects for connection ID: {Id}, type: {Type}", connectionId, objectType);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query objects for connection ID: {Id}, type: {Type}", connectionId, objectType);
                throw;
            }
        }

        private string BuildConnectionString(DbConnectionType connectionType, IEnumerable<ConnectionStringParameterCreateDto> parameters)
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder();
            
            foreach (var param in parameters.Where(p => !string.IsNullOrEmpty(p.Value)))
            {
                builder[param.Key] = param.Value;
            }

            return builder.ConnectionString;
        }

        public async Task<IDbContextFactory<DbContext>> GetDbContextFactoryByContextTypeFullNameAsync(string contextTypeFullName)
        {
            return await _contextFactoryCache.GetOrAddAsync(contextTypeFullName, async key =>
            {
                _logger.LogDebug("Cache miss for context type: {ContextTypeName}, creating new factory", key);
                DBConnection connection = await _dbConnectionRepository.FindByContextNameAsync(key);
                return await CreateDbContextFactoryAsync(connection);
            });
        }
        
        public async Task<IDbContextFactory<DbContext>> GetDbContextFactoryByIdAsync(int id)
        {
            return await _contextFactoryByIdCache.GetOrAddAsync(id, async key =>
            {
                _logger.LogDebug("Cache miss for connection ID: {Id}, creating new factory", key);
                var connection = await _dbConnectionRepository.FindByIdAsync(key);
                return await CreateDbContextFactoryAsync(connection);
            });
        }

        private async Task<IDbContextFactory<DbContext>> CreateDbContextFactoryAsync(DBConnection connection)
        {
            try
            {
                _logger.LogDebug("Creating DbContextFactory for connection: {Name}", connection.Name);
                
                var contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == connection.ContextName)
                    .ToList();

                if (!contextTypes.Any())
                {
                    _logger.LogError("No DbContext found with type name: {ContextTypeName}", connection.ContextName);
                    throw new InvalidOperationException($"No DbContext found with type name {connection.ContextName}");
                }

                var contextType = contextTypes.First();
                var scope = ServiceActivator.GetScope();

                // Construire la chaîne de connexion
                string connectionString = string.Join(';', connection.Parameters.Where(p => !p.IsEncrypted).Select(p => p.Key + "=" + p.StoredValue));
                foreach (var cryptedParameter in connection.Parameters.Where(p => p.IsEncrypted))
                {
                    string uncryptedParameterValue = await _encryptionService.DecryptAsync(cryptedParameter.StoredValue);
                    connectionString += $";{cryptedParameter.Key}={uncryptedParameterValue}";
                }

                // Créer le type générique de DbContextOptionsBuilder
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);

                // Configurer les options selon le type de base de données
                switch (connection.ConnectionType)
                {
                    case DbConnectionType.SqlServer:
                        _logger.LogDebug("Configuring SQL Server connection factory");
                        optionsBuilder.UseSqlServer(connectionString, 
                            options => options.EnableRetryOnFailure());
                        break;

                    case DbConnectionType.MySql:
                        _logger.LogDebug("Configuring MySQL connection factory");
                        optionsBuilder.UseMySql(connectionString,
                            ServerVersion.AutoDetect(connectionString),
                            options => options.EnableRetryOnFailure());
                        break;

                    case DbConnectionType.PgSql:
                        _logger.LogDebug("Configuring PostgreSQL connection factory");
                        optionsBuilder.UseNpgsql(connectionString,
                            options => options.EnableRetryOnFailure());
                        break;

                    default:
                        _logger.LogError("Unsupported database type: {ConnectionType}", connection.ConnectionType);
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
                }

                // Créer le type générique de DbContextFactory
                var factoryType = typeof(PooledDbContextFactory<>).MakeGenericType(contextType);
                
                // Créer la factory avec le type spécifique
                var factory = Activator.CreateInstance(
                    factoryType, 
                    optionsBuilder.Options,
                    1024);

                // Créer un wrapper qui convertit la factory spécifique en IDbContextFactory<DbContext>
                return new DbContextFactoryWrapper(factory, contextType);
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not NotSupportedException)
            {
                _logger.LogError(ex, "Error creating DbContextFactory for connection: {Name}", connection.Name);
                throw;
            }
        }

        // Classe wrapper pour gérer la conversion de type
        private class DbContextFactoryWrapper : IDbContextFactory<DbContext>
        {
            private readonly object _innerFactory;
            private readonly Type _contextType;
            private readonly MethodInfo _createDbContextMethod;

            public DbContextFactoryWrapper(object factory, Type contextType)
            {
                _innerFactory = factory;
                _contextType = contextType;
                
                // Obtenir la méthode CreateDbContext de la factory typée
                var factoryType = typeof(IDbContextFactory<>).MakeGenericType(_contextType);
                _createDbContextMethod = factoryType.GetMethod("CreateDbContext");
            }

            public DbContext CreateDbContext()
            {
                // Invoquer la méthode CreateDbContext sur la factory typée
                return (DbContext)_createDbContextMethod.Invoke(_innerFactory, null);
            }
        }
    }

    // Extension method pour ConcurrentDictionary avec support async
    public static class ConcurrentDictionaryExtensions
    {
        public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, Task<TValue>> valueFactory)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }

            value = await valueFactory(key);
            return dictionary.GetOrAdd(key, value);
        }
    }
}