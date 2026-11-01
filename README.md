# CHAMAPRO

Marketplace web para conectar clientes a profissionais de serviços. O repositório contém uma API ASP.NET Core, páginas HTML/CSS/JavaScript e scripts PostgreSQL para Supabase.

## Situação

O sistema está em desenvolvimento na branch `feat/telas-05-a-08-versao-vitoria`. O projeto Supabase `chamapro-dev` foi criado na região São Paulo e recebeu as migrações v2, v3 e v4. A base está vazia de contas e chamados de exemplo; contém apenas a taxonomia inicial de categorias. O projeto Supabase antigo `chamapro` não foi alterado.

O build e os testes locais de regras e validação passam. Ainda falta executar a aplicação contra o banco com uma credencial configurada localmente e validar os fluxos completos de ponta a ponta.

Não considere o sistema pronto para produção antes de configurar e validar o banco, SMTP, origens CORS, HTTPS e armazenamento persistente de arquivos.

## Funcionalidades implementadas

- Cadastro de cliente e profissional, login JWT, papéis e bloqueio de conta.
- Perfil de cliente e profissional, categorias, regiões, preços e disponibilidade semanal.
- Criação de chamados imediatos ou agendados, direcionamento manual ou automático a até cinco profissionais compatíveis.
- Fila de chamados, interesse/recusa, orçamentos, contratação, agenda, remarcação, cancelamento e conclusão.
- Contato via WhatsApp após contratação, quando habilitado pelo destinatário.
- Verificação profissional com aprovação administrativa e documentos privados protegidos por dono.
- Avaliações de serviços concluídos, favoritos, notificações persistidas e denúncias.
- Painel administrativo para usuários, chamados, categorias, verificações, denúncias e relatórios.

Notificações dentro do sistema funcionam com dados no PostgreSQL. E-mail e notificações push não estão configurados. Recuperação de senha e confirmação de e-mail também não estão implementadas.

## Arquitetura

- `Domain/`: entidades e enums.
- `Application/`: DTOs, serviços e regras do fluxo.
- `Infrastructure/`: Entity Framework Core, PostgreSQL, repositórios, rotinas e scripts SQL.
- `Presentation/`: controllers, middlewares e endpoints HTTP.
- `wwwroot/`: páginas, estilos e JavaScript da aplicação.
- `Tests/Chamapro.Tests/`: testes locais autocontidos, executados com `dotnet run`.

API versionada em `/api/v1`. Swagger fica em `/swagger` no ambiente `Development`.

## Requisitos

- .NET SDK 10.
- PostgreSQL 15+ ou projeto Supabase.
- Credenciais do banco e uma chave JWT com no mínimo 32 bytes.

## Configurar e executar

Na raiz do repositório, configure segredos locais com User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<usuario>;Password=<senha>"
dotnet user-secrets set "Jwt:Key" "<segredo aleatorio com pelo menos 32 bytes>"
dotnet user-secrets set "Bootstrap:AdminEmail" "<e-mail do primeiro administrador>"
dotnet user-secrets set "Bootstrap:AdminPassword" "<senha inicial aleatoria com pelo menos 16 caracteres>"
```

Ou defina as variáveis `ConnectionStrings__DefaultConnection` e `Jwt__Key`. Não coloque valores reais em arquivos versionados.

```powershell
dotnet restore Chamapro.Api.slnx
dotnet build Chamapro.Api.slnx
dotnet run --project Chamapro.Api.csproj
```

O servidor precisa de uma conexão PostgreSQL válida para as operações da API. As origens permitidas pelo CORS devem ser ajustadas em configuração para o ambiente de execução.

## Banco de dados

Para uma instalação nova, execute na ordem:

1. `Infrastructure/Persistence/Database/chamapro_schema_v2.sql`
2. `Infrastructure/Persistence/Database/chamapro_schema_v3.sql`
3. `Infrastructure/Persistence/Database/chamapro_schema_v4.sql`
4. `Infrastructure/Persistence/Database/chamapro_schema_v5.sql`

A migração v3 é incremental sobre o esquema v2. Ela cria as tabelas e colunas adicionais, índices e RLS. A API acessa o PostgreSQL pelo servidor; não exponha as tabelas diretamente pela chave pública do Supabase. Os documentos de verificação ficam fora da pasta estática, com registro de propriedade no banco. Imagens de perfil e portfólio são públicas.

A v3 insere somente a taxonomia inicial de serviços, sem usuários ou chamados fictícios. A v4 adiciona índices para as chaves estrangeiras dos pedidos. A v5 revoga privilégios de tabela e sequência de `PUBLIC`, `anon` e `authenticated`, inclusive para futuras tabelas criadas pelo papel `postgres`. RLS continua habilitado em todas as tabelas públicas e não há policies de acesso direto pelo PostgREST: o modelo atual acessa a base pela API privada. O administrador inicial é criado uma única vez pela API quando `Bootstrap:AdminEmail` e `Bootstrap:AdminPassword` estão configurados; a senha é armazenada com BCrypt e não é alterada em inicializações seguintes. Remova as duas configurações de bootstrap depois da criação.

Os arquivos são atualmente salvos no disco local da aplicação (`uploads` e `uploads-privado`, configuráveis). Para múltiplas instâncias ou publicação com disco efêmero, configure armazenamento durável e privado antes da produção.

`chamapro_seed_teste.sql` é apenas para bases de teste descartáveis. `chamapro_testes_sql.sql` contém consultas de diagnóstico; revise os resultados manualmente.

## Testes

```powershell
dotnet build Chamapro.Api.slnx --no-restore
dotnet run --project Tests/Chamapro.Tests/Chamapro.Tests.csproj
```

Os testes locais cobrem transições de chamados e validação de denúncias. Ainda não há testes de integração com PostgreSQL, autenticação HTTP ou fluxo completo de cliente a avaliação. Execute-os após conectar a nova base de desenvolvimento.

## Endpoints principais

| Área | Rotas |
|---|---|
| Autenticação | `POST /auth/cadastro/cliente`, `POST /auth/cadastro/profissional`, `POST /auth/login`, `GET /auth/me` |
| Clientes | `GET/PUT /clientes/me` |
| Profissionais | `GET /profissionais`, `GET /profissionais/{id}`, `GET /profissionais/{id}/avaliacoes`, `GET/PUT /profissionais/me`, `PUT /profissionais/me/servicos`, `/areas`, `/disponibilidade` |
| Chamados | `GET/POST /pedidos`, `GET /pedidos/{id}`, `POST /pedidos/{id}/aceitar`, `/recusar`, `/cancelar`, `PATCH /pedidos/{id}/status` |
| Orçamentos | `POST /pedidos/{id}/orcamentos`, `GET /pedidos/{id}/orcamentos`, `POST /orcamentos/{id}/aceitar`, `/recusar` |
| Agendamentos | `POST /agendamentos`, `GET /agendamentos/{pedidoId}`, `PUT /agendamentos/{id}`, `PATCH /agendamentos/{id}/status` |
| Avaliações e marketplace | `GET /avaliacoes/pendentes`, `POST /avaliacoes`, favoritos, notificações e denúncias em `/favoritos`, `/notificacoes` e `/denuncias` |
| Administração | `/admin/usuarios`, `/admin/pedidos`, `/admin/relatorios`, `/verificacoes` e `/denuncias` |

As rotas protegidas usam `Authorization: Bearer <JWT>`. A API verifica papel, conta ativa e propriedade/participação antes de permitir alterações.

## Segurança e credenciais

Os arquivos `appsettings*.json` não devem conter senhas ou chaves. Uma credencial de banco já apareceu no histórico público do repositório; trate-a como comprometida e rotacione-a no projeto antigo. A configuração atual do código foi limpa, mas o histórico remoto não pode ser apagado por esta branch.
