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
    var accounts=await db.Usuarios.AsNoTracking().Where(u=>u.Email=="cliente01@v1.demo.chamapro.invalid"||u.Email=="cliente05@v1.demo.chamapro.invalid"||u.Email=="profissional01@v1.demo.chamapro.invalid"||u.Email=="profissional09@v1.demo.chamapro.invalid").ToListAsync();
    if(accounts.Count!=4||accounts.Any(u=>!u.Ativo||!u.Nome.StartsWith("[DEMO LOCAL:")))throw new Exception("DemoGuard");
    var client=accounts.Single(u=>u.Email.StartsWith("cliente01@"));
    var otherClient=accounts.Single(u=>u.Email.StartsWith("cliente05@"));
    var professional=accounts.Single(u=>u.Email.StartsWith("profissional01@"));
    var otherProfessional=accounts.Single(u=>u.Email.StartsWith("profissional09@"));
    if(client.Tipo!=TipoUsuario.Cliente||otherClient.Tipo!=TipoUsuario.Cliente||professional.Tipo!=TipoUsuario.Profissional||otherProfessional.Tipo!=TipoUsuario.Profissional)throw new Exception("RoleGuard");
    using var http=new HttpClient{BaseAddress=new Uri("http://localhost:5088/api/v1/"),Timeout=TimeSpan.FromSeconds(15)};
    void Session(Chamapro.Api.Models.Entities.Usuario? account) => http.DefaultRequestHeaders.Authorization=account is null?null:new AuthenticationHeaderValue("Bearer",new TokenService(config).GerarToken(account));
    int checks=0;
    async Task Expect(string route,int status){using var response=await http.GetAsync(route);if((int)response.StatusCode!=status)throw new Exception("UnexpectedReadStatus");checks++;}
    var adminPaths=new[]{"admin/relatorios","admin/usuarios","admin/pedidos","admin/auditoria?limite=10","verificacoes","denuncias"};
    foreach(var route in adminPaths)await Expect(route,401);
    await Expect("arquivos/privados/00000000000000000000000000000000.pdf",401);
    Session(client);
    await Expect("auth/me",200);await Expect("clientes/me",200);await Expect("profissionais/me",403);
    foreach(var route in adminPaths)await Expect(route,403);
    var list=await http.GetFromJsonAsync<JsonElement>("pedidos?pagina=1&tamanho=10");
    if(list.GetProperty("itens").GetArrayLength()==0)throw new Exception("ClientScenarioMissing");
    int details=0;int target=list.GetProperty("itens")[0].GetProperty("id").GetInt32();
    foreach(var item in list.GetProperty("itens").EnumerateArray()){
        int id=item.GetProperty("id").GetInt32();await Expect($"pedidos/{id}",200);await Expect($"pedidos/{id}/orcamentos",200);await Expect($"pedidos/{id}/historico",200);details++;
    }
    await Expect("pedidos?pagina=2&tamanho=10",200);await Expect("agendamentos/me",200);await Expect("notificacoes",200);
    var favorites=await http.GetFromJsonAsync<JsonElement>("favoritos");
    foreach(var item in favorites.EnumerateArray().Take(5))await Expect($"profissionais/{item.GetProperty("profissionalId").GetInt32()}",200);
    Session(otherClient);
    await Expect($"pedidos/{target}",404);await Expect($"pedidos/{target}/historico",404);await Expect($"pedidos/{target}/orcamentos",404);
    Session(professional);
    await Expect("auth/me",200);await Expect("profissionais/me",200);await Expect("painel/profissional",200);await Expect("verificacoes/me",200);await Expect("favoritos",403);await Expect("clientes/me",403);
    foreach(var route in adminPaths)await Expect(route,403);
    var own=await http.GetFromJsonAsync<JsonElement>("pedidos?pagina=1&tamanho=10");
    foreach(var item in own.GetProperty("itens").EnumerateArray()){int id=item.GetProperty("id").GetInt32();await Expect($"pedidos/{id}",200);await Expect($"pedidos/{id}/orcamentos",403);}
    Session(otherProfessional);
    var otherProfile=await http.GetFromJsonAsync<JsonElement>("profissionais/me");
    var excluded=await db.Pedidos.AsNoTracking().Where(p=>p.ClienteId==client.Id&&!p.Destinatarios.Any(d=>d.ProfissionalId==otherProfile.GetProperty("id").GetInt32())).Select(p=>p.Id).FirstOrDefaultAsync();
    if(excluded==0)throw new Exception("NonRecipientScenarioMissing");
    await Expect($"pedidos/{excluded}",404);await Expect($"pedidos/{excluded}/historico",404);
    Console.WriteLine($"PASS API real: {checks} verificações HTTP GET; {details} pedidos do cliente com detalhe, propostas e histórico; {favorites.GetArrayLength()} favoritos lidos. 401/403 por perfil e 404 para pedido de outro cliente/profissional não destinatário confirmados.");
    Console.WriteLine("Leitura de documentos privados: 401 sem sessão confirmado em rota de arquivo inexistente. Acesso ao arquivo real do proprietário/administrador NÃO validado.");
    Console.WriteLine("SOMENTE LEITURA. Sem login por senha, POST, PUT, PATCH, DELETE, seed, migração, criação de administrador ou alteração de registros. Tokens emitidos apenas na memória para contas fictícias, sem saída em logs.");
}catch(Exception error){Console.Error.WriteLine("Verificação local não concluída: "+error.GetType().Name+". Nenhum segredo foi exibido.");Environment.ExitCode=1;}
