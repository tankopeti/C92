using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class Status
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "Active", "Inactive", "Prospect"
        public string? Color { get; set; } // e.g., "#28a745" (green)
        public string? Description { get; set; }
    }
}