using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

    public class CurrencyDto
    {
        public int CurrencyId { get; set; }

        [Required, StringLength(100)]
        public string? CurrencyName { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal ExchangeRate { get; set; }

        public bool IsBaseCurrency { get; set; }

        [Required]
        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
