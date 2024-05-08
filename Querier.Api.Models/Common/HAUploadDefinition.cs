using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Querier.Api.Models.Enums;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Common
{
    public class HAUploadDefinition : UIDBEntity
    {
        /// <summary>
        /// The id of upload
        /// </summary>
        [Key]
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
        /// The max size of the upload in bytes
        /// </summary>
        public long MaxSize { get; set; }
        /// <summary>
        /// The hash code to create a mechanics in the system file for the upload
        /// </summary>
        [AllowNull]
        public string Hash { get; set; }
        /// <summary>
        /// The path of the uploaded file
        /// </summary>
        [AllowNull]
        public string Path { get; set; }
        /// <summary>
        /// The size of the uploaded file in KiloByte
        /// </summary>
        [AllowNull]
        public float Size { get; set; }
        /// <summary>
        /// The nature of the uploaded file
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public HAUploadNatureEnum Nature { get; set; }

    }

    public class SimpleUploadDefinition
    {
        /// <summary>
        /// The name file of an upload
        /// </summary>
        public string FileName { get; set; }
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
        /// The max size of the upload in bytes
        /// </summary>
        public long MaxSize { get; set; }
        /// <summary>
        /// The nature of the uploaded file
        /// </summary>
        public HAUploadNatureEnum Nature { get; set; }
    }

    public class HAUploadDefinitionFromApi
    {
        public SimpleUploadDefinition Definition { get; set; }
        /// <summary>
        /// The file of the upload applied for the api
        /// </summary>
        public Stream UploadStream { get; set; }
    }

    public class HAUploadDefinitionVM
    {
        public SimpleUploadDefinition Definition { get; set; }
        /// <summary>
        /// The file of the upload applied for the view model
        /// </summary>
        [NotMapped]
        public IFormFile File { get; set; }
    }

    public class HAUploadUrl
    {
        public string Url { get; set; }
        public int FileId { get; set; }
    }
}
