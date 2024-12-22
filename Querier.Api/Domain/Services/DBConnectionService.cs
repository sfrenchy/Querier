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
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Database.Generators;
using Querier.Api.Infrastructure.Database.Parameters;

namespace Querier.Api.Domain.Services
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
                    Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureParameters.st"
                );
                var procedureParamsTemplate = new Template(File.ReadAllText(templatePath), '$', '$');
                procedureParamsTemplate.Add("nameSpace", connectionNamespace);
                procedureParamsTemplate.Add("procedureList", storedProcedures);
                string procedureParamsContent = procedureParamsTemplate.Render();
                srcZipContent.Add("ProcedureParameters\\ProcedureParameters.cs", procedureParamsContent);
                sourceFiles.Add(procedureParamsContent);

                var procedureResultTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureResultSet.st")
                ), '$', '$');
                procedureResultTemplate.Add("nameSpace", connectionNamespace);
                procedureResultTemplate.Add("procedureList", storedProcedures.Where(s => s.HasOutput).ToList());
                string procedureResultContent = procedureResultTemplate.Render();
                srcZipContent.Add("ProcedureResultSet\\ProcedureResultSet.cs", procedureResultContent);
                sourceFiles.Add(procedureResultContent);

                var procedureReportRequestTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureReportRequests.st")
                ), '$', '$');
                procedureReportRequestTemplate.Add("nameSpace", connectionNamespace);
                procedureReportRequestTemplate.Add("procedureList", storedProcedures);
                string procedureReportRequestContent = procedureReportRequestTemplate.Render();
                srcZipContent.Add("ProcedureReportRequests\\ProcedureReportRequests.cs", procedureReportRequestContent);
                sourceFiles.Add(procedureReportRequestContent);

                var procedureContextTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureContext.st")
                ), '$', '$');
                procedureContextTemplate.Add("nameSpace", connectionNamespace);
                procedureContextTemplate.Add("contextNameSpace", contextName);
                procedureContextTemplate.Add("procedureList", storedProcedures);
                string procedureContextContent = procedureContextTemplate.Render();
                srcZipContent.Add("ProcedureContext\\ProcedureContext.cs", procedureContextContent);
                sourceFiles.Add(procedureContextContent);

                var procedureServiceTemplate = new Template(File.ReadAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureService.st")
                ), '$', '$');
                procedureServiceTemplate.Add("nameSpace", connectionNamespace);
                procedureServiceTemplate.Add("contextNameSpace", contextName);
                procedureServiceTemplate.Add("procedureList", storedProcedures);
                string procedureServiceContent = procedureServiceTemplate.Render();
                srcZipContent.Add("ProcedureService\\ProcedureService.cs", procedureServiceContent);
                sourceFiles.Add(procedureServiceContent);

                var procedureServiceResolverTemplate = new Template(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureServiceResolver.st")), '$', '$');
                procedureServiceResolverTemplate.Add("nameSpace", connectionNamespace);
                procedureServiceResolverTemplate.Add("contextNameSpace", contextName);
                procedureServiceResolverTemplate.Add("procedureList", storedProcedures);
                string procedureServiceResolverContent = procedureServiceResolverTemplate.Render();
                srcZipContent.Add("ProcedureServiceResolver\\ProcedureServiceResolver.cs", procedureServiceResolverContent);
                sourceFiles.Add(procedureServiceResolverContent);

                var procedureControllerTemplate = new Template(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Templates", "DBTemplating", "ProcedureController.st")), '$', '$');
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

            if (!Directory.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");
            string srcPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.Sources.zip");
            using (FileStream srcFileStream = new FileStream(srcPath, FileMode.Create))
            {
                using (FileStream zipFileStream = new FileStream(sourceZipPath, FileMode.Open))
                {
                    zipFileStream.CopyTo(srcFileStream);
                }
            }
            // Templating done, we are now compiling generated sources
            MemoryStream peStream = new MemoryStream();
            MemoryStream pdbStream = new MemoryStream();
            EmitResult emitResult = GenerateCode(connection.Name, sourceFiles).Emit(peStream, pdbStream);

            if (!emitResult.Success)
            {
                var errorDetails = string.Join("\n", emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"Error {d.Id} at {d.Location}: {d.GetMessage()}"));
                _logger.LogError($"Code compilation failed with errors:\n{errorDetails}");
    
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
            newConnection.ContextName = connectionNamespace + "." + contextName;
            newConnection.ApiRoute = connection.ContextApiRoute;

            if (!Path.Exists("Assemblies"))
                Directory.CreateDirectory("Assemblies");

            string dllPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.dll");
            string pdbPath = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.pdb");

            using (FileStream dllFileStream = new FileStream(dllPath, FileMode.Create))
            {
                peStream.CopyTo(dllFileStream);
            }
            using (FileStream pdbFileStream = new FileStream(pdbPath, FileMode.Create))
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
    }
}