using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Data.Context
{
    public partial class ApiDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration { get; }
        public virtual DbSet<QRefreshToken> QRefreshTokens { get; set; }
        public virtual DbSet<QSetting> QSettings { get; set; }
        public virtual DbSet<Domain.Entities.QDBConnection.QDBConnection> QDBConnections { get; set; }
        public DbSet<ApiRole> ApiRoles { get; set; }
        public DbSet<ApiUserRole> ApiUserRoles { get; set; }
        public virtual DbSet<DynamicMenuCategory> DynamicMenuCategories { get; set; }
        public virtual DbSet<DynamicMenuCategoryTranslation> DynamicMenuCategoryTranslations { get; set; }
        public virtual DbSet<DynamicPage> DynamicPages { get; set; }
        public virtual DbSet<DynamicRow> DynamicRows { get; set; }
        public virtual DbSet<DynamicCard> DynamicCards { get; set; }
        public virtual DbSet<DynamicCardTranslation> DynamicCardTranslations { get; set; }
        public virtual DbSet<DynamicPageTranslation> DynamicPageTranslations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();
            switch (SQLEngine)
            {
                default:
                    optionsBuilder.UseLazyLoadingProxies().UseSqlite(_configuration.GetConnectionString("ApiDBConnection"));
                    break;
                case "MSSQL":
                    optionsBuilder.UseLazyLoadingProxies().UseSqlServer(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.SqlServer"));
                    break;
                case "MySQL":
                    var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                    optionsBuilder.UseLazyLoadingProxies().UseMySql(_configuration.GetConnectionString("ApiDBConnection"), serverVersion, x => x.MigrationsAssembly("HerdiaApp.Migration.MySQL"));
                    break;
                case "PgSQL":
                    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                    AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
                    optionsBuilder.UseLazyLoadingProxies().UseNpgsql(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.PgSQL"));
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed roles
            modelBuilder.Entity<ApiRole>().HasData(
                new ApiRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new ApiRole { Id = "2", Name = "Database Manager", NormalizedName = "DATABASE MANAGER" },
                new ApiRole { Id = "3", Name = "Content Manager", NormalizedName = "CONTENT MANAGER" },
                new ApiRole { Id = "4", Name = "User", NormalizedName = "USER" }
            );

            // Seeding isConfigured variable
            modelBuilder.Entity<QSetting>().HasData(new QSetting
            {
                Id = 1,
                Name = "api:isConfigured",
                Value = "false",
                Description = "Indicate if the application is configured",
                Type = "boolean"
            });

            modelBuilder.Entity<ApiUserRole>(entity =>
            {
                entity.ToTable("AspNetUserRoles");

                entity.HasOne<ApiUser>()
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                entity.HasOne<ApiRole>()
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            });

            var hasher = new PasswordHasher<ApiUser>();

            //add delete cascade on foreign key which point to aspNetUser table 
            modelBuilder.Entity<QRefreshToken>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<Domain.Entities.QDBConnection.QDBConnection>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<Domain.Entities.QDBConnection.QDBConnection>()
                .HasIndex(d => d.ApiRoute)
                .IsUnique();

            // Ajouter la contrainte d'unicité sur QSetting.Name
            modelBuilder.Entity<QSetting>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configuration des entités de menu
            modelBuilder.Entity<DynamicMenuCategory>(entity =>
            {
                entity.ToTable("DynamicMenuCategories");
                
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Route).HasMaxLength(100);
                entity.Property(e => e.Roles).HasMaxLength(1000);
                
                // Index sur Order pour faciliter le tri
                entity.HasIndex(e => e.Order);
            });

            modelBuilder.Entity<DynamicMenuCategoryTranslation>(entity =>
            {
                entity.ToTable("DynamicMenuCategoryTranslations");
                
                entity.HasKey(e => e.Id);
                
                // Contrainte d'unicité sur la combinaison MenuCategoryId et LanguageCode
                entity.HasIndex(e => new { DynamicMenuCategoryId = e.DynamicMenuCategoryId, e.LanguageCode }).IsUnique();
                
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

                // Configuration de la relation
                entity.HasOne(d => d.DynamicMenuCategory)
                    .WithMany(p => p.Translations)
                    .HasForeignKey(d => d.DynamicMenuCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Données de base pour les menus (optionnel)
            modelBuilder.Entity<DynamicMenuCategory>().HasData(
                new DynamicMenuCategory
                {
                    Id = 1,
                    Icon = "home",
                    Order = 1,
                    IsVisible = true,
                    Roles = "Admin,User",
                    Route = "/home"
                }
            );

            modelBuilder.Entity<DynamicMenuCategoryTranslation>().HasData(
                new DynamicMenuCategoryTranslation
                {
                    Id = 1,
                    DynamicMenuCategoryId = 1,
                    LanguageCode = "en",
                    Name = "Home"
                },
                new DynamicMenuCategoryTranslation
                {
                    Id = 2,
                    DynamicMenuCategoryId = 1,
                    LanguageCode = "fr",
                    Name = "Accueil"
                }
            );

            modelBuilder.Entity<DynamicPage>(entity =>
            {
                entity.ToTable("DynamicPages");
                
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Route).HasMaxLength(100);
                entity.Property(e => e.Roles).HasMaxLength(1000);
                
                entity.HasOne(e => e.DynamicMenuCategory)
                      .WithMany(e => e.Pages)
                      .HasForeignKey(e => e.DynamicMenuCategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.Order);
            });

            modelBuilder.Entity<DynamicPageTranslation>(entity =>
            {
                entity.ToTable("DynamicPageTranslations");
                
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => new { e.DynamicPageId, e.LanguageCode }).IsUnique();
                
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

                entity.HasOne(d => d.DynamicPage)
                      .WithMany(p => p.DynamicPageTranslations)
                      .HasForeignKey(d => d.DynamicPageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Ajout du seed pour la page Northwind
            modelBuilder.Entity<DynamicPage>().HasData(
                new DynamicPage
                {
                    Id = 1,
                    Icon = "dashboard",
                    Order = 1,
                    IsVisible = true,
                    Roles = "Admin,User",
                    Route = "/northwind/home",
                    DynamicMenuCategoryId = 1
                }
            );

            modelBuilder.Entity<DynamicPageTranslation>().HasData(
                new DynamicPageTranslation
                {
                    Id = 1,
                    DynamicPageId = 1,
                    LanguageCode = "fr",
                    Name = "Northwind - Accueil"
                },
                new DynamicPageTranslation
                {
                    Id = 2,
                    DynamicPageId = 1,
                    LanguageCode = "en",
                    Name = "Northwind - Home"
                }
            );

            modelBuilder.Entity<DynamicRow>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                
                entity.HasOne(d => d.Page)
                      .WithMany(p => p.Rows)
                      .HasForeignKey(d => d.PageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DynamicCard>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Configuration).HasColumnType("json");
                
                entity.HasOne(d => d.Row)
                      .WithMany(r => r.Cards)
                      .HasForeignKey(d => d.DynamicRowId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DynamicCardTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Contrainte d'unicité sur la combinaison DynamicCardId et LanguageCode
                entity.HasIndex(e => new { e.DynamicCardId, e.LanguageCode }).IsUnique();
                
                entity.Property(e => e.LanguageCode).HasMaxLength(5);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();

                // Configuration de la relation
                entity.HasOne(d => d.Card)
                    .WithMany(p => p.Translations)
                    .HasForeignKey(d => d.DynamicCardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
