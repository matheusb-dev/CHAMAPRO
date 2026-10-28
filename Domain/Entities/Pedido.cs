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

    /// <summary>Profissional escolhido (preenchido quando o cliente aceita um orçamento).</summary>
    public int? ProfissionalId { get; set; }

    // Cancelamento
    public int? CanceladoPorId { get; set; }          // null = cancelado pelo sistema
    public string? MotivoCancelamento { get; set; }
    public bool CancelamentoTardio { get; set; }      // RN11: 24 h ou menos antes do serviço
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
