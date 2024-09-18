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

    public class UserDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Seeding a  roles to AspNetRoles table
            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "46ab0744-da21-49af-9d60-349ad08e860c",
                Name = "User",
                NormalizedName = "USER".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "7b065115-d19c-4b95-b0ab-bd0e0a301479",
                Name = "PowerUser",
                NormalizedName = "POWERUSER".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                Name = "Admin",
                NormalizedName = "ADMIN".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "bb6d273d-1aa5-4b2e-870f-44f9f954582a",
                Name = "ApiUser",
                NormalizedName = "APIUSER".ToUpper()
            });

            var hasher = new PasswordHasher<ApiUser>();

            //Seeding the User to AspNetUsers table
            modelBuilder.Entity<ApiUser>().HasData(new ApiUser
            {
                Id = "3fcfa67e-b654-4df0-9558-d97cb90e415e", // primary key
                UserName = "Administrateur",
                Email = "admin@querier.fr",
                NormalizedEmail = "ADMIN@QUERIER.FR",
                FirstName = "Admin",
                LastName = "Admin",
                NormalizedUserName = "ADMINISTRATEUR",
                PasswordHash = hasher.HashPassword(null, "Admin-123"),
                EmailConfirmed = true
            });

            modelBuilder.Entity<ApiUser>().HasData(new ApiUser
            {
                Id = "545823b1-11b7-4e35-af5f-84e06aa1c050", // primary key
                UserName = "ApiClientApplication",
                Email = "apiclient@querier.fr",
                NormalizedEmail = "APICLIENT@QUERIER.FR",
                FirstName = "apiclient",
                LastName = "apiclient",
                NormalizedUserName = "APICLIENTAPPLICATION",
                PasswordHash = hasher.HashPassword(null, "9(V+UuzDC59EQyKb"),
                EmailConfirmed = true
            });

            //Seeding the relation between our user and role to AspNetUserRoles table
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e"
            });

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "bb6d273d-1aa5-4b2e-870f-44f9f954582a",
                UserId = "545823b1-11b7-4e35-af5f-84e06aa1c050"
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

        }
    }
    public partial class ApiDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {

        public IConfiguration _configuration { get; }
        public virtual DbSet<QRefreshToken> QRefreshTokens { get; set; }
        public virtual DbSet<QNotification> QNotifications { get; set; }
        public virtual DbSet<QPageCategory> QPageCategories { get; set; }
        public virtual DbSet<QPage> QPages { get; set; }
        public virtual DbSet<QPageRow> QPageRows { get; set; }
        public virtual DbSet<QPageCard> QPageCards { get; set; }
        public virtual DbSet<QPageCardDefinedConfiguration> QPageCardDefinedConfigurations { get; set; }
        public virtual DbSet<QTranslation> QTranslations { get; set; }
        public virtual DbSet<QHtmlPartialRef> QHtmlPartialRefs { get; set; }
        public virtual DbSet<QCategoryRole> QCategoryRoles { get; set; }
        public virtual DbSet<QPageRole> QPageRoles { get; set; }
        public virtual DbSet<QCardRole> QCardRoles { get; set; }
        public virtual DbSet<QTheme> QThemes { get; set; }
        public virtual DbSet<QThemeVariable> QThemeVariables { get; set; }
        public virtual DbSet<QDBConnection.QDBConnection> QDBConnections { get; set; }
        // public virtual DbSet<UserNotification> Notifications { get; set; }
        public virtual DbSet<QEntityAttribute> QEntityAttribute { get; set; }
        public virtual DbSet<QUploadDefinition> QUploadDefinitions { get; set; }
        public virtual DbSet<QApiUserAttributes> QApiUserAttributes { get; set; }
        public virtual DbSet<QFileDeposit> QFileDeposit { get; set; }
        public virtual DbSet<QFilesFromFileDeposit> QFilesFromFileDeposit { get; set; }


        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();
            switch (SQLEngine)
            {
                default:
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
            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();

            modelBuilder.ApplyConfiguration(new QEntityAttribute.QEntityAttributeConfiguration());
            base.OnModelCreating(modelBuilder);

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

            //Seeding a  roles to AspNetRoles table
            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "46ab0744-da21-49af-9d60-349ad08e860c",
                Name = "User",
                NormalizedName = "USER".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "7b065115-d19c-4b95-b0ab-bd0e0a301479",
                Name = "PowerUser",
                NormalizedName = "POWERUSER".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                Name = "Admin",
                NormalizedName = "ADMIN".ToUpper()
            });

            modelBuilder.Entity<ApiRole>().HasData(new ApiRole
            {
                Id = "bb6d273d-1aa5-4b2e-870f-44f9f954582a",
                Name = "ApiUser",
                NormalizedName = "APIUSER".ToUpper()
            });

            var hasher = new PasswordHasher<ApiUser>();

            //Seeding the User to AspNetUsers table
            modelBuilder.Entity<ApiUser>().HasData(new ApiUser
            {
                Id = "3fcfa67e-b654-4df0-9558-d97cb90e415e", // primary key
                UserName = "Administrateur",
                Email = "admin@querier.fr",
                NormalizedEmail = "admin@querier.fr",
                FirstName = "Admin",
                LastName = "Admin",
                NormalizedUserName = "ADMINISTRATEUR",
                PasswordHash = hasher.HashPassword(null, "Admin-123"),
                EmailConfirmed = true
            });

            modelBuilder.Entity<ApiUser>().HasData(new ApiUser
            {
                Id = "545823b1-11b7-4e35-af5f-84e06aa1c050", // primary key
                UserName = "ApiClientApplication",
                Email = "apiclient@querier.fr",
                FirstName = "apiclient",
                LastName = "apiclient",
                NormalizedUserName = "APICLIENTAPPLICATION",
                PasswordHash = hasher.HashPassword(null, "9(V+UuzDC59EQyKb"),
                EmailConfirmed = true
            });

            //Seeding the relation between our user and role to AspNetUserRoles table
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e"
            });

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "bb6d273d-1aa5-4b2e-870f-44f9f954582a",
                UserId = "545823b1-11b7-4e35-af5f-84e06aa1c050"
            });

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

            //Admin User Default 2 Themes
            modelBuilder.Entity<QTheme>().HasData(new QTheme
            {
                Id = 1,
                Label = "Theme1",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e",
            });

            modelBuilder.Entity<QTheme>().HasData(new QTheme
            {
                Id = 2,
                Label = "Theme2",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e",
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

            modelBuilder.Entity<QCategoryRole>().HasData(new QCategoryRole
            {
                ApiRoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                HAPageCategoryId = 1,
                View = true,
                Add = true,
                Edit = true
            });

            modelBuilder.Entity<QPageRole>().HasData(new QPageRole
            {
                ApiRoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                HAPageId = 1,
                View = true,
                Add = true,
                Edit = true,
                Remove = true
            });

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

            ////seeding data file deposit for testing during dev
            modelBuilder.Entity<QFileDeposit>().HasData(new QFileDeposit
            {
                Id = 1,
                Enable = true,
                Label = "Docuware lease_code",
                Filter = "['CODE_BAIL', 'IMMEUBLE']",
                Type = TypeFileDepositEnum.Docuware,
                Auth = AuthFileDepositEnum.Basic,
                Login = "RICH",
                Password = "Jaimevoirmesbaux1!",
                Host = "https://dw75.meylly.com",
                Port = 0,
                RootPath = "3328efb0-df0d-4f09-a237-91cd9aa69ef7",
                Capabilities = CapabilitiesEnum.Player,
                Tag = "lease_code"
            });

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
        }
    }
}
