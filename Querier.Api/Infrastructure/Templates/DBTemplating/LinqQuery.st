using System.Linq;
using $contextNamespace$;
using Querier.Api.Application.Interfaces.Infrastructure;
namespace $contextNamespace$.LinqQuery;

public static class $contextName$$linqQueryName$Query
{
    public static System.Func<IDynamicReadOnlyDbContext, dynamic> CreateDelegate()
    {
        
        return contextInterface =>
        {
            $contextName$ContextReadOnly context = contextInterface as $contextName$ContextReadOnly;
            var linq = $linqQueryCode$;
            return linq;
        \};
    \}
\}

