#!/bin/bash

# MiniCart E2E Test Script
# Run this after services are started

set -e

echo "=== MiniCart E2E Test ==="
echo ""

CATALOG_URL="http://localhost:5001"
CART_URL="http://localhost:5002"
ORDER_URL="http://localhost:5003"

# Test 1: Get products
echo "1. Fetching products from CatalogService..."
PRODUCTS=$(curl -s "$CATALOG_URL/v1/catalog/products")
echo "$PRODUCTS" | jq '.'
PRODUCT_ID=$(echo "$PRODUCTS" | jq -r '.data[0].id')
echo ""

# Test 2: Create cart
echo "2. Creating cart..."
CART=$(curl -s -X POST "$CART_URL/v1/cart")
CART_ID=$(echo "$CART" | jq -r '.data.id')
echo "Cart ID: $CART_ID"
echo ""

# Test 3: Add item to cart
echo "3. Adding product $PRODUCT_ID to cart..."
ADDED=$(curl -s -X POST "$CART_URL/v1/cart/$CART_ID/items?productId=$PRODUCT_ID&quantity=1")
echo "$ADDED" | jq '.'
echo ""

# Test 4: Create order
echo "4. Creating order..."
ORDER=$(curl -s -X POST "$ORDER_URL/v1/orders" \
  -H "Content-Type: application/json" \
  -d "{\"cartId\": \"$CART_ID\"}")
ORDER_ID=$(echo "$ORDER" | jq -r '.data.id')
TRACE_ID=$(echo "$ORDER" | jq -r '.traceId')
echo "Order ID: $ORDER_ID"
echo "Trace ID: $TRACE_ID"
echo ""

echo "✓ Test completed successfully!"
echo ""
echo "View trace in Grafana:"
echo "  1. Open http://localhost:3000"
echo "  2. Go to Explore → Tempo"
echo "  3. Search for trace ID: $TRACE_ID"
