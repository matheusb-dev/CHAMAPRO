# CHAMAPRO — organização em camadas

Esta versão reorganiza os arquivos em camadas lógicas sem alterar os namespaces existentes nem a lógica de negócio, para reduzir o risco de quebrar as rotas e os serviços durante a reorganização.

## Estrutura

```text
CHAMAPRO-main/
├── Presentation/
│   ├── Controllers/       # Endpoints HTTP da API
│   ├── Middlewares/       # Tratamento de erros, autenticação e políticas HTTP
│   └── Security/          # Configuração auxiliar de autenticação
├── Application/
│   ├── DTOs/              # Contratos de entrada e saída da API
│   ├── Services/          # Casos de uso e serviços da aplicação
│   └── Common/            # Exceções e tipos compartilhados pela aplicação
├── Domain/
│   ├── Entities/          # Entidades do negócio
│   └── Enums/             # Estados e tipos do domínio
├── Infrastructure/
│   ├── Persistence/       # DbContext, configurações e scripts SQL
│   ├── Repositories/      # Acesso a dados
│   ├── BackgroundServices/# Rotinas em segundo plano
│   └── DependencyInjection/# Registro de dependências
├── wwwroot/               # Front-end estático servido pela API
├── Program.cs             # Composição e inicialização da aplicação
└── Chamapro.Api.csproj    # Projeto executável da API
```

## Duplicação removida

- Removida a interface `IAuthService` redundante que estava dentro de `Presentation/Controllers`. O `AuthController` já usa `Chamapro.Api.Services.IAuthService`, implementada pelo `AuthService`.
- Removidos arquivos `.gitkeep`, que eram apenas marcadores de diretório e não continham código.
- Removida a connection string fictícia de `appsettings.Development.json`, para não tentar conectar usando host e senha de exemplo.

Os arquivos `DatabaseScripts.cs`, `chamapro_schema_v2.sql`, seed e testes SQL foram mantidos: eles têm finalidades diferentes ou podem servir como referências para o banco e não foram excluídos sem confirmar que são equivalentes.

## Observação arquitetural importante

Esta é uma primeira reorganização **por pastas dentro de um único projeto .NET**, preservando os namespaces antigos para evitar uma alteração ampla de código. Ainda não há isolamento de dependências por quatro arquivos `.csproj`. Uma segunda etapa pode separar `Chamapro.Api`, `Chamapro.Application`, `Chamapro.Domain` e `Chamapro.Infrastructure` em projetos independentes, ajustando os namespaces, as referências e a injeção de dependência e compilando cada etapa.

## Configurar a conexão Supabase com segurança

Não coloque a senha real no Git. Na pasta que contém `Chamapro.Api.csproj`, execute no PowerShell:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=SEU_HOST;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require"
dotnet build
dotnet run
```

Use o host, a porta e o usuário fornecidos pela tela **Connect** do Supabase. Se usar o Session Pooler, substitua os parâmetros pelos valores exibidos pelo Supabase. Não compartilhe a senha.
