# Etapa 4 — Cadastro e perfil profissional

## Implementação

Cadastro e perfil usam o mesmo editor e o mesmo Store. O cadastro apresenta três passos: serviços, regiões e horários. O perfil organiza os mesmos campos em seções recolhíveis; fotos, experiência e preferências de contato ficam disponíveis posteriormente.

Serviços usam o combobox existente e categorias da API. Cidade usa o mesmo snapshot IBGE da criação de pedidos, com UF visível e bairro livre. Regiões antigas são preservadas. O contrato atual armazena somente o nome da cidade: cidades homônimas continuam sendo uma limitação do backend, explicitamente comunicada na interface.

O editor de horários reutiliza o leitor/renderer que preserva todos os intervalos, flags e segundos não editados. Aplicar um horário comum pede confirmação antes de substituir configurações diferentes dos dias selecionados. Outros dias permanecem intactos. Personalizar permite editar, adicionar e remover intervalos. Remover todos exige confirmação e deixa o cadastro incompleto.

Preços existentes, inclusive zero, são preservados ao abrir, voltar, remover/reinserir uma categoria e salvar. Serviços podem ser salvos sem preços, conforme o contrato existente; preços continuam obrigatórios para conclusão do cadastro. Nenhuma validação backend foi relaxada. A prontidão consulta o painel da API e exige verificação, sem prometer pedidos fora da compatibilidade de região/horário.

Falhas de carregamento impedem gravações e oferecem nova tentativa. Falhas de gravação preservam os campos. Algumas seções usam vários PUTs existentes: não há atomicidade entre endpoints. Uma falha após a primeira gravação é comunicada como conclusão parcial. Preferências de contato não carregadas não podem ser sobrescritas.

## Arquivos da etapa

- wwwroot/js/components/professional-data.js (novo Store compartilhado)
- wwwroot/js/components/professional-editor.js (novo editor compartilhado)
- wwwroot/js/components/availability-editor.js (novo editor simples/avançado)
- wwwroot/css/pages/professional-editor.css (estilos limitados ao editor)
- wwwroot/js/pages/cadastro-profissional.js (inicialização compartilhada)
- wwwroot/js/pages/trabalhador.js (remoção do editor duplicado aposentado; fila preservada)
- wwwroot/pages/trabalhador/cadastro-profissional.html
- wwwroot/pages/trabalhador/perfil.html
- Tests/frontend/helpers/dom.cjs
- Tests/frontend/stage4-professional.test.cjs
- Tests/frontend/stage1-regressions.test.cjs (regressões adaptadas à fonte compartilhada)
- Tests/frontend/authenticated-layout.test.cjs (novos recursos das duas páginas)
- Tests/Chamapro.ProfileChecks/Chamapro.ProfileChecks.csproj e Program.cs (verificação local somente leitura)
- docs/ETAPA4_PROFISSIONAL.md

## Validação executada

- node --test Tests/frontend/*.test.cjs: 55 testes aprovados, incluindo 13 regressões da etapa 4. DOM e API simulados; não equivale a validação visual.
- Build da API: aprovado, zero avisos/erros, com intermediários/saída em diretório externo de validação.
- Testes backend: 8 aprovados, compilados com saída externa.
- Verificador ProfileChecks contra a API local real: leitura autenticada de perfil, painel, categorias e contato. Encontrou 2 serviços, 2 regiões, 7 intervalos e 19 categorias; cadastro completo informado pela API. Validou DTOs sem executar PUT, seed ou alterações no banco.
- Não houve validação visual das páginas no navegador nem gravação de perfil na API real nesta etapa.

## Verificação manual pendente

1. Navegar os três passos, voltar e reabrir o perfil; comparar preços e todos os intervalos persistidos.
2. Buscar serviços/cidades por teclado, adicionar/remover múltiplos itens e conferir UF/bairro.
3. Aplicar horários comuns, cancelar e confirmar substituição; editar/remover um intervalo e remover todos.
4. Conferir carregamento, erro/retry, salvamento parcial e mensagens de cadastro/verificação.
5. Testar upload de fotos, portfólio e contato com a API real.
6. Conferir desktop, celular, foco, teclado e leitores de tela.

Sem alteração de regras de conclusão, autenticação, banco, migrations ou credenciais. Sem commit/push. Alterações locais anteriores foram mantidas. Etapa 5 não iniciada.
