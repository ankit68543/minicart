using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// In-memory storage
var orders = new Dictionary<string, OrderData>();

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// HttpClient with trace context propagation
builder.Services.AddHttpClient("cart", client =>
{
    client.BaseAddress = new Uri("http://cart-service:5002");
    client.Timeout = TimeSpan.FromSeconds(2);
});

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("order-service", "1.0.0")
    .AddAttributes(new Dictionary<string, object>
    {
        { "environment", "development" },
        { "deployment.environment", "docker" },
        { "service.namespace", "minicart" }
    });

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(meterProvider =>
    {
        meterProvider
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging
        .SetResourceBuilder(resourceBuilder)
        .IncludeScopes = true
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://alloy:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        });
});

var app = builder.Build();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Order Service starting - Service Version: 1.0.0");

// Middleware
app.UseRouting();
app.UseCors("AllowAll");

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/live", () => Results.Ok(new { status = "alive" }));

// Routes
app.MapPost("/v1/orders", CreateOrder);
app.MapGet("/v1/orders/{orderId}", GetOrder);

app.Run("http://0.0.0.0:5003");

// ============================================================================
// HANDLER FUNCTIONS
// ============================================================================

async Task<IResult> CreateOrder(CreateOrderRequest request)
{

    try
    {
        // Call CartService to get cart
        var client = httpClientFactory.CreateClient("cart");
        var response = await client.GetAsync($"/v1/cart/{request.CartId}");
        
        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest(new
            {
                success = false,
                data = (object)null,
                error = "Cart not found",
                traceId = Activity.Current?.Id ?? "unknown",
                timestamp = DateTime.UtcNow
            });
        }

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var cartResponse = JsonSerializer.Deserialize<dynamic>(content, options);

        var orderId = Guid.NewGuid().ToString();
        var order = new OrderData
        {
            Id = orderId,
            CartId = request.CartId,
            Status = "confirmed"
        };

        orders[orderId] = order;

        return Results.Created($"/v1/orders/{orderId}", new
        {
            success = true,
            data = new
            {
                id = orderId,
                cartId = request.CartId,
                status = "confirmed",
                total = 0m,
                createdAt = order.CreatedAt
            },
            error = (string)null,
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
}

async Task<IResult> GetOrder(string orderId)
{

    if (!orders.TryGetValue(orderId, out var order))
    {
        return Results.NotFound(new
        {
            success = false,
            data = (object)null,
            error = "Order not found",
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }

    return Results.Ok(new
    {
        success = true,
        data = new
        {
            id = order.Id,
            cartId = order.CartId,
            items = order.Items,
            total = order.Total,
            status = order.Status,
            createdAt = order.CreatedAt
        },
        error = (string)null,
        traceId = Activity.Current?.Id ?? "unknown",
        timestamp = DateTime.UtcNow
    });
}

// ============================================================================
// SUPPORTING CLASSES
// ============================================================================

class OrderData
{
    public string Id { get; set; }
    public string CartId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

class OrderItem
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

class CreateOrderRequest
{
    public string CartId { get; set; }
}
