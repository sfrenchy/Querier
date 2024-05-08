namespace Querier.Api.Models.Cards
{
    public interface IQCard
    {
        /// <summary>
        /// Define the minimum used width for the card
        /// </summary>
        public int MinWidth => 1;
        /// <summary>
        /// Define the maximum used width for the card
        /// </summary>
        public int MaxWidth => 12;
        /// <summary>
        /// Define the human label for the card
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// Define if the card is fullscreenable
        /// </summary>
        public bool AllowFullscreen => true;
        /// <summary>
        /// Define if the card is mobile view compatible
        /// </summary>
        public bool AllowMobileView => true;
        /// <summary>
        /// Define if the card is collapsable
        /// </summary>
        public bool AllowCollapse => true;
        /// <summary>
        /// Define if the card has a footer
        /// </summary>
        public bool HasFooter => false;
        /// <summary>
        /// Define if the card has some specific buttons (card buttons)
        /// </summary>
        public bool HasButton => false;
        /// <summary>
        /// Get the card configuration
        /// </summary>
        public dynamic Configuration { get; }
    }
}