/* =========================================
   CHAMAPRO - TRABALHADOR / VERIFICAÇÃO E DENÚNCIAS
   Arquivo: wwwroot/js/pages/trabalhador-verificacao.js

   Funcionalidades:
   - Status da validação e documentos enviados
   - Envio de documento (POST /arquivos + POST /verificacoes)
   - Denúncia (POST /denuncias)

   A API ainda não tem rota para o profissional listar as
   próprias verificações; por isso a lista mostra os documentos
   enviados por este navegador (ou os de demonstração).

   A denúncia precisa de quem está sendo denunciado: a tela
   lê ?denunciado=ID&entidade=ID&tipo=PERFIL|PEDIDO|AVALIACAO.
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    const { api, temSessao, obterUsuario, mostrarAvisoDemo, toast,
        escaparHtml, formatarData, ICONES } = window.Chamapro;

    // =====================================
    // DADOS DE DEMONSTRAÇÃO (Figma)
    // =====================================

    const DEMO_CATEGORIA = "Eletricista";

    const DEMO_DOCUMENTOS = [
        { tipoDocumento: "Comprovante de qualificação", formato: "PDF", status: "Pendente", criadoEm: "2026-10-22T12:00:00Z" },
        { tipoDocumento: "Documento de identificação", formato: "JPG", status: "Aprovado", criadoEm: "2026-10-22T12:00:00Z" },
        { tipoDocumento: "Certificado adicional", opcional: true }
    ];

    const TIPOS_OBRIGATORIOS = ["Comprovante de qualificação", "Documento de identificação"];

    // =====================================
    // ESTADO E ELEMENTOS
    // =====================================

    const usuario = obterUsuario();
    const chaveLocal = `chamapro_verificacoes_${usuario?.id ?? "demo"}`;

    const estado = {
        demo: !temSessao(),
        documentos: []
    };

    const el = id => document.getElementById(id);

    const listaDocumentos = el("listaDocumentos");

    const modalDocumento = el("modalDocumento");
    const formDocumento = el("formDocumento");
    const tipoDocumento = el("tipoDocumento");
    const arquivoDocumento = el("arquivoDocumento");
    const btnSalvarDocumento = el("btnSalvarDocumento");

    const formDenuncia = el("formDenuncia");
    const tipoDenuncia = el("tipoDenuncia");
    const motivoDenuncia = el("motivoDenuncia");
    const descricaoDenuncia = el("descricaoDenuncia");
    const btnEnviarDenuncia = el("btnEnviarDenuncia");

    // =====================================
    // DOCUMENTOS SALVOS NESTE NAVEGADOR
    // =====================================

    function lerDocumentosLocais() {
        try {
            return JSON.parse(localStorage.getItem(chaveLocal) || "[]");
        } catch {
            return [];
        }
    }

    function salvarDocumentosLocais() {
        try {
            localStorage.setItem(chaveLocal, JSON.stringify(estado.documentos));
        } catch {
            // Sem armazenamento disponível: a lista vale só para esta visita.
        }
    }

    // =====================================
    // CARREGAMENTO
    // =====================================

    function carregar() {

        if (estado.demo) {
            estado.documentos = structuredClone(DEMO_DOCUMENTOS);
            el("tituloCategoria").textContent = DEMO_CATEGORIA;
            mostrarAvisoDemo("Entre como profissional para enviar documentos reais. Exibindo dados de demonstração.");
        } else {
            estado.documentos = lerDocumentosLocais();
            el("tituloCategoria").textContent = "Perfil profissional";
            mostrarAvisoDemo("A lista mostra os documentos enviados por este navegador; a API ainda não devolve o histórico de verificações.");
        }

        preencherDenunciaPelaUrl();
        renderizar();
    }

    // =====================================
    // STATUS GERAL
    // =====================================

    function calcularStatus() {

        const enviados = estado.documentos.filter(d => d.status);

        if (!enviados.length) {
            return { etapa: 0, rotulo: "PENDENTE", tag: "", texto: "Envie seus documentos para começar a validação desta categoria." };
        }

        if (enviados.some(d => d.status === "Rejeitado")) {
            return { etapa: 1, rotulo: "REPROVADO", tag: "tag-danger", texto: "Algum documento foi reprovado. Envie uma nova versão para continuar." };
        }

        const aprovados = TIPOS_OBRIGATORIOS.every(tipo =>
            enviados.some(d => d.tipoDocumento === tipo && d.status === "Aprovado")
        );

        if (aprovados) {
            return { etapa: 3, rotulo: "APROVADO", tag: "tag-success", texto: "Perfil validado! Você já pode enviar propostas nesta categoria." };
        }

        return { etapa: 2, rotulo: "EM ANÁLISE", tag: "tag-warning", texto: "A equipe está verificando as informações. Você poderá enviar propostas nesta categoria após a aprovação." };
    }

    function renderizarStatus() {

        const status = calcularStatus();

        const tag = el("tagStatus");
        tag.className = `tag ${status.tag}`;
        tag.querySelector("span").textContent = status.rotulo;

        el("textoStatus").textContent = status.texto;

        const datas = estado.documentos.filter(d => d.criadoEm).map(d => new Date(d.criadoEm));
        const primeira = datas.length ? new Date(Math.min(...datas)) : null;

        el("dataEnvio").textContent = primeira ? `Enviado em ${formatarData(primeira)}` : "";

        // Etapas: 1 = Enviado, 2 = Em análise, 3 = Aprovado
        const pontos = document.querySelectorAll(".progresso-ponto");
        const linhas = document.querySelectorAll(".progresso-linha");
        const rotulos = document.querySelectorAll(".progresso-rotulos li");

        pontos.forEach((ponto, i) => {
            const etapa = i + 1;
            ponto.classList.toggle("feito", etapa < status.etapa || status.etapa === 3);
            ponto.classList.toggle("atual", etapa === status.etapa && status.etapa !== 3);
            rotulos[i].classList.toggle("atual", etapa === status.etapa);
        });

        linhas.forEach((linha, i) => {
            linha.classList.toggle("feito", i + 1 < status.etapa);
        });
    }

    // =====================================
    // LISTA DE DOCUMENTOS
    // =====================================

    function tagDocumento(doc) {

        if (doc.status === "Aprovado") {
            return `<span class="tag tag-success">${ICONES.check} Validado</span>`;
        }

        if (doc.status === "Rejeitado") {
            return '<span class="tag tag-danger">Reprovado</span>';
        }

        if (doc.status) {
            return `<span class="tag tag-warning">${ICONES.relogio} Em análise</span>`;
        }

        return '<span class="tag">Pendente</span>';
    }

    function renderizar() {

        renderizarStatus();

        if (!estado.documentos.length) {
            listaDocumentos.innerHTML = '<p class="empty-state">Nenhum documento enviado ainda.</p>';
            return;
        }

        listaDocumentos.innerHTML = estado.documentos.map(doc => {

            const detalhe = doc.status
                ? `${doc.formato || "Arquivo"} · enviado`
                : (doc.opcional ? "Opcional" : "Não enviado");

            return `
                <div class="list-row">
                    <span class="documento-icone">${ICONES.documento}</span>
                    <div class="list-row-info">
                        <strong>${escaparHtml(doc.tipoDocumento)}</strong>
                        <span>${escaparHtml(detalhe)}</span>
                    </div>
                    ${tagDocumento(doc)}
                </div>
            `;
        }).join("");
    }

    // =====================================
    // ENVIO DE DOCUMENTO
    // =====================================

    el("btnEnviarDocumento").addEventListener("click", () => {
        formDocumento.reset();
        modalDocumento.showModal();
    });

    el("btnCancelarDocumento").addEventListener("click", () => modalDocumento.close());

    formDocumento.addEventListener("submit", async (event) => {

        event.preventDefault();

        const arquivo = arquivoDocumento.files[0];
        const tipo = tipoDocumento.value;

        if (!arquivo) {
            toast("Escolha o arquivo do documento.", "error");
            return;
        }

        if (arquivo.size > 5 * 1024 * 1024) {
            toast("O arquivo deve ter no máximo 5 MB.", "error");
            return;
        }

        btnSalvarDocumento.disabled = true;
        btnSalvarDocumento.textContent = "Enviando...";

        try {
            let verificacao;

            if (estado.demo) {
                verificacao = { status: "Pendente", criadoEm: new Date().toISOString() };
            } else {
                const dados = new FormData();
                dados.append("arquivo", arquivo);

                const upload = await api("/arquivos", { method: "POST", body: dados });

                verificacao = await api("/verificacoes", {
                    method: "POST",
                    body: { tipoDocumento: tipo, urlDocumento: upload.url }
                });
            }

            const formato = (arquivo.name.split(".").pop() || "").toUpperCase();

            // Substitui o documento do mesmo tipo (reenvio)
            estado.documentos = estado.documentos.filter(d => d.tipoDocumento !== tipo);
            estado.documentos.push({
                id: verificacao.id,
                tipoDocumento: tipo,
                formato,
                status: verificacao.status,
                criadoEm: verificacao.criadoEm
            });

            if (!estado.demo) salvarDocumentosLocais();

            renderizar();
            modalDocumento.close();
            toast("Documento enviado para análise.", "success");

        } catch (erro) {
            toast(erro.message, "error");
        } finally {
            btnSalvarDocumento.disabled = false;
            btnSalvarDocumento.textContent = "Enviar";
        }
    });

    // =====================================
    // DENÚNCIA
    // =====================================

    const parametros = new URLSearchParams(location.search);

    function preencherDenunciaPelaUrl() {

        const tipo = (parametros.get("tipo") || "").toUpperCase();

        if ([...tipoDenuncia.options].some(o => o.value === tipo)) {
            tipoDenuncia.value = tipo;
        }
    }

    formDenuncia.addEventListener("submit", async (event) => {

        event.preventDefault();

        const descricao = descricaoDenuncia.value.trim();

        if (!tipoDenuncia.value) {
            toast("Escolha o que deseja denunciar.", "error");
            tipoDenuncia.focus();
            return;
        }

        if (descricao.length < 10) {
            toast("Descreva o que aconteceu (mínimo de 10 caracteres).", "error");
            descricaoDenuncia.focus();
            return;
        }

        const denunciadoId = Number(parametros.get("denunciado"));
        const entidadeId = Number(parametros.get("entidade")) || denunciadoId;

        if (!estado.demo && !denunciadoId) {
            toast("Abra a denúncia pelo perfil, chamado ou avaliação que deseja denunciar.", "error");
            return;
        }

        btnEnviarDenuncia.disabled = true;

        try {
            if (!estado.demo) {
                await api("/denuncias", {
                    method: "POST",
                    body: {
                        denunciadoId,
                        tipoEntidade: tipoDenuncia.value,
                        entidadeId,
                        motivo: motivoDenuncia.value,
                        descricao
                    }
                });
            }

            formDenuncia.reset();
            toast("Denúncia enviada. A equipe vai analisar com sigilo.", "success");

        } catch (erro) {
            toast(erro.message, "error");
        } finally {
            btnEnviarDenuncia.disabled = false;
        }
    });

    // =====================================
    // INICIALIZAÇÃO
    // =====================================

    carregar();
});
