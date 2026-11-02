# Etapa 5 — Experiência operacional do profissional

## Auditoria e decisões

A fila antiga misturava oportunidades, pedidos atribuídos a outros profissionais e trabalhos, sem uma área de propostas. Carregava apenas os primeiros 50 pedidos. O painel reunia seis indicadores e editores de agenda, duplicando responsabilidades dos detalhes. Interesse do destinatário podia parecer contratação. Os detalhes consideravam qualquer profissional escolhido como suficiente para mostrar ações, sem comparar com o perfil atual. Formulários não impediam envio duplicado, e erros não ofereciam nova tentativa. Notificações marcavam leitura sem abrir o pedido relacionado.

A auditoria foi apresentada antes das alterações. A implementação preserva os controllers, DTOs, serviços, autorização, elegibilidade e transições do backend.

## Navegação e início

As três áreas principais reutilizam `fila-chamados.html`:

- `#oportunidades`: Encontrar serviços; também é o destino de links antigos sem hash.
- `#propostas`: Minhas propostas.
- `#trabalhos`: Meus trabalhos.

Início, cadastro, perfil e verificação continuam acessíveis. Nenhuma rota foi removida. Detalhes mantêm `detalhes-chamado.html?id=...`.

O início prioriza cadastro incompleto, identidade não verificada, trabalhos em andamento, serviços aceitos para agendar, próximos agendamentos e oportunidades. Contagens vêm do painel. O total histórico de propostas é identificado como total, incluindo encerradas; não é apresentado como propostas aguardando resposta. Interesses demonstrados não são tratados como trabalhos contratados. A identidade verificada e o cadastro completo não são apresentados como garantia de receber qualquer pedido: compatibilidade continua dependendo das regras reais.

Os serviços aceitos destacados vêm da primeira página de pedidos ACEITO, filtrados pelo profissional escolhido, com acesso às demais páginas. A agenda usa `/agendamentos/me`, cujo limite existente é 100; o início destaca até três agendamentos futuros dessa consulta.

## Oportunidades, propostas e trabalhos

Cada página consulta até 10 pedidos e seus respectivos detalhes. Os cartões apresentam serviço, categoria, cidade/bairro, descrição resumida, necessidade imediata/data e próxima ação. A lista de oportunidades informa se o profissional já demonstrou interesse ou enviou uma proposta. Não acrescenta dados pessoais aos cartões.

O filtro de situação é enviado ao backend. Busca por título, categoria, cidade e bairro e seleção de situação da proposta são explicitamente limitadas à página atual. A paginação percorre os pedidos recebidos, não uma contagem fictícia de propostas ou trabalhos. Uma página pode não conter itens da área selecionada; o usuário pode continuar para a próxima. Falhar ao carregar qualquer detalhe invalida a consulta e mostra erro/retry, sem omitir silenciosamente registros.

Propostas mostram a última proposta retornada por pedido, valor, prazo, validade e situação. ENVIADO é apresentado como aguardando resposta; ACEITO, RECUSADO e NAO_ESCOLHIDO mantêm distinção. Validade encerrada é uma apresentação derivada de `validoAte`, sem criar status persistido. Pedido cancelado é comunicado separadamente. Propostas aceitas oferecem acesso direto ao trabalho quando o perfil atual foi escolhido.

Meus trabalhos inclui somente pedidos cujo `profissionalId` corresponde ao perfil atual. Próximos preserva dois filtros distintos: ACEITO (combinar data) e AGENDADO. EM_ANDAMENTO, CONCLUIDO e CANCELADO continuam acessíveis separadamente. Não há fusão dos estados persistidos.

## Propostas e ações

O formulário exige somente valor e prazo estimado, obrigatórios no DTO existente. Mensagem/condições é opcional. O valor aceita até duas casas decimais, de 0,01 a 99.999.999,99. A validade padrão de sete dias fica com o backend.

Primeiro demonstrar interesse chama o endpoint existente de aceitar pedido; isso libera proposta e não contrata o serviço. Somente depois é exibido o formulário. Uma proposta existente pode ser revisada/reenviada por ação explícita, preservando a capacidade de reenvio do backend.

Os detalhes centralizam interesse, recusa, proposta, agendamento, confirmação/remarcação/cancelamento de agenda, início, conclusão, cancelamento do pedido e histórico. WhatsApp continua dependendo do endpoint autorizado e das preferências existentes.

Ações de trabalho verificam o profissional escolhido. Início é oferecido em AGENDADO e conclusão em EM_ANDAMENTO. Cancelamento de pedido é oferecido em ACEITO/AGENDADO e exige motivo de 5 a 200 caracteres. Cancelar agenda informa que também cancela o pedido. Confirmações e datas futuras são validadas sem substituir as validações reais da API. Ações importantes pedem confirmação; gravações em andamento desabilitam os controles e impedem duplicidade. Falhas preservam dados e exibem o erro da API. Sucesso recarrega o estado; se a gravação funcionar e o GET seguinte falhar, novas gravações ficam bloqueadas com link para recarregar.

## Notificações

O sistema existente é preservado. Notificações profissionais com `dados.pedidoId` inteiro positivo abrem os detalhes correspondentes depois de marcar leitura. JSON inválido ou metadados ausentes não produzem destinos inventados. A mudança não cria eventos nem aplica rotas profissionais a clientes/admin. Falhas de leitura/marcação são comunicadas, e carregamento possui retry. Nenhuma notificação real foi marcada como lida durante os testes.

## Limitações estruturais e proposta futura (não implementada)

1. GET `/pedidos` filtra apenas por status, com paginação de destinatários. Não oferece filtros por categoria, localização, urgência, profissional escolhido ou existência/situação de proposta. A solução atual é limitada e explícita por página; filtros globais corretos exigiriam endpoints/consultas específicos, sujeitos a aprovação.
2. Não há GET de propostas do profissional. GET de orçamentos do pedido é exclusivo do cliente. O frontend usa apenas `meuOrcamento` nos detalhes autorizados, sem tentar contornar essa autorização.
3. `OrcamentoResponse` não informa data de envio. A interface comunica essa ausência, mostra validade e não usa criação do pedido como data da proposta. O DTO também só permite reconstruir a última proposta por pedido, não todo o histórico de reenvios.
4. O painel informa total histórico de propostas e interesses, sem contagem global de propostas pendentes nem de contratos ACEITO. Não se inventam esses indicadores. O destaque de aceitos cobre a primeira página consultada.
5. `/agendamentos/me` limita a 100 agendamentos ativos, sem paginação. Não é usado como histórico de trabalhos.
6. As regras de agenda e pedido diferem: cancelar uma agenda ativa pode cancelar o pedido, inclusive quando a transição direta de pedido não permite cancelamento em andamento. A interface mantém os endpoints e explicita o efeito; nenhuma regra foi alterada. Uma futura unificação precisa de aprovação e testes backend.
7. Uma página requer até 11 GETs (lista e 10 detalhes). Não há varredura ilimitada. Um endpoint agregado reduziria requisições e permitiria paginação própria das três áreas.

## Arquivos da etapa

- `wwwroot/js/components/professional-work.js`: novo módulo operacional compartilhado.
- `wwwroot/js/pages/trabalhador.js`: inicialização de lista/detalhes, substituindo implementação operacional antiga.
- `wwwroot/js/pages/painel-trabalhador.js`: inicialização do início compartilhado.
- `wwwroot/js/components/sidebar.js`: áreas profissionais e seleção ativa por hash.
- `wwwroot/js/components/notifications.js`: destino profissional e tratamento de falhas/retry.
- `wwwroot/pages/trabalhador/painel.html`, `fila-chamados.html`, `detalhes-chamado.html`: contêineres e carregamento do módulo.
- Os outros dez HTMLs autenticados receberam somente atualização de versão dos scripts compartilhados sidebar/notifications para evitar cache de navegação/comportamento antigo: cliente `painel`, `criar-chamado`, `orcamentos`, `favoritos`, `perfil-profissional`, `perfil`; profissional `cadastro-profissional`, `perfil`, `verificacao`; admin `gestao`. Nenhum fluxo administrativo, cadastro ou cliente foi refatorado.
- `Tests/frontend/stage5-work.test.cjs`: regressões da etapa.
- `Tests/frontend/authenticated-layout.test.cjs`: novas versões e quantidade de links profissionais.
- `Tests/Chamapro.WorkChecks/Chamapro.WorkChecks.csproj` e `Program.cs`: verificador local somente leitura.
- `docs/ETAPA5_TRABALHO_PROFISSIONAL.md`: este relatório.

## Validação

- `node --test Tests/frontend/*.test.cjs`: 96 testes aprovados, sendo 41 novos nesta etapa; inclui as regressões anteriores de pedidos, favoritos, preços e múltiplos intervalos. DOM e API simulados, não navegador real.
- Build da API: aprovado, zero avisos/erros. Intermediários e saída gerados fora do repositório, no diretório autorizado de validação.
- Testes backend existentes: 8/8 aprovados, também com saída externa.
- WorkChecks: build aprovado e execução contra API real local aprovada. Seis filtros de status, segunda página, sete detalhes e históricos autorizados, quatro propostas e três pedidos escolhidos; leitura de agenda, notificações, perfil e painel. Apenas GET e consulta local de conta fictícia para autenticação do teste, sem expor token/credenciais/dados pessoais. A conexão precisou executar fora do sandbox por restrição de acesso local.
- Computer-use foi tentado, mas interrompido porque não conseguiu determinar a URL atual do Chrome com confiança. Nenhuma página foi validada visualmente nesta etapa. Nenhuma gravação real de pedido/proposta/agenda/notificação foi executada; testes de escrita são simulados.

## Verificação manual pendente

1. Conferir início com cadastro incompleto, verificação pendente e trabalhos em cada estado.
2. Navegar as três áreas, voltar, abrir links antigos e testar seleção ativa do menu no desktop/celular.
3. Percorrer várias páginas, usar filtros e busca local, abrir detalhes e conferir datas/horários.
4. Em ambiente isolado, testar interesse, recusa, proposta válida, reenvio, erros de negócio, preservação de campos e clique duplicado.
5. Em registros descartáveis, testar agendamento, confirmação, remarcação, início, conclusão e cancelamento com os respectivos efeitos reais.
6. Conferir WhatsApp autorizado, histórico, notificações com/sem pedido e navegação por teclado/leitor de tela.
7. Conferir responsividade e identidade visual; os testes automatizados não comprovam aparência.

Trabalho realizado na main, preservando alterações locais anteriores. Sem commit, push, migrations, alteração de credenciais/User Secrets ou dados do banco. Nenhum arquivo de backend foi alterado. Arquivos bin/obj que já estavam modificados não foram editados nesta etapa. Etapa 6 não iniciada.
