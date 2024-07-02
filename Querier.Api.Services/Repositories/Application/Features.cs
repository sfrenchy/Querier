using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;

namespace Querier.Api.Services.Repositories.Application;

public static class Features
{
    public static List<ApplicationFeatures> EnabledFeatures { get; } = new();
    public static string ApplicationName { get; set; } = "HerdiaApp";
    public static byte[] ApplicationIcon { get; set;}
    public static byte[] ApplicationBackgroundLogin { get; set; }
    public static string ApplicationRightPanelPackageName { get; set; }
    public static List<string> ApplicationDefaultTheme { get; set; }
    public static List<QEntityAttributeViewModel> ApplicationUserAttributes { get; set; }
    public static List<PropertyDefinition> ApplicationUserProperties { get; set; }

    public static string HerdiaAppAPIBuildId 
    { 
        get 
        {
            if (!File.Exists("BuildId"))
                File.WriteAllText("BuildId", "develop");
            return File.ReadAllText("BuildId");
        } 
    } 
}