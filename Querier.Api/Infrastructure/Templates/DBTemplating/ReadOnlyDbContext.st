using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using $contextNamespace$;

namespace $contextNamespace$
{
    public class $contextName$ReadOnlyContext : $contextName$
    {
        public $contextName$ReadOnlyContext(DbContextOptions<$contextName$> options)
            : base(options)
        {
        \}
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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
\}