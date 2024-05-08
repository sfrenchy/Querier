using System;
using System.Collections.Generic;

namespace Querier.Api.Models.Responses
{
	public class HAGetTranslationsResponse
    {
		public Dictionary<string, string> EN { get; set; }
		public Dictionary<string, string> FR { get; set; }
		public Dictionary<string, string> DE { get; set; }
	}
}

