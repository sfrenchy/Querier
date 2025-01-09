namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for page translations in different languages
    /// </summary>
    public class PageTranslationDto
    {
        /// <summary>
        /// Language code for the translation (e.g., 'en', 'fr', 'es')
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Translated name of the page in the specified language
        /// </summary>
        public string Name { get; set; }
    }
}