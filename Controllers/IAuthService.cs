using Chamapro.Api.DTOs.Requests;

namespace Chamapro.Api.Controllers
{
    internal interface IAuthService
    {
        Task LoginAsync(LoginRequest request, CancellationToken ct);
    }
}