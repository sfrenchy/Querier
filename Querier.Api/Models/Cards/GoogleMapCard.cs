﻿namespace Querier.Api.Models.Cards
{
    public class GoogleMapCard : IQCard
    {
        #region IAlhCardViewComponent

        public string Label => "Carte Google Map";

        public dynamic Configuration
        {
            get
            {
                dynamic c = new System.Dynamic.ExpandoObject();
                return c;
            }
        }

        #endregion
    }
}
