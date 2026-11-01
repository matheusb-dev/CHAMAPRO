namespace Chamapro.Api.Services;

public interface IArquivoService
{
    Task<string> SalvarImagemAsync(IFormFile arquivo, CancellationToken ct = default);
    Task<string> SalvarDocumentoPrivadoAsync(IFormFile arquivo, CancellationToken ct = default);
    string? ObterCaminhoDocumentoPrivado(string nomeArquivo);
}
