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
                Email = "admin@herdia.fr",
                NormalizedEmail = "ADMIN@HERDIA.FR",
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
                Email = "apiclient@herdia.fr",
                NormalizedEmail = "APICLIENT@HERDIA.FR",
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

            modelBuilder.Entity<HACategoryRole>()
               .HasKey(cr => new { cr.HAPageCategoryId, cr.ApiRoleId });

            modelBuilder.Entity<HAPageRole>()
                .HasKey(cr => new { cr.HAPageId, cr.ApiRoleId });

            modelBuilder.Entity<HACardRole>()
                .HasKey(cr => new { cr.HAPageCardId, cr.ApiRoleId });

            modelBuilder.Entity<HACategoryRole>()
                .HasOne(c => c.ApiRole)
                .WithMany(r => r.HACategoryRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAPageRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.HAPageRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACardRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.HACardRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACategoryRole>()
                .HasOne(c => c.HAPageCategory)
                .WithMany(r => r.HACategoryRoles)
                .HasForeignKey(c => c.HAPageCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAPageRole>()
                .HasOne(p => p.HAPage)
                .WithMany(r => r.HAPageRoles)
                .HasForeignKey(c => c.HAPageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACardRole>()
                .HasOne(p => p.HAPageCard)
                .WithMany(r => r.HACardRoles)
                .HasForeignKey(c => c.HAPageCardId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
    public partial class ApiDbContext : IdentityDbContext<ApiUser, ApiRole, string>
    {

        public IConfiguration _configuration { get; }
        public virtual DbSet<HARefreshToken> HARefreshTokens { get; set; }
        public virtual DbSet<HANotification> HANotifications { get; set; }
        public virtual DbSet<HAPageCategory> HAPageCategories { get; set; }
        public virtual DbSet<HAPage> HAPages { get; set; }
        public virtual DbSet<HAPageRow> HAPageRows { get; set; }
        public virtual DbSet<HAPageCard> HAPageCards { get; set; }
        public virtual DbSet<HAPageCardDefinedConfiguration> HAPageCardDefinedConfigurations { get; set; }
        public virtual DbSet<HATranslation> HATranslations { get; set; }
        public virtual DbSet<HAHtmlPartialRef> HAHtmlPartialRefs { get; set; }
        public virtual DbSet<HACategoryRole> HACategoryRoles { get; set; }
        public virtual DbSet<HAPageRole> HAPageRoles { get; set; }
        public virtual DbSet<HACardRole> HACardRoles { get; set; }
        public virtual DbSet<HATheme> HAThemes { get; set; }
        public virtual DbSet<HAThemeVariable> HAThemeVariables { get; set; }
        public virtual DbSet<HADBConnection.HADBConnection> HADBConnections { get; set; }
        // public virtual DbSet<UserNotification> Notifications { get; set; }
        public virtual DbSet<HAEntityAttribute> HAEntityAttributes { get; set; }
        public virtual DbSet<HAUploadDefinition> HAUploadDefinitions { get; set; }
        public virtual DbSet<HAApiUserAttributes> HAApiUserAttributes { get; set; }
        public virtual DbSet<HAFileDeposit> HAFileDeposit { get; set; }
        public virtual DbSet<HAFilesFromFileDeposit> HAFilesFromFileDeposit { get; set; }


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
                case "Oracle":
                    optionsBuilder.UseLazyLoadingProxies().UseOracle(_configuration.GetConnectionString("ApiDBConnection"), x => x.MigrationsAssembly("HerdiaApp.Migration.Oracle"));
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var SQLEngine = _configuration.GetSection("SQLEngine").Get<string>();

            modelBuilder.ApplyConfiguration(new HAEntityAttribute.HAEntityAttributeConfiguration());
            base.OnModelCreating(modelBuilder);

            switch (SQLEngine)
            {
                case "PgSQL":
                    modelBuilder.Entity<HAEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", "num_nonnulls(\"StringAttribute\", \"IntAttribute\", \"DecimalAttribute\", \"DateTimeAttribute\") = 1"));
                    break;
                case "Oracle":
                    modelBuilder.Entity<HAEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", @"
                        (
                            (CASE WHEN NVL(""StringAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN NVL(""IntAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN NVL(""DecimalAttribute"", 0) IS NULL THEN 0 ELSE 1 END) +
                            (CASE WHEN ""DateTimeAttribute"" IS NULL THEN 0 ELSE 1 END)
                        ) = 1")
                    );
                    break;
                default:
                    modelBuilder.Entity<HAEntityAttribute>(entity => entity.HasCheckConstraint("CK_ONLY_ONE_NOT_NULL", @"
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
                Email = "admin@herdia.fr",
                NormalizedEmail = "admin@herdia.fr",
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
                Email = "apiclient@herdia.fr",
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

            modelBuilder.Entity<HAPageCategory>().HasData(new HAPageCategory
            {
                Id = 1,
                Label = "Welcome",
                Description = "Welcome Category",
                Icon = "book"
            });

            modelBuilder.Entity<HAPage>().HasData(new HAPage
            {
                Id = 1,
                Description = "Page d'accueil par défaut",
                Title = "Accueil",
                HAPageCategoryId = 1,
                Icon = "book"
            });

            //Admin User Default 2 Themes
            modelBuilder.Entity<HATheme>().HasData(new HATheme
            {
                Id = 1,
                Label = "Theme1",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e",
            });

            modelBuilder.Entity<HATheme>().HasData(new HATheme
            {
                Id = 2,
                Label = "Theme2",
                UserId = "3fcfa67e-b654-4df0-9558-d97cb90e415e",
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 1,
                VariableName = "PrimaryColor",
                VariableValue = "#61baac",
                HAThemeId = 1
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 2,
                VariableName = "SecondaryColor",
                VariableValue = "#807a70",
                HAThemeId = 1
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 3,
                VariableName = "NavbarColor",
                VariableValue = "#807a70",
                HAThemeId = 1
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 4,
                VariableName = "TopNavbarColor",
                VariableValue = "#FFF",
                HAThemeId = 1
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 5,
                VariableName = "customFontSize",
                VariableValue = "1",
                HAThemeId = 1
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 6,
                VariableName = "PrimaryColor",
                VariableValue = "#2f3350",
                HAThemeId = 2
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 7,
                VariableName = "SecondaryColor",
                VariableValue = "#ce2c2c",
                HAThemeId = 2
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 8,
                VariableName = "NavbarColor",
                VariableValue = "#807a70",
                HAThemeId = 2
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 9,
                VariableName = "TopNavbarColor",
                VariableValue = "#FFF",
                HAThemeId = 2
            });

            modelBuilder.Entity<HAThemeVariable>().HasData(new HAThemeVariable
            {
                Id = 10,
                VariableName = "customFontSize",
                VariableValue = "1",
                HAThemeId = 2
            });

            modelBuilder.Entity<HAPageRow>().HasData(new HAPageRow
            {
                Id = 1,
                HAPageId = 1,
                Order = 1
            });

            modelBuilder.Entity<HACategoryRole>()
                .HasKey(cr => new { cr.HAPageCategoryId, cr.ApiRoleId });

            modelBuilder.Entity<HAPageRole>()
                .HasKey(cr => new { cr.HAPageId, cr.ApiRoleId });

            modelBuilder.Entity<HACardRole>()
                .HasKey(cr => new { cr.HAPageCardId, cr.ApiRoleId });

            modelBuilder.Entity<HACategoryRole>()
                .HasOne(c => c.ApiRole)
                .WithMany(r => r.HACategoryRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAPageRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.HAPageRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACardRole>()
                .HasOne(p => p.ApiRole)
                .WithMany(r => r.HACardRoles)
                .HasForeignKey(c => c.ApiRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACategoryRole>()
                .HasOne(c => c.HAPageCategory)
                .WithMany(r => r.HACategoryRoles)
                .HasForeignKey(c => c.HAPageCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAPageRole>()
                .HasOne(p => p.HAPage)
                .WithMany(r => r.HAPageRoles)
                .HasForeignKey(c => c.HAPageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACardRole>()
                .HasOne(p => p.HAPageCard)
                .WithMany(r => r.HACardRoles)
                .HasForeignKey(c => c.HAPageCardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HACategoryRole>().HasData(new HACategoryRole
            {
                ApiRoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                HAPageCategoryId = 1,
                View = true,
                Add = true,
                Edit = true
            });

            modelBuilder.Entity<HAPageRole>().HasData(new HAPageRole
            {
                ApiRoleId = "bae3a8d8-5abe-4c9e-8088-bbdf863e4fb9",
                HAPageId = 1,
                View = true,
                Add = true,
                Edit = true,
                Remove = true
            });

            //add delete cascade on foreign key which point to aspNetUser table 
            modelBuilder.Entity<HARefreshToken>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            //

            modelBuilder.Entity<HATheme>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAApiUserAttributes>()
                .HasOne(e => e.User)
                .WithMany(a => a.HAApiUserAttributes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HAApiUserAttributes>()
                .HasOne(e => e.EntityAttribute)
                .WithMany(a => a.HAApiUserAttributes)
                .HasForeignKey(e => e.EntityAttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HADBConnection.HADBConnection>()
                .HasIndex(d => d.Name)
                .IsUnique();
            
            modelBuilder.Entity<HADBConnection.HADBConnection>()
                .HasIndex(d => d.ApiRoute)
                .IsUnique();

            modelBuilder.Entity<HADBConnection.HADBConnection>()
                .HasOne(e => e.AssemblyUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.AssemblyUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HADBConnection.HADBConnection>()
                .HasOne(e => e.PDBUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.PDBUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<HADBConnection.HADBConnection>()
                .HasOne(e => e.SourcesUploadDefinition)
                .WithMany()
                .HasForeignKey(e => e.SourcesUploadDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            //add unique constrain on colomn tags from table HAFileDeposit
            modelBuilder.Entity<HAFileDeposit>()
                .HasIndex(d => d.Tag)
                .IsUnique();

            //add delete cascade on fk : HAFilesFromFileDeposit.HAFileDepositId
            modelBuilder.Entity<HAFilesFromFileDeposit>()
                .HasOne(e => e.HAFileDeposit)
                .WithMany()
                .HasForeignKey(e => e.HAFileDepositId)
                .OnDelete(DeleteBehavior.Restrict);

            ////seeding data file deposit for testing during dev
            modelBuilder.Entity<HAFileDeposit>().HasData(new HAFileDeposit
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

            modelBuilder.Entity<HAFileDeposit>().HasData(new HAFileDeposit
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
