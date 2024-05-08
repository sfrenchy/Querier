using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Querier.Api.Models.UI
{
    public class HAThemeVariable
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("VariableName")]
        public string VariableName { get; set; }
        [Column("VariableValue")]
        public string VariableValue { get; set; }
        [Column("ThemeId")]
        public int HAThemeId { get; set; }
        [JsonIgnore]
        public virtual HATheme HATheme { get; set; }
    }
}
