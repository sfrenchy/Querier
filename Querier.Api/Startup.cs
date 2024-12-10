using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Services;
using Querier.Api.Services.Role;
using Querier.Api.Services.UI;
using Querier.Api.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Querier.Api.Models.Interfaces;
using Newtonsoft.Json;
using Querier.Api.CustomTokenProviders;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Services.Repositories.Role;
using Querier.Api.Services.Repositories.User;
using Querier.Api.Tools;
using IQUploadService = Querier.Api.Services.IQUploadService;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace Querier.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(services);
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // Configurer Redis avec les valeurs par défaut pour le moment
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
            });

            // ... configuration de la base de données ...
            switch (_configuration["SQLEngine"])
            {
                case "SQLite":
                    services.AddDbContext<ApiDbContext>();
                    services.AddDbContextFactory<ApiDbContext>();
                    break;
                // ... autres cas ...
            }

            // Ajouter ISettingService après la configuration de la base de données
            services.AddScoped<ISettingService, SettingService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
                
                // Ensure this path matches where your XML file is actually being generated
                var xmlFile = $"Querier.Api.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                
                // Swagger 2.+ support
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
            var sqlEngine = _configuration.GetSection("SQLEngine").Get<string>();
            switch (sqlEngine)
            {
                default:
                    // Register DbContext options
                    services.AddSingleton<DbContextOptions<ApiDbContext>>(provider =>
                        new DbContextOptionsBuilder<ApiDbContext>()
                            .UseSqlite(_configuration.GetConnectionString("ApiDBConnection"))
                            .Options);

                    services.AddSingleton<DbContextOptions<UserDbContext>>(provider =>
                        new DbContextOptionsBuilder<UserDbContext>()
                            .UseSqlite(_configuration.GetConnectionString("ApiDBConnection"))
                            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                            .Options);

                    // Register DbContexts as scoped
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseSqlite(_configuration.GetConnectionString("ApiDBConnection")));

                    services.AddDbContext<UserDbContext>(options => 
                        options.UseSqlite(_configuration.GetConnectionString("ApiDBConnection"))
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

                    // Register factory
                    services.AddDbContextFactory<ApiDbContext>();
                    break;
                case "MSSQL":
                    services.AddDbContext<ApiDbContext>(options => 
                        options.UseSqlServer(_configuration.GetConnectionString("ApiDBConnection")));
                    services.AddDbContextFactory<ApiDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("ApiDBConnection")));
                    services.AddDbContext<UserDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("ApiDBConnection")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);
                    break;
                case "MySQL":
                    var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                    services.AddDbContext<ApiDbContext>(options => options.UseMySql(_configuration.GetConnectionString("ApiDBConnection"), serverVersion, x => x.MigrationsAssembly("HerdiaApp.Migration.MySQL")));
                    services.AddDbContextFactory<ApiDbContext>(options => options.UseMySql(_configuration.GetConnectionString("ApiDBConnection"), serverVersion));
                    services.AddDbContext<UserDbContext>(options => options.UseMySql(_configuration.GetConnectionString("ApiDBConnection"), serverVersion).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);
                    break;
                case "PgSQL":
                    services.AddDbContext<ApiDbContext>(options => options.UseNpgsql(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.PgSQL")));
                    services.AddDbContextFactory<ApiDbContext>(options => options.UseNpgsql(_configuration.GetConnectionString("ApiDBConnection")));
                    services.AddDbContext<UserDbContext>(options => options.UseNpgsql(_configuration.GetConnectionString("ApiDBConnection")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);
                    break;
            }
            
            services.Configure<JwtConfig>(_configuration.GetSection("JwtConfig"));
            services.Configure<IdentityOptions>(async options =>
            {
                using (var serviceProvider = services.BuildServiceProvider())
                {
                    var settingService = serviceProvider.GetRequiredService<ISettingService>();
                    
                    // Récupérer les règles de mot de passe depuis la BDD
                    var requireDigit = await settingService.GetSettingValue("api:password:requireDigit", "true");
                    var requireLowercase = await settingService.GetSettingValue("api:password:requireLowercase", "true");
                    var requireNonAlphanumeric = await settingService.GetSettingValue("api:password:requireNonAlphanumeric", "true");
                    var requireUppercase = await settingService.GetSettingValue("api:password:requireUppercase", "true");
                    var requiredLength = await settingService.GetSettingValue("api:password:requiredLength", "12");
                    var requiredUniqueChars = await settingService.GetSettingValue("api:password:requiredUniqueChars", "1");

                    // Appliquer les règles
                    options.Password.RequireDigit = bool.Parse(requireDigit);
                    options.Password.RequireLowercase = bool.Parse(requireLowercase);
                    options.Password.RequireNonAlphanumeric = bool.Parse(requireNonAlphanumeric);
                    options.Password.RequireUppercase = bool.Parse(requireUppercase);
                    options.Password.RequiredLength = int.Parse(requiredLength);
                    options.Password.RequiredUniqueChars = int.Parse(requiredUniqueChars);
                    options.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";
                }
            });
            services.AddDefaultIdentity<ApiUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
                .AddRoles<ApiRole>()
                .AddEntityFrameworkStores<UserDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<EmailConfirmationTokenProvider<ApiUser>>("emailconfirmation");
            services.AddScoped<ISettingService, SettingService>();
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var settingService = serviceProvider.GetRequiredService<ISettingService>();
                var validityDays = settingService.GetSettingValue("api:email:confirmationTokenValidityLifeSpanDays", "2").Result;
                var resetPasswordValidity = settingService.GetSettingValue("api:email:ResetPasswordTokenValidityLifeSpanMinutes", "15").Result;

                services.Configure<EmailConfirmationTokenProviderOptions>(opt =>
                {
                    opt.TokenLifespan = TimeSpan.FromDays(int.Parse(validityDays));
                });

                services.Configure<DataProtectionTokenProviderOptions>(opt =>
                {
                    opt.TokenLifespan = TimeSpan.FromMinutes(int.Parse(resetPasswordValidity));
                });
            }
            var key = Encoding.ASCII.GetBytes(_configuration["JwtConfig:Secret"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = false,

                // Allow to use seconds for expiration of token
                // Required only when token lifetime less than 5 minutes
                // THIS ONE
                ClockSkew = TimeSpan.Zero
            };

            services.AddSingleton(tokenValidationParameters);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IEntityCRUDService, EntityCRUDService>();
            services.AddScoped<Models.Interfaces.IQUploadService, IQUploadService>();
            services.AddScoped<IUICategoryService, UICategoryService>();
            services.AddScoped<IUIPageService, UIPageService>();
            services.AddScoped<IUIRowService, UIRowService>();
            services.AddScoped<IUICardService, UICardService>();
            services.AddScoped<IExportGeneratorService, ExportGeneratorService>();
            services.AddScoped<IWizardService, WizardService>();
            services.AddScoped<IUserManagerService, UserManagerService>();
            services.AddScoped<IEmailSendingService, SMTPEmailSendingService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthManagementService, AuthManagementService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IDBConnectionService, DBConnectionService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            
            // services.AddEntityFrameworkSqlServer()
            //     .AddLogging()
            //     .AddEntityFrameworkDesignTimeServices()
            //     .AddSingleton<LoggingDefinitions, SqlServerLoggingDefinitions>()
            //     .AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
            //     .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
            //     .AddSingleton<IDatabaseModelFactory, SqlServerDatabaseModelFactory>()
            //     .AddSingleton<IProviderConfigurationCodeGenerator, SqlServerCodeGenerator>()
            //     .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
            //     .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>();

            services.AddAuthorization();
            services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>();
            services.AddSignalR();
            services.AddHealthChecks();

            // we can use options pattern to support hooking your own configuration
            // because we don't use service registration api, 
            // we need to manually ensure the job is present in DI
            services.AddSingleton<IDynamicContextList, DynamicContextList>(_ => DynamicContextList.Instance);
            IMvcBuilder mvc = services.AddControllers();
            var assemblyPath = _configuration.GetSection("ApplicationSettings:AssemblyPath").Get<string>();
            var loadAssemblies = _configuration.GetSection("ApplicationSettings:LoadAssemblies").Get<List<string>>() ?? new List<string>();
            var pluginsStartupTypes = _configuration.GetSection("ApplicationSettings:PluginsStartupTypes").Get<List<string>>() ?? new List<string>();
            List<Type> pluginTypes = new List<Type>();
            List<Assembly> pluginAssemblies = new List<Assembly>();
            List<string> availableDynamicContexts = new List<string>();

            // Load dynamically API DB assemblies
            
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            /*
            using (ApiDbContext apiDbContext = new ApiDbContext(optionsBuilder.Options, _configuration))
            {
                apiDbContext.Database.EnsureCreated();
                
                foreach(QDBConnection connection in apiDbContext.QDBConnections.ToList())
                {
                    Console.WriteLine("Loading assembly for " + connection.Name);
                    var dbAssembly = Assembly.LoadFrom(connection.AssemblyUploadDefinition.Path);
                    pluginAssemblies.Add(dbAssembly);
                    Type dynamicInterfaceType = typeof(IDynamicContextProceduresServicesResolver);
                    if (dbAssembly.GetTypes().Any(t => dynamicInterfaceType.IsAssignableFrom(t)))
                    {
                        if (dbAssembly.GetTypes().Count(t => dynamicInterfaceType.IsAssignableFrom(t)) != 1)
                            throw new Exception("One and only one IDynamicContextProceduresServicesResolver per assembly must be implemented");

                        Type assemblyServiceResolverType = dbAssembly.GetTypes().First(t => dynamicInterfaceType.IsAssignableFrom(t));
                        IDynamicContextProceduresServicesResolver assemblyServiceResolver = (IDynamicContextProceduresServicesResolver) Activator.CreateInstance(assemblyServiceResolverType);
                        assemblyServiceResolver.ConfigureServices(services, connection.ConnectionString);
                        // services.AddSingleton(typeof(IDynamicContextProceduresServicesResolver), assemblyServiceResolver.GetType());
                        using (var tempProvider = services.BuildServiceProvider())
                        {
                            var dynamicContextListService = tempProvider.GetRequiredService<IDynamicContextList>();
                            Console.WriteLine($"Adding DynamicContext {connection.Name}");
                            dynamicContextListService.DynamicContexts.Add(connection.Name, assemblyServiceResolver);
                        }
                        foreach (KeyValuePair<Type, Type> service in assemblyServiceResolver.ProceduresServices)
                        {
                            //Console.WriteLine($"Registering service {service.Key}");
                            services.AddSingleton(service.Key, service.Value);
                        }

                        mvc.AddApplicationPart(dbAssembly);
                        Console.WriteLine($"New available dynamic context: {connection.Name}");
                        availableDynamicContexts.Add(assemblyServiceResolver.DynamicContextName);
                    }
                }
                
            }
            */
            foreach (string assemblyToLoad in loadAssemblies)
            {
                pluginAssemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyPath, assemblyToLoad)));
            }

            foreach (string startupType in pluginsStartupTypes)
            {
                Type type = Type.GetType(startupType, true);
                pluginTypes.Add(type);
                pluginAssemblies.Add(type.Assembly);
            }

            foreach (var ha in from Type plugin in pluginTypes
                     let ha = (IQPlugin)Activator.CreateInstance(plugin)
                     select ha)
            {
                var m = ha.GetSpecificProperties();
                if (m.RequiredDynamicContexts.All(i => availableDynamicContexts.Contains(i)))
                {
                    ha.ConfigureServices(services, _configuration);
                }
                else
                {
                    Console.WriteLine($"Unable to load services for application as I need some DynamicContexts ({String.Join(", ", m.RequiredDynamicContexts.ToArray())})");
                }
                services.AddSingleton(typeof(IQPlugin), ha);
            }

            foreach (Assembly a in pluginAssemblies)
            {
                mvc.AddApplicationPart(a);
            }

            mvc.AddNewtonsoftJson(options => {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // Mise à jour de la configuration du logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
                builder.AddDebug();
                
                // Définir le niveau de log minimum à Information ou Debug pour voir plus de détails
                builder.SetMinimumLevel(LogLevel.Debug);
                
                // Configuration spécifique pour certaines catégories
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddFilter("Querier.Api", LogLevel.Debug);
            });

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
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["JwtConfig:Issuer"],
                    ValidAudience = _configuration["JwtConfig:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"])),
                    ClockSkew = TimeSpan.Zero
                };
                // Ajoutez ces événements pour le debug
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogError($"Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });

            // Désactivez l'authentification par cookie si elle est configurée
            services.AddControllers();
            
            // Configurez CORS si nécessaire
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env,
            ApiDbContext dbContext)
        {
            // Créer un scope pour résoudre ISettingService
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();

                // Récupérer les paramètres CORS depuis la BDD
                var allowedHosts = settingService.GetSettingValue("api:allowedHosts", "*").Result?.Split(',');
                var allowedOrigins = settingService.GetSettingValue("api:allowedOrigins", "*").Result?.Split(',');
                var allowedMethods = settingService.GetSettingValue("api:allowedMethods", "GET,POST,DELETE,OPTIONS,PUT").Result?.Split(',');
                var allowedHeaders = settingService.GetSettingValue("api:allowedHeaders", "X-Request-Token,Accept,Content-Type,Authorization").Result?.Split(',');

                app.UseCors(builder =>
                {
                    if (allowedOrigins.Contains("*"))
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(allowedOrigins);
                    }

                    if (allowedHeaders.Contains("*"))
                    {
                        builder.AllowAnyHeader();
                    }
                    else
                    {
                        builder.WithHeaders(allowedHeaders);
                    }

                    if (allowedMethods.Contains("*"))
                    {
                        builder.AllowAnyMethod();
                    }
                    else
                    {
                        builder.WithMethods(allowedMethods);
                    }
                });
            }

            ServiceActivator.Configure(app.ApplicationServices);
            

            app.UseDeveloperExceptionPage();
            //app.UseExceptionHandler("/error");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // Configurer les endpoints publics explicitement
                endpoints.MapControllerRoute(
                    name: "public",
                    pattern: "api/v1/settings/configured",
                    defaults: new { controller = "PublicSettings", action = "GetIsConfigured" }
                ).WithMetadata(new AllowAnonymousAttribute());
            });

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));
            
            app.UseHealthChecks("/healthcheck");
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
            var pluginsStartupTypes = _configuration.GetSection("ApplicationSettings:PluginsStartupTypes").Get<List<string>>() ?? new List<string>();
            List<Type> pluginTypes = new List<Type>();
            if (pluginsStartupTypes != null)
            {
                foreach (string startupType in pluginsStartupTypes)
                {
                    Type type = Type.GetType(startupType, true);
                    pluginTypes.Add(type);
                }

                foreach (var ha in from Type plugin in pluginTypes
                                let ha = (IQPlugin)Activator.CreateInstance(plugin)
                                select ha)
                {
                    ha.ConfigureApp(app, env);
                    ha.CreateTemplateEmail().GetAwaiter().GetResult();
                }
            }
        }
    }
}
