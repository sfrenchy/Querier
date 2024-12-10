using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.Ged;
using Querier.Api.Models.Notifications;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Common
{
    public partial class ApiDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {
        //public virtual DbSet<QApiUserAttributes> QApiUserAttributes { get; set; }
        //public virtual DbSet<QFileDeposit> QFileDeposit { get; set; }
        //public virtual DbSet<QFilesFromFileDeposit> QFilesFromFileDeposit { get; set; }


        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration { get; }
        public virtual DbSet<QRefreshToken> QRefreshTokens { get; set; }
        public virtual DbSet<QNotification> QNotifications { get; set; }
        public virtual DbSet<QPageCategory> QPageCategories { get; set; }
        public virtual DbSet<QPage> QPages { get; set; }
        public virtual DbSet<QPageRow> QPageRows { get; set; }
        public virtual DbSet<QPageCard> QPageCards { get; set; }
        public virtual DbSet<QSetting> QSettings { get; set; }

        public virtual DbSet<QPageCardDefinedConfiguration> QPageCardDefinedConfigurations { get; set; }

        //public virtual DbSet<QTranslation> QTranslations { get; set; }
        //public virtual DbSet<QHtmlPartialRef> QHtmlPartialRefs { get; set; }
        public virtual DbSet<QCategoryRole> QCategoryRoles { get; set; }
        public virtual DbSet<QPageRole> QPageRoles { get; set; }

        public virtual DbSet<QCardRole> QCardRoles { get; set; }

        //public virtual DbSet<QTheme> QThemes { get; set; }
        //public virtual DbSet<QThemeVariable> QThemeVariables { get; set; }
        public virtual DbSet<QDBConnection.QDBConnection> QDBConnections { get; set; }

        // public virtual DbSet<UserNotification> Notifications { get; set; }
        public virtual DbSet<QEntityAttribute> QEntityAttribute { get; set; }
        public virtual DbSet<QUploadDefinition> QUploadDefinitions { get; set; }
        public DbSet<ApiRole> ApiRoles { get; set; }
        public DbSet<ApiUserRole> ApiUserRoles { get; set; }
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

            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();

            modelBuilder.ApplyConfiguration(new QEntityAttribute.QEntityAttributeConfiguration());

            switch (SQLEngine)
            {
                case "PgSQL":
                    modelBuilder.Entity<QEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", "num_nonnulls(\"StringAttribute\", \"IntAttribute\", \"DecimalAttribute\", \"DateTimeAttribute\") = 1"));
                    break;
                case "Oracle":
                    modelBuilder.Entity<QEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", @"
                        (
                            (CASE WHEN NVL(""StringAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN NVL(""IntAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN NVL(""DecimalAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN ""DateTimeAttribute"" IS NULL THEN 0 ELSE 1 END)
                        ) = 1")
                    );
                    break;
                default:
                    modelBuilder.Entity<QEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", @"
                        (CASE WHEN StringAttribute IS NULL THEN 0 ELSE 1 END 
                            + CASE WHEN IntAttribute IS NULL THEN 0 ELSE 1 END
                            + CASE WHEN DecimalAttribute IS NULL THEN 0 ELSE 1 END
                            + CASE WHEN DateTimeAttribute IS NULL THEN 0 ELSE 1 END) =1")
                    );                   
                    break;
            }

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

            modelBuilder.Entity<QPageCategory>().HasData(new QPageCategory
            {
                Id = 1,
                Label = "Welcome",
                Description = "Welcome Category",
                Icon = "book"
            });

            modelBuilder.Entity<QPage>().HasData(new QPage
            {
                Id = 1,
                Description = "Page d'accueil par défaut",
                Title = "Accueil",
                HAPageCategoryId = 1,
                Icon = "book"
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 1,
                VariableName = "PrimaryColor",
                VariableValue = "#61baac",
                HAThemeId = 1
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 2,
                VariableName = "SecondaryColor",
                VariableValue = "#807a70",
                HAThemeId = 1
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 3,
                VariableName = "NavbarColor",
                VariableValue = "#807a70",
                HAThemeId = 1
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 4,
                VariableName = "TopNavbarColor",
                VariableValue = "#FFF",
                HAThemeId = 1
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 5,
                VariableName = "customFontSize",
                VariableValue = "1",
                HAThemeId = 1
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 6,
                VariableName = "PrimaryColor",
                VariableValue = "#2f3350",
                HAThemeId = 2
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 7,
                VariableName = "SecondaryColor",
                VariableValue = "#ce2c2c",
                HAThemeId = 2
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 8,
                VariableName = "NavbarColor",
                VariableValue = "#807a70",
                HAThemeId = 2
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 9,
                VariableName = "TopNavbarColor",
                VariableValue = "#FFF",
                HAThemeId = 2
            });

            modelBuilder.Entity<QThemeVariable>().HasData(new QThemeVariable
            {
                Id = 10,
                VariableName = "customFontSize",
                VariableValue = "1",
                HAThemeId = 2
            });

            modelBuilder.Entity<QPageRow>().HasData(new QPageRow
            {
                Id = 1,
                HAPageId = 1,
                Order = 1
            });

            modelBuilder.Entity<QCategoryRole>()
                .HasKey(cr => new { cr.HAPageCategoryId, cr.ApiRoleId });

            modelBuilder.Entity<QPageRole>()
                .HasKey(cr => new { cr.HAPageId, cr.ApiRoleId });

            modelBuilder.Entity<QCardRole>()
                .HasKey(cr => new { cr.HAPageCardId, cr.ApiRoleId });

            modelBuilder.Entity<QCategoryRole>()
                .HasOne(c => c.ApiRole)
                .WithMany(r => r.QCategoryRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QPageRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.QPageRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QCardRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.QCardRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QCategoryRole>()
                .HasOne(c => c.QPageCategory)
                .WithMany(r => r.QCategoryRoles)
                .HasForeignKey(c => c.HAPageCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QPageRole>()
                .HasOne(p => p.QPage)
                .WithMany(r => r.QPageRoles)
                .HasForeignKey(c => c.HAPageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QCardRole>()
                .HasOne(p => p.QPageCard)
                .WithMany(r => r.QCardRoles)
                .HasForeignKey(c => c.HAPageCardId)
                .OnDelete(DeleteBehavior.Cascade);


            //add delete cascade on foreign key which point to aspNetUser table 
            modelBuilder.Entity<QRefreshToken>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            //

            modelBuilder.Entity<QTheme>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QApiUserAttributes>()
                .HasOne(e => e.User)
                .WithMany(a => a.QApiUserAttributes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QApiUserAttributes>()
                .HasOne(e => e.EntityAttribute)
                .WithMany(a => a.QApiUserAttributes)
                .HasForeignKey(e => e.EntityAttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasIndex(d => d.Name)
                .IsUnique();
            
            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasIndex(d => d.ApiRoute)
                .IsUnique();

            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasOne(e => e.AssemblyUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.AssemblyUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasOne(e => e.PDBUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.PDBUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasOne(e => e.SourcesUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.SourcesUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            //add unique constrain on colomn tags from table QFileDeposit
            modelBuilder.Entity<QFileDeposit>()
                .HasIndex(d => d.Tag)
                .IsUnique();

            //add delete cascade on fk : QFilesFromFileDeposit.QFileDepositId
            modelBuilder.Entity<QFilesFromFileDeposit>()
                .HasOne(e => e.QFileDeposit)
                .WithMany()
                .HasForeignKey(e => e.QFileDepositId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QFileDeposit>().HasData(new QFileDeposit
            {
                Id = 2,
                Enable = true,
                Label = "File System facture",
                Filter = "['/ged/lease_code/$CODE_BAIL$_todo.txt', '/ged/lease_code/$CLIENT$/$CODE_BAIL$_todo.txt']",
                Type = TypeFileDepositEnum.FileSystem,
                Auth = AuthFileDepositEnum.Basic,
                Login = "",
                Password = "",
                Host = "",
                Port = 0,
                RootPath = "/ged/lease_code",
                Capabilities = CapabilitiesEnum.None,
                Tag = "facture"
            });

            // Configuration de QCategoryRole
            modelBuilder.Entity<QCategoryRole>(entity =>
            {
                entity.ToTable("QCategoryRole");
                entity.HasKey(e => new { e.ApiRoleId, e.HAPageCategoryId });
                
                entity.HasOne<ApiRole>()
                    .WithMany(r => r.QCategoryRoles)
                    .HasForeignKey(cr => cr.ApiRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration de QPageRole
            modelBuilder.Entity<QPageRole>(entity =>
            {
                entity.ToTable("QPageRole");
                entity.HasKey(e => new { e.ApiRoleId, e.HAPageId });
                
                entity.HasOne<ApiRole>()
                    .WithMany(r => r.QPageRoles)
                    .HasForeignKey(cr => cr.ApiRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration de QCardRole
            modelBuilder.Entity<QCardRole>(entity =>
            {
                entity.ToTable("QCardRole");
                entity.HasKey(e => new { e.ApiRoleId, e.HAPageCardId });
                
                entity.HasOne<ApiRole>()
                    .WithMany(r => r.QCardRoles)
                    .HasForeignKey(cr => cr.ApiRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ajouter la contrainte d'unicité sur QSetting.Name
            modelBuilder.Entity<QSetting>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }
    }
}
 