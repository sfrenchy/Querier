using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Querier.Api.Models.Attributes;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Ged
{
    public class QFilesFromFileDeposit : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("FileRef")]
        public string FileRef { get; set; }

        [Column("FileInformation")]
        [Required]
        [JsonString]
        public string FileInformation { get; set; }

        [Column("QFileDepositId")]
        public int QFileDepositId { get; set; }

        [JsonIgnore]
        public virtual QFileDeposit QFileDeposit { get; set; }

        public T GetConfiguration<T>()
        {
            return JsonConvert.DeserializeObject<T>(FileInformation ?? "");
        }

        public void SetConfiguration<T>(T val)
        {
            FileInformation = JsonConvert.SerializeObject(val);
        }
    }

    public class ConfigurationFileSystem
    {
        //public string FilePath { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateModification { get; set; }
        public DateTime LastAcces { get; set; }
    }

    public class ConfigurationDocuware
    {
        public string Title { get; set; }
        public DateTime StoredDate { get; set; }
        public DateTime DateModification { get; set; }
    }
}

