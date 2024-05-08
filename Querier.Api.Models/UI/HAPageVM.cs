using Querier.Api.Models.Responses.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.UI
{
    public class HAPageVM
    {
        public int Id { get; set; }
        public List<HAPageRowVM> HAPageRows { get; set; }
        public dynamic Roles { get; set; }
        public static HAPageVM FromHAPage(HAPage page)
        {
            return new HAPageVM
            {
                Id = page.Id,
                HAPageRows = page.HAPageRows.OrderBy(r => r.Order).Select(r => HAPageRowVM.FromHAPageRow(r)).ToList(),
                Roles = page.HAPageRoles.Where(r => r.View == true).Select(r => new { Id = r.ApiRoleId }).ToList()
            };
        }
    }
}
