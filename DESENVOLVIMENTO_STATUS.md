# CHAMAPRO — status de conclusão

## Repositório e Supabase

- Branch de trabalho: `feat/telas-05-a-08-versao-vitoria`. A branch `main` não foi usada nem alterada.
- Sem push. Commits locais: `fe8a146` (API/persistência), `446d344` (telas), `24b96b0` (testes/documentação), `1a5ce67` (status inicial), `030ccd0` (auditoria administrativa), `c9517f5` (painéis e agenda), `9393152` (segurança de autenticação), `97a3e18` (documentação de limitações), `3c08ce6` (cadastro profissional em etapas).
- Novo projeto Supabase: `chamapro-dev`, região São Paulo, referência `eqxkrwkgixhchizdheux`.
- Migrações aplicadas ao projeto novo: `chamapro_schema_v2` a `chamapro_schema_v7`.
- Reconsulta remota em 31/10/2026: projeto `ACTIVE_HEALTHY`, PostgreSQL 17; a lista remota ainda confirma v2–v7.
- Verificação remota: 18 tabelas públicas encontradas e RLS habilitado em todas; as nove categorias iniciais foram inseridas; índices de FK foram adicionados pelas v4 e v7; privilégios de `PUBLIC`, `anon` e `authenticated` foram revogados pela v5; tabela de auditoria administrativa criada pela v6.
- O projeto Supabase antigo `chamapro` permaneceu sem alterações.

## Implementado

- Repositórios, serviços e endpoints usam PostgreSQL/EF em vez dos dados fictícios em memória.
- Cadastro, login, perfil de cliente e profissional, categorias, áreas atendidas e disponibilidade semanal.
- Chamados imediatos e agendados, distribuição, fila do profissional, interesse/recusa, orçamentos, contratação, agenda, remarcação, cancelamento e conclusão.
- Verificação profissional com revisão administrativa e documentos privados com controle de propriedade.
- Avaliações, favoritos, notificações persistidas, denúncias e relatórios de administração.
- Contato por WhatsApp condicionado à contratação e à preferência do usuário.
- Histórico persistido dos eventos de chamado.
- Trilha de auditoria administrativa persistida, consultável em `/api/v1/admin/auditoria` e exibida no painel.
- Painel profissional com contagens persistidas e agenda com confirmação, remarcação e cancelamento; painel do cliente também lista seus agendamentos.
- Cadastro profissional guiado em cinco etapas: conta, profissão/serviços, localização, preços/horários e verificação. A API salva cada etapa; o painel identifica cadastro incompleto a partir dos dados persistidos e oferece retomada. O envio de certificado/documento é opcional quando a atividade não exige comprovação.
- Limite de tentativas por IP para login e cadastro; o login executa uma verificação BCrypt fictícia quando a conta não existe ou está inativa para reduzir diferença temporal.
- DTOs administrativos não serializam credenciais nem entidades com navegações sensíveis.
- Arquivos de configuração versionados não contêm conexão real ou chave JWT. O primeiro administrador pode ser criado uma única vez por configuração local de bootstrap.
- Foram removidos fluxos demonstrativos da interface administrativa e conectadas as telas principais à API.

## Verificações locais

- `dotnet build Chamapro.Api.slnx --no-restore`: passou sem warnings ou erros.
- `dotnet run --project Tests/Chamapro.Tests/Chamapro.Tests.csproj`: 6 de 6 verificações passaram, incluindo a gravação de categorias antes da definição da faixa de preço.
- Smoke test sem banco válido: `/`, painel profissional, painel do cliente, nova tela de cadastro profissional, tela de verificação, CSS/JS do assistente e Swagger responderam 200; `/api/v1/auth/me` e `/api/v1/painel/profissional` responderam 401 sem token. No endpoint de login, cinco corpos inválidos responderam 400 e a sexta tentativa respondeu 429, confirmando o limite sem acessar o banco.
- JavaScript: os 14 arquivos passaram na checagem sintática com `vm.Script`.
- Auditoria estática de links/assets: 15 páginas HTML verificadas, sem `href` ou `src` local quebrado.
- `git diff --check`: passou na revisão atual.
- Não foi possível validar operações de ponta a ponta no PostgreSQL porque a senha do novo banco não é fornecida pelo conector. Ela deve ser configurada localmente no User Secrets antes de executar o backend.
- A restauração diretamente do NuGet falha nesta máquina por erro TLS. Build e testes foram executados usando pacotes NuGet já presentes no cache local.

## Segurança e limitações conhecidas

- Todas as tabelas públicas têm RLS ligado. A verificação SQL confirmou zero grants de tabela a `PUBLIC`, `anon` ou `authenticated` e sem SELECT/INSERT/UPDATE para os papéis públicos; isso combina com a arquitetura de API privada. Caso o frontend passe a acessar Supabase diretamente, será necessário criar policies com propriedade por usuário e grants mínimos antes de conceder acesso.
- A auditoria de performance mais recente aponta 27 índices sem uso (INFO), esperado enquanto o banco de desenvolvimento está vazio, e um índice duplicado (WARN) em `agendamentos.pedido_id`: a v2 já cria a restrição `UNIQUE` e a v3 cria `ux_agendamentos_pedido` sobre a mesma coluna. Não removi nenhum objeto remoto; o aviso não afeta a correção funcional e deve ser revisto numa migração aprovada antes de remover o índice redundante.
- O limite de tentativas está em memória no processo da API e usa o IP da conexão. Em implantação com várias instâncias, deve ser substituído por armazenamento distribuído; se houver proxy, a identificação de IP precisa ser configurada com encaminhamento confiável. Configure também `Cors:Origins` com as origens reais do frontend antes da publicação.
- Uma credencial de banco apareceu no histórico público do repositório antigo. Ela foi retirada das configurações atuais, mas o histórico remoto não foi reescrito. A credencial do projeto antigo precisa ser rotacionada pelo responsável; nenhum valor é repetido neste repositório.
- E-mail SMTP, confirmação de e-mail e recuperação de senha ainda não estão implementados/configurados.
- A revisão visual interativa no navegador não foi concluída: a ferramenta de automação interrompeu o controle porque não conseguiu determinar com confiança a URL aberta no Brave. A checagem atual cobre build, sintaxe JS e respostas HTTP das páginas, sem atestar o layout visual.
- O roteiro de fluxo completo do prompt mestre (cadastro de dois perfis, revisão administrativa, chamado, orçamento, contratação, agendamento, conclusão e avaliação) ainda não foi executado contra o PostgreSQL novo. O conector não fornece a senha e nenhum segredo foi encontrado em User Secrets locais; essa validação depende da configuração local da conexão.
- Documentos privados e imagens ficam no disco da instância. Para publicar com múltiplas instâncias ou disco efêmero, mover os arquivos para armazenamento durável e privado.
- O primeiro administrador exige `Bootstrap:AdminEmail` e `Bootstrap:AdminPassword` em segredo local; remover essa configuração após a criação.
- A distribuição e a busca usam categoria, cidade, bairro, disponibilidade, conta ativa e aprovação de identidade. `DistanciaMaxKm` é solicitado e salvo, mas não filtra por raio geográfico porque os endereços ainda não têm coordenadas e não há serviço de geocodificação configurado.

## Próximos passos para executar localmente

1. No Supabase novo, definir/consultar a senha do banco pelo painel sem compartilhá-la no chat.
2. Configurar `ConnectionStrings:DefaultConnection`, `Jwt:Key` e as credenciais de bootstrap com `dotnet user-secrets`.
3. Iniciar a API e executar o fluxo completo do cliente e profissional no projeto `chamapro-dev`, validando persistência, autorização e avaliações.
4. Revisar visualmente as etapas do cadastro e validar responsividade em desktop e celular quando houver uma janela de navegador local identificável.
5. Configurar SMTP e armazenamento durável antes de uma publicação.
