using System;
using Querier.Api.Models.Responses;

namespace Querier.Api.Models.Interfaces
{
	public interface IHAClientTranslation
	{
        public HAGetTranslationsResponse GetTranslations();
    }
}

