using Chamapro.Api.Common;

namespace Chamapro.Api.Services;

public class ArquivoService : IArquivoService
{
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024;
    public const long TamanhoMaximoDocumentoBytes = 10 * 1024 * 1024;
    public const string RotaPublica = "/uploads";
    public const string RotaPrivada = "/api/v1/arquivos/privados";

    private readonly string _pastaPublica;
    private readonly string _pastaPrivada;

    public ArquivoService(IConfiguration configuracao, IWebHostEnvironment ambiente)
    {
        var nomePasta = configuracao["Arquivos:PastaPublica"] ?? "uploads";
        _pastaPublica = Path.Combine(ambiente.ContentRootPath, nomePasta);
        var nomePastaPrivada = configuracao["Arquivos:PastaPrivada"] ?? "private-uploads";
        _pastaPrivada = Path.Combine(ambiente.ContentRootPath, nomePastaPrivada);
    }

    public async Task<string> SalvarImagemAsync(IFormFile arquivo, CancellationToken ct = default)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw BusinessException.Invalido("ARQUIVO_INVALIDO", "Envie um arquivo no campo 'arquivo'.");

        if (arquivo.Length > TamanhoMaximoBytes)
            throw BusinessException.Invalido("ARQUIVO_MUITO_GRANDE", "O arquivo deve ter no máximo 5 MB.");

        var extensao = await DescobrirExtensaoPeloConteudoAsync(arquivo, ct);
        if (extensao is null)
            throw BusinessException.Invalido("ARQUIVO_TIPO_INVALIDO", "Somente imagens JPG, PNG ou WEBP são aceitas.");

        Directory.CreateDirectory(_pastaPublica);

        var nomeFinal = $"{Guid.NewGuid():N}{extensao}";
        var caminhoFisico = Path.Combine(_pastaPublica, nomeFinal);

        await using (var origem = arquivo.OpenReadStream())
        await using (var destino = new FileStream(caminhoFisico, FileMode.CreateNew, FileAccess.Write))
        {
            await origem.CopyToAsync(destino, ct);
        }

        return $"{RotaPublica}/{nomeFinal}";
    }

    public async Task<string> SalvarDocumentoPrivadoAsync(IFormFile arquivo, CancellationToken ct = default)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw BusinessException.Invalido("ARQUIVO_INVALIDO", "Envie um documento no campo 'arquivo'.");
        if (arquivo.Length > TamanhoMaximoDocumentoBytes)
            throw BusinessException.Invalido("ARQUIVO_MUITO_GRANDE", "O documento deve ter no máximo 10 MB.");

        var extensao = await DescobrirExtensaoDocumentoAsync(arquivo, ct);
        if (extensao is null)
            throw BusinessException.Invalido("DOCUMENTO_TIPO_INVALIDO", "Envie um documento PDF, JPG, PNG ou WEBP válido.");

        Directory.CreateDirectory(_pastaPrivada);
        var nomeFinal = $"{Guid.NewGuid():N}{extensao}";
        var caminhoFisico = Path.Combine(_pastaPrivada, nomeFinal);
        await using (var origem = arquivo.OpenReadStream())
        await using (var destino = new FileStream(caminhoFisico, FileMode.CreateNew, FileAccess.Write))
            await origem.CopyToAsync(destino, ct);

        return $"{RotaPrivada}/{nomeFinal}";
    }

    public string? ObterCaminhoDocumentoPrivado(string nomeArquivo)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(nomeArquivo, @"^[a-f0-9]{32}\.(pdf|jpg|png|webp)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return null;
        var caminho = Path.GetFullPath(Path.Combine(_pastaPrivada, nomeArquivo));
        var pasta = Path.GetFullPath(_pastaPrivada) + Path.DirectorySeparatorChar;
        return caminho.StartsWith(pasta, StringComparison.OrdinalIgnoreCase) ? caminho : null;
    }

    private static async Task<string?> DescobrirExtensaoPeloConteudoAsync(IFormFile arquivo, CancellationToken ct)
    {
        var cabecalho = new byte[12];
        int lidos = 0;

        await using (var fluxo = arquivo.OpenReadStream())
        {
            while (lidos < cabecalho.Length)
            {
                var n = await fluxo.ReadAsync(cabecalho.AsMemory(lidos, cabecalho.Length - lidos), ct);
                if (n == 0) break;
                lidos += n;
            }
        }

        if (lidos < 12) return null;

        if (cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return ".jpg";

        if (cabecalho[0] == 0x89 && cabecalho[1] == 0x50 && cabecalho[2] == 0x4E && cabecalho[3] == 0x47 &&
            cabecalho[4] == 0x0D && cabecalho[5] == 0x0A && cabecalho[6] == 0x1A && cabecalho[7] == 0x0A)
            return ".png";

        if (cabecalho[0] == 'R' && cabecalho[1] == 'I' && cabecalho[2] == 'F' && cabecalho[3] == 'F' &&
            cabecalho[8] == 'W' && cabecalho[9] == 'E' && cabecalho[10] == 'B' && cabecalho[11] == 'P')
            return ".webp";

        return null;
    }

    private static async Task<string?> DescobrirExtensaoDocumentoAsync(IFormFile arquivo, CancellationToken ct)
    {
        var buffer = new byte[12];
        var lidos = 0;
        await using (var fluxo = arquivo.OpenReadStream())
        {
            while (lidos < buffer.Length)
            {
                var n = await fluxo.ReadAsync(buffer.AsMemory(lidos, buffer.Length - lidos), ct);
                if (n == 0) break;
                lidos += n;
            }
        }

        if (lidos >= 5 && buffer.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return ".pdf";
        var extensaoImagem = await DescobrirExtensaoPeloConteudoAsync(arquivo, ct);
        return extensaoImagem;
    }
}
