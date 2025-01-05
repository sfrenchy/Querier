using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Threading.Tasks;
using System.Linq;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Security.TokenProviders;
using Querier.Api.Domain.Services;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Services.Role;
using Querier.Api.Domain.Services.User;
using Querier.Api.Domain.Services.Repositories.Role;
using Querier.Api.Infrastructure.Services.Menu;
using Querier.Api.Infrastructure.Data.Repositories.Menu;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Application.Interfaces.Services.Role;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Services.Repositories.User;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Querier.Api.Domain.Services.Identity;
using Microsoft.OpenApi.Models;
using System.IO;
using Querier.Api.Infrastructure.Swagger.Helpers;

namespace Querier.Api.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var sqlEngine = configuration.GetSection("SQLEngine").Get<string>();
            switch (sqlEngine?.ToUpper())
            {
                case "MSSQL":
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseSqlServer(configuration.GetConnectionString("ApiDBConnection")));
                    break;
                case "MYSQL":
                    var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseMySql(configuration.GetConnectionString("ApiDBConnection"), serverVersion));
                    break;
                case "PGSQL":
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseNpgsql(configuration.GetConnectionString("ApiDBConnection")));
                    break;
                default: // SQLite
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseSqlite(configuration.GetConnectionString("ApiDBConnection")));
                    break;
            }
            services.AddDbContextFactory<ApiDbContext>();
            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
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
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Token validated successfully");
                        await Task.CompletedTask;
                    },
                    OnAuthenticationFailed = async context =>
                    {
                        var settingService = context.HttpContext.RequestServices
                            .GetRequiredService<ISettingService>();
                        
                        // Si l'application n'est pas configurée, on permet l'accès
                        if (!await settingService.GetIsConfigured())
                        {
                            context.NoResult();
                            context.Success();
                            return;
                        }
                    },
                    OnMessageReceived = async context =>
                    {
                        var settingService = context.HttpContext.RequestServices.GetRequiredService<ISettingService>();
                        var secret = await settingService.GetSettingValue("JwtSecret", "DefaultDevSecretKey_12345678901234567890123456789012");
                        var key = Encoding.ASCII.GetBytes(secret);
                        var signingKey = new SymmetricSecurityKey(key) { KeyId = "default_signing_key" };

                        var issuer = await settingService.GetSettingValue("JwtIssuer", "QuerierApi");
                        var audience = await settingService.GetSettingValue("JwtAudience", "QuerierClient");

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
                    }
                };
            });

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
            services.AddScoped<IIdentityConfigurationService, IdentityConfigurationService>();
            return services;
        }

        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddSingleton(services);
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IIdentityConfigurationService, IdentityConfigurationService>();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IEntityCRUDService, EntityCRUDService>();
            services.AddScoped<IWizardService, WizardService>();
            services.AddScoped<IDBConnectionService, DBConnectionService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();

            // User and Auth services
            services.AddScoped<IUserManagerService, UserManagerService>();
            services.AddScoped<IEmailSendingService, SMTPEmailSendingService>();
            services.AddScoped<IAuthManagementService, AuthManagementService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>();

            // Repositories
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDynamicRowRepository, DynamicRowRepository>();
            services.AddScoped<IDynamicCardRepository, DynamicCardRepository>();

            // Menu and Layout services
            services.AddScoped<IDynamicMenuCategoryService, DynamicMenuCategoryService>();
            services.AddScoped<IDynamicPageService, DynamicPageService>();
            services.AddScoped<IDynamicRowService, DynamicRowService>();
            services.AddScoped<IDynamicCardService, DynamicCardService>();
            services.AddScoped<ILayoutService, LayoutService>();
            services.AddScoped<ISQLQueryService, SQLQueryService>();

            // Menu repositories
            services.AddScoped<IDynamicMenuCategoryRepository, DynamicMenuCategoryRepository>();
            services.AddScoped<IDynamicPageRepository, DynamicPageRepository>();

            return services;
        }

        public static IServiceCollection AddCustomLogging(this IServiceCollection services)
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

            return services;
        }

        public static async Task AddDynamicAssemblies(this IServiceCollection services, IConfiguration configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            using (var apiDbContext = new ApiDbContext(optionsBuilder.Options, configuration))
            {
                var serviceProvider = services.BuildServiceProvider();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<Startup>();
                var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
                var mvc = services.AddControllers();

                foreach(var connection in apiDbContext.QDBConnections.ToList())
                {
                    await AssemblyLoader.LoadAssemblyFromQDBConnection(connection, serviceProvider, mvc.PartManager, logger);
                }
                AssemblyLoader.RegenerateSwagger(swaggerProvider, logger);
            }
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
                
                var xmlFile = "Querier.Api.xml";
                var xmlPath = Path.Combine(System.AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                
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
            });

            return services;
        }
    }
} 