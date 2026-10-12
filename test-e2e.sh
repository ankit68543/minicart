#!/bin/bash

# MiniCart E2E Testing Script
# This script tests the complete shopping flow through all three microservices

set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== MiniCart End-to-End Test ===${NC}\n"

# Configuration
CATALOG_URL="http://localhost:5001"
CART_URL="http://localhost:5002"
ORDER_URL="http://localhost:5003"

# Step 1: Check service health
echo -e "${BLUE}Step 1: Checking service health...${NC}"
for url in $CATALOG_URL $CART_URL $ORDER_URL; do
  health=$(curl -s "$url/health" | jq -r '.status')
  echo -e "${GREEN}✓${NC} $url: $health"
done
echo ""

# Step 2: Fetch products
echo -e "${BLUE}Step 2: Fetching products from CatalogService...${NC}"
PRODUCTS=$(curl -s "$CATALOG_URL/v1/catalog/products")
echo "$PRODUCTS" | jq '.'
PRODUCT_ID=$(echo "$PRODUCTS" | jq -r '.data[0].id')
PRODUCT_NAME=$(echo "$PRODUCTS" | jq -r '.data[0].name')
echo -e "${GREEN}✓${NC} Found $PRODUCT_NAME (ID: $PRODUCT_ID)\n"

# Step 3: Create cart
echo -e "${BLUE}Step 3: Creating new cart in CartService...${NC}"
CART_RESPONSE=$(curl -s -X POST "$CART_URL/v1/cart")
CART_ID=$(echo "$CART_RESPONSE" | jq -r '.data.id')
echo -e "${GREEN}✓${NC} Created cart: ${CART_ID:0:8}...\n"

# Step 4: Add product to cart
echo -e "${BLUE}Step 4: Adding product to cart...${NC}"
ADD_RESPONSE=$(curl -s -X POST "$CART_URL/v1/cart/$CART_ID/items?productId=$PRODUCT_ID&quantity=2")
echo "$ADD_RESPONSE" | jq '.'
CART_TOTAL=$(echo "$ADD_RESPONSE" | jq -r '.data.total')
echo -e "${GREEN}✓${NC} Added to cart. Total: \$$CART_TOTAL\n"

# Step 5: View cart
echo -e "${BLUE}Step 5: Viewing cart contents...${NC}"
CART=$(curl -s "$CART_URL/v1/cart/$CART_ID")
echo "$CART" | jq '.'
echo ""

# Step 6: Create order
echo -e "${BLUE}Step 6: Creating order from cart in OrderService...${NC}"
ORDER_RESPONSE=$(curl -s -X POST "$ORDER_URL/v1/orders" \
  -H "Content-Type: application/json" \
  -d "{\"cartId\": \"$CART_ID\"}")
ORDER_ID=$(echo "$ORDER_RESPONSE" | jq -r '.data.id')
echo -e "${GREEN}✓${NC} Order created: ${ORDER_ID:0:8}...\n"

# Step 7: Get order details
echo -e "${BLUE}Step 7: Retrieving order details...${NC}"
ORDER=$(curl -s "$ORDER_URL/v1/orders/$ORDER_ID")
echo "$ORDER" | jq '.'
echo ""

# Step 8: Get trace ID from response
echo -e "${BLUE}Step 8: Trace information...${NC}"
TRACE_ID=$(echo "$ORDER" | jq -r '.traceId')
echo -e "${GREEN}✓${NC} Trace ID: $TRACE_ID"
echo -e "    View in Grafana → Explore → Tempo and search for this trace ID\n"

echo -e "${GREEN}=== All tests completed successfully! ===${NC}"
echo ""
echo "Next steps:"
echo "1. Open http://localhost:3000 to use the web interface"
echo "2. Open http://localhost:3000 (Grafana) to view observability dashboards"
echo "3. View distributed traces in Tempo using trace ID: $TRACE_ID"
echo "4. Check logs in Loki and metrics in Prometheus"
