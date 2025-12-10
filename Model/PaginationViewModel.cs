using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string SearchTerm { get; set; }
        public string PageRoute { get; set; }
        public string EntityName { get; set; }
    }
}