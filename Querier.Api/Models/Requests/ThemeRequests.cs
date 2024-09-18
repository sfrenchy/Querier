using Quartz.Impl.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Requests
{
    public class UpdateThemeRequest
    {
        public string Label { get; set; }
        public string PrimaryValue { get; set; }
        public string SecondaryValue { get; set; }
        public string customFontSize { get; set; }
        public string navbarValue { get; set; }
        public string topNavbarValue { get; set; }
    }
}
