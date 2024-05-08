namespace Querier.Api.Models.Cards
{
    public class LabelCard : IQCard
    {
        #region IAlhCardViewComponent
        public string Label => "Carte de libellé";

        public dynamic Configuration
        {
            get
            {
                dynamic c = new System.Dynamic.ExpandoObject();
                c.Label = "Libellé par défaut";
                return c;
            }
        }
        #endregion
    }
}
