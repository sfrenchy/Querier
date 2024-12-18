using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Menu.Requests
{
    public class UpdateDynamicPageRequest
    {
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string[] Roles { get; set; }
        public string Route { get; set; }
        public int DynamicMenuCategoryId { get; set; }
        public List<DynamicPageTranslationDto> Translations { get; set; }
    }

    public class DynamicPageTranslationDto
    {
        public string LanguageCode { get; set; }
        public string Name { get; set; }
    }
}