using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Setup;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Domain.Services
{
    public class WizardService(
        UserManager<ApiUser> userManager,
        RoleManager<ApiRole> roleManager,
        ISettingService settingService,
        IDbConnectionService dbConnectionService,
        ISqlQueryService sqlQueryService,
        IMenuService menuService,
        IPageService pageService,
        IRowService rowService,
        ICardService cardService,
        ILogger<WizardService> logger)
        : IWizardService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task<(bool Success, string Error)> SetupAsync(SetupDto request)
        {
            try
            {
                logger.LogInformation("Starting application setup process");

                if (request == null)
                {
                    logger.LogError("Setup request is null");
                    return (false, "Setup configuration data is required");
                }

                if (request.Admin == null)
                {
                    logger.LogError("Admin configuration is missing");
                    return (false, "Admin configuration is required");
                }

                logger.LogDebug("Attempting to acquire setup lock");
                if (!await _semaphore.WaitAsync(TimeSpan.FromMinutes(1)))
                {
                    logger.LogError("Failed to acquire setup lock - timeout occurred");
                    return (false, "Setup lock timeout - another setup process might be running");
                }

                try
                {
                    logger.LogInformation("Configuring JWT settings");
                    var jwtSecret = GenerateSecureSecret();
                    
                    await UpdateJwtSettings(jwtSecret);
                    await ConfigureAdminRole();
                    await CreateAdminUser(request.Admin);
                    await ConfigureSmtpSettings(request.Smtp);
                    await UpdateApiConfiguration();
                    ApiUser adminUser = await userManager.FindByEmailAsync(request.Admin.Email);
                    
                    if (request.CreateSample)
                    {
                        DBConnectionCreateResultDto dbConnectionResult = await RetrieveAndCreateNorthwindDatabase(request.OperationId);
                        DBConnectionDto connectionDto = await dbConnectionService.GetByIdAsync(dbConnectionResult.Id);
                        SqlQueryDto querySample = await CreateSampleQuery(request.OperationId, connectionDto, adminUser,
                            "Product Quantity By Country",
                            "Show number of sales products by country",
                            "SELECT\n   s.Country,\n   SUM(od.Quantity)   AS Quantity\nFROM\n    [Order Details]   AS od\nINNER JOIN\n    Products          AS p\n        ON  p.ProductID = od.ProductID\nINNER JOIN\n    Suppliers         AS s\n        ON  s.SupplierID = p.SupplierID\nGROUP BY\n   s.Country",
                            new Dictionary<string, object>());
                        MenuDto menu = await CreateMenu(request.OperationId);
                        await CreateWelcomePage(request.OperationId, menu);
                        await CreateDatatablePage(request.OperationId, menu);
                        
                        PageDto chartsPage= await CreatePage(
                            request.OperationId, 
                            menu, 
                            3, 
                            "simple_charts",
                            [
                                new TranslatableStringDto() { LanguageCode = "fr", Value = "Charts simple" },
                                new TranslatableStringDto() { LanguageCode = "en", Value = "Simple charts" }
                            ]);
                        PageDto advancedPage= await CreatePage(
                            request.OperationId, 
                            menu, 
                            3, 
                            "advanced",
                            [
                                new TranslatableStringDto() { LanguageCode = "fr", Value = "Conf. Avancée" },
                                new TranslatableStringDto() { LanguageCode = "en", Value = "Advanced" }
                            ]);
                        
                        
                    }
                    logger.LogInformation("Application setup completed successfully");
                    return (true, null);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Setup process failed");
                    return (false, $"Setup failed: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                    logger.LogDebug("Setup lock released");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during setup process");
                return (false, $"Unexpected error during setup: {ex.Message}");
            }
        }

        private async Task CreateDatatablePage(string requestOperationId, MenuDto menu)
        {
            PageDto dataTablePage= await CreatePage(
                requestOperationId, 
                menu, 
                2, 
                "simple_datatable",
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Datatable simple" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Simple datatable" }
                ]);
            RowDto row1 = await CreateRow(requestOperationId, dataTablePage.Id, 300, 1);
            RowDto row2 = await CreateRow(requestOperationId, dataTablePage.Id, 530, 2);
            RowDto row3 = await CreateRow(requestOperationId, dataTablePage.Id, 350, 3);
            
            await CreateCard(requestOperationId, row1.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Introduction aux datatables" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Datatable introduction" }
                ],
                1,
                "html content",
                12,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/dataTableGeneralDescription.json")
            );
            
            await CreateCard(requestOperationId, row2.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Commandes" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Orders" }
                ],
                1,
                "datatable",
                12,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/ordersDatatable.json")
            );
            
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Affichage et navigation" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Display and navigation" }
                ],
                1,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/datatableDisplayFeatures.json")
            );
            
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Manipulation des données" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Data handling" }
                ],
                2,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/datatableDataHandling.json")
            );
            
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Edition et CRUD" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Edit and CRUD" }
                ],
                3,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/datatableEditAndCRUD.json")
            );
            
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Personalisation" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Personalization" }
                ],
                4,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/datatablePersonalisation.json")
            );
        }

        private async Task CreateWelcomePage(string requestOperationId, MenuDto menu)
        {
            PageDto welcomePage = await CreatePage(
                requestOperationId, 
                menu, 
                1, 
                "welcome",
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Accueil" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Home" }
                ]);
            RowDto row1 = await CreateRow(requestOperationId, welcomePage.Id, 300, 1);
            RowDto row2 = await CreateRow(requestOperationId, welcomePage.Id, 300, 2);
            RowDto row3 = await CreateRow(requestOperationId, welcomePage.Id, 300, 3);

            await CreateCard(requestOperationId, row1.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Bienvenue" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Welcome" }
                ],
                1,
                "html content",
                12,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/generalDescription.json")
            );
            await CreateCard(requestOperationId, row2.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Architecture backend" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Backend architecture" }
                ],
                1,
                "html content",
                6,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/backendArchitecture.json")
            );
            await CreateCard(requestOperationId, row2.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Architecture backend" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Backend architecture" }
                ],
                2,
                "html content",
                6,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/frontendArchitecture.json")
            );
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Tableaux de bord" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Dashboards" }
                ],
                1,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/dashboards.json")
            );
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Bases de données" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Databases" }
                ],
                2,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/databases.json")
            );
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Sources de données" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Datasources" }
                ],
                3,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/datasources.json")
            );
            await CreateCard(requestOperationId, row3.Id, 
                [
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Fonctionnalités" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Features" }
                ],
                4,
                "html content",
                3,
                await File.ReadAllTextAsync("Infrastructure/Templates/WizardSampleCards/features.json")
            );
        }

        private async Task CreateCard(string requestOperationId, int rowId, IEnumerable<TranslatableStringDto> title, int order, string type, int gridWidth, string cardConfiguration)
        {
            CardDto cardDto = new CardDto()
            {
                Title = title,
                Order = order,
                Type = type,
                GridWidth = gridWidth,
                Configuration = JsonConvert.DeserializeObject<ExpandoObject>(cardConfiguration),
                BackgroundColor = 2042167,
                TextColor = 16777215,
                HeaderBackgroundColor = 1006484,
                HeaderTextColor = 16777215,
                DisplayHeader = true,
                DisplayFooter = false,
                Icon = "fa-solid fa-circle-plus",
                RowId = rowId
            };
            await cardService.CreateAsync(rowId, cardDto);
        }

        private async Task<RowDto> CreateRow(object operationId, int pageId, double height, int order)
        {
            RowCreateDto createRow = new RowCreateDto()
            {
                Height = height,
                Order = order
            };
            return await rowService.CreateAsync(pageId, createRow);
        }

        private async Task<SqlQueryDto> CreateSampleQuery(string requestOperationId, DBConnectionDto dbConnection, ApiUser user, string name, string description, string query, Dictionary<string, object> parameters)
        {
            SqlQueryDto queryDto = new SqlQueryDto()
            {
                CreatedAt = DateTime.Now,
                CreatedBy = user.Id,
                CreatedByEmail = user.Email,
                DBConnection = dbConnection,
                DBConnectionId = dbConnection.Id,
                Description = description,
                IsPublic = true,
                LastModifiedAt = DateTime.Now,
                Name = name,
                Query = query,
                Parameters = parameters,
                OutputDescription = ""
            };
            return await sqlQueryService.CreateQueryAsync(queryDto, parameters);
        }

        private async Task<PageDto> CreatePage(string requestOperationId, MenuDto menu, int orderId, string route, List<TranslatableStringDto> title)
        {
            PageCreateDto createPage = new PageCreateDto()
            {
                Icon = "home",
                MenuId = menu.Id,
                Order = orderId,
                Roles = menu.Roles,
                Route = route,
                IsVisible = true,
                Title = title
            };
            return await pageService.CreateAsync(createPage);
        }

        private async Task<MenuDto> CreateMenu(string requestOperationId)
        {
            MenuCreateDto menuDto = new MenuCreateDto()
            {
                Icon = "home",
                IsVisible = true,
                Order = 1,
                Roles = roleManager.Roles.Select(RoleDto.FromEntity).ToList(),
                Route = "northwindlite",
                Title = new List<TranslatableStringDto>()
                {
                    new TranslatableStringDto() { LanguageCode = "fr", Value = "Northwind exemple" },
                    new TranslatableStringDto() { LanguageCode = "en", Value = "Northwind example" }
                }
            };

            return await menuService.CreateAsync(menuDto);
        }

        private async Task<DBConnectionCreateResultDto> RetrieveAndCreateNorthwindDatabase(string operationId)
        {
            using var client = new WebClient();
            client.DownloadFile("https://github.com/jpwhite3/northwind-SQLite3/raw/refs/heads/main/dist/northwind.db", "northwind_sample.db");
            DBConnectionCreateDto createDBConnectionDto = new DBConnectionCreateDto()
            {
                OperationId = operationId,
                Parameters = new List<ConnectionStringParameterCreateDto>()
                {
                    new ConnectionStringParameterCreateDto()
                    {
                        Key = "Data Source",
                        Value = "./northwind_sample.db",
                        IsEncrypted = false
                    }
                },
                ApiRoute = "northwind_sample",
                ConnectionType = DbConnectionType.SQLite,
                ContextName = "NorthwindSQLiteSample",
                GenerateProcedureControllersAndServices = false,
                Name = "NorthwindSQLiteSample"
            };
            return await dbConnectionService.AddConnectionAsync(createDBConnectionDto);
        }

        private async Task UpdateJwtSettings(string jwtSecret)
        {
            try
            {
                logger.LogDebug("Updating JWT settings");
                await settingService.UpdateSettingIfExistsAsync("jwt:secret", jwtSecret);
                await settingService.UpdateSettingIfExistsAsync("jwt:issuer", "QuerierApi");
                await settingService.UpdateSettingIfExistsAsync("jwt:audience", "QuerierClient");
                await settingService.UpdateSettingIfExistsAsync("jwt:expiry", 60);
                logger.LogInformation("JWT settings updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update JWT settings");
                throw;
            }
        }

        private async Task ConfigureAdminRole()
        {
            try
            {
                logger.LogDebug("Checking if Admin role exists");
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    logger.LogInformation("Creating Admin role");
                    var adminRole = new ApiRole
                    {
                        Name = "Admin",
                        NormalizedName = "ADMIN"
                    };
                    var createRoleResult = await roleManager.CreateAsync(adminRole);
                    if (!createRoleResult.Succeeded)
                    {
                        var errors = string.Join(", ", createRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        logger.LogError("Failed to create Admin role: {Errors}", errors);
                        throw new InvalidOperationException($"Failed to create Admin role: {errors}");
                    }
                }
                logger.LogInformation("Admin role configuration completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Admin role");
                throw;
            }
        }

        private async Task CreateAdminUser(SetupAdminDto adminSetup)
        {
            try
            {
                logger.LogInformation("Creating admin user with email: {Email}", adminSetup.Email);
                
                var existingUser = await userManager.FindByEmailAsync(adminSetup.Email);
                if (existingUser != null)
                {
                    logger.LogWarning("User with email {Email} already exists", adminSetup.Email);
                    throw new InvalidOperationException("User already exists");
                }

                var adminUser = new ApiUser
                {
                    UserName = adminSetup.Email,
                    Email = adminSetup.Email,
                    FirstName = adminSetup.FirstName,
                    LastName = adminSetup.Name,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminSetup.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("Failed to create admin user: {Errors}", errors);
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }

                logger.LogDebug("Assigning Admin role to user");
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("Failed to assign Admin role: {Errors}", errors);
                    throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
                }

                logger.LogInformation("Admin user created and configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create admin user");
                throw;
            }
        }

        private async Task ConfigureSmtpSettings(SetupSmtpDto smtpSetup)
        {
            try
            {
                logger.LogDebug("Configuring SMTP settings");
                await settingService.UpdateSettingIfExistsAsync("smtp:host", smtpSetup.Host);
                await settingService.UpdateSettingIfExistsAsync("smtp:port", smtpSetup.Port);
                await settingService.UpdateSettingIfExistsAsync("smtp:username", smtpSetup.Username);
                await settingService.UpdateSettingIfExistsAsync("smtp:useSSL", smtpSetup.useSSL);
                await settingService.UpdateSettingIfExistsAsync("smtp:senderEmail", smtpSetup.SenderEmail);
                await settingService.UpdateSettingIfExistsAsync("smtp:senderName", smtpSetup.SenderName);
                await settingService.UpdateSettingIfExistsAsync("smtp:requiresAuth", smtpSetup.RequireAuth);
                logger.LogInformation("SMTP settings configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure SMTP settings");
                throw;
            }
        }

        private async Task UpdateApiConfiguration()
        {
            try
            {
                logger.LogDebug("Updating API configuration status");
                var isConfigured = await settingService.GetSettingValueIfExistsAsync("api:isConfigured", false, "");
                if (!isConfigured)
                {
                    await settingService.UpdateSettingIfExistsAsync("api:isConfigured", true);
                }
                logger.LogInformation("API configuration status updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update API configuration status");
                throw;
            }
        }

        private string GenerateSecureSecret(int length = 32)
        {
            try
            {
                logger.LogDebug("Generating secure secret with length: {Length}", length);
                using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                var secret = Convert.ToBase64String(bytes);
                logger.LogDebug("Secure secret generated successfully");
                return secret;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate secure secret");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    logger.LogDebug("Disposing WizardService resources");
                    _semaphore.Dispose();
                }
                _disposed = true;
            }
        }

        ~WizardService()
        {
            Dispose(false);
        }
    }
}