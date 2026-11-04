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
    var user=await db.Usuarios.AsNoTracking().SingleAsync(u=>u.Email=="profissional01@v1.demo.chamapro.invalid");
    if(!user.Ativo||user.Tipo!=TipoUsuario.Profissional||!user.Nome.StartsWith("[DEMO LOCAL:"))throw new Exception();
    using var http=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("CHAMAPRO_TEST_BASE_URL") ?? "http://localhost:5088/api/v1/"),Timeout=TimeSpan.FromSeconds(15)};
    http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new TokenService(config).GerarToken(user));
    var profile=await http.GetFromJsonAsync<JsonElement>("profissionais/me");
    var panel=await http.GetFromJsonAsync<JsonElement>("painel/profissional");
    var appointments=await http.GetFromJsonAsync<JsonElement>("agendamentos/me");
    var notifications=await http.GetFromJsonAsync<JsonElement>("notificacoes");
    int pages=0, details=0, quotes=0, chosen=0;
    foreach(var status in new[]{"PENDENTE","ACEITO","AGENDADO","EM_ANDAMENTO","CONCLUIDO","CANCELADO"}) {
        var result=await http.GetFromJsonAsync<JsonElement>($"pedidos?pagina=1&tamanho=10&status={status}");
        if(result.GetProperty("itens").GetArrayLength()>10)throw new Exception("Pagination");
        pages++;
        foreach(var item in result.GetProperty("itens").EnumerateArray()) {
            var id=item.GetProperty("id").GetInt32();
            var detail=await http.GetFromJsonAsync<JsonElement>($"pedidos/{id}");
            if(detail.GetProperty("id").GetInt32()!=id)throw new Exception("Identity");
            if(!detail.GetProperty("destinatarios").EnumerateArray().All(d=>d.GetProperty("profissionalId").GetInt32()==profile.GetProperty("id").GetInt32()))throw new Exception("Scope");
            if(detail.GetProperty("meuOrcamento").ValueKind!=JsonValueKind.Null)quotes++;
            if(detail.GetProperty("profissionalId").ValueKind!=JsonValueKind.Null&&detail.GetProperty("profissionalId").GetInt32()==profile.GetProperty("id").GetInt32())chosen++;
            (await http.GetAsync($"pedidos/{id}/historico")).EnsureSuccessStatusCode();
            details++;
        }
    }
    (await http.GetAsync("pedidos?pagina=2&tamanho=10&status=PENDENTE")).EnsureSuccessStatusCode();
    Console.WriteLine($"PASS API real local: {pages} filtros de status, segunda página, {details} detalhes/históricos autorizados, {quotes} últimas propostas, {chosen} pedidos escolhidos. Agenda: {appointments.GetArrayLength()}; notificações: {notifications.GetArrayLength()}; cadastro completo: {panel.GetProperty("cadastroCompleto").GetBoolean()}.");
    Console.WriteLine("SOMENTE GET. Sem alterações de pedidos, propostas, agenda, notificações, credenciais ou banco. Sem token ou dados pessoais exibidos.");
}catch(Exception error){Console.Error.WriteLine("Verificação local não concluída: "+error.GetType().Name+". Nenhum segredo foi exibido.");Environment.ExitCode=1;}
