using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Chanzup.Infrastructure;
using Chanzup.API.Authorization;
using Chanzup.API.Middleware;
using Chanzup.API.Configuration;
using Chanzup.Infrastructure.Monitoring.Middleware;
using Chanzup.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure strongly typed settings
builder.Services.Configure<ExternalServicesOptions>(
    builder.Configuration.GetSection(ExternalServicesOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<GameSettingsOptions>(
    builder.Configuration.GetSection(GameSettingsOptions.SectionName));
builder.Services.Configure<HealthChecksOptions>(
    builder.Configuration.GetSection(HealthChecksOptions.SectionName));
builder.Services.Configure<CachingOptions>(
    builder.Configuration.GetSection(CachingOptions.SectionName));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with comprehensive configuration
builder.Services.AddSwaggerDocumentation();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "Chanzup",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "Chanzup",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization with policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("BusinessOwnerOnly", policy => policy.RequireRole("BusinessOwner"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff", "BusinessOwner"));
    options.AddPolicy("PlayerOnly", policy => policy.RequireRole("Player"));
    options.AddPolicy("BusinessUsers", policy => policy.RequireRole("BusinessOwner", "Staff"));
    
    // Permission-based policies
    options.AddPolicy("CanManageCampaigns", policy => 
        policy.Requirements.Add(new PermissionRequirement("campaign:write")));
    options.AddPolicy("CanViewAnalytics", policy => 
        policy.Requirements.Add(new PermissionRequirement("analytics:read")));
    options.AddPolicy("CanVerifyRedemptions", policy => 
        policy.Requirements.Add(new PermissionRequirement("redemption:verify")));
    options.AddPolicy("CanPlayGames", policy => 
        policy.Requirements.Add(new PermissionRequirement("game:play")));
    
    // Tenant-based policy
    options.AddPolicy("RequireTenant", policy => 
        policy.Requirements.Add(new TenantRequirement()));
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantHandler>();

// Add CORS
var corsOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(',') ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Local")
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
    else
    {
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwaggerDocumentation(app.Environment);

app.UseHttpsRedirection();

// Use environment-specific CORS policy
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

// Add security middleware
app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<LocationVerificationMiddleware>();
app.UseMiddleware<MetricsMiddleware>();

app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Initialize database
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    await app.MigrateDatabaseAsync();
    
    // Check if we should seed demo data
    if (args.Contains("--seed-demo"))
    {
        await app.SeedDemoDataAsync();
    }
}

app.Run();