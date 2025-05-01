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

        // public DbSet<Order> Orders { get; set; }
        // public DbSet<OrderItem> OrderItems { get; set; }
        // public DbSet<DeliveryNote> DeliveryNotes { get; set; }
        // public DbSet<DeliveryNoteItem> DeliveryNoteItems { get; set; }
        // public DbSet<TaxRate> TaxRates { get; set; }
        // public DbSet<Invoice> Invoices { get; set; }
        // public DbSet<InvoiceItem> InvoiceItems { get; set; }
        // public DbSet<PaymentMethod> PaymentMethods { get; set; }
        // public DbSet<Receipt> Receipts { get; set; }
        // public DbSet<WarehouseMovement> WarehouseMovements { get; set; }
        // public DbSet<WarehouseMovementItem> WarehouseMovementItems { get; set; }
        public DbSet<ProductFile> ProductFiles { get; set; }
        public DbSet<QuoteHistory> QuoteHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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