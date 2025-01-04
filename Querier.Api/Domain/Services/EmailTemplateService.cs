using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Antlr4.StringTemplate;

namespace Querier.Api.Domain.Services
{
    public interface IEmailTemplateService
    {
        Task<string> GetTemplateAsync(string templateName, string language, Dictionary<string, string> parameters);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly string _templateBasePath;

        public EmailTemplateService(ILogger<EmailTemplateService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _templateBasePath = Path.Combine(env.ContentRootPath, "Infrastructure", "Templates", "Email", "Templates");
        }

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
                _logger.LogError(ex, $"Error loading email template {templateName}");
                throw;
            }
        }
    }
}