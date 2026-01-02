# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Chanzup.API/Chanzup.API.csproj", "src/Chanzup.API/"]
COPY ["src/Chanzup.Application/Chanzup.Application.csproj", "src/Chanzup.Application/"]
COPY ["src/Chanzup.Domain/Chanzup.Domain.csproj", "src/Chanzup.Domain/"]
COPY ["src/Chanzup.Infrastructure/Chanzup.Infrastructure.csproj", "src/Chanzup.Infrastructure/"]

RUN dotnet restore "src/Chanzup.API/Chanzup.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Chanzup.API"
RUN dotnet build "Chanzup.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Chanzup.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Chanzup.API.dll"]