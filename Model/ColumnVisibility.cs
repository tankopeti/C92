namespace Cloud9_2.Models
{
    public class ColumnVisibility
    {
        public int Id { get; set; }
        public string PageName { get; set; } // e.g., "/Admin/LoginLog"
        public string RoleName { get; set; } // e.g., "SuperAdmin", "Admin"
        public string ColumnName { get; set; } // e.g., "UserId", "LoginTime"
        public bool IsVisible { get; set; } // True if column is visible for this role
    }
}

//Store column visibility settings for each role and page.