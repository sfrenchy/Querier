namespace Querier.Api.Models.Cards
{
    public class ErrorCard : IQCard
    {
        public string Label => "Ceci est une erreur";

        public dynamic Configuration => new System.Dynamic.ExpandoObject();
    }
}