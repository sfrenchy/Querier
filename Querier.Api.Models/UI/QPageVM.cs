using Querier.Api.Models.Responses.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.UI
{
    public class QPageVM
    {
        public int Id { get; set; }
        public List<QPageRowVM> QPageRows { get; set; }
        public dynamic Roles { get; set; }
        public static QPageVM FromHAPage(QPage page)
        {
            return new QPageVM
            {
                Id = page.Id,
                QPageRows = page.QPageRows.OrderBy(r => r.Order).Select(r => QPageRowVM.FromHAPageRow(r)).ToList(),
                Roles = page.QPageRoles.Where(r => r.View == true).Select(r => new { Id = r.ApiRoleId }).ToList()
            };
        }
    }
}
