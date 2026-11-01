using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int CategoriaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string? Bairro { get; set; }
    public List<string> FotoUrls { get; set; } = new();
    public StatusPedido Status { get; set; } = StatusPedido.Pendente;

    public int? ProfissionalId { get; set; }

    public int? CanceladoPorId { get; set; }
    public string? MotivoCancelamento { get; set; }
    public bool CancelamentoTardio { get; set; }
    public DateTime? CanceladoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Usuario Cliente { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
    public Profissional? Profissional { get; set; }
    public ICollection<PedidoDestinatario> Destinatarios { get; set; } = new List<PedidoDestinatario>();
    public ICollection<Orcamento> Orcamentos { get; set; } = new List<Orcamento>();
    public Agendamento? Agendamento { get; set; }
    public Avaliacao? Avaliacao { get; set; }
}