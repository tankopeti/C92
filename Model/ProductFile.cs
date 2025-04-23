using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ProductFile
    {
        [Key]
        public int ProductFileId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? ProductUOMId { get; set; }

        [Required, StringLength(200)]
        public string FileName { get; set; }

        [Required, StringLength(50)]
        public string FileType { get; set; }

        [Range(0, long.MaxValue)]
        public long FileSize { get; set; }

        [Required]
        public byte[] FileData { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, StringLength(50)]
        public string FileCategory { get; set; }

        public bool IsPrimaryImage { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; }
        public ProductUOM ProductUOM { get; set; }
        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// ProductFileId: Primary key.
// ProductId: Foreign key to Product.
// ProductUOMId: Optional link to ProductUOM for packaging unit-specific files (e.g., PDF for “Pack”).
// FileName: Name (e.g., “manual.pdf”).
// FileType: MIME type (e.g., “application/pdf”, “image/jpeg”).
// FileSize: Size in bytes.
// FileData: Binary data (PDFs, images, etc.).
// Description: Optional caption (e.g., “User Manual”).
// FileCategory: Type (e.g., “Image”, “Manual”, “Certificate”).
// IsPrimaryImage: For FileCategory = “Image”, marks primary display image.
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Product, ProductUOM, Creator, LastModifier.
// Purpose:
// Stores files (PDFs for manuals, images for visuals) with metadata.
// Supports packaging unit files via ProductUOMId.
// Replaces ProductImage for unified file handling.