using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;

namespace Querier.Api.Models
{
    public class AttachmentTypeProperty
    {
        [Required]
        public Stream contentStream { get; set; }

        [Required]
        public string contentType { get; set; }

        [Required]
        public string fileName{ get; set; }
    }
}
