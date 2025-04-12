using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Partner> Partners { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<PartnerType> PartnerTypes { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<AccessPermission> AccessPermissions { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<ColumnVisibility> ColumnVisibilities { get; set; } //oszlopok láthatósága usercsoportonként
        public DbSet<DocumentType> DocumentTypes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Define the relationship between Contact and Partner
                modelBuilder.Entity<Contact>(entity =>
                {
                    entity.HasOne(c => c.Partner)          // Contact has one Partner
                        .WithMany(p => p.Contacts)      // Partner has many Contacts (if Partner has List<Contact>)
                        .HasForeignKey(c => c.PartnerId) // FK is PartnerId
                        .OnDelete(DeleteBehavior.Cascade); // Or other delete behavior
                });
            }
    }
}