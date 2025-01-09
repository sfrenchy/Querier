using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for a card component in the page layout
    /// </summary>
    public class CardDto
    {
        /// <summary>
        /// Unique identifier of the card
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Dictionary of localized titles for the card, where key is the language code
        /// </summary>
        public Dictionary<string, string> Titles { get; set; }

        /// <summary>
        /// Display order of the card within its row
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Type of card component (e.g., 'chart', 'table', 'text')
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Width of the card in grid units
        /// </summary>
        public int GridWidth { get; set; }

        /// <summary>
        /// Card-specific configuration object based on the card type
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Optional background color of the card body (in RGBA format)
        /// </summary>
        public uint? BackgroundColor { get; set; }

        /// <summary>
        /// Optional text color of the card body (in RGBA format)
        /// </summary>
        public uint? TextColor { get; set; }

        /// <summary>
        /// Optional background color of the card header (in RGBA format)
        /// </summary>
        public uint? HeaderBackgroundColor { get; set; }

        /// <summary>
        /// Optional text color of the card header (in RGBA format)
        /// </summary>
        public uint? HeaderTextColor { get; set; }
    }
} 