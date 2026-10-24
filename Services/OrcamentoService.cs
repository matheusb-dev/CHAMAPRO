using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;

namespace Chamapro.Api.Services;

public class OrcamentoService : IOrcamentoService
{
    private const int ValidadePadraoEmDias = 7;

    // O aviso de "valor fora da faixa" não bloqueia o envio. Os limites abaixo são uma escolha nossa:
    // a definição da API só diz "muito fora da faixa".
    private const decimal FatorAbaixoDoMinimo = 0.5m;
    private const decimal FatorAcimaDoMaximo = 2m;

    private readonly IPedidoRepository _pedidos;
    private readonly IOrcamentoRepository _orcamentos;
    private readonly ICurrentUser _user;

    public OrcamentoService(IPedidoRepository pedidos, IOrcamentoRepository orcamentos, ICurrentUser user)
    {
        _pedidos = pedidos;
        _orcamentos = orcamentos;
        _user = user;
    }

    // ---------------------------------------------------------------- POST /pedidos/{id}/orcamentos

    public async Task<OrcamentoResponse> EnviarAsync(int pedidoId, OrcamentoCriarRequest request)
    {
        var profissionalId = await _pedidos.ObterProfissionalIdPorUsuarioAsync(_user.Id)
            ?? throw PedidoErros.SemPermissao("Este usuário não possui perfil de profissional.");

        await using var tx = await _pedidos.IniciarTransacaoAsync();

        await _pedidos.BloquearAsync(pedidoId);
        var pedido = await _pedidos.ObterParaAtualizacaoAsync(pedidoId) ?? throw PedidoErros.PedidoNaoEncontrado();

        var destinatario = pedido.Destinatarios.FirstOrDefault(d => d.ProfissionalId == profissionalId)
            ?? throw PedidoErros.PedidoNaoEncontrado();

        if (pedido.Status != StatusPedido.Pendente)
            throw PedidoErros.StatusInvalido("Só é possível enviar orçamento para um pedido pendente.");

        // RN05: o profissional só orça depois de aceitar o pedido.
        if (destinatario.Situacao != SituacaoDestinatario.Aceito)
            throw PedidoErros.Regra("PEDIDO_NAO_ACEITO", "Aceite o pedido antes de enviar o orçamento.");

        var agora = DateTime.UtcNow;
        var validoAte = request.ValidoAteUtc ?? agora.AddDays(ValidadePadraoEmDias);

        // Um orçamento ativo por profissional por pedido: reenviar substitui o anterior se ainda estiver ENVIADO.
        var orcamento = pedido.Orcamentos.FirstOrDefault(o =>
            o.ProfissionalId == profissionalId && o.Status == StatusOrcamento.Enviado);

        if (orcamento is null)
        {
            orcamento = new Orcamento
            {
                PedidoId = pedido.Id,
                ProfissionalId = profissionalId,
                Status = StatusOrcamento.Enviado,
                CriadoEm = agora
            };
            await _orcamentos.AdicionarAsync(orcamento);
        }

        orcamento.Valor = request.Valor;
        orcamento.PrazoEstimado = request.PrazoEstimado.Trim();
        orcamento.Condicoes = string.IsNullOrWhiteSpace(request.Condicoes) ? null : request.Condicoes.Trim();
        orcamento.ValidoAte = validoAte;

        var (min, max) = await _orcamentos.ObterFaixaPrecoAsync(profissionalId);

        await _pedidos.SalvarAsync();
        await tx.CommitAsync();

        return orcamento.ToResponse(GerarAviso(request.Valor, min, max));
    }

    // ---------------------------------------------------------------- GET /pedidos/{id}/orcamentos

    public async Task<IReadOnlyList<OrcamentoComProfissionalResponse>> ListarDoPedidoAsync(int pedidoId)
    {
        // RN13: só o cliente dono do pedido vê os orçamentos.
        if (!await _pedidos.ClienteEhDonoAsync(pedidoId, _user.Id))
            throw PedidoErros.PedidoNaoEncontrado();

        var agora = DateTime.UtcNow;
        var orcamentos = await _orcamentos.ListarDoPedidoAsync(pedidoId);

        return orcamentos.Select(o => o.ToComProfissional(agora)).ToList();
    }

    // ---------------------------------------------------------------- POST /orcamentos/{id}/aceitar

    public async Task<PedidoDetalheResponse> AceitarAsync(int orcamentoId)
    {
        var pedidoId = await _orcamentos.ObterPedidoIdAsync(orcamentoId) ?? throw PedidoErros.OrcamentoNaoEncontrado();

        // Tudo na mesma transação: orçamento escolhido, demais orçamentos, destinatários e pedido.
        await using (var tx = await _pedidos.IniciarTransacaoAsync())
        {
            await _pedidos.BloquearAsync(pedidoId);
            var pedido = await _pedidos.ObterParaAtualizacaoAsync(pedidoId) ?? throw PedidoErros.OrcamentoNaoEncontrado();

            if (pedido.ClienteId != _user.Id) throw PedidoErros.OrcamentoNaoEncontrado();   // RN13

            var escolhido = pedido.Orcamentos.FirstOrDefault(o => o.Id == orcamentoId)
                ?? throw PedidoErros.OrcamentoNaoEncontrado();

            PedidoTransicoes.Garantir(pedido.Status, StatusPedido.Aceito, AtorTransicao.Cliente, "PEDIDO_STATUS_INVALIDO");

            if (escolhido.Status != StatusOrcamento.Enviado)
                throw PedidoErros.Regra("ORCAMENTO_STATUS_INVALIDO", "Este orçamento não está mais disponível para aceite.");

            if (escolhido.ValidoAte <= DateTime.UtcNow)
                throw PedidoErros.Regra("ORCAMENTO_EXPIRADO", "Este orçamento passou da validade.");

            // RN06: um único orçamento por pedido; escolher um encerra a disputa dos demais.
            escolhido.Status = StatusOrcamento.Aceito;
            foreach (var outro in pedido.Orcamentos.Where(o => o.Id != escolhido.Id && o.Status == StatusOrcamento.Enviado))
                outro.Status = StatusOrcamento.NaoEscolhido;

            // Quem já recusou o pedido continua RECUSADO; os demais viram NAO_ESCOLHIDO.
            foreach (var destinatario in pedido.Destinatarios)
            {
                if (destinatario.ProfissionalId == escolhido.ProfissionalId)
                    destinatario.Situacao = SituacaoDestinatario.Aceito;
                else if (destinatario.Situacao != SituacaoDestinatario.Recusado)
                    destinatario.Situacao = SituacaoDestinatario.NaoEscolhido;
            }

            pedido.Status = StatusPedido.Aceito;
            pedido.ProfissionalId = escolhido.ProfissionalId;

            await _pedidos.SalvarAsync();
            await tx.CommitAsync();
        }

        var detalhe = await _pedidos.ObterDetalheAsync(pedidoId) ?? throw PedidoErros.PedidoNaoEncontrado();
        return detalhe.ToDetalhe(ehCliente: true, profissionalId: null);
    }

    // ---------------------------------------------------------------- POST /orcamentos/{id}/recusar

    public async Task RecusarAsync(int orcamentoId)
    {
        var pedidoId = await _orcamentos.ObterPedidoIdAsync(orcamentoId) ?? throw PedidoErros.OrcamentoNaoEncontrado();

        await using var tx = await _pedidos.IniciarTransacaoAsync();

        await _pedidos.BloquearAsync(pedidoId);
        var pedido = await _pedidos.ObterParaAtualizacaoAsync(pedidoId) ?? throw PedidoErros.OrcamentoNaoEncontrado();

        if (pedido.ClienteId != _user.Id) throw PedidoErros.OrcamentoNaoEncontrado();

        var orcamento = pedido.Orcamentos.FirstOrDefault(o => o.Id == orcamentoId)
            ?? throw PedidoErros.OrcamentoNaoEncontrado();

        if (pedido.Status != StatusPedido.Pendente)
            throw PedidoErros.StatusInvalido("Só é possível recusar orçamentos de um pedido pendente.");

        if (orcamento.Status != StatusOrcamento.Enviado)
            throw PedidoErros.Regra("ORCAMENTO_STATUS_INVALIDO", "Este orçamento não está mais disponível para recusa.");

        orcamento.Status = StatusOrcamento.Recusado;   // o pedido continua PENDENTE para outros orçamentos

        await _pedidos.SalvarAsync();
        await tx.CommitAsync();
    }

    // ---------------------------------------------------------------- apoio

    private static string? GerarAviso(decimal valor, decimal? min, decimal? max)
    {
        if (min.HasValue && min.Value > 0 && valor < min.Value * FatorAbaixoDoMinimo)
            return "O valor está muito abaixo da faixa de preço do seu perfil.";

        if (max.HasValue && max.Value > 0 && valor > max.Value * FatorAcimaDoMaximo)
            return "O valor está muito acima da faixa de preço do seu perfil.";

        return null;
    }
}
