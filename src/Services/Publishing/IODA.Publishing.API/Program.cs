using System.Net;
using System.Text;
using IODA.Publishing.Application;
using IODA.Publishing.Application.Exceptions;
using IODA.Publishing.Infrastructure;
using IODA.Publishing.Domain.Exceptions;
using IODA.Shared.Api;
using IODA.Shared.Api.Middleware;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

static (HttpStatusCode StatusCode, Microsoft.AspNetCore.Mvc.ProblemDetails Details)? MapPublishingException(Exception ex, IHostEnvironment? _)
{
    return ex switch
    {
        CoreApiException coreEx => CreateCoreApiProblem(coreEx),
        PublicationRequestNotFoundException => (
            HttpStatusCode.NotFound,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 404, Title = "Not Found", Detail = ex.Message }),
        DomainException domainEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Bad Request", Detail = domainEx.Message }),
        ArgumentException => IODA.Shared.Api.ExceptionMappingConvention.Map(ex),
        InvalidOperationException => IODA.Shared.Api.ExceptionMappingConvention.Map(ex),
        HttpRequestException => (
            HttpStatusCode.BadGateway,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 502, Title = "Bad Gateway", Detail = "Core API unavailable or returned an error." }),
        _ => null
    };

    static (HttpStatusCode, Microsoft.AspNetCore.Mvc.ProblemDetails) CreateCoreApiProblem(CoreApiException ex)
    {
        if (ex.ProblemDetails != null)
        {
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = ex.ProblemDetails.Status ?? ex.StatusCode,
                Title = $"Core API Error: {ex.ProblemDetails.Title ?? "Error"}",
                Detail = ex.ProblemDetails.Detail ?? ex.Message
            };
            if (ex.ProblemDetails.Extensions != null)
                problem.Extensions = new Dictionary<string, object?>(ex.ProblemDetails.Extensions);
            problem.Extensions ??= new Dictionary<string, object?>();
            problem.Extensions["coreApiStatusCode"] = ex.StatusCode;
            return ((HttpStatusCode)(problem.Status ?? ex.StatusCode), problem);
        }
        return ((HttpStatusCode)ex.StatusCode, new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = ex.StatusCode, Title = "Core API Error", Detail = ex.Message });
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSharedErrorHandling(MapPublishingException);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IODA Publishing API",
        Version = "v1",
        Description = "Ciclo de vida del contenido: solicitudes de publicación, validación y aprobación (llamada al Core API para publicar). Requiere JWT (rol Editor para solicitudes; aprobar/rechazar suelen requerir rol con permiso de publicación)."
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

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ioda-cms";
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Editor", policy => policy.RequireRole("Editor", "Admin"));
    });
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    allowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173", "https://localhost:3000", "https://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IODA Publishing API v1"));
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
