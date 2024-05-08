using System;

namespace Querier.Api.Models.Requests
{
    public class AddFileRequest
    {
        public int Id { get; set; }
        /// <summary>
        /// The name file of an upload
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// The date of upload
        /// </summary>
        public DateTime DateUpload { get; set; }
        /// <summary>
        /// The upload deadline
        /// </summary>
        public int DayRetention { get; set; }
        /// <summary>
        /// Whether the upload contains sensitive data
        /// </summary>
        public bool SensitiveData { get; set; }
        /// <summary>
        /// The type mime of the file uploaded
        /// </summary>
        public string MimeType { get; set; }
        /// <summary>
        /// The max size of the upload
        /// </summary>
        public int MaxSize { get; set; }
    }
}
