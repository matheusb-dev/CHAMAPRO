# Etapa 6 — Organização e simplificação da administração

## Auditoria: funcionalidades reais

A única página administrativa existente é `wwwroot/pages/admin/gestao.html`. A API oferece:

| Área | Leituras e ações existentes | Autorização e limites |
| --- | --- | --- |
| Início | GET `/admin/relatorios` | ADMIN; contagens globais reais e destaques limitados a cinco categorias/profissionais |
| Usuários | GET `/admin/usuarios`; PATCH `/admin/usuarios/{id}/status` e rota legada `/bloquear` | ADMIN; busca por nome/e-mail, perfil e situação; até 200 resultados, sem paginação; alteração da própria conta é recusada |
| Pedidos | GET `/admin/pedidos`; PATCH `/admin/pedidos/{id}/status` | ADMIN; lista sem paginação; alteração direta de status; não há endpoint administrativo de detalhe |
| Verificações | GET `/verificacoes`; PATCH `/verificacoes/{id}/avaliar` | ADMIN; lista pendentes, em análise e documentação pendente; não lista decisões encerradas |
| Documento privado | GET `/arquivos/privados/{nome}` | Autenticado; servidor exige propriedade do arquivo ou ADMIN; resposta privada sem cache |
| Denúncias | GET `/denuncias`; PATCH `/denuncias/{id}/resolver` | ADMIN; fila pendente/em análise; observação opcional de até 2.000 caracteres |
| Categorias | GET `/categorias`; POST/PUT/DELETE nas rotas existentes | Leitura pública de ativas; gravações ADMIN; DELETE desativa quando há vínculos ou exclui quando não há |
| Auditoria | GET `/admin/auditoria` | ADMIN; limite configurável até 200; interface consulta as últimas 50 ações |

O perfil público `/profissionais/{id}` é consultado apenas para complementar nome, serviços e regiões na fila de verificação. Não se usam endpoints exclusivos de cliente/profissional para simular acesso administrativo. O perfil público só existe para contas ativas e não informa todos os requisitos do cadastro.

Foram revisados `AdminController`, `VerificacoesController`, `DenunciasController`, `CategoriasController`, `ArquivosController`, a base `[Authorize]`, o guard de páginas em `app.js` e os DTOs correspondentes. As permissões continuam no servidor. Nenhum backend, contrato, credencial, regra ou esquema foi alterado.

## Problemas encontrados

- Toda a administração era carregada ao abrir uma única página longa, incluindo módulos que o administrador não havia escolhido.
- Aprovação/rejeição, bloqueio/reativação e decisões de denúncias não tinham confirmação adequada. Cancelar o prompt de observação ainda podia executar a decisão.
- Não havia proteção consistente contra cliques repetidos, recuperação de contexto e novas tentativas de carregamento.
- Verificações eram identificadas principalmente por IDs; faltava contexto do profissional.
- Documento privado era aberto como link comum, sem o cabeçalho de autenticação exigido pela API.
- Usuários tinham teto de 200 sem aviso. Pedidos eram truncados visualmente em 50, mesmo quando a API retornava mais.
- O texto “Desativar categoria” omitia a possibilidade de exclusão quando não existem vínculos.
- Auditoria e relatório de profissionais eram adicionados por manipulação estrutural duplicada no carregamento do script.
- Mensagens técnicas de falha podiam ser exibidas diretamente.

## Navegação e página inicial

A rota `gestao.html` foi preservada. A navegação interna organiza Início, Verificações, Denúncias, Usuários, Pedidos, Categorias e Atividade administrativa. Não há módulos vazios de configurações. Os hashes antigos, como `#verificationList`, `#usersList`, `#reportsList`, `#adminOrders` e `#categoryForm`, continuam abrindo a seção correspondente. Nenhum componente compartilhado ou fluxo de cliente/profissional foi alterado.

O início responde “O que precisa da sua atenção agora?” com as duas filas existentes. A contagem de verificações representa **solicitações**, não pessoas únicas. As duas APIs de fila retornam todos os registros daquele escopo, sem `Take` no backend atual. As informações globais vêm de `/admin/relatorios` e ficam em uma seção recolhível. Não se inferem contagens globais de pendências de usuários, pedidos problemáticos ou falhas operacionais.

Cada consulta tem carregamento, vazio, falha e nova tentativa. Respostas 401 e 403 são diferenciadas; 403 não é apresentado como ausência de registros. Falhas de um cartão do início não apagam os demais.

## Verificação de profissionais

A fila mostra nome, serviços e regiões quando o perfil público está disponível, além de situação, documento e data. Os perfis são consultados somente para os dez registros visíveis. Se um perfil não puder ser lido, a solicitação permanece visível com aviso explícito. A busca nesta fila é por tipo de documento, não uma falsa busca global por nomes de perfis ainda não consultados. Os filtros correspondem exatamente aos três estados retornados pelo endpoint.

Detalhes usam `details/summary` nativos. Apresentam as informações fornecidas, observação e ações existentes: iniciar análise, aprovar, solicitar correção e rejeitar. Não são criados requisitos documentais nem uma conclusão de cadastro que a API não informa. Observação continua opcional, com limite de 500 caracteres.

A confirmação identifica solicitação/profissional e explica o efeito: há notificação e registro; aprovação pode verificar a identidade, enquanto rejeição/correção não eliminam outras aprovações existentes. Durante o envio os controles são bloqueados. Erros preservam observação e contexto. Sucesso atualiza a fila; uma falha na atualização é distinguida de falha na gravação.

Documento privado é obtido por GET autenticado na mesma origem e rota privada validada. O token fica somente no cabeçalho, nunca em URL ou relatório. Redirecionamentos são recusados; apenas PDF/JPEG/PNG/WEBP geram link temporário `blob:`. Esses links são revogados ao sair, atualizar ou trocar os registros exibidos. Não são inseridos HTML ou conteúdo executável retornado por documentos.

## Usuários, pedidos e demais ações

**Usuários:** busca e filtros são enviados à API, que pesquisa a base antes de aplicar o teto. Quando chegam 200 resultados, a tela pede refinamento e não inventa uma próxima página. Nome, tipo de conta e estado ficam no cartão; e-mail e ações ficam nos detalhes. Bloqueio/reativação exige confirmação nominativa e não altera tipo de conta/permissões. A própria conta não recebe ação de status; a regra também permanece no backend. Não existem botões de excluir, redefinir senha ou editar permissões.

**Pedidos:** a interface percorre todos os registros retornados por `/admin/pedidos` em páginas visuais de dez, com busca/status locais identificados. Não há mais truncamento em 50. Os detalhes mostram somente os campos administrativos disponíveis, sem consultar rotas privadas de outros perfis. A intervenção de status usa exclusivamente a rota administrativa existente, valida o enum e exige confirmação com título, estado anterior, novo estado e aviso sobre a falta de sincronização com agenda/proposta. Selecionar o estado já atual não gera gravação desnecessária.

**Denúncias:** o fluxo existente foi mantido, com detalhes, observação opcional, limites reais, confirmação e proteção contra duplicidade. Resolver/rejeitar não bloqueia contas automaticamente; a confirmação informa isso. Não foi criado sistema novo de moderação.

**Categorias:** criar/editar usa os limites reais (nome de 3 a 60, ícone opcional até 60). A edição leva o foco ao campo. Confirmações identificam a categoria. Retirada explica as duas possibilidades reais: desativação com vínculos ou exclusão sem vínculos. A API não oferece lista de inativas/reativação neste fluxo; esses botões não foram inventados.

**Auditoria:** últimas 50 ações, com rótulos compreensíveis, busca e páginas locais. Referências técnicas ficam em detalhes, disponíveis para rastreabilidade. Não se apresenta o recorte como histórico completo.

## Segurança, acessibilidade e limites

- O módulo não realiza requisições administrativas para usuários comuns ou sem usuário autenticado. A página compartilhada também consulta `/auth/me` e exige ADMIN antes de renderizar a área.
- Ocultar botões não é a autorização: todos os endpoints administrativos permanecem protegidos no backend.
- Erros inesperados/5xx usam mensagem segura; erros de regra/validação são tratados sem exibir stack traces, SQL ou credenciais.
- Formulários, botões, detalhes e confirmações usam elementos nativos. Há rótulos, mensagens associadas às validações dos campos, regiões de status, indicação da seção atual e foco no título ao navegar e no campo ao editar categoria.
- CSS é restrito à área administrativa, com foco visível, quebra de textos e layout para celular sem tabelas largas. Isso foi verificado por testes estáticos; contraste, comportamento real do teclado/leitor e aparência ainda precisam de validação humana.
- Usuários: limite 200 sem paginação. Pedidos, filas de verificação/denúncia e categorias não possuem paginação de servidor. Carregamento é sob demanda; a paginação visual não reduz o volume que esses endpoints retornam. Não foram inventados parâmetros ignorados pelo backend.
- A fila de verificação não permite consultar aprovados/rejeitados encerrados. Perfil público indisponível não comprova ausência de cadastro ou requisitos.
- A intervenção administrativa de status pode deixar dados relacionados inconsistentes porque o endpoint atual não os sincroniza. Nenhuma correção de regra foi improvisada no frontend.

Melhorias futuras que exigem aprovação de API: paginação com totais/filtros nos módulos sem suporte; detalhe administrativo de usuário/profissional/pedido; histórico completo de verificações; consulta/reativação de categorias inativas; indicadores confiáveis de intervenção; transições administrativas consistentes com proposta/agendamento e tratamento de concorrência. Não é necessário alterar banco para as mudanças frontend desta etapa; qualquer necessidade futura deve ser avaliada separadamente.

## Arquivos modificados/criados nesta etapa

- `wwwroot/pages/admin/gestao.html`: mantém shell/rota e contém a área de trabalho, com recursos versionados da etapa.
- `wwwroot/js/pages/admin.js`: navegação, consultas, ações, formulários e documento autenticado; reutiliza helpers internos de coleção, confirmação e erros.
- `wwwroot/css/pages/admin.css`: estilos isolados para o módulo.
- `Tests/frontend/stage6-admin.test.cjs`: regressões administrativas.
- `Tests/frontend/authenticated-layout.test.cjs`: validação dos dois novos recursos/versionamento administrativos.
- `Tests/Chamapro.AdminChecks/Chamapro.AdminChecks.csproj` e `Program.cs`: verificador local exclusivamente de leitura, sem criar administradores.
- `docs/ETAPA6_ADMINISTRACAO.md`: este relatório.

As alterações locais prévias das etapas anteriores foram preservadas. Não se alteraram componentes compartilhados nem arquivos de backend/bin/obj nesta etapa. Build e restore usam intermediários e saídas no diretório externo autorizado de validação.

## Testes executados e limites da evidência

- `node --test Tests/frontend/*.test.cjs`: **134/134 aprovados**, incluindo **38 novos testes administrativos** e todas as regressões anteriores. DOM e API simulados; não é validação visual ou gravação real.
- Testes backend existentes: **8/8 aprovados**.
- `dotnet build Chamapro.Api.csproj` com saída/intermediários externos: **aprovado, zero avisos e erros**.
- Build de `Chamapro.AdminChecks`: **aprovado, zero avisos e erros**.
- API real local: seis rotas (`/admin/relatorios`, `/admin/usuarios` com filtros, `/admin/pedidos`, `/admin/auditoria`, `/verificacoes`, `/denuncias`) retornaram **401 sem sessão** e **403 para um profissional autenticado**.
- **Leituras autenticadas como administrador não foram executadas:** o banco local não contém administrador ativo existente. O verificador informa execução incompleta; nenhuma conta, permissão ou credencial foi criada/alterada para contornar isso.
- Nenhum PATCH/POST/PUT/DELETE administrativo foi executado contra a API real. Aprovação, rejeição, bloqueio, alteração de pedido, categoria e denúncia foram exercitados apenas com mocks.
- Nenhuma página foi verificada visualmente nesta etapa. Além da ausência de administrador ativo, a tentativa de computer-use da etapa anterior foi interrompida pelo helper por não conseguir identificar a URL atual do Chrome com confiança; ela não foi repetida aqui. Testes de CSS/DOM não comprovam comportamento de navegador.

## Roteiro manual pendente

1. Entrar com um administrador autorizado já configurado pelo responsável pelo ambiente; confirmar também redirecionamento de cliente/profissional.
2. Conferir início, filas e todas as seções, incluindo links antigos, voltar/avançar, foco, teclado, leitor de tela, contraste e telas pequenas.
3. Consultar profissional ativo/inativo e documento privado real autorizado; verificar documento inexistente, sessão expirada e acesso recusado.
4. Conferir busca de usuários na API, filtros e aviso do teto de 200; conferir busca/status e páginas locais dos pedidos.
5. Conferir 401/403, falha de rede/5xx, tentativa novamente e falha após gravação bem-sucedida.
6. **Somente em ambiente isolado e após autorização explícita**, criar registros descartáveis pelo responsável e testar decisões de verificação, bloqueio/reativação, denúncias, categorias com/sem vínculos e intervenção em pedidos. Comparar efeitos reais, auditoria/notificações, cancelamento de confirmação, clique duplicado e preservação após erro. Não executar esse roteiro em contas/pedidos reais sem autorização.

Sem commit/push, migrations, mudança de esquema, alteração de credenciais/User Secrets, criação de administrador ou mudança dos dados de demonstração. Etapa 7 não iniciada.
