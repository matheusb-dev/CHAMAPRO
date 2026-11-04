using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.NameTranslation;
try {
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development") throw new Exception();
    var config=new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true).AddUserSecrets(typeof(AppDbContext).Assembly,true).AddEnvironmentVariables().Build();
    var cs=new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
    if(cs.Host is not("localhost" or "127.0.0.1" or "::1")||cs.Port!=5432||cs.Database!="chamapro_local")throw new Exception();
    var translator=new NpgsqlNullNameTranslator();
    var options=new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs.ConnectionString,p=>{p.MapEnum<TipoUsuario>("tipo_usuario",nameTranslator:translator);p.MapEnum<StatusPedido>("status_pedido",nameTranslator:translator);p.MapEnum<StatusAgendamento>("status_agendamento",nameTranslator:translator);p.MapEnum<StatusVerificacao>("status_verificacao",nameTranslator:translator);}).Options;
    await using var db=new AppDbContext(options);
    var admin=await db.Usuarios.AsNoTracking().Where(u=>u.Tipo==TipoUsuario.Admin&&u.Ativo).OrderBy(u=>u.Id).FirstOrDefaultAsync();
    var ordinary=await db.Usuarios.AsNoTracking().SingleAsync(u=>u.Email=="profissional01@v1.demo.chamapro.invalid");
    if(!ordinary.Ativo||ordinary.Tipo!=TipoUsuario.Profissional||!ordinary.Nome.StartsWith("[DEMO LOCAL:"))throw new Exception();
    using var http=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("CHAMAPRO_TEST_BASE_URL") ?? "http://localhost:5088/api/v1/"),Timeout=TimeSpan.FromSeconds(15)};
    var protectedPaths=new[]{"admin/relatorios","admin/usuarios?busca=DEMO&perfil=Profissional&ativo=true","admin/pedidos","admin/auditoria?limite=10","verificacoes","denuncias"};
    foreach(var route in protectedPaths) {
        using var response=await http.GetAsync(route);
        if(response.StatusCode!=System.Net.HttpStatusCode.Unauthorized)throw new Exception("AnonymousAccess");
    }
    http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new TokenService(config).GerarToken(ordinary));
    foreach(var route in protectedPaths) {
        using var response=await http.GetAsync(route);
        if(response.StatusCode!=System.Net.HttpStatusCode.Forbidden)throw new Exception("RoleAccess");
    }
    Console.WriteLine("PASS API real: 6 rotas administrativas negaram acesso anônimo (401) e profissional (403).");
    if(admin is null){Console.WriteLine("LEITURA ADMIN NÃO EXECUTADA: não há administrador ativo existente. Nenhuma conta foi criada.");Environment.ExitCode=2;return;}
    http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new TokenService(config).GerarToken(admin));
    foreach(var route in protectedPaths) { using var response=await http.GetAsync(route); Console.WriteLine($"ADMIN GET {route}: HTTP {(int)response.StatusCode}"); response.EnsureSuccessStatusCode(); }
    var report=await http.GetFromJsonAsync<JsonElement>("admin/relatorios");
    var users=await http.GetFromJsonAsync<JsonElement>("admin/usuarios?busca=DEMO&perfil=Profissional&ativo=true");
    if(users.GetArrayLength()>200)throw new Exception("UserLimit");
    var queue=await http.GetFromJsonAsync<JsonElement>("verificacoes");
    var complaints=await http.GetFromJsonAsync<JsonElement>("denuncias");
    var orders=await http.GetFromJsonAsync<JsonElement>("admin/pedidos");
    var categories=await http.GetFromJsonAsync<JsonElement>("categorias");
    Console.WriteLine($"PASS API real: administrador existente leu relatórios, usuários filtrados ({users.GetArrayLength()}), verificações ({queue.GetArrayLength()}), denúncias ({complaints.GetArrayLength()}), pedidos ({orders.GetArrayLength()}), categorias ({categories.GetArrayLength()}) e auditoria. Relatório global: {report.GetProperty("usuarios").GetInt32()} usuários.");
    Console.WriteLine("SOMENTE LEITURA/GET. Sem avaliar documentos, bloquear contas, mudar pedidos/categorias ou criar administradores. Sem tokens, credenciais ou dados pessoais exibidos.");
}catch(Exception error){Console.Error.WriteLine("Verificação local não concluída: "+error.GetType().Name+". Nenhum segredo foi exibido.");Environment.ExitCode=1;}
