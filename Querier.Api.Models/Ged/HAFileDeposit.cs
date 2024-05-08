using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Querier.Api.Models.Attributes;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Ged
{
    public class HAFileDeposit : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Required]
        [Column("Enable")]
        public bool Enable { get; set; }
        [Required]
        [Column("Label")]
        public string Label { get; set; }
        [Required]
        [Column("Filter")]
        [JsonString]
        public string Filter { get; set; }

        [NotMapped]
        public List<string> ConfigurationFilter
        {
            get => JsonConvert.DeserializeObject<List<string>>(Filter ?? "");
            set => Filter = JsonConvert.SerializeObject(value);
        }
        [Required]
        [Column("Type")]
        public TypeFileDepositEnum Type { get; set; }
        [Required]
        [Column("Login")]
        public string Login { get; set; }
        [Required]
        [Column("Password")]
        public string Password { get; set; }
        [Required]
        [Column("Auth")]
        public AuthFileDepositEnum Auth { get; set; }
        [Column("Host")]
        public string Host { get; set; }
        [Column("Port")]
        public int Port { get; set; }
        [Column("RootPath")]
        public string RootPath { get; set; }
        [Required]
        [Column("Tag")]
        public string Tag { get; set; }
        [Required]
        [Column("Capabilities")]
        public CapabilitiesEnum Capabilities { get; set; }
    }
}
