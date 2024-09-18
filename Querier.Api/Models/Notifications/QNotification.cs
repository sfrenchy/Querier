using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.Notifications
{
    public class QNotification
    {
        [Key]
        [Required]
        public string Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string JsonContent { get; set; }
        [JsonIgnore]
        public virtual ApiUser User { get; set; }
	}
}

