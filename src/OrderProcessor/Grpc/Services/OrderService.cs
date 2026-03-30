using System.Diagnostics;
using System.Diagnostics.Metrics;
using Grpc.Core;
using TccMicroservices.Grpc;

namespace OrderProcessor.Grpc.Services;

public class OrderService(
    ILogger<OrderService> logger,
    Counter<long> ordersProcessed,
    Histogram<double> processingTime) : TccMicroservices.Grpc.OrderService.OrderServiceBase
{
    private readonly Counter<long> _ordersProcessed = ordersProcessed;
    private readonly Histogram<double> _processingTime = processingTime;

    public override Task<OrderResponse> ProcessOrder(
        OrderRequest request, ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Processing order {OrderId}", request.OrderId);

        var response = new OrderResponse
        {
            OrderId = request.OrderId,
            Status = "PROCESSED",
            ProcessedAt = DateTime.UtcNow.ToString("o")
        };

        stopwatch.Stop();

        _ordersProcessed.Add(1);
        _processingTime.Record(stopwatch.Elapsed.TotalMilliseconds);

        return Task.FromResult(response);
    }
}