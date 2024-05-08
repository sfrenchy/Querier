using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.UI
{
    public class HATheme
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Label")]
        public string Label { get; set; }
        [Column("UserId")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual ApiUser User { get;set;}
        [JsonIgnore]
        public virtual List<HAThemeVariable> HAThemeVariables { get; set; }
    }
}