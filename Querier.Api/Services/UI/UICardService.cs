using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Services.UI
{
    public interface IUICardService
    {
        Task<List<QPageCard>> GetCardsAsync(int rowId);
        Task<List<QPageCard>> AddCardAsync(AddCardRequest card);
        Task<QPageCard> UpdateCardAsync(QPageCard cardUpdated);
        Task<QPageCard> DeleteCardAsync(int cardId);
        Task<List<QPageCard>> AddPredefinedCardAsync(AddPredefinedCardRequest model);
        Task<QPageCard> CardContentAsync(int haPageCardId);
        Task<object> SaveCardConfigurationAsync(CardDefinedConfigRequest model);
        Task<object> ExportCardConfigurationAsync(CardDefinedConfigRequest model);
        Task<List<QPageCard>> ImportCardConfigurationAsync(CardImportConfigRequest config);
        Task<object> UpdateCardConfigurationAsync(dynamic newConfiguration);
        Task<QPageCard> GetCardConfigurationAsync(int cardId);
        object CardMaxWidth(int cardId, int cardRowId);
        Task<List<QPageCardDefinedConfiguration>> GetPredefinedCards();
        Task<List<QPageCard>> UpdateCardOrder(QPageRowVM row);

    }
    public class UICardService : IUICardService
    {
        private readonly ILogger<UICardService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly Models.Interfaces.IQUploadService _uploadService;

        public UICardService(ILogger<UICardService> logger, IDbContextFactory<ApiDbContext> contextFactory, Models.Interfaces.IQUploadService uploadService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
        }

        public async Task<List<QPageCard>> GetCardsAsync(int rowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageRow row = await apidbContext.QPageRows.FindAsync(rowId);
                return row.QPageCards;
            }
        }

        public async Task<List<QPageCard>> AddCardAsync(AddCardRequest card)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                int order = 0;
                QPageRow row = apidbContext.QPageRows.Find(card.pageRowId);
                if (row.QPageCards.Count < 1)
                {
                    order = 1;
                }
                else
                {
                    List<int> listOrders = row.QPageCards.Select(r => r.Order).ToList();
                    order = listOrders.Max() + 1;
                }
                QPageCard newCard = new QPageCard()
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

                row.QPageCards.Add(newCard);
                await apidbContext.SaveChangesAsync();

                newCard.Configuration = new
                {
                    icon = card.icon,
                    cardId = newCard.Id
                };
                await apidbContext.SaveChangesAsync();

                return row.QPageCards;
            }
        }

        public async Task<QPageCard> UpdateCardAsync(QPageCard cardUpdated)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCard card = await apidbContext.QPageCards.FindAsync(cardUpdated.Id);
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

        public async Task<QPageCard> DeleteCardAsync(int cardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCard card = await apidbContext.QPageCards.FindAsync(cardId);

                if (card != null)
                {
                    QPageRow rowDb = await apidbContext.QPageRows.FindAsync(card.HAPageRowId);
                    rowDb.QPageCards.Remove(card);

                    //set up a counter to restore the order of the cards to proper
                    int orderCounter = 1;

                    //Browse the list with the previously deleted line
                    foreach (var (c, index) in rowDb.QPageCards.Select((value, i) => (value, i)).ToList())
                    {
                        //The new order is applied to each cards in the list
                        c.Order = orderCounter;

                        //counter increment for the next items in the list
                        orderCounter++;
                        apidbContext.QPageCards.Update(c);
                    }
                    await apidbContext.SaveChangesAsync();
                }

                return card;
            }
        }

        public async Task<List<QPageCard>> AddPredefinedCardAsync(AddPredefinedCardRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageRow row = await apidbContext.QPageRows.FindAsync(model.pageRowId);
                QPageCardDefinedConfiguration pConf = await apidbContext.QPageCardDefinedConfigurations.FindAsync(model.predefinedCardId);

                int order = 0;
                if (row.QPageCards.Count < 1)
                {
                    order = 1;
                }
                else
                {
                    List<int> listOrders = row.QPageCards.Select(r => r.Order).ToList();
                    order = listOrders.Max() + 1;
                }
                row.QPageCards.Add(new QPageCard()
                {
                    CardTypeLabel = pConf.CardTypeLabel,
                    Title = pConf.Title,
                    Width = model.predefinedCardWidth,
                    CardConfiguration = pConf.CardConfiguration,
                    Package = pConf.PackageLabel,
                    Order = order
                });
                await apidbContext.SaveChangesAsync();

                return row.QPageCards;
            }
        }

        public async Task<QPageCard> CardContentAsync(int haPageCardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCard card = await apidbContext.QPageCards.FindAsync(haPageCardId);

                return card;
            }
        }

        public async Task<object> SaveCardConfigurationAsync(CardDefinedConfigRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                await apidbContext.QPageCardDefinedConfigurations.AddAsync(new QPageCardDefinedConfiguration()
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
                    Nature = QUploadNatureEnum.CardConfiguration
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
            //_toastMessageEmitterService.PublishToast(exportAvailableMessage);

            return new { Msg = $"The Card configuration is available to download" };
        }

        public async Task<List<QPageCard>> ImportCardConfigurationAsync(CardImportConfigRequest configRequest)
        {
            string json = "";
            using (StreamReader r = new StreamReader(configRequest.FilePath))
            {
                json = r.ReadToEnd();
            }
            Dictionary<string, string> dictionaryConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageRow row = await apidbContext.QPageRows.FindAsync(configRequest.PageRowId);

                row.QPageCards.Add(new QPageCard()
                {
                    CardTypeLabel = dictionaryConfig["CardTypeLabel"],
                    Title = dictionaryConfig["CardTitle"],
                    Width = Convert.ToInt32(dictionaryConfig["Width"]),
                    CardConfiguration = dictionaryConfig["Config"],
                    Package = dictionaryConfig["PackageLabel"]
                });

                await apidbContext.SaveChangesAsync();

                return row.QPageCards;
            }

        }

        public async Task<object> UpdateCardConfigurationAsync(dynamic newConfiguration)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCard card = await apidbContext.QPageCards.FindAsync(Convert.ToInt32(newConfiguration.cardId.Value));
                card.Configuration = newConfiguration;

                await apidbContext.SaveChangesAsync();

                return new { Msg = $"The Card configuration has been updated" };
            }
        }

        public async Task<QPageCard> GetCardConfigurationAsync(int cardId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCard card = await apidbContext.QPageCards.FindAsync(cardId);

                return card;
            }
        }
        public object CardMaxWidth(int cardId, int cardRowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var maxWidth = 12 - apidbContext.QPageCards.Where(c => c.QPageRow.Id == cardRowId && c.Id != cardId).Sum(c => c.Width);
                return new { maxWidth };
            }
        }

        public async Task<List<QPageCardDefinedConfiguration>> GetPredefinedCards()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.QPageCardDefinedConfigurations.OrderBy(a => a.Title).ToListAsync();
            }
        }
        public async Task<List<QPageCard>> UpdateCardOrder(QPageRowVM row)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //ordering of the list by ID as it is retrieved ordered by the field 'Order' from the front
                List<QPageCard> listOrdered = row.QPageCards.OrderBy(c => c.Id).ToList();
                QPageRow rowDB = await apidbContext.QPageRows.FindAsync(row.Id);

                foreach (var (card, index) in rowDB.QPageCards.Select((value, i) => (value, i)).ToList())
                {
                    //we do the treatment if there is a difference in the order
                    if (card.Order != listOrdered[index].Order)
                    {
                        //transformation of the view model by the repository model to be able to store in a database
                        apidbContext.QPageCards.First(r => r.Id == listOrdered[index].Id).Order = listOrdered[index].Order;
                    }
                }
                await apidbContext.SaveChangesAsync();
                return listOrdered;
            }
        }
    }
}
