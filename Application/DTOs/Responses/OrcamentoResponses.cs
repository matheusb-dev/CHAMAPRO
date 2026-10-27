using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Responses;

/// <summary>Saída de POST /pedidos/{id}/orcamentos. "Aviso" só vem quando o valor foge muito da faixa do perfil (não bloqueia).</summary>
public record OrcamentoResponse(
    int Id,
    int PedidoId,
    int ProfissionalId,
    decimal Valor,
    string PrazoEstimado,
    string? Condicoes,
    DateTime ValidoAte,
    StatusOrcamento Situacao,
    string? Aviso);

public record ProfissionalOrcamentoResponse(int Id, string Nome, decimal NotaMedia, bool Verificado);

/// <summary>Item de GET /pedidos/{id}/orcamentos: o cliente compara valor, prazo, nota e selo.</summary>
public record OrcamentoComProfissionalResponse(
    int Id,
    int PedidoId,
    decimal Valor,
    string PrazoEstimado,
    string? Condicoes,
    DateTime ValidoAte,
    bool Expirado,
    StatusOrcamento Situacao,
    ProfissionalOrcamentoResponse Profissional);
