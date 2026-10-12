using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("catalog-service", "1.0.0")
    .AddAttributes(new Dictionary<string, object>
    {
        { "environment", "development" },
        { "deployment.environment", "docker" },
        { "service.namespace", "minicart" }
    });

// Create a custom meter for business metrics
var meterName = "minicart.catalog.service";
var tracerName = "minicart.catalog.service";

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

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Catalog Service starting - Service Version: 1.0.0");

// Middleware
app.UseRouting();
app.UseCors("AllowAll");

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }));
app.MapGet("/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

// Routes
app.MapGet("/v1/catalog/products", GetProducts);
app.MapGet("/v1/catalog/products/{id}", GetProduct);

app.Run("http://0.0.0.0:5001");

// Handlers
async Task<IResult> GetProducts()
{
    using (Activity.Current?.AddEvent(new ActivityEvent("GetProducts called")).GetDisposedObject())
    {
        logger.LogInformation("Retrieving all products");
        
        var products = new List<object>
        {
            new { id = 1, name = "Laptop", price = 999.99m, stock = 5 },
            new { id = 2, name = "Mouse", price = 29.99m, stock = 100 },
            new { id = 3, name = "Keyboard", price = 79.99m, stock = 50 },
            new { id = 4, name = "Monitor", price = 299.99m, stock = 10 },
            new { id = 5, name = "USB Cable", price = 9.99m, stock = 200 }
        };

        logger.LogInformation("Retrieved {ProductCount} products", products.Count);

        return Results.Ok(new
        {
            success = true,
            data = products,
            error = (string)null,
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }
}

async Task<IResult> GetProduct(int id)
{
    using (Activity.Current?.AddEvent(new ActivityEvent($"GetProduct called with id={id}")).GetDisposedObject())
    {
        logger.LogInformation("Retrieving product with id {ProductId}", id);
        
        var products = new Dictionary<int, object>
        {
            { 1, new { id = 1, name = "Laptop", price = 999.99m, stock = 5 } },
            { 2, new { id = 2, name = "Mouse", price = 29.99m, stock = 100 } },
            { 3, new { id = 3, name = "Keyboard", price = 79.99m, stock = 50 } },
            { 4, new { id = 4, name = "Monitor", price = 299.99m, stock = 10 } },
            { 5, new { id = 5, name = "USB Cable", price = 9.99m, stock = 200 } }
        };

        if (products.TryGetValue(id, out var product))
        {
            logger.LogInformation("Found product: {ProductName}", (product as dynamic).name);
            
            return Results.Ok(new
            {
                success = true,
                data = product,
                error = (string)null,
                traceId = Activity.Current?.Id ?? "unknown",
                timestamp = DateTime.UtcNow
            });
        }

        logger.LogWarning("Product not found with id {ProductId}", id);
        
        return Results.NotFound(new
        {
            success = false,
            data = (object)null,
            error = $"Product {id} not found",
            traceId = Activity.Current?.Id ?? "unknown",
            timestamp = DateTime.UtcNow
        });
    }
}
