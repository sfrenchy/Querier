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
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly string _templateBasePath;

        public EmailTemplateService(ILogger<EmailTemplateService> logger, IWebHostEnvironment env)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (env == null) throw new ArgumentNullException(nameof(env));

            _templateBasePath = Path.Combine(env.ContentRootPath, "Infrastructure", "Templates", "Email", "Templates");
            if (!Directory.Exists(_templateBasePath))
            {
                throw new DirectoryNotFoundException($"Template directory not found: {_templateBasePath}");
            }
        }

        public async Task<string> GetTemplateAsync(string templateName, string language, Dictionary<string, string> parameters)
        {
            try
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    _logger.LogError("Template name is null or empty");
                    throw new ArgumentException("Template name is required", nameof(templateName));
                }

                if (parameters == null)
                {
                    _logger.LogError("Parameters dictionary is null");
                    throw new ArgumentNullException(nameof(parameters));
                }

                _logger.LogInformation("Loading template {TemplateName} for language {Language}", templateName, language);

                var templatePath = Path.Combine(_templateBasePath, templateName, $"{language}.html");
                if (!File.Exists(templatePath))
                {
                    _logger.LogWarning("Template not found for language {Language}, falling back to English", language);
                    templatePath = Path.Combine(_templateBasePath, templateName, "en.html");
                    
                    if (!File.Exists(templatePath))
                    {
                        _logger.LogError("Template {TemplateName} not found for any language", templateName);
                        throw new FileNotFoundException($"Template {templateName} not found");
                    }
                }

                _logger.LogDebug("Reading template file: {TemplatePath}", templatePath);
                var templateContent = await File.ReadAllTextAsync(templatePath);
                var template = new Template(templateContent, '$', '$');

                var cssPath = Path.Combine(_templateBasePath, "..", "Styles", "email.css");
                if (!File.Exists(cssPath))
                {
                    _logger.LogWarning("CSS file not found: {CssPath}", cssPath);
                }
                else
                {
                    _logger.LogDebug("Adding CSS styles to template");
                    var css = await File.ReadAllTextAsync(cssPath);
                    template.Add("css", "<style>" + css + "</style>");
                }

                _logger.LogDebug("Adding {Count} parameters to template", parameters.Count);
                foreach (var entry in parameters)
                {
                    template.Add(entry.Key, entry.Value);
                }

                var body = template.Render();
                _logger.LogInformation("Successfully rendered template {TemplateName}", templateName);
                return body;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error processing template {TemplateName} for language {Language}", templateName, language);
                throw;
            }
        }
    }
}