using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.NameTranslation;

var root = Directory.GetCurrentDirectory();
var output = Path.Combine(root, "Tests/.runtime/readonly");
Directory.CreateDirectory(output);
const string address = "http://127.0.0.1:5096/";
Process? server = null;
Task<string>? serverLog = null, serverErrors = null;
try
{
    var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", true).AddUserSecrets(typeof(AppDbContext).Assembly, true)
        .AddEnvironmentVariables().Build();
    var cs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
    if (cs.Host is not ("localhost" or "127.0.0.1" or "::1") || cs.Port != 5432 || cs.Database != "chamapro_local")
        throw new Exception("DatabaseGuard");
    cs.Options = "-c default_transaction_read_only=on";
    await using var connection = new NpgsqlConnection(cs.ConnectionString);
    await connection.OpenAsync();
    await using (var guard = new NpgsqlCommand("SHOW default_transaction_read_only", connection))
        if ((string?)await guard.ExecuteScalarAsync() != "on") throw new Exception("ReadOnlyGuard");
    var tables = new List<string>();
    await using (var command = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename", connection))
    await using (var reader = await command.ExecuteReaderAsync())
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
    async Task<Dictionary<string, string>> Fingerprint()
    {
        var result = new Dictionary<string, string>();
        foreach (var table in tables)
        {
            var quoted = '"' + table.Replace("\"", "\"\"") + '"';
            await using var command = new NpgsqlCommand($"SELECT count(*)::text || ':' || md5(coalesce(string_agg(hash, ',' ORDER BY hash), '')) FROM (SELECT md5(row_to_json(t)::text) AS hash FROM public.{quoted} t) x", connection);
            result[table] = (string)(await command.ExecuteScalarAsync())!;
        }
        return result;
    }
    var before = await Fingerprint();
    var translator = new NpgsqlNullNameTranslator();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs.ConnectionString, p => {
        p.MapEnum<TipoUsuario>("tipo_usuario", nameTranslator: translator); p.MapEnum<StatusPedido>("status_pedido", nameTranslator: translator);
        p.MapEnum<StatusAgendamento>("status_agendamento", nameTranslator: translator); p.MapEnum<StatusVerificacao>("status_verificacao", nameTranslator: translator);
    }).Options;
    await using var db = new AppDbContext(options);
    var accounts = await db.Usuarios.AsNoTracking().Where(u => u.Ativo).OrderBy(u => u.Id).ToListAsync();
    var professional = await db.Profissionais.AsNoTracking().Where(p => p.Usuario.Ativo).Select(p => (int?)p.Id).FirstOrDefaultAsync();
    var exampleOrder = await db.Pedidos.AsNoTracking().Where(p => p.Orcamentos.Any(o => o.Profissional.Usuario.Ativo)).OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.ClienteId, ProUsuario = p.Destinatarios.OrderBy(d => d.ProfissionalId).Select(d => (int?)d.Profissional.UsuarioId).FirstOrDefault() }).FirstOrDefaultAsync();
    var sessions = new Dictionary<string, object>();
    foreach (var type in new[] { TipoUsuario.Cliente, TipoUsuario.Profissional, TipoUsuario.Admin })
    {
        var user = accounts.FirstOrDefault(u => u.Tipo == type && (type != TipoUsuario.Cliente || exampleOrder == null || u.Id == exampleOrder.ClienteId))
            ?? accounts.FirstOrDefault(u => u.Tipo == type);
        if (user != null) sessions[type.ToString().ToUpperInvariant()] = new { token = new TokenService(config).GerarToken(user), usuario = new { id = user.Id, nome = user.Nome, perfil = type.ToString().ToUpperInvariant(), ativo = true } };
    }
    var unverified = accounts.FirstOrDefault(u => u.Tipo == TipoUsuario.Profissional && !u.IdentidadeVerificada);
    var blocked = await db.Usuarios.AsNoTracking().Where(u => u.Tipo == TipoUsuario.Profissional && !u.Ativo).OrderBy(u => u.Id).FirstOrDefaultAsync();
    foreach(var item in new[] { ("UNVERIFIED", unverified), ("BLOCKED", blocked) })
        if(item.Item2 is { } user) sessions[item.Item1] = new { token = new TokenService(config).GerarToken(user), usuario = new { id = user.Id, perfil = "PROFISSIONAL", ativo = user.Ativo } };
    var listener = new TcpListener(IPAddress.Loopback, 5096); listener.Start(); listener.Stop();
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    start.ArgumentList.Add(Path.Combine(root, "Tests/.artifacts/bin/Chamapro.Api/debug/Chamapro.Api.dll"));
    start.ArgumentList.Add("--urls"); start.ArgumentList.Add(address.TrimEnd('/'));
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ConnectionStrings__DefaultConnection"] = cs.ConnectionString;
    start.Environment["Bootstrap__AdminEmail"] = ""; start.Environment["Bootstrap__AdminPassword"] = "";
    start.Environment["Arquivos__PastaPublica"] = Path.Combine(output, "public"); start.Environment["Arquivos__PastaPrivada"] = Path.Combine(output, "private");
    start.Environment["Logging__LogLevel__Default"] = "None";
    start.Environment["Logging__LogLevel__Chamapro.Api.Middlewares.ErrorHandlingMiddleware"] = "Error";
    start.Environment["Rotinas__Habilitadas"] = "false";
    server = Process.Start(start)!;
    serverLog = server.StandardOutput.ReadToEndAsync(); serverErrors = server.StandardError.ReadToEndAsync();
    using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(3) };
    bool ready = false;
    for (var i = 0; i < 50; i++) { if (server.HasExited) break; try { if ((await http.GetAsync("api/v1/categorias")).IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { } await Task.Delay(200); }
    if (!ready) throw new Exception("ApiNotReady");
    var ratedProfiles = 0; var unratedProfiles = 0;
    var professionals = await db.Profissionais.AsNoTracking().Where(p => p.Usuario.Ativo).Select(p => new { p.Id, p.UsuarioId }).ToListAsync();
    foreach(var pro in professionals)
    {
        var ratings = await db.Avaliacoes.AsNoTracking().Where(a => a.AvaliadoId == pro.UsuarioId).Select(a => a.Nota).ToListAsync();
        var publicJson = await http.GetStringAsync("api/v1/profissionais/" + pro.Id);
        using var profile = JsonDocument.Parse(publicJson);
        var expected = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 1);
        if(profile.RootElement.GetProperty("notaMedia").GetDecimal() != expected || profile.RootElement.GetProperty("totalAvaliacoes").GetInt32() != ratings.Count) throw new Exception("RealRatingMismatch");
        if(ratings.Count == 0) unratedProfiles++; else ratedProfiles++;
    }
    Console.WriteLine($"RATINGS real reads: {ratedProfiles} perfis com média real; {unratedProfiles} perfis sem avaliações; sem criar avaliações.");
    var results = new Dictionary<string, int>();
    foreach (var check in new[] { "DatabaseChecks", "AdminChecks", "IntegrationChecks", "OrderChecks", "ProfileChecks", "WorkChecks" })
    {
        var run = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        run.ArgumentList.Add(Path.Combine(root, $"Tests/.artifacts/bin/Chamapro.{check}/debug/Chamapro.{check}.dll"));
        run.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        run.Environment["ConnectionStrings__DefaultConnection"] = cs.ConnectionString;
        run.Environment["CHAMAPRO_TEST_BASE_URL"] = address + "api/v1/";
        using var process = Process.Start(run)!; var log = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(); var text = await log + await error;
        await File.WriteAllTextAsync(Path.Combine(output, check + ".txt"), text);
        results[check] = process.ExitCode; Console.WriteLine($"READ {check}: exit={process.ExitCode}");
    }
    if (!args.Contains("--api-only")) {
    var node = new ProcessStartInfo("node") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
    node.ArgumentList.Add("Tests/browser/homologacao.cjs");
    node.Environment["NODE_PATH"] = "C:/Users/otavi/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules";
    using (var process = Process.Start(node)!)
    {
        var log = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { baseUrl = address, accounts = sessions, professionalId = professional, orderId = exampleOrder?.Id, evidence = output })); process.StandardInput.Close();
        await process.WaitForExitAsync(); await File.WriteAllTextAsync(Path.Combine(output, "browser.txt"), await log + await error);
        results["Browser"] = process.ExitCode; Console.WriteLine("BROWSER exit=" + process.ExitCode);
    }
    }
    var after = await Fingerprint();
    var unchanged = before.Count == after.Count && before.All(p => after.TryGetValue(p.Key, out var hash) && hash == p.Value);
    await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new { database = cs.Database, databaseReadOnly = true, sourceTablesUnchanged = unchanged, tableCount = tables.Count, ratedProfiles, unratedProfiles, results }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"READONLY: {tables.Count} tabelas; conteúdo preservado={unchanged}; nenhum segredo exibido.");
    Environment.ExitCode = unchanged && results.Values.All(v => v == 0) ? 0 : 1;
}
catch (Exception error) { Console.Error.WriteLine("READONLY FAIL: " + error.GetType().Name + (error is PostgresException pg ? " SQLSTATE=" + pg.SqlState : "")); Environment.ExitCode = 1; }
finally { if (server is { HasExited: false }) { server.Kill(true); await server.WaitForExitAsync(); } if(serverLog != null && serverErrors != null) await File.WriteAllTextAsync(Path.Combine(output,"api-errors.txt"),await serverLog + await serverErrors); }
