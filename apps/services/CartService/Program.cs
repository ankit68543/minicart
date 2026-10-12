using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
var carts = new Dictionary<string, CartData>();

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
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("http://catalog-service:5001");
    client.Timeout = TimeSpan.FromSeconds(2);
});

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("cart-service", "1.0.0")
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
logger.LogInformation("Cart Service starting - Service Version: 1.0.0");

// Middleware
app.UseRouting();
app.UseCors("AllowAll");

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/live", () => Results.Ok(new { status = "alive" }));

// Routes
app.MapPost("/v1/cart", CreateCart);
app.MapPost("/v1/cart/{cartId}/items", AddItemToCart);
app.MapDelete("/v1/cart/{cartId}/items/{itemId}", RemoveItemFromCart);
app.MapGet("/v1/cart/{cartId}", GetCart);

app.Run("http://0.0.0.0:5002");

// Handlers
async Task<IResult> CreateCart()
{
    var cartId = Guid.NewGuid().ToString();
    var cart = new CartData { Id = cartId };
    carts[cartId] = cart;

    return Results.Created($"/v1/cart/{cartId}", new
    {
        success = true,
        data = new { id = cartId, items = cart.Items, total = cart.Total },
        error = (string)null,
        traceId = Activity.Current?.Id ?? "unknown",
        timestamp = DateTime.UtcNow
    });
}

async Task<IResult> AddItemToCart(string cartId, int productId, int quantity)
{

    if (!carts.ContainsKey(cartId))
    {
        return Results.NotFound(new
        {
            success = false,
            data = (object)null,
            error = "Cart not found",
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }

    try
    {
        // Call CatalogService to get product info
        var client = httpClientFactory.CreateClient("catalog");
        var response = await client.GetAsync($"/v1/catalog/products/{productId}");
        
        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest(new
            {
                success = false,
                data = (object)null,
                error = "Product not found or unavailable",
                traceId = Activity.Current?.Id ?? "unknown",
                timestamp = DateTime.UtcNow
            });
        }

        var content = await response.Content.ReadAsStringAsync();
        dynamic productResponse = System.Text.Json.JsonSerializer.Deserialize<dynamic>(content);
        dynamic product = ((System.Text.Json.JsonElement)productResponse)
            .GetProperty("data");

        var cart = carts[cartId];
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Name = product.GetProperty("name").GetString(),
                Price = product.GetProperty("price").GetDecimal(),
                Quantity = quantity
            });
        }

        return Results.Ok(new
        {
            success = true,
            data = new { id = cartId, items = cart.Items, total = cart.Total },
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

async Task<IResult> RemoveItemFromCart(string cartId, int itemId)
{

    if (!carts.ContainsKey(cartId))
    {
        return Results.NotFound(new
        {
            success = false,
            data = (object)null,
            error = "Cart not found",
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }

    var cart = carts[cartId];
    var item = cart.Items.FirstOrDefault(i => i.ProductId == itemId);
    
    if (item != null)
    {
        cart.Items.Remove(item);
    }

    return Results.Ok(new
    {
        success = true,
        data = new { id = cartId, items = cart.Items, total = cart.Total },
        error = (string)null,
        traceId = Activity.Current?.Id ?? "unknown",
        timestamp = DateTime.UtcNow
    });
}

async Task<IResult> GetCart(string cartId)
{

    if (!carts.TryGetValue(cartId, out var cart))
    {
        return Results.NotFound(new
        {
            success = false,
            data = (object)null,
            error = "Cart not found",
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }

    return Results.Ok(new
    {
        success = true,
        data = new { id = cartId, items = cart.Items, total = cart.Total },
        error = (string)null,
        traceId = Activity.Current?.Id ?? "unknown",
        timestamp = DateTime.UtcNow
    });
}

// ============================================================================
// SUPPORTING CLASSES
// ============================================================================

class CartData
{
    public string Id { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);
}

class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
