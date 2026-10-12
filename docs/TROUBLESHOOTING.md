# Troubleshooting Guide

Common issues and solutions when running MiniCart.

---

## Docker & Compose Issues

### Issue: "docker: command not found"

**Problem:** Docker CLI is not installed or not in PATH

**Solution:**
```powershell
# Verify Docker is running in WSL 2
wsl -u root systemctl is-active docker

# If not running:
wsl -u root systemctl start docker

# Test Docker:
docker version
```

### Issue: "Cannot connect to Docker daemon"

**Problem:** Docker daemon not started in WSL 2

**Solution:**
```powershell
# Start Docker in WSL
wsl -u root systemctl start docker

# Verify:
wsl -u root systemctl is-active docker

# If daemon keeps stopping, rebuild WSL distro (last resort)
wsl --unregister Ubuntu-22.04
wsl --install Ubuntu-22.04
```

### Issue: "docker-compose: not found" or wrong version

**Problem:** Docker Compose V2 (Plugin) might not be installed

**Solution:**
```powershell
# Check version
docker compose version

# Should output:
# Docker Compose version v2.X.X

# If not available, install manually:
$version = "2.25.0"  # Check latest on GitHub releases
$dest = "$env:USERPROFILE\AppData\Local\Docker\cli-plugins"
New-Item -ItemType Directory -Path $dest -Force | Out-Null
# Download and place docker-compose.exe there
```

### Issue: "Network already exists"

**Problem:** Networks from previous run still exist

**Solution:**
```bash
# Remove existing networks
docker network rm minicart-app minicart-observability

# Then restart
docker compose up -d
```

---

## Service Issues

### Issue: "Service container fails to start"

**Problem:** Check service logs

**Solution:**
```powershell
# View logs for a service
docker compose logs cart-service

# Watch logs in real-time
docker compose logs -f cart-service

# Check specific error
docker compose logs catalog-service | Select-String ERROR
```

### Issue: "Services cannot connect to each other"

**Problem:** Networking issue or DNS resolution problem

**Solution:**
```bash
# Test DNS resolution within a container
docker compose exec cart-service nslookup catalog-service

# If DNS fails, services are not on same network
# Check docker-compose.yml: both should have "networks: - app"

# Test connectivity
docker compose exec cart-service curl http://catalog-service:80/health
```

### Issue: "Port already in use"

**Problem:** Another service is using port 3000, 3001, etc.

**Solution:**
```powershell
# Find what's using port 3000
Get-NetTCPConnection -LocalPort 3000

# Kill that process (if safe)
Stop-Process -Id <PID> -Force

# Or change the port in docker-compose.yml
# "3001:80" → "3004:80"
```

### Issue: "Service is not healthy"

**Problem:** Health check failing

**Solution:**
```bash
# Check health endpoint directly
curl http://localhost:3001/health

# Should return 200 OK and JSON

# If fails, check service logs
docker compose logs catalog-service

# Common causes:
# - Service hasn't started yet (wait a few seconds)
# - Port mapping wrong
# - Service crashed during startup
```

---

## Frontend Issues

### Issue: "Cannot connect to APIs from web"

**Problem:** API URLs are wrong or services not running

**Solution:**
1. **Check environment variables in docker-compose.yml:**
   ```yaml
   environment:
     NEXT_PUBLIC_API_CATALOG_URL: http://catalog-service:80
   ```

2. **Test connectivity:**
   ```bash
   docker compose exec web curl http://catalog-service:80/api/products
   ```

3. **Check browser console for 404 or connection errors**

### Issue: "Frontend loads but APIs show errors in browser"

**Problem:** CORS, routing, or port mapping issue

**Solution:**
```powershell
# 1. Verify APIs are running
docker compose ps

# 2. Test API directly from host
curl http://localhost:3001/api/products

# 3. Check if APIs are on app network
docker network inspect minicart-app | Select-String catalog-service

# 4. Verify port mapping
docker port catalog-service | Select-String 80
# Should show: 80/tcp -> 0.0.0.0:3001
```

### Issue: "Next.js build fails"

**Problem:** TypeScript or build error

**Solution:**
```bash
# View build logs
docker compose logs web

# Common causes:
# - TypeScript compilation error (check tsconfig.json)
# - Missing dependency (check package.json)
# - Dockerfile issue (check Dockerfile)

# Rebuild with no cache
docker compose build --no-cache web
```

---

## Observability Issues

### Issue: "No metrics in Prometheus"

**Problem:** OTel Collector not receiving metrics or Prometheus not scraping

**Solution:**
```bash
# 1. Check OTel Collector is running
docker compose ps otel-collector

# 2. Test OTLP endpoint
curl -X POST http://localhost:4317/ \
  -H "Content-Type: application/grpc" \
  -d ""

# 3. Check Prometheus config
curl http://localhost:9090/api/v1/targets
# Look for "UP" status for all targets

# 4. Check service is emitting metrics
docker compose logs catalog-service | grep -i "otel\|metrics"

# 5. Check Prometheus storage
docker compose exec prometheus ls /prometheus/
```

### Issue: "No logs in Loki"

**Problem:** Log pipeline not configured or services not sending logs

**Solution:**
```bash
# 1. Check Loki is running
docker compose ps loki

# 2. Check Loki has received logs
curl http://localhost:3100/loki/api/v1/labels

# Should return: {"status":"success","data":["service","level",...]}

# 3. If empty, check if Docker Loki plugin is available
docker plugin ls | grep loki

# 4. Alternative: Check if services are logging to stdout
docker compose logs cart-service | wc -l

# 5. Check Loki config
docker compose exec loki cat /etc/loki/local-config.yml
```

### Issue: "No traces in Tempo"

**Problem:** Services not sending traces to Collector

**Solution:**
```bash
# 1. Check Tempo is running
docker compose ps tempo

# 2. Check Collector is receiving traces
docker compose logs otel-collector | grep -i trace

# 3. Verify service OTEL environment variables
docker compose exec cart-service env | grep OTEL

# 4. Test OTLP connection from service
docker compose exec cart-service \
  curl -X POST http://otel-collector:4317/ \
  -H "Content-Type: application/grpc" \
  -d ""

# 5. Query Tempo for traces
curl http://localhost:3200/api/traces
```

### Issue: "Grafana shows no data"

**Problem:** Data sources misconfigured or not connected

**Solution:**
```bash
# 1. Access Grafana
# http://localhost:3000
# Login: admin / admin

# 2. Check data sources
# Settings → Data Sources
# Look for: Prometheus, Loki, Tempo
# Each should show "Successfully saved and tested"

# 3. Test each data source
# Click on data source → Test button

# 4. If test fails, check URLs are correct
# Prometheus: http://prometheus:9090
# Loki: http://loki:3100
# Tempo: http://tempo:3200

# 5. Check these are reachable from Grafana
docker compose exec grafana curl http://prometheus:9090/-/healthy
```

### Issue: "Grafana port 3000 conflicts with web"

**Problem:** Both web and Grafana want port 3000

**Solution 1 - Change web port:**
```yaml
# docker-compose.yml
web:
  ports:
    - "3001:3000"  # Change external port
```

**Solution 2 - Change Grafana port:**
```yaml
grafana:
  ports:
    - "3000:3000"  # Keep Grafana at 3000
  # Then access web at:
  # http://localhost:3001
```

---

## Performance Issues

### Issue: "Services are slow (latency > 500ms)"

**Problem:** Could be any component

**Investigation:**
```bash
# 1. Check system resources
docker stats

# 2. Check if a specific service is slow
# Use Prometheus query:
# histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# 3. Check if network is slow
docker compose exec web ping catalog-service

# 4. Check OTel Collector is not overloaded
docker compose logs otel-collector | grep -i "drop\|error"

# 5. Restart services if memory is high
docker compose down
docker compose up -d
```

### Issue: "Docker taking too much disk space"

**Problem:** Volumes or images accumulating

**Solution:**
```bash
# Clean up old images
docker image prune -a

# Clean up volumes
docker volume prune

# Check volume usage
docker volume ls
du -sh /var/lib/docker/volumes/*/

# Clean Prometheus data (warning: deletes metrics!)
docker compose exec prometheus rm -rf /prometheus/*

# Clean Loki data (warning: deletes logs!)
docker compose exec loki rm -rf /loki/*

# Clean Tempo data (warning: deletes traces!)
docker compose exec tempo rm -rf /var/tempo/*
```

---

## AIDLC & Configuration Issues

### Issue: "aidlc command not found"

**Problem:** AIDLC not in PATH or new terminal needed

**Solution:**
```powershell
# 1. Use full path
C:\Users\<YourUsername>\AppData\Local\aidlc\bin\aidlc.cmd config

# 2. OR restart PowerShell to reload PATH
exit
# Open new PowerShell window

# 3. Verify installation
$env:PATH -split ';' | Select-String aidlc
```

### Issue: "aidlc config fails"

**Problem:** Git not initialized or invalid directory

**Solution:**
```bash
# 1. Ensure you're in project root
cd D:\OTelLearning\minicart

# 2. Verify git repo exists
ls .git

# 3. Verify .aidlc directory
ls .aidlc

# 4. Reconfigure
C:\Users\<YourUsername>\AppData\Local\aidlc\bin\aidlc.cmd config --harness copilot
```

### Issue: "GitHub Copilot not connected"

**Problem:** Copilot not authenticated

**Solution:**
1. Open VS Code
2. Open GitHub Copilot Chat (Ctrl+Shift+I)
3. Follow authentication prompt
4. Sign in with GitHub
5. Allow VS Code to access Copilot API
6. Try `/aidlc` command again

---

## Network Debugging

### Command: Inspect a network

```bash
docker network inspect minicart-app
```

**Look for:**
- All expected services listed under "Containers"
- Each service has an IP address
- Services can reach each other via DNS name (service name)

### Command: Test inter-service connectivity

```bash
# From web container, can you reach catalog-service?
docker compose exec web curl http://catalog-service:80/api/products

# From cart-service, can you reach order-service?
docker compose exec cart-service curl http://order-service:80/health
```

### Command: Check DNS resolution

```bash
docker compose exec web nslookup catalog-service

# Should resolve to an IP like 172.X.X.X
```

---

## Database & Persistence (When You Add It)

### Issue: "Data disappeared after restart"

**Problem:** This is expected! Data is in-memory

**Explanation:**
MiniCart stores data in memory. When services restart, all data is lost. This is intentional for learning.

**When ready to persist:**
1. Add PostgreSQL or MongoDB to docker-compose.yml
2. Update services to use database instead of in-memory
3. Add migration scripts

---

## Common Workflow

### "I made a change and it's not showing up"

```bash
# 1. Rebuild the affected service
docker compose build catalog-service

# 2. Restart the service
docker compose up -d catalog-service

# 3. Verify it's running
docker compose ps catalog-service

# 4. Check logs
docker compose logs catalog-service

# 5. Test the endpoint
curl http://localhost:3001/health
```

### "I want to start fresh"

```bash
# 1. Stop all services
docker compose down

# 2. Remove volumes (WARNING: deletes data!)
docker compose down -v

# 3. Rebuild all images
docker compose build --no-cache

# 4. Start fresh
docker compose up -d

# 5. Verify all healthy
docker compose ps
```

### "I want to see what's happening"

```bash
# 1. Watch all logs
docker compose logs -f

# 2. Watch a specific service
docker compose logs -f cart-service

# 3. Search logs for errors
docker compose logs | Select-String ERROR

# 4. Get last 50 lines
docker compose logs --tail 50 catalog-service

# 5. Timestamp format
docker compose logs -t
```

---

## Getting Help

If something is broken:

1. **Check logs** — 90% of issues are in logs
   ```bash
   docker compose logs <service>
   ```

2. **Verify connectivity** — Test with curl
   ```bash
   curl http://localhost:3001/health
   docker compose exec web curl http://catalog-service/health
   ```

3. **Check configuration** — Verify docker-compose.yml
   ```bash
   docker compose config
   ```

4. **Restart everything** — Sometimes it just needs a fresh start
   ```bash
   docker compose down
   docker compose up -d
   ```

5. **Check resources** — Ensure Docker has enough CPU/RAM
   ```bash
   docker stats
   ```

---

## Next Steps

- Review [ARCHITECTURE.md](ARCHITECTURE.md) for service design
- Review [OBSERVABILITY.md](OBSERVABILITY.md) for telemetry setup
- Run `/aidlc` to proceed with Phase 1
