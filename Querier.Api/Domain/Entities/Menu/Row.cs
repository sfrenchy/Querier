using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu;

public class Row
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int Order { get; set; }
    public double? Height { get; set; }
    public virtual Page Page { get; set; }
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}