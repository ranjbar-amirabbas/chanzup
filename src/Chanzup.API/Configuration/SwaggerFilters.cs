using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Chanzup.API.Authorization;
using Microsoft.OpenApi.Any;

namespace Chanzup.API.Configuration;

/// <summary>
/// Adds example values to Swagger documentation
/// </summary>
public class SwaggerExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionName = context.MethodInfo.Name.ToLower();
        var controllerName = context.MethodInfo.DeclaringType?.Name.Replace("Controller", "").ToLower();

        // Add examples based on endpoint
        if (controllerName == "auth" && actionName.Contains("login"))
        {
            AddLoginExamples(operation, actionName);
        }
        else if (controllerName == "auth" && actionName.Contains("register"))
        {
            AddRegisterExamples(operation);
        }
        else if (controllerName == "campaign")
        {
            AddCampaignExamples(operation, actionName);
        }
        else if (controllerName == "prize")
        {
            AddPrizeExamples(operation, actionName);
        }

        // Add common response examples
        AddCommonResponseExamples(operation);
    }

    private void AddLoginExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.RequestBody?.Content?.ContainsKey("application/json") == true)
        {
            var example = actionName switch
            {
                var name when name.Contains("business") => new OpenApiObject
                {
                    ["email"] = new OpenApiString("owner@coffeeshop.com"),
                    ["password"] = new OpenApiString("DemoPassword123!")
                },
                var name when name.Contains("player") => new OpenApiObject
                {
                    ["email"] = new OpenApiString("demo@player.com"),
                    ["password"] = new OpenApiString("PlayerPassword123!")
                },
                var name when name.Contains("admin") => new OpenApiObject
                {
                    ["email"] = new OpenApiString("admin@chanzup.com"),
                    ["password"] = new OpenApiString("AdminPassword123!")
                },
                _ => new OpenApiObject
                {
                    ["email"] = new OpenApiString("user@example.com"),
                    ["password"] = new OpenApiString("Password123!")
                }
            };

            operation.RequestBody.Content["application/json"].Example = example;
        }
    }

    private void AddRegisterExamples(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content?.ContainsKey("application/json") == true)
        {
            var example = new OpenApiObject
            {
                ["businessName"] = new OpenApiString("My Coffee Shop"),
                ["email"] = new OpenApiString("owner@mycoffeeshop.com"),
                ["password"] = new OpenApiString("SecurePassword123!"),
                ["phone"] = new OpenApiString("+1-555-123-4567"),
                ["address"] = new OpenApiString("123 Main Street, City, State 12345"),
                ["latitude"] = new OpenApiDouble(40.7128),
                ["longitude"] = new OpenApiDouble(-74.0060),
                ["subscriptionTier"] = new OpenApiInteger(1)
            };

            operation.RequestBody.Content["application/json"].Example = example;
        }
    }

    private void AddCampaignExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.RequestBody?.Content?.ContainsKey("application/json") == true && actionName.Contains("create"))
        {
            var example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Summer Promotion"),
                ["description"] = new OpenApiString("Spin the wheel for summer prizes!"),
                ["gameType"] = new OpenApiInteger(0), // WheelOfLuck
                ["tokenCostPerSpin"] = new OpenApiInteger(5),
                ["maxSpinsPerDay"] = new OpenApiInteger(3),
                ["startDate"] = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                ["endDate"] = new OpenApiString(DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                ["isActive"] = new OpenApiBoolean(true)
            };

            operation.RequestBody.Content["application/json"].Example = example;
        }
    }

    private void AddPrizeExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.RequestBody?.Content?.ContainsKey("application/json") == true && actionName.Contains("create"))
        {
            var example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Free Coffee"),
                ["description"] = new OpenApiString("One free regular coffee"),
                ["value"] = new OpenApiDouble(5.00),
                ["currency"] = new OpenApiString("USD"),
                ["totalQuantity"] = new OpenApiInteger(100),
                ["winProbability"] = new OpenApiDouble(0.25),
                ["isActive"] = new OpenApiBoolean(true)
            };

            operation.RequestBody.Content["application/json"].Example = example;
        }
    }

    private void AddCommonResponseExamples(OpenApiOperation operation)
    {
        // Add 401 Unauthorized example
        if (operation.Responses.ContainsKey("401"))
        {
            var unauthorizedExample = new OpenApiObject
            {
                ["error"] = new OpenApiString("Unauthorized"),
                ["message"] = new OpenApiString("Invalid or missing authentication token")
            };
            operation.Responses["401"].Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Example = unauthorizedExample
                }
            };
        }

        // Add 403 Forbidden example
        if (operation.Responses.ContainsKey("403"))
        {
            var forbiddenExample = new OpenApiObject
            {
                ["error"] = new OpenApiString("Forbidden"),
                ["message"] = new OpenApiString("Insufficient permissions to access this resource")
            };
            operation.Responses["403"].Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Example = forbiddenExample
                }
            };
        }

        // Add 500 Internal Server Error example
        if (operation.Responses.ContainsKey("500"))
        {
            var errorExample = new OpenApiObject
            {
                ["error"] = new OpenApiString("Internal server error"),
                ["message"] = new OpenApiString("An unexpected error occurred")
            };
            operation.Responses["500"].Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Example = errorExample
                }
            };
        }
    }
}

/// <summary>
/// Adds authorization information to Swagger operations
/// </summary>
public class SwaggerAuthorizationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .Any() == true;

        var hasAllowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AllowAnonymousAttribute>()
            .Any() == true;

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized - Invalid or missing authentication token" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden - Insufficient permissions" });

            // Add security requirements
            var jwtScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement { [jwtScheme] = new List<string>() }
            };

            // Add role/permission information
            var roleAttributes = context.MethodInfo.GetCustomAttributes<RequireRoleAttribute>()
                .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes<RequireRoleAttribute>() ?? Enumerable.Empty<RequireRoleAttribute>());

            var permissionAttributes = context.MethodInfo.GetCustomAttributes<RequirePermissionAttribute>()
                .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes<RequirePermissionAttribute>() ?? Enumerable.Empty<RequirePermissionAttribute>());

            var requirements = new List<string>();

            foreach (var roleAttr in roleAttributes)
            {
                var roles = roleAttr.GetType().GetField("_roles", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(roleAttr) as string[];
                if (roles != null)
                {
                    requirements.Add($"Roles: {string.Join(", ", roles)}");
                }
            }

            foreach (var permAttr in permissionAttributes)
            {
                var permission = permAttr.GetType().GetField("_permission", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(permAttr) as string;
                if (permission != null)
                {
                    requirements.Add($"Permission: {permission}");
                }
            }

            if (requirements.Any())
            {
                operation.Description += $"\n\n**Authorization Requirements:**\n- {string.Join("\n- ", requirements)}";
            }
        }
    }
}

/// <summary>
/// Adds descriptions to Swagger tags
/// </summary>
public class SwaggerTagDescriptionFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "🔐 Authentication",
                Description = "Endpoints for user authentication, registration, and token management. Supports business staff, players, and admin authentication with JWT tokens."
            },
            new OpenApiTag
            {
                Name = "🏢 Business Management",
                Description = "Business-related operations including business information, dashboard metrics, and business settings management."
            },
            new OpenApiTag
            {
                Name = "📢 Campaign Management",
                Description = "Create, update, and manage marketing campaigns. Configure game types, prizes, and campaign settings."
            },
            new OpenApiTag
            {
                Name = "📊 Analytics & Reporting",
                Description = "Business analytics, reporting, and insights. Track campaign performance, player engagement, and revenue metrics."
            },
            new OpenApiTag
            {
                Name = "📱 QR Code & Sessions",
                Description = "QR code generation, scanning, and session management for location-based game interactions."
            },
            new OpenApiTag
            {
                Name = "🎰 Wheel of Fortune",
                Description = "Wheel of fortune game mechanics, spinning, and prize distribution logic."
            },
            new OpenApiTag
            {
                Name = "🎁 Prize Management",
                Description = "Prize creation, inventory management, and prize configuration for campaigns."
            },
            new OpenApiTag
            {
                Name = "🎫 Prize Redemption",
                Description = "Prize redemption process, verification, and redemption history tracking."
            },
            new OpenApiTag
            {
                Name = "👤 Player Management",
                Description = "Player account management, token balance, game history, and player analytics."
            },
            new OpenApiTag
            {
                Name = "⚙️ Admin Management",
                Description = "Administrative functions for system management, business approval, and platform oversight."
            },
            new OpenApiTag
            {
                Name = "❤️ Health Checks",
                Description = "System health monitoring endpoints for database connectivity, external services, and overall system status."
            }
        };
    }
}

/// <summary>
/// Improves enum documentation in Swagger schemas
/// </summary>
public class SwaggerEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            var enumValues = new List<string>();

            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                var enumMember = context.Type.GetMember(enumValue.ToString()!).FirstOrDefault();
                var description = enumMember?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? enumValue.ToString();
                
                schema.Enum.Add(new OpenApiInteger((int)enumValue));
                enumValues.Add($"{(int)enumValue}: {enumValue} - {description}");
            }

            schema.Description = $"Possible values:\n- {string.Join("\n- ", enumValues)}";
        }
    }
}