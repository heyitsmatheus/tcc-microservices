namespace OrderGateway.Kafka.Models;

public class OrderResponse
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}