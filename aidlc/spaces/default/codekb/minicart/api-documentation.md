# API Documentation — MiniCart Microservices

**Communication Protocol:** REST/HTTP  
**Content-Type:** JSON  
**Trace Context:** W3C Trace Context (traceparent header)  

---

## API Base URLs

| Service | URL | Port |
|---------|-----|------|
| CatalogService | `http://catalog-service:5001` | 5001 |
| CartService | `http://cart-service:5002` | 5002 |
| OrderService | `http://order-service:5003` | 5003 |
| Next.js Frontend | `http://web:3000` | 3000 |

**Local Development:** Replace service hostname with `localhost`  
**Example:** `http://localhost:5001/api/products`

---

## Health Check Endpoints

All services implement health checks for Docker container probes:

### CatalogService Health

```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "service": "CatalogService",
  "version": "1.0.0",
  "timestamp": "2026-10-09T14:30:00Z"
}
```

**Status Codes:**
- `200 OK` — Service is healthy
- `503 Service Unavailable` — Service is degraded

---

## CatalogService API

**Role:** Product catalog and inventory  
**Port:** 5001  
**Responsibilities:** Return product data, validate product existence  

### GET /api/products

List all products in catalog.

**Request:**
```http
GET /api/products HTTP/1.1
Host: catalog-service:5001
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Laptop",
      "description": "High-performance business laptop",
      "price": 999.99,
      "stockLevel": 50,
      "category": "Electronics"
    },
    {
      "id": 2,
      "name": "Mouse",
      "description": "Ergonomic wireless mouse",
      "price": 29.99,
      "stockLevel": 200,
      "category": "Accessories"
    },
    {
      "id": 3,
      "name": "Keyboard",
      "description": "Mechanical RGB keyboard",
      "price": 149.99,
      "stockLevel": 75,
      "category": "Accessories"
    }
  ],
  "count": 3,
  "timestamp": "2026-10-09T14:30:00Z"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "error": "Failed to retrieve products",
  "message": "Database connection timeout",
  "timestamp": "2026-10-09T14:30:00Z"
}
```

---

### GET /api/products/{id}

Retrieve single product by ID.

**Request:**
```http
GET /api/products/1 HTTP/1.1
Host: catalog-service:5001
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Laptop",
    "description": "High-performance business laptop",
    "price": 999.99,
    "stockLevel": 50,
    "category": "Electronics",
    "createdAt": "2026-01-15T10:00:00Z"
  },
  "timestamp": "2026-10-09T14:30:00Z"
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "error": "Product not found",
  "productId": 999,
  "timestamp": "2026-10-09T14:30:00Z"
}
```

---

### GET /api/products/search?category={category}

Search products by category.

**Request:**
```http
GET /api/products/search?category=Electronics HTTP/1.1
Host: catalog-service:5001
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Laptop",
      "category": "Electronics",
      "price": 999.99,
      "stockLevel": 50
    }
  ],
  "count": 1,
  "searchedCategory": "Electronics",
  "timestamp": "2026-10-09T14:30:00Z"
}
```

---

## CartService API

**Role:** Shopping cart management  
**Port:** 5002  
**Responsibilities:** Create carts, manage items, calculate totals  

### POST /api/cart

Create a new shopping cart.

**Request:**
```http
POST /api/cart HTTP/1.1
Host: cart-service:5002
Content-Type: application/json
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01

{
  "customerId": "customer-123"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "cart-550e8400",
    "customerId": "customer-123",
    "items": [],
    "total": 0.00,
    "createdAt": "2026-10-09T14:30:00Z",
    "lastModified": "2026-10-09T14:30:00Z"
  },
  "timestamp": "2026-10-09T14:30:00Z"
}
```

---

### GET /api/cart/{cartId}

Retrieve cart contents.

**Request:**
```http
GET /api/cart/cart-550e8400 HTTP/1.1
Host: cart-service:5002
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "cart-550e8400",
    "customerId": "customer-123",
    "items": [
      {
        "id": "item-001",
        "productId": 1,
        "quantity": 1,
        "priceAtTime": 999.99
      },
      {
        "id": "item-002",
        "productId": 2,
        "quantity": 2,
        "priceAtTime": 29.99
      }
    ],
    "total": 1059.97,
    "lastModified": "2026-10-09T14:32:00Z"
  },
  "timestamp": "2026-10-09T14:32:00Z"
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "error": "Cart not found",
  "cartId": "cart-999999",
  "timestamp": "2026-10-09T14:30:00Z"
}
```

---

### POST /api/cart/{cartId}/items

Add item to cart.

**Request:**
```http
POST /api/cart/cart-550e8400/items HTTP/1.1
Host: cart-service:5002
Content-Type: application/json
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01

{
  "productId": 1,
  "quantity": 1
}
```

**Process:**
1. CartService receives request with trace context
2. CartService calls CatalogService: `GET /api/products/1` (propagates traceparent header)
3. CatalogService returns product info (with same trace context)
4. CartService validates stock: `stockLevel >= quantity`
5. CartService adds item to cart, calculates new total
6. Returns updated cart

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "cart-550e8400",
    "items": [
      {
        "id": "item-001",
        "productId": 1,
        "quantity": 1,
        "priceAtTime": 999.99
      }
    ],
    "total": 999.99,
    "lastModified": "2026-10-09T14:32:00Z"
  },
  "itemAdded": {
    "productId": 1,
    "quantity": 1
  },
  "timestamp": "2026-10-09T14:32:00Z"
}
```

**Error Response (400 Bad Request) — Product not found:**
```json
{
  "success": false,
  "error": "Product not found",
  "productId": 999,
  "cartId": "cart-550e8400",
  "timestamp": "2026-10-09T14:32:00Z"
}
```

**Error Response (400 Bad Request) — Insufficient stock:**
```json
{
  "success": false,
  "error": "Insufficient stock",
  "productId": 1,
  "requested": 100,
  "available": 50,
  "timestamp": "2026-10-09T14:32:00Z"
}
```

---

### PUT /api/cart/{cartId}/items/{itemId}

Update item quantity in cart.

**Request:**
```http
PUT /api/cart/cart-550e8400/items/item-001 HTTP/1.1
Host: cart-service:5002
Content-Type: application/json
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01

{
  "quantity": 2
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "item-001",
    "productId": 1,
    "quantity": 2,
    "priceAtTime": 999.99,
    "subtotal": 1999.98
  },
  "cartTotal": 2029.97,
  "timestamp": "2026-10-09T14:33:00Z"
}
```

---

### DELETE /api/cart/{cartId}/items/{itemId}

Remove item from cart.

**Request:**
```http
DELETE /api/cart/cart-550e8400/items/item-001 HTTP/1.1
Host: cart-service:5002
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "cart-550e8400",
    "items": [
      {
        "id": "item-002",
        "productId": 2,
        "quantity": 2,
        "priceAtTime": 29.99
      }
    ],
    "total": 59.98,
    "lastModified": "2026-10-09T14:34:00Z"
  },
  "itemRemoved": {
    "id": "item-001",
    "productId": 1
  },
  "timestamp": "2026-10-09T14:34:00Z"
}
```

---

### GET /api/cart/{cartId}/total

Get cart total without full cart contents.

**Request:**
```http
GET /api/cart/cart-550e8400/total HTTP/1.1
Host: cart-service:5002
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "cartId": "cart-550e8400",
    "itemCount": 3,
    "total": 1089.96,
    "lastModified": "2026-10-09T14:34:00Z"
  },
  "timestamp": "2026-10-09T14:34:00Z"
}
```

---

## OrderService API

**Role:** Order processing and fulfillment  
**Port:** 5003  
**Responsibilities:** Create orders, track order status, validate before confirmation  

### POST /api/orders

Create order from cart.

**Request:**
```http
POST /api/orders HTTP/1.1
Host: order-service:5003
Content-Type: application/json
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01

{
  "cartId": "cart-550e8400",
  "customerId": "customer-123",
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Portland",
    "state": "OR",
    "zip": "97201"
  }
}
```

**Process:**
1. OrderService receives request with trace context
2. Calls CartService: `GET /api/cart/{cartId}` (propagates trace context)
3. CartService returns cart contents
4. For each item, calls CatalogService: `GET /api/products/{productId}`
5. Validates stock levels, retrieves current prices
6. Creates Order record
7. Returns confirmation with Order ID

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "order-550e8400",
    "cartId": "cart-550e8400",
    "customerId": "customer-123",
    "items": [
      {
        "productId": 1,
        "quantity": 1,
        "price": 999.99
      },
      {
        "productId": 2,
        "quantity": 2,
        "price": 29.99
      }
    ],
    "total": 1059.97,
    "status": "Confirmed",
    "shippingAddress": {
      "street": "123 Main St",
      "city": "Portland",
      "state": "OR",
      "zip": "97201"
    },
    "createdAt": "2026-10-09T14:35:00Z"
  },
  "timestamp": "2026-10-09T14:35:00Z"
}
```

**Error Response (400 Bad Request) — Cart empty:**
```json
{
  "success": false,
  "error": "Cart is empty",
  "cartId": "cart-550e8400",
  "timestamp": "2026-10-09T14:35:00Z"
}
```

**Error Response (400 Bad Request) — Item out of stock:**
```json
{
  "success": false,
  "error": "Item out of stock",
  "productId": 1,
  "requested": 5,
  "available": 2,
  "timestamp": "2026-10-09T14:35:00Z"
}
```

---

### GET /api/orders/{orderId}

Retrieve order details.

**Request:**
```http
GET /api/orders/order-550e8400 HTTP/1.1
Host: order-service:5003
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "order-550e8400",
    "cartId": "cart-550e8400",
    "customerId": "customer-123",
    "items": [
      {
        "productId": 1,
        "quantity": 1,
        "price": 999.99
      }
    ],
    "total": 999.99,
    "status": "Confirmed",
    "createdAt": "2026-10-09T14:35:00Z"
  },
  "timestamp": "2026-10-09T14:35:00Z"
}
```

---

### GET /api/orders/customer/{customerId}

List all orders for a customer.

**Request:**
```http
GET /api/orders/customer/customer-123 HTTP/1.1
Host: order-service:5003
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "order-550e8400",
      "status": "Confirmed",
      "total": 999.99,
      "createdAt": "2026-10-09T14:35:00Z"
    },
    {
      "id": "order-550e8401",
      "status": "Pending",
      "total": 149.99,
      "createdAt": "2026-10-08T10:00:00Z"
    }
  ],
  "count": 2,
  "customerId": "customer-123",
  "timestamp": "2026-10-09T14:35:00Z"
}
```

---

### PUT /api/orders/{orderId}/status

Update order status.

**Request:**
```http
PUT /api/orders/order-550e8400/status HTTP/1.1
Host: order-service:5003
Content-Type: application/json
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01

{
  "status": "Processing"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "order-550e8400",
    "status": "Processing",
    "previousStatus": "Confirmed",
    "updatedAt": "2026-10-09T14:36:00Z"
  },
  "timestamp": "2026-10-09T14:36:00Z"
}
```

---

## Standard Error Responses

All services return errors in this format:

```json
{
  "success": false,
  "error": "Error title",
  "message": "Detailed error description",
  "timestamp": "2026-10-09T14:30:00Z"
}
```

### HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | Successfully retrieved resource |
| 201 | Created | Successfully created resource (order, cart) |
| 400 | Bad Request | Invalid input (missing field, invalid type) |
| 404 | Not Found | Resource doesn't exist (product ID, cart ID) |
| 500 | Internal Server Error | Service error (unhandled exception) |
| 503 | Service Unavailable | Dependency down (CatalogService unreachable) |

---

## Trace Context Propagation (W3C Standard)

Every request and inter-service call includes the `traceparent` header:

```
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
           version---trace-id---------span-id------flags
```

| Part | Bytes | Example | Purpose |
|------|-------|---------|---------|
| Version | 2 | `00` | W3C Trace Context v0 |
| Trace ID | 32 | `550e8400e29b41d4a716446655440000` | Unique request ID (128-bit hex) |
| Span ID | 16 | `a7d1d1c0e29b41d4` | Current operation ID (64-bit hex) |
| Flags | 2 | `01` | Sampling: `01`=sample, `00`=don't sample |

### Example Flow

```
1. Frontend requests CatalogService with traceparent:
   00-550e8400...16446655440000-a7d1d1c0...e29b41d4-01

2. CatalogService receives header, uses same Trace ID:
   - Extracts: trace_id = 550e8400...16446655440000
   - Creates new span for this operation
   - Returns result

3. CatalogService calls Loki/Prometheus:
   - Logs include: trace_id = 550e8400...16446655440000

4. In Grafana:
   - Click on trace ID 550e8400...16446655440000
   - See all related logs and metrics (same trace ID)
```

---

## Request/Response Examples in cURL

### List Products

```bash
curl -i \
  -H "traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01" \
  http://localhost:5001/api/products
```

### Add Item to Cart

```bash
curl -i -X POST \
  -H "Content-Type: application/json" \
  -H "traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01" \
  -d '{"productId": 1, "quantity": 1}' \
  http://localhost:5002/api/cart/cart-123/items
```

### Create Order

```bash
curl -i -X POST \
  -H "Content-Type: application/json" \
  -H "traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01" \
  -d '{"cartId": "cart-123", "customerId": "cust-456"}' \
  http://localhost:5003/api/orders
```

---

## Service-to-Service Communication Example

When CartService calls CatalogService, it must propagate the trace context:

**Request (sent by CartService to CatalogService):**
```http
GET /api/products/1 HTTP/1.1
Host: catalog-service:5001
traceparent: 00-550e8400e29b41d4a716446655440000-<NEW-SPAN-ID>-01
```

**Key Points:**
- `Trace ID` stays the same (550e8400e29b41d4a716446655440000)
- `Span ID` is new (one per operation)
- All three services use same Trace ID
- Result: End-to-end request visibility in Grafana
