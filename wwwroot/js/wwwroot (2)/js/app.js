(() => {
    const destinos = {
        CLIENTE: "/pages/cliente/painel.html",
        PROFISSIONAL: "/pages/trabalhador/painel.html",
        ADMIN: "/pages/admin/gestao.html"
    };
    const exigidosPorCaminho = [
        ["/pages/cliente/", "CLIENTE"],
        ["/pages/trabalhador/", "PROFISSIONAL"],
        ["/pages/admin/", "ADMIN"]
    ];

    async function inicializar() {
        if (!window.Chamapro?.temSessao()) {
            window.location.replace("/pages/autenticacao/login.html");
            return null;
        }

        let usuario;
        try {
            usuario = await window.Chamapro.api("/auth/me");
        } catch (erro) {
            // Só encerra a sessão se o servidor recusou o token (401).
            if (erro.status === 401) {
                sessionStorage.removeItem("chamapro_token");
                sessionStorage.removeItem("chamapro_usuario");
                window.location.replace("/pages/autenticacao/login.html?expirada=1");
                return null;
            }

            // Falha temporária (rede, 429, 5xx): mantém a sessão e usa o usuário em cache.
            usuario = window.Chamapro.obterUsuario();
            if (!usuario) {
                window.Chamapro.toast("Servidor indisponível. Atualize a página em instantes.", "error");
                return null;
            }
            console.warn("Falha ao validar a sessão; usando usuário em cache.", erro);
        }

        const perfil = String(usuario.perfil || "").toUpperCase();
        const requisito = exigidosPorCaminho.find(([prefixo]) => location.pathname.startsWith(prefixo));
        if (requisito && requisito[1] !== perfil) {
            window.location.replace(destinos[perfil] || "/");
            return null;
        }

        sessionStorage.setItem("chamapro_usuario", JSON.stringify(usuario));
        document.body.dataset.perfil = perfil.toLowerCase();

        // Erros ao montar o layout NÃO podem derrubar a sessão.
        try {
            document.querySelectorAll("[data-app-header]").forEach(el => window.ChamaproHeader?.render(el, usuario));
            document.querySelectorAll("[data-app-sidebar]").forEach(el => window.ChamaproSidebar?.render(el, usuario));
            window.ChamaproNotifications?.init();
            window.Chamapro?.preencherUsuario();
        } catch (erro) {
            console.error("Falha ao montar o layout:", erro);
        }

        return usuario;
    }

    const ready = new Promise(resolve => {
        document.addEventListener("DOMContentLoaded", () => inicializar().then(resolve), { once: true });
    });
    window.ChamaproApp = { ready, destinos };
})();
