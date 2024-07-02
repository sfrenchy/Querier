using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Services;
using Querier.Api.Services.MQServices;
using Querier.Api.Services.Role;
using Querier.Api.Services.UI;
using Querier.Api.Services.User;
using Querier.Tools;
using KissLog;
using KissLog.AspNetCore;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Formatters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Quartz.Spi;
using Quartz.Impl.Matchers;
using System.Runtime.Loader;
using Querier.Api.Models.Interfaces;
using Querier.Api.Services.Ged;
using Querier.Api.Services.Factory;
using Querier.Api.Models.Enums;
using System.Collections;
using System.Resources;
using Querier.Api.CustomTokenProviders;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Quartz;
using Querier.Api.Services.Repositories.Application;
using Querier.Api.Services.Repositories.Role;
using Querier.Api.Services.Repositories.User;

namespace Querier.Api
{
    public class Startup
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IWebHostEnvironment hostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _environment = hostEnvironment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var clientId = _configuration.GetSection("GoogleAuthSettings:ClientID").Get<string>();
            var clientSecret = _configuration.GetSection("GoogleAuthSettings:ClientSecret").Get<string>();
            
            services.AddSingleton(services);
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddStackExchangeRedisCache(options => { options.Configuration = _configuration["RedisCacheUrl"]; });
            services.AddLogging(logging =>
            {
                logging.AddKissLog(options =>
                {
                    options.Formatter = (FormatterArgs args) =>
                    {
                        if (args.Exception == null)
                            return args.DefaultValue;

                        string exceptionStr = new ExceptionFormatter().Format(args.Exception, args.Logger);

                        return string.Join(Environment.NewLine, new[] { args.DefaultValue, exceptionStr });
                    };
                });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
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
            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();
            switch (SQLEngine)
            {
                default:
                case "MSSQL":
                    services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.SqlServer")), ServiceLifetime.Singleton);
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
                case "Oracle":
                    services.AddDbContext<ApiDbContext>(options => options.UseOracle(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.Oracle")));
                    services.AddDbContextFactory<ApiDbContext>(options => options.UseOracle(_configuration.GetConnectionString("ApiDBConnection")));
                    services.AddDbContext<UserDbContext>(options => options.UseOracle(_configuration.GetConnectionString("ApiDBConnection")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);

                    break;
            }
            
            services.Configure<JwtConfig>(_configuration.GetSection("JwtConfig"));
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireDigit").Get<bool>();
                options.Password.RequireLowercase = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireLowercase").Get<bool>();
                options.Password.RequireNonAlphanumeric = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireNonAlphanumeric").Get<bool>();
                options.Password.RequireUppercase = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireUppercase").Get<bool>();
                options.Password.RequiredLength = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequiredLength").Get<int>();
                options.Password.RequiredUniqueChars = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequiredUniqueChars").Get<int>();
                options.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";
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
            services.AddAuthentication()
                .AddGoogle(opts =>
                {
                    opts.ClientId = clientId;
                    opts.ClientSecret = clientSecret;
                    opts.SignInScheme = IdentityConstants.ExternalScheme;
                });
            services.Configure<EmailConfirmationTokenProviderOptions>(opt =>
                opt.TokenLifespan = TimeSpan.FromDays(_configuration.GetSection("ApplicationSettings:EmailConfirmationTokenValidityLifeSpanDays").Get<int>()));
            services.Configure<DataProtectionTokenProviderOptions>(opt =>
                opt.TokenLifespan = TimeSpan.FromMinutes(_configuration.GetSection("ApplicationSettings:ResetPasswordTokenValidityLifeSpanMinutes").Get<int>()));
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
            services.AddSingleton<IEntityCRUDService, EntityCRUDService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IQTaskScheduler, QTaskScheduler>();
            services.AddSingleton<IQUploadService, IqUploadService>();
            services.AddSingleton<IQTranslationService, QTranslationService>();
            services.AddScoped<IAuthManagementService, AuthManagementService>();
            services.AddSingleton<IEditModeService, EditModeService>();
            services.AddSingleton<IUICategoryService, UICategoryService>();
            services.AddSingleton<IUIPageService, UIPageService>();
            services.AddSingleton<IUIRowService, UIRowService>();
            services.AddSingleton<IUICardService, UICardService>();
            services.AddSingleton<IHtmlPartialService, HtmlPartialService>();
            services.AddScoped<IUserManagerService, UserManagerService>();
            services.AddSingleton<IEmailSendingService, SMTPEmailSendingService>();
            services.AddSingleton<IEmailTemplateCrudUserService, EmailTemplateCrudUserService>();
            services.AddScoped<IEmailTemplateCrudCommonService, EmailTemplateCrudCommonService>();
            services.AddSingleton<IQFileReadOnlyDeposit, GedDocuwareService>();
            services.AddSingleton<FileDepositFactory, FileDepositFactory>();
            services.AddSingleton<IFileDepositService, FileDepositService>();
            services.AddHostedService<ToastMessageReceiverService>();
            services.AddHostedService<DataExportReceiverService>();
            services.AddHostedService<DataImportReceiverService>();
            services.AddSingleton<IDynamicContextResolver, DynamicContextResolver>();
            services.AddSingleton<ICacheManagementService, CacheManagementService>();
            services.AddSingleton<IExportGeneratorService, ExportGeneratorService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwt =>
            {
                jwt.SaveToken = true;
                jwt.TokenValidationParameters = tokenValidationParameters;
                jwt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        if (!path.StartsWithSegments("/notificationhub")) return Task.CompletedTask;
                        using (var serviceScope = ServiceActivator.GetScope())
                        {
                            IOptionsMonitor<JwtConfig> option = (IOptionsMonitor<JwtConfig>)serviceScope.ServiceProvider.GetService(typeof(IOptionsMonitor<JwtConfig>));
                            UserManager<ApiUser> userManager = serviceScope.ServiceProvider.GetService<UserManager<ApiUser>>();
                            ApiDbContext apiDbContext = serviceScope.ServiceProvider.GetService<ApiDbContext>();
                            // If the request is for our hub...
                            var userMail = context.Request.Query["email"];

                            ApiUser user = userManager.FindByEmailAsync(userMail).GetAwaiter().GetResult();
                            var jwtTokenHandler = new JwtSecurityTokenHandler();
                            var key = Encoding.ASCII.GetBytes(option.CurrentValue.Secret);
                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new ClaimsIdentity(new[]
                                {
                                    new Claim("Id", user.Id),
                                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                                }),
                                Expires = DateTime.UtcNow.Add(option.CurrentValue.ExpiryTimeFrame),
                                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                            };

                            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
                            var jwtToken = jwtTokenHandler.WriteToken(token);

                            var refreshToken = new QRefreshToken()
                            {
                                JwtId = token.Id,
                                IsUsed = false,
                                UserId = user.Id,
                                AddedDate = DateTime.UtcNow,
                                ExpiryDate = DateTime.UtcNow.AddYears(1),
                                IsRevoked = false,
                                Token = Utils.RandomString(25) + Guid.NewGuid()
                            };

                            apiDbContext.QRefreshTokens.AddAsync(refreshToken).GetAwaiter().GetResult();
                            apiDbContext.SaveChangesAsync().GetAwaiter().GetResult();

                            context.Token = jwtToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            services.AddSingleton<INotification, Notification>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddSingleton<IToastMessageEmitterService, ToastMessageEmitterService>();
            services.AddScoped<IDBConnectionService, DBConnectionService>();
            
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

            services.Configure<QuartzOptions>(options =>
            {
                options.Scheduling.IgnoreDuplicates = true; // default: false
                options.Scheduling.OverWriteExistingData = true; // default: true
            });

            services.AddQuartz(q =>
            {
                // handy when part of cluster or you want to otherwise identify multiple schedulers
                q.SchedulerId = "Scheduler-CoreId";
                q.SchedulerName = "Scheduler-Core";

                // we take this from appsettings.json, just show it's possible
                // q.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";

                // as of 3.3.2 this also injects scoped services (like EF DbContext) without problems
                q.UseMicrosoftDependencyInjectionJobFactory();

                // or for scoped service support like EF Core DbContext
                // q.UseMicrosoftDependencyInjectionScopedJobFactory();

                // these are the defaults
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 10;
                });
                q.UsePersistentStore(s =>
                {
                    s.PerformSchemaValidation = false; // default
                    s.UseProperties = true; // preferred, but not default
                    switch (SQLEngine)
                    {
                        default:
                        case "MSSQL":
                            s.UseSqlServer(sql =>
                            {
                                sql.ConnectionString = _configuration.GetConnectionString("ApiDBConnection");
                                sql.TablePrefix = "QRTZ_";
                            });
                            break;
                        case "MySQL":
                            s.UseMySqlConnector(sql =>
                            {
                                sql.ConnectionString = _configuration.GetConnectionString("ApiDBConnection");
                                sql.TablePrefix = "QRTZ_";
                            });
                            break;
                        case "PgSQL":
                            s.UsePostgres(sql =>
                            {
                                sql.ConnectionString = _configuration.GetConnectionString("ApiDBConnection");
                                sql.TablePrefix = "QRTZ_";
                            });
                            break;
                        case "Oracle":
                            s.UseOracle(sql =>
                            {
                                sql.ConnectionString = _configuration.GetConnectionString("ApiDBConnection");
                                sql.TablePrefix = "QRTZ_";
                            });
                            break;
                    }
                    s.UseJsonSerializer();
                    s.UseClustering(c =>
                    {
                        c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                        c.CheckinInterval = TimeSpan.FromSeconds(10);
                    });
                });
            });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
            // we can use options pattern to support hooking your own configuration
            // because we don't use service registration api, 
            // we need to manually ensure the job is present in DI
            services.AddTransient<DeleteUploadJob>();
            services.AddTransient<UpdateFileDeposit>();
            services.AddSingleton<IDynamicContextList, DynamicContextList>(sp => { return DynamicContextList.Instance; });
            IMvcBuilder mvc = services.AddControllers();
            var assemblyPath = _configuration.GetSection("ApplicationSettings:AssemblyPath").Get<string>();
            var loadAssemblies = _configuration.GetSection("ApplicationSettings:LoadAssemblies").Get<List<string>>() ?? new List<string>();
            var pluginsStartupTypes = _configuration.GetSection("ApplicationSettings:PluginsStartupTypes").Get<List<string>>() ?? new List<string>();
            List<Type> pluginTypes = new List<Type>();
            List<Assembly> pluginAssemblies = new List<Assembly>();
            List<string> availableDynamicContexts = new List<string>();

            // Load dynamically API DB assemblies
            
            var provider = services.BuildServiceProvider();
            Console.WriteLine("Configuring Dynamic Contexts");
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            
            using (ApiDbContext apiDbContext = new ApiDbContext(optionsBuilder.Options, _configuration))
            {
                apiDbContext.Database.EnsureCreated();
                // Check if QuartzModel exists, deploy if needed
                if (apiDbContext.Database.IsMySql())
                {
                    object qrtzExists = apiDbContext.ExecuteScalar(@"
                                SELECT COUNT(TABLE_NAME)
                                FROM 
                                    information_schema.TABLES 
                                WHERE 
                                    TABLE_SCHEMA LIKE 'HAAPIDB' AND 
                                    TABLE_TYPE LIKE 'BASE TABLE' AND
                                    TABLE_NAME = 'QRTZ_CALENDARS';");
                    if (Convert.ToInt32(qrtzExists) == 0) {
                        string initDBFilePath = "Quartz/Scripts/MYSQL.sql";
                        apiDbContext.Database.ExecuteSqlRaw(File.ReadAllText(initDBFilePath));
                    }
                }

                if (apiDbContext.Database.IsSqlServer())
                {
                    object qrtzExists = apiDbContext.ExecuteScalar(@"
                                SELECT COUNT(*) 
                                    FROM INFORMATION_SCHEMA.TABLES 
                                    WHERE TABLE_SCHEMA = 'dbo' 
                                    AND  TABLE_NAME = 'QRTZ_CALENDARS'");
                    if (Convert.ToInt32(qrtzExists) == 0) {
                        string initDBFilePath = "Quartz/Scripts/MSSQL.sql";
                        apiDbContext.Database.ExecuteSqlRaw(File.ReadAllText(initDBFilePath).Replace("GO",""));
                    }
                }

                if (apiDbContext.Database.IsNpgsql())
                {
                    object qrtzExists = apiDbContext.ExecuteScalar(@"
                                SELECT 
                                    COUNT(table_name)
                                FROM 
                                    information_schema.tables 
                                WHERE 
                                    table_schema LIKE 'HAAPIDB' AND 
                                    table_type LIKE 'BASE TABLE' AND
                                    table_name = 'QRTZ_CALENDARS';");
                    if (Convert.ToInt32(qrtzExists) == 0) {
                        string initDBFilePath = "Quartz/Scripts/PGSQL.sql";
                        apiDbContext.Database.ExecuteSqlRaw(File.ReadAllText(initDBFilePath));
                    }
                }

                if (apiDbContext.Database.IsOracle())
                {
                    object qrtzExists = apiDbContext.ExecuteScalar(@"
                                SELECT COUNT(table_name )
                                FROM USER_TABLES
                                WHERE table_name='QRTZ_CALENDARS'");
                    if (Convert.ToInt32(qrtzExists) == 0) {
                        //string initDBFilePath = "Quartz/Scripts/ORACLE.sql";
                        //apiDbContext.Database.ExecuteSqlRaw(File.ReadAllText(initDBFilePath));
                        string initDBFilePath = "Quartz/Scripts/ORACLE.sql";
                        string[] sqlStatements = File.ReadAllText(initDBFilePath).Split(';');

                        foreach (string sqlStatement in sqlStatements)
                        {
                            if (!string.IsNullOrWhiteSpace(sqlStatement))
                            {
                                apiDbContext.Database.ExecuteSqlRaw(sqlStatement);
                            }
                        }
                    }
                }
                Console.WriteLine("Quartz.Net model OK");
                foreach(QDBConnection connection in apiDbContext.QDBConnections.ToList())
                {
                    Console.WriteLine("Loading assembly for " + connection.Name);
                    var loadContext = new AssemblyLoadContext(null, false); 
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
                        var dynamicContextListService = provider.GetRequiredService<IDynamicContextList>();
                        Console.WriteLine($"Adding DynamicContext {connection.Name}");
                        dynamicContextListService.DynamicContexts.Add(connection.Name, assemblyServiceResolver);
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
            
            foreach (string assemblyToLoad in loadAssemblies)
            {
                pluginAssemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyPath, assemblyToLoad)));
            }

            if (pluginsStartupTypes != null)
            {
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
                    Features.EnabledFeatures.AddRange(m.Features);
                    Features.ApplicationName = m.Title;
                    Features.ApplicationIcon = m.Icon;
                    Features.ApplicationBackgroundLogin = m.BackgroundLogin;
                    Features.ApplicationDefaultTheme = m.ApplicationDefaultTheme;
                    Features.ApplicationRightPanelPackageName = m.RightPanelPackageName;
                    Features.ApplicationUserAttributes = m.ApplicationUserAttributes;
                    Features.ApplicationUserProperties = m.ApplicationUserProperties;
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
            }
            
            foreach (Assembly a in pluginAssemblies)
            {
                mvc.AddApplicationPart(a);
            }

            mvc.AddNewtonsoftJson(options => {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ServiceActivator.Configure(app.ApplicationServices);
            

            app.UseDeveloperExceptionPage();
            //app.UseExceptionHandler("/error");

            app.UseAuthentication();
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();

            app.UseCors(builder =>
            {
                if (_configuration["AllowAllCrossOrigins"] == "True")
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    builder
                        .WithOrigins(_configuration["AllowedOriginsList"])
                        .WithHeaders(_configuration["AllowedHeadersList"])
                        .WithMethods(_configuration["AllowedMethodsList"]);
                }
            });
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));
            
            app.UseHealthChecks("/healthcheck");
            app.UseStaticFiles();

            app.UseKissLogMiddleware(options => ConfigureKissLog(options));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationHub>("/notificationhub").RequireCors(builder =>
                {
                    if (_configuration["AllowAllCrossOrigins"] == "True")
                    {
                        builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(_configuration["AllowedOriginsList"])
                            .WithHeaders(_configuration["AllowedHeadersList"])
                            .WithMethods(_configuration["AllowedMethodsList"]);
                    }
                });
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

            CreateJobByDefaultAsync().GetAwaiter().GetResult();
            CreateTemplateEmail().GetAwaiter().GetResult();
            CreateJobForUpdateFileDepositActive().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Used to create needed directories in the content directory
        /// </summary>
        /// <param name="requiredContentDirectories">The list of required directories</param>
        private void EnsurePathExists(List<string> requiredContentDirectories)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;

            foreach (string requiredDirectory in requiredContentDirectories)
            {
                string path = Path.Combine(webRootPath, requiredDirectory);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
        }

        private void ConfigureKissLog(IOptionsBuilder options)
        {
            KissLogConfiguration.Listeners
                .Add(new RequestLogsApiListener(new Application(_configuration["KissLog.OrganizationId"], _configuration["KissLog.ApplicationId"]))
                {
                    ApiUrl = _configuration["KissLog.ApiUrl"]
                });
        }

        private async Task CreateJobByDefaultAsync()
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                IScheduler scheduler;

                var schedulerFactory = serviceScope.ServiceProvider.GetService<ISchedulerFactory>();
                var jobFactory = serviceScope.ServiceProvider.GetService<IJobFactory>();

                scheduler = await schedulerFactory.GetScheduler();
                scheduler.JobFactory = jobFactory;

                List<IJobDetail> jobs = new List<IJobDetail>();

                //Retrieve all existing job keys for a given scheduler
                foreach (JobKey jobKey in await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
                {
                    jobs.Add(await scheduler.GetJobDetail(jobKey));
                }

                bool deleteUploadJob = jobs.Any(job => job.JobType == typeof(DeleteUploadJob));
                if (!deleteUploadJob)
                {
                    IJobDetail newJob = JobBuilder
                       .Create(typeof(DeleteUploadJob))
                       .WithIdentity("DeleteUploadJob.StartUp")
                       .WithDescription("a job that will allow to delete twice a day the uploads that fulfil the management rules")
                       .UsingJobData("Creator", "System")
                       .PersistJobDataAfterExecution()
                       .Build();

                    ITrigger newTrigger = TriggerBuilder
                       .Create()
                       .WithIdentity("DeleteUpload.Trigger")
                       .WithCronSchedule("0 0 0/12 ? * * *", x => x
                            .InTimeZone(TimeZoneInfo.Local))
                       .WithDescription("a trigger that runs twice a day")
                       .UsingJobData("Creator", "System")
                       .Build();

                    await scheduler.ScheduleJob(newJob, newTrigger);
                }
            }
        }

        private async Task CreateJobForUpdateFileDepositActive()
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                IScheduler scheduler;

                var schedulerFactory = serviceScope.ServiceProvider.GetService<ISchedulerFactory>();
                var jobFactory = serviceScope.ServiceProvider.GetService<IJobFactory>();

                scheduler = await schedulerFactory.GetScheduler();
                scheduler.JobFactory = jobFactory;

                List<IJobDetail> jobs = new List<IJobDetail>();

                //Retrieve all existing job keys for a given scheduler
                foreach (JobKey jobKey in await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
                {
                    jobs.Add(await scheduler.GetJobDetail(jobKey));
                }

                bool updateFileDepositJob = jobs.Any(job => job.JobType == typeof(UpdateFileDeposit));
                if (!updateFileDepositJob)
                {
                    IJobDetail newJob = JobBuilder
                       .Create(typeof(UpdateFileDeposit))
                       .WithIdentity("UpdateFileDeposit.StartUp")
                       .WithDescription("a job which will update the file deposit information in db.")
                       .UsingJobData("Creator", "System")
                       .PersistJobDataAfterExecution()
                       .Build();

                    ITrigger newTrigger = TriggerBuilder
                       .Create()
                       .WithIdentity("UpdateFileDeposit.Trigger")
                       .WithCronSchedule("0 */15 * ? * *", x => x
                            .InTimeZone(TimeZoneInfo.Local))
                       .WithDescription("a trigger that runs every 15 minutes")
                       .UsingJobData("Creator", "System")
                       .Build();

                    await scheduler.ScheduleJob(newJob, newTrigger);
                }
            }
        }

        //Add email template dynamically, you just need to add the origin template in the directory Services/MailTemplating with the extension ".html"
        private async Task CreateTemplateEmail()
        {
            //use to have IQUploadService and IDbContextFactory services 
            using (var serviceScope = ServiceActivator.GetScope())
            {
                IQUploadService uploadSrv;
                IDbContextFactory<ApiDbContext> dbContextFactory;

                uploadSrv = serviceScope.ServiceProvider.GetService<IQUploadService>();
                dbContextFactory = serviceScope.ServiceProvider.GetService<IDbContextFactory<ApiDbContext>>();

                ResourceSet resourceSet = Properties.Resources.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true);
                List<string> resourceNames = new List<string>();
                foreach (DictionaryEntry resource in resourceSet)
                {
                    string resourceName = (string)resource.Key;
                    int lastDotIndex = resourceName.LastIndexOf('.');
                    string languageCode = "";
                    if (lastDotIndex != -1)
                    {
                        languageCode = resourceName.Substring(lastDotIndex);
                        if (languageCode.Contains(".de") || languageCode.Contains(".en") || languageCode.Contains(".fr"))
                            resourceNames.Add(resourceName);
                    }

                }

                foreach (string resourceName in resourceNames)
                {
                    //check if the template already exist 
                    List<QUploadDefinition> templateCount;

                    using (var apidbContext = dbContextFactory.CreateDbContext())
                    {
                        templateCount = apidbContext.QUploadDefinitions.Where(t => t.Nature == QUploadNatureEnum.ApplicationEmail && t.FileName == resourceName).ToList();
                    }

                    //test if the default template exist and if the template already exist in db 
                    if (templateCount.Count() == 0)
                    {
                        // convert string to stream
                        byte[] byteArray = Encoding.UTF8.GetBytes(Properties.Resources.ResourceManager.GetString(resourceName));
                        //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                        MemoryStream stream = new MemoryStream(byteArray);

                        HAUploadDefinitionFromApi requestParam = new HAUploadDefinitionFromApi()
                        {
                            Definition = new SimpleUploadDefinition
                            {
                                FileName = resourceName,
                                MimeType = "text/html",
                                Nature = QUploadNatureEnum.ApplicationEmail
                            },
                            UploadStream = stream
                        };
                        var result = await uploadSrv.UploadFileFromApiAsync(requestParam);
                    }
                }
            }
        }
    }
}
