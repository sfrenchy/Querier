using Querier.Api.Models.Auth;
using System.Collections.Generic;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;

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
        /// <summary>
        /// The default theme of the application
        /// </summary>
        public List<string> ApplicationDefaultTheme { get; set; }
        /// <summary>
        /// Required for the applications that need a right sidebar
        /// </summary>
        public string RightPanelPackageName { get; set; }
        /// <summary>
        /// A list of <see cref="HAEntityAttributeViewModel"/> that the application needs for it's AspNetUsers
        /// </summary>
        public List<HAEntityAttributeViewModel> ApplicationUserAttributes { get; set; }
        /// <summary>
        /// A list of <see cref="PropertyDefinition"/> that the application needs for a specific user table
        /// </summary>
        public List<PropertyDefinition> ApplicationUserProperties { get; set; }
    }
}
