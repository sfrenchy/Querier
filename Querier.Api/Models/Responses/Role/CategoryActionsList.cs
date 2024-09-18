using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role;

public class CategoryActionsList
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public List<CategoryActions> Actions { get; set; }
    public List<PageActionsList> Pages { get; set; }
}
