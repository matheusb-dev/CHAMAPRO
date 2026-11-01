# CHAMAPRO — status de conclusão

## Repositório e Supabase

- Branch de trabalho: `feat/telas-05-a-08-versao-vitoria`. A branch `main` não foi usada nem alterada.
- Sem push: as alterações ficam locais e serão registradas em commits organizados nesta branch.
- Novo projeto Supabase: `chamapro-dev`, região São Paulo, referência `eqxkrwkgixhchizdheux`.
- Migrações aplicadas ao projeto novo: `chamapro_schema_v2` a `chamapro_schema_v5`.
- Verificação remota: 17 tabelas públicas encontradas e RLS habilitado em todas; as nove categorias iniciais foram inseridas; índices de FK faltantes foram adicionados pela v4; privilégios de `PUBLIC`, `anon` e `authenticated` foram revogados pela v5.
- O projeto Supabase antigo `chamapro` permaneceu sem alterações.

## Implementado

- Repositórios, serviços e endpoints usam PostgreSQL/EF em vez dos dados fictícios em memória.
- Cadastro, login, perfil de cliente e profissional, categorias, áreas atendidas e disponibilidade semanal.
- Chamados imediatos e agendados, distribuição, fila do profissional, interesse/recusa, orçamentos, contratação, agenda, remarcação, cancelamento e conclusão.
- Verificação profissional com revisão administrativa e documentos privados com controle de propriedade.
- Avaliações, favoritos, notificações persistidas, denúncias e relatórios de administração.
- Contato por WhatsApp condicionado à contratação e à preferência do usuário.
- Histórico persistido dos eventos de chamado.
- DTOs administrativos não serializam credenciais nem entidades com navegações sensíveis.
- Arquivos de configuração versionados não contêm conexão real ou chave JWT. O primeiro administrador pode ser criado uma única vez por configuração local de bootstrap.
- Foram removidos fluxos demonstrativos da interface administrativa e conectadas as telas principais à API.

## Verificações locais

- `dotnet build Chamapro.Api.slnx --no-restore`: passou sem warnings ou erros.
- `dotnet run --project Tests/Chamapro.Tests/Chamapro.Tests.csproj`: 5 de 5 verificações passaram.
- Smoke test sem banco válido: `/`, página de perfil e Swagger responderam 200; `/api/v1/auth/me` sem autenticação respondeu 401.
- `git diff --check`: passou antes da última atualização de documentação; repetir antes dos commits.
- Não foi possível validar operações de ponta a ponta no PostgreSQL porque a senha do novo banco não é fornecida pelo conector. Ela deve ser configurada localmente no User Secrets antes de executar o backend.
- A restauração diretamente do NuGet falha nesta máquina por erro TLS. Build e testes foram executados usando pacotes NuGet já presentes no cache local.

## Segurança e limitações conhecidas

- Todas as tabelas públicas têm RLS ligado. A verificação SQL confirmou zero grants de tabela a `PUBLIC`, `anon` ou `authenticated` e sem SELECT/INSERT/UPDATE para os papéis públicos; isso combina com a arquitetura de API privada. Caso o frontend passe a acessar Supabase diretamente, será necessário criar policies com propriedade por usuário e grants mínimos antes de conceder acesso.
- Os advisors apontam índices não usados no projeto recém-criado, o que é esperado antes de haver consultas reais, e um índice duplicado em `agendamentos` herdado da combinação v2/v3. A tentativa de removê-lo expirou na confirmação do Supabase; o índice redundante não afeta a correção funcional.
- Uma credencial de banco apareceu no histórico público do repositório antigo. Ela foi retirada das configurações atuais, mas o histórico remoto não foi reescrito. A credencial do projeto antigo precisa ser rotacionada pelo responsável; nenhum valor é repetido neste repositório.
- E-mail SMTP, confirmação de e-mail e recuperação de senha ainda não estão implementados/configurados.
- Documentos privados e imagens ficam no disco da instância. Para publicar com múltiplas instâncias ou disco efêmero, mover os arquivos para armazenamento durável e privado.
- O primeiro administrador exige `Bootstrap:AdminEmail` e `Bootstrap:AdminPassword` em segredo local; remover essa configuração após a criação.

## Próximos passos para executar localmente

1. No Supabase novo, definir/consultar a senha do banco pelo painel sem compartilhá-la no chat.
2. Configurar `ConnectionStrings:DefaultConnection`, `Jwt:Key` e as credenciais de bootstrap com `dotnet user-secrets`.
3. Iniciar a API e testar cadastro, chamados, orçamentos, agenda, avaliação e administração no projeto `chamapro-dev`.
4. Configurar SMTP e armazenamento durável antes de uma publicação.
