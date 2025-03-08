using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace $contextNamespace$
{
    public partial class $contextName$ReadOnly : $contextName$, IDynamicReadOnlyDbContext
    {
        public Dictionary<string, Func<IDynamicReadOnlyDbContext, dynamic>> CompiledQueries { get; }
            = new Dictionary<string, Func<IDynamicReadOnlyDbContext, dynamic>>();
            
        public $contextName$ReadOnly(DbContextOptions<$contextName$ReadOnly> options)
            : base(options)
        {
        \}
        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        \}
        */
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
\}