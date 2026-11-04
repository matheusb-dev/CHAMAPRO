using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Chamapro.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chamapro.Api.Controllers;
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;
using Chamapro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

var failures = 0;
await Check("urgent request accepts no date and no automatic recipients", async () =>
{
    var request = Request();
    Assert(Validator.TryValidateObject(request, new(request), [], true));
    var fixture = new Fixture();
    var result = await fixture.Orders.CriarAsync(request);
    Assert(result.Status == StatusPedido.Pendente && result.Destinatarios.Count == 0);
    Assert(fixture.Saved?.DataDesejada is null && fixture.Saved?.TipoAtendimento == TipoAtendimento.Imediato);
    Assert(fixture.History.Contains("Chamado urgente aberto a profissionais compatíveis."));
});
await Check("manual selection without professionals remains invalid", () =>
{
    var request = Request(); request.DistribuirParaCompativeis = false;
    Assert(!Validator.TryValidateObject(request, new(request), [], true)); return Task.CompletedTask;
});
await Check("new scheduled requests are rejected while DTO remains readable", () =>
{
    var request = Request(); request.TipoAtendimento = TipoAtendimento.Agendado; request.DataDesejada = DateTime.UtcNow.AddDays(1);
    Assert(!Validator.TryValidateObject(request, new(request), [], true)); return Task.CompletedTask;
});
await Check("blocked professional cannot send proposal", async () =>
{
    var f = new Fixture { Active = false };
    await Rejected(() => f.Quotes.EnviarAsync(1, new() { Valor = 100, PrazoEstimado = "2 horas" }));
    Assert(f.Saves == 0);
});
await Check("blocked professional cannot be selected, historical proposal remains intact", async () =>
{
    var f = new Fixture(); f.Order.Orcamentos.Add(Quote(false));
    await Rejected(() => f.Quotes.AceitarAsync(7), "PROFISSIONAL_BLOQUEADO");
    Assert(f.Saves == 0 && f.Order.Status == StatusPedido.Pendente && f.Order.Orcamentos.Count == 1);
});
await Check("client proposal list hides blocked professionals", async () =>
{
    var f = new Fixture(); f.Order.Orcamentos.Add(Quote(false)); f.Order.Orcamentos.Add(Quote(true));
    var result = await f.Quotes.ListarDoPedidoAsync(1);
    Assert(result.Count == 1 && result[0].Profissional.Telefone == "11987654321");
    Assert(f.Order.Orcamentos.Count == 2);
});
await Check("other client cannot inspect proposals or select professional", async () =>
{
    var f = new Fixture { Owner = false }; f.Order.ClienteId = 99; f.Order.Orcamentos.Add(Quote(true));
    await Rejected(() => f.Quotes.ListarDoPedidoAsync(1));
    await Rejected(() => f.Quotes.AceitarAsync(7)); Assert(f.Saves == 0);
});
await Check("later compatible professional can demonstrate interest in open order", async () =>
{
    var f = new Fixture { Compatible = true };
    await f.Orders.AceitarAsync(1);
    Assert(f.Order.Destinatarios.Single().Situacao == SituacaoDestinatario.Aceito && f.Saves > 0);
});
await Check("manual or incompatible order does not enroll stranger", async () =>
{
    var f = new Fixture { Compatible = false };
    await Rejected(() => f.Orders.AceitarAsync(1)); Assert(f.Saves == 0 && f.Order.Destinatarios.Count == 0);
});
await Check("urgent accepted order starts directly and historical scheduled order still starts", () =>
{
    Assert(PedidoTransicoes.Permitida(StatusPedido.Aceito, StatusPedido.EmAndamento, AtorTransicao.Profissional));
    Assert(PedidoTransicoes.Permitida(StatusPedido.Agendado, StatusPedido.EmAndamento, AtorTransicao.Profissional));
    Assert(!PedidoTransicoes.Permitida(StatusPedido.Aceito, StatusPedido.EmAndamento, AtorTransicao.Cliente));
    return Task.CompletedTask;
});
await Check("historical dates and appointments remain readable", () =>
{
    var f = new Fixture(); var time = new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc);
    f.Order.TipoAtendimento = TipoAtendimento.Agendado; f.Order.DataDesejada = time;
    f.Order.Agendamento = new() { Id = 5, DataHora = time, Status = StatusAgendamento.Confirmado };
    var dto = f.Order.ToDetalhe(true, null);
    Assert(dto.DataDesejada == time && dto.Agendamento?.DataHora == time); return Task.CompletedTask;
});
await Check("PostgreSQL open opportunity query translates without availability or database access", () =>
{
    using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=not_opened;Username=not_used").Options);
    var method = typeof(PedidoRepository).GetMethod("OportunidadesAbertas", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var query = (IQueryable<Pedido>)method.Invoke(new PedidoRepository(db), [3])!;
    var sql = query.ToQueryString();
    Assert(sql.Contains("Chamado urgente aberto") && !sql.Contains("disponibilidade_profissional"));
    Assert(sql.Contains("ativo") && sql.Contains("identidade_verificada")); return Task.CompletedTask;
});
await Check("all legacy scheduling writes return Gone before accessing the database", () =>
{
    foreach (var verb in new[] { "POST", "PUT", "PATCH", "DELETE" })
    {
        var http = new DefaultHttpContext(); http.Request.Method = verb;
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        var context = new ActionExecutingContext(action, [], new Dictionary<string, object?>(), new object());
        var controller = new AgendamentosController(null!, null!, null!) { ControllerContext = new ControllerContext(action) };
        controller.OnActionExecuting(context);
        Assert(context.Result is ObjectResult { StatusCode: 410 });
    }
    return Task.CompletedTask;
});
await Check("legacy availability write returns Gone without changing saved hours", async () =>
{
    var controller = new ProfissionaisController(null!);
    var result = await controller.DefinirDisponibilidade([], default);
    Assert(result is ObjectResult { StatusCode: 410 });
});
await Check("administrative professional contact requires admin authorization", () =>
{
    var method = typeof(WhatsAppController).GetMethod(nameof(WhatsAppController.GetContatoAdministrativo))!;
    Assert(method.GetCustomAttribute<AuthorizeAttribute>()?.Roles == Perfis.Admin);
    Assert(method.GetCustomAttribute<AllowAnonymousAttribute>() is null);
    return Task.CompletedTask;
});
await Check("refusal does not close an automatic order awaiting later professionals", async () =>
{
    var f = new Fixture { Open = true };
    f.Order.Destinatarios.Add(new() { ProfissionalId = 3, Situacao = SituacaoDestinatario.Pendente });
    await f.Orders.RecusarAsync(1, null);
    Assert(f.Order.Status == StatusPedido.Pendente && f.Saves > 0);
});
await Check("administrative ranking aggregate translates to PostgreSQL before DTO construction", () =>
{
    using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=not_opened;Username=not_used").Options);
    var controller = new AdminController(db, null!, null!);
    var method = typeof(AdminController).GetMethod("CategoriasMaisSolicitadas", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var query = (IQueryable<AdminCategoriaUsoResponse>)method.Invoke(controller, null)!;
    var sql = query.ToQueryString();
    Assert(sql.Contains("GROUP BY") && sql.Contains("ORDER BY") && sql.Contains("LIMIT"));
    return Task.CompletedTask;
});
await Check("read-only test instance disables every scheduled background routine", () =>
{
    var services = new ServiceCollection();
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Rotinas:Habilitadas"] = "false" }).Build();
    services.AddServicesERepositories(config);
    Assert(!services.Any(s => s.ServiceType == typeof(IHostedService)));
    return Task.CompletedTask;
});
Console.WriteLine($"Final checks: {18 - failures}/18 passed. No database was opened.");
return failures == 0 ? 0 : 1;

async Task Check(string name, Func<Task> run) { try { await run(); Console.WriteLine("PASS " + name); } catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); } }
static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
static async Task Rejected(Func<Task> run, string? code = null) { try { await run(); } catch (BusinessException e) when (code is null || e.Codigo == code) { return; } throw new Exception("Expected business rejection"); }
static PedidoCriarRequest Request() => new() { CategoriaId = 1, Titulo = "Reparo urgente", Descricao = "Preciso reparar uma torneira", Cidade = "São Paulo", DistribuirParaCompativeis = true };
static Orcamento Quote(bool active) => new() { Id = 7, PedidoId = 1, ProfissionalId = 3, Status = StatusOrcamento.Enviado, ValidoAte = DateTime.UtcNow.AddDays(1), Profissional = new() { Id = 3, Usuario = new() { Ativo = active, Nome = "Fixture", Telefone = "11987654321" } } };

public class Stub : DispatchProxy
{
    public Func<string, object?[]?, object?> Handle = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handle(method!.Name, args);
    public static T Make<T>(Func<string, object?[]?, object?> handler) where T : class { var proxy = Create<T, Stub>(); ((Stub)(object)proxy).Handle = handler; return proxy; }
}
class Fixture
{
    public bool Active = true, Owner = true, Compatible, Open;
    public Pedido? Saved; public int Saves; public List<string> History = [];
    public Pedido Order = new() { Id = 1, ClienteId = 10, Categoria = new() { Nome = "Reparos" }, Cliente = new() { Nome = "Cliente" } };
    public PedidoService Orders; public OrcamentoService Quotes;
    public Fixture()
    {
        var user = Stub.Make<ICurrentUser>((name, _) => name switch { "get_Id" => 10, "get_Role" => "CLIENTE", "get_EhCliente" => true, _ => false });
        var tx = Stub.Make<IDbContextTransaction>((name, _) => name switch { "DisposeAsync" => ValueTask.CompletedTask, "CommitAsync" => Task.CompletedTask, _ => throw new Exception(name) });
        var repo = Stub.Make<IPedidoRepository>((name, args) => name switch
        {
            "IniciarTransacaoAsync" => Task.FromResult(tx), "BloquearAsync" => Task.CompletedTask,
            "SalvarAsync" => Save(), "ContarPendentesDoClienteAsync" => Task.FromResult(0), "CategoriaExisteAsync" => Task.FromResult(true),
            "SelecionarProfissionaisCompativeisAsync" => Task.FromResult(new List<int>()),
            "FiltrarProfissionaisQueAtendemAsync" => Task.FromResult(new HashSet<int>()),
            "AdicionarAsync" => Add((Pedido)args![0]!), "AdicionarHistorico" => AddHistory((string)args![4]!),
            "ListarUsuarioIdsProfissionaisAsync" => Task.FromResult(new List<int>()),
            "ObterProfissionalIdPorUsuarioAsync" => Task.FromResult<int?>(Active ? 3 : null),
            "ObterParaAtualizacaoAsync" or "ObterDetalheAsync" => Task.FromResult<Pedido?>(Order),
            "ClienteEhDonoAsync" => Task.FromResult(Owner), "PodeReceberAbertoAsync" => Task.FromResult(Compatible),
            "DistribuicaoAbertaAsync" => Task.FromResult(Open),
            _ => throw new Exception(name)
        });
        var quotes = Stub.Make<IOrcamentoRepository>((name, _) => name switch
        {
            "ObterPedidoIdAsync" => Task.FromResult<int?>(1),
            "ListarDoPedidoAsync" => Task.FromResult<IReadOnlyList<Orcamento>>(Order.Orcamentos.ToList()),
            _ => throw new Exception(name)
        });
        var notifications = Stub.Make<INotificacaoService>((_, _) => null);
        Orders = new(repo, user, notifications); Quotes = new(repo, quotes, user, notifications);
    }
    Task Save() { Saves++; return Task.CompletedTask; }
    Task Add(Pedido order) { Saved = order; return Task.CompletedTask; }
    object? AddHistory(string value) { History.Add(value); return null; }
}
