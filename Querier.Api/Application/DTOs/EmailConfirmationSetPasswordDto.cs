namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for setting a password during email confirmation
    /// </summary>
    public class EmailConfirmationSetPasswordDto
    {
        /// <summary>
        /// Email confirmation token received by the user
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Email address being confirmed
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// New password to set for the user account
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Confirmation of the new password (must match Password)
        /// </summary>
        public string ConfirmPassword { get; set; }
    }
}