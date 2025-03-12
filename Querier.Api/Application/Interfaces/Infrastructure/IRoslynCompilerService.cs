using System;
using System.Collections.Generic;
using Querier.Api.Domain.Models;

namespace Querier.Api.Application.Interfaces.Infrastructure;

public interface IRoslynCompilerService
{
    CompilationResult CompileAssembly(string assemblyName, Dictionary<string, string> sourceCodes, List<Type> referenceTypes = null, List<byte[]> additionalAssemblyReferences = null);
}