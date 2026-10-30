# Esqueleto da API — Pessoa 2

Projeto `Chamapro.Api` (net10.0). Requer SDK 10 e Visual Studio 2026.

## Como rodar

1. Configure a string de conexão (nunca no repositório):
   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Port=5432;Database=postgres;Username=...;Password=..."
   ```
2. `dotnet run` (ambiente Development). O Swagger UI fica em `/swagger`.
3. Teste o login de desenvolvimento:
   ```
   GET /api/v1/dev/whoami
   X-Dev-User-Id: 2
   X-Dev-Role: PROFISSIONAL
   ```

## Como cada módulo se encaixa

- **Controller**: herde de `ApiControllerBase` e use `[Route(ApiRotas.Prefixo + "/pedidos")]`.
  Rotas públicas levam `[AllowAnonymous]`; rotas de admin levam `[Authorize(Roles = Perfis.Admin)]`.
- **Service**: classe `PedidoService` em `Services/` com interface `IPedidoService`.
- **Repository**: classe `PedidoRepository` em `Repositories/` com interface `IPedidoRepository`.
- Services e Repositories são **registrados automaticamente** (nome da classe termina em `Service`/`Repository`
  e existe a interface `I` + nome). Ninguém edita o `Program.cs`.
- Um Service **nunca** chama o Service de outro módulo.
- Para saber quem é o usuário logado, injete `ICurrentUser` (`Id`, `Perfil`, `EhCliente`, `EhProfissional`, `EhAdmin`).
- Para erros de negócio, lance `BusinessException` (`Regra` = 422, `Conflito` = 409, `NaoEncontrado` = 404,
  `Proibido` = 403, `Invalido` = 400). O middleware monta a resposta padrão.
- Para listas, receba `[FromQuery] PaginacaoQuery` e termine a consulta com `.ParaPaginadoAsync(paginacao)`.

## Pendências de outras pessoas

- **P1**: `AppDbContext` (DbSets e mapeamento), Entities, enums, JWT, `UsuarioAtivoMiddleware`, rate limiting do login.
- Na integração: remover `DevAuthenticationHandler`, `DevController` e o bloco de Development em `Program.cs`.
