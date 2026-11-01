namespace Chamapro.Api.Services;

public interface INotificacaoService
{
    void Adicionar(int usuarioId, string tipo, string titulo, string mensagem, object? dados = null);
}
