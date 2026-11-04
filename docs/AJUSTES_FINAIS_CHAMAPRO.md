# CHAMAPRO — ajustes finais

Data: 04/11/2026. Repositório: C:\Users\otavi\source\repos\matheusb-dev\CHAMAPRO. Branch confirmada no início: main.

## Situação dos oito ajustes

| Ajuste | Situação | Resultado |
| --- | --- | --- |
| 1. Cadastro e verificação | Concluído | Terceira etapa passa a concluir o cadastro. Salva, confirma os requisitos e consulta novamente a verificação antes de redirecionar. Falhas mantêm os campos; profissionais verificados vão ao painel. |
| 2. Urgência sem agenda | Concluído | Cadastro, busca, compatibilidade, pedidos, painel e início do serviço deixam de depender de horários. Removidos controles de agenda. Escritas antigas de agenda retornam 410; leituras históricas permanecem autorizadas. |
| 3. WhatsApp | Concluído | Contato abaixo do preço da proposta, antes da seleção. Telefone real no DTO restrito ao cliente dono. Normalização brasileira/internacional e estado indisponível para números inválidos. Administração tem contato autorizado nos detalhes da verificação e na consulta de usuários. |
| 4. Profissionais bloqueados | Concluído | Filtro no repositório e no serviço de propostas, bloqueio no aceite e checagem de conta ativa na identificação profissional. Registros preservados e nenhum serviço em andamento cancelado automaticamente. |
| 5. Confirmação da escolha | Concluído | Confirmação explica solicitação urgente, encerramento das demais propostas e contato pelo WhatsApp. Detalhes posteriores orientam combinar o atendimento, sem calendário. |
| 6. Logo | Concluído | Links reais no cabeçalho, rodapé lateral e logos mobile de autenticação. Destinos por perfil; visitante vai à página pública. A página pública também carrega o utilitário de navegação. |
| 7. Nota visual inicial | Concluído | Formatação exibe 5,0 na ausência de nota. Perfis, propostas, favoritos e busca explicam a nota inicial sem avaliações. Notas reais e cálculos/ordenação do backend foram preservados. |
| 8. Pedido sem profissionais | Concluído | Distribuição automática salva pedido pendente com zero destinatários. Mensagem clara no envio e nos detalhes. A fila passa a considerar profissionais que se tornem compatíveis depois. Seleção manual continua exigindo IDs válidos e compatíveis. |

“Concluído” descreve a implementação local e as verificações abaixo. Não significa homologação no PostgreSQL ou aprovação visual em navegador real.

## Regras de negócio

- Novos pedidos usam IMEDIATO e DataDesejada nula. Solicitações novas de agendamento são recusadas na validação; o contrato de leitura conserva os campos antigos.
- A compatibilidade considera categoria ativa, cidade/bairro, identidade verificada e conta ativa. Não exige intervalos nem o antigo sinalizador de urgência, pois todos os novos serviços seguem esse modelo.
- A escolha mantém o fluxo de propostas e leva a ACEITO; o profissional escolhido pode iniciar diretamente EM_ANDAMENTO. Pedidos históricos AGENDADO continuam podendo avançar e ser concluídos/cancelados pelas rotas de pedido existentes.
- POST/PUT/PATCH/DELETE de agendamentos ficam descontinuados. GETs de histórico não foram removidos. A rota de configuração de disponibilidade também retorna 410 sem apagar horários antigos. Administração não pode atribuir AGENDADO novamente. A rotina de lembretes de agenda deixa de ser registrada.
- Pedidos automáticos recebem um registro de criação específico no histórico existente: “Chamado urgente aberto a profissionais compatíveis.” Esse registro identifica a fila aberta sem migration. O texto deve ser preservado; alterar sua redação isoladamente quebraria a identificação de pedidos já criados nessa rodada.
- A fila e a contagem de oportunidades incluem pedidos abertos compatíveis ainda sem destinatário para o profissional. A adesão só é persistida quando ele demonstra interesse ou recusa, dentro da transação/trava do pedido. Leituras não criam destinatários. Pedidos manuais não entram nessa fila aberta.
- Pedidos automáticos continuam pendentes mesmo que todos os destinatários iniciais recusem. Pedidos IMEDIATO deixam de expirar automaticamente após sete dias; o limite existente de cinco pedidos pendentes por cliente permanece.
- Contato profissional é disponibilizado ao cliente que recebeu a proposta, independentemente do antigo opt-in de WhatsApp do profissional. Preferências de contato do cliente continuam protegidas. Não foi criado endpoint público com telefones. O endpoint administrativo exige ADMIN e o acesso por usuário valida a relação/autorização.
- Links WhatsApp validam formato, DDD brasileiro e comprimento, incluem 55 quando necessário e codificam a mensagem. Isso não confirma que o número possui uma conta ativa no WhatsApp. Números internacionais fora do Brasil precisam informar DDI com + ou 00.
- A nota 5,0 é exclusivamente visual: não há avaliações criadas nem mudança no ranking ou nos filtros de nota do backend.

## Testes executados

| Verificação | Resultado |
| --- | --- |
| node --test Tests/frontend/*.test.cjs | 159/159 aprovados |
| dotnet run --project Tests/Chamapro.Tests/Chamapro.Tests.csproj --artifacts-path Tests/.artifacts -p:NuGetAudit=false --ignore-failed-sources | 8/8 aprovados |
| dotnet run --project Tests/Chamapro.FinalChecks/Chamapro.FinalChecks.csproj --artifacts-path Tests/.artifacts -p:NuGetAudit=false --no-restore | 16/16 aprovados |
| Compilação da API pela referência do projeto FinalChecks | Aprovada |
| Compilação de Chamapro.PersistenceChecks e Chamapro.OrderChecks | Aprovada; zero avisos/erros |
| git diff --check restrito aos fontes, com core.whitespace=cr-at-eol | Aprovado |

Os testes novos exercitam validação de pedido urgente, persistência simulada sem destinatários, permissões entre clientes, recusa de proposta/seleção de bloqueados, preservação de registros, adesão posterior a pedidos abertos, leitura histórica, transições urgentes e históricas, rotas antigas 410 e autorização administrativa. A consulta da fila foi traduzida pelo provedor PostgreSQL usando ToQueryString sem abrir conexão.

No frontend há cobertura para conclusão/redirecionamento com e sem verificação, falha de salvamento e preservação dos campos, contato na proposta antes da seleção, telefone inválido/ausente, confirmação de escolha, navegação por perfil, 5,0 inicial, notas reais e pedido sem destinatários. Testes de escrita da antiga agenda foram adaptados para o novo comportamento; testes históricos e demais regressões foram mantidos. O teste de layout foi atualizado para reconhecer também os assets sino/martelo já existentes no início do trabalho.

## Limitações e riscos

- Nenhuma aplicação ASP.NET foi iniciada e nenhum teste abriu o banco de demonstração. Não houve migrations, seeds, criação de contas ou administradores, alteração de credenciais ou privilégios. Os serviços foram exercitados com repositórios simulados e a tradução SQL foi verificada sem conexão.
- Os verificadores que dependem da API/PostgreSQL (DatabaseChecks, IntegrationChecks, ProfileChecks, WorkChecks, OrderChecks e PersistenceChecks) não foram executados. Os dois últimos tiveram as expectativas antigas adaptadas e foram apenas compilados. OrderChecks agora exige --create também para o cenário sem profissionais, que passou a gravar um pedido; sem essa opção, não envia POSTs.
- Persistência PostgreSQL real, concorrência entre bloqueio e contratação e funcionamento de wa.me em dispositivos reais precisam de homologação em base isolada autorizada.
- Responsividade em 360, 390, 768 e 1366 px não foi validada visualmente: o Chromium do Playwright não está instalado e o Edge disponível encerrou na inicialização dentro do ambiente restrito. Não há screenshots nem aprovação visual presumida. Componentes, estilos, regras responsivas e identidade visual existentes foram preservados; a suíte de frontend passa.
- Identificar a distribuição aberta pelo histórico evita mudança de esquema, mas exige preservar o registro marcador. Em grande volume, revisar desempenho e eventual campo explícito/indexação em uma rodada futura autorizada.
- Pedidos urgentes abertos passam a ocupar o limite de pendências até contratação/cancelamento. Rever política de expiração com o produto caso necessário.
- A compatibilidade regional continua identificando cidade por nome; homônimos de UFs diferentes permanecem uma limitação anterior sinalizada na interface.
- bin/ e obj/ já apareciam modificados antes do trabalho. A compilação inicial também atualizou intermediários de obj/. Esses arquivos não integram a entrega de fontes e não foram revertidos, para preservar o estado local anterior. Os diretórios temporários de saída criados nesta rodada foram removidos. Revisar apenas os arquivos de código/testes/documentação listados abaixo.

## Homologação manual recomendada

1. Em base isolada autorizada, concluir o cadastro profissional sem horários: conferir serviços, preços e regiões persistidos; validar o destino da terceira etapa em contas verificadas e não verificadas. Simular falha de rede e conferir preservação dos campos.
2. Criar pedido urgente sem profissionais na cidade. Confirmar sucesso, mensagem explicativa e presença em Meus pedidos. Depois cadastrar/verificar um profissional compatível e conferir oportunidade, contagem no painel, interesse e envio de proposta.
3. Confirmar que pedido manual exige profissionais compatíveis e não aparece para profissionais não selecionados.
4. Enviar proposta com telefone válido, sem telefone e com telefone inválido; conferir botão abaixo do preço, DDI, mensagem, abertura em celular/computador e indisponibilidade sem link quando necessário.
5. Bloquear o profissional com proposta enviada e atualizar o cliente. Conferir ocultação e tentativa direta de aceite rejeitada; conferir token antigo impedido de enviar propostas. Validar que serviço já em andamento não foi cancelado.
6. Escolher profissional e iniciar/concluir atendimento sem agendamento. Conferir confirmação, contato e avaliação real posterior.
7. Consultar pedido histórico agendado sem permitir reagendamento; verificar histórico e continuidade de execução pelas rotas de pedido.
8. Conferir o contato administrativo em verificação e consulta de usuário; tentar a rota administrativa com cliente, profissional e visitante.
9. Navegar pelas logos via mouse, Tab e Enter nos quatro perfis, inclusive mobile. Conferir nota 5,0 inicial e média real após avaliações, sem alterar ranking.
10. Inspecionar páginas de cadastro, pedido, propostas e administração em 360, 390, 768 e 1366 px, incluindo textos longos e navegação pelo teclado.

## Banco e entrega

Não há migration necessária para estas correções. Tabelas, colunas, disponibilidade e agendamentos existentes foram mantidos. Uma eventual evolução do marcador de fila e de índices deve ser planejada e autorizada separadamente.

Não houve pull, branch nova, worktree, commit, push, merge ou rebase. As alterações estão nos arquivos reais da main para revisão.

## Arquivos modificados/criados

- Application/DTOs/Requests/BuscaProfissionaisQuery.cs
- Application/DTOs/Requests/PedidoCriarRequest.cs
- Application/DTOs/Responses/OrcamentoResponses.cs
- Application/Services/OrcamentoService.cs
- Application/Services/PedidoMapper.cs
- Application/Services/PedidoService.cs
- Application/Services/PedidoTransicoes.cs
- Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
- Infrastructure/Repositories/IPedidoRepository.cs
- Infrastructure/Repositories/OrcamentoRepository.cs
- Infrastructure/Repositories/PedidoRepository.cs
- Infrastructure/Repositories/ProfissionalRepository.cs
- Infrastructure/Repositories/RotinasRepository.cs
- Presentation/Controllers/AdminController.cs
- Presentation/Controllers/AgendamentosController.cs
- Presentation/Controllers/PainelProfissionalController.cs
- Presentation/Controllers/ProfissionaisController.cs
- Presentation/Controllers/WhatsAppController.cs
- Tests/Chamapro.FinalChecks/Chamapro.FinalChecks.csproj
- Tests/Chamapro.FinalChecks/Program.cs
- Tests/Chamapro.OrderChecks/Program.cs
- Tests/Chamapro.PersistenceChecks/Program.cs
- Tests/frontend/authenticated-layout.test.cjs
- Tests/frontend/client-experience.test.cjs
- Tests/frontend/final-adjustments.test.cjs
- Tests/frontend/helpers/api.cjs
- Tests/frontend/stage1-regressions.test.cjs
- Tests/frontend/stage2-client.test.cjs
- Tests/frontend/stage3-order.test.cjs
- Tests/frontend/stage4-professional.test.cjs
- Tests/frontend/stage5-work.test.cjs
- Tests/frontend/stage6-admin.test.cjs
- Tests/frontend/stage7-integrated.test.cjs
- docs/AJUSTES_FINAIS_CHAMAPRO.md
- wwwroot/index.html
- wwwroot/js/api.js
- wwwroot/js/components/header.js
- wwwroot/js/components/professional-data.js
- wwwroot/js/components/professional-editor.js
- wwwroot/js/components/professional-work.js
- wwwroot/js/components/sidebar.js
- wwwroot/js/pages/admin.js
- wwwroot/js/pages/cliente-favoritos.js
- wwwroot/js/pages/cliente-inicio.js
- wwwroot/js/pages/cliente-orcamentos.js
- wwwroot/js/pages/cliente-perfil-profissional.js
- wwwroot/js/pages/cliente.js
- wwwroot/js/pages/trabalhador-verificacao.js
- wwwroot/pages/admin/gestao.html
- wwwroot/pages/autenticacao/cadastro.html
- wwwroot/pages/autenticacao/login.html
- wwwroot/pages/cliente/criar-chamado.html
- wwwroot/pages/cliente/favoritos.html
- wwwroot/pages/cliente/orcamentos.html
- wwwroot/pages/cliente/painel.html
- wwwroot/pages/cliente/perfil-profissional.html
- wwwroot/pages/cliente/perfil.html
- wwwroot/pages/trabalhador/cadastro-profissional.html
- wwwroot/pages/trabalhador/detalhes-chamado.html
- wwwroot/pages/trabalhador/fila-chamados.html
- wwwroot/pages/trabalhador/painel.html
- wwwroot/pages/trabalhador/perfil.html
- wwwroot/pages/trabalhador/verificacao.html
