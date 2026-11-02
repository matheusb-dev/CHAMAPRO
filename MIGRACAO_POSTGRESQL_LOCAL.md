# Migração local executada

## Correção posterior: inicialização pelo PowerShell

Foi reproduzida a falha com `dotnet run --no-launch-profile --environment Development --urls http://localhost:5088`. No SDK 10.0.400 instalado, a opção `dotnet run --environment` recebe uma variável `NOME=VALOR`; ela não seleciona diretamente o ambiente ASP.NET Core. Sem o perfil de inicialização, o comando informado não ativava Development e os User Secrets não eram carregados.

O projeto já possuía `UserSecretsId=chamapro-api-dev`. A chave `Jwt:Key` forte e a conexão local estavam presentes nos User Secrets corretos. Ambas foram preservadas, sem regenerar a chave nem substituir credenciais.

Foi adicionado `run-local.ps1`, que seleciona explicitamente `ASPNETCORE_ENVIRONMENT=Development`, resolve o projeto a partir da própria pasta do script e usa a porta 5088 por padrão. A mensagem de erro de JWT em `Program.cs` agora informa o ambiente atual e orienta a configuração sem revelar valores secretos.

Comando validado:

```powershell
dotnet run --project Chamapro.Api.csproj --no-launch-profile --environment ASPNETCORE_ENVIRONMENT=Development -- --urls http://localhost:5088
```

Resultado: inicialização em Development, cadastro HTTP 201, login HTTP 200 e `/api/v1/auth/me` com token HTTP 200. Consulta direta confirmou `current_database() = chamapro_local`, endereço local `::1`, porta 5432 e a presença da conta recém-cadastrada nesse banco. Nenhuma senha ou token foi impresso.

**Atualização do estado do banco:** para atender ao teste de login solicitado posteriormente, uma conta de cliente identificada como teste foi criada em `chamapro_local` e preservada. Suas credenciais aleatórias estão somente nos User Secrets, nas entradas `LocalSmokeTest:Email` e `LocalSmokeTest:Password`. Portanto, as referências a banco vazio abaixo descrevem a validação inicial da migração; o banco agora contém essa conta de teste. O verificador `Chamapro.DatabaseChecks` inclui uma asserção de banco vazio e deve ser usado apenas nessa condição inicial.

Não foram acessados serviços do Supabase, feitos commits/pushes ou descartadas alterações existentes.

Projeto alterado diretamente em `C:\Users\otavi\source\repos\matheusb-dev\CHAMAPRO`, branch `main`, HEAD `a9c75db9ae2a432faa0772f22ee21b6f5a643e03`. Não foram criados clones, worktrees, branches, commits ou pushes. O Git estava limpo antes das alterações.

Os metadados locais do Visual Studio em `.vs/CHAMAPRO.slnx/v18/DocumentLayout.json` referenciam o `Program.cs` desta mesma pasta absoluta. A solução `Chamapro.Api.slnx` também referencia o projeto desta pasta. Isso confirma a correspondência dos arquivos; não foi feita uma verificação visual da janela atualmente aberta do Visual Studio.

## Banco e segurança

- Servidor confirmado: PostgreSQL **18.4**, `localhost:5432` (conexão observada em `::1`).
- Na inspeção inicial, `chamapro_local` não existia. O banco anterior `chamapro_vs_tests_9ac236a2` foi preservado, sem alterações.
- Criados `chamapro_local` e `chamapro_migration_tests_20261101`, pertencentes ao papel local já existente `chamapro_app_9354804f7f`.
- A senha administrativa foi recebida em janela com entrada oculta, usada somente na criação dos bancos e retirada do ambiente do processo. Não foi salva pelo script.
- A aplicação usa User Secrets com ID `chamapro-api-dev`: conexão local com o Passfile já existente e nova chave JWT aleatória. Nenhuma senha nova foi colocada nos arquivos versionados.
- Pool configurado: mínimo 0, máximo 30 conexões; timeout de conexão 10 segundos e de comandos 30 segundos.
- O banco de uso permanece **inteiramente vazio**, inclusive categorias e administradores. Os testes de gravação aconteceram exclusivamente no banco separado.
- Não houve acesso, modificação nem cópia de dados do Supabase.

## Estrutura instalada e conferida

O script `Infrastructure/Persistence/Database/chamapro_local.sql` consolida a estrutura dos scripts originais v2–v8, sem dados de exemplo, taxonomia inicial, papéis `anon`/`authenticated` ou configurações de exposição via PostgREST. Executa em transação e recusa um esquema público com tabelas existentes. Não é executado automaticamente na inicialização da API.

Foram instaladas **19 tabelas, 29 chaves estrangeiras, 19 chaves primárias, 5 constraints UNIQUE, 5 CHECKs, 57 índices e 14 sequências**. Há quatro tipos enum: `tipo_usuario`, `status_pedido`, `status_agendamento`, `status_verificacao`, com os rótulos exigidos pelos enums C#.

Tabelas:

`usuarios`, `categorias`, `profissionais`, `profissional_categorias`, `areas_atendimento`, `pedidos`, `pedido_destinatarios`, `orcamentos`, `agendamentos`, `avaliacoes`, `verificacoes_profissionais`, `denuncias`, `favoritos`, `notificacoes`, `disponibilidade_profissional`, `arquivos_privados`, `historico_pedidos`, `auditoria_admin`, `recuperacoes_senha`.

O modelo EF foi confrontado com as colunas, PKs e FKs reais das 19 tabelas. Todos os rótulos dos quatro enums foram confrontados com o catálogo PostgreSQL. O verificador confirmou zero registros em cada tabela de uso. Não foram necessárias funções ou triggers de negócio; essas operações já são realizadas pela aplicação.

Dump somente da estrutura: [.migration-local/chamapro_local-schema.sql](.migration-local/chamapro_local-schema.sql).

## Código e configurações

Arquivos modificados por esta execução:

- `Program.cs`: suporte a autenticação PostgreSQL por Passfile; tradutor de enums compartilhado entre contextos; remoção da repetição automática incompatível com as transações explícitas dos serviços.
- `appsettings.json`: remoção da conexão remota e da chave JWT versionada; configuração base local sem senha. User Secrets têm precedência no perfil Development usado pelo Visual Studio.
- `Infrastructure/Persistence/Data/AppDbContext.cs`: explicitação dos relacionamentos de cancelamento, profissional do agendamento e usuários da avaliação; índice único do perfil profissional por usuário.
- `Application/DTOs/Requests/OrcamentoCriarRequest.cs`: interpretação invariável dos limites decimais para impedir erro HTTP 500 em `pt-BR`.
- `Tests/Chamapro.Tests/Program.cs` e `Tests/Chamapro.Tests/Chamapro.Tests.csproj`: regressão para valores de orçamento na cultura `pt-BR`.
- `.gitignore`: exclusão do diretório privado de evidências e backup `.migration-local/`.

Arquivos adicionados: script SQL local, este relatório e o projeto de conferência `Tests/Chamapro.DatabaseChecks/`.

`Infrastructure/Repositories/ProfissionalRepository.cs` recebeu uma alteração externa durante o trabalho, depois da primeira verificação de Git limpo. Ela foi preservada: projeção de avaliações antes da ordenação/paginação. Não foi descartada nem atribuída a esta execução.

O projeto já utilizava .NET 10, EF Core 10.0.14 e Npgsql EF Core 10.0.3; não foi necessário substituir pacotes. Autenticação JWT, BCrypt e armazenamento local de arquivos já existiam. Não havia SDK Supabase a remover. Os scripts históricos continuam disponíveis para referência, sem participação na execução local. O mapeamento de enums segue a [documentação oficial do Npgsql](https://www.npgsql.org/efcore/mapping/enum.html).

## Resultados executados

- `dotnet restore`: sucesso.
- `dotnet build --no-restore`: sucesso, **zero erros e zero avisos**.
- Testes existentes e regressão decimal: **7/7 passaram**.
- Conferência do banco: **19/19 entidades e 4/4 enums passaram**; todas as tabelas de `chamapro_local` vazias.
- API no banco separado, porta 5087: **37 chamadas HTTP bem-sucedidas**. Cadastro de cliente e profissional, login, autenticação, serviços, áreas, disponibilidade, busca, pedido, aceite, orçamento, seleção do orçamento, criação/leitura/confirmação do agendamento, histórico, notificações, painel, favoritos, conclusão de serviço, avaliação e denúncia.
- A preparação da categoria e a marcação de identidade verificada para esse fluxo aconteceram por SQL **somente no banco de testes**; isso não constitui teste de aprovação administrativa pela API.
- API no banco de uso, usando o perfil Development e User Secrets padrão, porta temporária 5088: página inicial, Swagger e categorias retornaram HTTP 200; `/api/v1/auth/me` sem token retornou HTTP 401, como esperado.
- O catálogo confirmou conexões da aplicação em `chamapro_local`; a configuração executada não aponta para o Supabase.

Evidências locais sem tokens ou senhas:

- [Restore](.migration-local/restore-result.txt)
- [Build](.migration-local/build-result.txt)
- [Testes de regressão](.migration-local/unit-results.txt)
- [Conferência de banco](.migration-local/database-results.txt)
- [37 chamadas HTTP](.migration-local/http-results.txt)
- [Git status completo](.migration-local/git-status.txt)

## Limites e estado para uso

O teste do perfil padrão encontrou a porta 5000 ocupada. O processo existente não foi encerrado; por isso a validação do banco de uso ocorreu em 5088 sem alterar `Properties/launchSettings.json`. As duas instâncias iniciadas por esta execução (5087 e 5088) foram encerradas ao final, liberando seus executáveis para recompilação no Visual Studio.

SMTP não está configurado. Envio real de e-mails, recuperação de senha por e-mail e lembretes não foram validados. Não foram testados nesta execução upload/download de arquivos, aprovação administrativa de documentos, todos os endpoints administrativos nem navegação visual completa das telas. Esses fluxos não dependem do Supabase pelo código analisado, mas não devem ser considerados integralmente testados.

Nenhuma conta administrativa ou categoria foi criada no banco de uso, atendendo ao pedido de banco vazio. O mecanismo original `Bootstrap:AdminEmail`/`Bootstrap:AdminPassword` continua disponível para provisionamento administrativo explícito por configuração segura.

## Preservação e Git

Backup do HEAD original: `.migration-local/original-a9c75db.zip`. A configuração local ignorada anterior foi preservada em `.migration-local/appsettings.Local.original.json`. Esses backups permanecem fora das pastas públicas de arquivos e fora do versionamento. Nenhuma alteração de outro desenvolvedor foi revertida.

`bin/` e `obj/` já possuíam arquivos versionados no HEAD original, apesar das regras de ignore. Restore/build alteraram esses artefatos rastreados, inclusive binários dos testes. Eles estão listados no Git status completo; não foram removidos do índice nem restaurados sobre possíveis alterações concorrentes.

A branch permanece `main`, com alterações locais sem commit. O estado exato está registrado no arquivo de Git status vinculado acima.
