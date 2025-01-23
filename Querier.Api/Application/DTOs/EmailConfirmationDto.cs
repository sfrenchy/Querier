using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for email confirmation process
    /// </summary>
    public class EmailConfirmationDto
    {
        /// <summary>
        /// Required confirmation token sent to the user's email
        /// </summary>
        [Required]
        public string Token { get; set; }

        /// <summary>
        /// Required email address to confirm
        /// </summary>
        [Required]
        public string Email { get; set; }
    }
}
