namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for page translations in different languages
    /// </summary>
    public class TranslatableStringDto
    {
        /// <summary>
        /// Language code for the translation (e.g., 'en', 'fr', 'es')
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Value of the page in the specified language
        /// </summary>
        public string Value { get; set; }
    }
}