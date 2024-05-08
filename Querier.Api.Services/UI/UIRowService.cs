using Castle.Components.DictionaryAdapter;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Spreadsheet;
using Querier.Api.Models;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;

namespace Querier.Api.Services.UI
{
    public interface IUIRowService
    {
        Task<List<HAPageRow>> GetRowsAsync(int pageId);
        Task<HAPage> AddRowAsync(int pageId);
        Task<HAPageRowVM> DeleteRowAsync(int rowId);
        Task<List<HAPageRowVM>> UpdateRowOrder(HAPageVM page);

    }
    public class UIRowService : IUIRowService
    {
        private readonly ILogger<UIRowService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public UIRowService(ILogger<UIRowService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<List<HAPageRow>> GetRowsAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage page = await apidbContext.HAPages.FindAsync(pageId);
                return page.HAPageRows;
            }
        }

        public async Task<HAPage> AddRowAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage page = await apidbContext.HAPages.FindAsync(pageId);
                if (page != null)
                {
                    page.HAPageRows ??= new EditableList<HAPageRow>();
                    HAPageRow row = new HAPageRow();
                    if (page.HAPageRows.Count < 1)
                    {
                        row.Order = 1;
                        page.HAPageRows.Add(row);
                    }
                    else
                    {
                        List<int> listOrders = page.HAPageRows.Select(r => r.Order).ToList();
                        row.Order = listOrders.Max() + 1;
                        page.HAPageRows.Add(row);
                    }
                    await apidbContext.SaveChangesAsync();
                }
                return page;
            }
        }

        public async Task<HAPageRowVM> DeleteRowAsync(int rowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRow row = await apidbContext.HAPageRows.FindAsync(rowId);
                if (row.HAPageCards.Count != 0)
                {
                    row.HAPageCards.Clear();
                }
                HAPage pageDb = await apidbContext.HAPages.FindAsync(row.HAPageId);

                //deleting the row
                pageDb.HAPageRows.Remove(row);

                //set up a counter to restore the order of the lines to proper
                int orderCounter = 1;

                //Browse the list with the previously deleted line
                foreach (var (r, index) in pageDb.HAPageRows.Select((value, i) => (value, i)).ToList())
                {
                    //The new order is applied to each row in the list
                    r.Order = orderCounter;

                    //counter increment for the next items in the list
                    orderCounter++;
                    apidbContext.HAPageRows.Update(r);
                }
                await apidbContext.SaveChangesAsync();

                return HAPageRowVM.FromHAPageRow(row);
            }
        }

        public async Task<List<HAPageRowVM>> UpdateRowOrder(HAPageVM page)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //ordering of the list by ID as it is retrieved ordered by the field 'Order' from the front
                List<HAPageRowVM> listOrdered = page.HAPageRows.OrderBy(row => row.Id).ToList();
                HAPage pageDb = await apidbContext.HAPages.FindAsync(page.Id);

                foreach (var (row, index) in pageDb.HAPageRows.Select((value, i) => (value, i)).ToList())
                {
                    //we do the treatment if there is a difference in the order
                    if (row.Order != listOrdered[index].Order)
                    {
                        //transformation of the view model by the repository model to be able to store in a database
                        HAPageRow rowTransformed = new HAPageRow();
                        rowTransformed = HAPageRow.FromHAPageVMRow(listOrdered[index]);
                        apidbContext.HAPageRows.First(r => r.Id == rowTransformed.Id).Order = rowTransformed.Order;
                    }
                }
                await apidbContext.SaveChangesAsync();
                return listOrdered;
            }
        }
    }
}
