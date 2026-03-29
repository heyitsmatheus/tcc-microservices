namespace OrderProcessor.Models;

public class OrderRequest
{
    public Guid OrderId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}