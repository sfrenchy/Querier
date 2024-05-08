using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Services.UI
{
    public interface IUICardService
    {
        Task<List<HAPageCard>> GetCardsAsync(int rowId);
        Task<List<HAPageCard>> AddCardAsync(AddCardRequest card);
        Task<HAPageCard> UpdateCardAsync(HAPageCard cardUpdated);
        Task<HAPageCard> DeleteCardAsync(int cardId);
        Task<List<HAPageCard>> AddPredefinedCardAsync(AddPredefinedCardRequest model);
        Task<HAPageCard> CardContentAsync(int haPageCardId);
        Task<object> SaveCardConfigurationAsync(CardDefinedConfigRequest model);
        Task<object> ExportCardConfigurationAsync(CardDefinedConfigRequest model);
        Task<List<HAPageCard>> ImportCardConfigurationAsync(CardImportConfigRequest config);
        Task<object> UpdateCardConfigurationAsync(dynamic newConfiguration);
        Task<HAPageCard> GetCardConfigurationAsync(int cardId);
        object CardMaxWidth(int cardId, int cardRowId);
        Task<List<HAPageCardDefinedConfiguration>> GetPredefinedCards();
        Task<List<HAPageCard>> UpdateCardOrder(HAPageRowVM row);

    }
    public class UICardService : IUICardService
    {
        private readonly ILogger<UICardService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IHAUploadService _uploadService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;

        public UICardService(ILogger<UICardService> logger, IDbContextFactory<ApiDbContext> contextFactory, IHAUploadService uploadService, IToastMessageEmitterService toastMessageEmitterService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
            _toastMessageEmitterService = toastMessageEmitterService;
        }

        public async Task<List<HAPageCard>> GetCardsAsync(int rowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRow row = await apidbContext.HAPageRows.FindAsync(rowId);
                return row.HAPageCards;
            }
        }

        public async Task<List<HAPageCard>> AddCardAsync(AddCardRequest card)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                int order = 0;
                HAPageRow row = apidbContext.HAPageRows.Find(card.pageRowId);
                if (row.HAPageCards.Count < 1)
                {
                    order = 1;
                }
                else
                {
                    List<int> listOrders = row.HAPageCards.Select(r => r.Order).ToList();
                    order = listOrders.Max() + 1;
                }
                HAPageCard newCard = new HAPageCard()
                {
                    CardTypeLabel = card.cardType,
                    Title = card.cardTitle,
                    Width = card.cardWidth,
                    Configuration = new
                    {
                        icon = card.icon
                    },
                    Package = card.package,
                    Order = order
                };

                row.HAPageCards.Add(newCard);
                await apidbContext.SaveChangesAsync();

                newCard.Configuration = new
                {
                    icon = card.icon,
                    cardId = newCard.Id
                };
                await apidbContext.SaveChangesAsync();

                return row.HAPageCards;
            }
        }

        public async Task<HAPageCard> UpdateCardAsync(HAPageCard cardUpdated)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCard card = await apidbContext.HAPageCards.FindAsync(cardUpdated.Id);
                if (card != null)
                {
                    card.Title = cardUpdated.Title;
                    card.Width = cardUpdated.Width;
                    card.CardConfiguration = cardUpdated.CardConfiguration;

                    await apidbContext.SaveChangesAsync();
                }

                return card;
            }
        }

        public async Task<HAPageCard> DeleteCardAsync(int cardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCard card = await apidbContext.HAPageCards.FindAsync(cardId);

                if (card != null)
                {
                    HAPageRow rowDb = await apidbContext.HAPageRows.FindAsync(card.HAPageRowId);
                    rowDb.HAPageCards.Remove(card);

                    //set up a counter to restore the order of the cards to proper
                    int orderCounter = 1;

                    //Browse the list with the previously deleted line
                    foreach (var (c, index) in rowDb.HAPageCards.Select((value, i) => (value, i)).ToList())
                    {
                        //The new order is applied to each cards in the list
                        c.Order = orderCounter;

                        //counter increment for the next items in the list
                        orderCounter++;
                        apidbContext.HAPageCards.Update(c);
                    }
                    await apidbContext.SaveChangesAsync();
                }

                return card;
            }
        }

        public async Task<List<HAPageCard>> AddPredefinedCardAsync(AddPredefinedCardRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRow row = await apidbContext.HAPageRows.FindAsync(model.pageRowId);
                HAPageCardDefinedConfiguration pConf = await apidbContext.HAPageCardDefinedConfigurations.FindAsync(model.predefinedCardId);

                int order = 0;
                if (row.HAPageCards.Count < 1)
                {
                    order = 1;
                }
                else
                {
                    List<int> listOrders = row.HAPageCards.Select(r => r.Order).ToList();
                    order = listOrders.Max() + 1;
                }
                row.HAPageCards.Add(new HAPageCard()
                {
                    CardTypeLabel = pConf.CardTypeLabel,
                    Title = pConf.Title,
                    Width = model.predefinedCardWidth,
                    CardConfiguration = pConf.CardConfiguration,
                    Package = pConf.PackageLabel,
                    Order = order
                });
                await apidbContext.SaveChangesAsync();

                return row.HAPageCards;
            }
        }

        public async Task<HAPageCard> CardContentAsync(int haPageCardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCard card = await apidbContext.HAPageCards.FindAsync(haPageCardId);

                return card;
            }
        }

        public async Task<object> SaveCardConfigurationAsync(CardDefinedConfigRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                await apidbContext.HAPageCardDefinedConfigurations.AddAsync(new HAPageCardDefinedConfiguration()
                {
                    Title = model.Title,
                    CardConfiguration = model.Config,
                    CardTypeLabel = model.CardTypeLabel,
                    PackageLabel = model.PackageLabel
                });

                await apidbContext.SaveChangesAsync();

                return new { Msg = $"The Card configuration {model.Title} has been saved" };
            }
        }

        public async Task<object> ExportCardConfigurationAsync(CardDefinedConfigRequest model)
        {
            _logger.LogInformation("Generating export card configuration");

            string bodyHash = Guid.NewGuid().ToString() + ".dat";

            byte[] content = JsonSerializer.SerializeToUtf8Bytes(model);

            HAUploadDefinitionFromApi uploadDef = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = bodyHash,
                    MimeType = "application/octet-stream",
                    DayRetention = 1,
                    Nature = HAUploadNatureEnum.CardConfiguration
                },
                UploadStream = new MemoryStream(content)
            };

            int idUpload = await _uploadService.UploadFileFromApiAsync(uploadDef);

            string downloadURL = $"api/HAUpload/GetFile/{idUpload}";

            ToastMessage exportAvailableMessage = new ToastMessage();
            exportAvailableMessage.TitleCode = "lbl-export-card-configuration-available-title";
            exportAvailableMessage.Recipient = model.RequestUserEmail;
            exportAvailableMessage.ContentCode = "lbl-export-card-configuration-available-content";
            exportAvailableMessage.ContentDownloadURL = downloadURL;
            exportAvailableMessage.ContentDownloadsFilename = $"{model.Title}.dat";
            exportAvailableMessage.Closable = true;
            exportAvailableMessage.Persistent = true;
            exportAvailableMessage.Type = ToastType.Success;
            _logger.LogInformation("Publishing export card configuration notification");
            _toastMessageEmitterService.PublishToast(exportAvailableMessage);

            return new { Msg = $"The Card configuration is available to download" };
        }

        public async Task<List<HAPageCard>> ImportCardConfigurationAsync(CardImportConfigRequest configRequest)
        {
            string json = "";
            using (StreamReader r = new StreamReader(configRequest.FilePath))
            {
                json = r.ReadToEnd();
            }
            Dictionary<string, string> dictionaryConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRow row = await apidbContext.HAPageRows.FindAsync(configRequest.PageRowId);

                row.HAPageCards.Add(new HAPageCard()
                {
                    CardTypeLabel = dictionaryConfig["CardTypeLabel"],
                    Title = dictionaryConfig["CardTitle"],
                    Width = Convert.ToInt32(dictionaryConfig["Width"]),
                    CardConfiguration = dictionaryConfig["Config"],
                    Package = dictionaryConfig["PackageLabel"]
                });

                await apidbContext.SaveChangesAsync();

                return row.HAPageCards;
            }

        }

        public async Task<object> UpdateCardConfigurationAsync(dynamic newConfiguration)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCard card = await apidbContext.HAPageCards.FindAsync(Convert.ToInt32(newConfiguration.cardId.Value));
                card.Configuration = newConfiguration;

                await apidbContext.SaveChangesAsync();

                return new { Msg = $"The Card configuration has been updated" };
            }
        }

        public async Task<HAPageCard> GetCardConfigurationAsync(int cardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCard card = await apidbContext.HAPageCards.FindAsync(cardId);

                return card;
            }
        }
        public object CardMaxWidth(int cardId, int cardRowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var maxWidth = 12 - apidbContext.HAPageCards.Where(c => c.HAPageRow.Id == cardRowId && c.Id != cardId).Sum(c => c.Width);
                return new { maxWidth };
            }
        }

        public async Task<List<HAPageCardDefinedConfiguration>> GetPredefinedCards()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAPageCardDefinedConfigurations.OrderBy(a => a.Title).ToListAsync();
            }
        }
        public async Task<List<HAPageCard>> UpdateCardOrder(HAPageRowVM row)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //ordering of the list by ID as it is retrieved ordered by the field 'Order' from the front
                List<HAPageCard> listOrdered = row.HAPageCards.OrderBy(c => c.Id).ToList();
                HAPageRow rowDB = await apidbContext.HAPageRows.FindAsync(row.Id);

                foreach (var (card, index) in rowDB.HAPageCards.Select((value, i) => (value, i)).ToList())
                {
                    //we do the treatment if there is a difference in the order
                    if (card.Order != listOrdered[index].Order)
                    {
                        //transformation of the view model by the repository model to be able to store in a database
                        apidbContext.HAPageCards.First(r => r.Id == listOrdered[index].Id).Order = listOrdered[index].Order;
                    }
                }
                await apidbContext.SaveChangesAsync();
                return listOrdered;
            }
        }
    }
}
