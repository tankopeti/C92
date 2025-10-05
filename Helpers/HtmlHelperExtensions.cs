using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cloud9_2.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static bool IsActive(this IHtmlHelper html, string pageName, string area = null)
        {
            var routeData = html.ViewContext.RouteData;
            var currentPage = routeData.Values["page"]?.ToString()?.TrimStart('/') ?? string.Empty;

            // Construct expected page path
            var expectedPage = string.IsNullOrEmpty(area) ? pageName : $"{area}/{pageName}";

            // Debug: Log comparison
            System.Diagnostics.Debug.WriteLine($"IsActive: expectedPage={expectedPage}, currentPage={currentPage}, routeData={string.Join(", ", routeData.Values.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");

            // Match with or without /Index
            bool isMatch = currentPage.Equals(expectedPage, StringComparison.OrdinalIgnoreCase) ||
                           currentPage.Equals(expectedPage.Replace("/Index", ""), StringComparison.OrdinalIgnoreCase);

            // Extra debug for Contacts
            if (expectedPage.Contains("Contacts"))
            {
                System.Diagnostics.Debug.WriteLine($"Contacts Debug: isMatch={isMatch}, expectedPage={expectedPage}, currentPage={currentPage}");
            }

            return isMatch;
        }
    }
}