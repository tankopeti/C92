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
        public DbSet<CommunicationType> CommunicationTypes { get; set; }
        public DbSet<CustomerCommunication> CustomerCommunications { get; set; }
        public DbSet<CommunicationStatus> CommunicationStatuses { get; set; }
        public DbSet<CommunicationPost> CommunicationPosts { get; set; }
        public DbSet<CommunicationResponsible> CommunicationResponsibles { get; set; }
        public DbSet<VatType> VatTypes { get; set; }
        public DbSet<VolumeDiscount> VolumeDiscounts { get; set; }
        public DbSet<PartnerGroup> PartnerGroups { get; set; }
        public DbSet<PartnerProductPrice> PartnerProductPrice { get; set; }
        public DbSet<ProductGroup> ProductGroups { get; set; }
        public DbSet<ProductGroupProduct> ProductGroupProducts { get; set; }
        public DbSet<PartnerProductGroupPrice> PartnerProductGroupPrices { get; set; }
        public DbSet<QuoteItemDiscount> QuoteItemDiscounts { get; set; }
        public DbSet<TaskTypePM> TaskTypePMs { get; set; }
        public DbSet<TaskPM> TaskPMs { get; set; }
        public DbSet<TaskCommentPM> TaskCommentsPMs { get; set; }
        public DbSet<TaskAttachmentPM> TaskAttachmentsPMs { get; set; }
        public DbSet<TaskStatusPM> TaskStatusPMs { get; set; }
        public DbSet<TaskPriorityPM> TaskPriorityPMs { get; set; }
        public DbSet<ProjectPM> ProjectPMs { get; set; }
        public DbSet<ProjectStatusPM> ProjectStatusPMs { get; set; }
        public DbSet<OrderItemDiscount> OrderItemDiscounts { get; set; } // Added for CS1061
        public DbSet<DocumentMetadata> DocumentMetadata { get; set; }
        public DbSet<DocumentLink> DocumentLinks { get; set; }
        public DbSet<DocumentStatusHistory> DocumentStatusHistory { get; set; }
        public DbSet<Status> PartnerStatuses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Status>().ToTable("PartnerStatuses");

            modelBuilder.Entity<PartnerDto>()
                .HasOne(p => p.Status)
                .WithMany()
                .HasForeignKey(p => p.StatusId);

            modelBuilder.Entity<SiteDto>()
                .HasOne(s => s.Status)
                .WithMany()
                .HasForeignKey(s => s.StatusId);

            modelBuilder.Entity<Site>()
                .Property(s => s.SiteName)
                .IsRequired();
            modelBuilder.Entity<Site>()
                .Property(s => s.PartnerId)
                .IsRequired();

            modelBuilder.Entity<Partner>()
                .HasMany(p => p.Documents)
                .WithOne() // No navigation property in Document
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.DocumentMetadata)
                .WithOne(m => m.Document)
                .HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.DocumentLinks)
                .WithOne(l => l.Document)
                .HasForeignKey(l => l.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.StatusHistory)
                .WithOne(s => s.Document)
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasConversion<int>();

            modelBuilder.Entity<DocumentStatusHistory>()
                .Property(h => h.OldStatus)
                .HasConversion<int>();
            modelBuilder.Entity<DocumentStatusHistory>()
                .Property(h => h.NewStatus)
                .HasConversion<int>();

            // Configure relationships
            modelBuilder.Entity<DocumentStatusHistory>()
                .HasOne(h => h.Document)
                .WithMany(d => d.StatusHistory)
                .HasForeignKey(h => h.DocumentId);

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasConversion<int>();

            modelBuilder.Entity<DocumentMetadata>()
                .HasIndex(m => m.Key);

            modelBuilder.Entity<DocumentMetadata>()
                .HasIndex(m => m.Value);


            modelBuilder.Entity<TaskPM>(entity =>
            {
                entity.Property(t => t.IsActive).HasDefaultValue(true);

                // Relationships
                entity.HasOne(t => t.TaskTypePM)
                    .WithMany()
                    .HasForeignKey(t => t.TaskTypePMId);

                entity.HasOne(t => t.ProjectPM)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(t => t.ProjectPMId);

                // Other relationships...
            });

            modelBuilder.Entity<TaskPM>()
                .HasOne(t => t.ProjectPM)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectPMId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskTypePM>(entity =>
                {
                    entity.HasKey(tt => tt.TaskTypePMId);
                    entity.Property(tt => tt.Name).IsRequired().HasMaxLength(50);
                });

            modelBuilder.Entity<TaskPM>(entity =>
            {
                entity.HasKey(t => t.TaskPMId);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(100);

                entity.HasOne(t => t.TaskTypePM)
                      .WithMany(tt => tt.Tasks)
                      .HasForeignKey(t => t.TaskTypePMId);

                // entity.HasOne(t => t.Project)
                //       .WithMany(p => p.Tasks)
                //       .HasForeignKey(t => t.ProjectId);

                entity.HasOne(t => t.CreatedBy)
                      .WithMany()
                      .HasForeignKey(t => t.CreatedById);

                entity.HasOne(t => t.AssignedTo)
                      .WithMany()
                      .HasForeignKey(t => t.AssignedToId);
            });

            modelBuilder.Entity<TaskCommentPM>(entity =>
            {
                entity.HasKey(tc => tc.TaskCommentPMId);

                entity.HasOne(tc => tc.TaskPM)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(tc => tc.TaskPMId);

                entity.HasOne(tc => tc.CreatedBy)
                      .WithMany()
                      .HasForeignKey(tc => tc.CreatedById);
            });

            modelBuilder.Entity<TaskAttachmentPM>(entity =>
            {
                entity.HasKey(ta => ta.TaskAttachmentPMId);

                entity.HasOne(ta => ta.TaskPM)
                      .WithMany(t => t.Attachments)
                      .HasForeignKey(ta => ta.TaskPMId);

                entity.HasOne(ta => ta.UploadedBy)
                      .WithMany()
                      .HasForeignKey(ta => ta.UploadedById);
            });

            // Configure QuoteItemDiscount
            modelBuilder.Entity<QuoteItemDiscount>()
                .ToTable("QuoteItemDiscount") // Explicitly set table name
                .HasOne(d => d.QuoteItem)
                .WithOne(qi => qi.Discount)
                .HasForeignKey<QuoteItemDiscount>(d => d.QuoteItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Map DiscountType enum to string
            modelBuilder.Entity<QuoteItemDiscount>()
                .Property(d => d.DiscountType)
                .HasConversion<string>();

            // Configure PartnerProductPrice
            modelBuilder.Entity<PartnerProductPrice>().ToTable("PartnerProductPrice");
            modelBuilder.Entity<PartnerProductPrice>()
                .HasIndex(p => new { p.PartnerId, p.ProductId })
                .IsUnique();

            // Configure PartnerGroup relationship
            modelBuilder.Entity<Partner>()
                .HasOne(p => p.PartnerGroup)
                .WithMany(pg => pg.Partners)
                .HasForeignKey(p => p.PartnerGroupId)
                .OnDelete(DeleteBehavior.SetNull); // Optional: Set PartnerGroupId to null if group is deleted

            // Configure composite key for ProductGroupProduct
            modelBuilder.Entity<ProductGroupProduct>()
                .HasKey(pgp => new { pgp.ProductId, pgp.ProductGroupId });

            // Configure relationships for ProductGroupProduct
            modelBuilder.Entity<ProductGroupProduct>()
                .HasOne(pgp => pgp.Product)
                .WithMany(p => p.ProductGroupProducts)
                .HasForeignKey(pgp => pgp.ProductId);

            modelBuilder.Entity<ProductGroupProduct>()
                .HasOne(pgp => pgp.ProductGroup)
                .WithMany(pg => pg.ProductGroupProducts)
                .HasForeignKey(pgp => pgp.ProductGroupId);


            // Configure unique indexes
            modelBuilder.Entity<VatType>()
                .HasIndex(v => v.TypeName)
                .IsUnique();

            modelBuilder.Entity<PartnerProductPrice>()
                .HasIndex(cpp => new { cpp.PartnerId, cpp.ProductId })
                .IsUnique();

            modelBuilder.Entity<PartnerProductGroupPrice>()
                .HasIndex(cpgp => new { cpgp.PartnerId, cpgp.ProductGroupId })
                .IsUnique();

            //prices
            modelBuilder.Entity<VatType>().HasData(
                new VatType { VatTypeId = 1, TypeName = "27%", Rate = 27.00m }
            );

            modelBuilder.Entity<PartnerGroup>().HasData(
                new PartnerGroup { PartnerGroupId = 1, PartnerGroupName = "VIP Customers", DiscountPercentage = 5.00m }
            );

            modelBuilder.Entity<VolumeDiscount>().HasData(
                new VolumeDiscount { VolumeDiscountId = 1, ProductId = 1, MinQuantity = 10, DiscountPercentage = 10.00m }
            );

            // CustomerCommunication
            modelBuilder.Entity<CustomerCommunication>()
                .HasKey(c => c.CustomerCommunicationId);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.CommunicationType)
                .WithMany(ct => ct.CustomerCommunications)
                .HasForeignKey(c => c.CommunicationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Status)
                .WithMany(cs => cs.CustomerCommunications)
                .HasForeignKey(c => c.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Contact)
                .WithMany(c => c.CustomerCommunications)
                .HasForeignKey(c => c.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Agent)
                .WithMany(u => u.CustomerCommunications)
                .HasForeignKey(c => c.AgentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Partner)
                .WithMany(p => p.Communications)
                .HasForeignKey(c => c.PartnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Lead)
                .WithMany(l => l.Communications)
                .HasForeignKey(c => c.LeadId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Quote)
                .WithMany(q => q.Communications)
                .HasForeignKey(c => c.QuoteId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<CustomerCommunication>()
                .HasOne(c => c.Order)
                .WithMany(o => o.Communications)
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // CommunicationResponsible
            modelBuilder.Entity<CommunicationResponsible>()
                .HasKey(cr => cr.CommunicationResponsibleId);

            modelBuilder.Entity<CommunicationResponsible>()
                .HasOne(cr => cr.CustomerCommunication)
                .WithMany(c => c.ResponsibleHistory)
                .HasForeignKey(cr => cr.CustomerCommunicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunicationResponsible>()
                .HasOne(cr => cr.Responsible)
                .WithMany()
                .HasForeignKey(cr => cr.ResponsibleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunicationResponsible>()
                .HasOne(cr => cr.AssignedBy)
                .WithMany()
                .HasForeignKey(cr => cr.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            // CommunicationPost
            modelBuilder.Entity<CommunicationPost>()
                .HasKey(p => p.CommunicationPostId);

            modelBuilder.Entity<CommunicationPost>()
                .HasOne(p => p.CustomerCommunication)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CustomerCommunicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunicationPost>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Disable auto-include
            modelBuilder.Entity<CustomerCommunication>()
                .Navigation(c => c.Posts)
                .AutoInclude(false);

            modelBuilder.Entity<CustomerCommunication>()
                .Navigation(c => c.ResponsibleHistory)
                .AutoInclude(false);

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
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.Property(e => e.Quantity)
                    .HasColumnType("decimal(18,4)");
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");
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

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);


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
            .HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict); // or Cascade, as your business logic requires

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