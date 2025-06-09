using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class VatType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VatTypeId { get; set; }

        [Required(ErrorMessage = "VAT type is required")]
        [MaxLength(50, ErrorMessage = "VAT type name cannot exceed 50 characters")]
        [ValidateTypeAndRate]
        public string TypeName { get; set; } = "27%";

        [Required(ErrorMessage = "VAT rate is required")]
        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; } = 27.00m;

        public string FormattedRate => $"{Rate}%";
    }

    public class ValidateTypeAndRateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var vatType = (VatType)validationContext.ObjectInstance;
            var type = (string)value; // The value of TypeName
            var expectedType = $"{vatType.Rate}%"; // Expected TypeName based on Rate

            if (type != expectedType)
            {
                return new ValidationResult($"The Type must be '{expectedType}' to match the Rate of {vatType.Rate}.");
            }

            return ValidationResult.Success;
        }
    }

    public class VatTypeDto
    {
        public int VatTypeId { get; set; }
        public string TypeName { get; set; }
        public decimal Rate { get; set; }
        public string FormattedRate { get; set; }
    }
    
}