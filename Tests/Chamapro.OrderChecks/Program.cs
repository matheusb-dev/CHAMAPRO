using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.NameTranslation;

try {
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development") throw new Exception("Development obrigatório");
    var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).AddUserSecrets(typeof(AppDbContext).Assembly, true).AddEnvironmentVariables().Build();
    var cs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
    if (cs.Host is not ("localhost" or "127.0.0.1" or "::1") || cs.Port != 5432 || cs.Database != "chamapro_local") throw new Exception("Somente banco local");
    var translator = new NpgsqlNullNameTranslator();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs.ConnectionString, p => { p.MapEnum<TipoUsuario>("tipo_usuario", nameTranslator: translator); p.MapEnum<StatusPedido>("status_pedido", nameTranslator: translator); p.MapEnum<StatusAgendamento>("status_agendamento", nameTranslator: translator); p.MapEnum<StatusVerificacao>("status_verificacao", nameTranslator: translator); }).Options;
    await using var db = new AppDbContext(options);
    var client = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Email == "cliente02@v1.demo.chamapro.invalid");
    if (!client.Ativo || client.Tipo != TipoUsuario.Cliente || !client.Nome.StartsWith("[DEMO LOCAL:")) throw new Exception("Conta demo inválida");
    using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5088/api/v1/"), Timeout = TimeSpan.FromSeconds(15) };
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new TokenService(config).GerarToken(client));
    var me = await http.GetAsync("auth/me"); Console.WriteLine($"GET /auth/me: {(int)me.StatusCode}; conta demo CLIENTE ativa."); me.EnsureSuccessStatusCode();
    var categories = await http.GetFromJsonAsync<JsonElement>("categorias");
    var category = categories.EnumerateArray().Single(c => c.GetProperty("nome").GetString() == "Pedreiro").GetProperty("id").GetInt32();
    async Task Probe(string city, bool manual = false) {
        var ids = Array.Empty<int>();
        if (manual) {
            var pros = await http.GetFromJsonAsync<JsonElement>($"profissionais?categoriaId={category}&cidade=S%C3%A3o%20Paulo&tipoAtendimento=IMEDIATO&verificado=true&pagina=1&tamanho=50");
            ids = new[] { pros.GetProperty("itens")[0].GetProperty("id").GetInt32() };
        }
        var response = await http.PostAsJsonAsync("pedidos", new { categoriaId = category, titulo = "[TESTE ETAPA 3] Pedido de validação local", descricao = "Pedido fictício para validar criação e mensagens. Não realizar atendimento.", cidade = city, bairro = "Centro", tipoAtendimento = "IMEDIATO", distribuirParaCompativeis = !manual, profissionalIds = ids, fotoUrls = Array.Empty<string>() });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!response.IsSuccessStatusCode) Console.WriteLine($"POST /pedidos ({city}, {(manual ? "manual" : "auto")}): HTTP {(int)response.StatusCode}; codigo={body.GetProperty("codigo").GetString()}; detalhe={body.GetProperty("detalhe").GetString()}; titulo={body.GetProperty("titulo").GetString()}");
        else Console.WriteLine($"POST /pedidos ({(manual ? "manual" : "auto")}): HTTP {(int)response.StatusCode}; pedido demo {body.GetProperty("id").GetInt32()} criado e preservado.");
        if (city == "Campo Mourão" && ((int)response.StatusCode != 422 || body.GetProperty("codigo").GetString() != "SEM_PROFISSIONAIS_COMPATIVEIS")) throw new Exception("Resposta inesperada no cenário incompatível");
        if (city == "São Paulo") response.EnsureSuccessStatusCode();
    }
    await Probe("Campo Mourão");
    if (args.Contains("--create")) { await Probe("São Paulo"); await Probe("São Paulo", true); }
} catch (Exception error) { Console.Error.WriteLine("Verificação não concluída: " + error.GetType().Name + ". Nenhum token ou segredo foi exibido."); Environment.ExitCode = 1; }
