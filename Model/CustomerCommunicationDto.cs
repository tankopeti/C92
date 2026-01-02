using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

public class CustomerCommunicationDto
    {
public int CustomerCommunicationId { get; set; }
        public int CommunicationTypeId { get; set; }
        public string? CommunicationTypeName { get; set; }
        public DateTime Date { get; set; }
        public string? Subject { get; set; }
        public string? Note { get; set; }
        public int? ContactId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AgentId { get; set; }
        public string? AgentName { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? AttachmentPath { get; set; }
        public string? Metadata { get; set; }
        public string? PartnerName { get; set; } 
        public int? PartnerId { get; set; }
        public int? LeadId { get; set; }
        public int? QuoteId { get; set; }
        public int? OrderId { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }
        public List<CommunicationPostDto> Posts { get; set; }
        public CommunicationResponsibleDto? CurrentResponsible { get; set; }
        public List<CommunicationResponsibleDto> ResponsibleHistory { get; set; }
    }

    public class CommunicationStatusDto
    {
        public int StatusId { get; set; }
        public string Name { get; set; }= string.Empty;
        public string? Description { get; set; }
    }

    public class CommunicationTypeDto
    {
        public int CommunicationTypeId { get; set; }
        public string Name { get; set; }
    }

    public class CommunicationPostDto
    {
        public int CommunicationPostId { get; set; }
        public string? Content { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CommunicationResponsibleDto
    {
        public int CommunicationResponsibleId { get; set; }
        public string? ResponsibleId { get; set; }
        public string? ResponsibleName { get; set; }
        public string? AssignedById { get; set; }
        public string? AssignedByName { get; set; }
        public DateTime? AssignedAt { get; set; }
    }

    public class PostDto
    {
        public string Content { get; set; }
    }

    public class AssignResponsibleDto
    {
        [Required(ErrorMessage = "ResponsibleUserId is required.")]
        public string ResponsibleUserId { get; set; } // Use string to match ApplicationUser.Id
    }
    
    public class AddCommunicationDto
    {
        [Required]
        public int CommunicationTypeId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string Subject { get; set; } = string.Empty;
        public string? Note { get; set; }
        [Required]
        // public int? ContactId { get; set; }
        public string? AgentId { get; set; }
        [Required]
        public int StatusId { get; set; }
        public string? ResponsibleId { get; set; }
        public int? PartnerId { get; set; }
        public int? SiteId { get; set; }
        // public int? LeadId { get; set; }
        // public int? QuoteId { get; set; }
        // public int? OrderId { get; set; }
    }
}