using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Querier.Api.Models.UI;
using Querier.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services
{
    public interface IHATranslationService
    {
        HAGetTranslationsSignatureResponse GetSignature();
        HAGetTranslationsResponse GetTranslations();
        void UpdateTranslation(HAUpdateTranslationRequest request);
        bool UpdateGlobalTranslation(HAUpdateGlobalTranslationRequest request);

        public List<HATranslation> GetTranslationTable();
    }

    public class HATranslationService : IHATranslationService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<HATranslationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HATranslationService(IDbContextFactory<ApiDbContext> contextFactory, IServiceProvider serviceProvider, ILogger<HATranslationService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public List<HATranslation> GetTranslationTable()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return apidbContext.HATranslations.ToList();
            }
        }
        public HAGetTranslationsResponse GetTranslations()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAGetTranslationsResponse result = new HAGetTranslationsResponse();
                result.DE = new Dictionary<string, string>();
                result.EN = new Dictionary<string, string>();
                result.FR = new Dictionary<string, string>();

                result.DE = apidbContext.HATranslations.ToDictionary(t => t.Code, t => t.DeLabel);
                result.EN = apidbContext.HATranslations.ToDictionary(t => t.Code, t => t.EnLabel);
                result.FR = apidbContext.HATranslations.ToDictionary(t => t.Code, t => t.FrLabel);

                if (Repositories.Application.Features.EnabledFeatures.Contains(Querier.Api.Models.Enums.ApplicationFeatures.OwnTranslation))
                {
                    IHAClientTranslation clientTranslationService = (IHAClientTranslation)_serviceProvider.GetService(typeof(IHAClientTranslation));
                    if (clientTranslationService != null)
                    {
                        var clientTranslations = clientTranslationService.GetTranslations();
                        foreach (var frClientTranslation in clientTranslations.FR)
                        {
                            if (!result.FR.ContainsKey(frClientTranslation.Key))
                                result.FR.Add(frClientTranslation.Key, frClientTranslation.Value);
                        }

                        foreach (var enClientTranslation in clientTranslations.EN)
                        {
                            if (!result.EN.ContainsKey(enClientTranslation.Key))
                                result.EN.Add(enClientTranslation.Key, enClientTranslation.Value);
                        }

                        foreach (var deClientTranslation in clientTranslations.DE)
                        {
                            if (!result.DE.ContainsKey(deClientTranslation.Key))
                                result.DE.Add(deClientTranslation.Key, deClientTranslation.Value);
                        }
                    }
                }

                return result;
            }
        }

        public HAGetTranslationsSignatureResponse GetSignature()
        {
            return new HAGetTranslationsSignatureResponse() { Signature = GetTranslations().GetSHA1Hash() };
        }

        public void UpdateTranslation(HAUpdateTranslationRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {


                var translation = apidbContext.HATranslations.FirstOrDefault(t => t.Code == request.Code);
                if (translation == null)
                {
                    translation = new Querier.Api.Models.UI.HATranslation()
                    {
                        Code = request.Code,
                        FrLabel = request.Language.ToLower().Contains("fr") ? request.Value : null,
                        EnLabel = request.Language.ToLower().Contains("en") ? request.Value : null,
                        DeLabel = request.Language.ToLower().Contains("de") ? request.Value : null
                    };
                    apidbContext.HATranslations.Add(translation);
                }
                else
                {
                    if (request.Language.ToLower().Contains("fr"))
                        translation.FrLabel = request.Value;
                    if (request.Language.ToLower().Contains("en"))
                        translation.EnLabel = request.Value;
                    if (request.Language.ToLower().Contains("de"))
                        translation.DeLabel = request.Value;
                }

                apidbContext.SaveChanges();
            }
        }

        public bool UpdateGlobalTranslation(HAUpdateGlobalTranslationRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var translation = apidbContext.HATranslations.FirstOrDefault(t => t.Code == request.Code);
                if (translation != null)
                {
                    translation.EnLabel = request.EnLabel;
                    translation.FrLabel = request.FrLabel;
                    translation.DeLabel = request.DeLabel;
                    translation.Context = request.Context;

                    apidbContext.SaveChanges();
                    return true;
                }

                return false;
            }
        }
    }
}

