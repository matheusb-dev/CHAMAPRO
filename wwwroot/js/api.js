/* =========================================
   CHAMAPRO - CLIENTE DA API E UTILITÁRIOS
   Arquivo: wwwroot/js/api.js

   Funcionalidades:
   - Requisições autenticadas (JWT salvo no login)
   - Leitura da sessão do usuário
   - Preenchimento do header (nome e iniciais)
   - Toast de feedback e formatação
========================================= */

(() => {

    const API_BASE = "/api/v1";

    // =====================================
    // SESSÃO
    // =====================================

    function obterToken() {
        try {
            return sessionStorage.getItem("chamapro_token");
        } catch {
            return null;
        }
    }

    function obterUsuario() {
        try {
            return JSON.parse(sessionStorage.getItem("chamapro_usuario") || "null");
        } catch {
            return null;
        }
    }

    function temSessao() {
        return Boolean(obterToken());
    }

    // =====================================
    // REQUISIÇÕES
    // =====================================

    async function api(caminho, opcoes = {}) {

        const headers = { ...(opcoes.headers || {}) };
        const token = obterToken();

        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }

        let body = opcoes.body;

        if (body && !(body instanceof FormData) && typeof body !== "string") {
            headers["Content-Type"] = "application/json";
            body = JSON.stringify(body);
        }

        let response, texto;
        try {
            response = await fetch(`${API_BASE}${caminho}`, { ...opcoes, headers, body });
            texto = await response.text();
        } catch (cause) {
            if (cause?.name === "AbortError") throw cause;
            const erro = new Error("Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.");
            erro.status = 0;
            erro.codigo = "FALHA_REDE";
            throw erro;
        }
        let resultado = null;

        try {
            resultado = texto ? JSON.parse(texto) : null;
        } catch {
            resultado = null;
        }

        if (!response.ok) {
            const erro = new Error(
                resultado?.erros?.[0]?.mensagem ||
                resultado?.detalhe ||
                resultado?.titulo ||
                `Erro ${response.status} ao acessar a API.`
            );

            erro.status = response.status;
            erro.codigo = resultado?.codigo || null;
            erro.campos = resultado?.erros || [];
            throw erro;
        }

        return resultado;
    }

    // =====================================
    // HEADER: NOME E INICIAIS DO USUÁRIO
    // =====================================

    function iniciais(nome) {
        return String(nome || "")
            .trim()
            .split(/\s+/)
            .filter(Boolean)
            .slice(0, 2)
            .map(parte => parte[0].toUpperCase())
            .join("");
    }

    function preencherUsuario() {

        const usuario = obterUsuario();
        if (!usuario?.nome) return;

        document.querySelectorAll("[data-usuario-nome]").forEach(el => {
            el.textContent = usuario.nome;
        });

        document.querySelectorAll("[data-usuario-iniciais]").forEach(el => {
            el.textContent = iniciais(usuario.nome);
        });
    }

    // =====================================
    // TOAST
    // =====================================

    let toastTimer;

    function toast(mensagem, tipo = "info") {

        let el = document.getElementById("appToast");

        if (!el) {
            el = document.createElement("div");
            el.id = "appToast";
            el.setAttribute("role", "status");
            el.setAttribute("aria-live", "polite");
            document.body.appendChild(el);
        }

        el.className = `toast toast-${tipo}`;
        el.textContent = mensagem;

        requestAnimationFrame(() => el.classList.add("visible"));

        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove("visible"), 3800);
    }

    // =====================================
    // FORMATAÇÃO
    // =====================================

    const moeda = new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL"
    });

    function formatarMoeda(valor) {
        return moeda.format(Number(valor) || 0);
    }

    function normalizarWhatsApp(value) {
        const raw=String(value||'').trim();
        if (!raw || /[^\d\s()+.\-]/.test(raw)) return null;
        let digits=raw.replace(/\D/g,'');
        const international=raw.startsWith('+')||raw.startsWith('00');
        if(raw.startsWith('00'))digits=digits.slice(2);
        if(!international && [10,11].includes(digits.length))digits='55'+digits;
        if(!/^[1-9]\d{7,14}$/.test(digits)||/^(\d)\1+$/.test(digits))return null;
        if(digits.startsWith('55')) {
            const local=digits.slice(2), ddd=Number(local.slice(0,2));
            const validDDD=[11,12,13,14,15,16,17,18,19,21,22,24,27,28,31,32,33,34,35,37,38,41,42,43,44,45,46,47,48,49,51,53,54,55,61,62,63,64,65,66,67,68,69,71,73,74,75,77,79,81,82,83,84,85,86,87,88,89,91,92,93,94,95,96,97,98,99];
            if(!validDDD.includes(ddd)||!(/^[1-9]\d[2-5]\d{7}$/.test(local)||/^[1-9]\d9\d{8}$/.test(local)))return null;
        } else if(!international)return null;
        return digits;
    }
    function contatoWhatsApp(telefone, mensagem='Olá! Estou entrando em contato pelo CHAMAPRO.') {
        const number=normalizarWhatsApp(telefone);
        return number ? '<a class="btn btn-primary btn-small" target="_blank" rel="noopener noreferrer" href="https://wa.me/'+number+'?text='+encodeURIComponent(mensagem)+'">Entrar em contato</a>' : '<span class="app-muted">WhatsApp não disponível</span>';
    }
    function inicioDoUsuario(user) {
        return ({CLIENTE:'/pages/cliente/painel.html',PROFISSIONAL:'/pages/trabalhador/painel.html',ADMIN:'/pages/admin/gestao.html'})[String(user?.perfil||'').toUpperCase()]||'/';
    }
    function formatarNota(nota) {
        return (Number(nota) > 0 ? Number(nota) : 5).toFixed(1).replace(".", ",");
    }

    function formatarData(data) {
        if (!data) return "";
        return new Date(data).toLocaleDateString("pt-BR");
    }

    function escaparHtml(texto) {
        return String(texto ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    // =====================================
    // ÍCONES (exportados do Figma)
    // =====================================

    const ICONES = {
        check: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        relogio: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M10.5 6C10.5 8.48528 8.48528 10.5 6 10.5C3.51472 10.5 1.5 8.48528 1.5 6C1.5 3.51472 3.51472 1.5 6 1.5C8.48528 1.5 10.5 3.51472 10.5 6Z" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/><path d="M6 3.5V6L7.5 7" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        estrelaContorno: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M7 1.75L8.575 5.01667L12.1334 5.54167L9.5667 8.05L10.15 11.6083L7 9.91667L3.85 11.6083L4.43337 8.05L1.8667 5.54167L5.425 5.01667L7 1.75Z" stroke="currentColor" stroke-width="1.28333" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        estrela14: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M7 1.75L8.575 5.01667L12.1334 5.54167L9.5667 8.05L10.15 11.6083L7 9.91667L3.85 11.6083L4.43337 8.05L1.8667 5.54167L5.425 5.01667L7 1.75Z" fill="currentColor" stroke="currentColor" stroke-width="1.16667" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        estrela11: '<svg width="11" height="11" viewBox="0 0 11 11" fill="none" aria-hidden="true"><path d="M5.5 1.375L6.73739 3.94167L9.53322 4.35417L7.51655 6.325L7.97489 9.12083L5.5 7.79167L3.02489 9.12083L3.48322 6.325L1.46655 4.35417L4.26239 3.94167L5.5 1.375Z" fill="currentColor" stroke="currentColor" stroke-width="0.916667" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        seta: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M2.9165 7H11.0832M7.58317 10.5L11.0832 7L7.58317 3.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        bandeira: '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M3.3335 14V8M3.3335 8V2.66663H10.6668L9.3335 5.33329L10.6668 8H3.3335Z" stroke="currentColor" stroke-width="1.33333" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        documento: '<svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true"><path d="M10.5 2.25H4.5V15.75H13.5V5.25L10.5 2.25Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M10.5 2.25V5.25H13.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    };

    function estrelas(nota, tamanho = 14) {

        const icone = tamanho === 11 ? ICONES.estrela11 : ICONES.estrela14;
        const cheias = Math.round(Number(nota) > 0 ? Number(nota) : 5);

        return Array.from({ length: 5 }, (_, i) =>
            i < cheias ? icone : `<span class="star-off">${icone}</span>`
        ).join("");
    }

    // =====================================
    // EXPOSIÇÃO GLOBAL
    // =====================================

    window.Chamapro = {
        api,
        obterToken,
        obterUsuario,
        temSessao,
        iniciais,
        preencherUsuario,
        toast,
        formatarMoeda,
        formatarNota,
        normalizarWhatsApp,
        contatoWhatsApp,
        inicioDoUsuario,
        formatarData,
        escaparHtml,
        estrelas,
        ICONES
    };

    document.addEventListener("DOMContentLoaded", () => {
        preencherUsuario();
        document.querySelectorAll('a.brand, a.logo').forEach(link => link.href=inicioDoUsuario(obterUsuario()));
    });

})();
