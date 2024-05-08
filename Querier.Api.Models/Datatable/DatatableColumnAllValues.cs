using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Datatable
{
    public class DatatableColumnAllValues
    {
        public DatatableColumn Column { get; set; }
        public List<string> Values { get; set; }
    }
}
