using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Services;

/// <summary>Conversão Entity -> DTO do módulo. As entities nunca saem na resposta.</summary>
public static class PedidoMapper
{
    public static PedidoCriadoResponse ToCriadoResponse(this Pedido p)
        => new(p.Id, p.Status, p.CriadoEm,
            p.Destinatarios.Select(d => new DestinatarioResponse(d.ProfissionalId, null, d.Situacao)).ToList());

    public static PedidoResumoResponse ToResumo(this Pedido p, int? profissionalId)
        => new(p.Id, p.Titulo, p.Categoria.Nome, p.Status, p.CriadoEm, p.Cidade, p.Bairro,
            profissionalId is null
                ? null
                : p.Destinatarios.FirstOrDefault(d => d.ProfissionalId == profissionalId)?.Situacao);

    /// <summary>ehCliente = true: visão do cliente dono. Senão, visão do profissional destinatário (só a própria situação).</summary>
    public static PedidoDetalheResponse ToDetalhe(this Pedido p, bool ehCliente, int? profissionalId)
    {
        var destinatarios = (ehCliente ? p.Destinatarios : p.Destinatarios.Where(d => d.ProfissionalId == profissionalId))
            .Select(d => new DestinatarioResponse(d.ProfissionalId, d.Profissional?.Usuario?.Nome, d.Situacao))
            .ToList();

        var aceito = p.Orcamentos.FirstOrDefault(o => o.Status == StatusOrcamento.Aceito);
        var orcamentoAceito = aceito is not null && (ehCliente || aceito.ProfissionalId == profissionalId)
            ? aceito.ToResponse()
            : null;

        var meuOrcamento = ehCliente
            ? null
            : p.Orcamentos.Where(o => o.ProfissionalId == profissionalId)
                .OrderByDescending(o => o.CriadoEm)
                .FirstOrDefault()?.ToResponse();

        // O contato do cliente não é exposto ao profissional: só o primeiro nome.
        var nomeCliente = ehCliente
            ? null
            : p.Cliente.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        var agendamento = p.Agendamento is null
            ? null
            : new AgendamentoResumoResponse(p.Agendamento.Id, p.Agendamento.DataHora, EnumTexto.Rotulo(p.Agendamento.Status));

        var avaliacao = p.Avaliacao is null
            ? null
            : new AvaliacaoResumoResponse(p.Avaliacao.Id, p.Avaliacao.Nota, p.Avaliacao.Comentario);

        return new PedidoDetalheResponse(
            p.Id, p.Titulo, p.Descricao, p.CategoriaId, p.Categoria.Nome, p.Cidade, p.Bairro,
            p.FotoUrls, p.Status, p.CriadoEm, nomeCliente, p.ProfissionalId,
            destinatarios, orcamentoAceito, meuOrcamento, agendamento, avaliacao,
            p.CancelamentoTardio, p.MotivoCancelamento);
    }

    public static OrcamentoResponse ToResponse(this Orcamento o, string? aviso = null)
        => new(o.Id, o.PedidoId, o.ProfissionalId, o.Valor, o.PrazoEstimado, o.Condicoes, o.ValidoAte, o.Status, aviso);

    public static OrcamentoComProfissionalResponse ToComProfissional(this Orcamento o, DateTime agora)
        => new(o.Id, o.PedidoId, o.Valor, o.PrazoEstimado, o.Condicoes, o.ValidoAte,
            Expirado: o.Status == StatusOrcamento.Enviado && o.ValidoAte <= agora,
            o.Status,
            new ProfissionalOrcamentoResponse(
                o.Profissional.Id,
                o.Profissional.Usuario.Nome,
                o.Profissional.MediaAvaliacao,
                o.Profissional.Usuario.IdentidadeVerificada));
}
