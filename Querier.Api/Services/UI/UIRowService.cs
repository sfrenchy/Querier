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
        Task<List<QPageRow>> GetRowsAsync(int pageId);
        Task<QPage> AddRowAsync(int pageId);
        Task<QPageRowVM> DeleteRowAsync(int rowId);
        Task<List<QPageRowVM>> UpdateRowOrder(QPageVM page);
    }
    public class UIRowService : IUIRowService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<UIRowService> _logger;

        public UIRowService(ILogger<UIRowService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<List<QPageRow>> GetRowsAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage page = await apidbContext.QPages.FindAsync(pageId);
                return page.QPageRows;
            }
        }

        public async Task<QPage> AddRowAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage page = await apidbContext.QPages.FindAsync(pageId);
                if (page != null)
                {
                    page.QPageRows ??= new EditableList<QPageRow>();
                    QPageRow row = new QPageRow();
                    if (page.QPageRows.Count < 1)
                    {
                        row.Order = 1;
                        page.QPageRows.Add(row);
                    }
                    else
                    {
                        List<int> listOrders = page.QPageRows.Select(r => r.Order).ToList();
                        row.Order = listOrders.Max() + 1;
                        page.QPageRows.Add(row);
                    }
                    await apidbContext.SaveChangesAsync();
                }
                return page;
            }
        }

        public async Task<QPageRowVM> DeleteRowAsync(int rowId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageRow row = await apidbContext.QPageRows.FindAsync(rowId);
                if (row.QPageCards.Count != 0)
                {
                    row.QPageCards.Clear();
                }
                QPage pageDb = await apidbContext.QPages.FindAsync(row.HAPageId);

                //deleting the row
                pageDb.QPageRows.Remove(row);

                //set up a counter to restore the order of the lines to proper
                int orderCounter = 1;

                //Browse the list with the previously deleted line
                foreach (var (r, index) in pageDb.QPageRows.Select((value, i) => (value, i)).ToList())
                {
                    //The new order is applied to each row in the list
                    r.Order = orderCounter;

                    //counter increment for the next items in the list
                    orderCounter++;
                    apidbContext.QPageRows.Update(r);
                }
                await apidbContext.SaveChangesAsync();

                return QPageRowVM.FromHAPageRow(row);
            }
        }

        public async Task<List<QPageRowVM>> UpdateRowOrder(QPageVM page)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //ordering of the list by ID as it is retrieved ordered by the field 'Order' from the front
                List<QPageRowVM> listOrdered = page.QPageRows.OrderBy(row => row.Id).ToList();
                QPage pageDb = await apidbContext.QPages.FindAsync(page.Id);

                foreach (var (row, index) in pageDb.QPageRows.Select((value, i) => (value, i)).ToList())
                {
                    //we do the treatment if there is a difference in the order
                    if (row.Order != listOrdered[index].Order)
                    {
                        //transformation of the view model by the repository model to be able to store in a database
                        QPageRow rowTransformed = new QPageRow();
                        rowTransformed = QPageRow.FromHAPageVMRow(listOrdered[index]);
                        apidbContext.QPageRows.First(r => r.Id == rowTransformed.Id).Order = rowTransformed.Order;
                    }
                }
                await apidbContext.SaveChangesAsync();
                return listOrdered;
            }
        }
    }
}
