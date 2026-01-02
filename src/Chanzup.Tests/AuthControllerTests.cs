using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Xunit;
using Chanzup.Infrastructure.Data;
using Chanzup.Application.DTOs;

namespace Chanzup.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task LoginBusiness_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        await SeedTestDataAsync();
        
        var loginRequest = new LoginRequest
        {
            Email = "owner@coffeeshop.com",
            Password = "DemoPassword123!"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login/business", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.Equal("BusinessOwner", loginResponse.User.Role);
        Assert.Equal("owner@coffeeshop.com", loginResponse.User.Email);
    }

    [Fact]
    public async Task LoginBusiness_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        
        var loginRequest = new LoginRequest
        {
            Email = "owner@coffeeshop.com",
            Password = "WrongPassword"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login/business", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await context.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedDemoDataAsync(context);
    }
}