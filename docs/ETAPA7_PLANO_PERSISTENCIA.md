# Etapa 7 — Plano de persistência autorizado, bloqueado pela infraestrutura

O usuário autorizou nesta sessão preparar `chamapro_etapa7_isolado` e executar os testes, sem criar administrador nem tocar na demonstração. A tentativa de `CREATE DATABASE` foi recusada pelo PostgreSQL com **SQLSTATE 42501 (permissão insuficiente)**. Nenhuma base, tabela, conta ou cenário foi criado. Não se alteraram privilégios para contornar a recusa.

Não foi encontrado Docker/Podman no PATH nem foi comprovada outra base isolada disponível. `chamapro_local` contém demonstrações em uso e não será usado para testes de escrita. Um rollback externo não reverte requisições HTTP feitas por outra conexão.

## Teste preparado e bloqueio atual

`Tests/Chamapro.PersistenceChecks` contém um executável de integração preparado para os casos abaixo. Seu build foi validado; suas asserções reais **não foram executadas**, pois a preparação foi bloqueada antes de iniciar a API de teste.

Modos explícitos:

- `--prepare`: recusa base/marker preexistente, cria apenas a nova base e exporta somente o esquema para revisão. Foi o modo tentado e recusado por permissão.
- `--prepare-empty`: alternativa para uma base com o nome exato previamente disponibilizada pelo responsável pelo PostgreSQL. Recusa qualquer tabela de usuário preexistente e não altera privilégios. Ainda não foi executado.
- `--run`: exige marker, destino exato e hash do esquema exportado. Somente após revisão do esquema, restaura-o na base vazia, inicia API própria em `127.0.0.1:5097` e executa fixtures/testes. Mantém JWT e autorização normais; bootstrap administrativo fica sem credenciais; arquivos vão para pastas exclusivas. Não faz migrations, não elimina registros e encerra somente o processo de API que criou. Ainda não foi executado.

Para desbloquear, o responsável pelo PostgreSQL precisa disponibilizar a base **vazia** `chamapro_etapa7_isolado` e seu acesso ao papel de teste. Isso não significa criar/promover administrador do CHAMAPRO, conceder privilégios na demonstração ou alterar User Secrets. Não solicitar senha em relatório nem enviar credenciais por argumentos. Depois, revisar a exportação somente de esquema antes de executar `--run`.

## Plano concreto para uma rodada posterior

1. Após autorização, preparar **outra base**, denominada `chamapro_etapa7_isolado`, em PostgreSQL local. Ela não pode ser `chamapro_local`, uma base de produção ou conter dados anteriores. Usar identidade de teste restrita à nova base. Não copiar usuários, senhas, documentos, tokens ou registros da demonstração.
2. Revisar uma cópia somente do esquema necessário. Existem scripts em `Infrastructure/Persistence/Database`, mas não devem ser executados indiscriminadamente: há versões incrementais e scripts com dados/comandos destrutivos. A preparação precisa de revisão específica. Não executar migrations nem restaurar scripts sobre a base atual.
3. Hospedar uma instância separada da API para testes, preferencialmente dentro do processo de integração. Fornecer configuração temporária apenas ao processo de teste: conexão da nova base, chave JWT exclusiva e pastas de arquivos exclusivas do ambiente. Não alterar User Secrets, configurações de produção nem a API que atende em 5088.
4. Antes da primeira escrita, conferir o destino efetivo da conexão (`current_database()`), porta/host loopback, identidade da base e ausência de dados preexistentes. Falhar de forma fechada se qualquer condição divergir. O cliente HTTP deve estar ligado à instância de teste; nunca reaproveitar o endereço de demonstração por padrão.
5. Criar fixtures inteiramente fictícias com prefixo `[TESTE ETAPA7]`, contas no domínio reservado `.invalid` e credenciais aleatórias somente em memória. Usar login/autorização normais, sem DevAuth, alteração de claims no navegador ou desativação de middleware.
6. Isolar também e-mail, arquivos e rotinas de background para evitar mensagens externas ou alteração assíncrona dos cenários. Essa configuração pertence apenas ao host de testes, preservando a autorização e a configuração do aplicativo normal.
7. Executar a matriz abaixo comparando **GET antes, escrita autorizada e GET após recarga**. Uma resposta 2xx isolada não comprova persistência.
8. Manter evidências sem dados pessoais, senhas ou tokens. Deixar a base/pastas isoladas identificadas ao final. Excluir a base ou limpar arquivos depende de autorização separada; não executar limpeza destrutiva automática.

## Casos preparados para execução nesse ambiente

| Caso | Preparação descartável | Ação real proposta | Asserção após nova leitura |
| --- | --- | --- | --- |
| Perfil | Profissional com fotos, contato e descrição conhecidos | PUT `/profissionais/me` | Campos alterados persistem; fotos e campos não editados permanecem |
| Categorias/preços | Duas categorias, uma com mínimo zero | PUT `/profissionais/me/servicos`; reabrir e salvar | Zero, máximo e categorias preservados; faixa parcial rejeitada |
| Regiões | Duas cidades e bairros diferentes | PUT `/profissionais/me/areas`; remover só uma | Apenas a região removida desaparece; textos antigos não são convertidos silenciosamente |
| Horários | Dois intervalos na segunda e outro na quarta, flags e segundos diferentes | PUT `/profissionais/me/disponibilidade` | Todos os intervalos/flags/segundos não editados permanecem; sobreposição é rejeitada |
| Salvamento parcial | Perfil e regiões iniciais conhecidos | Primeiro PUT funciona; segundo falha de forma controlada no host de teste | GET comprova exatamente a parte salva; interface não diz que tudo foi concluído; retry não apaga preços/horários |
| Pedido automático/manual | Profissional verificado, categoria, região e disponibilidade compatíveis | POST `/pedidos` em cada modo | Detalhe contém o pedido criado e destinatários corretos; automático não inclui IDs antigos |
| Sem compatíveis | Categoria/região sem fixture compatível | POST `/pedidos` | 422 `SEM_PROFISSIONAIS_COMPATIVEIS`, mensagem útil e nenhum pedido criado |
| Proposta | Pedido direcionado, interesse demonstrado | POST `/pedidos/{id}/aceitar`, depois `/orcamentos` | `meuOrcamento` confirma valor/prazo/condições; reenvio permitido segue regra do orçamento ativo |
| Escolha e execução | Propostas de dois profissionais | Cliente escolhe; agenda; profissional escolhido inicia/conclui | GET confirma cada estado; concorrente e outro cliente são recusados; histórico e avaliações respeitam regras |
| Favoritos | Cliente e profissional descartáveis | POST e DELETE `/favoritos/{id}` | GET adiciona/remove só o favorito desse cliente, sem afetar outra conta |
| Documento | Arquivo fictício privado, sem conteúdo pessoal | Upload privado; consulta como dono, outro usuário e administrador autorizado | Conteúdo só chega aos perfis autorizados; cache privado; MIME/tamanho/caminho validados |
| Administração | **Depende de autorização adicional para conta administrativa** | Decisão de verificação, bloqueio/reativação, categoria com/sem vínculo, denúncia e status de pedido | GET confirma resultado, auditoria e efeitos reais; própria conta não é desativada; documentos de terceiros protegidos |

Esses são casos de integração a implementar/executar depois que o ambiente for autorizado e validado. Os testes DOM atuais não substituem esta rodada. Não existe, nesta entrega, um comando de escrita apontado para o banco de demonstração.

## Procedimento oficial de administrador encontrado no projeto

O serviço `Infrastructure/Services/AdminBootstrapService.cs`, registrado em `Program.cs`, provisiona uma conta **nova** somente quando as duas configurações explícitas estão presentes no startup:

- `Bootstrap:AdminEmail`, por variável temporária `Bootstrap__AdminEmail`.
- `Bootstrap:AdminPassword`, por variável temporária `Bootstrap__AdminPassword`.

O e-mail deve ser válido e a senha ter de 16 a 128 caracteres. Sem ambas as configurações, nenhuma conta é criada. O serviço recusa e-mail de usuário não administrador, não promove conta existente e não troca credenciais de administrador já existente. Também **não reativa** automaticamente administrador inativo.

Somente após autorização explícita para esse provisionamento:

1. Confirmar a base isolada ou outro destino local especificamente aprovado, separado da demonstração em uso.
2. Escolher e-mail de teste novo em domínio `.invalid`; não reutilizar e-mail de conta existente.
3. Receber uma senha forte por entrada segura, sem eco nem argumento de linha de comando. Passá-la apenas ao processo filho por ambiente temporário, sem registrá-la em arquivo, log ou User Secrets.
4. Iniciar a API de teste uma vez com essas variáveis, verificar o login normal e remover as variáveis de bootstrap do processo ao terminar. Não iniciar o servidor atual com essas variáveis por engano.
5. Se a necessidade for habilitar uma conta inativa existente, interromper: o bootstrap não faz isso. É necessária autorização e um procedimento separado revisado; não executar UPDATE improvisado nem promoção automática.

Não foram lidos valores de bootstrap, não foi iniciada nova API e nenhuma conta administrativa foi criada, promovida ou modificada nesta etapa.
