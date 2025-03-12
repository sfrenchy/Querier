using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Models;

namespace Querier.Api.Domain.Services;



public class CompilationFailedException : Exception
{
    public IEnumerable<Diagnostic> Diagnostics { get; }

    public CompilationFailedException(string message, IEnumerable<Diagnostic> diagnostics) : base(message)
    {
        Diagnostics = diagnostics;
    }
}

public class RoslynCompilerService(ILogger<RoslynCompilerService> logger) : IRoslynCompilerService
{
    public CompilationResult CompileAssembly(string assemblyName,
        Dictionary<string, string> sourceFiles,
        List<Type> referenceTypes = null,
        List<byte[]> refAssemblyBytes = null)
    {
        var peStream = new MemoryStream();
        var pdbStream = new MemoryStream();

        var compilation = GenerateCode(assemblyName, sourceFiles, referenceTypes, refAssemblyBytes);
        var emitResult = compilation.Emit(peStream, pdbStream);

        if (!emitResult.Success)
        {
            var compilationErrors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            var errorMessage = string.Join("\n", compilationErrors.Select(e =>
                $"Error {e.Id} at line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}"));

            logger.LogError("Compilation failed for {AssemblyName}: {Errors}", assemblyName, errorMessage);

            return new CompilationResult(null, null, compilationErrors);
        }

        peStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);

        return new CompilationResult(peStream.ToArray(), pdbStream.ToArray(), Enumerable.Empty<Diagnostic>());
    }

    private CSharpCompilation GenerateCode(string assemblyName, Dictionary<string, string> sourceFiles, List<Type> referenceTypes,
        List<byte[]> refAssembliesBytes)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        var parsedSyntaxTrees = sourceFiles.Select(f => SyntaxFactory.ParseSyntaxTree(f.Value, options, f.Key.EndsWith(".cs") ? f.Key : $"{f.Key}.cs", Encoding.UTF8));

        return CSharpCompilation.Create($"{assemblyName}.dll",
            parsedSyntaxTrees,
            references: GetCompilationReferences(referenceTypes, refAssembliesBytes),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug));
    }

    private List<MetadataReference> GetCompilationReferences(List<Type> referenceTypes, List<byte[]> refAssembliesBytes)
    {
        var refs = new List<MetadataReference>();

        var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
        refs.AddRange(referencedAssemblies.Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

        var coreAssemblies = new[]
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

        refs.AddRange(coreAssemblies.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)));

        if (referenceTypes != null)
            refs.AddRange(referenceTypes.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)));

        if (refAssembliesBytes != null)
        {
            refs.AddRange(refAssembliesBytes.Select(bytes => MetadataReference.CreateFromStream(new MemoryStream(bytes))));
        }

        refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));

        return refs;
    }
}


