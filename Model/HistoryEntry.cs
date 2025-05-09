using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
public class HistoryEntry
{
    public string Action { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; }
    public string Notes { get; set; }
    
    [NotMapped]
    public string DisplayAction => Action switch
    {
        "Created" => "Creation",
        "StatusChanged" => "Status Change",
        "FieldUpdated" => "Update",
        _ => Action
    };
}
}