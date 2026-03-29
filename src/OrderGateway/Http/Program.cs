using System.Text;
using System.Text.Json;
using OrderGateway.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("processor");

var app = builder.Build();

app.MapPost("/api/orders", async (OrderRequest request, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var processorUrl = configuration["PROCESSOR_URL"]
        ?? throw new InvalidOperationException("PROCESSOR_URL not configured");

    var client = httpClientFactory.CreateClient("processor");

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync($"{processorUrl}/api/orders", content);

    if (!response.IsSuccessStatusCode)
        return Results.StatusCode((int)response.StatusCode);

    var body = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<OrderResponse>(body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    return Results.Ok(result);
});

app.Run();