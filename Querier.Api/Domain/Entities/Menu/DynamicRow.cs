using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicRow
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public int Order { get; set; }
        public double? Height { get; set; }
        public virtual DynamicPage Page { get; set; }
        public virtual ICollection<DynamicCard> Cards { get; set; } = new List<DynamicCard>();
    }

    public enum MainAxisAlignment
    {
        Start,
        Center,
        End,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly
    }

    public enum CrossAxisAlignment
    {
        Start,
        Center,
        End,
        Stretch,
        Baseline
    }
} 