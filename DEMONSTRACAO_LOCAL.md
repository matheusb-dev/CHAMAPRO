# Demonstração local do CHAMAPRO

Ferramenta independente em `Tests/Chamapro.DemoSeed`. Não é chamada pelo startup, não disponibiliza endpoint de seed, não faz migrations e não inicia rotinas de background. O seed SQL antigo não é usado.

## Executar

Na raiz do repositório, com PostgreSQL disponível:

```powershell
# Somente leitura: destino, tabelas e categorias existentes
.\seed-demo.ps1

# Criar ou reconhecer o lote v1 sem duplicá-lo
.\seed-demo.ps1 -Apply

# Definir interativamente a senha das contas FICTÍCIAS do lote existente
.\seed-demo.ps1 -ConfigurePassword

# Criar outro conjunto de cenários, preservando o lote anterior e dados reais
.\seed-demo.ps1 -Apply -Batch rodada2 -ConfigurePassword
```

`-ConfigurePassword` pede a senha sem eco. O valor só é transmitido ao processo filho por variável de ambiente temporária e é armazenado com BCrypt, como na autenticação atual. Não informe senha em argumentos, arquivos ou commits. Sem esse passo, contas novas recebem senhas aleatórias descartadas, portanto não são acessíveis com uma senha conhecida. A operação troca somente as senhas das 17 contas identificadas do lote; não cria administrador. As variáveis anteriores são restauradas no final.

A configuração é lida das mesmas fontes padrão do app em Development (appsettings, User Secrets da API e variáveis de ambiente), sem gravá-las. Somente `chamapro_local` em `localhost`, `127.0.0.1` ou `::1`, porta 5432, é permitido. Servidor e cliente efetivos também precisam ter endereço loopback. Um destino diferente bloqueia antes de inserir. A transação usa trava consultiva para impedir duas cargas simultâneas do mesmo mecanismo. Falhas antes do commit revertem a carga; sequências PostgreSQL podem avançar mesmo após rollback.

## Dados do lote v1

- 19 categorias: Diarista/Faxineira representam o mesmo serviço; não são duplicadas. Categorias existentes são reutilizadas por nomes normalizados; categorias inativas ou equivalências ambíguas bloqueiam a carga sem reativação automática.
- 5 clientes e 12 profissionais, com nomes `[DEMO LOCAL:v1]` e e-mails no domínio reservado `v1.demo.chamapro.invalid`.
- 24 vínculos profissionais/categorias, 13 áreas e 84 intervalos de disponibilidade.
- 20 pedidos: 8 pendentes, 3 com profissional escolhido, 3 agendados, 2 em andamento, 3 concluídos e 1 cancelado.
- 40 destinatários, 32 propostas, 8 agendamentos e 2 avaliações; um serviço concluído fica aguardando avaliação.
- 4 favoritos, 60 notificações e 80 eventos de histórico.
- 11 verificações simuladas: 8 aprovadas, 1 pendente, 1 rejeitada e 1 com documentação pendente. Um profissional não tem verificação.

As verificações são SIMULAÇÕES, sem documentos ou comprovação de identidade. Seus links abrem uma página em branco identificada como demonstração. Telefones são `00000000000`, propositalmente sem contato real; WhatsApp fica desativado. Nenhuma imagem ou documento pessoal é criado. Não use esses perfis para serviços reais.

Os pedidos estão concentrados em São Paulo (Centro/Vila Mariana). Os últimos quatro profissionais atendem Campinas ou Santos, permitindo testar incompatibilidade regional e ausência de oportunidades. O primeiro profissional também atende Campinas. Há categorias sem profissional compatível, como Limpeza de terrenos, para testar ausência de correspondência. Disponibilidade é ampla e explicitamente fictícia para facilitar testes de imediato/agendado.

## Entrar nas contas

Após configurar a senha interativamente e iniciar `./run-local.ps1`, use o login normal em `http://localhost:5088`:

| Cenário | Conta |
|---|---|
| Cliente com pedidos, propostas e favorito | `cliente01@v1.demo.chamapro.invalid` |
| Cliente vazio, sem pedidos/favoritos/notificações | `cliente05@v1.demo.chamapro.invalid` |
| Profissional com oportunidades | `profissional01@v1.demo.chamapro.invalid` |
| Profissional com serviço em andamento | `profissional03@v1.demo.chamapro.invalid` |
| Profissional sem oportunidades e verificação pendente | `profissional09@v1.demo.chamapro.invalid` |

Clientes 01 a 04 têm cinco pedidos cada. Profissionais 01 a 08 são os verificados necessários aos cenários. Favoritos permitem abrir o perfil e usar o pedido existente; a pré-seleção automática do favorito continua sendo dependência da refatoração, não implementada pelo seed.

## Repetição e preservação

Reexecutar `-Apply` no mesmo lote NÃO restaura status, propostas ou preços após testes manuais, nem altera senhas. Isso preserva o trabalho já feito. Para cenários novos use outro `-Batch`, sem apagar o anterior. A data dos cenários e validade das propostas são relativas à criação do lote; após vários dias, crie outro lote para testar propostas válidas.

Uma carga parcial ou colisão com conta sem a identificação esperada é bloqueada; o programa não tenta reparar ou sobrescrever registros. Categorias são compartilhadas e podem não ter o prefixo de demonstração. Não as exclua por nome para “limpar” o ambiente.

## Verificação executada

No ambiente local original foram encontrados 4 usuários, 1 profissional, 1 arquivo privado e 1 verificação, todos preservados. Categorias, pedidos e demais tabelas de cenário estavam vazios. Após a carga existem 21 usuários e 13 profissionais no total. A segunda execução do lote v1 manteve todas as contagens, comprovando idempotência por contagem.

A consulta HTTP real `GET /api/v1/categorias` retornou 19 categorias em localhost:5088. A aparência do seletor e os fluxos autenticados precisam ser conferidos no navegador. Não houve configuração automática de senha conhecida nem criação de administrador.

## Tabelas acrescentadas no lote inicial

| Tabela | Registros novos |
|---|---:|
| categorias | 19 |
| usuarios | 17 |
| profissionais | 12 |
| profissional_categorias | 24 |
| areas_atendimento | 13 |
| disponibilidade_profissional | 84 |
| pedidos | 20 |
| pedido_destinatarios | 40 |
| orcamentos | 32 |
| agendamentos | 8 |
| avaliacoes | 2 |
| favoritos | 4 |
| notificacoes | 60 |
| historico_pedidos | 80 |
| verificacoes_profissionais | 11 |

Total da carga inicial: 426 registros, incluindo categorias reutilizáveis. Nenhum registro adicionado a arquivos_privados, auditoria_admin, denuncias ou recuperacoes_senha. Os arquivos privados e a verificação anteriores permanecem intocados.

Testes executados: build da API e da ferramenta sem avisos/erros; 27 testes frontend; 7 testes backend existentes (binário já compilado, fontes desses testes não alteradas); bloqueio em Production e bloqueio de host remoto; compatibilidade dos 40 destinatários pelo repositório real; validação das relações de contratação/agendamento/avaliação; repetição do lote sem duplicações. A configuração interativa de senha e o login no navegador não foram executados automaticamente.
