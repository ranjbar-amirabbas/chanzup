using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Chanzup.API.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Chanzup API",
                Description = "A comprehensive API for the Chanzup gamification platform that enables businesses to create engaging campaigns and manage customer interactions through QR code-based games.",
                Contact = new OpenApiContact
                {
                    Name = "Chanzup Support",
                    Email = "support@chanzup.com",
                    Url = new Uri("https://chanzup.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' followed by a space and your JWT token. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // XML Comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Custom Schema IDs
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Operation Filters
            options.OperationFilter<SwaggerExampleFilter>();
            options.OperationFilter<SwaggerAuthorizationFilter>();

            // Document Filters
            options.DocumentFilter<SwaggerTagDescriptionFilter>();

            // Schema Filters
            options.SchemaFilter<SwaggerEnumSchemaFilter>();

            // Enable annotations
            options.EnableAnnotations();

            // Group endpoints by tags
            options.TagActionsBy(api => new[] { GetControllerName(api.ActionDescriptor.RouteValues["controller"]) });
            options.DocInclusionPredicate((name, api) => true);

            // Custom ordering
            options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.EnvironmentName == "Local")
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chanzup API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Chanzup API Documentation";
                
                // UI Customization
                options.DefaultModelsExpandDepth(-1); // Hide models section by default
                options.DefaultModelExpandDepth(2);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnableValidator();
                
                // Custom CSS
                options.InjectStylesheet("/swagger-ui/custom.css");
                
                // Try it out enabled by default
                options.SupportedSubmitMethods(
                    Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                    Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                    Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                    Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete,
                    Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Patch
                );
            });
        }

        return app;
    }

    private static string GetControllerName(string? controllerName)
    {
        return controllerName switch
        {
            "Auth" => "🔐 Authentication",
            "Business" => "🏢 Business Management",
            "Campaign" => "📢 Campaign Management",
            "Analytics" => "📊 Analytics & Reporting",
            "QR" => "📱 QR Code & Sessions",
            "Wheel" => "🎰 Wheel of Fortune",
            "Prize" => "🎁 Prize Management",
            "Redemption" => "🎫 Prize Redemption",
            "Player" => "👤 Player Management",
            "Admin" => "⚙️ Admin Management",
            "Health" => "❤️ Health Checks",
            _ => controllerName ?? "Other"
        };
    }
}