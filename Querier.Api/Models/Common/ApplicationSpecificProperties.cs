using Querier.Api.Models.Auth;
using System.Collections.Generic;
using Querier.Api.Models.Enums;

namespace Querier.Api.Models.Common
{
    public class ApplicationSpecificProperties
    {
        /// <summary>
        /// The List of the feature of the application
        /// </summary>
        public List<ApplicationFeatures> Features { get; set; }

        /// <summary>
        /// The title of the application
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The icon of the application
        /// </summary>
        public byte[] Icon { get; set; }

        /// <summary>
        /// the background of the login page of the application
        /// </summary>
        public byte[] BackgroundLogin { get; set; }

        /// <summary>
        /// Required dynamic contexts for this application to work properly
        /// </summary>
        public List<string> RequiredDynamicContexts { get; set; }
    }
}
