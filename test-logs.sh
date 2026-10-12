#!/bin/bash

echo "================================"
echo "Logs Integration Test"
echo "================================"

# Test 1: Generate traffic
echo -e "\n[1/5] Generating traffic from services..."
for i in {1..10}; do
    curl -s http://localhost:5001/v1/catalog/products > /dev/null
    curl -s http://localhost:5002/v1/cart/test-cart/items -X GET > /dev/null 2>&1 || true
    curl -s http://localhost:5003/v1/order -X GET > /dev/null 2>&1 || true
    sleep 0.2
done
echo "✓ Traffic generated"

# Wait for logs to propagate
echo -e "\n[2/5] Waiting for logs to propagate through stack..."
sleep 5

# Test 2: Check Loki labels
echo -e "\n[3/5] Checking Loki labels..."
LABELS=$(curl -s http://localhost:3100/loki/api/v1/labels | jq -r '.data[]' 2>/dev/null)
echo "Available labels in Loki:"
echo "$LABELS" | head -10

# Test 3: Check service values
echo -e "\n[4/5] Checking services with logs in Loki..."
SERVICES=$(curl -s http://localhost:3100/loki/api/v1/label/service_name/values | jq -r '.data[]' 2>/dev/null || echo "unknown_service")
echo "Services with logs: $SERVICES"

# Test 4: Query Loki for recent logs
echo -e "\n[5/5] Querying Loki for recent logs (last 1 minute)..."
NOW_NS=$(date +%s)000000000
MINUTE_AGO_NS=$((NOW_NS - 60000000000))

# Try query with just a matcher
RESULT=$(curl -s "http://localhost:3100/loki/api/v1/query_range?query=%7Bjob%3D~%22.%2B%22%7D&start=$MINUTE_AGO_NS&end=$NOW_NS" 2>/dev/null | jq '.data.result | length' 2>/dev/null || echo "0")
echo "Recent log entries found: $RESULT"

echo -e "\n================================"
echo "Verification Complete"
echo "================================"
echo -e "\nNext: Open Grafana at http://localhost:3000"
echo "  → Explore → Loki"
echo "  → Query: {job=~\".+\"}"
echo "  → Should see service logs"
