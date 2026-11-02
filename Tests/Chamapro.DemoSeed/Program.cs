using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;
using Chamapro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.NameTranslation;

try { await Run(args); }
catch (Exception error) { Console.Error.WriteLine(error is SeedGuardException ? "Seed bloqueado: " + error.Message : $"Seed interrompido ({error.GetType().Name}; SQLSTATE {(error.GetBaseException() as PostgresException)?.SqlState ?? "n/a"}). Nenhuma credencial foi exibida."); Console.Error.WriteLine("Causa técnica: " + error.GetBaseException().GetType().Name + (error.GetBaseException() is System.Net.Sockets.SocketException socket ? " / " + socket.SocketErrorCode : "")); Environment.ExitCode = 1; }

static async Task Run(string[] args)
{
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development") throw new SeedGuardException("Development obrigatório.");
    if (!File.Exists("Chamapro.Api.csproj")) throw new SeedGuardException("Execute na raiz do CHAMAPRO.");
    var apply = args.Contains("--apply");
    var configurePassword = args.Contains("--set-password");
    var batchIndex = Array.IndexOf(args, "--batch");
    var batch = batchIndex < 0 ? "v1" : args[batchIndex + 1];
    if (!System.Text.RegularExpressions.Regex.IsMatch(batch, "^[a-z0-9]{1,16}$")) throw new SeedGuardException("Lote inválido.");
    var marker = $"[DEMO LOCAL:{batch}]";
    var domain = $"@{batch}.demo.chamapro.invalid";
    var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets(typeof(AppDbContext).Assembly, optional: true).AddEnvironmentVariables().Build();
    var cs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? throw new SeedGuardException("Conexão ausente."));
    if (cs.Host is not ("localhost" or "127.0.0.1" or "::1") || cs.Port != 5432 || cs.Database != "chamapro_local" || cs.Multiplexing)
        throw new SeedGuardException("Destino não autorizado. Somente chamapro_local no PostgreSQL loopback:5432.");
    cs.Timeout = 5; cs.CommandTimeout = 30; cs.IncludeErrorDetail = false;
    var translator = new NpgsqlNullNameTranslator();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs.ConnectionString, pg => {
        pg.MapEnum<TipoUsuario>("tipo_usuario", nameTranslator: translator);
        pg.MapEnum<StatusPedido>("status_pedido", nameTranslator: translator);
        pg.MapEnum<StatusAgendamento>("status_agendamento", nameTranslator: translator);
        pg.MapEnum<StatusVerificacao>("status_verificacao", nameTranslator: translator);
    }).Options;
    await using var db = new AppDbContext(options);
    await db.Database.OpenConnectionAsync();
    await using (var command = db.Database.GetDbConnection().CreateCommand()) {
        command.CommandText = "SELECT current_database(), inet_server_addr()::text, inet_client_addr()::text, inet_server_port(), pg_is_in_recovery()";
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        if (reader.GetString(0) != "chamapro_local" || !IPAddress.IsLoopback(IPAddress.Parse(reader.GetString(1).Split('/')[0])) || !IPAddress.IsLoopback(IPAddress.Parse(reader.GetString(2).Split('/')[0])) || reader.GetInt32(3) != 5432 || reader.GetBoolean(4))
            throw new SeedGuardException("A identidade do servidor local não foi confirmada.");
    }
    Console.WriteLine("Destino confirmado: PostgreSQL loopback:5432 / chamapro_local; Development. Sem alterações de esquema.");
    await Counts(db, "ANTES");
    var categories = await db.Categorias.AsNoTracking().OrderBy(c => c.Id).Select(c => new { c.Nome, c.Ativa }).ToListAsync();
    Console.WriteLine("Categorias existentes: " + JsonSerializer.Serialize(categories));
    if (!apply && !configurePassword) { Console.WriteLine("INSPEÇÃO SOMENTE LEITURA. Use --apply para inserir um lote."); return; }
    await using var tx = await db.Database.BeginTransactionAsync();
    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734018221)");
    var existing = await db.Usuarios.Where(u => u.Email.EndsWith(domain)).ToListAsync();
    foreach (var user in existing) if (!user.Nome.StartsWith(marker) || user.Tipo == TipoUsuario.Admin) throw new SeedGuardException("Colisão com conta não pertencente ao lote.");
    if (existing.Count > 0) {
        if (existing.Count != 17 || await db.Pedidos.CountAsync(p => p.Titulo.StartsWith(marker)) != 20) throw new SeedGuardException("Lote parcial: preservado sem reparação automática. Use outro lote.");
        Console.WriteLine("Lote já existente: nenhum cenário foi sobrescrito.");
    } else if (apply) {
        await Seed(db, marker, domain);
    } else throw new SeedGuardException("Crie o lote antes de configurar as senhas.");
    if (configurePassword) {
        var password = Environment.GetEnvironmentVariable("CHAMAPRO_DEMO_PASSWORD");
        if (password is null || password.Length < 12 || password.Length > 128) throw new SeedGuardException("Informe uma senha interativa de 12 a 128 caracteres.");
        var users = await db.Usuarios.Where(u => u.Email.EndsWith(domain)).ToListAsync();
        foreach (var user in users) user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(password);
        await db.SaveChangesAsync(); Console.WriteLine("Senha atualizada somente nas contas fictícias deste lote; valor não exibido nem armazenado em texto puro.");
    }
    if (apply) await Validate(db, marker, domain);
    await tx.CommitAsync();
    await Counts(db, "DEPOIS");
    Console.WriteLine($"Lote {batch}: 5 clientes, 12 profissionais, 20 pedidos. Contas: cliente01..05{domain}; profissional01..12{domain}.");
    Console.WriteLine("Sem administrador novo. Para acessar, execute o procedimento interativo -ConfigurePassword do launcher.");
}

static string Key(string text) => string.Concat(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Replace(" ", "").Replace("-", "");

static async Task Seed(AppDbContext db, string marker, string domain)
{
    var names = new[] { "Pedreiro", "Eletricista", "Encanador", "Pintor", "Diarista", "Jardineiro", "Montador de móveis", "Marceneiro", "Serralheiro", "Técnico em ar-condicionado", "Técnico em informática", "Instalador de câmeras", "Gesseiro", "Azulejista", "Chaveiro", "Vidraceiro", "Limpador de piscinas", "Reparos domésticos", "Limpeza de terrenos" };
    var catalog = await db.Categorias.ToListAsync();
    var selectedCategories = new List<Categoria>();
    var categoryService = new CategoriaService(new CategoriaRepository(db));
    foreach (var name in names) {
        var keys = new HashSet<string> { Key(name) };
        if (name == "Diarista") foreach (var alias in new[] { "Faxineira", "Faxineiro", "Limpeza residencial", "Diarista / Faxina" }) keys.Add(Key(alias));
        var matches = catalog.Where(c => keys.Contains(Key(c.Nome))).ToList();
        if (matches.Count > 1) throw new SeedGuardException("Categorias equivalentes ambíguas; ajuste o catálogo explicitamente antes de inserir.");
        var category = matches.SingleOrDefault();
        if (category is null) {
            var response = await categoryService.CriarAsync(new CategoriaRequest { Nome = name, Icone = null });
            category = await db.Categorias.SingleAsync(c => c.Id == response.Id); catalog.Add(category);
        }
        if (!category.Ativa) throw new SeedGuardException("Categoria existente inativa; não será reativada pelo seed.");
        selectedCategories.Add(category);
    }
    var users = new List<Usuario>();
    // No reusable plaintext password: interactive configuration can replace these random hashes later.
    for (var i = 0; i < 17; i++) {
        var client = i < 5; var number = client ? i + 1 : i - 4;
        var user = new Usuario { Nome = $"{marker} {(client ? "Cliente" : "Profissional")} {number:00}", Email = $"{(client ? "cliente" : "profissional")}{number:00}{domain}", Tipo = client ? TipoUsuario.Cliente : TipoUsuario.Profissional, SenhaHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(24))), Telefone = "00000000000", Cidade = client || number <= 8 ? "São Paulo" : number <= 10 ? "Campinas" : "Santos", Bairro = "Centro", EnderecoPrincipal = "Endereço fictício de demonstração — não realizar visitas", WhatsAppAtivo = false, CriadoEm = DateTime.UtcNow.AddDays(-2) };
        db.Usuarios.Add(user); users.Add(user);
    }
    await db.SaveChangesAsync();
    var pros = new List<Profissional>();
    for (var i = 0; i < 12; i++) {
        var user = users[i + 5];
        var pro = new Profissional { UsuarioId = user.Id, DescricaoPerfil = marker + " Perfil fictício para testes de contratação local.", DistanciaMaxKm = 25, AtendeDomicilio = true, AtendeUrgencia = true, ExperienciaAnos = 3, PrecoMin = 100, PrecoMax = 500 };
        user.IdentidadeVerificada = i < 8;
        foreach (var category in new[] { selectedCategories[i % 4], selectedCategories[4 + i] }.DistinctBy(c => c.Id)) pro.Categorias.Add(new ProfissionalCategoria { CategoriaId = category.Id, FaixaPrecoMin = 100, FaixaPrecoMax = 500 });
        pro.Areas.Add(new AreaAtendimento { Cidade = user.Cidade!, Bairro = null });
        if (i == 0) pro.Areas.Add(new AreaAtendimento { Cidade = "Campinas", Bairro = "Centro" });
        for (short day = 0; day < 7; day++) pro.Disponibilidade.Add(new DisponibilidadeProfissional { DiaSemana = day, HoraInicio = new TimeOnly(0, 0), HoraFim = new TimeOnly(23, 59, 59), AtendeImediato = true, AtendeAgendado = true });
        db.Profissionais.Add(pro); pros.Add(pro);
    }
    await db.SaveChangesAsync();
    for (var i = 0; i < 11; i++) db.Verificacoes.Add(new VerificacaoProfissional { ProfissionalId = pros[i].Id, TipoDocumento = "SIMULAÇÃO LOCAL — sem documento pessoal", UrlDocumento = "about:blank#chamapro-demo-sem-documento", Status = i < 8 ? StatusVerificacao.Aprovado : i == 8 ? StatusVerificacao.Pendente : i == 9 ? StatusVerificacao.Rejeitado : StatusVerificacao.DocumentacaoPendente, Observacao = marker + " Aprovação fictícia exclusiva para exercitar as regras locais; não comprova identidade." });
    var now = DateTime.UtcNow;
    for (var i = 0; i < 20; i++) {
        var group = i % 4; var client = users[group]; var first = pros[group]; var second = pros[group + 4];
        var final = i < 8 ? StatusPedido.Pendente : i < 11 ? StatusPedido.Aceito : i < 14 ? StatusPedido.Agendado : i < 16 ? StatusPedido.EmAndamento : i < 19 ? StatusPedido.Concluido : StatusPedido.Cancelado;
        var scheduledTime = final == StatusPedido.Concluido ? now.AddHours(-2).AddMinutes(i) : final == StatusPedido.EmAndamento ? now.AddHours(-1).AddMinutes(i) : now.AddDays(i + 1);
        var order = new Pedido { ClienteId = client.Id, CategoriaId = selectedCategories[group].Id, Titulo = $"{marker} {i + 1:00} — {names[group]} em residência fictícia", Descricao = marker + " Cenário de demonstração. Não corresponde a um serviço real.", Cidade = "São Paulo", Bairro = i % 2 == 0 ? "Centro" : "Vila Mariana", TipoAtendimento = TipoAtendimento.Agendado, DataDesejada = scheduledTime, Status = StatusPedido.Pendente, CriadoEm = now.AddHours(-4).AddMinutes(i) };
        var recipients = new[] { first, second }.Select(pro => new PedidoDestinatario { ProfissionalId = pro.Id, Situacao = SituacaoDestinatario.Pendente }).ToList();
        order.Destinatarios = recipients;
        db.Pedidos.Add(order); await db.SaveChangesAsync();
        var eventTime = order.CriadoEm;
        void History(StatusPedido? before, StatusPedido after, int actor, string detail) => db.HistoricosPedidos.Add(new HistoricoPedido { PedidoId = order.Id, AtorId = actor, StatusAnterior = before?.ToString(), StatusNovo = after.ToString(), Detalhe = marker + " " + detail, CriadoEm = eventTime = eventTime.AddSeconds(1) });
        void Transition(StatusPedido next, AtorTransicao actor, int actorId) { var previous = order.Status; PedidoTransicoes.Garantir(previous, next, actor); order.Status = next; History(previous, next, actorId, "Transição de demonstração."); }
        History(null, StatusPedido.Pendente, client.Id, "Pedido criado.");
        var quotes = final == StatusPedido.Cancelado || i < 2 ? 0 : i < 4 ? 1 : 2;
        for (var q = 0; q < quotes; q++) {
            var pro = q == 0 ? first : second; recipients[q].Situacao = SituacaoDestinatario.Aceito;
            History(order.Status, order.Status, pro.UsuarioId, "Profissional demonstrou interesse e enviou proposta.");
            order.Orcamentos.Add(new Orcamento { ProfissionalId = pro.Id, Valor = 150 + i * 10 + q * 30, PrazoEstimado = "1 dia", Condicoes = marker + " Apenas teste; sem cobrança real.", Status = final == StatusPedido.Pendente ? (q == 1 && i == 7 ? StatusOrcamento.Recusado : StatusOrcamento.Enviado) : q == 0 ? StatusOrcamento.Aceito : StatusOrcamento.NaoEscolhido, CriadoEm = eventTime, ValidoAte = now.AddDays(7) });
        }
        if (final is not (StatusPedido.Pendente or StatusPedido.Cancelado)) {
            Transition(StatusPedido.Aceito, AtorTransicao.Cliente, client.Id); order.ProfissionalId = first.Id;
            recipients[1].Situacao = SituacaoDestinatario.NaoEscolhido;
            if (final != StatusPedido.Aceito) {
                Transition(StatusPedido.Agendado, AtorTransicao.Profissional, first.UsuarioId);
                order.Agendamento = new Agendamento { ProfissionalId = first.Id, DataHora = scheduledTime, Status = final == StatusPedido.Concluido ? StatusAgendamento.Concluido : StatusAgendamento.Confirmado, LembreteEnviadoEm = now };
                if (final is StatusPedido.EmAndamento or StatusPedido.Concluido) { eventTime = scheduledTime.AddSeconds(-1); Transition(StatusPedido.EmAndamento, AtorTransicao.Profissional, first.UsuarioId); }
                if (final == StatusPedido.Concluido) {
                    eventTime = now.AddMinutes(-40).AddSeconds(-1);
                    Transition(StatusPedido.Concluido, AtorTransicao.Profissional, first.UsuarioId);
                    if (i < 18) order.Avaliacao = new Avaliacao { AvaliadorId = client.Id, AvaliadoId = first.UsuarioId, Nota = i == 16 ? 5 : 4, Comentario = marker + " Avaliação fictícia de demonstração.", CriadoEm = now.AddMinutes(-30) };
                }
            }
        } else if (final == StatusPedido.Cancelado) { Transition(StatusPedido.Cancelado, AtorTransicao.Cliente, client.Id); order.CanceladoPorId = client.Id; order.CanceladoEm = now.AddMinutes(-10); order.MotivoCancelamento = marker + " Cancelamento de demonstração."; }
        db.Notificacoes.Add(new Notificacao { UsuarioId = client.Id, Tipo = "demo_local", Titulo = marker + " Pedido de teste", Mensagem = "Abra Meus pedidos para acompanhar este cenário fictício.", Dados = JsonSerializer.Serialize(new { pedidoId = order.Id }) });
        foreach (var pro in new[] { first, second }) db.Notificacoes.Add(new Notificacao { UsuarioId = pro.UsuarioId, Tipo = "novo_chamado", Titulo = marker + " Oportunidade fictícia", Mensagem = "Pedido de demonstração local.", Dados = JsonSerializer.Serialize(new { pedidoId = order.Id }) });
        await db.SaveChangesAsync();
    }
    for (var i = 0; i < 4; i++) db.Favoritos.Add(new Favorito { ClienteId = users[i].Id, ProfissionalId = pros[i].Id });
    foreach (var pro in pros) { var ratings = await db.Avaliacoes.Where(a => a.AvaliadoId == pro.UsuarioId).Select(a => a.Nota).ToListAsync(); pro.MediaAvaliacao = ratings.Count == 0 ? 0 : (decimal)ratings.Average(); }
    await db.SaveChangesAsync();
}

static async Task Validate(AppDbContext db, string marker, string domain)
{
    var orders = await db.Pedidos.Where(p => p.Titulo.StartsWith(marker)).Include(p => p.Destinatarios).Include(p => p.Orcamentos).Include(p => p.Agendamento).Include(p => p.Avaliacao).ToListAsync();
    if (orders.Count != 20 || await db.Usuarios.CountAsync(u => u.Email.EndsWith(domain)) != 17) throw new SeedGuardException("Contagem do lote inválida.");
    var repository = new PedidoRepository(db);
    foreach (var order in orders) {
        var ids = order.Destinatarios.Select(d => d.ProfissionalId).ToList();
        var compatible = await repository.FiltrarProfissionaisQueAtendemAsync(ids, order.CategoriaId, order.Cidade, order.Bairro, order.TipoAtendimento, order.DataDesejada);
        if (compatible.Count != ids.Count) throw new SeedGuardException("Há destinatário incompatível com categoria, região, verificação ou disponibilidade. Os dados foram preservados.");
        if (order.Agendamento != null && order.Agendamento.ProfissionalId != order.ProfissionalId) throw new SeedGuardException("Agendamento inconsistente.");
        if (order.Avaliacao != null && (order.Avaliacao.AvaliadorId != order.ClienteId || !await db.Profissionais.AnyAsync(p => p.Id == order.ProfissionalId && p.UsuarioId == order.Avaliacao.AvaliadoId))) throw new SeedGuardException("Participantes da avaliação inconsistentes.");
        if (order.Destinatarios.Count is < 1 or > 5 || order.Orcamentos.Any(q => order.Destinatarios.All(d => d.ProfissionalId != q.ProfissionalId))) throw new SeedGuardException("Destinatários inválidos.");
        if (order.ProfissionalId != null && order.Orcamentos.Count(q => q.Status == StatusOrcamento.Aceito && q.ProfissionalId == order.ProfissionalId) != 1) throw new SeedGuardException("Contratação inconsistente.");
        if (order.Avaliacao != null && order.Status != StatusPedido.Concluido) throw new SeedGuardException("Avaliação antes da conclusão.");
    }
    var emptyUser = await db.Usuarios.SingleAsync(u => u.Email == "cliente05" + domain);
    if (await db.Pedidos.AnyAsync(p => p.ClienteId == emptyUser.Id) || await db.Favoritos.AnyAsync(f => f.ClienteId == emptyUser.Id) || await db.Notificacoes.AnyAsync(n => n.UsuarioId == emptyUser.Id)) throw new SeedGuardException("O cliente destinado ao cenário vazio já foi utilizado; crie outro lote para repetir esse cenário.");
    Console.WriteLine("PASS integridade do lote: contas, 20 pedidos, compatibilidade real de destinatários, contratação, agendamentos, avaliações e cliente vazio.");
    Console.WriteLine("Situações: " + JsonSerializer.Serialize(orders.GroupBy(p => p.Status).ToDictionary(g => g.Key.ToString(), g => g.Count())));
}

static async Task Counts(AppDbContext db, string label)
{
    var counts = new SortedDictionary<string, long>();
    foreach (var entity in db.Model.GetEntityTypes().OrderBy(e => e.GetTableName())) {
        var table = entity.GetTableName()!; var schema = entity.GetSchema() ?? "public";
        static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT count(*) FROM {Quote(schema)}.{Quote(table)}";
        counts[table] = Convert.ToInt64(await command.ExecuteScalarAsync());
    }
    Console.WriteLine(label + " " + JsonSerializer.Serialize(counts));
}

sealed class SeedGuardException(string message) : Exception(message);




