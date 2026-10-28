namespace Chamapro.Api.Services;

public interface IArquivoService
{
    Task<string> SalvarImagemAsync(IFormFile arquivo, CancellationToken ct = default);
}