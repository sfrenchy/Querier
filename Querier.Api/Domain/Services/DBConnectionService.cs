using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;

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
        private readonly IEncryptionService _encryptionService;
        private readonly ConcurrentDictionary<string, IDbContextFactory<DbContext>> _contextFactoryCache = new();
        private readonly ConcurrentDictionary<int, IDbContextFactory<DbContext>> _contextFactoryByIdCache = new();
        private readonly ConcurrentDictionary<int, IDbContextFactory<DbContext>> _readOnlyContextFactoryByIdCache = new();
        private readonly IProgressService _progressService;
        private readonly IRoslynCompilerService _roslynCompilerService;
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
            IAssemblyManagerService assemblyManager,
            IEncryptionService encryptionService,
            IRoslynCompilerService roslynCompilerService,
            IProgressService progressService)
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
            _encryptionService = encryptionService;
            _progressService = progressService;
            _roslynCompilerService = roslynCompilerService;
        }

        public async Task<DBConnectionCreateResultDto> AddConnectionAsync(DBConnectionCreateDto connection)
        {
            try
            {
                await _progressService.StartOperation(connection.OperationId, ProgressStatus.Starting);
                _logger.LogDebug("Adding new database connection: {Name}", connection.Name);
                DBConnectionCreateResultDto result = new DBConnectionCreateResultDto();
                
                string rootNamespace = connection.Name;
                string contextName = $"{connection.Name}DbContext";
                string contextNamespace = $"{connection.Name}.Contexts";
                string modelNamespace = $"{connection.Name}.Entities";
                
                //string connectionNamespace = "";
                string procedureDescription = "";

                // Validation step (10%)
                await _progressService.ReportProgress(connection.OperationId, 10, ProgressStatus.ValidatingConnection);
                var connectionString = BuildConnectionString(connection.ConnectionType, connection.Parameters);
                
                // Schema retrieval (30%)
                await _progressService.ReportProgress(connection.OperationId, 30, ProgressStatus.RetrievingSchema);
                try
                {
                    SourceCodeFromDatabaseService sourceCodeService = new SourceCodeFromDatabaseService(connection.ConnectionType, connectionString, connection.Name, connection.ApiRoute, _logger);
                    await sourceCodeService.GenerateDbConnectionSourcesAsync();
                    // Create source zip
                    byte[] sourceZipBytes = await sourceCodeService.CreateSourceZipAsync();

                    if (!Directory.Exists("Assemblies"))
                        Directory.CreateDirectory("Assemblies");
                    string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
                    await File.WriteAllBytesAsync(srcPath, sourceZipBytes);

                    // Compile generated sources
                    await _progressService.ReportProgress(connection.OperationId, 80, ProgressStatus.Compiling);
                    var compilationResult = _roslynCompilerService.CompileAssembly(connection.Name, sourceCodeService.GetGeneratedSyntaxTrees());
                    if (compilationResult.AssemblyBytes == null)
                    {
                        await _progressService.FailOperation(connection.OperationId, ProgressStatus.Failed);
                        result.State = DBConnectionState.CompilationError;
                        return result;
                    }
                    await _progressService.ReportProgress(connection.OperationId, 90, ProgressStatus.CompilationSucceeded);

                    // Calculate assembly hash
                    var hash = ComputeAssemblyHash(compilationResult.AssemblyBytes);

                    // Load assembly and configure services
                    await _progressService.ReportProgress(connection.OperationId, 95, ProgressStatus.LoadingAssembly);
                    var container = await _assemblyManager.LoadDbConnectionAssemblyAsync(
                        connection.Name,
                        connection.ConnectionType,
                        connectionString,
                        compilationResult.AssemblyBytes);

                    if (container == null)
                    {
                        await _progressService.FailOperation(connection.OperationId, ProgressStatus.Failed);
                        result.State = DBConnectionState.LoadError;
                        return result;
                    }

                    using DbContext newDbContext = Utils.GetDbContextFromTypeName($"{contextNamespace}.{contextName}", connectionString, connection.ConnectionType);
                    
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
                        AssemblyDll = compilationResult.AssemblyBytes,
                        AssemblyPdb = compilationResult.PdbBytes,
                        AssemblySourceZip = sourceZipBytes,
                        ContextName = $"{contextNamespace}.{contextName}",
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

                    await _progressService.CompleteOperation(connection.OperationId, ProgressStatus.Completed);
                    result.State = DBConnectionState.Available;
                    result.Id = newConnection.Id;
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scaffolding for database: {Name}", connection.Name);
                    await _progressService.FailOperation(connection.OperationId, ProgressStatus.Failed);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create connection {ConnectionName}", connection.Name);
                await _progressService.FailOperation(connection.OperationId, ProgressStatus.Failed);
                throw;
            }
        }

        private void GenerateProcedureFiles(TemplateModel templateModel, Dictionary<string, string> srcZipContent, Dictionary<string, string> sourceFiles)
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

                template.Add("rootNamespace", templateModel.RootNamespace);
                template.Add("contextNamespace", templateModel.ContextNamespace);
                template.Add("contextName", templateModel.ContextName);
                template.Add("modelNamespace", templateModel.ModelNamespace);
                
                template.Add("procedureList", templateName == "ProcedureDto" 
                    ? templateModel.ProcedureList.Where(s => s.HasOutput).ToList() 
                    : templateModel.ProcedureList);

                if (templateName == "ProcedureController")
                    template.Add("contextRoute", templateModel.ContextRoute);

                string content = template.Render();
                srcZipContent.Add(outputPath, content);
                sourceFiles.Add(outputPath, content);
            }
        }

        private void GenerateEntityFiles(TemplateModel templateModel, Dictionary<string, string> srcZipContent, Dictionary<string, string> sourceFiles)
        {
            var templates = new[]
            {
                ("DynamicServiceContainer", "Services\\DynamicServiceContainer.cs"),
                ("ReadOnlyDbContext", "Context\\ReadOnlyDbContext.cs"),
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

                template.Add("rootNamespace", templateModel.RootNamespace);
                template.Add("contextNamespace", templateModel.ContextNamespace);
                template.Add("contextName", templateModel.ContextName);
                template.Add("modelNamespace", templateModel.ModelNamespace);
                template.Add("procedureList", templateModel.ProcedureList);
                template.Add("entityList", templateModel.EntityList);

                if (templateName == "EntityController")
                    template.Add("contextRoute", templateModel.ContextRoute);

                string content = template.Render();
                srcZipContent.Add(outputPath, content);
                sourceFiles.Add(outputPath, content);
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
        
        private string ComputeAssemblyHash(byte[] assemblyBytes)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(assemblyBytes);
            return Convert.ToBase64String(hash);
        }

        public async Task DeleteDbConnectionAsync(int dbConnectionId)
        {
            try
            {
                _logger.LogDebug("Deleting database connection with ID: {Id}", dbConnectionId);
                
                // Exécuter les opérations en parallèle quand c'est possible
                var connection = await _dbConnectionRepository.FindByIdAsync(dbConnectionId);
                
                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", dbConnectionId);
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");
                }

                // Décharger l'assembly en parallèle avec la suppression en base
                var unloadTask = _assemblyManager.IsAssemblyLoaded(connection.Name) 
                    ? _assemblyManager.UnloadAssemblyAsync(connection.Name)
                    : Task.CompletedTask;

                var deleteTask = _dbConnectionRepository.DeleteDbConnectionAsync(dbConnectionId);

                // Attendre que les deux opérations soient terminées
                await Task.WhenAll(unloadTask, deleteTask);

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

        private List<TemplateEntityMetadata> ExtractEntityMetadata(ScaffoldedModel scaffoldedModel)
        {
            var contextFile = scaffoldedModel.ContextFile;
            var entityFiles = scaffoldedModel.AdditionalFiles.Where(f => !f.Path.EndsWith("Context.cs"));
            var pluralizer = new Bricelam.EntityFrameworkCore.Design.Pluralizer();
            var viewEntities = new HashSet<string>();

            // Identifier les vues depuis le fichier du contexte
            var contextSyntaxTree = CSharpSyntaxTree.ParseText(contextFile.Code).GetRoot();
            var onModelCreatingNode = contextSyntaxTree.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            if (onModelCreatingNode != null)
            {
                string currentEntity = "";
                foreach (var invocation in onModelCreatingNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccessEntity &&
                        memberAccessEntity.Name.Identifier.Text == "Entity")
                    {
                        if (memberAccessEntity.Name is GenericNameSyntax genericName)
                        {
                            currentEntity = genericName.TypeArgumentList.Arguments.First().ToString();
                        }
                    }
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name.Identifier.Text == "ToView")
                    {
                        viewEntities.Add(currentEntity);
                    }
                }
            }

            // Extraction des entités
            var entityMap = new Dictionary<string, TemplateEntityMetadata>();

            foreach (var entityFile in entityFiles)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(entityFile.Code);
                var root = syntaxTree.GetRoot();
                var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclaration == null) continue;

                var entityName = classDeclaration.Identifier.Text;
                var entity = new TemplateEntityMetadata
                {
                    Name = entityName,
                    PluralName = pluralizer.Pluralize(entityName),
                    Properties = new List<TemplateProperty>(),
                    ForeignKeys = new List<TemplateForeignKey>(),
                    IsViewEntity = viewEntities.Contains(entityName),
                    //KeyNames = new List<string>(),
                   // KeyTypes = new List<string>()
                };

                var keys = new HashSet<string>();
                var foreignKeys = new HashSet<string>();

                // Détection de [PrimaryKey] sur la classe (clé composite)
                var primaryKeyAttribute = classDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(a => a.Name.ToString() == "PrimaryKey");

                if (primaryKeyAttribute?.ArgumentList != null)
                {
                    foreach (var arg in primaryKeyAttribute.ArgumentList.Arguments)
                    {
                        keys.Add(arg.Expression.ToString().Replace("\"", ""));
                    }
                }

                // Détection des [Key] sur les propriétés (clés individuelles)
                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    if (property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(a => a.Name.ToString() == "Key"))
                    {
                        keys.Add(property.Identifier.Text);
                    }
                }

                // Détection des clés étrangères et des propriétés de navigation
                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var foreignKeyAttributes = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == "ForeignKey")
                        .SelectMany(a => a.ArgumentList.Arguments)
                        .Select(a => a.Expression.ToString().Replace("\"", ""))
                        .ToList();

                    foreach (var fk in foreignKeyAttributes)
                    {
                        if (!keys.Contains(fk))
                        {
                            foreignKeys.Add(fk);
                        }
                    }

                    // Gestion des propriétés de navigation avec [InverseProperty]
                    var navigationProperty = property.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "ForeignKey");

                    if (navigationProperty != null)
                    {
                        var inverseProperty = property.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .FirstOrDefault(a => a.Name.ToString() == "InverseProperty");

                        bool foreignKeyIsCurrentPrimaryKey = navigationProperty.ArgumentList.Arguments
                            .Any(a => keys.Contains(a.Expression.ToString().Replace("\"", "")));

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
                                        ReferencedColumn = "Id",
                                        IsCollection = isCollection
                                    };

                                    if (!entity.ForeignKeys.Any(f => f.Name == fk.Name))
                                        entity.ForeignKeys.Add(fk);
                                }
                            }
                        }
                    }
                }

                // Extraction des propriétés
                foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var attributes = property.AttributeLists.SelectMany(al => al.Attributes).Select(a => a.Name.ToString()).ToList();
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
                        //entity.KeyNames.Add(prop.Name);
                        //entity.KeyTypes.Add(prop.CSType);
                    }
                }

                // Définir une clé primaire si aucune n'a été trouvée
                //if (!entity.KeyNames.Any())
                //{
                //    var idProperty = entity.Properties.FirstOrDefault(p => 
                //        p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                //        p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

                //    if (idProperty != null)
                //    {
                //        idProperty.IsKey = true;
                //        entity.KeyNames.Add(idProperty.Name);
                //        entity.KeyTypes.Add(idProperty.CSType);
                //    }
                //}

                entityMap[entityName] = entity;
            }

            // 🔹 Deuxième passe : mise à jour des clés étrangères avec la bonne colonne référencée
            foreach (var entity in entityMap.Values)
            {
                foreach (var fk in entity.ForeignKeys)
                {
                    if (entityMap.TryGetValue(fk.ReferencedEntitySingular, out var referencedEntity))
                    {
                       // fk.ReferencedColumn = referencedEntity.KeyNames.FirstOrDefault() ?? "Id";
                    }
                }
            }

            return entityMap.Values.ToList();
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
                if (!param.IsEncrypted)
                    builder[param.Key] = param.Value;
                else
                {
                    string uncryptedParameterValue = _encryptionService.DecryptAsync(param.Value).GetAwaiter().GetResult();
                    builder[param.Key] = uncryptedParameterValue;
                }
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
        
        public async Task<IDbContextFactory<DbContext>> GetReadOnlyDbContextFactoryByIdAsync(int id)
        {
            return await _readOnlyContextFactoryByIdCache.GetOrAddAsync(id, async key =>
            {
                _logger.LogDebug("Cache miss for connection ID: {Id}, creating new factory", key);
                var connection = await _dbConnectionRepository.FindByIdAsync(key);
                return await CreateDbContextFactoryAsync(connection, true);
            });
        }

        private async Task<IDbContextFactory<DbContext>> CreateDbContextFactoryAsync(DBConnection connection, bool getReadOnlyContext = false)
        {
            try
            {
                _logger.LogDebug("Creating DbContextFactory for connection: {Name}", connection.Name);
                
                var standardContextTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == connection.ContextName)
                    .ToList();
                
                var readOnlyContextTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == connection.ContextName + "ReadOnly")
                    .ToList();

                if (!standardContextTypes.Any())
                {
                    _logger.LogError("No DbContext found with type name: {ContextTypeName}", connection.ContextName);
                    throw new InvalidOperationException($"No DbContext found with type name {connection.ContextName}");
                }
                
                if (!readOnlyContextTypes.Any())
                {
                    _logger.LogError("No ReadOnly DbContext found with type name: {ContextTypeName}", connection.ContextName);
                    throw new InvalidOperationException($"No ReadOnly DbContext found with type name {connection.ContextName}");
                }

                var standardContextType = standardContextTypes.First();
                var readOnlyContextType = readOnlyContextTypes.First();
                var scope = ServiceActivator.GetScope();

                // Construire la chaîne de connexion
                string connectionString = string.Join(';', connection.Parameters.Where(p => !p.IsEncrypted).Select(p => p.Key + "=" + p.StoredValue));
                foreach (var cryptedParameter in connection.Parameters.Where(p => p.IsEncrypted))
                {
                    string uncryptedParameterValue = await _encryptionService.DecryptAsync(cryptedParameter.StoredValue);
                    connectionString += $";{cryptedParameter.Key}={uncryptedParameterValue}";
                }

                // Créer le type générique de DbContextOptionsBuilder
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(getReadOnlyContext? readOnlyContextType: standardContextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);
                if (optionsBuilder == null)
                    throw new NullReferenceException("optionsBuilder cannot be null");
                // Configurer les options selon le type de base de données
                switch (connection.ConnectionType)
                {
                    case DbConnectionType.SqlServer:
                        _logger.LogDebug("Configuring SQL Server connection factory");
                        optionsBuilder.UseSqlServer(connectionString, 
                            options => 
                                options.EnableRetryOnFailure()
                                );
                        break;

                    case DbConnectionType.MySql:
                        _logger.LogDebug("Configuring MySQL connection factory");
                        optionsBuilder.UseMySql(connectionString,
                            ServerVersion.AutoDetect(connectionString),
                            options => 
                                options.EnableRetryOnFailure()
                                );
                        break;

                    case DbConnectionType.PgSql:
                        _logger.LogDebug("Configuring PostgreSQL connection factory");
                        optionsBuilder.UseNpgsql(connectionString,
                            options => 
                                options.EnableRetryOnFailure()
                                );
                        break;
                    case DbConnectionType.SQLite:
                        _logger.LogDebug("Configuring SQLite connection factory");
                        optionsBuilder.UseSqlite(connectionString);
                        break;
                    default:
                        _logger.LogError("Unsupported database type: {ConnectionType}", connection.ConnectionType);
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
                }

                // Créer le type générique de DbContextFactory
                var factoryType = typeof(PooledDbContextFactory<>).MakeGenericType(getReadOnlyContext ? readOnlyContextType: standardContextType);
                
                // Créer la factory avec le type spécifique
                var factory = Activator.CreateInstance(
                    factoryType, 
                    optionsBuilder.Options,
                    1024);

                // Créer un wrapper qui convertit la factory spécifique en IDbContextFactory<DbContext>
                return new DbContextFactoryWrapper(factory, getReadOnlyContext ? readOnlyContextType : standardContextType);
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