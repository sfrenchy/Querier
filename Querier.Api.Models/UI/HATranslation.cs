using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Models.UI
{
    public class HATranslation : UIDBEntity
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; }
        public string EnLabel { get; set; }
        public string FrLabel { get; set; }
        public string DeLabel { get; set; }
        public string Context { get; set; }
    }
}
