using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for resetting a user's password
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// New password for the user account
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Confirmation of the new password (must match Password)
        /// </summary>
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// Email address of the user requesting the password reset
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password reset token received by the user
        /// </summary>
        public string Token { get; set; }
    }
}
