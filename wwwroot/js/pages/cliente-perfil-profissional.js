/* =========================================
   CHAMAPRO - PERFIL PROFISSIONAL E AVALIAÇÃO
   Arquivo: wwwroot/js/pages/cliente-perfil-profissional.js

   Funcionalidades:
   - Perfil público (GET /profissionais/{id})
   - Avaliações de clientes (GET /profissionais/{id}/avaliacoes)
   - Favoritar (salvo só neste navegador; a API não tem favoritos)
   - Solicitar orçamento (vai para criar-chamado com o profissional)

   O profissional vem de ?id=ID na URL.
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    const { api, mostrarAvisoDemo, iniciais, formatarNota,
        escaparHtml, estrelas } = window.Chamapro;

    // =====================================
    // DADOS DE DEMONSTRAÇÃO (Figma)
    // =====================================

    const DEMO_PERFIL = {
        id: 0,
        nome: "Carlos Mendes",
        descricao: "Atendimento residencial, manutenção preventiva e reparos com orçamento transparente.",
        especialidades: [
            { categoria: "Hidráulica" },
            { categoria: "Instalação" },
            { categoria: "Manutenção" }
        ],
        areasAtendimento: ["Maringá"],
        notaMedia: 4.9,
        totalAvaliacoes: 37,
        verificado: true
    };

    const DEMO_AVALIACOES = [
        { nota: 5, comentario: "Serviço rápido e bem feito.", nomeCliente: "João S." },
        { nota: 5, comentario: "Explicou o orçamento antes de começar.", nomeCliente: "Marina R." },
        { nota: 5, comentario: "Chegou no horário combinado.", nomeCliente: "Paulo A." }
    ];

    // =====================================
    // ELEMENTOS
    // =====================================

    const el = id => document.getElementById(id);

    const profissionalId = Number(new URLSearchParams(location.search).get("id"));
    const btnFavoritar = el("btnFavoritar");

    // =====================================
    // CARREGAMENTO
    // =====================================

    async function carregar() {

        if (!profissionalId) {
            mostrarAvisoDemo("Nenhum profissional informado. Exibindo dados de demonstração.");
            renderizar(DEMO_PERFIL, DEMO_AVALIACOES);
            return;
        }

        try {
            const [perfil, avaliacoes] = await Promise.all([
                api(`/profissionais/${profissionalId}`),
                api(`/profissionais/${profissionalId}/avaliacoes?tamanho=3`)
            ]);

            renderizar(perfil, avaliacoes?.itens || []);

        } catch (erro) {
            console.error("Erro ao carregar o perfil:", erro);
            mostrarAvisoDemo("Não foi possível carregar o perfil pela API. Exibindo dados de demonstração.");
            renderizar(DEMO_PERFIL, DEMO_AVALIACOES);
        }
    }

    // =====================================
    // RENDERIZAÇÃO
    // =====================================

    function renderizar(perfil, avaliacoes) {

        const nota = formatarNota(perfil.notaMedia);
        const total = perfil.totalAvaliacoes;
        const textoTotal = total === 1 ? "1 avaliação" : `${total} avaliações`;

        const categorias = (perfil.especialidades || []).map(e => e.categoria);

        document.title = `CHAMAPRO | ${perfil.nome}`;

        el("breadcrumbNome").textContent = `Perfil de ${perfil.nome}`;
        el("tituloNome").textContent = perfil.nome;
        el("perfilNome").textContent = perfil.nome;
        el("perfilAvatar").textContent = iniciais(perfil.nome);
        el("tagVerificado").hidden = !perfil.verificado;

        el("perfilEstrelas").innerHTML = estrelas(perfil.notaMedia);
        el("perfilNota").textContent = `${nota} · ${textoTotal}`;

        el("perfilLocal").textContent = [
            perfil.areasAtendimento?.[0],
            juntarComE(categorias)
        ].filter(Boolean).join(" · ");

        el("perfilDescricao").textContent =
            perfil.descricao || "Este profissional ainda não escreveu uma apresentação.";

        el("perfilAreas").innerHTML = categorias.length
            ? categorias.map(c => `<span class="chip">${escaparHtml(c)}</span>`).join("")
            : '<span class="perfil-local">Nenhuma área informada.</span>';

        if (perfil.id) {
            el("btnSolicitar").href = `./criar-chamado.html?profissional=${perfil.id}`;
        }

        // Resumo das avaliações
        el("notaGrande").textContent = nota;
        el("notaEstrelas").innerHTML = estrelas(perfil.notaMedia);
        el("notaTotal").textContent = total === 1
            ? "1 avaliação verificada"
            : `${total} avaliações verificadas`;

        renderizarAvaliacoes(avaliacoes);
        iniciarFavorito(perfil.id);
    }

    // ["Hidráulica", "Instalação"] → "Hidráulica e instalação"
    function juntarComE(lista) {
        const nomes = lista.map((nome, i) => i ? nome.toLowerCase() : nome);
        if (nomes.length <= 1) return nomes[0] || "";
        return `${nomes.slice(0, -1).join(", ")} e ${nomes[nomes.length - 1]}`;
    }

    function renderizarAvaliacoes(avaliacoes) {

        const lista = el("listaAvaliacoes");

        if (!avaliacoes.length) {
            lista.innerHTML = '<p class="empty-state">Este profissional ainda não recebeu avaliações.</p>';
            return;
        }

        lista.innerHTML = avaliacoes.map(a => `
            <article class="avaliacao">
                <div class="avaliacao-topo">
                    <strong>${escaparHtml(a.nomeCliente)}</strong>
                    <span class="stars" aria-label="Nota ${a.nota} de 5">${estrelas(a.nota, 11)}</span>
                </div>
                <p>${escaparHtml(a.comentario || "Avaliou sem deixar comentário.")}</p>
            </article>
        `).join("");
    }

    // =====================================
    // FAVORITAR (somente neste navegador)
    // =====================================

    const CHAVE_FAVORITOS = "chamapro_favoritos";

    function lerFavoritos() {
        try {
            return JSON.parse(localStorage.getItem(CHAVE_FAVORITOS) || "[]");
        } catch {
            return [];
        }
    }

    function iniciarFavorito(id) {

        const ativo = lerFavoritos().includes(id);
        btnFavoritar.setAttribute("aria-pressed", String(ativo));

        btnFavoritar.onclick = () => {

            const favoritos = lerFavoritos();
            const marcado = !favoritos.includes(id);

            const novos = marcado
                ? [...favoritos, id]
                : favoritos.filter(f => f !== id);

            try {
                localStorage.setItem(CHAVE_FAVORITOS, JSON.stringify(novos));
            } catch {
                // Sem armazenamento: o favorito vale só até recarregar.
            }

            btnFavoritar.setAttribute("aria-pressed", String(marcado));
            btnFavoritar.setAttribute("aria-label", marcado ? "Remover dos favoritos" : "Favoritar profissional");
        };
    }

    // =====================================
    // INICIALIZAÇÃO
    // =====================================

    carregar();
});
