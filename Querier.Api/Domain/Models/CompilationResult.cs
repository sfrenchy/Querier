using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Querier.Api.Domain.Models;

public record CompilationResult(byte[] AssemblyBytes, byte[] PdbBytes, IEnumerable<Diagnostic> Diagnostics)
{
    public bool Success => AssemblyBytes != null && !Diagnostics.Any();
}