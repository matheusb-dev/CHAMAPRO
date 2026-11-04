using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;

namespace Chamapro.Api.Services;

public class PedidoService : IPedidoService
{
    private const int MaxPendentesPorCliente = 5;
    private static readonly TimeSpan JanelaCancelamentoTardio = TimeSpan.FromHours(24);

    private readonly IPedidoRepository _pedidos;
    private readonly ICurrentUser _user;
    private readonly INotificacaoService _notificacoes;

    public PedidoService(IPedidoRepository pedidos, ICurrentUser user, INotificacaoService notificacoes)
    {
        _pedidos = pedidos;
        _user = user;
        _notificacoes = notificacoes;
    }

    private bool EhCliente => string.Equals(_user.Role, "CLIENTE", StringComparison.OrdinalIgnoreCase);

    // ---------------------------------------------------------------- POST /pedidos

    public async Task<PedidoCriadoResponse> CriarAsync(PedidoCriarRequest request)
    {
        if (await _pedidos.ContarPendentesDoClienteAsync(_user.Id) >= MaxPendentesPorCliente)
            throw PedidoErros.Regra("LIMITE_PEDIDOS_PENDENTES",
                $"Você já tem {MaxPendentesPorCliente} pedidos pendentes. Aguarde respostas ou cancele algum antes de criar outro.");

        if (!await _pedidos.CategoriaExisteAsync(request.CategoriaId))
            throw PedidoErros.Regra("CATEGORIA_INVALIDA", "A categoria informada não existe.");

        // RN04: lista selecionada ou distribuição regional ficam limitadas a cinco perfis ativos e verificados.
        var cidade = request.Cidade.Trim();
        var profissionalIds = request.DistribuirParaCompativeis
            ? await _pedidos.SelecionarProfissionaisCompativeisAsync(request.CategoriaId, cidade, request.Bairro,
                request.TipoAtendimento, request.DataDesejada)
            : request.ProfissionalIds;

        if (profissionalIds.Count == 0 && !request.DistribuirParaCompativeis)
            throw PedidoErros.Regra("SEM_PROFISSIONAIS_COMPATIVEIS", "Não encontramos profissionais verificados que atendam a região informada.");

        var atendem = await _pedidos.FiltrarProfissionaisQueAtendemAsync(
            profissionalIds, request.CategoriaId, cidade, request.Bairro, request.TipoAtendimento, request.DataDesejada);
        var foraDoAtendimento = profissionalIds.Where(id => !atendem.Contains(id)).ToList();
        if (foraDoAtendimento.Count > 0)
            throw PedidoErros.Regra("PROFISSIONAL_NAO_ATENDE",
                $"Estes profissionais não atendem a categoria e a cidade do pedido: {string.Join(", ", foraDoAtendimento)}.");

        var pedido = new Pedido
        {
            ClienteId = _user.Id,
            CategoriaId = request.CategoriaId,
            Titulo = request.Titulo.Trim(),
            Descricao = request.Descricao.Trim(),
            Cidade = cidade,
            Bairro = string.IsNullOrWhiteSpace(request.Bairro) ? null : request.Bairro.Trim(),
            Endereco = string.IsNullOrWhiteSpace(request.Endereco) ? null : request.Endereco.Trim(),
            TipoAtendimento = TipoAtendimento.Imediato,
            DataDesejada = null,
            FotoUrls = request.FotoUrls.Select(f => f.Trim()).ToList(),
            Status = StatusPedido.Pendente,
            CriadoEm = DateTime.UtcNow,
            Destinatarios = profissionalIds
                .Select(id => new PedidoDestinatario { ProfissionalId = id, Situacao = SituacaoDestinatario.Pendente })
                .ToList()
        };

        await using var tx = await _pedidos.IniciarTransacaoAsync();
        await _pedidos.AdicionarAsync(pedido);
        await _pedidos.SalvarAsync();
        _pedidos.AdicionarHistorico(pedido.Id, _user.Id, null, pedido.Status, request.DistribuirParaCompativeis ? "Chamado urgente aberto a profissionais compatíveis." : "Chamado criado.");

        var destinatariosUsuarios = await _pedidos.ListarUsuarioIdsProfissionaisAsync(profissionalIds);
        foreach (var usuarioId in destinatariosUsuarios)
            _notificacoes.Adicionar(usuarioId, "novo_chamado", "Novo chamado na sua região",
                $"Um chamado de {pedido.CategoriaId} foi direcionado a você.", new { pedidoId = pedido.Id });
        await _pedidos.SalvarAsync();
        await tx.CommitAsync();

        return pedido.ToCriadoResponse();
    }

    // ---------------------------------------------------------------- GET /pedidos

    public async Task<PagedResponse<PedidoResumoResponse>> ListarAsync(PedidoListarQuery query)
    {
        int? clienteId = null;
        int? profissionalId = null;

        // RN13: o cliente só vê os que criou; o profissional só vê aqueles em que é destinatário.
        if (EhCliente) clienteId = _user.Id;
        else profissionalId = await ProfissionalIdAtualAsync();

        var (itens, total) = await _pedidos.ListarAsync(clienteId, profissionalId, query.ObterStatus(), query.Pagina, query.Tamanho);

        return new PagedResponse<PedidoResumoResponse>(
            itens.Select(p => p.ToResumo(profissionalId)).ToList(), query.Pagina, query.Tamanho, total);
    }

    // ---------------------------------------------------------------- GET /pedidos/{id}

    public Task<PedidoDetalheResponse> ObterAsync(int id) => MontarDetalheAsync(id);

    public async Task<IReadOnlyList<PedidoHistoricoResponse>> HistoricoAsync(int id)
    {
        var clienteId = EhCliente ? _user.Id : (int?)null;
        int? profissionalId = EhCliente ? null : await ProfissionalIdAtualAsync();
        return await _pedidos.ListarHistoricoAsync(id, clienteId, profissionalId)
            ?? throw PedidoErros.PedidoNaoEncontrado();
    }

    // ---------------------------------------------------------------- POST /pedidos/{id}/aceitar

    public async Task AceitarAsync(int id)
    {
        var profissionalId = await ProfissionalIdAtualAsync();

        await using var tx = await _pedidos.IniciarTransacaoAsync();
        var pedido = await CarregarBloqueadoAsync(id);
        var destinatario = await ObterOuAdicionarDestinatarioAsync(pedido, profissionalId);

        if (pedido.Status != StatusPedido.Pendente || destinatario.Situacao != SituacaoDestinatario.Pendente)
            throw PedidoErros.StatusInvalido("Só é possível aceitar um pedido pendente que você ainda não respondeu.");

        destinatario.Situacao = SituacaoDestinatario.Aceito;   // libera o envio de orçamento (RN05)
        _pedidos.AdicionarHistorico(pedido.Id, _user.Id, pedido.Status, pedido.Status, "Profissional demonstrou interesse no chamado.");
        _notificacoes.Adicionar(pedido.ClienteId, "chamado_aceito", "Profissional interessado",
            "Um profissional aceitou receber seu chamado.", new { pedidoId = pedido.Id });

        await _pedidos.SalvarAsync();
        await tx.CommitAsync();
    }

    // ---------------------------------------------------------------- POST /pedidos/{id}/recusar

    public async Task RecusarAsync(int id, PedidoRecusarRequest? request)
    {
        var profissionalId = await ProfissionalIdAtualAsync();

        await using var tx = await _pedidos.IniciarTransacaoAsync();
        var pedido = await CarregarBloqueadoAsync(id);
        var destinatario = await ObterOuAdicionarDestinatarioAsync(pedido, profissionalId);
        var statusAnterior = pedido.Status;

        if (pedido.Status != StatusPedido.Pendente || destinatario.Situacao != SituacaoDestinatario.Pendente)
            throw PedidoErros.StatusInvalido("Só é possível recusar um pedido pendente que você ainda não respondeu.");

        destinatario.Situacao = SituacaoDestinatario.Recusado;
        destinatario.MotivoRecusa = string.IsNullOrWhiteSpace(request?.Motivo) ? null : request!.Motivo!.Trim();

        // Pedidos manuais mantêm a regra anterior; a distribuição aberta aguarda novos profissionais.
        if (pedido.Destinatarios.All(d => d.Situacao == SituacaoDestinatario.Recusado)
            && !await _pedidos.DistribuicaoAbertaAsync(pedido.Id))
        {
            PedidoTransicoes.Garantir(pedido.Status, StatusPedido.Cancelado, AtorTransicao.Sistema, "PEDIDO_STATUS_INVALIDO");
            pedido.Status = StatusPedido.Cancelado;
            pedido.CanceladoPorId = null;
            pedido.CanceladoEm = DateTime.UtcNow;
            pedido.MotivoCancelamento = "Todos os profissionais recusaram o pedido.";
            _notificacoes.Adicionar(pedido.ClienteId, "chamado_cancelado", "Chamado sem profissionais disponíveis",
                "Todos os profissionais direcionados recusaram este chamado.", new { pedidoId = pedido.Id });
        }

        _pedidos.AdicionarHistorico(pedido.Id, _user.Id, statusAnterior, pedido.Status,
            pedido.Status == StatusPedido.Cancelado ? "Todos recusaram; o chamado foi cancelado automaticamente." : "Profissional recusou o chamado.");

        await _pedidos.SalvarAsync();
        await tx.CommitAsync();
    }

    // ---------------------------------------------------------------- POST /pedidos/{id}/cancelar

    public async Task CancelarAsync(int id, PedidoCancelarRequest request)
    {
        int? profissionalId = EhCliente ? null : await ProfissionalIdAtualAsync();

        await using var tx = await _pedidos.IniciarTransacaoAsync();
        var pedido = await CarregarBloqueadoAsync(id);

        AtorTransicao ator;
        if (EhCliente)
        {
            if (pedido.ClienteId != _user.Id) throw PedidoErros.PedidoNaoEncontrado();
            ator = AtorTransicao.Cliente;
        }
        else
        {
            if (pedido.Destinatarios.All(d => d.ProfissionalId != profissionalId))
                throw PedidoErros.PedidoNaoEncontrado();

            if (pedido.ProfissionalId != profissionalId)
                throw PedidoErros.SemPermissao(
                    "Só o profissional escolhido pode cancelar. Para um pedido pendente, use a opção de recusar.");

            ator = AtorTransicao.Profissional;
        }

        PedidoTransicoes.Garantir(pedido.Status, StatusPedido.Cancelado, ator, "PEDIDO_STATUS_INVALIDO");

        var agora = DateTime.UtcNow;
        var statusAnterior = pedido.Status;

        // RN11: cancelar um pedido AGENDADO com 24 h ou menos de antecedência é cancelamento tardio.
        if (pedido.Status == StatusPedido.Agendado
            && pedido.Agendamento is { } agendamentoAtual
            && agendamentoAtual.DataHora - agora <= JanelaCancelamentoTardio)
        {
            pedido.CancelamentoTardio = true;
        }

        pedido.Status = StatusPedido.Cancelado;
        pedido.CanceladoPorId = _user.Id;
        pedido.CanceladoEm = agora;
        pedido.MotivoCancelamento = request.Motivo.Trim();
        if (pedido.Agendamento is { } agendamento)
            agendamento.Status = StatusAgendamento.Cancelado;

        _pedidos.AdicionarHistorico(pedido.Id, _user.Id, statusAnterior, pedido.Status, "Chamado cancelado por um participante.");

        await _pedidos.SalvarAsync();
        await tx.CommitAsync();
    }

    // ---------------------------------------------------------------- PATCH /pedidos/{id}/status

    public async Task<PedidoDetalheResponse> AtualizarStatusAsync(int id, PedidoStatusRequest request)
    {
        var profissionalId = await ProfissionalIdAtualAsync();
        var novoStatus = request.Status!.Value;

        await using (var tx = await _pedidos.IniciarTransacaoAsync())
        {
            var pedido = await CarregarBloqueadoAsync(id);

            if (pedido.ProfissionalId != profissionalId)
            {
                throw pedido.Destinatarios.Any(d => d.ProfissionalId == profissionalId)
                    ? PedidoErros.SemPermissao("Só o profissional escolhido pode atualizar o status do pedido.")
                    : PedidoErros.PedidoNaoEncontrado();
            }

            // Este endpoint só marca EM_ANDAMENTO e CONCLUIDO. AGENDADO vem da confirmação do agendamento (P4).
            if (novoStatus is not (StatusPedido.EmAndamento or StatusPedido.Concluido))
                throw PedidoErros.Regra("TRANSICAO_INVALIDA", "Informe EM_ANDAMENTO ou CONCLUIDO.");

            PedidoTransicoes.Garantir(pedido.Status, novoStatus, AtorTransicao.Profissional);

            var statusAnterior = pedido.Status;
            pedido.Status = novoStatus;
            if (novoStatus == StatusPedido.Concluido && pedido.Agendamento is { } agendamento)
                agendamento.Status = StatusAgendamento.Concluido;
            if (novoStatus == StatusPedido.Concluido)
                _notificacoes.Adicionar(pedido.ClienteId, "servico_concluido", "Serviço concluído",
                    "O profissional marcou o serviço como concluído. Você já pode avaliar o atendimento.", new { pedidoId = pedido.Id });
            _pedidos.AdicionarHistorico(pedido.Id, _user.Id, statusAnterior, novoStatus,
                novoStatus == StatusPedido.Concluido ? "Serviço concluído." : "Serviço iniciado.");

            await _pedidos.SalvarAsync();
            await tx.CommitAsync();
        }

        return await MontarDetalheAsync(id);
    }

    // ---------------------------------------------------------------- apoio

    private async Task<int> ProfissionalIdAtualAsync()
    {
        var id = await _pedidos.ObterProfissionalIdPorUsuarioAsync(_user.Id);
        return id ?? throw PedidoErros.SemPermissao("Este usuário não possui perfil de profissional.");
    }

    /// <summary>Trava a linha do pedido (FOR UPDATE) e carrega o agregado rastreado. Chamar dentro de transação.</summary>
    private async Task<Pedido> CarregarBloqueadoAsync(int id)
    {
        await _pedidos.BloquearAsync(id);
        return await _pedidos.ObterParaAtualizacaoAsync(id) ?? throw PedidoErros.PedidoNaoEncontrado();
    }

    private async Task<PedidoDestinatario> ObterOuAdicionarDestinatarioAsync(Pedido pedido, int profissionalId)
    {
        var destinatario = pedido.Destinatarios.FirstOrDefault(d => d.ProfissionalId == profissionalId);
        if (destinatario is not null) return destinatario;
        if (!await _pedidos.PodeReceberAbertoAsync(pedido.Id, profissionalId))
            throw PedidoErros.PedidoNaoEncontrado();
        destinatario = new PedidoDestinatario { PedidoId = pedido.Id, ProfissionalId = profissionalId };
        pedido.Destinatarios.Add(destinatario);
        return destinatario;
    }

    /// <summary>Lê o pedido e aplica a visibilidade (RN13): outro cliente ou profissional que não recebeu o pedido recebe 404.</summary>
    private async Task<PedidoDetalheResponse> MontarDetalheAsync(int id)
    {
        var pedido = await _pedidos.ObterDetalheAsync(id) ?? throw PedidoErros.PedidoNaoEncontrado();

        if (EhCliente)
        {
            if (pedido.ClienteId != _user.Id) throw PedidoErros.PedidoNaoEncontrado();
            return pedido.ToDetalhe(ehCliente: true, profissionalId: null);
        }

        var profissionalId = await ProfissionalIdAtualAsync();
        if (pedido.Destinatarios.All(d => d.ProfissionalId != profissionalId))
        {
            if (!await _pedidos.PodeReceberAbertoAsync(id, profissionalId)) throw PedidoErros.PedidoNaoEncontrado();
            pedido.Destinatarios.Add(new PedidoDestinatario { ProfissionalId = profissionalId });
        }

        return pedido.ToDetalhe(ehCliente: false, profissionalId);
    }
}
