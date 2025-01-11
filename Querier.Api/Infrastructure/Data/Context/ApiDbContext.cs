using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Domain.Entities;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Data.Context
{
    public partial class ApiDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiDbContext> _logger;

        public ApiDbContext(
            DbContextOptions<ApiDbContext> options, 
            IConfiguration configuration,
            ILogger<ApiDbContext> logger) 
            : base(options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<DBConnection> DBConnections { get; set; }
        public virtual DbSet<ApiRole> ApiRoles { get; set; }
        public virtual DbSet<ApiUserRole> ApiUserRoles { get; set; }
        public virtual DbSet<Menu> Menus { get; set; }
        public virtual DbSet<MenuTranslation> MenuTranslations { get; set; }
        public virtual DbSet<Page> Pages { get; set; }
        public virtual DbSet<PageTranslation> PageTranslations { get; set; }
        public virtual DbSet<Row> Rows { get; set; }
        public virtual DbSet<Card> Cards { get; set; }
        public virtual DbSet<CardTranslation> CardTranslations { get; set; }
        public DbSet<SQLQuery> SQLQueries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                _logger.LogDebug("Configuring database connection");

                var sqlEngine = _configuration.GetSection("SQLEngine").Get<string>();
                if (string.IsNullOrEmpty(sqlEngine))
                {
                    _logger.LogWarning("SQLEngine not specified in configuration, defaulting to SQLite");
                    sqlEngine = "SQLite";
                }

                var connectionString = _configuration.GetConnectionString("ApiDBConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    const string error = "Database connection string is not configured";
                    _logger.LogError(error);
                    throw new InvalidOperationException(error);
                }

                _logger.LogInformation("Configuring database with engine: {SqlEngine}", sqlEngine);

                switch (sqlEngine.ToUpper())
                {
                    case "SQLITE":
                default:
                        _logger.LogDebug("Using SQLite database");
                        optionsBuilder.UseLazyLoadingProxies()
                            .UseSqlite(connectionString);
                    break;

                case "MSSQL":
                        _logger.LogDebug("Using SQL Server database");
                        optionsBuilder.UseLazyLoadingProxies()
                            .UseSqlServer(connectionString, x => 
                                x.MigrationsAssembly("HerdiaApp.Migration.SqlServer"));
                    break;

                    case "MYSQL":
                        _logger.LogDebug("Using MySQL/MariaDB database");
                    var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                        optionsBuilder.UseLazyLoadingProxies()
                            .UseMySql(connectionString, serverVersion, x => 
                                x.MigrationsAssembly("HerdiaApp.Migration.MySQL"));
                    break;

                    case "PGSQL":
                        _logger.LogDebug("Using PostgreSQL database");
                    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                    AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
                        optionsBuilder.UseLazyLoadingProxies()
                            .UseNpgsql(connectionString, x => 
                                x.MigrationsAssembly("HerdiaApp.Migration.PgSQL"));
                    break;
                }

                _logger.LogInformation("Database configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring database connection");
                throw;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            try
            {
                _logger.LogDebug("Starting database model configuration");

            base.OnModelCreating(modelBuilder);

                ConfigureRoles(modelBuilder);
                ConfigureSettings(modelBuilder);
                ConfigureUserRoles(modelBuilder);
                ConfigureRefreshTokens(modelBuilder);
                ConfigureDBConnections(modelBuilder);
                ConfigureMenus(modelBuilder);
                ConfigurePages(modelBuilder);
                ConfigureCards(modelBuilder);
                ConfigureSQLQueries(modelBuilder);

                _logger.LogInformation("Database model configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring database model");
                throw;
            }
        }

        private void ConfigureRoles(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring roles");

            modelBuilder.Entity<ApiRole>().HasData(
                new ApiRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new ApiRole { Id = "2", Name = "Database Manager", NormalizedName = "DATABASE MANAGER" },
                new ApiRole { Id = "3", Name = "Content Manager", NormalizedName = "CONTENT MANAGER" },
                new ApiRole { Id = "4", Name = "User", NormalizedName = "USER" }
            );
        }

        private void ConfigureSettings(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring settings");

            modelBuilder.Entity<Setting>().HasData(new Setting
            {
                Id = 1,
                Name = "api:isConfigured",
                Value = "false",
                Description = "Indicate if the application is configured",
                Type = typeof(bool).ToString()
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }

        private void ConfigureUserRoles(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring user roles");

            modelBuilder.Entity<ApiUserRole>(entity =>
            {
                entity.ToTable("AspNetUserRoles");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            });
        }

        private void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring refresh tokens");

            modelBuilder.Entity<RefreshToken>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureDBConnections(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring database connections");

            modelBuilder.Entity<DBConnection>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<DBConnection>()
                .HasIndex(d => d.ApiRoute)
                .IsUnique();
        }

        private void ConfigureMenus(ModelBuilder modelBuilder)
            {
            _logger.LogDebug("Configuring menus");

            modelBuilder.Entity<Menu>(entity =>
            {
                entity.ToTable("Menus");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Route).HasMaxLength(100);
                entity.Property(e => e.Roles).HasMaxLength(1000);
                entity.HasIndex(e => e.Order);
            });

            modelBuilder.Entity<MenuTranslation>(entity =>
            {
                entity.ToTable("MenuTranslations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { MenuId = e.MenuId, e.LanguageCode }).IsUnique();
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

                entity.HasOne(d => d.Menu)
                    .WithMany(p => p.Translations)
                    .HasForeignKey(d => d.MenuId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            SeedMenuData(modelBuilder);
        }

        private void SeedMenuData(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Seeding menu data");

            modelBuilder.Entity<Menu>().HasData(
                new Menu
                {
                    Id = 1,
                    Icon = "home",
                    Order = 1,
                    IsVisible = true,
                    Roles = "Admin,User",
                    Route = "/home"
                }
            );

            modelBuilder.Entity<MenuTranslation>().HasData(
                new MenuTranslation
                {
                    Id = 1,
                    MenuId = 1,
                    LanguageCode = "en",
                    Name = "Home"
                },
                new MenuTranslation
                {
                    Id = 2,
                    MenuId = 1,
                    LanguageCode = "fr",
                    Name = "Accueil"
                }
            );
        }

        private void ConfigurePages(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring pages");

            modelBuilder.Entity<Page>(entity =>
            {
                entity.ToTable("Pages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Route).HasMaxLength(100);
                entity.Property(e => e.Roles).HasMaxLength(1000);
                
                entity.HasOne(e => e.Menu)
                      .WithMany(e => e.Pages)
                      .HasForeignKey(e => e.MenuId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.Order);
            });

            modelBuilder.Entity<PageTranslation>(entity =>
            {
                entity.ToTable("PageTranslations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PageId, e.LanguageCode }).IsUnique();
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

                entity.HasOne(d => d.Page)
                      .WithMany(p => p.PageTranslations)
                      .HasForeignKey(d => d.PageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            SeedPageData(modelBuilder);
        }

        private void SeedPageData(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Seeding page data");

            modelBuilder.Entity<Page>().HasData(
                new Page
                {
                    Id = 1,
                    Icon = "dashboard",
                    Order = 1,
                    IsVisible = true,
                    Roles = "Admin,User",
                    Route = "/northwind/home",
                    MenuId = 1
                }
            );

            modelBuilder.Entity<PageTranslation>().HasData(
                new PageTranslation
                {
                    Id = 1,
                    PageId = 1,
                    LanguageCode = "fr",
                    Name = "Northwind - Accueil"
                },
                new PageTranslation
                {
                    Id = 2,
                    PageId = 1,
                    LanguageCode = "en",
                    Name = "Northwind - Home"
                }
            );
        }

        private void ConfigureCards(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring cards");

            modelBuilder.Entity<Row>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                
                entity.HasOne(d => d.Page)
                      .WithMany(p => p.Rows)
                      .HasForeignKey(d => d.PageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Configuration).HasColumnType("json");
                
                entity.HasOne(d => d.Row)
                      .WithMany(r => r.Cards)
                      .HasForeignKey(d => d.RowId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CardTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CardId, e.LanguageCode }).IsUnique();
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.CardTranslations)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureSQLQueries(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring SQL queries");

            modelBuilder.Entity<SQLQuery>(entity =>
            {
                var converter = new ValueConverter<Dictionary<string, object>, string>(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));

                var comparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)),
                    c => new Dictionary<string, object>(c));

                entity.Property(e => e.Parameters)
                    .HasConversion(converter)
                    .Metadata.SetValueComparer(comparer);
            });
        }

        public override void Dispose()
        {
            try
            {
                _logger.LogDebug("Disposing database context");
                base.Dispose();
                _logger.LogDebug("Database context disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing database context");
                throw;
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                _logger.LogDebug("Disposing database context asynchronously");
                await base.DisposeAsync();
                _logger.LogDebug("Database context disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing database context asynchronously");
                throw;
            }
        }
    }
}
