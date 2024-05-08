namespace Querier.Api.Models.Cards
{
    public class ReportCard : IQCard
    {
        #region IAlhCardViewComponent
        public string Label => "Carte de rapport";
        public bool HasFooter => true;
        public dynamic Configuration
        {
            get
            {
                dynamic c = new System.Dynamic.ExpandoObject();
                return c;
            }
        }
        public int MinWidth => 8;
        public bool HasButton => true;
        #endregion       
    }
}