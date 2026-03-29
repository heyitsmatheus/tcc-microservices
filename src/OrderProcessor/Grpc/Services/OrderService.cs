using Grpc.Core;
using TccMicroservices.Grpc;

namespace OrderProcessor.Grpc.Services;

public class OrderService(ILogger<OrderService> logger) : TccMicroservices.Grpc.OrderService.OrderServiceBase
{
    private readonly ILogger<OrderService> _logger = logger;

    public override Task<OrderResponse> ProcessOrder(OrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Processing order {OrderId}", request.OrderId);

        var response = new OrderResponse
        {
            OrderId = request.OrderId,
            Status = "PROCESSED",
            ProcessedAt = DateTime.UtcNow.ToString("o")
        };

        return Task.FromResult(response);
    }
}