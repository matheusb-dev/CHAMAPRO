namespace Chamapro.Api.Models.Entities;

public class RecuperacaoSenha
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public DateTime? UsadoEm { get; set; }
}
