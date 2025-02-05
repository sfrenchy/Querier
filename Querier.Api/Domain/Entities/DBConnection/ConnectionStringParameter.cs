using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Querier.Api.Infrastructure.Services;

namespace Querier.Api.Domain.Entities.DBConnection
{
    public class ConnectionStringParameter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DBConnectionId { get; set; }

        [Required]
        public string Key { get; set; }

        public string StoredValue { get; set; }

        public bool IsEncrypted { get; set; }

        [ForeignKey(nameof(DBConnectionId))]
        public virtual DBConnection DBConnection { get; set; }

        [NotMapped]
        public IEncryptionService EncryptionService { get; set; }

        private string GetValue()
        {
            if (!IsEncrypted || EncryptionService == null)
                return StoredValue;

            return EncryptionService.DecryptAsync(StoredValue).Result;
        }

        private void SetValue(string value)
        {
            if (!IsEncrypted || EncryptionService == null)
            {
                StoredValue = value;
                return;
            }

            StoredValue = EncryptionService.EncryptAsync(value).Result;
        }
    }
} 