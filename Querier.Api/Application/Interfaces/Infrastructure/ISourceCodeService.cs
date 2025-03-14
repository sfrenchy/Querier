using Microsoft.CodeAnalysis;
using Querier.Api.Domain.Common.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Infrastructure;

public interface ISourceCodeService
{
    Task<IEnumerable<SyntaxTree>> GenerateDbConnectionSourcesAsync();
}
