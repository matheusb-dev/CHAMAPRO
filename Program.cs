using System.Text.Json;
using System.Text.Json.Serialization;
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.Extensions;
using Chamapro.Api.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ErrorHandlingMiddleware.RespostaValidacao;
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(new JsonUpperCaseNamingPolicy()));
    });

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(3)
    ));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddServicesERepositories();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "CHAMAPRO-DEV-KEY-CHANGE-ME-2026-!@#";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Chamapro.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Chamapro.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "sub",
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClienteOnly", policy => policy.RequireRole(Perfis.Cliente));
    options.AddPolicy("ProfissionalOnly", policy => policy.RequireRole(Perfis.Profissional));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Perfis.Admin));
    options.AddPolicy("ClienteOuAdmin", policy => policy.RequireRole(Perfis.Cliente, Perfis.Admin));
    options.AddPolicy("ProfissionalOuAdmin", policy => policy.RequireRole(Perfis.Profissional, Perfis.Admin));
});

var origens = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<LoginRateLimitMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "CHAMAPRO API v1"));
}

app.UseStaticFiles();

var pastaPublica = Path.Combine(app.Environment.ContentRootPath, app.Configuration["Arquivos:PastaPublica"] ?? "uploads");
Directory.CreateDirectory(pastaPublica);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pastaPublica),
    RequestPath = "/uploads"
});

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<UserActiveMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
