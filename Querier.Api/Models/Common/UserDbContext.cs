using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Models.Auth;

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
                UserName = "admin@querier.fr",
                Email = "admin@querier.fr",
                NormalizedEmail = "ADMIN@QUERIER.FR",
                FirstName = "Admin",
                LastName = "Admin",
                NormalizedUserName = "ADMIN@QUERIER.FR",
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
}
