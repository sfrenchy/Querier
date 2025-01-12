using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services;
using Querier.Api.Domain.Services.Role;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Data.Repositories;
using Querier.Api.Infrastructure.Security.TokenProviders;
using Querier.Api.Infrastructure.Services;
using Querier.Api.Infrastructure.Swagger.Extensions;
using Querier.Api.Infrastructure.Swagger.Filters;
using Querier.Api.Infrastructure.Swagger.Helpers;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Querier.Api.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Configuring database connection");

            try
            {
                var sqlEngine = configuration.GetSection("SQLEngine").Get<string>();
                logger.LogInformation("Using SQL engine: {Engine}", sqlEngine?.ToUpper() ?? "SQLite");

                switch (sqlEngine?.ToUpper())
                {
                    case "MSSQL":
                        logger.LogDebug("Configuring SQL Server connection");
                        services.AddDbContext<ApiDbContext>(options => 
                            options.UseSqlServer(configuration.GetConnectionString("ApiDBConnection")));
                        break;
                    case "MYSQL":
                        logger.LogDebug("Configuring MySQL connection");
                        var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                        services.AddDbContext<ApiDbContext>(options => 
                            options.UseMySql(configuration.GetConnectionString("ApiDBConnection"), serverVersion));
                        break;
                    case "PGSQL":
                        logger.LogDebug("Configuring PostgreSQL connection");
                        services.AddDbContext<ApiDbContext>(options => 
                            options.UseNpgsql(configuration.GetConnectionString("ApiDBConnection")));
                        break;
                    default:
                        logger.LogDebug("Configuring SQLite connection");
                        services.AddDbContext<ApiDbContext>(options => 
                            options.UseSqlite(configuration.GetConnectionString("ApiDBConnection")));
                        break;
                }

                services.AddDbContextFactory<ApiDbContext>();
                logger.LogInformation("Database configuration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure database connection");
                throw;
            }

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Configuring authentication");

            try
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var tokenLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            tokenLogger.LogInformation("Token validated successfully");
                            await Task.CompletedTask;
                        },
                        OnAuthenticationFailed = async context =>
                        {
                            var tokenLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            var settingService = context.HttpContext.RequestServices
                                .GetRequiredService<ISettingService>();
                            
                            // Si l'application n'est pas configurée, on permet l'accès
                            if (!await settingService.GetApiIsConfiguredAsync())
                            {
                                tokenLogger.LogWarning("Application not configured, allowing anonymous access");
                                var anonymousClaims = new[]
                                {
                                    new Claim(ClaimTypes.Name, "Anonymous"),
                                    new Claim(ClaimTypes.Role, "Anonymous")
                                };
                                var anonymousIdentity = new ClaimsIdentity(anonymousClaims);
                                var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);

                                context.Principal = anonymousPrincipal;
                                context.Success();
                                return;
                            }
                            tokenLogger.LogWarning("Authentication failed");
                        },
                        OnMessageReceived = async context =>
                        {
                            var tokenLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            tokenLogger.LogDebug("Configuring token validation parameters");

                            var settingService = context.HttpContext.RequestServices.GetRequiredService<ISettingService>();
                            var secret = await settingService.GetSettingValueIfExistsAsync("jwt:secret", "DefaultDevSecretKey_12345678901234567890123456789012", "JWT secret");
                            var key = Encoding.ASCII.GetBytes(secret);
                            var signingKey = new SymmetricSecurityKey(key) { KeyId = "default_signing_key" };

                            var issuer = await settingService.GetSettingValueIfExistsAsync("jwt:issuer", "QuerierApi", "JWT issuer");
                            var audience = await settingService.GetSettingValueIfExistsAsync("jwt:audience", "QuerierClient", "JWT Audience");

                            // Mettre à jour les paramètres de validation du token de manière dynamique
                            context.Options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = signingKey,
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                RequireExpirationTime = true,
                                ClockSkew = TimeSpan.Zero,
                                ValidIssuer = issuer,
                                ValidAudience = audience
                            };

                            tokenLogger.LogDebug("Token validation parameters configured successfully");
                        }
                    };
                });

                logger.LogInformation("Authentication configuration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure authentication");
                throw;
            }

            return services;
        }

        public static IServiceCollection AddCustomIdentity(this IServiceCollection services)
        {
            // Configuration de base de l'identité avec des valeurs par défaut
            services.AddDefaultIdentity<ApiUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";
            })
            .AddRoles<ApiRole>()
            .AddEntityFrameworkStores<ApiDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider<ApiUser>>("emailconfirmation");

            // Configuration des durées de validité des tokens avec des valeurs par défaut
            services.Configure<EmailConfirmationTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromDays(2);
            });

            services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromMinutes(15);
            });

            // Configuration des paramètres de validation JWT
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = false,
                ClockSkew = TimeSpan.Zero
            };
            services.AddSingleton(tokenValidationParameters);

            return services;
        }

        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
        {
            services.AddScoped<IAspnetIdentityConfigurationService, AspnetIdentityConfigurationService>();
            return services;
        }

        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddSingleton(services);
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IAspnetIdentityConfigurationService, AspnetIdentityConfigurationService>();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IEntityCrudService, EntityCrudService>();
            services.AddScoped<IWizardService, WizardService>();
            services.AddScoped<IDbConnectionService, DbConnectionService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();

            // User and Auth services
            services.AddScoped<IEmailSendingService, SmtpEmailSendingService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>();

            // Repositories
            services.AddScoped<ICardRepository, CardRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRowRepository, RowRepository>();
            services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            services.AddScoped<IMenuRepository, MenuRepository>();
            services.AddScoped<IPageRepository, PageRepository>();
            services.AddScoped<ISettingRepository, SettingRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDbConnectionRepository, DbConnectionRepository>();
            
            // Menu and Layout services
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IRowService, RowService>();
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<ILayoutService, LayoutService>();
            services.AddScoped<ISqlQueryService, SqlQueryService>();

            // Menu repositories
            services.AddScoped<IMenuRepository, MenuRepository>();
            services.AddScoped<IPageRepository, PageRepository>();

            return services;
        }

        public static IServiceCollection AddCustomLogging(this IServiceCollection services)
        {
            try
            {
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    });
                    builder.AddDebug();
                    
                    builder.SetMinimumLevel(LogLevel.Debug);
                    builder.AddFilter("Microsoft", LogLevel.Warning)
                           .AddFilter("System", LogLevel.Warning)
                           .AddFilter("Querier.Api", LogLevel.Debug);
                });

                var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
                logger.LogInformation("Logging configuration completed successfully");
            }
            catch (Exception ex)
            {
                // Ici nous ne pouvons pas logger l'erreur car le logging n'est pas encore configuré
                Console.Error.WriteLine($"Failed to configure logging: {ex.Message}");
                throw;
            }

            return services;
        }

        public static async Task AddDynamicAssemblies(this IServiceCollection services, IConfiguration configuration)
        {
            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Loading dynamic assemblies");

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
                var serviceProvider = services.BuildServiceProvider();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                var dbLogger = loggerFactory.CreateLogger<ApiDbContext>();

                await using var apiDbContext = new ApiDbContext(optionsBuilder.Options, configuration, dbLogger);
                var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
                var mvc = services.AddControllers();

                foreach(var connection in apiDbContext.DBConnections.ToList())
                {
                    logger.LogDebug("Loading assembly for connection: {ConnectionName}", connection.Name);
                    AssemblyLoader.LoadAssemblyFromDbConnection(connection, serviceProvider, mvc.PartManager, logger);
                }

                logger.LogDebug("Regenerating Swagger documentation");
                AssemblyLoader.RegenerateSwagger(swaggerProvider, logger);
                logger.LogInformation("Dynamic assemblies loaded successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load dynamic assemblies");
                throw;
            }
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { 
                    Title = "Querier API", 
                    Version = "v1",
                    Description = "API de gestion des requêtes et des données pour Querier"
                });
                
                // Inclusion des fichiers XML de documentation
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    c.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
                }

                // Configuration pour inclure les DTOs et leurs descriptions
                c.EnableAnnotations();
                c.SchemaFilter<EnumSchemaFilter>();
                c.UseInlineDefinitionsForEnums();
                c.DescribeAllParametersInCamelCase();
                c.UseAllOfForInheritance();
                c.UseOneOfForPolymorphism();
                c.CustomSchemaIds(type => type.FullName);
                
                // Afficher les modèles même s'ils ne sont pas directement référencés
                c.DocumentFilter<ShowAllModelsDocumentFilter>();
                c.DocumentFilter<ConnectionPrefixDocumentFilter>();
                
                // Configuration de la sécurité
                c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "bearerAuth"
                            }
                        },
                        new string[] {}
                    }
                });

                var schemaHelper = new SwashbuckleSchemaHelper();
                c.CustomSchemaIds(type => schemaHelper.GetSchemaId(type));

                // Résoudre les conflits d'actions en préférant les contrôleurs de l'assembly principal
                c.ResolveConflictingActions(apiDescriptions =>
                {
                    var mainAssemblyController = apiDescriptions
                        .FirstOrDefault(api => api.ActionDescriptor.DisplayName?.Contains("Querier.Api") == true);
                    
                    if (mainAssemblyController != null)
                        return mainAssemblyController;

                    return apiDescriptions.First();
                });

                // Personnaliser les IDs d'opération pour éviter les conflits
                c.CustomOperationIds(apiDesc =>
                {
                    var assemblyName = apiDesc.ActionDescriptor.DisplayName?.Split(',')
                        .Skip(1)
                        .FirstOrDefault()
                        ?.Trim()
                        ?? string.Empty;

                    return $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.ActionDescriptor.RouteValues["action"]}_{assemblyName}";
                });

                // Ordonner les contrôleurs alphabétiquement
                c.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath}");
                c.TagActionsBy(api => new[] { api.ActionDescriptor.RouteValues["controller"] });
                c.DocInclusionPredicate((name, api) => true);
            });

            return services;
        }

        public static void UseCustomSwagger(this IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
                c.EnableDeepLinking();
                c.DisplayRequestDuration();
                c.DefaultModelExpandDepth(2);
                c.DefaultModelRendering(ModelRendering.Model);
                c.DisplayOperationId();
                c.ShowCommonExtensions();
                c.EnableFilter();
                
                // Police Roboto pour la cohérence
                c.InjectStylesheet("https://fonts.googleapis.com/css?family=Roboto:300,400,500,700");
                c.InjectStylesheet("https://fonts.googleapis.com/css?family=Roboto+Mono:400,500,700");
                
                // Utilisation du thème personnalisé (après les polices)
                c.InjectStylesheet("/swagger-ui/custom-theme.css");
                c.DocumentTitle = "Querier API Documentation";

                // Ajouter le JavaScript pour grouper les tags
                c.InjectJavascript("/swagger-ui/custom-script.js");

                // Configuration du logo et styles supplémentaires
                c.HeadContent = @"
                    <style>
                        :root {
                            --bg-primary: #121212;
                            --bg-secondary: #1E1E1E;
                            --text-primary: #FFFFFF;
                            --text-secondary: #CCCCCC;
                            --border-color: #333333;
                            --accent-color: #4CAF50;
                            --accent-light: #81C784;
                            --accent-dark: #388E3C;
                            --method-get: #2196F3;
                            --method-post: #4CAF50;
                            --method-put: #FF9800;
                            --method-delete: #F44336;
                            --code-bg: #2d2d2d;
                        }

                        body, .swagger-ui {
                            background-color: var(--bg-primary) !important;
                            color: var(--text-primary) !important;
                        }

                        /* Suppression de la bande blanche */
                        .swagger-ui .scheme-container {
                            background-color: var(--bg-primary) !important;
                            box-shadow: none !important;
                            border-bottom: 1px solid var(--border-color) !important;
                        }

                        .swagger-ui .auth-wrapper {
                            color: var(--text-primary) !important;
                        }

                        .swagger-ui .auth-container {
                            background-color: var(--bg-secondary) !important;
                            border-color: var(--border-color) !important;
                        }

                        .swagger-ui .info .title,
                        .swagger-ui .info h1,
                        .swagger-ui .info h2,
                        .swagger-ui .info h3,
                        .swagger-ui .info h4,
                        .swagger-ui .info h5,
                        .swagger-ui .info h6,
                        .swagger-ui .opblock-tag,
                        .swagger-ui table thead tr th,
                        .swagger-ui .parameter__name,
                        .swagger-ui .tab li,
                        .swagger-ui .opblock .opblock-summary-operation-id,
                        .swagger-ui .opblock .opblock-summary-path,
                        .swagger-ui .opblock .opblock-summary-description {
                            color: var(--text-primary) !important;
                        }

                        /* Amélioration du contraste pour le texte */
                        .swagger-ui .opblock .opblock-section-header {
                            background-color: var(--bg-secondary) !important;
                            box-shadow: none !important;
                        }

                        .swagger-ui .opblock .opblock-section-header h4 {
                            color: var(--text-primary) !important;
                        }

                        .swagger-ui .parameter__name,
                        .swagger-ui .parameter__type,
                        .swagger-ui .parameter__deprecated,
                        .swagger-ui .parameter__in,
                        .swagger-ui table.parameters td,
                        .swagger-ui table.parameters th {
                            color: var(--text-primary) !important;
                        }

                        .swagger-ui .opblock {
                            background-color: var(--bg-secondary) !important;
                            border-color: var(--border-color) !important;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3) !important;
                            margin: 0 0 10px 0 !important;
                        }

                        /* Topbar et Logo */
                        .swagger-ui .topbar {
                            background-color: var(--bg-secondary) !important;
                            border-bottom: 1px solid var(--border-color) !important;
                            padding: 8px 0 !important;
                        }

                        .swagger-ui .topbar-wrapper {
                            padding: 0 16px !important;
                        }

                        /* Correction du logo */
                        .swagger-ui img {
                            display: block !important;
                        }

                        .swagger-ui .topbar-wrapper img {
                            content: url('/swagger-ui/querier_logo_no_bg_big.png') !important;
                            height: 40px !important;
                            width: auto !important;
                            display: block !important;
                            margin: 0 !important;
                        }

                        /* Masquer le texte Swagger UI par défaut */
                        .swagger-ui .topbar a span {
                            display: none !important;
                        }

                        /* Méthodes HTTP avec meilleur contraste */
                        .swagger-ui .opblock.opblock-get {
                            border-color: var(--method-get) !important;
                            background-color: rgba(33, 150, 243, 0.1) !important;
                        }

                        .swagger-ui .opblock.opblock-post {
                            border-color: var(--method-post) !important;
                            background-color: rgba(76, 175, 80, 0.1) !important;
                        }

                        .swagger-ui .opblock.opblock-put {
                            border-color: var(--method-put) !important;
                            background-color: rgba(255, 152, 0, 0.1) !important;
                        }

                        .swagger-ui .opblock.opblock-delete {
                            border-color: var(--method-delete) !important;
                            background-color: rgba(244, 67, 54, 0.1) !important;
                        }

                        /* Inputs et Boutons avec meilleur contraste */
                        .swagger-ui input[type=text],
                        .swagger-ui input[type=password],
                        .swagger-ui input[type=search],
                        .swagger-ui input[type=number],
                        .swagger-ui textarea {
                            background-color: var(--bg-primary) !important;
                            color: var(--text-primary) !important;
                            border: 1px solid var(--border-color) !important;
                        }

                        .swagger-ui .btn {
                            background-color: var(--accent-color) !important;
                            color: white !important;
                            border: none !important;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2) !important;
                        }

                        .swagger-ui .btn:hover {
                            background-color: var(--accent-light) !important;
                        }

                        .swagger-ui .btn.authorize {
                            background-color: var(--accent-color) !important;
                            border-color: var(--accent-color) !important;
                            color: white !important;
                        }

                        .swagger-ui .btn.authorize svg {
                            fill: white !important;
                        }

                        /* Schémas et Modèles */
                        .swagger-ui section.models {
                            background-color: var(--bg-secondary) !important;
                            border-color: var(--border-color) !important;
                        }

                        .swagger-ui section.models h4 {
                            color: var(--text-primary) !important;
                        }

                        /* Description et Documentation */
                        .swagger-ui .markdown p,
                        .swagger-ui .markdown pre,
                        .swagger-ui .renderedMarkdown p,
                        .swagger-ui .renderedMarkdown pre {
                            color: var(--text-secondary) !important;
                        }

                        /* Sélecteurs et Filtres */
                        .swagger-ui select {
                            background-color: var(--bg-primary) !important;
                            color: var(--text-primary) !important;
                            border-color: var(--border-color) !important;
                        }

                        /* Amélioration du contraste pour les tables */
                        .swagger-ui table {
                            background-color: var(--bg-secondary) !important;
                            border-collapse: separate !important;
                            border-spacing: 0 !important;
                            border: 1px solid var(--border-color) !important;
                        }

                        .swagger-ui table thead tr {
                            background-color: var(--bg-primary) !important;
                            border-bottom: 1px solid var(--border-color) !important;
                        }

                        .swagger-ui table tbody tr:hover {
                            background-color: rgba(255, 255, 255, 0.05) !important;
                        }
                    </style>
                ";
            });
        }
    }
} 