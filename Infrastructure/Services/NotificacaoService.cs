using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Services;

namespace Chamapro.Api.Services;

public sealed class NotificacaoService : INotificacaoService
{
    private readonly AppDbContext _db;

    public NotificacaoService(AppDbContext db) => _db = db;

    public void Adicionar(int usuarioId, string tipo, string titulo, string mensagem, object? dados = null)
    {
        _db.Notificacoes.Add(new Notificacao
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Titulo = titulo,
            Mensagem = mensagem,
            Dados = JsonSerializer.Serialize(dados ?? new { }),
            CriadaEm = DateTime.UtcNow
        });
    }
}
