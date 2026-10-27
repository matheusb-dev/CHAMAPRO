using System.Net;
using System.Net.Mail;

namespace Chamapro.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        var host = _config["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("E-mail simulado (SMTP não configurado). Para: {Para} | Assunto: {Assunto}", para, assunto);
            return;
        }

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