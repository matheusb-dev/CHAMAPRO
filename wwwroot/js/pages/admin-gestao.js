/* =========================================
   CHAMAPRO - ADMINISTRADOR / GESTÃO
   Arquivo: wwwroot/js/pages/admin-gestao.js

   Funcionalidades:
   - Métricas (GET /admin/usuarios, /admin/pedidos, /denuncias)
   - Verificações pendentes (GET /verificacoes)
   - Aprovar / reprovar documentos (PATCH /verificacoes/{id}/avaliar)
   - Denúncias recentes e chamados por categoria
   - Usa dados de demonstração para o que a API não devolver
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    const { api, temSessao, mostrarAvisoDemo, toast, iniciais,
        escaparHtml, formatarData, ICONES } = window.Chamapro;

    const numero = new Intl.NumberFormat("pt-BR");

    // =====================================
    // DADOS DE DEMONSTRAÇÃO (Figma)
    // =====================================

    const DEMO = {
        usuariosAtivos: 1284,
        detalheUsuarios: "+8% neste mês",
        profissionais: 386,
        chamadosAtivos: 148,
        aguardandoProposta: 27,

        verificacoes: [
            {
                profissionalId: 1, nome: "Carlos Mendes", documentos: [
                    { id: 1, tipoDocumento: "Eletricista", urlDocumento: "#", criadoEm: "2026-10-22" },
                    { id: 2, tipoDocumento: "Documento de identificação", urlDocumento: "#", criadoEm: "2026-10-22" }
                ]
            },
            {
                profissionalId: 2, nome: "Mariana Lopes", documentos: [
                    { id: 3, tipoDocumento: "Instalação elétrica", urlDocumento: "#", criadoEm: "2026-10-23" },
                    { id: 4, tipoDocumento: "Documento de identificação", urlDocumento: "#", criadoEm: "2026-10-23" },
                    { id: 5, tipoDocumento: "Certificado NR-10", urlDocumento: "#", criadoEm: "2026-10-23" }
                ]
            },
            {
                profissionalId: 3, nome: "João Ferreira", documentos: [
                    { id: 6, tipoDocumento: "Gás e hidráulica", urlDocumento: "#", criadoEm: "2026-10-24" }
                ]
            }
        ],
        filaVerificacoes: 42,

        denuncias: [
            { id: 1, motivo: "Perfil suspeito" },
            { id: 2, motivo: "Avaliação abusiva" },
            { id: 3, motivo: "Chamado duplicado" }
        ],
        totalDenuncias: 12,

        categorias: [
            { nome: "Hidráulica", total: 52 },
            { nome: "Elétrica", total: 41 },
            { nome: "Pintura", total: 24 },
            { nome: "Montagem", total: 17 }
        ]
    };

    // =====================================
    // ESTADO E ELEMENTOS
    // =====================================

    const estado = {
        demo: false,
        grupos: [],
        denuncias: [],
        verTodas: false,
        grupoAberto: null
    };

    const el = id => document.getElementById(id);

    const listaVerificacoes = el("listaVerificacoes");
    const listaDenuncias = el("listaDenuncias");
    const barrasCategorias = el("barrasCategorias");
    const btnVerTodas = el("btnVerTodas");

    const modal = el("modalVerificacao");
    const modalObservacao = el("modalObservacao");

    // =====================================
    // CARREGAMENTO
    // =====================================

    async function carregar() {

        if (!temSessao()) {
            mostrarAvisoDemo("Entre como administrador para ver os dados reais. Exibindo dados de demonstração.");
            usarDemo();
            return;
        }

        const [usuarios, pedidos, verificacoes, denuncias] = await Promise.allSettled([
            api("/admin/usuarios"),
            api("/admin/pedidos"),
            api("/verificacoes"),
            api("/denuncias")
        ]);

        const falhas = [usuarios, pedidos, verificacoes, denuncias]
            .filter(r => r.status === "rejected");

        if (falhas.length === 4) {
            console.error("Erro ao carregar a gestão:", falhas[0].reason);
            mostrarAvisoDemo("Não foi possível carregar os dados da API. Exibindo dados de demonstração.");
            usarDemo();
            return;
        }

        if (falhas.length) {
            falhas.forEach(f => console.error("Falha parcial na gestão:", f.reason));
            mostrarAvisoDemo("Parte dos dados não veio da API; esses blocos mostram dados de demonstração.");
        }

        // Verificações (agrupadas por profissional)
        estado.grupos = verificacoes.status === "fulfilled"
            ? agruparVerificacoes(verificacoes.value || [])
            : structuredClone(DEMO.verificacoes);

        // Denúncias (mais recentes primeiro)
        estado.denuncias = denuncias.status === "fulfilled"
            ? [...(denuncias.value || [])].reverse()
            : structuredClone(DEMO.denuncias);

        // Métricas de usuários
        if (usuarios.status === "fulfilled") {
            const lista = usuarios.value || [];
            const ativos = lista.filter(u => u.ativo).length;
            const profissionais = lista.filter(u =>
                String(u.perfil).toUpperCase() === "PROFISSIONAL"
            ).length;

            el("metricaUsuarios").textContent = numero.format(ativos);
            el("detalheUsuarios").textContent = `${numero.format(lista.length)} cadastrados`;
            el("metricaProfissionais").textContent = numero.format(profissionais);
        } else {
            preencherMetricasUsuariosDemo();
        }

        // Métricas e categorias dos chamados
        if (pedidos.status === "fulfilled") {
            const lista = pedidos.value || [];
            const ativos = lista.filter(p => !["CONCLUIDO", "CANCELADO"].includes(p.status));
            const pendentes = lista.filter(p => p.status === "PENDENTE");

            el("metricaChamados").textContent = numero.format(ativos.length);
            el("detalheChamados").textContent = `${numero.format(pendentes.length)} aguardando proposta`;

            renderizarCategorias(contarPorCategoria(lista));
        } else {
            el("metricaChamados").textContent = numero.format(DEMO.chamadosAtivos);
            el("detalheChamados").textContent = `${DEMO.aguardandoProposta} aguardando proposta`;
            renderizarCategorias(DEMO.categorias);
        }

        const fila = verificacoes.status === "fulfilled" ? estado.grupos.length : DEMO.filaVerificacoes;
        const totalDenuncias = denuncias.status === "fulfilled" ? estado.denuncias.length : DEMO.totalDenuncias;

        atualizarContadores(fila, totalDenuncias);
        renderizarVerificacoes();
        renderizarDenuncias();
    }

    function usarDemo() {

        estado.demo = true;
        estado.grupos = structuredClone(DEMO.verificacoes);
        estado.denuncias = structuredClone(DEMO.denuncias);

        preencherMetricasUsuariosDemo();
        el("metricaChamados").textContent = numero.format(DEMO.chamadosAtivos);
        el("detalheChamados").textContent = `${DEMO.aguardandoProposta} aguardando proposta`;

        atualizarContadores(DEMO.filaVerificacoes, DEMO.totalDenuncias);
        renderizarVerificacoes();
        renderizarDenuncias();
        renderizarCategorias(DEMO.categorias);
    }

    function preencherMetricasUsuariosDemo() {
        el("metricaUsuarios").textContent = numero.format(DEMO.usuariosAtivos);
        el("detalheUsuarios").textContent = DEMO.detalheUsuarios;
        el("metricaProfissionais").textContent = numero.format(DEMO.profissionais);
    }

    // =====================================
    // CONTADORES (métricas, badges e tag)
    // =====================================

    function atualizarContadores(fila, denuncias) {

        el("detalheProfissionais").textContent = `${numero.format(fila)} em verificação`;
        el("tagFila").textContent = `${numero.format(fila)} na fila`;
        el("metricaDenuncias").textContent = numero.format(denuncias);

        definirBadge(el("badgeVerificacoes"), fila);
        definirBadge(el("badgeDenuncias"), denuncias);
    }

    function definirBadge(badge, valor) {
        badge.textContent = valor;
        badge.hidden = !valor;
    }

    // =====================================
    // VERIFICAÇÕES
    // =====================================

    function agruparVerificacoes(verificacoes) {

        const grupos = new Map();

        verificacoes.forEach(v => {
            if (!grupos.has(v.profissionalId)) {
                grupos.set(v.profissionalId, {
                    profissionalId: v.profissionalId,
                    // A API devolve só o id do profissional, sem o nome.
                    nome: `Profissional #${v.profissionalId}`,
                    documentos: []
                });
            }

            grupos.get(v.profissionalId).documentos.push(v);
        });

        return [...grupos.values()];
    }

    function descreverGrupo(grupo) {

        const qtd = grupo.documentos.length;
        const documentos = qtd === 1 ? "1 documento" : `${qtd} documentos`;

        return `${grupo.documentos[0].tipoDocumento} · ${documentos}`;
    }

    function renderizarVerificacoes() {

        if (!estado.grupos.length) {
            listaVerificacoes.innerHTML = '<p class="empty-state">Nenhuma verificação aguardando análise.</p>';
            return;
        }

        listaVerificacoes.innerHTML = estado.grupos.slice(0, 3).map(grupo => `
            <div class="list-row">
                <span class="app-avatar app-avatar-soft">${escaparHtml(iniciais(grupo.nome.replace("#", "")))}</span>
                <div class="list-row-info">
                    <strong>${escaparHtml(grupo.nome)}</strong>
                    <span>${escaparHtml(descreverGrupo(grupo))}</span>
                </div>
                <button type="button" class="app-btn app-btn-sm" data-analisar="${grupo.profissionalId}">
                    Analisar ${ICONES.seta}
                </button>
            </div>
        `).join("");
    }

    listaVerificacoes.addEventListener("click", (event) => {

        const botao = event.target.closest("[data-analisar]");
        if (!botao) return;

        const grupo = estado.grupos.find(g => g.profissionalId === Number(botao.dataset.analisar));
        if (grupo) abrirModal(grupo);
    });

    // =====================================
    // MODAL DE ANÁLISE
    // =====================================

    function abrirModal(grupo) {

        estado.grupoAberto = grupo;

        el("modalAvatar").textContent = iniciais(grupo.nome.replace("#", ""));
        el("modalTitulo").textContent = grupo.nome;
        el("modalSubtitulo").textContent = descreverGrupo(grupo);
        modalObservacao.value = "";

        el("modalDocumentos").innerHTML = grupo.documentos.map(doc => {

            const link = doc.urlDocumento && doc.urlDocumento !== "#"
                ? `<a href="${escaparHtml(doc.urlDocumento)}" target="_blank" rel="noopener">Abrir</a>`
                : '<span class="tag">Exemplo</span>';

            return `
                <div class="modal-documento">
                    <span>${escaparHtml(doc.tipoDocumento)} <small class="text-secondary">· ${formatarData(doc.criadoEm)}</small></span>
                    ${link}
                </div>
            `;
        }).join("");

        modal.showModal();
    }

    modal.addEventListener("click", async (event) => {

        const botao = event.target.closest("[data-decisao]");
        if (!botao || !estado.grupoAberto) return;

        const aprovado = botao.dataset.decisao === "aprovar";
        const grupo = estado.grupoAberto;
        const observacao = modalObservacao.value.trim() || null;

        if (!aprovado && !observacao) {
            toast("Informe o motivo da reprovação na observação.", "error");
            modalObservacao.focus();
            return;
        }

        modal.querySelectorAll("button").forEach(b => b.disabled = true);

        try {
            if (!estado.demo) {
                for (const doc of grupo.documentos) {
                    await api(`/verificacoes/${doc.id}/avaliar`, {
                        method: "PATCH",
                        body: { aprovado, observacao }
                    });
                }
            }

            estado.grupos = estado.grupos.filter(g => g !== grupo);

            const fila = Math.max(0, Number(el("badgeVerificacoes").textContent) - 1);
            atualizarContadores(fila, Number(el("badgeDenuncias").textContent) || 0);
            renderizarVerificacoes();

            modal.close();
            toast(aprovado ? "Profissional aprovado." : "Verificação reprovada.", "success");

        } catch (erro) {
            toast(erro.message, "error");
        } finally {
            modal.querySelectorAll("button").forEach(b => b.disabled = false);
        }
    });

    modal.addEventListener("close", () => {
        estado.grupoAberto = null;
    });

    // =====================================
    // DENÚNCIAS
    // =====================================

    // A API não tem campo de prioridade: ela é deduzida pelo motivo.
    function prioridade(motivo) {

        const texto = String(motivo || "").toLowerCase();

        if (/golpe|fraude|suspeit|amea|assédio|assedio|falso/.test(texto)) {
            return { classe: "alta", tag: "tag-danger", rotulo: "Alta prioridade" };
        }

        if (/abusiv|ofens|agress|inadequad/.test(texto)) {
            return { classe: "normal", tag: "tag-warning", rotulo: "Normal" };
        }

        return { classe: "baixa", tag: "", rotulo: "Baixa prioridade" };
    }

    function renderizarDenuncias() {

        if (!estado.denuncias.length) {
            listaDenuncias.innerHTML = '<p class="empty-state">Nenhuma denúncia pendente.</p>';
            btnVerTodas.hidden = true;
            return;
        }

        const lista = estado.verTodas ? estado.denuncias : estado.denuncias.slice(0, 3);

        btnVerTodas.hidden = estado.denuncias.length <= 3 && !estado.demo;
        btnVerTodas.textContent = estado.verTodas ? "Ver menos" : "Ver todas";

        listaDenuncias.innerHTML = lista.map(d => {

            const p = prioridade(d.motivo);

            return `
                <div class="list-row denuncia-row">
                    <span class="denuncia-${p.classe}">${ICONES.bandeira}</span>
                    <span class="denuncia-titulo" title="${escaparHtml(d.descricao || "")}">${escaparHtml(d.motivo)}</span>
                    <span class="tag ${p.tag}">${p.rotulo}</span>
                </div>
            `;
        }).join("");
    }

    btnVerTodas.addEventListener("click", () => {

        if (estado.demo && estado.denuncias.length <= 3) {
            toast("Na demonstração só há 3 denúncias de exemplo.");
            return;
        }

        estado.verTodas = !estado.verTodas;
        renderizarDenuncias();
    });

    // =====================================
    // CHAMADOS POR CATEGORIA
    // =====================================

    function contarPorCategoria(pedidos) {

        const contagem = new Map();

        pedidos.forEach(p => {
            const nome = p.categoria?.nome || "Outros";
            contagem.set(nome, (contagem.get(nome) || 0) + 1);
        });

        return [...contagem.entries()]
            .map(([nome, total]) => ({ nome, total }))
            .sort((a, b) => b.total - a.total)
            .slice(0, 4);
    }

    function renderizarCategorias(categorias) {

        if (!categorias.length) {
            barrasCategorias.innerHTML = '<p class="empty-state">Nenhum chamado registrado ainda.</p>';
            return;
        }

        const maior = Math.max(...categorias.map(c => c.total));

        barrasCategorias.innerHTML = categorias.map(c => `
            <div class="categoria-barra">
                <span>${escaparHtml(c.nome)} <small>${c.total}</small></span>
                <div class="trilho" role="progressbar" aria-label="${escaparHtml(c.nome)}"
                     aria-valuemin="0" aria-valuemax="${maior}" aria-valuenow="${c.total}">
                    <div class="preenchimento" style="width: ${(c.total / maior) * 70}%"></div>
                </div>
            </div>
        `).join("");
    }

    // =====================================
    // INICIALIZAÇÃO
    // =====================================

    carregar();
});
