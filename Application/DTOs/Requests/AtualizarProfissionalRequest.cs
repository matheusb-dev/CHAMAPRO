using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class AtualizarProfissionalRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "O telefone deve ter 10 ou 11 dígitos, apenas números.")]
    public string Telefone { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "A URL da foto deve ter no máximo 300 caracteres.")]
    public string? FotoUrl { get; set; }

    [MaxLength(10, ErrorMessage = "Envie no máximo 10 fotos de trabalhos.")]
    public List<string> FotosTrabalhos { get; set; } = new();

    [Range(0, 80)]
    public short? ExperienciaAnos { get; set; }

    [Range(typeof(decimal), "0", "1000")]
    public decimal? DistanciaMaxKm { get; set; }

    public bool AtendeUrgencia { get; set; }
    public bool AtendeDomicilio { get; set; } = true;
    public bool AtendeEstabelecimento { get; set; }
}
