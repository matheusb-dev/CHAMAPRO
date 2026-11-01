
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.Extensions;
using Chamapro.Api.Middlewares;
using Chamapro.Api.Models.Enums;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using Npgsql;
using Npgsql.NameTranslation;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ======================================================
// CONFIGURAÇÕES GERAIS
// ======================================================

// A configuração será carregada pelo appsettings.json,
// appsettings.{Environment}.json e variáveis de ambiente.

// ======================================================
// CONTROLLERS E CONFIGURAÇÃO JSON
// ======================================================

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
            ErrorHandlingMiddleware.RespostaValidacao;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(new JsonUpperCaseNamingPolicy())
        );
    });

// ======================================================
// BANCO DE DADOS - POSTGRESQL / SUPABASE
// ======================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não foi encontrada. " +
        "Verifique o appsettings.json ou a variável de ambiente."
    );
}

try
{
    var connectionBuilder =
        new NpgsqlConnectionStringBuilder(connectionString);

    if (string.IsNullOrWhiteSpace(connectionBuilder.Host) ||
        string.IsNullOrWhiteSpace(connectionBuilder.Database) ||
        string.IsNullOrWhiteSpace(connectionBuilder.Username) ||
        string.IsNullOrWhiteSpace(connectionBuilder.Password))
    {
        throw new InvalidOperationException(
            "A connection string do PostgreSQL está incompleta. " +
            "Verifique Host, Database, Username e Password."
        );
    }
}
catch (ArgumentException ex)
{
    throw new InvalidOperationException(
        "A connection string do PostgreSQL possui formato inválido.",
        ex
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );

            npgsql.MapEnum<TipoUsuario>(
                "tipo_usuario",
                nameTranslator: new NpgsqlNullNameTranslator()
            );

            npgsql.MapEnum<StatusPedido>(
                "status_pedido",
                nameTranslator: new NpgsqlNullNameTranslator()
            );

            npgsql.MapEnum<StatusAgendamento>(
                "status_agendamento",
                nameTranslator: new NpgsqlNullNameTranslator()
            );

            npgsql.MapEnum<StatusVerificacao>(
                "status_verificacao",
                nameTranslator: new NpgsqlNullNameTranslator()
            );
        }
    );
});

// ======================================================
// INJEÇÃO DE DEPENDÊNCIAS
// ======================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddHostedService<
    Chamapro.Api.Services.AdminBootstrapService
>();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddServicesERepositories();

// ======================================================
// AUTENTICAÇÃO JWT
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key não foi configurada. " +
        "Verifique o appsettings.json ou a variável de ambiente."
    );
}

if (jwtKey.StartsWith("SUA_", StringComparison.OrdinalIgnoreCase) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key precisa ser uma chave secreta válida com pelo menos 32 bytes."
    );
}

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"] ?? "Chamapro.Api";

var jwtAudience =
    builder.Configuration["Jwt:Audience"] ?? "Chamapro.Clients";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata =
            !builder.Environment.IsDevelopment();

        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType = "sub",

                RoleClaimType =
                    System.Security.Claims.ClaimTypes.Role
            };
    });

// ======================================================
// AUTORIZAÇÃO E POLÍTICAS
// ======================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "ClienteOnly",
        policy => policy.RequireRole(Perfis.Cliente)
    );

    options.AddPolicy(
        "ProfissionalOnly",
        policy => policy.RequireRole(Perfis.Profissional)
    );

    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole(Perfis.Admin)
    );

    options.AddPolicy(
        "ClienteOuAdmin",
        policy => policy.RequireRole(
            Perfis.Cliente,
            Perfis.Admin
        )
    );

    options.AddPolicy(
        "ProfissionalOuAdmin",
        policy => policy.RequireRole(
            Perfis.Profissional,
            Perfis.Admin
        )
    );
});

// ======================================================
// CORS
// ======================================================

var origens = builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (origens.Length > 0)
        {
            policy
                .WithOrigins(origens)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy
                .WithOrigins(
                    "http://localhost:5500",
                    "https://localhost:5501",
                    "http://127.0.0.1:5500",
                    "https://127.0.0.1:5501"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// ======================================================
// SWAGGER
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CHAMAPRO API",
        Version = "v1",
        Description = "API do sistema CHAMAPRO"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "Insira seu token JWT para autenticar as requisições."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        }
    );
});

// ======================================================
// CONSTRUÇÃO DA APLICAÇÃO
// ======================================================

var app = builder.Build();

// ======================================================
// HTTPS
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ======================================================
// MIDDLEWARES
// ======================================================

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseMiddleware<LoginRateLimitMiddleware>();

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "CHAMAPRO API v1"
        );

        options.RoutePrefix = "swagger";
    });
}

// ======================================================
// ARQUIVOS ESTÁTICOS
// ======================================================

app.UseDefaultFiles();

app.UseStaticFiles();

var pastaPublica = Path.Combine(
    app.Environment.ContentRootPath,
    app.Configuration["Arquivos:PastaPublica"] ?? "uploads"
);

Directory.CreateDirectory(pastaPublica);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pastaPublica),
    RequestPath = "/uploads"
});

// ======================================================
// PIPELINE HTTP
// ======================================================

app.UseCors();

app.UseAuthentication();

app.UseMiddleware<UserActiveMiddleware>();

app.UseAuthorization();

// ======================================================
// MAPEAMENTO DOS CONTROLLERS
// ======================================================

app.MapControllers();

// ======================================================
// INICIALIZAÇÃO
// ======================================================

app.Run();
