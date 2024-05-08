using System;
using Querier.Api.Models.Responses;

namespace Querier.Api.Models.Interfaces
{
	public interface IQClientTranslation
	{
        public HAGetTranslationsResponse GetTranslations();
    }
}

