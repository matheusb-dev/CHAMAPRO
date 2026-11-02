# Etapa 7 — Revisão final, integração e usabilidade

## Resumo executivo

Revisão realizada sobre o estado local da `main`, preservando as alterações das etapas anteriores. Foram lidos os relatórios 3–6, a documentação de demonstração e os testes das etapas 1–2. A matriz inicial priorizou autenticação, criação/compatibilidade, propostas, persistência profissional e autorização; depois navegação, mensagens e acessibilidade. Não houve redesenho geral.

Foram corrigidos defeitos concretos de sessão, duplicidade de ações do cliente, validação de agendamento, consulta de documentos profissionais, verificação legada e navegação de notificações. **149 testes frontend e 8 testes unitários backend passaram**. O build da API passou sem avisos/erros. O verificador novo executou **66 verificações HTTP GET na API local real**, incluindo acesso entre contas.

O usuário autorizou preparar uma base isolada e testar persistência, sem criar administrador ou modificar a demonstração. A criação foi **bloqueada pelo PostgreSQL com SQLSTATE 42501**. O executável de persistência está preparado e compilado, mas os casos de gravação não foram executados. Não se concederam privilégios nem se usou a demonstração como alternativa.

O navegador integrado funcionou: foram verificadas a tela de login em quatro larguras e as três áreas protegidas redirecionando para login sem sessão. **Não houve revisão visual das páginas autenticadas**, login com senha de demonstração ou validação com leitor de tela. A administração continua sem conta ativa real para leitura administrativa.

Esta entrega conclui a revisão/correções/documentação no escopo executável, com bloqueios explícitos. **Não certifica prontidão para lançamento público.** Persistência real, navegação autenticada e administração precisam das verificações pendentes abaixo.

## A. Auditoria por gravidade

Não foi confirmada uma vulnerabilidade crítica nesta revisão limitada. Isso não comprova ausência de vulnerabilidades; testes de escrita/autorização administrativa e avaliações completas de navegador não foram concluídos.

| ID / gravidade | Onde e como reproduzir | Impacto | Correção ou proposta | Estado |
| --- | --- | --- | --- | --- |
| A01 — Alto | `app.js`: simular falha de rede/503 em `/auth/me` com sessão existente | Logout indevido e perda do contexto por falha temporária | Falhas temporárias oferecem retry, preservam sessão e nós dos formulários; dados protegidos ficam ocultos até confirmar o usuário. 401/403 e conta inativa continuam encerrando a sessão; perfil errado continua redirecionado | Corrigido; regressões simuladas aprovadas |
| A02 — Alto | Detalhe do pedido do cliente: clicar repetidamente em aceitar proposta ou submeter agenda/avaliação enquanto a requisição está pendente | Gravações repetidas ou ações sobre estado desatualizado | Trava compartilhada das ações de escrita, controles do pedido bloqueados durante envio, restauração após erro e bloqueio até recarregar após sucesso | Corrigido; DOM/API simulados. Escrita real bloqueada pela infraestrutura |
| A03 — Alto | Histórico de verificação profissional: abrir link comum de um documento privado | Link não envia Bearer; documento autorizado não abre | Leitor privado compartilhado com a administração: mesma origem/rota validada, Bearer somente no cabeçalho, sem redirecionamento/cache, MIME permitido e URLs temporárias revogadas | Corrigido; testes simulados. Arquivo real do proprietário ainda não validado |
| A04 — Alto | Selecionar municípios homônimos de UFs diferentes; criar pedido ou área profissional | A API compara só o nome; UF visível não elimina ambiguidade e direcionamento incorreto | Proposta de identificação IBGE/UF, migração aditiva e reconciliação explícita de legados | Aberto; requer aprovação de contrato/banco |
| A05 — Alto | Salvar regiões/horários ou passo profissional; primeiro PUT funciona e segundo falha | Persistência parcial, estado repartido entre recursos | Mensagem parcial atual comprovada por dois novos testes integrados de frontend; proposta atômica descrita adiante | Limitação aberta, comunicada; não alterado contrato |
| A06 — Alto | Intervenção administrativa altera status sem atualizar proposta/agenda | Possíveis estados relacionados inconsistentes | Aviso e confirmação mantidos; definir transições administrativas e consistência transacional antes de alterar backend | Aberto; decisão estrutural necessária |
| A07 — Médio | Agendamento do cliente com valor vazio/inválido: `toISOString()` era chamado fora do tratamento de erro | Exceção sem orientação; formulário aparenta não funcionar | Data/horário futuros e válidos verificados antes da conversão; dados preservados e estado permitido conferido | Corrigido; regressões aprovadas |
| A08 — Médio | `verificacao.html?onboarding=1` mostrava cinco etapas e recalculava conclusão separadamente | Contradição com cadastro de três passos; divergência das regras reais | Removido progresso antigo; conclusão consultada no painel real do backend; pendências continuam explícitas | Corrigido; teste simulado. Não promete aprovação de identidade |
| A09 — Médio | Repetir envio de documento ou tentar novamente após upload funcionar e solicitação falhar | Requisições/arquivos duplicados desnecessários | Guarda de envio e cache por arquivo reutilizam upload já concluído; erro de histórico tem retry | Corrigido no frontend. Idempotência completa do servidor segue aberta |
| A10 — Médio | Notificação de pedido do cliente ou verificação do profissional | Clique marcava leitura sem abrir destino útil; Escape não devolvia foco | Rotas existentes por perfil/metadados válidos; Escape fecha e devolve foco | Corrigido; testes simulados; nenhum item real marcado como lido |
| A11 — Médio | Minhas propostas: filtrar/buscar em uma página sem o pedido desejado | Proposta existe, mas não aparece nessa amostra; data de envio indisponível | Limitação local continua explícita; proposta de endpoint global paginado abaixo | Aberto; sem novo contrato implementado |
| A12 — Médio | Resposta perdida depois de pedido/proposta já ter sido gravado | Retry posterior pode repetir operação; guarda do clique não garante idempotência | Conferir listagem antes de repetir; propor chave de idempotência e tratamento de concorrência no servidor | Aberto; mudanças de contrato exigem aprovação |
| A13 — Baixo | Mensagens de propostas, cadastro e administração citavam API/endpoint como orientação principal | Linguagem técnica desnecessária | Textos simplificados sem esconder limites: data indisponível, consulta limitada, alteração não sincroniza dados relacionados | Corrigido pontualmente; sem redesenho |

Bloqueios de validação, separados de defeitos do produto: falta de privilégio para criar base isolada; ausência de administrador ativo; falta de senha de demonstração disponível na sessão para navegação autenticada. Não se alteraram contas para contorná-los.

## B. Correções e arquivos

- `wwwroot/js/app.js`: recuperação de consulta de sessão, preservação do DOM e tratamento de conta inativa.
- `wwwroot/js/pages/cliente-orcamentos.js`: proteção das decisões/agenda/avaliação, datas futuras, motivo/nota/comentário válidos, preservação após erro e exibição do horário no detalhe.
- `wwwroot/js/components/private-document.js`: novo leitor privado reutilizado pelas duas áreas; autorização continua sendo do servidor.
- `wwwroot/js/pages/admin.js`: usa o leitor compartilhado e linguagem mais simples; não muda decisões ou permissões.
- `wwwroot/js/pages/trabalhador-verificacao.js`: documento autenticado, retry de histórico, upload reutilizado, envio único e consulta da conclusão ao backend.
- `wwwroot/js/components/notifications.js`: destinos de cliente/verificação e fechamento por Escape.
- `wwwroot/js/components/professional-editor.js` e `professional-work.js`: mensagens simplificadas, preservando regras/limitações.
- `wwwroot/pages/trabalhador/verificacao.html`: progresso legado removido e leitor compartilhado carregado. `wwwroot/pages/admin/gestao.html` também carrega o leitor. Os demais HTMLs autenticados receberam somente atualização das versões dos scripts efetivamente alterados para evitar cache antigo.
- `Tests/frontend/stage7-integrated.test.cjs`: 13 regressões de sessão, ações do cliente, documentos, conclusão legada e notificações.
- `Tests/frontend/stage4-professional.test.cjs`: dois cenários de salvamento parcial, com preços e intervalos preservados.
- `Tests/frontend/helpers/dom.cjs`: suporte a controles nativos, inserção sem recriar nós e `currentTarget` para simulação integrada.
- Testes `authenticated-layout`, `stage1-regressions`, `stage5-work` e `stage6-admin`: recursos/versionamento, dependência compartilhada e expectativas atualizadas. A regressão de aceitar/recusar agora usa consultas independentes: não exige recusar uma proposta depois que o mesmo pedido já foi aceito/recarregado.
- `Tests/Chamapro.IntegrationChecks`: novo verificador exclusivamente GET para contas fictícias existentes.
- `Tests/Chamapro.PersistenceChecks`: testes de gravação isolada preparados após autorização; build validado, execução dos cenários bloqueada antes da preparação.
- Documentos desta entrega: este relatório, `ETAPA7_ROTEIRO_MANUAL.md` e `ETAPA7_PLANO_PERSISTENCIA.md`.

Nenhum serviço/controller/contrato/backend de produção, esquema ou configuração de produção foi modificado. Não foram alterados preços/horários reais, permissões ou credenciais de contas existentes. Nenhuma migration foi executada, não houve commit/push e nenhuma conta administrativa foi criada.

## C. Matriz de validação preenchida

Estados referem-se somente ao escopo de cada linha. “Aprovado” em um teste simulado não aprova automaticamente persistência real ou aparência.

| Funcionalidade | Perfil | Tipo | Resultado | Evidência | Pendência |
| --- | --- | --- | --- | --- | --- |
| Suíte frontend anterior e regressões finais | Todos | Unitário/integração frontend simulada | Aprovado | `node --test Tests/frontend/*.test.cjs`: 149/149, sendo 15 novos | Não comprova gravação/visualização real |
| Regras/DTOs, preços zero, orçamento pt-BR, transições e motivo de erro | Cliente/profissional | Unitário backend | Aprovado | `Chamapro.Tests`: 8/8 | Integração de escrita bloqueada |
| Build API e verificadores novos | Todos | Compilação | Aprovado | Saídas/intermediários externos; zero avisos/erros no build final | — |
| Cadastro/login por senha e recuperação | Cliente/profissional | API/navegador real | Não executado | Sem senha demo disponível; sem modificar senhas/contas | Casos preparados na base isolada; recuperação de senha permanece manual |
| Consulta de sessão, retry, perfil incorreto e inativo | Todos | Integração frontend simulada | Aprovado | `stage7-integrated`: erro temporário conserva sessão/nós; 401/inativo encerram; perfil incorreto redireciona | Repetir com rede real no navegador autenticado |
| Guard de página sem sessão | Todos | Navegador real | Aprovado | Abrir cliente/painel, trabalhador/painel e admin/gestao levou a login | Não valida troca de perfil autenticado no navegador |
| Login 360, 390, 768, 1366 px | Todos | Navegador real/visual | Aprovado | Capturas na sessão; `scrollWidth` igual a cada largura; nenhum erro no console consultado | Somente login, não as áreas autenticadas |
| Uma transição de Tab no login | Todos | Navegador real | Aprovado | E-mail → “Esqueceu a senha?”, foco observado no DOM | Ciclo completo, toque e leitor não executados |
| Responsividade de pedidos, perfil, trabalhos e administração | Todos | Navegador real | Não executado | Sem sessão autenticada/administrador | Executar nas quatro larguras; não substituir por media queries |
| Contraste e leitor de tela completos | Todos | Manual | Não executado | Não houve ferramenta/leitor executado para esses cenários | Roteiro manual |
| Criação automática/manual, modos, seletores, cidade/UF, anexos, voltar/avançar e favorito compatível | Cliente | Integração frontend simulada | Aprovado | Regressões das etapas 1–3 mantidas | POST real e toque bloqueados/não executados |
| Mensagem `SEM_PROFISSIONAIS_COMPATIVEIS` | Cliente | Unitário backend + frontend simulado | Aprovado | Fábrica de erro e orientação por código continuam aprovadas | Não executar POST no seed para reproduzir; caso isolado preparado |
| Pedidos, propostas, histórico e favoritos próprios | Cliente | API real, somente GET | Aprovado | `IntegrationChecks`: cinco pedidos com detalhe/propostas/histórico; um favorito lido | Escritas pendentes |
| Recurso de outro cliente/profissional não destinatário | Cliente/profissional | API real, somente GET | Aprovado | 404 para detalhe/histórico alheios; propostas do outro cliente recusadas | Não comprova todos os endpoints de escrita |
| Permissões por perfil e sem autenticação | Todos | API real, somente GET | Aprovado | 66 verificações no novo runner; 401/403 confirmados; `AdminChecks` repetiu seis rotas | Não equivale a pentest completo |
| Decisão de proposta, agenda e avaliação sem duplicidade/inválidos | Cliente | Integração frontend simulada | Aprovado | Novas regressões de ação pendente, sucesso, erro, data e estado | Persistência real bloqueada |
| Cadastro e perfil: preços, regiões, horários complexos | Profissional | Simulado + DTOs reais lidos | Aprovado | `ProfileChecks`: dois serviços, duas regiões, sete intervalos, 19 categorias; regressões de zero/múltiplos intervalos | PUT/recarga reais bloqueados |
| Salvamento parcial de regiões/horários | Profissional | Integração frontend simulada | Aprovado | Dois testes novos: primeiro PUT bem-sucedido e segundo falho; mensagem parcial e retry | Transação atômica não existe; caso real bloqueado |
| Oportunidades/propostas/trabalhos/agenda | Profissional | API real, somente GET | Aprovado | `WorkChecks`: seis filtros, segunda página, sete detalhes/históricos, quatro propostas, três trabalhos escolhidos, uma agenda | Escritas/visual autenticado pendentes |
| Ações somente do escolhido e transições permitidas | Profissional | Simulado + regras unitárias | Aprovado | Etapa 5 preservada; nenhuma ação de escolhido para concorrente | API de escrita real ainda não testada nesta etapa |
| Documento privado, mesma origem, MIME, cabeçalho, cancelamento e consulta profissional | Profissional/admin | Integração frontend simulada | Aprovado | Leitor compartilhado; testes das etapas 6–7 | Arquivo real não foi enviado/aberto |
| Rota privada sem sessão | Todos | API real, GET | Aprovado | 401 com nome de arquivo inexistente no caminho protegido | Não comprova acesso ao arquivo real pelo dono/admin |
| Navegação e decisões administrativas | Admin | DOM/API simulados | Aprovado | 38 testes da etapa 6 preservados com leitor compartilhado | Não houve conta administrativa real nem navegador admin |
| Leituras autenticadas administrativas | Admin | API real | Bloqueado | `AdminChecks`: não há administrador ativo; execução parcial, não sucesso completo | Provisionamento exige autorização específica, não concedida |
| Provisionamento oficial | Admin | Auditoria de código | Aprovado | `AdminBootstrapService`: configuração explícita, sem promoção/reativação implícita | Nenhuma execução/provisionamento realizado |
| Persistência isolada | Cliente/profissional | API real com banco isolado | Bloqueado | `PersistenceChecks --prepare`: SQLSTATE 42501 ao criar nova base | Base vazia e acesso precisam ser disponibilizados; autorização do usuário já recebida |
| Ações administrativas reais | Admin | Integração com escrita | Não executado | Sem conta e sem autorização para criá-la | Autorização específica e base descartável |
| Links HTML estáticos locais | Todos | Auditoria automatizada | Aprovado | 17 links locais `.html`, nenhum destino ausente; recursos das 13 páginas autenticadas aprovados | Não valida automaticamente links gerados por dados |
| Notificações para pedido/verificação e Escape | Cliente/profissional | Simulado | Aprovado | Destinos por perfil/metadados e retorno de foco; nenhum PATCH real | Clique real altera leitura e aguarda teste autorizado |

### Comandos/verificadores não executados

- `Chamapro.OrderChecks`: mesmo sem `--create`, faz POST para reproduzir incompatibilidade. Não foi executado sobre a demonstração nesta etapa.
- `Chamapro.DatabaseChecks`: exige banco vazio e consulta a configuração da demonstração; é incompatível com o seed existente. Não foi executado nem “corrigido” apagando dados.
- Seed, migrations, bootstrap e alterações administrativas: não executados.
- `PersistenceChecks --run`/`--prepare-empty`: não executados; a preparação inicial foi recusada. O teste está preparado, não aprovado por execução.

Os builds usam diretórios autorizados de validação fora do checkout. Os verificadores somente leitura usam `TokenService` em memória para contas fictícias existentes; isso não testa login por senha nem altera autenticação do aplicativo. Nenhum token, senha ou conexão foi impresso em relatório.

## D. Segurança defensiva e limites

Foram inspecionados o JWT (assinatura, emissor, audiência e validade), middleware de usuário ativo, atributos de autorização, escopo de pedidos/propostas/histórico/favoritos/notificações, documentos privados, validações dos DTOs e upload por tamanho/assinatura de conteúdo. `DevAuthenticationHandler` existe no código, mas não está registrado no pipeline JWT atual; não foi habilitado para testes.

O servidor continua responsável por autorização; esconder formulário não substitui essa proteção. A navegação administrativa não consulta rotas exclusivas de cliente/profissional. O helper de documento só envia JWT à rota privada da mesma origem, recusa redirecionamento e não transforma HTML em documento executável. JWT de sessão não foi colocado em URL. A recuperação de senha usa token específico no link por desenho existente; esse fluxo não foi validado integralmente e não foi alterado.

Não houve teste invasivo externo, varredura de senhas, mudança de segredos, upload de documento pessoal ou operação sobre registros reais. A auditoria não inclui análise especializada de arquivos maliciosos, corrida de gravações, exaustão de recursos ou todos os fluxos de recuperação. O contrato atual disponibiliza endereço do pedido aos destinatários autorizados; eventual redução antes da contratação exige decisão de produto/regra, não um falso bloqueio apenas visual.

## E. Propostas técnicas futuras — não implementadas

### Municípios IBGE e UF

Fluxos afetados: conta/localização do cliente (`AtualizarClienteRequest`, cidade armazenada em `Usuario`); preenchimento da criação de pedido (`PedidoCriarRequest`); áreas do cadastro/perfil profissional (`AreaAtendimentoRequest`); busca manual/automática e reutilização de favorito (`BuscaProfissionaisQuery` e repositórios de compatibilidade); exibição pública/administrativa, filtros textuais e dados legados do seed.

Hoje os DTOs recebem somente `Cidade`; os repositórios comparam texto, em geral com trim/caixa e sem UF. A seleção IBGE local conserva código/UF apenas no navegador. Mostrar “Cidade — UF” não corrige o vínculo persistido.

Proposta aditiva: catálogo de municípios identificado pelo código IBGE; referência nullable em usuário/localização, pedido e área de atendimento; UF obtida do município, sem aceitar código e UF contraditórios. Manter texto histórico e contratos antigos durante transição aprovada. Novos formulários enviariam código; respostas devolveriam código/nome/UF e estado de reconciliação. Compatibilidade passaria a comparar identificadores quando disponíveis.

Migração segura proposta: inventário e backup verificados; adição sem apagar colunas; backfill somente quando houver evidência inequívoca. Homônimos, grafias antigas e dados sem UF permanecem pendentes de confirmação, nunca associados à primeira coincidência. Contas antigas recebem reconciliação assistida. Histórico de pedidos não é reescrito silenciosamente. Comparar resultados antigos/novos em modo de observação, liberar gradualmente e retirar fallback textual somente após medir pendências e aprovar a nova regra. Índices/constraints e eventual unicidade de área devem ser revisados antes da migration. Nenhuma dessas alterações foi executada.

### Listagem global de propostas

A limitação foi confirmada em `OrcamentosController`/DTOs: não há consulta de propostas do profissional; GET de propostas do pedido é exclusivo do cliente; o detalhe traz só `meuOrcamento` mais recente e não inclui `CriadoEm`, embora a entidade o possua. A amostra de dez pedidos pode omitir propostas relevantes, e não permite contagem global de aguardando resposta.

Propor, após aprovação, GET `/orcamentos/me`, exclusivo PROFISSIONAL e escopado pelo usuário autenticado, sem aceitar outro profissional por parâmetro. Filtros reais por situação da proposta, situação do pedido, validade, período e busca limitada; `pagina/tamanho` com teto 50; ordenação estável por criação e ID; total e itens do mesmo escopo. Retornar proposta, valor/prazo/condições, criação, validade/expiração derivada, resumo permitido do pedido e vínculo com trabalho escolhido. Reenvio hoje atualiza proposta ativa sem alterar a criação: não chamar essa data de “último envio”; um campo de atualização exigiria proposta específica de esquema/semântica.

O frontend substituiria a varredura por página de pedidos pela paginação própria, mantendo todos os estados e mensagens de erro. Nenhum novo estado de negócio ou contrato foi criado nesta revisão.

### Salvamento atômico e concorrência

Operações afetadas: passo inicial salva perfil e serviços; regiões salvam áreas e distância/modalidades no perfil; horários salvam intervalos e urgência no perfil; fotos fazem upload antes de vincular URLs; contato tem recurso próprio. Cada PUT é transacional isoladamente, não existe transação compartilhada entre chamadas. Um erro/rede perdida pode deixar apenas a primeira parte persistida. Os testes novos comprovam que a mensagem parcial é apresentada quando o primeiro recurso realmente muda, sem apagar rascunhos/preços/intervalos.

Propor endpoint agregado de cadastro/perfil, validando todas as partes antes de uma única transação no servidor, com escopo do usuário, arrays omitidos diferentes de remoção explícita e controle de versão/concorrência. Não usar rollback externo de cliente HTTP. Arquivos precisam de estágio/vínculo e política de expiração separada; transação SQL não desfaz filesystem ou e-mail. Uma chave de idempotência pode proteger criação/reenvio após resposta perdida. Nenhuma mudança de endpoint, transação complexa, limpeza de arquivo ou esquema foi implementada.

## Próximos passos e condição de liberação

1. Responsável pelo PostgreSQL disponibiliza a base isolada vazia e acesso; executar/revisar o teste preparado conforme [plano de persistência](ETAPA7_PLANO_PERSISTENCIA.md). A autorização para essa rodada já foi recebida; não implica autorização para administrador.
2. Com senha demo já configurada pelo responsável, executar o [roteiro manual](ETAPA7_ROTEIRO_MANUAL.md), incluindo páginas autenticadas nas quatro larguras, teclado/toque e leitor de tela.
3. Se necessário, obter autorização específica para provisionar administrador de teste pelo procedimento oficial e validar administração em dados descartáveis.
4. Aprovar separadamente municípios, propostas globais, atomicidade/idempotência e consistência de intervenção administrativa. Essas limitações não foram ocultadas nem resolvidas apenas por texto/CSS.

Sem essas verificações críticas, não declarar o sistema inteiro validado nem liberado para uso público. Não foi iniciada nova etapa ou funcionalidade grande fora desta revisão.
