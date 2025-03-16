using Antlr4.StringTemplate;
using Bricelam.EntityFrameworkCore.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Sqlite.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Scaffolding.Internal;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Infrastructure.Database.Generators;
using Querier.Api.Infrastructure.Database.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Domain.Services;

public class SourceCodeService
{
    private readonly ILogger _logger;
    private readonly DbConnectionType _dbConnectionType;
    private readonly string _connectionString;
    private readonly string _rootNamespace;
    private readonly string _modelNamespace;
    private readonly string _apiRoute;
    private readonly Pluralizer _pluralizer = new Pluralizer();
    private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();
    private readonly AdhocWorkspace _workspace = new AdhocWorkspace();
    private DatabaseModel _dbModel = new();
    private Dictionary<string, HashSet<SyntaxTree>> _generatedSyntaxTrees = new()
    {
        { "Entities", new() },
        { "Dtos", new() },
        { "Interfaces", new() },
        { "Repositories", new() },
        { "Services", new() },
        { "Controllers", new() },
        { "Contexts", new() },
        { "ServiceContainer", new() }
    };
    private HashSet<string> _viewEntities = new HashSet<string>();
    private List<TemplateEntityMetadata> _entityMap = new();
    private List<StoredProcedureMetadata> _procedureMap = new();
    public SourceCodeService(DbConnectionType dbConnectionType, string connectionString, string rootNamespace, string apiRoute, ILogger logger)
    {
        _dbConnectionType = dbConnectionType;
        _connectionString = connectionString;
        _rootNamespace = rootNamespace;
        _apiRoute = apiRoute;
        _logger = logger;
    }   

    public IEnumerable<SyntaxTree> GetGeneratedSyntaxTrees()
    {
        return _generatedSyntaxTrees.SelectMany(kvp => kvp.Value);
    }

    public async Task GenerateDbConnectionSourcesAsync()
    {
        foreach (string templateFile in Directory.GetFiles(Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "Infrastructure",
                        "Templates",
                        "SourceCodeService")))
        {
            _templates.Add(Path.GetFileNameWithoutExtension(templateFile), await File.ReadAllTextAsync(templateFile));
        }
        
        ScaffoldedModel();
        _entityMap = GenerateEntityMap();

        IDatabaseMetadataProvider dbMetadataProvider = _dbConnectionType switch
        {
            DbConnectionType.SqlServer => new SqlServerDatabaseMetadataProvider(_dbModel, _logger),
            DbConnectionType.MySql => new MySqlDatabaseMetadataProvider(_logger),
            DbConnectionType.PgSql => new PostgreSqlDatabaseMetadataProvider(_logger),
            DbConnectionType.SQLite => new SqliteDatabaseMetadataProvider(),
            _ => throw new NotSupportedException($"Database type {_dbConnectionType} not supported")
        };

        var procedureMetadataExtractor = new ProcedureMetadataExtractorSqlServer(_connectionString, _dbModel);
        _procedureMap = procedureMetadataExtractor.ProcedureMetadata;

        await Task.WhenAll(
            GenerateFromEntities("EntityToDto", "Dtos", _generatedSyntaxTrees["Dtos"]),
            GenerateFromEntities("EntityToInterfaceRepository", "Interfaces/Repositories", _generatedSyntaxTrees["Interfaces"]),
            GenerateFromEntities("EntityToRepository", "Repositories", _generatedSyntaxTrees["Repositories"]),
            GenerateFromEntities("EntityToInterfaceService", "Interfaces/Services", _generatedSyntaxTrees["Interfaces"]),
            GenerateFromEntities("EntityToService", "Services", _generatedSyntaxTrees["Services"]),
            GenerateFromEntities("EntityToController", "Controllers", _generatedSyntaxTrees["Controllers"]),
            GenerateProcedureFromDatabase("ProcedureToInputDto", "Dtos/Procedures", _procedureMap, _generatedSyntaxTrees["Dtos"]),
            GenerateProcedureFromDatabase("ProcedureToOutputDto", "Dtos/Procedures", _procedureMap, _generatedSyntaxTrees["Dtos"]),
            GenerateProcedureFromDatabase("ProcedureToInterfaceRepository", "Interfaces/Procedures/Repositories", _procedureMap, _generatedSyntaxTrees["Interfaces"]),
            GenerateProcedureFromDatabase("ProcedureToRepository", "Repositories/Procedures", _procedureMap, _generatedSyntaxTrees["Repositories"]),
            GenerateProcedureFromDatabase("ProcedureToInterfaceService", "Interfaces/Procedures/Services", _procedureMap, _generatedSyntaxTrees["Interfaces"]),
            GenerateProcedureFromDatabase("ProcedureToService", "Services/Procedures", _procedureMap, _generatedSyntaxTrees["Repositories"]),
            GenerateProcedureFromDatabase("ProcedureToController", "Controllers/Procedures", _procedureMap, _generatedSyntaxTrees["Controllers"]),
            GenerateReadOnlyDbContext()
        );

        await GenerateServiceContainer();
    }

    

    private async Task GenerateServiceContainer()
    {
        await Task.Run(() =>
        {
            var template = new Template(_templates["ServiceContainer"], '$', '$');
            template.Add("model", new
            {
                RootNamespace = _rootNamespace,
                EntityRepositories = _generatedSyntaxTrees["Repositories"].Where(s => !s.FilePath.Contains("Procedures")).Select(s => Path.GetFileNameWithoutExtension(s.FilePath)),
                ProcedureRepositories = _generatedSyntaxTrees["Repositories"].Where(s => s.FilePath.Contains("Procedures")).Select(s => Path.GetFileNameWithoutExtension(s.FilePath)),
                EntityServices = _generatedSyntaxTrees["Services"].Where(s => !s.FilePath.Contains("Procedures")).Select(s => Path.GetFileNameWithoutExtension(s.FilePath)),
                ProcedureServices = _generatedSyntaxTrees["Services"].Where(s => s.FilePath.Contains("Procedures")).Select(s => Path.GetFileNameWithoutExtension(s.FilePath)),
            });
            string code = template.Render();
            SyntaxTree codeSyntaxTree = CSharpSyntaxTree.ParseText(code, null, $"{_rootNamespace}ServiceContainer.cs", Encoding.UTF8);
            SyntaxNode root = codeSyntaxTree.GetRoot();
            SyntaxNode formattedRoot = Formatter.Format(root, _workspace);
            var diagnostics = codeSyntaxTree.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var error in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var location = error.Location.GetLineSpan();
                    int lineNumber = location.StartLinePosition.Line + 1;
                    var codeLines = code.Split('\n');
                    string errorLine = lineNumber <= codeLines.Length ? codeLines[lineNumber - 1].Trim() : "Unknown line";
                    _logger.LogDebug($"Error in file {_rootNamespace}ServiceContainer.cs at line {lineNumber}: {errorLine}");
                    _logger.LogDebug($"Diagnostic: {error.GetMessage()}");
                }
                throw new InvalidOperationException($"Error while generating code for ServiceContainer. See logs for details.");
            }
            lock (_generatedSyntaxTrees["ServiceContainer"])
            {
                _generatedSyntaxTrees["ServiceContainer"].Add(CSharpSyntaxTree.ParseText(formattedRoot.ToFullString(), null, $"{_rootNamespace}ServiceContainer.cs", Encoding.UTF8));
            }
        });
    }

    private async Task GenerateReadOnlyDbContext()
    {
        await Task.Run(() =>
        {
            var template = new Template(_templates["DbContextReadOnly"], '$', '$');
            template.Add("model", new
            {
                RootNamespace = _rootNamespace
            });

            string code = template.Render();
            SyntaxTree codeSyntaxTree = CSharpSyntaxTree.ParseText(code, null, $"Contexts/{_rootNamespace}DbContextReadOnly.cs", Encoding.UTF8);
            SyntaxNode root = codeSyntaxTree.GetRoot();
            SyntaxNode formattedRoot = Formatter.Format(root, _workspace);
            var diagnostics = codeSyntaxTree.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var error in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var location = error.Location.GetLineSpan();
                    int lineNumber = location.StartLinePosition.Line + 1;

                    var codeLines = code.Split('\n');
                    string errorLine = lineNumber <= codeLines.Length ? codeLines[lineNumber - 1].Trim() : "Unknown line";

                    _logger.LogDebug($"Error in file {_rootNamespace}DbContextReadOnly.cs at line {lineNumber}: {errorLine}");
                    _logger.LogDebug($"Diagnostic: {error.GetMessage()}");
                }

                throw new InvalidOperationException($"Error while generating code for ReadOnlyDbContext. See logs for details.");
            }
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
            var codeDeclarationName = classDeclaration != null ? classDeclaration.Identifier.Text : interfaceDeclaration.Identifier.Text;
            
            lock (_generatedSyntaxTrees["Contexts"])
            {
                _generatedSyntaxTrees["Contexts"].Add(CSharpSyntaxTree.ParseText(formattedRoot.ToFullString(), null, $"Contexts/{_rootNamespace}DbContextReadOnly.cs", Encoding.UTF8));
            }
        });
    }

    private async Task GenerateProcedureFromDatabase(string templateFile, string sourcePath, List<StoredProcedureMetadata> proceduresMetadata, HashSet<SyntaxTree> targetSyntaxTrees)
    {
        foreach (StoredProcedureMetadata procedureMetadata in proceduresMetadata)
        {
            await Task.Run(() =>
            {
                var template = new Template(_templates[templateFile], '$', '$');
                template.Add("model", new
                {
                    RootNamespace = _rootNamespace,
                    ApiRoute = _apiRoute,
                    CSName = procedureMetadata.CSName,
                    OutputSet = procedureMetadata.OutputSet,
                    Parameters = procedureMetadata.Parameters,
                    HasOutput = procedureMetadata.HasOutput,
                    HasParameters = procedureMetadata.HasParameters,
                    InlineParameters = procedureMetadata.InlineParameters
                });

                string code = template.Render();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    SyntaxTree codeSyntaxTree = CSharpSyntaxTree.ParseText(code, null, sourcePath, Encoding.UTF8);
                    SyntaxNode root = codeSyntaxTree.GetRoot();
                    SyntaxNode formattedRoot = Formatter.Format(root, _workspace);
                    var diagnostics = codeSyntaxTree.GetDiagnostics();
                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        foreach (var error in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                        {
                            var location = error.Location.GetLineSpan();
                            int lineNumber = location.StartLinePosition.Line + 1;

                            var codeLines = code.Split('\n');
                            string errorLine = lineNumber <= codeLines.Length ? codeLines[lineNumber - 1].Trim() : "Unknown line";

                            _logger.LogDebug($"Error in file {sourcePath} at line {lineNumber}: {errorLine}");
                            _logger.LogDebug($"Diagnostic: {error.GetMessage()}");
                        }

                        throw new InvalidOperationException($"Error while generating code for procedure {procedureMetadata.Name}. See logs for details.");
                    }
                    var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
                    var codeDeclarationName = classDeclaration != null ? classDeclaration.Identifier.Text : interfaceDeclaration.Identifier.Text;
                    string filePath = sourcePath + "/" + codeDeclarationName + ".cs";

                    lock (targetSyntaxTrees)
                    {
                        targetSyntaxTrees.Add(CSharpSyntaxTree.ParseText(formattedRoot.ToFullString(), null, filePath, Encoding.UTF8));
                    }
                }
            });
        }
    }

    public async Task<byte[]> CreateSourceZipAsync()
    {
        return await Task.Run(() =>
        {
            using var sourceStream = new MemoryStream();
            using (var archive = new ZipArchive(sourceStream, ZipArchiveMode.Create))
            {
                foreach (var item in _generatedSyntaxTrees)
                {
                    foreach (var syntaxTree in item.Value)
                    {
                        var filePath = syntaxTree.FilePath;
                        var entry = archive.CreateEntry(filePath);
                        using var entryStream = entry.Open();
                        using var streamWriter = new StreamWriter(entryStream);
                        streamWriter.Write(syntaxTree.ToString());
                    }
                }
            }
            return sourceStream.ToArray();
        });
    }

    private async Task GenerateFromEntities(string templateFile, string sourcePath, HashSet<SyntaxTree> targetSyntaxTrees)
    {
        foreach (var entity in _entityMap)
        {
            await Task.Run(() =>
            {
                var template = new Template(_templates[templateFile], '$', '$');
                string methodSignatureParameters = string.Join(", ", entity.Keys.Select(k => $"{k.Value} {char.ToLowerInvariant(k.Key[0])}{k.Key.Substring(1)}"));
                string linqEntityFilter = $"e => ";
                var parametersKey = entity.Keys.Select(k => $"{char.ToLowerInvariant(k.Key[0])}{k.Key.Substring(1)}");
                string keyParameterLine = string.Join(", ", parametersKey);
                string stringConcatParameters = "{" + keyParameterLine.Replace(",", "},{") + "}";
                string cacheKeyIdentifier = stringConcatParameters.Replace(",", "_");
                string linqEntityFilterConditions = "e != null &&";
                List<string> conditions = new List<string>();
                foreach (var key in entity.Keys)
                    conditions.Add($"e.{key.Key} == {char.ToLowerInvariant(key.Key[0])}{key.Key.Substring(1)}");
                linqEntityFilterConditions += string.Join(" && ", conditions);
                linqEntityFilter += linqEntityFilterConditions;

                template.Add("model", new
                {
                    RootNamespace = _rootNamespace,
                    Entity = new
                    {
                        Name = entity.Name,
                        PluralName = entity.PluralName,
                        Keys = entity.Keys,
                        IsViewEntity = entity.IsViewEntity,
                        IsTableEntity = entity.IsTableEntity,
                        Properties = entity.Properties,
                        ForeignKeys = entity.ForeignKeys,
                        HasKeys = entity.Keys.Count > 0,
                        MethodSignatureParameter = methodSignatureParameters,
                        LinqEntityFilter = linqEntityFilter,
                        KeyParameterLine = keyParameterLine,
                        StringConcatParameters = stringConcatParameters,
                        ParametersNamesArray = entity.Keys.Select(k => $"{char.ToLowerInvariant(k.Key[0])}{k.Key.Substring(1)}").ToArray(),
                        CacheKeyIdentifier = cacheKeyIdentifier
                    },
                    ApiRoute = _apiRoute
                });
                string code = template.Render();
                SyntaxTree codeSyntaxTree = CSharpSyntaxTree.ParseText(code, null, sourcePath, Encoding.UTF8);
                SyntaxNode root = codeSyntaxTree.GetRoot();
                SyntaxNode formattedRoot = Formatter.Format(root, _workspace);
                var diagnostics = codeSyntaxTree.GetDiagnostics();
                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    foreach (var error in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        var location = error.Location.GetLineSpan();
                        int lineNumber = location.StartLinePosition.Line + 1;

                        var codeLines = code.Split('\n');
                        string errorLine = lineNumber <= codeLines.Length ? codeLines[lineNumber - 1].Trim() : "Unknown line";

                        _logger.LogDebug($"Error in file {sourcePath} at line {lineNumber}: {errorLine}");
                        _logger.LogDebug($"Diagnostic: {error.GetMessage()}");
                    }

                    throw new InvalidOperationException($"Error while generating code for entity {entity.Name}. See logs for details.");
                }
                var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
                var codeDeclarationName = classDeclaration != null ? classDeclaration.Identifier.Text : interfaceDeclaration.Identifier.Text;
                string filePath = sourcePath + "/" + codeDeclarationName + ".cs";

                lock (targetSyntaxTrees)
                {
                    targetSyntaxTrees.Add(CSharpSyntaxTree.ParseText(formattedRoot.ToFullString(), null, filePath, Encoding.UTF8));
                }
            });
        }
    }

    private List<TemplateEntityMetadata> GenerateEntityMap()
    {
        var entityMap = new Dictionary<string, TemplateEntityMetadata>();
        // Extract metadata from entities
        foreach (SyntaxTree entitySyntaxTree in _generatedSyntaxTrees["Entities"]) 
        {
            var entityMetadataExtraction = ExtractEntityMetadata(entitySyntaxTree);
            entityMap[entityMetadataExtraction.EntityName] = entityMetadataExtraction.EntityMetadata;
        }

        // Set foreign key references
        foreach (var entity in entityMap.Values)
        {
            foreach (var fk in entity.ForeignKeys)
            {
                if (entityMap.TryGetValue(fk.ReferencedEntitySingular, out var referencedEntity))
                {
                    fk.ReferencedColumn = referencedEntity.Keys.FirstOrDefault().Key ?? "Id";
                }
            }
        }
        return entityMap.Values.ToList();
    }

    private (string EntityName, TemplateEntityMetadata EntityMetadata) ExtractEntityMetadata(SyntaxTree entitySyntaxTree)
    {
        var root = entitySyntaxTree.GetRoot();
        var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        var entityName = classDeclaration.Identifier.Text;
        var entityMetadata = new TemplateEntityMetadata
        {
            Name = entityName,
            PluralName = _pluralizer.Pluralize(entityName),
            Properties = new List<TemplateProperty>(),
            ForeignKeys = new List<TemplateForeignKey>(),
            IsViewEntity = _viewEntities.Contains(entityName),
            Keys = new Dictionary<string, string>()
        };

        var keys = new HashSet<string>();
        var foreignKeys = new HashSet<string>();

        // Detection of [PrimaryKey] attribute on the class
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

        // Detection of [Key] attribute on properties
        foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (property.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString() == "Key"))
            {
                keys.Add(property.Identifier.Text);
            }
        }

        // Detection of [ForeignKey] attribute on properties
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

            // Detection of [InverseProperty] attribute on properties
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
                        a.Expression.ToString().Replace("\"", "") == _pluralizer.Pluralize(entityName));

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
                                NamePlural = _pluralizer.Pluralize(foreignKeys.LastOrDefault() ?? property.Identifier.Text),
                                ReferencedEntityPlural = _pluralizer.Pluralize(actualType),
                                ReferencedEntitySingular = actualType,
                                ReferencedColumn = "Id",
                                IsCollection = isCollection
                            };

                            if (!entityMetadata.ForeignKeys.Any(f => f.Name == fk.Name))
                                entityMetadata.ForeignKeys.Add(fk);
                        }
                    }
                }
            }
        }

        // Extraction of properties
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

            entityMetadata.Properties.Add(prop);

            if (isKey)
            {
                entityMetadata.Keys.Add(prop.Name, prop.CSType);
            }
        }

        // If no key has been found, we try to find a property named "Id" or ending with "Id"
        if (!entityMetadata.Keys.Any())
        {
            var idProperty = entityMetadata.Properties.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

            if (idProperty != null)
            {
                idProperty.IsKey = true;
                entityMetadata.Keys.Add(idProperty.Name, idProperty.CSType);
            }
        }

        return (entityName, entityMetadata);
    }

    private void ScaffoldedModel()
    {
        IReverseEngineerScaffolder scaffolder = _dbConnectionType switch
        {
            DbConnectionType.SqlServer => DatabaseScaffolderFactory.CreateMssqlScaffolder(),
            DbConnectionType.MySql => DatabaseScaffolderFactory.CreateMySQLScaffolder(),
            DbConnectionType.PgSql => DatabaseScaffolderFactory.CreatePgSQLScaffolder(),
            DbConnectionType.SQLite => DatabaseScaffolderFactory.CreateSQLiteScaffolder(),
            _ => throw new NotSupportedException($"Database type {_dbConnectionType} not supported")
        };

        var databaseEFCoreServiceProvider = _dbConnectionType switch
        {
            DbConnectionType.SqlServer => new ServiceCollection().AddEntityFrameworkSqlServer().BuildServiceProvider(),
            DbConnectionType.MySql => new ServiceCollection().AddEntityFrameworkMySql().BuildServiceProvider(),
            DbConnectionType.PgSql => new ServiceCollection().AddEntityFrameworkNpgsql().BuildServiceProvider(),
            DbConnectionType.SQLite => new ServiceCollection().AddEntityFrameworkSqlite().BuildServiceProvider(),
            _ => throw new NotSupportedException($"Database type {_dbConnectionType} not supported")
        };

        var diagnosticsLogger = databaseEFCoreServiceProvider.GetRequiredService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>();
        var relationalTypeMapperSource = databaseEFCoreServiceProvider.GetRequiredService<IRelationalTypeMappingSource>();

        DatabaseModelFactory DatabaseModelFactory = _dbConnectionType switch
        { 
            DbConnectionType.SqlServer => new SqlServerDatabaseModelFactory(diagnosticsLogger, relationalTypeMapperSource),
            DbConnectionType.MySql => new MySqlDatabaseModelFactory(diagnosticsLogger, relationalTypeMapperSource, new MySqlOptions()),
            DbConnectionType.PgSql => new NpgsqlDatabaseModelFactory(diagnosticsLogger),
            DbConnectionType.SQLite => new SqliteDatabaseModelFactory(diagnosticsLogger, relationalTypeMapperSource),
            _ => throw new NotSupportedException($"Database type {_dbConnectionType} not supported")
        };


        var dbOpts = new DatabaseModelFactoryOptions();
        var modelOpts = new ModelReverseEngineerOptions();
        var codeGenOpts = new ModelCodeGenerationOptions()
        {
            RootNamespace = _rootNamespace,
            ContextName = _rootNamespace + "DbContext",
            ContextNamespace = _rootNamespace + ".Contexts",
            ModelNamespace = _rootNamespace + ".Entities",
            SuppressConnectionStringWarning = true,
            SuppressOnConfiguring = true,
            UseDataAnnotations = true,
            ConnectionString = _connectionString,
            ContextDir = "Context", 
            Language = "C#",
            UseNullableReferenceTypes = true
        };

        ScaffoldedModel scaffoldedModelSources = scaffolder.ScaffoldModel(_connectionString, dbOpts, modelOpts, codeGenOpts);

        _dbModel = DatabaseModelFactory.Create(_connectionString, dbOpts);

        PrepareScalffoldedModel(scaffoldedModelSources);
        var contextSyntaxTree = SyntaxFactory.ParseSyntaxTree(scaffoldedModelSources.ContextFile.Code, null, $"Contexts/{codeGenOpts.ContextName}.cs", Encoding.UTF8);
        
        _generatedSyntaxTrees["Contexts"].Add(contextSyntaxTree);

        // Identify view entities from the context file
        var contextSyntaxNode = contextSyntaxTree.GetRoot();
        var onModelCreatingNode = contextSyntaxNode.DescendantNodes().OfType<MethodDeclarationSyntax>()
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
                    _viewEntities.Add(currentEntity);
                }
            }
        }

        // Separate table entities from view entities
        foreach (var entity in scaffoldedModelSources.AdditionalFiles)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(entity.Code, null, "", Encoding.UTF8);
            var root = syntaxTree.GetRoot();
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration == null) continue;

            var entityName = classDeclaration.Identifier.Text;
            string filePath = "Entities/" + entityName + ".cs";
            _generatedSyntaxTrees["Entities"].Add(CSharpSyntaxTree.ParseText(entity.Code, null, filePath, Encoding.UTF8));
        }
    }

    private void PrepareScalffoldedModel(ScaffoldedModel scaffoldedModelSources)
    {
        scaffoldedModelSources.ContextFile.Code = scaffoldedModelSources.ContextFile.Code.Replace($"DbContextOptions<{_rootNamespace + "DbContext"}>", "DbContextOptions");
    }   
}
