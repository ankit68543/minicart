**Collaborator:** aidlc-developer-agent

## Developer Code Practices

Affirmed for MiniCart ASP.NET Core microservices:

### Code Structure & Organization

**Per-Service Folder Structure:**
```
CartService/
├── Controllers/
│   └── CartController.cs
├── Services/
│   ├── ICartService.cs
│   └── CartService.cs
├── Models/
│   ├── Cart.cs
│   ├── CartItem.cs
│   └── CreateCartItemRequest.cs
├── Exceptions/
│   ├── CartNotFoundException.cs
│   └── InsufficientStockException.cs
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
├── Program.cs
└── appsettings.json
```

**Separation of Concerns:**
- **Controllers:** HTTP mapping only, delegate to services
- **Services:** Business logic, orchestration, calls to other services
- **Models:** Data transfer objects (DTOs), domain entities
- **Middleware:** Cross-cutting concerns (logging, error handling, trace context)

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Classes | PascalCase | `CartService`, `CartController` |
| Interfaces | PascalCase with I prefix | `ICartService`, `IProductCatalog` |
| Methods | PascalCase | `AddToCart()`, `ValidateStock()` |
| Private methods | PascalCase (not camelCase) | `ValidateRequest()` |
| Parameters | camelCase | `cartId`, `productId` |
| Local variables | camelCase | `cart`, `items` |
| Constants | UPPER_SNAKE_CASE | `DEFAULT_TIMEOUT` |
| Properties | PascalCase | `CartId`, `TotalPrice` |
| Private fields | camelCase with underscore | `_logger`, `_catalogService` |

### Async/Await Patterns (MANDATORY)

**Rule:** All I/O operations (HTTP calls, database queries) MUST be async.

✅ **DO:**
```csharp
public async Task<Cart> GetCartAsync(string cartId)
{
    return await _cartRepository.GetAsync(cartId);
}

public async Task<bool> AddItemAsync(string cartId, AddItemRequest request)
{
    var cart = await GetCartAsync(cartId);
    var product = await _catalogService.GetProductAsync(request.ProductId);
    
    if (!await _catalogService.IsInStockAsync(product.Id, request.Quantity))
        throw new InsufficientStockException();
    
    cart.Items.Add(new CartItem { ... });
    await _cartRepository.SaveAsync(cart);
    return true;
}
```

❌ **DON'T:**
```csharp
// Blocking async operations
public Cart GetCart(string cartId) => _cartRepository.GetAsync(cartId).Result;

// Mixing sync/async
public async Task<Cart> GetCart(string cartId)
{
    var cart = _cartRepository.Get(cartId); // Blocking call in async method!
    return cart;
}
```

**Why:** Prevents thread pool starvation and deadlocks in high-concurrency scenarios.

### Dependency Injection (Constructor Injection ONLY)

✅ **DO:**
```csharp
public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CartService> _logger;
    
    public CartService(
        ICartRepository cartRepository,
        ICatalogService catalogService,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _catalogService = catalogService;
        _logger = logger;
    }
}
```

❌ **DON'T:**
```csharp
// Property injection
public ICartRepository CartRepository { get; set; }

// Service locator pattern
var service = ServiceLocator.GetService<ICartService>();

// Static state
public static ICartRepository Repository { get; set; }
```

**Why:** Makes dependencies explicit, testable, and prevents circular references.

**DI Registration in Program.cs:**
```csharp
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddHttpClient<ICatalogService, CatalogServiceClient>();
```

### Error Handling & Exceptions

**Rule:** Use exceptions for exceptional cases; never use exceptions for control flow.

✅ **DO:**
```csharp
public async Task<Cart> GetCartAsync(string cartId)
{
    var cart = await _cartRepository.GetAsync(cartId);
    if (cart == null)
        throw new CartNotFoundException($"Cart {cartId} not found");
    return cart;
}
```

❌ **DON'T:**
```csharp
public async Task<Cart> GetCartAsync(string cartId)
{
    try
    {
        return await _cartRepository.GetAsync(cartId);
    }
    catch (CartNotFoundException)
    {
        // Control flow using exception
    }
}
```

**Custom Exceptions:**
```csharp
public class CartNotFoundException : Exception
{
    public CartNotFoundException(string message) : base(message) { }
}

public class InsufficientStockException : Exception
{
    public string ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    
    public InsufficientStockException(
        string productId, 
        int requestedQuantity) 
        : base($"Insufficient stock: {productId} ({requestedQuantity} requested)")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
    }
}
```

### HTTP Response Mapping

All endpoints return consistent response format:

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
    public string TraceId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Controller implementation:**
```csharp
[HttpPost("cart")]
public async Task<ActionResult<ApiResponse<Cart>>> AddToCart(AddToCartRequest request)
{
    try
    {
        var cart = await _cartService.AddItemAsync(request.CartId, request.Item);
        return Ok(new ApiResponse<Cart>
        {
            Success = true,
            Data = cart,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (CartNotFoundException ex)
    {
        return NotFound(new ApiResponse<Cart>
        {
            Success = false,
            Error = ex.Message,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Comments & Documentation

- **Public methods:** XML documentation comments
- **Complex logic:** Inline comments explaining "why", not "what"
- **No:** Over-commenting obvious code

```csharp
/// <summary>
/// Adds an item to the shopping cart and validates stock availability.
/// </summary>
/// <param name="cartId">The unique cart identifier</param>
/// <param name="request">Request containing product ID and quantity</param>
/// <returns>Updated cart with new item</returns>
/// <exception cref="CartNotFoundException">Thrown when cart does not exist</exception>
/// <exception cref="InsufficientStockException">Thrown when insufficient stock</exception>
public async Task<Cart> AddItemAsync(string cartId, AddToCartRequest request)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(cartId))
        throw new ArgumentNullException(nameof(cartId));
    
    var cart = await _cartRepository.GetAsync(cartId);
    // ... implementation
}
```

### Testability

**Rules for testable code:**
1. No static dependencies
2. All dependencies injected
3. No direct DateTime.Now (inject IClock)
4. No direct Guid.NewGuid() (inject IIdGenerator)
5. All async methods start with Async suffix
6. Private logic should be tested through public APIs

### Code Review Checklist

Every code submission must pass:
- ✅ Naming conventions followed
- ✅ All async methods end with `Async`
- ✅ All I/O is async
- ✅ Constructor injection only
- ✅ No static state
- ✅ Exceptions used correctly
- ✅ Consistent response format
- ✅ Public methods documented
- ✅ Unit tests include
- ✅ Integration tests for cross-service calls

### Sign-Off

Developer code practices affirmed. Standards ready for Code Generation.

**Developer consent:** Affirmed ✅
