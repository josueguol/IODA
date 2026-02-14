using System.Net;
using System.Text;
using IODA.Core.Application;
using IODA.Core.Infrastructure;
using IODA.Core.Infrastructure.Persistence;
using IODA.Core.API.Middleware;
using IODA.Core.Domain.Exceptions;
using IODA.Shared.Api;
using IODA.Shared.Api.Extensions;
using IODA.Shared.Api.Middleware;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

static (HttpStatusCode StatusCode, Microsoft.AspNetCore.Mvc.ProblemDetails Details)? MapCoreException(Exception ex, IHostEnvironment? env)
{
    return ex switch
    {
        SchemaValidationException schemaEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 400,
                Title = "Schema Validation Error",
                Detail = "Content does not conform to the schema.",
                Extensions = new Dictionary<string, object?>
                {
                    ["errors"] = schemaEx.Errors.GroupBy(e => e.Field).ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray())
                }
            }),
        ContentNotFoundException or SiteNotFoundException or SchemaNotFoundException or ProjectNotFoundException or EnvironmentNotFoundException => (
            HttpStatusCode.NotFound,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 404, Title = "Not Found", Detail = ex.Message }),
        DomainException domainEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Domain Error", Detail = domainEx.Message }),
        ArgumentException argEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Bad Request", Detail = argEx.Message }),
        InvalidOperationException opEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Invalid Operation", Detail = opEx.Message }),
        _ => null
    };
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSharedErrorHandling(MapCoreException);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IODA Core API",
        Version = "v1",
        Description = "API del CMS Core: proyectos, schemas y contenido."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Bearer token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<CoreDbContext>("database", failureStatus: HealthStatus.Unhealthy);

var rabbitEnabled = string.Equals(builder.Configuration["RabbitMQ:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
var rabbitHost = builder.Configuration["RabbitMQ:Host"];
if (rabbitEnabled && !string.IsNullOrWhiteSpace(rabbitHost))
{
    var rabbitVhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
    var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    var rabbitConnectionString = $"amqp://{Uri.EscapeDataString(rabbitUser)}:{Uri.EscapeDataString(rabbitPassword)}@{rabbitHost}/{Uri.EscapeDataString(rabbitVhost.TrimStart('/'))}";
    healthChecksBuilder.AddRabbitMQ(rabbitConnectionString, name: "rabbitmq", failureStatus: HealthStatus.Degraded);
}

builder.Services.AddDefaultCors(builder.Configuration, builder.Environment);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// 1.4: en no-Development, no arrancar sin configuración crítica
if (!builder.Environment.IsDevelopment())
{
    var missing = new List<string>();
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SecretKey"])) missing.Add("Jwt:SecretKey");
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection"))) missing.Add("ConnectionStrings:DefaultConnection");
    if (missing.Count > 0)
        throw new InvalidOperationException($"Missing required configuration: {string.Join(", ", missing)}. Set via environment variables or vault.");
}

var app = builder.Build();

app.UseMiddleware<IODA.Shared.Api.Middleware.ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IODA Core API v1"));
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
