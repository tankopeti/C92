using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

    public class ProductDto
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ListPrice { get; set; }
        public decimal VolumePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public VolumePricing VolumePricing { get; set; }
    }

    public class VolumePricing
    {
        public int Volume1 { get; set; }
        public decimal? Volume1Price { get; set; }
        public int Volume2 { get; set; }
        public decimal? Volume2Price { get; set; }
        public int Volume3 { get; set; }
        public decimal? Volume3Price { get; set; }
}
}