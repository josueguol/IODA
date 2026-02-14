using System.Net;
using System.Text;
using IODA.Authorization.Application;
using IODA.Authorization.Infrastructure;
using IODA.Authorization.Domain.Exceptions;
using IODA.Shared.Api;
using IODA.Shared.Api.Middleware;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

static (HttpStatusCode StatusCode, Microsoft.AspNetCore.Mvc.ProblemDetails Details)? MapAuthorizationException(Exception ex, IHostEnvironment? _)
{
    return ex switch
    {
        RoleNotFoundException or PermissionNotFoundException or AccessRuleNotFoundException => (
            HttpStatusCode.NotFound,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 404, Title = "Not Found", Detail = ex.Message }),
        DomainException domainEx => (
            HttpStatusCode.BadRequest,
            new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Bad Request", Detail = domainEx.Message }),
        ArgumentException => IODA.Shared.Api.ExceptionMappingConvention.Map(ex),
        InvalidOperationException => IODA.Shared.Api.ExceptionMappingConvention.Map(ex),
        _ => null
    };
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSharedErrorHandling(MapAuthorizationException);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IODA Authorization API",
        Version = "v1",
        Description = "Access Rules: permisos, roles, reglas contextuales y comprobación de acceso."
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
        // 2.4: policy por permiso (JWT incluye claim "permission" con códigos desde Identity)
        options.AddPolicy("Admin", policy => policy.RequireClaim("permission", "role.manage"));
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

// 1.2: seed de permisos del catálogo (idempotente por code)
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IODA.Authorization.Infrastructure.Persistence.PermissionSeeder>();
    await seeder.SeedAsync();
}
// 2.5: rol SuperAdmin con todos los permisos del catálogo
using (var scope = app.Services.CreateScope())
{
    var superAdminSeeder = scope.ServiceProvider.GetRequiredService<IODA.Authorization.Infrastructure.Persistence.SuperAdminRoleSeeder>();
    await superAdminSeeder.SeedAsync();
}

app.UseMiddleware<IODA.Shared.Api.Middleware.ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IODA Authorization API v1"));
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
