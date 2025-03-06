using System.Collections.Generic;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators;

public class SqliteDatabaseMetadataProvider : DatabaseMetadataProviderBase, IDatabaseMetadataProvider
{
    public List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString)
    {
        return new List<StoredProcedureMetadata>();
    }
}