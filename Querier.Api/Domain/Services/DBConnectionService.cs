using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Database.Generators;
using Querier.Api.Infrastructure.Database.Templates;
using Swashbuckle.AspNetCore.Swagger;
using System.Security.Cryptography;
using QDBConnection = Querier.Api.Domain.Entities.QDBConnection.QDBConnection;
using ParamInfo = Querier.Api.Application.DTOs.Responses.DBConnection.ParameterInfo;

namespace Querier.Api.Domain.Services
{
    public class DBConnectionService : IDBConnectionService
    {
        private readonly IDbContextFactory<ApiDbContext> _apiDbContextFactory;
        private readonly IDynamicContextList _dynamicContextList;
        private readonly ILogger<DBConnectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationPartManager _partManager;
        private readonly JsonSchemaGenerator _jsonSchemaGenerator;
        private readonly EndpointExtractor _endpointExtractor;
        private readonly DatabaseServerDiscovery _serverDiscovery;
        private readonly DatabaseSchemaExtractor _schemaExtractor;

        public DBConnectionService(
            IDynamicContextList dynamicContextList,
            IDbContextFactory<ApiDbContext> apiDbContextFactory,
            IServiceProvider serviceProvider,
            ILogger<DBConnectionService> logger,
            ApplicationPartManager partManager,
            ILogger<DatabaseServerDiscovery> serverDiscoveryLogger,
            ILogger<DatabaseSchemaExtractor> schemaExtractorLogger)
        {
            _logger = logger;
            _apiDbContextFactory = apiDbContextFactory;
            _serviceProvider = serviceProvider;
            _dynamicContextList = dynamicContextList;
            _partManager = partManager;
            _jsonSchemaGenerator = new JsonSchemaGenerator();
            _endpointExtractor = new EndpointExtractor(_jsonSchemaGenerator);
            _serverDiscovery = new DatabaseServerDiscovery(serverDiscoveryLogger);
            _schemaExtractor = new DatabaseSchemaExtractor(schemaExtractorLogger);
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
            IReverseEngineerScaffolder scaffolder = connection.ConnectionType switch
            {
                QDBConnectionType.SqlServer => DatabaseScaffolderFactory.CreateMssqlScaffolder(),
                QDBConnectionType.MySQL => DatabaseScaffolderFactory.CreateMySQLScaffolder(),
                QDBConnectionType.PgSQL => DatabaseScaffolderFactory.CreatePgSQLScaffolder(),
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
                SuppressOnConfiguring = true
            };

            var scaffoldedModelSources = scaffolder.ScaffoldModel(connection.ConnectionString, dbOpts, modelOpts, codeGenOpts);

            var contextFile = connection.ConnectionType switch
            {
                QDBConnectionType.SqlServer => scaffoldedModelSources.ContextFile.Code.Replace(".UseSqlServer", ".UseLazyLoadingProxies().UseSqlServer"),
                QDBConnectionType.MySQL => scaffoldedModelSources.ContextFile.Code.Replace(".UseMySql", ".UseLazyLoadingProxies().UseMySql"),
                QDBConnectionType.PgSQL => scaffoldedModelSources.ContextFile.Code.Replace(".UseNpgsql", ".UseLazyLoadingProxies().UseNpgsql"),
                _ => throw new NotSupportedException($"Database type {connection.ConnectionType} not supported")
            };

            var sourceFiles = new List<string> { contextFile };
            sourceFiles.AddRange(scaffoldedModelSources.AdditionalFiles.Select(f => f.Code));

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
                procedureDescription = System.Text.Json.JsonSerializer.Serialize(storedProcedures);

                var procedureModel = new StoredProcedureTemplateModel
                {
                    NameSpace = connectionNamespace,
                    ContextNameSpace = contextName,
                    ContextRoute = connection.ContextApiRoute,
                    ProcedureList = ExtractStoredProcedureMetadata(storedProcedures)
                };

                await GenerateProcedureFiles(procedureModel, srcZipContent, sourceFiles);
            }

            // Extract entity metadata from scaffolded model
            var entityModel = new TemplateModel
            {
                NameSpace = connectionNamespace,
                ContextNameSpace = contextName,
                ContextRoute = connection.ContextApiRoute,
                EntityList = ExtractEntityMetadata(scaffoldedModelSources)
            };

            await GenerateEntityFiles(entityModel, srcZipContent, sourceFiles);

            // Create source zip
            byte[] sourceZipBytes = await CreateSourceZip(srcZipContent);

            if (!Directory.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");
            string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
            File.WriteAllBytes(srcPath, sourceZipBytes);

            // Compile generated sources
            var (assemblyBytes, pdbBytes) = await CompileAssembly(connection.Name, sourceFiles);
            if (assemblyBytes == null)
            {
                result.State = QDBConnectionState.CompilationError;
                return result;
            }

            // Calculate assembly hash
            var hash = ComputeAssemblyHash(assemblyBytes);

            // Load assembly
            var assemblyLoadContext = new AssemblyLoadContext("DbContext", false);
            var loadedAssembly = assemblyLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));

            // Create new connection
            var endpoints = _endpointExtractor.ExtractFromAssembly(loadedAssembly);
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
                ApiRoute = connection.ContextApiRoute,
                Endpoints = endpoints
            };

            using (var apiDbContext = _apiDbContextFactory.CreateDbContext())
            {
                apiDbContext.QDBConnections.Add(newConnection);
                await apiDbContext.SaveChangesAsync();
            }

            result.State = QDBConnectionState.Available;
            await AssemblyLoader.LoadAssemblyFromQDBConnection(newConnection, _serviceProvider, _partManager, _logger);
            
            // Regenerate Swagger documentation
            var swaggerProvider = _serviceProvider.GetRequiredService<ISwaggerProvider>();
            AssemblyLoader.RegenerateSwagger(swaggerProvider, _logger);
            
            return result;
        }

        private async Task GenerateProcedureFiles(StoredProcedureTemplateModel model, Dictionary<string, string> srcZipContent, List<string> sourceFiles)
        {
            var templates = new[]
            {
                ("ProcedureParameters", "ProcedureParameters\\ProcedureParameters.cs"),
                ("ProcedureResultSet", "ProcedureResultSet\\ProcedureResultSet.cs"),
                ("ProcedureReportRequests", "ProcedureReportRequests\\ProcedureReportRequests.cs"),
                ("ProcedureContext", "ProcedureContext\\ProcedureContext.cs"),
                ("ProcedureService", "ProcedureService\\ProcedureService.cs"),
                ("ProcedureServiceResolver", "ProcedureServiceResolver\\ProcedureServiceResolver.cs"),
                ("ProcedureController", "ProcedureController\\ProcedureController.cs")
            };

            foreach (var (templateName, outputPath) in templates)
            {
                var template = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", $"{templateName}.st")
                ), '$', '$');

                template.Add("nameSpace", model.NameSpace);
                template.Add("contextNameSpace", model.ContextNameSpace);
                template.Add("procedureList", templateName == "ProcedureResultSet" 
                    ? model.ProcedureList.Where(s => s.HasOutput).ToList() 
                    : model.ProcedureList);

                if (templateName == "ProcedureController")
                    template.Add("contextRoute", model.ContextRoute);

                string content = template.Render();
                srcZipContent.Add(outputPath, content);
                sourceFiles.Add(content);
            }
        }

        private async Task GenerateEntityFiles(TemplateModel model, Dictionary<string, string> srcZipContent, List<string> sourceFiles)
        {
            var templates = new[]
            {
                ("EntityDto", "DTOs\\EntityDtos.cs"),
                ("EntityService", "Services\\EntityServices.cs"),
                ("EntityController", "Controllers\\EntityControllers.cs"),
                ("EntityServiceResolver", "Services\\EntityServiceResolver.cs")
            };

            foreach (var (templateName, outputPath) in templates)
            {
                var template = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", $"{templateName}.st")
                ), '$', '$');

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

        private async Task<byte[]> CreateSourceZip(Dictionary<string, string> srcZipContent)
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

        private async Task<(byte[] assemblyBytes, byte[] pdbBytes)> CompileAssembly(string contextName, List<string> sourceFiles)
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
                typeof(System.Linq.Enumerable),
                typeof(MemoryStream),
                typeof(StreamReader)
            };

            refs.AddRange(additionalAssemblies.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)));
            refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));

            return refs;
        }

        public async Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
            var toDelete = apiDbContext.QDBConnections.Find(request.DBConnectionId);
            if (toDelete == null)
                throw new KeyNotFoundException($"Connection with ID {request.DBConnectionId} not found");

            int toDeleteId = toDelete.Id;
            apiDbContext.QDBConnections.Remove(toDelete);
            await apiDbContext.SaveChangesAsync();

            return new DeleteDBConnectionResponse { DeletedDBConnectionId = toDeleteId };
        }

        public async Task<List<QDBConnectionResponse>> GetAll()
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
            return await apiDbContext.QDBConnections
                .Select(c => new QDBConnectionResponse
                {
                    ApiRoute = c.ApiRoute,
                    ConnectionString = c.ConnectionString,
                    ConnectionType = c.ConnectionType.ToString(),
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<DatabaseSchemaResponse> GetDatabaseSchema(int connectionId)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
            var connection = await apiDbContext.QDBConnections.FindAsync(connectionId);
            
            if (connection == null)
                throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

            return await _schemaExtractor.ExtractSchema(connection.ConnectionType, connection.ConnectionString);
        }

        public async Task<List<DatabaseServerInfo>> EnumerateServersAsync(string databaseType)
        {
            return await _serverDiscovery.EnumerateServersAsync(databaseType);
        }

        public async Task<SourceDownload> GetConnectionSourcesAsync(int connectionId)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
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

        public async Task<List<EndpointInfoResponse>> GetEndpointsAsync(int connectionId)
        {
            using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
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
                Parameters = e.Parameters.Select(p => new ParamInfo
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

        public async Task<QueryAnalysisResponse> GetQueryObjects(int connectionId, string objectType)
        {
            try
            {
                using var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync();
                var connection = await apiDbContext.QDBConnections.FindAsync(connectionId);
                
                if (connection == null)
                    throw new KeyNotFoundException($"Connection with ID {connectionId} not found");

                var schema = await _schemaExtractor.ExtractSchema(connection.ConnectionType, connection.ConnectionString);

                var response = new QueryAnalysisResponse();

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
                        throw new ArgumentException($"Invalid object type: {objectType}");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting query objects for connection {ConnectionId}", connectionId);
                throw;
            }
        }

        private string GetParameterSource(FromBodyAttribute fromBody, FromQueryAttribute fromQuery, FromRouteAttribute fromRoute)
        {
            if (fromBody != null) return "FromBody";
            if (fromQuery != null) return "FromQuery";
            if (fromRoute != null) return "FromRoute";
            return "FromQuery";
        }
    }
}