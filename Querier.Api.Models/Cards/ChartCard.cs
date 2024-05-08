namespace Querier.Api.Models.Cards
{
    public class ChartCard : IQCard
    {
        public string Label => "Carte de chart";

        public dynamic Configuration
        {
            get
            {
                dynamic c = new System.Dynamic.ExpandoObject();
                return c;
            }
        }
        public int MinWidth => 4;
        public bool HasButton => false;
        public bool HasFooter => true;
    }
}
