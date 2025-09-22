public interface IEmailEntityDto
{
    int Id { get; }
    string Number { get; }
    DateTime? Date { get; }
    string CompanyName { get; }
    decimal? TotalAmount { get; }
    string Status { get; }
    IEnumerable<object> Items { get; } // Flexible for OrderItems or QuoteItems
}