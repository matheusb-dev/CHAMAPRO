using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Responses;

public record AdminPedidoResponse(
    int Id,
    string Titulo,
    string Categoria,
    int ClienteId,
    string ClienteNome,
    int? ProfissionalId,
    string? ProfissionalNome,
    string Cidade,
    string? Bairro,
    StatusPedido Status,
    DateTime CriadoEm);

public record AdminPedidoStatusResponse(int Id, StatusPedido Status);

public record AdminCategoriaUsoResponse(string Categoria, int Chamados);
public record AdminProfissionalAvaliacaoResponse(int ProfissionalId, string Nome, decimal Media, int Avaliacoes);
public record AdminAuditoriaResponse(long Id, int AdminId, string AdminNome, string Acao, string Entidade,
    int EntidadeId, string? EstadoAnterior, string? EstadoNovo, DateTime CriadoEm);
public record AdminRelatorioResponse(
    int Usuarios, int Clientes, int Profissionais, int ProfissionaisVerificados,
    int Chamados, int ChamadosPendentes, int ChamadosConcluidos, int ChamadosCancelados,
    IReadOnlyList<AdminCategoriaUsoResponse> CategoriasMaisSolicitadas,
    IReadOnlyList<AdminProfissionalAvaliacaoResponse> ProfissionaisMaisAvaliados);

public record ClientePerfilResponse(int Id, string Nome, string Email, string? Telefone, string? Cidade,
    string? Bairro, string? EnderecoPrincipal, string? PreferenciaContato, string? FotoUrl);

public record PainelProfissionalResponse(
    int NovosChamados, int ChamadosAceitos, int OrcamentosEnviados, int AgendamentosFuturos,
    int ServicosEmAndamento, int ServicosConcluidos, decimal MediaAvaliacao, string StatusVerificacao);

public record AgendamentoListaResponse(
    int Id, int PedidoId, string Titulo, DateTime DataHora, string StatusAgendamento, string StatusPedido,
    string ClienteNome, string ProfissionalNome);
