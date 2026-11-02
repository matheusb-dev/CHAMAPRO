# Etapa 7 — Roteiro manual

## Preparação e limites

Use a senha que o responsável já configurou para as contas fictícias. Nenhuma senha é fornecida aqui. Não execute `-ConfigurePassword`, seed, migrations ou bootstrap para seguir a parte somente leitura. Se não houver senha configurada, interrompa o login e peça ao responsável a preparação autorizada; não altere contas para contornar isso.

Aplicação de demonstração: `http://localhost:5088`. Os dados podem ter sido alterados em testes anteriores; confira o estado que realmente aparece. Propostas antigas podem estar com validade encerrada. Não reexecute seed para tentar restaurar cenários.

**As ações de escrita abaixo só devem ocorrer em registros descartáveis da base isolada autorizada. A preparação dessa base está bloqueada por falta de permissão CREATE DATABASE. Não executar essas ações na demonstração enquanto esse bloqueio existir.**

## A. Demonstração: navegação e consulta, sem gravar

| Perfil/conta fictícia | Página e ação | Resultado esperado / observar | Altera dados? |
| --- | --- | --- | --- |
| Sem sessão | Abrir `/pages/cliente/painel.html`, depois profissional/painel e admin/gestao | Redirecionamento para login; nenhum dado protegido exibido | Não |
| Cliente `cliente01@v1.demo.chamapro.invalid` | Entrar normalmente; abrir Início | Pedidos/avisos reais; erros não aparecem como ausência de pedidos | Login cria sessão, sem alterar pedidos |
| Cliente 01 | Pedir um serviço; pesquisar `tecnico` e escolher categoria | Acentos preservados, lista compacta, escolha por setas/Enter e clique | Não, antes de confirmar |
| Cliente 01 | Descrever serviço, escolher cidade/UF e informar bairro/endereço | Nome canônico com acento; cidade trocada limpa localização antiga; aviso quando há homônimos | Não |
| Cliente 01 | Alternar imediato/agendado; informar data futura; voltar/avançar | Campos e anexos continuam presentes; não enviar arquivos nem confirmar | Não, se parar antes da confirmação |
| Cliente 01 | Seleção manual; buscar; voltar ao modo automático; abrir revisão | Lista respeita compatibilidade. Automático não conserva profissionais escolhidos. Não clicar Confirmar pedido | Busca não; confirmar seria escrita |
| Cliente 01 | Meus pedidos; situação; próxima/anterior; abrir um pedido | Página correta, detalhe do pedido escolhido, histórico e próximos passos coerentes | Não |
| Cliente 01 | Consultar propostas recebidas; abrir perfil | Preço/prazo/validade e situação claros; proposta expirada sem botão de escolha. Não aceitar/recusar | Não |
| Cliente 01 | Pedido agendado ou concluído | Horário visível; avaliação só após conclusão; nenhum formulário deve enviar nesta parte | Não |
| Cliente 01 | Meus profissionais; Ver profissional; Pedir outro serviço | Favorito chega à criação; seleção depende de compatibilidade atual. Não remover/favoritar/confirmar | Não |
| Cliente `cliente05@v1.demo.chamapro.invalid` | Início, pedidos e favoritos | Estados vazios explicativos; não inventar atividades | Não |
| Profissional `profissional01@v1.demo.chamapro.invalid` | Cadastro e Meu perfil; expandir serviços e horários | Preços zero quando existentes e todos os intervalos preservados. Não salvar nesta parte | Não |
| Profissional 01 | Adicionar região/categoria somente como rascunho; voltar etapas | Valores devem permanecer ao voltar etapas. Não sair da página nem salvar nesta parte | Não, sem Salvar |
| Profissional 01 | Encontrar serviços; busca nesta página; paginação; abrir detalhe | Categoria, local, necessidade e situação compreensíveis; não demonstrar interesse/recusar/enviar | Não |
| Profissional 01 | Minhas propostas; filtros e Ver trabalho | Filtro local explicitado; datas de envio não inventadas; só trabalho escolhido com ações próprias | Não |
| Profissional `profissional03@v1.demo.chamapro.invalid` | Meus trabalhos → Em andamento | Acesso ao trabalho e ação de concluir somente se escolhido; não clicar Concluir | Não |
| Profissional `profissional09@v1.demo.chamapro.invalid` | Início, oportunidades e verificação | Pendências reais e estado vazio; não apresentar como apto a receber qualquer pedido | Não |
| Profissional | Histórico de verificação, Consultar meu documento | Documento real: consulta autenticada e link temporário; documento fictício do seed pode não existir e deve gerar mensagem segura | GET não altera dados |
| Cliente/profissional | Abrir notificações e fechar por Escape | Fechar devolve foco ao botão. Não clicar item/Marcar todas nesta parte, pois isso altera leitura | Abrir/fechar não; marcar/clicar altera leitura |
| Administrador existente autorizado | `/pages/admin/gestao.html`, todas as seções | Não executado: não há administrador ativo. Não criar/promover um para esse roteiro sem autorização específica | Consultas não; decisões sim |

Não há promessa de que toda conta fictícia preserve o cenário original do seed; escolha um pedido com o estado apropriado que ainda exista ou use a futura fixture isolada.

## B. Larguras, teclado e acessibilidade

Para cada página acessível acima, repetir em **360, 390, 768 e 1366 px**. Nesta entrega, somente o login foi aberto nessas quatro larguras; o restante continua pendente.

1. Conferir se há rolagem horizontal e qual elemento a causa. Textos longos, cartões, anexos e horários devem caber na tela.
2. No celular, abrir/fechar menu; seguir as ações principais; verificar tamanho dos campos, botões e lista interna dos seletores.
3. Navegar por Tab/Shift+Tab. O foco deve permanecer visível e seguir ordem compreensível. Conferir rótulos e mensagens de erro.
4. Nos seletores, testar setas, Home/End, Enter, Escape, Tab, clique/toque, busca sem resultado e erro/retry. A seleção deve permanecer ao fechar e voltar etapas.
5. Abrir/fechar detalhes, confirmações e notificações por teclado; conferir retorno de foco. Dismiss de confirmação não deve enviar decisão.
6. Com leitor de tela real, conferir títulos, campos, progresso, opção ativa e anúncios de erro/carregamento. Esse teste não foi executado automaticamente.
7. Medir contraste de textos, estados desabilitados e foco. Não considerar o tema escuro ou a presença de CSS como aprovação de contraste.

## C. Escritas: somente ambiente isolado, após desbloqueio

Os casos e o procedimento estão em [ETAPA7_PLANO_PERSISTENCIA.md](ETAPA7_PLANO_PERSISTENCIA.md). Usar contas `[TESTE ETAPA7]` do ambiente isolado, nunca substituir os e-mails da demonstração nos testes de escrita.

| Perfil | Clique/ação | Resultado esperado | Altera dados? |
| --- | --- | --- | --- |
| Profissional descartável | Salvar perfil; recarregar | Apenas dados editados mudam; fotos e demais campos permanecem | Sim |
| Profissional descartável | Remover/reinserir categoria; salvar preço mínimo zero; recarregar | Categorias e faixas corretas, inclusive zero | Sim |
| Profissional descartável | Alterar uma região e um dos múltiplos intervalos; salvar; recarregar | Somente itens editados mudam; demais intervalos/flags/segundos permanecem | Sim |
| Profissional descartável | Aplicar horário comum; cancelar e depois confirmar | Cancelar preserva horários. Confirmar afeta somente os dias selecionados | Sim ao confirmar e salvar |
| Cliente descartável | Confirmar pedido automático/manual | Um envio por clique, redirecionamento ao pedido criado, destinatários compatíveis | Sim |
| Cliente descartável | Confirmar sem profissionais compatíveis | Mensagem explica serviço/local/horário e como tentar novamente; nenhum pedido criado | Requisição de escrita recusada |
| Profissional descartável | Preparar proposta, preencher, Enviar; clique repetido | Interesse não equivale a contratação; uma requisição em andamento; erro mantém campos | Sim |
| Cliente descartável | Escolher proposta válida; confirmar | Profissional escolhido e concorrentes atualizados; próximo passo é agendar | Sim |
| Cliente/profissional escolhido | Agendar/remarcar/confirmar; profissional inicia e conclui | Cada transição persiste; profissional não escolhido é recusado | Sim |
| Cliente descartável | Avaliar serviço concluído; recarregar; tentar repetição | Avaliação salva uma vez; repetição recusada | Sim |
| Cliente descartável | Favoritar/desfavoritar; recarregar; consultar outra conta | Mudança pertence somente à conta atual | Sim |
| Profissional descartável | Enviar arquivo fictício; consulta por dono e outro usuário | Dono lê; outro usuário é recusado; erro na solicitação permite retry sem novo upload desnecessário | Sim no upload/envio |
| Administrador de teste | Decisões de verificação/conta/categoria/denúncia/pedido | **Bloqueado: criação administrativa não autorizada.** Exige autorização específica adicional e registros descartáveis | Sim |

Após cada escrita, conferir a nova leitura; uma mensagem de sucesso sozinha não comprova persistência. Não apagar base, registros ou arquivos para limpar testes sem autorização.
