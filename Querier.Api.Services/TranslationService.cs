using Querier.Api.Models;
using Querier.Api.Models.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Services
{
    public interface ITranslationService
    {
        dynamic GetTranslations(string languageCode);
        QTranslation CreateTranslation(CreateOrUpdateTranslationRequest request);
        QTranslation UpdateTranslation(CreateOrUpdateTranslationRequest request);
    }
    public class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public TranslationService(IDbContextFactory<ApiDbContext> contextFactory, ILogger<TranslationService> logger)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }
        public dynamic GetTranslations(string languageCode)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                string columnNameLanguage = "";
                var propertiesTranslation = new QTranslation().GetType().GetProperties();
                foreach (var property in propertiesTranslation)
                {
                    if (property.Name.Contains(languageCode))
                    {
                        columnNameLanguage = property.Name;
                    }
                }

                dynamic translationsLanguage;

                if (columnNameLanguage != "")
                {
                    translationsLanguage = apidbContext.HATranslations.Select(haTranslation => new
                    {
                        Id = haTranslation.Id,
                        Code = haTranslation.Code,
                        Label = haTranslation.GetType().GetProperty(columnNameLanguage).GetValue(haTranslation).ToString()
                    });
                }
                else
                {
                    translationsLanguage = apidbContext.HATranslations.Select(haTranslation => new
                    {
                        Id = haTranslation.Id,
                        Code = haTranslation.Code,
                        Label = haTranslation.GetType().GetProperty("EnLabel").GetValue(haTranslation).ToString()
                    });
                }
                return translationsLanguage;
            }
        }

        public QTranslation CreateTranslation(CreateOrUpdateTranslationRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QTranslation newTranslation = new()
                {
                    Code = request.Code,
                    Context = request.Context
                };
                string propertyLabel = "EnLabel";
                switch (request.LanguageCode.Substring(0, 2).ToLower())
                {
                    case "fr":
                        propertyLabel = "FrLabel";
                        break;
                    case "de":
                        propertyLabel = "DeLabel";
                        break;
                }

                typeof(QTranslation).GetProperty(propertyLabel)?.SetValue(newTranslation, request.Value);

                apidbContext.HATranslations.Add(newTranslation);
                apidbContext.SaveChanges();
                return newTranslation;
            }
        }

        public QTranslation UpdateTranslation(CreateOrUpdateTranslationRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QTranslation existingTranslation = apidbContext.HATranslations.First(t => t.Code == request.Code);
                string propertyLabel = "EnLabel";
                switch (request.LanguageCode.Substring(0, 2).ToLower())
                {
                    case "fr":
                        propertyLabel = "FrLabel";
                        break;
                    case "de":
                        propertyLabel = "DeLabel";
                        break;
                }

                typeof(QTranslation).GetProperty(propertyLabel)?.SetValue(existingTranslation, request.Value);
                apidbContext.SaveChanges();
                return existingTranslation;
            }
        }
    }
}
