namespace Querier.Api.Models.Cards
{
    public class ErrorCard : IHACard
    {
        public string Label => "Ceci est une erreur";

        public dynamic Configuration => new System.Dynamic.ExpandoObject();
    }
}