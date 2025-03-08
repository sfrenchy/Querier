using System.Collections.Generic;
using System.Data.Common;
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators;

public interface IDatabaseMetadataProvider
{
    List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString);
}