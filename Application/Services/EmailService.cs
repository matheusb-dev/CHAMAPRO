using System.Net;
using System.Net.Mail;

namespace Chamapro.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        var host = _config["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("SMTP não configurado; o e-mail não foi enviado.");

        var porta = int.TryParse(_config["Email:Porta"], out var p) ? p : 587;
        var remetente = _config["Email:Remetente"] ?? "nao-responda@chamapro.local";

        using var mensagem = new MailMessage(remetente, para, assunto, corpoHtml) { IsBodyHtml = true };
        using var cliente = new SmtpClient(host, porta)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_config["Email:Usuario"], _config["Email:Senha"])
        };

        await cliente.SendMailAsync(mensagem, ct);
    }
}
