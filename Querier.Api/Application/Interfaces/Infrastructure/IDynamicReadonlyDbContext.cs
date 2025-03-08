using System;
using System.Collections.Generic;

namespace Querier.Api.Application.Interfaces.Infrastructure;

public interface IDynamicReadOnlyDbContext
{
    public Dictionary<string, Func<IDynamicReadOnlyDbContext, dynamic>> CompiledQueries { get; }
}