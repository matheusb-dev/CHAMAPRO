using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services
{
    public class PagedResponse<T>
    {
        private List<PedidoResumoResponse> pedidoResumoResponses;
        private int pagina;
        private int tamanho;
        private int total;

        public PagedResponse(List<PedidoResumoResponse> pedidoResumoResponses, int pagina, int tamanho, int total)
        {
            this.pedidoResumoResponses = pedidoResumoResponses;
            this.pagina = pagina;
            this.tamanho = tamanho;
            this.total = total;
        }
    }
}