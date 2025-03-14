using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace $model.RootNamespace$.Contexts;

public partial class $model.RootNamespace$DbContextReadOnly : $model.RootNamespace$DbContext, IDynamicReadOnlyDbContext
{
    public Dictionary<string, Func<IDynamicReadOnlyDbContext, dynamic>> CompiledQueries { get; }
        = new Dictionary<string, Func<IDynamicReadOnlyDbContext, dynamic>>();
            
    public $model.RootNamespace$DbContextReadOnly(DbContextOptions<$model.RootNamespace$DbContextReadOnly> options)
        : base(options)
    {
    \}

    public override int SaveChanges()
    {
        throw new NotSupportedException("This context does not support saving changes.");
    \}
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("This context does not support saving changes.");
    \}
    
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException("This context does not support saving changes.");
    \}
}