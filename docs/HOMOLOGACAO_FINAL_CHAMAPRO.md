# Homologação final do CHAMAPRO

Data: 04/11/2026. Repositório local original, branch main, sem nova branch/worktree, commit, push, pull, merge ou rebase. Alterações locais da rodada anterior preservadas.

## Resultado objetivo

1. **Os fluxos principais estão funcionando?** Leitura de dados existentes, permissões de leitura, navegação e telas principais passaram com API e PostgreSQL reais. Formulários e regras de escrita passaram em testes simulados. Não foi validada a persistência real de novas solicitações, propostas, contratação ou avaliação.
2. **Problemas corrigidos:** HTTP 500 no relatório administrativo por consulta EF não traduzível; mensagem técnica em inglês em falha de rede ao salvar perfil. O verificador de catálogo também foi corrigido para aceitar uma base de demonstração que já possui dados. A instância de teste agora desabilita explicitamente as rotinas de fundo.
3. **O que ainda exige homologação de escrita:** concluir cadastro e recarregar; criar pedido sem profissionais e receber proposta posterior; enviar/selecionar proposta; bloquear conta com proposta pendente; iniciar/concluir serviço e registrar avaliação; envio real de documento. Esses testes precisam de ambiente de escrita autorizado; não devem ser executados no chamapro_local sob a autorização atual.
4. **Impedimento para publicar:** não há falha de leitura aberta nos cenários executados, mas a publicação ainda não tem homologação completa de escrita e concorrência. Recomenda-se validar essa parte em ambiente próprio antes de publicar. Este relatório não declara o produto integralmente homologado de ponta a ponta.

## PASSOU — testes realmente executados

| Camada | Resultado | Natureza da evidência |
| --- | --- | --- |
| Frontend Node | 160/160 | Testes automatizados de componentes, regras, formulários e regressões, com dados simulados. |
| Backend Chamapro.Tests | 8/8 | Testes de validações e transições, sem banco. |
| Backend Chamapro.FinalChecks | 18/18 | Serviços com repositórios simulados, regras de bloqueio/seleção, pedidos abertos, compatibilidade histórica, tradução SQL e desativação de rotinas. |
| DatabaseChecks | PASSOU | PostgreSQL real: catálogo de 19 entidades, colunas/PK/FK e quatro enums; não exige base vazia. |
| IntegrationChecks | PASSOU | 66 verificações GET reais, incluindo cinco pedidos do cliente, detalhe/propostas/histórico, favoritos e limites de acesso 401/403/404. |
| AdminChecks | PASSOU | Seis rotas administrativas: anônimo 401, profissional 403, administrador existente 200. Relatórios, usuários, pedidos, auditoria, verificações e denúncias lidos. |
| OrderChecks, modo sem --create | PASSOU | Sessão de cliente via GET /auth/me; sem criação de pedidos. |
| ProfileChecks | PASSOU | Perfil profissional, serviços/preços, regiões, disponibilidade histórica e situação do cadastro consultados via API real. |
| WorkChecks | PASSOU | Seis filtros de status, segunda página, sete detalhes/históricos, quatro propostas, três trabalhos escolhidos e leituras históricas. |
| Conferência de notas | PASSOU | Médias/quantidades retornadas pela API comparadas com SELECTs: 2 profissionais com avaliações e 12 sem avaliações. Nenhuma avaliação criada. |
| Edge + Playwright | 91/91 | Navegador real, HTML/CSS/JS reais, API de leitura real; testes de contatos e falha de rede explicitamente simulados abaixo. |
| Responsividade | PASSOU | 76 capturas: 19 páginas/estados por largura, em 360, 390, 768 e 1366 px. Zero transbordamentos horizontais detectados. |
| Erros de execução | PASSOU | Zero erros JavaScript não tratados e zero respostas API 5xx na rodada final de navegador. |
| Integridade dos dados | PASSOU | Conteúdo das 19 tabelas comparado antes/depois por quantidade e digest das linhas; resultado inalterado. |
| Compilação | PASSOU | API e projetos dos verificadores utilizados compilam. |

### Fluxos reais de navegador e leitura

Foram abertas as páginas pública e de autenticação; início, criação, lista/detalhe de pedidos, favoritos, conta e perfil profissional do cliente; painel, fila, perfil, cadastro e verificação do profissional; início, verificações, usuários e pedidos da administração. Todas as quatro funções — visitante, cliente, profissional e administrador existente — participaram da navegação de logos por teclado.

O formulário de pedido foi preenchido até a navegação entre etapas e voltou mantendo o rascunho, **sem enviá-lo**. A ausência de campos de data/horário foi conferida no pedido e no cadastro profissional. Pedidos e propostas existentes foram lidos e renderizados com valores, contatos disponíveis/indisponíveis e registros históricos. O profissional não verificado existente não recebeu oportunidades compatíveis na consulta pendente. A leitura do telefone administrativo foi aceita para administrador existente e recusada para cliente.

As capturas demonstram execução real do navegador. Foram inspecionadas visualmente amostras de detalhe de pedido/perfil profissional em 390 px e painel administrativo em 768 px. As demais capturas foram produzidas e verificadas automaticamente quanto a conteúdo, navegação e largura; não houve inspeção humana pixel a pixel de todas elas. Não foram enviados dados a WhatsApp, nem mensagens reais. O navegador bloqueou recursos externos de outros domínios: fontes e imagens remotas não foram homologadas, e o layout pode ter usado fontes de fallback. Não foi instalada nem alterada configuração global de navegador; foi utilizado o Edge já disponível, em modo headless e com sessão temporária.

### Simulações executadas no navegador, sem persistência

- O salvamento de perfil foi interrompido antes de chegar à API. O texto permaneceu preenchido, os controles foram liberados e a mensagem em português foi exibida. A única tentativa de PUT foi abortada pelo interceptor do navegador; não chegou ao servidor.
- Na tela de proposta real, apenas o campo de telefone da resposta GET foi substituído temporariamente no navegador para testar telefone válido, ausente e inválido. O link válido incluiu 55/DDD, ficou abaixo do preço e usou wa.me; os outros casos não produziram link funcional. O banco e a resposta original do servidor não foram alterados.
- A confirmação de escolha foi aberta e dispensada. O texto explica urgência e contato; o aceite não foi enviado.
- Cadastro concluído/redirecionamento, criação sem destinatários, interesse/proposta posterior, bloqueio e seleção, início/conclusão e avaliação têm cobertura adicional nos testes de frontend/serviços com repositórios simulados. Isso não comprova o ciclo real de escrita no PostgreSQL.

## FALHOU — reprodução, causa e correção

### 1. Relatório administrativo retornava HTTP 500 — corrigido

**Reprodução real:** GET /api/v1/admin/relatorios com administrador existente na instância da API atual, ligada ao PostgreSQL em modo somente leitura.

**Causa:** a consulta agrupava pedidos por categoria, construía o record AdminCategoriaUsoResponse e só depois ordenava por uma propriedade do record. O provedor EF/Npgsql não conseguia traduzir essa composição.

**Correção:** agregar/ordenar/limitar por projeção intermediária traduzível e construir o DTO no final. Sem avaliação em memória de toda a tabela, migration ou alteração de dados.

**Validação:** teste de regressão ToQueryString; GET real passou a HTTP 200; AdminChecks integral aprovado e painel administrativo aberto nas quatro larguras. Na rodada final o arquivo de erros da API está vazio.

### 2. Falha de rede mostrava “Failed to fetch” no perfil — corrigido

**Reprodução real de UI com falha de rede injetada:** ao salvar o perfil, o navegador abortava o PUT antes da API e o texto nativo em inglês aparecia na tela.

**Causa:** o cliente compartilhado da API deixava escapar o TypeError de fetch.

**Correção:** normalizar falhas de transporte com código FALHA_REDE e a mensagem “Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.” Cancelamentos AbortError continuam sendo preservados. Assets da API receberam versão nova para invalidar cache.

**Validação:** teste automatizado novo no frontend e repetição no Edge. Rascunho e dados reais preservados; nenhuma gravação chegou à API.

### 3. Premissa incorreta do verificador de catálogo — corrigida no teste

DatabaseChecks falhava apenas porque a tabela já possuía dados. A exigência de base vazia foi retirada, mantendo verificações de colunas, chaves e enums. O verificador usa conexão explicitamente somente leitura. Passou nas 19 entidades existentes.

### Ajustes no ambiente de homologação

A API registra uma rotina de cancelamento ao iniciar. Para não depender apenas da proteção do PostgreSQL contra gravações durante testes de leitura, foi adicionada a opção Rotinas:Habilitadas, cujo padrão mantém o comportamento anterior. A instância de teste define false e um teste comprova que nenhuma rotina de fundo é registrada. Bootstrap administrativo fica desabilitado somente no processo temporário de teste, sem alterar arquivo de configuração ou conta.

A primeira rodada do navegador também apontou três problemas no próprio teste: aceitar /index.html como destino público equivalente a /; aguardar o preenchimento automático do título antes de inserir o rascunho; selecionar o summary direto da seção em vez de dois summaries aninhados. Esses casos foram corrigidos no roteiro e não foram classificados como defeitos do CHAMAPRO.

## NÃO TESTADO — persistência real e limites restantes

| Fluxo | O que ficou sem comprovação real |
| --- | --- |
| Pedido urgente novo | INSERT, salvamento sem compatíveis, presença após recarregar e proposta posterior. |
| Cadastro profissional completo | Gravação de serviços/preços/regiões, conclusão e redirecionamento após salvar no banco. |
| Proposta e escolha | Envio/reenvio, seleção exclusiva, notificações e transação de contratação gravadas. |
| Bloqueio administrativo | Aplicar bloqueio em conta existente e verificar a corrida concorrente entre bloquear, enviar e selecionar proposta. Não há fixture de profissional já bloqueado no navegador desta rodada; a regra foi coberta por simulação de serviços. |
| Execução e avaliação novas | Iniciar/concluir um serviço e adicionar avaliação com atualização de média/cache. Médias de avaliações já existentes foram verificadas. |
| Documentos | Upload/download real de documento privado do proprietário e avaliação administrativa. A negação anônima e a tela foram verificadas. |
| WhatsApp em dispositivos físicos | Abertura do aplicativo no celular/computador real. Formato do link e estado visual foram verificados; nenhuma mensagem foi enviada. |
| Publicação | Deploy, HTTPS de produção, desempenho sob carga e concorrência de gravações não foram testados. |

Não se executaram seeds nem migrations. Chamapro.DemoSeed é um utilitário de escrita, não uma suíte de leitura, e não foi executado. PersistenceChecks foi inspecionado e compilado, mas não completou nenhum teste de escrita. Não houve validação bem-sucedida de persistência real nova.

## Banco utilizado e autorização

- Banco efetivamente testado: chamapro_local, PostgreSQL local. A instância própria da API usou 127.0.0.1:5096 com default_transaction_read_only=on; as rotinas de fundo foram desabilitadas e o navegador bloqueou métodos de mutação. A instância já existente na porta 5088 não foi reiniciada nem modificada.
- Uma tentativa inicial de criar a base isolada autorizada foi recusada por SQLSTATE 42501. O usuário depois disponibilizou chamapro_homologacao_20261104 vazio. A restauração de esquema, em transação única, foi recusada por falta de permissão no public e revertida. Não foram criados usuários, pedidos, propostas ou avaliações nessa base.
- Em seguida, a orientação foi alterada para não realizar mais testes de escrita nem ajustar permissões. A homologação prosseguiu exclusivamente em leitura no chamapro_local.
- Não foram alterados privilégios, credenciais ou contas administrativas. Tokens temporários foram gerados somente na memória para contas existentes e transmitidos ao navegador por stdin; não foram registrados nos relatórios.
- O resultado final confirma sourceTablesUnchanged=true para as 19 tabelas. Nenhuma gravação de homologação foi efetivada no banco original; a comparação do conteúdo das 19 tabelas confirmou sua preservação.

## Evidências e reprodução

- [result.json](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/result.json) — modo somente leitura, 19 tabelas preservadas e códigos de saída das seis suítes/API/navegador.
- [browser-results.json](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/browser-results.json) — 91 cenários aprovados, URLs, larguras, capturas e bloqueio da tentativa de PUT.
- [browser.txt](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/browser.txt) — resultado de cada cenário de navegador.
- [AdminChecks.txt](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/AdminChecks.txt), [IntegrationChecks.txt](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/IntegrationChecks.txt), [DatabaseChecks.txt](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/DatabaseChecks.txt) — resultados reais de leitura.
- [screenshots/390-cliente-6.png](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/screenshots/390-cliente-6.png) — detalhe/proposta e contato inválido de registro existente em 390 px.
- [screenshots/390-profissional-2.png](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/screenshots/390-profissional-2.png) — perfil profissional em 390 px.
- [screenshots/768-admin-0.png](C:/Users/otavi/source/repos/matheusb-dev/CHAMAPRO/Tests/.runtime/readonly/screenshots/768-admin-0.png) — painel administrativo corrigido em 768 px.

Os artefatos locais ficam em Tests/.runtime e Tests/.artifacts, ignorados pelo Git para não incluir perfis de navegador, saídas de compilação e sessões nos fontes. As 76 capturas permanecem disponíveis localmente; não foram publicadas.

Comandos utilizados:

~~~powershell
node --test Tests/frontend/*.test.cjs
dotnet run --project Tests/Chamapro.Tests/Chamapro.Tests.csproj --artifacts-path Tests/.artifacts -p:NuGetAudit=false --ignore-failed-sources
dotnet run --project Tests/Chamapro.FinalChecks/Chamapro.FinalChecks.csproj --artifacts-path Tests/.artifacts -p:NuGetAudit=false --no-restore
dotnet build Tests/Chamapro.ReadOnlyChecks/Chamapro.ReadOnlyChecks.csproj --artifacts-path Tests/.artifacts -p:NuGetAudit=false --no-restore
# Após compilar também os seis verificadores de leitura:
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet Tests/.artifacts/bin/Chamapro.ReadOnlyChecks/debug/Chamapro.ReadOnlyChecks.dll
~~~

O último comando exige acesso local ao PostgreSQL, User Secrets já configurados e Edge/Playwright disponíveis. O roteiro utiliza somente GETs reais; a tentativa simulada de PUT é abortada antes de chegar à API. Não executar modos --prepare/--run de PersistenceChecks no chamapro_local. O teste automatizado de escrita foi suspenso conforme a última orientação do usuário.

## Arquivos desta rodada

- .gitignore
- Program.cs
- Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
- Presentation/Controllers/AdminController.cs
- wwwroot/js/api.js
- Tests/Chamapro.DatabaseChecks/Program.cs
- Tests/Chamapro.AdminChecks/Program.cs
- Tests/Chamapro.IntegrationChecks/Program.cs
- Tests/Chamapro.OrderChecks/Program.cs
- Tests/Chamapro.ProfileChecks/Program.cs
- Tests/Chamapro.WorkChecks/Program.cs
- Tests/Chamapro.PersistenceChecks/Program.cs
- Tests/Chamapro.FinalChecks/Program.cs
- Tests/Chamapro.ReadOnlyChecks/Chamapro.ReadOnlyChecks.csproj
- Tests/Chamapro.ReadOnlyChecks/Program.cs
- Tests/browser/homologacao.cjs
- Tests/frontend/final-adjustments.test.cjs
- Tests/frontend/authenticated-layout.test.cjs
- docs/HOMOLOGACAO_FINAL_CHAMAPRO.md
- Páginas HTML que já carregam /js/api.js: atualização apenas da versão do asset para 20261104-homologacao1. Sem reformulação visual.

bin/ e obj/ já estavam modificados no início e foram preservados; as compilações desta rodada usaram Tests/.artifacts. Não fazem parte da entrega de fontes. Todas as correções estão nos arquivos locais da main para revisão.
