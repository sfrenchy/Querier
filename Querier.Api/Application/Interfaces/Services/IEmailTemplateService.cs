using System.Collections.Generic;
using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Services;

public interface IEmailTemplateService
{
    Task<string> GetTemplateAsync(string templateName, string language, Dictionary<string, string> parameters);
}