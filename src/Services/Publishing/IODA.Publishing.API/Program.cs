using System.Text;
using IODA.Publishing.API.Middleware;
using IODA.Publishing.Application;
using IODA.Publishing.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseMiddleware<ErrorHandlingMiddleware>();

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
