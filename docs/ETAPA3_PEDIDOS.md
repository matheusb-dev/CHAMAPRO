# Etapa 3 — Solicitação de serviços

## Diagnóstico do erro

Foi reproduzido no servidor local em `localhost:5088`, com token temporário emitido pelo `TokenService` para `cliente02@v1.demo.chamapro.invalid` (sem alterar sua senha nem registrar token):

- `GET /api/v1/auth/me`: 200; cliente ativo.
- `POST /api/v1/pedidos`: 422, `SEM_PROFISSIONAIS_COMPATIVEIS`, categoria Pedreiro, cidade Campo Mourão, Centro, IMEDIATO, distribuição automática e lista de IDs vazia.
- Resposta da versão que estava em execução: `detalhe = "Operação não permitida"`; o motivo real estava em `titulo`.
- Em São Paulo/Centro, automático e manual retornaram 201, criando os pedidos de teste 21 e 22. Permanecem no banco com título `[TESTE ETAPA 3]`; nenhum pedido do seed foi apagado ou alterado.

Causa comprovada da mensagem genérica: `PedidoErros` usava a assinatura `(status, codigo, titulo, detalhe)`, mas `BusinessException` recebe `(status, codigo, detalhe, titulo)`. A fábrica foi corrigida, preservando códigos HTTP, códigos de erro, contratos e regras. O frontend mantém o código da regra e apresenta uma orientação segura. Não houve enfraquecimento da verificação ou disponibilidade.

Não havia captura da requisição original do navegador. A reprodução comprova esse caminho de falha, não identifica retroativamente a cidade/conta usadas naquele teste. Campo Mourão não tinha profissionais compatíveis no seed examinado. Uma cidade sem correspondência deve continuar sendo recusada com explicação útil.

Reinicie a API para carregar a fábrica corrigida; a instância usada no teste HTTP ainda executava o binário anterior. O teste unitário da nova fábrica confirma o motivo correto e os status 422/403.

## Fluxo

1. **O que você precisa?** Categoria real da API, descrição e resumo editável. Se o resumo estiver vazio, uma sugestão é extraída da descrição; nunca sobrescreve um título já preenchido.
2. **Onde e quando?** Localização da conta reaproveitada, cidade/UF pesquisável, bairro e endereço editáveis. Ao mudar a cidade, bairro e endereço anteriores são limpos para evitar combinações incorretas.
3. **Conferir e pedir.** Fotos opcionais, distribuição automática como padrão, escolha manual secundária, resumo na mesma largura e confirmação. Após 201, abre o detalhe do pedido criado.

Os controles permanecem montados ao trocar de etapa. Fotos e valores não são recriados. Uploads já concluídos são reutilizados numa tentativa posterior com o mesmo arquivo. Cliques repetidos ficam bloqueados enquanto o envio está em andamento. Falhas de rede preservam os campos e permitem tentar novamente.

A opção mais rápida procura profissionais com urgência e disponibilidade agora; não promete atendimento. Agendamento exige data futura. Categorias, região, identidade verificada e disponibilidade continuam sendo validadas pelo backend.

## Seletores

`wwwroot/js/components/combobox.js` é um componente reutilizável sem dependências. Os controles originais continuam fornecendo os valores enviados à API. A pesquisa ignora acentos/caixa; o rótulo mantém grafia original. Há combobox/listbox, opção selecionada, estado expandido, identificação da opção ativa, setas, Home/End, Enter, Escape, Tab, clique/toque e fechamento ao sair.

A lista tem rolagem interna e altura máxima de 240px (limitada também pela altura da tela no celular). No máximo 80 resultados são montados de uma vez, com aviso para refinar buscas amplas. A base pesquisada permanece completa. Carregamento, ausência de resultados, erro e nova tentativa têm estados próprios. Tipo de atendimento permanece nativo, pois tem apenas duas opções.

## Municípios e bairros

Fonte: API oficial de localidades do IBGE, endpoint `https://servicodados.ibge.gov.br/api/v1/localidades/municipios?orderBy=nome`.

A cópia local tem 5.571 municípios, identificadores únicos e as 27 UFs, coletada em 02/11/2026. Foram mantidos apenas `id`, `nome` e `uf`. A fonte e o SHA-256 estão em `wwwroot/data/municipios-ibge.metadata.json`. O navegador consulta somente o JSON local; não transmite os dados do cliente ao IBGE nem faz chamadas externas por digitação.

O rótulo é `Campo Mourão — PR`, mas a API recebe `cidade: "Campo Mourão"`. Identificador e UF são usados apenas na seleção local. Acentos são preservados no payload. Localização da conta com nome ambíguo exige escolha explícita da UF; não é selecionada a primeira correspondência silenciosamente.

**Limitação estrutural:** o backend ainda identifica cidade por nome, sem UF/código municipal. Exibir UF não elimina a ambiguidade entre cidades homônimas. A interface avisa essa limitação. Uma evolução separada deverá adicionar identificação municipal opcional aos DTOs e entidades de cliente/pedido/área profissional, fazer preenchimento assistido preservando textos antigos, ajustar compatibilidade e criar migration somente após aprovação. Não foi implementada nesta etapa.

Não foi encontrada base confiável de bairros no projeto. Bairro continua texto livre opcional, sem lista parcial apresentada como oficial.

## Favoritos

Em Meus profissionais ou no perfil, o link leva a `criar-chamado.html?profissionalId=...`. Depois de escolher serviço/local/horário, a busca existente de compatíveis é executada com `verificado=true`. O favorito só é marcado se estiver entre os resultados compatíveis; a busca percorre páginas adicionais quando necessário para não presumir incompatibilidade apenas pela primeira página.

Se incompatível, a mensagem explica as condições e oferece outro profissional ou distribuição automática. Nenhum ID oculto é enviado no modo automático. Mudar critérios invalida a seleção e descarta respostas antigas da busca. Voltar e avançar sem mudar critérios conserva a seleção.

O domínio ainda não fornece uma razão individual detalhada para cada incompatibilidade. Por isso a interface informa categoria, região, horário ou verificação, sem inventar qual foi o impedimento específico.

## Classificação dos campos

| Campo | Tratamento |
|---|---|
| Categoria | Seleção única pesquisável da API |
| Cidade | Seleção única pesquisável do IBGE local, com UF |
| Quando precisa | Seleção única nativa com duas opções |
| Data/horário | Preenchimento condicional para agendamento |
| Distribuição | Seleção única, automática como padrão |
| Profissionais | Seleção múltipla opcional, até cinco compatíveis |
| Descrição e título | Texto próprio do cliente; título exigido pelo contrato |
| Bairro/endereço | Texto livre opcional, reaproveitado da conta |
| Fotos | Anexos opcionais, até cinco |
| Código IBGE/UF | Estado local do seletor, sem novos campos no payload |

Nenhum campo do contrato foi excluído. Regras técnicas de direcionamento ficaram fora da explicação principal, mas continuam sendo aplicadas.

## Testes e limites

- Testes frontend anteriores preservados; novos testes usam DOM/API simulados e cobrem busca, teclado, cidade/UF, ausências, localização, favorito compatível/incompatível, envio automático/manual, modos, erros, datas, anexos entre etapas, duplicidade e redirecionamento.
- A base local é validada por contagem, UF, identificadores únicos e Campo Mourão/PR.
- Teste C# da fábrica de erros verifica motivo, título, código e status HTTP.
- A ferramenta `Tests/Chamapro.OrderChecks` verifica a API real local. Por padrão apenas reproduz a recusa; `--create` acrescenta dois pedidos demo e NÃO deve ser executado repetidamente sem considerar o limite de pendentes do cliente.
- Não houve login interativo nem revisão visual no navegador. Os testes de teclado são simulações, não validação com leitor de tela.
- Cliques duplicados são bloqueados. Uma resposta de rede perdida após o servidor já ter confirmado o pedido ainda exige conferir Meus pedidos antes de repetir; garantia completa de idempotência no servidor demandaria uma evolução de contrato separada.

## Conferência manual

Reinicie a API com `./run-local.ps1`, abra Pedir um serviço e teste: pesquisa por `tecnico`/`campo mourao`, escolha por teclado, lista com rolagem, prefill da conta, troca de cidade, data futura, fotos ao voltar, favorito com e sem compatibilidade, falha de rede e retorno ao detalhe após confirmar. Confira desktop e celular, zoom e leitor de tela. Não avance para a Etapa 4.

Nomes de cidades antigos gravados sem a grafia oficial também continuam sujeitos à comparação textual existente no backend. Não houve limpeza automática de dados nem alteração silenciosa das regiões profissionais.
