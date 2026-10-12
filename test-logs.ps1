# Loki Integration Test Script

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Logs Integration Test" -ForegroundColor Cyan  
Write-Host "================================" -ForegroundColor Cyan

# Test 1: Generate traffic
Write-Host "`n[1/5] Generating traffic from services..." -ForegroundColor Yellow
for ($i = 0; $i -lt 10; $i++) {
    Invoke-WebRequest http://localhost:5001/v1/catalog/products -UseBasicParsing | Out-Null
    try {
        Invoke-WebRequest http://localhost:5002/v1/cart/test-cart/items -UseBasicParsing | Out-Null
    } catch {}
    try {
        Invoke-WebRequest http://localhost:5003/v1/order -UseBasicParsing | Out-Null
    } catch {}
    Start-Sleep -Milliseconds 200
}
Write-Host "Check traffic generated" -ForegroundColor Green

# Wait for logs to propagate
Write-Host "`n[2/5] Waiting for logs to propagate..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test 2: Check Loki labels
Write-Host "`n[3/5] Checking Loki labels..." -ForegroundColor Yellow
try {
    $labels = (Invoke-WebRequest http://localhost:3100/loki/api/v1/labels -UseBasicParsing | ConvertFrom-Json).data
    Write-Host "Available labels in Loki:" -ForegroundColor Cyan
    $labels | ForEach-Object { Write-Host "  - $_" }
} catch {
    Write-Host "Error querying Loki labels" -ForegroundColor Red
}

# Test 3: Check service values
Write-Host "`n[4/5] Checking services with logs..." -ForegroundColor Yellow
try {
    $services = (Invoke-WebRequest http://localhost:3100/loki/api/v1/label/service_name/values -UseBasicParsing | ConvertFrom-Json).data
    Write-Host "Services with logs:" -ForegroundColor Cyan
    if ($services.Count -gt 0) {
        $services | ForEach-Object { Write-Host "  - $_" }
    } else {
        Write-Host "  No service_name labels found (checking job label)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error querying service labels" -ForegroundColor Red
}

# Test 4: Check job labels
Write-Host "`n[5/5] Checking containers with logs..." -ForegroundColor Yellow
try {
    $jobs = (Invoke-WebRequest http://localhost:3100/loki/api/v1/label/job/values -UseBasicParsing | ConvertFrom-Json).data
    if ($jobs.Count -gt 0) {
        Write-Host "Containers found:" -ForegroundColor Cyan
        $jobs | ForEach-Object { Write-Host "  - $_" }
    } else {
        Write-Host "No job labels found yet" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error querying job labels" -ForegroundColor Red
}

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

Write-Host "`nNext: Check Grafana Dashboard" -ForegroundColor Green
Write-Host "1. Grafana URL: http://localhost:3000" -ForegroundColor Cyan
Write-Host "2. Credentials: admin / admin" -ForegroundColor Cyan
Write-Host "3. Open: Minicart Microservices dashboard" -ForegroundColor Cyan
Write-Host "4. Logs panel should now show data" -ForegroundColor Cyan

