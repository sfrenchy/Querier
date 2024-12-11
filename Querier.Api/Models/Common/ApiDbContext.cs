using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.Common
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
        public virtual DbSet<QDBConnection.QDBConnection> QDBConnections { get; set; }
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

            

            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasIndex(d => d.Name)
                .IsUnique();
            
            modelBuilder.Entity<QDBConnection.QDBConnection>()
                .HasIndex(d => d.ApiRoute)
                .IsUnique();

            // Ajouter la contrainte d'unicité sur QSetting.Name
            modelBuilder.Entity<QSetting>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }
    }
}
 