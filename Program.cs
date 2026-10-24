using System.Text.Json;
using System.Text.Json.Serialization;
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.Extensions;
using Chamapro.Api.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

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
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddServicesERepositories();

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(DevAuthenticationDefaults.Scheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationDefaults.Scheme, _ => { });
}
else
{
    builder.Services.AddAuthentication();
}
builder.Services.AddAuthorization();

var origens = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "CHAMAPRO API v1"));
}

var pastaPublica = Path.Combine(app.Environment.ContentRootPath, app.Configuration["Arquivos:PastaPublica"] ?? "uploads");
Directory.CreateDirectory(pastaPublica);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pastaPublica),
    RequestPath = "/uploads"
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();