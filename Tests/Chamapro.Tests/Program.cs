using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Models.Time;
using Chamapro.Api.Services;

var testes = new (string Nome, Action Executar)[]
{
    ("transições de pedido respeitam ator e estado", TestarTransicoes),
    ("transição inválida retorna erro de negócio", TestarTransicaoInvalida),
    ("denúncia rejeita campos fora dos limites", TestarValidacaoDenuncia),
    ("denúncia aceita campos válidos", TestarDenunciaValida),
    ("serviço profissional aceita preço adiado e rejeita faixa parcial", TestarFaixaPrecoServico),
    ("agenda converte UTC para horário de Brasília", TestarFusoHorario)
};

var falhas = 0;
foreach (var (nome, executar) in testes)
{
    try
    {
        executar();
        Console.WriteLine($"PASS {nome}");
    }
    catch (Exception erro)
    {
        falhas++;
        Console.Error.WriteLine($"FAIL {nome}: {erro.Message}");
    }
}

Console.WriteLine($"{testes.Length - falhas}/{testes.Length} testes passaram.");
return falhas == 0 ? 0 : 1;

static void TestarTransicoes()
{
    var casos = new[]
    {
        (StatusPedido.Pendente, StatusPedido.Aceito, AtorTransicao.Cliente, true),
        (StatusPedido.Pendente, StatusPedido.Aceito, AtorTransicao.Profissional, false),
        (StatusPedido.Aceito, StatusPedido.Agendado, AtorTransicao.Profissional, true),
        (StatusPedido.Agendado, StatusPedido.EmAndamento, AtorTransicao.Profissional, true),
        (StatusPedido.EmAndamento, StatusPedido.Concluido, AtorTransicao.Profissional, true),
        (StatusPedido.Concluido, StatusPedido.Cancelado, AtorTransicao.Cliente, false),
        (StatusPedido.Cancelado, StatusPedido.Pendente, AtorTransicao.Sistema, false)
    };

    foreach (var (de, para, ator, esperado) in casos)
        Exigir(PedidoTransicoes.Permitida(de, para, ator) == esperado,
            $"transição {de} → {para} por {ator} deveria ser {esperado}");
}

static void TestarTransicaoInvalida()
{
    var erro = Capturar<BusinessException>(() =>
        PedidoTransicoes.Garantir(StatusPedido.Pendente, StatusPedido.Concluido, AtorTransicao.Cliente));
    Exigir(erro.Codigo == "TRANSICAO_INVALIDA", "código de erro diferente do contrato");
}

static void TestarValidacaoDenuncia()
{
    var invalida = new DenunciaCriarRequest
    {
        DenunciadoId = 0,
        TipoEntidade = "",
        EntidadeId = 0,
        Motivo = "x",
        Descricao = "curta"
    };
    Exigir(!Validar(invalida), "a validação aceitou campos inválidos");
}

static void TestarDenunciaValida()
{
    var valida = new DenunciaCriarRequest
    {
        DenunciadoId = 12,
        TipoEntidade = "PROFISSIONAL",
        EntidadeId = 4,
        Motivo = "Conduta inadequada",
        Descricao = "Descrição suficiente para relatar a situação observada."
    };
    Exigir(Validar(valida), "a validação rejeitou uma denúncia válida");
}

static void TestarFusoHorario()
{
    var local = HorarioBrasil.ParaLocal(new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));
    Exigir(local.Hour == 12 && local.DayOfWeek == DayOfWeek.Thursday,
        "15:00 UTC deveria corresponder a 12:00 na quinta-feira em Brasília");
}

static void TestarFaixaPrecoServico()
{
    var rascunho = new ServicoProfissionalRequest { CategoriaId = 1 };
    Exigir(Validar(rascunho), "a categoria sem preço deveria ser salva como rascunho");

    var faixaParcial = new ServicoProfissionalRequest { CategoriaId = 1, FaixaPrecoMin = 100 };
    Exigir(!Validar(faixaParcial), "a validação aceitou uma faixa com apenas um preço");

    var faixaCompleta = new ServicoProfissionalRequest
    {
        CategoriaId = 1,
        FaixaPrecoMin = 100,
        FaixaPrecoMax = 250
    };
    Exigir(Validar(faixaCompleta), "a validação rejeitou uma faixa de preço completa");
}

static bool Validar(object modelo)
    => Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true);

static void Exigir(bool condicao, string mensagem)
{
    if (!condicao) throw new InvalidOperationException(mensagem);
}

static T Capturar<T>(Action acao) where T : Exception
{
    try { acao(); }
    catch (T erro) { return erro; }
    throw new InvalidOperationException($"exceção {typeof(T).Name} não foi lançada");
}
