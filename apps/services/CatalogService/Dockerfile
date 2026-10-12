FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /app
COPY CatalogService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=builder /app/build .
EXPOSE 5001
ENTRYPOINT ["dotnet", "CatalogService.dll"]
