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
    }
}