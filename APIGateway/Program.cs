using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry with comprehensive metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "APIGateway",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

// CORS - allow frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:55757",  // Vite dev server
                "http://localhost:3000"     // Docker frontend
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// JWT Authentication with Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"] 
            ?? "http://localhost:18080/realms/instaclone";
        options.Audience = builder.Configuration["Keycloak:Audience"] 
            ?? "public-client";
        options.RequireHttpsMetadata = false; // Dev only - use true in production
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"] 
                ?? "http://localhost:18080/realms/instaclone",
            ValidateAudience = true,
            ValidAudiences = new[] { "public-client", "account" },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy for routes that require authentication
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
    
    // Note: For anonymous/public routes, don't specify AuthorizationPolicy in YARP config
    // YARP treats routes without a policy as public by default
});

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Prometheus metrics endpoint at /metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Map reverse proxy with custom transforms
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        // Extract user information from JWT claims and add as headers
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Get user ID from 'sub' claim (Keycloak standard)
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? context.User.FindFirst("sub")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers["X-User-Id"] = userId;
            }
            
            // Optionally add other claims
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value 
                     ?? context.User.FindFirst("email")?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                context.Request.Headers["X-User-Email"] = email;
            }
            
            var username = context.User.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(username))
            {
                context.Request.Headers["X-User-Name"] = username;
            }
        }
        
        await next();
    });
});

app.Run();
