using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Domain.Services
{
    public class EmailTemplateService(ILogger<EmailTemplateService> logger, IWebHostEnvironment env)
        : IEmailTemplateService
    {
        private readonly string _templateBasePath = Path.Combine(env.ContentRootPath, "Infrastructure", "Templates", "Email", "Templates");

        public async Task<string> GetTemplateAsync(string templateName, string language, Dictionary<string, string> parameters)
        {
            try
            {
                var templatePath = Path.Combine(_templateBasePath, templateName, $"{language}.html");
                if (!File.Exists(templatePath))
                {
                    templatePath = Path.Combine(_templateBasePath, templateName, "en.html");
                }

                var template = new Template(await File.ReadAllTextAsync(templatePath), '$', '$');
                var cssPath = Path.Combine(_templateBasePath, "..", "Styles", "email.css");
                var css = await File.ReadAllTextAsync(cssPath);
                template.Add("css", "<style>" + css + "</style>");

                foreach (var entry in parameters)
                {
                    template.Add(entry.Key.ToString(), entry.Value.ToString());
                }

                var body = template.Render();
                return body;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error loading email template {templateName}");
                throw;
            }
        }
    }
}