using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicRow
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public int PageId { get; set; }
        public MainAxisAlignment Alignment { get; set; }
        public CrossAxisAlignment CrossAlignment { get; set; }
        public double Spacing { get; set; }

        public virtual Page Page { get; set; }
        public virtual ICollection<DynamicCard> Cards { get; set; }
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