using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<ColumnVisibility> ColumnVisibilities { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<LeadSource> LeadSources { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadHistory> LeadHistories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductPrice> ProductPrices { get; set; }
        public DbSet<UnitOfMeasurement> UnitsOfMeasurement { get; set; }
        public DbSet<ProductUOM> ProductUOMs { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteItem> QuoteItems { get; set; }
        public DbSet<ProductFile> ProductFiles { get; set; }
        public DbSet<QuoteHistory> QuoteHistories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

                        // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderNumber)
                    .HasMaxLength(100);
                entity.Property(e => e.OrderDate)
                    .HasColumnType("date");
                entity.Property(e => e.Deadline)
                    .HasColumnType("date");
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.SalesPerson)
                    .HasMaxLength(100);
                entity.Property(e => e.DeliveryDate)
                    .HasColumnType("date");
                entity.Property(e => e.DiscountPercentage)
                    .HasColumnType("decimal(5,2)");
                entity.Property(e => e.DiscountAmount)
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.CompanyName)
                    .HasMaxLength(100);
                entity.Property(e => e.Subject)
                    .HasMaxLength(200);
                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .HasDefaultValue("System");
                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ModifiedBy)
                    .HasMaxLength(100)
                    .HasDefaultValue("System");
                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");
                entity.Property(e => e.ReferenceNumber)
                    .HasMaxLength(100);
                entity.Property(e => e.PaymentTerms)
                    .HasMaxLength(100);
                entity.Property(e => e.ShippingMethod)
                    .HasMaxLength(100);
                entity.Property(e => e.OrderType)
                    .HasMaxLength(50);

                // Relationships
                entity.HasOne(e => e.Partner)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(e => e.PartnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(e => e.SiteId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Currency)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Quote)
                    .WithMany(q => q.Orders)
                    .HasForeignKey(e => e.QuoteId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.OrderNumber).IsUnique();
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.ItemName)
                    .HasMaxLength(200);
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.Property(e => e.Quantity)
                    .HasColumnType("decimal(18,4)");
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage)
                    .HasColumnType("decimal(5,2)");
                entity.Property(e => e.DiscountAmount)
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitOfMeasure)
                    .HasMaxLength(50);
                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .HasDefaultValue("System");
                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ModifiedBy)
                    .HasMaxLength(100)
                    .HasDefaultValue("System");
                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationship
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Partner-Lead one-to-many relationship
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Partner)
                .WithMany(p => p.Leads)
                .HasForeignKey(l => l.PartnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quote>(entity =>
            {
                entity.Property(e => e.QuoteNumber).IsRequired(false);
                entity.Property(e => e.Description).IsRequired(false);
                entity.Property(e => e.SalesPerson).IsRequired(false);
                entity.Property(e => e.Subject).IsRequired(false);
                entity.Property(e => e.DetailedDescription).IsRequired(false);
                entity.Property(e => e.CompanyName).IsRequired(false);
                entity.Property(e => e.CreatedBy).IsRequired(false);
                entity.Property(e => e.ModifiedBy).IsRequired(false);
                entity.Property(e => e.Status).IsRequired(false);
            });

            modelBuilder.Entity<Quote>()
            .HasMany(q => q.QuoteHistories)
            .WithOne(qi => qi.Quote)
            .HasForeignKey(qi => qi.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quote>()
            .HasMany(q => q.QuoteItems)
            .WithOne(qi => qi.Quote)
            .HasForeignKey(qi => qi.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuoteItem>()
                .HasOne(qi => qi.Quote)
                .WithMany(q => q.QuoteItems)
                .HasForeignKey(qi => qi.QuoteId);

            modelBuilder.Entity<QuoteItem>()
                .HasOne(qi => qi.Product)
                .WithMany()
                .HasForeignKey(qi => qi.ProductId);

            modelBuilder.Entity<Quote>()
                .HasIndex(q => q.QuoteNumber)
                .IsUnique();

            modelBuilder.Entity<Contact>()
            .HasOne(c => c.Partner)
            .WithMany(p => p.Contacts)
            .HasForeignKey(c => c.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Partner)
                .WithMany(p => p.Quotes)
                .HasForeignKey(q => q.PartnerId);

            modelBuilder.Entity<Partner>()
                .HasMany(p => p.Quotes)
                .WithOne(q => q.Partner)
                .HasForeignKey(q => q.PartnerId);

            modelBuilder.Entity<Product>()
            .HasOne(p => p.BaseUOM)
            .WithMany()
            .HasForeignKey(p => p.BaseUOMId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
            .HasOne(p => p.WeightUOM)
            .WithMany()
            .HasForeignKey(p => p.WeightUOMId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
            .HasOne(p => p.DimensionUOM)
            .WithMany()
            .HasForeignKey(p => p.DimensionUOMId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
            .HasOne(p => p.Creator)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
            .HasOne(p => p.LastModifier)
            .WithMany()
            .HasForeignKey(p => p.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.UnitOfMeasurement)
            .WithMany()
            .HasForeignKey(pp => pp.UnitOfMeasurementId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.Currency)
            .WithMany()
            .HasForeignKey(pp => pp.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.Creator)
            .WithMany()
            .HasForeignKey(pp => pp.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.LastModifier)
            .WithMany()
            .HasForeignKey(pp => pp.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UnitOfMeasurement>()
            .HasOne(u => u.Creator)
            .WithMany()
            .HasForeignKey(u => u.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitOfMeasurement>()
            .HasOne(u => u.LastModifier)
            .WithMany()
            .HasForeignKey(u => u.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductUOM>()
            .HasOne(pu => pu.Product)
            .WithMany()
            .HasForeignKey(pu => pu.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductUOM>()
            .HasOne(pu => pu.UnitOfMeasurement)
            .WithMany()
            .HasForeignKey(pu => pu.UnitOfMeasurementId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductUOM>()
            .HasOne(pu => pu.Creator)
            .WithMany()
            .HasForeignKey(pu => pu.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductUOM>()
            .HasOne(pu => pu.LastModifier)
            .WithMany()
            .HasForeignKey(pu => pu.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Currency>()
            .HasOne(c => c.Creator)
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Currency>()
            .HasOne(c => c.LastModifier)
            .WithMany()
            .HasForeignKey(c => c.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Warehouse>()
            .HasOne(w => w.Creator)
            .WithMany()
            .HasForeignKey(w => w.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Warehouse>()
            .HasOne(w => w.LastModifier)
            .WithMany()
            .HasForeignKey(w => w.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.Warehouse)
            .WithMany(w => w.Stocks)
            .HasForeignKey(ws => ws.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.Product)
            .WithMany()
            .HasForeignKey(ws => ws.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.UnitOfMeasurement)
            .WithMany()
            .HasForeignKey(ws => ws.UnitOfMeasurementId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.Creator)
            .WithMany()
            .HasForeignKey(ws => ws.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.LastModifier)
            .WithMany()
            .HasForeignKey(ws => ws.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductFile>()
            .HasOne(pf => pf.Product)
            .WithMany(p => p.Files)
            .HasForeignKey(pf => pf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductFile>()
            .HasOne(pf => pf.ProductUOM)
            .WithMany()
            .HasForeignKey(pf => pf.ProductUOMId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductFile>()
            .HasOne(pf => pf.Creator)
            .WithMany()
            .HasForeignKey(pf => pf.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductFile>()
            .HasOne(pf => pf.LastModifier)
            .WithMany()
            .HasForeignKey(pf => pf.LastModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.LastModifier)
                .WithMany()
                .HasForeignKey(c => c.LastModifiedBy)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}