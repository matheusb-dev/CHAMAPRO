/* =========================================
   CHAMAPRO - CLIENTE / ORÇAMENTOS E AGENDA
   Arquivo: wwwroot/js/pages/cliente-orcamentos.js

   Funcionalidades:
   - Lista as propostas do pedido (GET /pedidos/{id}/orcamentos)
   - Aceita uma proposta (POST /orcamentos/{id}/aceitar)
   - Agenda o atendimento (POST /agendamentos)
   - Usa dados de demonstração sem sessão ou sem API

   O pedido vem de ?pedido=ID na URL; sem ele, usa o
   pedido mais recente do cliente que esteja PENDENTE ou ACEITO.
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    const { api, temSessao, mostrarAvisoDemo, toast, iniciais,
        formatarMoeda, formatarNota, escaparHtml, ICONES } = window.Chamapro;

    // =====================================
    // DADOS DE DEMONSTRAÇÃO (Figma)
    // =====================================

    const DEMO_PEDIDO = {
        id: 0,
        titulo: "Troca de torneira",
        categoria: "Hidráulica",
        status: "PENDENTE",
        orcamentoAceito: null,
        agendamento: null
    };

    const DEMO_PROPOSTAS = [
        {
            id: 1,
            valor: 150,
            prazoEstimado: "1 dia",
            condicoes: "Troca da torneira, instalação e teste de vazamento. Material não incluso.",
            expirado: false,
            situacao: "ENVIADO",
            totalAvaliacoes: 37,
            profissional: { id: 1, nome: "Carlos Mendes", notaMedia: 4.9, verificado: true }
        },
        {
            id: 2,
            valor: 180,
            prazoEstimado: "Mesmo dia",
            condicoes: "Atendimento no mesmo dia, mão de obra e garantia de 30 dias.",
            expirado: false,
            situacao: "ENVIADO",
            totalAvaliacoes: 21,
            profissional: { id: 2, nome: "Rafael Serviços", notaMedia: 4.7, verificado: true }
        }
    ];

    // =====================================
    // ESTADO
    // =====================================

    const estado = {
        demo: false,
        pedido: null,
        propostas: [],
        selecionadaId: null
    };

    // =====================================
    // ELEMENTOS
    // =====================================

    const tituloPropostas = document.getElementById("tituloPropostas");
    const listaPropostas = document.getElementById("listaPropostas");
    const badgeOrcamentos = document.getElementById("badgeOrcamentos");
    const btnWhatsApp = document.getElementById("btnWhatsApp");

    const formAgendamento = document.getElementById("formAgendamento");
    const selectData = document.getElementById("dataAgendamento");
    const selectHorario = document.getElementById("horarioAgendamento");
    const btnConfirmar = document.getElementById("btnConfirmarAgendamento");
    const avisoAgendamento = document.getElementById("avisoAgendamento");

    // =====================================
    // CARREGAMENTO
    // =====================================

    async function obterPedidoId() {

        const daUrl = Number(new URLSearchParams(location.search).get("pedido"));
        if (daUrl > 0) return daUrl;

        const pagina = await api("/pedidos?tamanho=50");

        const pedido = (pagina?.itens || []).find(p =>
            p.status === "PENDENTE" || p.status === "ACEITO"
        );

        return pedido?.id ?? null;
    }

    async function carregar() {

        if (!temSessao()) {
            usarDemo("Entre como cliente para ver suas propostas reais. Exibindo dados de demonstração.");
            return;
        }

        try {
            const pedidoId = await obterPedidoId();

            if (!pedidoId) {
                estado.pedido = null;
                estado.propostas = [];
                renderizar();
                return;
            }

            const [pedido, propostas] = await Promise.all([
                api(`/pedidos/${pedidoId}`),
                api(`/pedidos/${pedidoId}/orcamentos`)
            ]);

            estado.pedido = pedido;
            estado.propostas = propostas || [];
            estado.selecionadaId = pedido.orcamentoAceito?.id ?? null;

            renderizar();

        } catch (erro) {
            console.error("Erro ao carregar propostas:", erro);
            usarDemo("Não foi possível carregar os dados da API. Exibindo dados de demonstração.");
        }
    }

    function usarDemo(mensagem) {

        estado.demo = true;
        estado.pedido = structuredClone(DEMO_PEDIDO);
        estado.propostas = structuredClone(DEMO_PROPOSTAS);

        mostrarAvisoDemo(mensagem);
        renderizar();
    }

    // =====================================
    // RENDERIZAÇÃO DAS PROPOSTAS
    // =====================================

    function propostaAceita() {
        return estado.pedido?.orcamentoAceito ?? null;
    }

    function renderizar() {

        const { pedido } = estado;

        const propostas = [...estado.propostas].sort((a, b) =>
            (b.profissional?.notaMedia ?? 0) - (a.profissional?.notaMedia ?? 0)
        );

        const aceita = propostaAceita();

        if (!estado.selecionadaId && propostas.length) {
            estado.selecionadaId = propostas[0].id;
        }

        const pendentes = aceita ? 0 : propostas.filter(p => !p.expirado).length;
        badgeOrcamentos.textContent = pendentes;
        badgeOrcamentos.hidden = pendentes === 0;

        if (!pedido) {
            tituloPropostas.textContent = "Nenhum pedido aguardando propostas";
            listaPropostas.innerHTML =
                '<p class="empty-state">Quando você abrir um chamado, as propostas recebidas aparecem aqui.</p>';
            atualizarAgendamento();
            return;
        }

        const quantidade = propostas.length === 1 ? "1 proposta" : `${propostas.length} propostas`;
        tituloPropostas.textContent = `${quantidade} para “${pedido.titulo}”`;

        if (!propostas.length) {
            listaPropostas.innerHTML =
                '<p class="empty-state">Ainda não chegaram propostas para este pedido.</p>';
            atualizarAgendamento();
            return;
        }

        listaPropostas.innerHTML = propostas.map(p => renderizarProposta(p, aceita)).join("");

        atualizarAgendamento();
    }

    function renderizarProposta(proposta, aceita) {

        const prof = proposta.profissional || {};
        const selecionada = proposta.id === estado.selecionadaId;

        const avaliacoes = proposta.totalAvaliacoes
            ? ` · ${proposta.totalAvaliacoes} avaliações`
            : "";

        const descricao = proposta.condicoes
            || `Prazo estimado: ${proposta.prazoEstimado}`;

        let acao;

        if (aceita) {
            acao = aceita.id === proposta.id
                ? `<span class="tag tag-success">${ICONES.check} Proposta aceita</span>`
                : '<span class="tag">Não escolhida</span>';
        } else if (proposta.expirado) {
            acao = '<span class="tag">Expirada</span>';
        } else if (selecionada) {
            acao = `<button type="button" class="app-btn app-btn-primary" data-aceitar="${proposta.id}">Aceitar proposta</button>`;
        } else {
            acao = `<button type="button" class="app-btn" data-ver="${proposta.id}">Ver proposta</button>`;
        }

        return `
            <article class="proposta ${selecionada ? "selecionada" : ""}" data-id="${proposta.id}">

                <span class="app-avatar app-avatar-soft">${escaparHtml(iniciais(prof.nome))}</span>

                <div class="proposta-info">
                    <div class="proposta-nome">
                        <a href="./perfil-profissional.html?id=${prof.id}"><strong>${escaparHtml(prof.nome)}</strong></a>
                        ${prof.verificado ? `<span class="tag tag-success">${ICONES.check} Verificado</span>` : ""}
                    </div>

                    <div class="proposta-avaliacao">
                        ${ICONES.estrelaContorno}
                        <span>${formatarNota(prof.notaMedia)}${avaliacoes} · ${escaparHtml(estado.pedido.categoria)}</span>
                    </div>

                    <p class="proposta-descricao">${escaparHtml(descricao)}</p>
                </div>

                <div class="proposta-valor">
                    <strong>${formatarMoeda(proposta.valor)}</strong>
                    ${acao}
                </div>

            </article>
        `;
    }

    // =====================================
    // AÇÕES DAS PROPOSTAS
    // =====================================

    listaPropostas.addEventListener("click", async (event) => {

        const ver = event.target.closest("[data-ver]");

        if (ver) {
            estado.selecionadaId = Number(ver.dataset.ver);
            renderizar();
            return;
        }

        const aceitar = event.target.closest("[data-aceitar]");
        if (!aceitar) return;

        const id = Number(aceitar.dataset.aceitar);

        aceitar.disabled = true;
        aceitar.textContent = "Aceitando...";

        try {
            if (estado.demo) {
                const proposta = estado.propostas.find(p => p.id === id);
                estado.pedido.orcamentoAceito = { id, profissionalId: proposta.profissional.id };
                estado.pedido.status = "ACEITO";
            } else {
                estado.pedido = await api(`/orcamentos/${id}/aceitar`, { method: "POST" });
            }

            estado.selecionadaId = id;
            renderizar();
            toast("Proposta aceita! Agora escolha a data do atendimento.", "success");

        } catch (erro) {
            toast(erro.message, "error");
            renderizar();
        }
    });

    btnWhatsApp.addEventListener("click", () => {

        if (!propostaAceita()) {
            toast("Aceite uma proposta para liberar a conversa pelo WhatsApp.");
            return;
        }

        toast("O contato por WhatsApp do profissional ainda não está disponível.");
    });

    // =====================================
    // AGENDAMENTO
    // =====================================

    const formatoData = new Intl.DateTimeFormat("pt-BR", {
        weekday: "long",
        day: "numeric",
        month: "long"
    });

    function rotuloData(data) {
        const texto = formatoData.format(data);
        return texto.charAt(0).toUpperCase() + texto.slice(1);
    }

    function preencherDatas() {

        const hoje = new Date();

        for (let i = 1; i <= 14; i++) {
            const dia = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() + i);

            const option = document.createElement("option");
            option.value = `${dia.getFullYear()}-${dia.getMonth() + 1}-${dia.getDate()}`;
            option.textContent = rotuloData(dia);

            selectData.appendChild(option);
        }
    }

    function dataHoraSelecionada() {
        const [ano, mes, dia] = selectData.value.split("-").map(Number);
        const [hora, minuto] = selectHorario.value.split(":").map(Number);
        return new Date(ano, mes - 1, dia, hora, minuto);
    }

    function mostrarAviso(texto, sucesso = false) {
        avisoAgendamento.querySelector("span").textContent = texto;
        avisoAgendamento.classList.toggle("sucesso", sucesso);
    }

    function atualizarAgendamento() {

        const agendamento = estado.pedido?.agendamento;

        if (agendamento) {
            const data = new Date(agendamento.dataHora);
            const hora = data.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });

            mostrarAviso(`Atendimento agendado: ${rotuloData(data)}, às ${hora}.`, true);

            btnConfirmar.disabled = true;
            selectData.disabled = true;
            selectHorario.disabled = true;
            return;
        }

        mostrarAviso("Você pode remarcar até 24h antes do horário.");
    }

    formAgendamento.addEventListener("submit", async (event) => {

        event.preventDefault();

        if (!estado.pedido) {
            toast("Nenhum pedido para agendar.", "error");
            return;
        }

        if (!propostaAceita()) {
            toast("Aceite uma proposta antes de confirmar o agendamento.", "error");
            return;
        }

        const dataHora = dataHoraSelecionada();

        btnConfirmar.disabled = true;

        try {
            const agendamento = estado.demo
                ? { id: 1, pedidoId: 0, dataHora: dataHora.toISOString(), status: "Agendado" }
                : await api("/agendamentos", {
                    method: "POST",
                    body: { pedidoId: estado.pedido.id, dataHora: dataHora.toISOString() }
                });

            estado.pedido.agendamento = agendamento;
            atualizarAgendamento();
            toast("Agendamento confirmado!", "success");

        } catch (erro) {
            btnConfirmar.disabled = false;
            toast(erro.message, "error");
        }
    });

    // =====================================
    // INICIALIZAÇÃO
    // =====================================

    preencherDatas();
    carregar();
});
