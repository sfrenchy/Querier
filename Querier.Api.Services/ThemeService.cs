using DocumentFormat.OpenXml.Office.CustomUI;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;

namespace Querier.Api.Services
{
    public interface IThemeService
    {
        /// <summary>
        /// This method is used to get the theme list of a specific User
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns> A list of <see cref="HATheme"/> </returns>
        public List<HATheme> GetUserThemeList(string UserId);
        /// <summary>
        /// This method is used to get the content of a specific theme
        /// </summary>
        /// <param name="ThemeId"></param>
        /// <returns>A list of <see cref="HAThemeVariable" /> objects</returns>
        public List<HAThemeVariable> GetThemeDefinition(int ThemeId);
        /// <summary>
        /// This method is used to update the values of a specific theme 
        /// </summary>
        /// <param name="ThemeId"
        /// <param name="TargetTheme"></param>
        /// <returns> Return true if it worked, will return false if it didn't</returns>
        public bool UpdateThemeVariableValues(int ThemeId, UpdateThemeRequest TargetTheme);
       /// <summary>
       /// This method is used to get the Id of a theme based on the User's Id and the theme label
       /// </summary>
       /// <param name="Label"></param>
       /// <param name="UserId"></param>
       /// <returns> The theme Id</returns>
        public int GetThemeId(string Label, string UserId);
        /// <summary>
        /// This method is used to create the default theme and affect it to a User
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns>The number of entries added in the "HAThemeVariables" database table</returns>
        public int CreateDefaultTheme(string UserId);
    }
    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public ThemeService(ILogger<ThemeService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public List<HATheme> GetUserThemeList(string UserId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                List<HATheme> result = new List<HATheme>();
                result = apidbContext.HAThemes.Where(i => i.UserId == UserId).ToList();
                return result;
            }
        }
        public List<HAThemeVariable> GetThemeDefinition(int ThemeId)
        {
            List<HAThemeVariable> result = new List<HAThemeVariable>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                List<HAThemeVariable> ThemeDefinitionObj = apidbContext.HAThemeVariables.ToList(); //PAS ICI BORDEL

                foreach (HAThemeVariable ThemeDefinition in ThemeDefinitionObj)
                {
                    if (ThemeDefinition.HAThemeId == ThemeId)
                        result.Add(ThemeDefinition);
                }
                return result;
            }
        }

        public int GetThemeId(string Label, string UserId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HATheme currentThemeId = apidbContext.HAThemes.FirstOrDefault(t => t.UserId == UserId && t.Label == Label);
                int result = currentThemeId.Id;
                return result;
            }
        }

        public int CreateDefaultTheme(string UserId)
        {
            using (var apiDbContext = _contextFactory.CreateDbContext())
            {
                HATheme a = apiDbContext.HAThemes.FirstOrDefault(t => t.UserId == UserId && t.Label == "Theme1");
                if (a == null)
                {
                    // Create Theme1
                    HATheme defaultTheme = new HATheme()
                    {
                        Label = "Theme1",
                        UserId = UserId
                    };
                    apiDbContext.HAThemes.Add(defaultTheme);
                    apiDbContext.SaveChanges();
                }

                HATheme b = apiDbContext.HAThemes.FirstOrDefault(t => t.UserId == UserId && t.Label == "Theme2");
                if (b == null)
                {
                    // Create Theme2
                    HATheme defaultTheme2 = new HATheme()
                    {
                        Label = "Theme2",
                        UserId = UserId
                    };
                    apiDbContext.HAThemes.Add(defaultTheme2);
                    apiDbContext.SaveChanges();
                }

                //fill Theme1 default Variables
                Dictionary<string, string> ThemeableElementsList1 = new Dictionary<string, string>()
                {
                    { "PrimaryColor", "#61baac" },
                    { "SecondaryColor", "#807a70" },
                    { "customFontSize", "1" },
                    { "NavbarColor", "#807a70" },
                    { "TopNavbarColor", "#ffffff" }
                };

                int userTheme1Id = GetThemeId("Theme1", UserId);

                foreach (KeyValuePair<string, string> ThemeableElement in ThemeableElementsList1)
                {
                    HAThemeVariable userThemeVariable = apiDbContext.HAThemeVariables.FirstOrDefault(t => t.HAThemeId == userTheme1Id && t.VariableName == ThemeableElement.Key);

                    if (userThemeVariable == null)
                    { 
                        HAThemeVariable defaultThemeVariable = new HAThemeVariable()
                        {
                            VariableName = ThemeableElement.Key,
                            VariableValue = ThemeableElement.Value,
                            HAThemeId = GetThemeId("Theme1", UserId)
                        };
                        apiDbContext.HAThemeVariables.Add(defaultThemeVariable);
                    }
                }

                //fill Theme2 default Variables
                Dictionary<string, string> ThemeableElementsList2 = new Dictionary<string, string>()
                {
                    { "PrimaryColor", "#343a40" },
                    { "SecondaryColor", "#94181c" },
                    { "customFontSize", "1" },
                    { "NavbarColor", "#807a70" },
                    { "TopNavbarColor", "#ffffff" }
                };

                int userTheme2Id = GetThemeId("Theme2", UserId);

                foreach (KeyValuePair<string, string> ThemeableElement in ThemeableElementsList2)
                {
                    HAThemeVariable userTheme2Variable = apiDbContext.HAThemeVariables.FirstOrDefault(t => t.HAThemeId == userTheme2Id && t.VariableName == ThemeableElement.Key);
                    if (userTheme2Variable == null)
                    {
                        HAThemeVariable defaultThemeVariable = new HAThemeVariable()
                        {
                            VariableName = ThemeableElement.Key,
                            VariableValue = ThemeableElement.Value,
                            HAThemeId = GetThemeId("Theme2", UserId)
                        };
                        apiDbContext.HAThemeVariables.Add(defaultThemeVariable);
                    }
                }
                return apiDbContext.SaveChanges();
            }
        }
        public bool UpdateThemeVariableValues(int ThemeId, UpdateThemeRequest TargetTheme)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAThemeVariable existingPrimaryColor = apidbContext.HAThemeVariables.Where(p => p.HAThemeId == ThemeId).FirstOrDefault(l => l.VariableName == "PrimaryColor");
                if (existingPrimaryColor == null)
                    return false;
                existingPrimaryColor.VariableValue = TargetTheme.PrimaryValue;

                HAThemeVariable existingSecondaryColor = apidbContext.HAThemeVariables.Where(p => p.HAThemeId == ThemeId).FirstOrDefault(p => p.VariableName == "SecondaryColor");
                if (existingSecondaryColor == null)
                    return false;
                existingSecondaryColor.VariableValue = TargetTheme.SecondaryValue;

                HAThemeVariable existingNavbarColor = apidbContext.HAThemeVariables.Where(p => p.HAThemeId == ThemeId).FirstOrDefault(p => p.VariableName == "NavbarColor");
                if (existingNavbarColor == null)
                    return false;
                existingNavbarColor.VariableValue = TargetTheme.navbarValue;

                HAThemeVariable existingTopNavbarColor = apidbContext.HAThemeVariables.Where(p => p.HAThemeId == ThemeId).FirstOrDefault(p => p.VariableName == "TopNavbarColor");
                if (existingTopNavbarColor == null)
                    return false;
                existingTopNavbarColor.VariableValue = TargetTheme.topNavbarValue;

                HAThemeVariable existingCustomFontSize = apidbContext.HAThemeVariables.Where(k => k.HAThemeId == ThemeId).FirstOrDefault(k => k.VariableName == "customFontSize");
                if (existingCustomFontSize == null)
                    return false;
                existingCustomFontSize.VariableValue = TargetTheme.customFontSize;

                apidbContext.SaveChanges();
                return true;
            }
        }
    }
}
