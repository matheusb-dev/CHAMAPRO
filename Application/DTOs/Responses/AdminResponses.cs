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
public record AdminRelatorioResponse(
    int Usuarios, int Clientes, int Profissionais, int ProfissionaisVerificados,
    int Chamados, int ChamadosPendentes, int ChamadosConcluidos, int ChamadosCancelados,
    IReadOnlyList<AdminCategoriaUsoResponse> CategoriasMaisSolicitadas,
    IReadOnlyList<AdminProfissionalAvaliacaoResponse> ProfissionaisMaisAvaliados);

public record ClientePerfilResponse(int Id, string Nome, string Email, string? Telefone, string? Cidade,
    string? Bairro, string? EnderecoPrincipal, string? PreferenciaContato, string? FotoUrl);
