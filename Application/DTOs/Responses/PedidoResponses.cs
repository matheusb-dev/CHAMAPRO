using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Responses;

public record DestinatarioResponse(int ProfissionalId, string? Nome, SituacaoDestinatario Situacao);

/// <summary>Saída de POST /pedidos.</summary>
public record PedidoCriadoResponse(
    int Id,
    StatusPedido Status,
    DateTime CriadoEm,
    IReadOnlyList<DestinatarioResponse> Destinatarios);

/// <summary>Item de GET /pedidos. "MinhaSituacao" só vem para o profissional.</summary>
public record PedidoResumoResponse(
    int Id,
    string Titulo,
    string Categoria,
    StatusPedido Status,
    DateTime CriadoEm,
    string Cidade,
    string? Bairro,
    SituacaoDestinatario? MinhaSituacao);

public record AgendamentoResumoResponse(int Id, DateTime DataHora, string Situacao);

public record AvaliacaoResumoResponse(int Id, int Nota, string? Comentario);

/// <summary>
/// Saída de GET /pedidos/{id} (e do aceite de orçamento / mudança de status).
/// O cliente vê todos os destinatários; o profissional vê só a própria situação (não a dos concorrentes).
/// </summary>
public record PedidoDetalheResponse(
    int Id,
    string Titulo,
    string Descricao,
    int CategoriaId,
    string Categoria,
    string Cidade,
    string? Bairro,
    IReadOnlyList<string> FotoUrls,
    StatusPedido Status,
    DateTime CriadoEm,
    string? NomeCliente,                        // só o primeiro nome, e só para o profissional
    int? ProfissionalId,
    IReadOnlyList<DestinatarioResponse> Destinatarios,
    OrcamentoResponse? OrcamentoAceito,
    OrcamentoResponse? MeuOrcamento,            // só para o profissional
    AgendamentoResumoResponse? Agendamento,
    AvaliacaoResumoResponse? Avaliacao,
    bool CancelamentoTardio,
    string? MotivoCancelamento);
