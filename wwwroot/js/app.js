(() => {
    const destinos = {
        CLIENTE: "/pages/cliente/painel.html",
        PROFISSIONAL: "/pages/trabalhador/fila-chamados.html",
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

        try {
            const usuario = await window.Chamapro.api("/auth/me");
            const perfil = String(usuario.perfil || "").toUpperCase();
            const requisito = exigidosPorCaminho.find(([prefixo]) => location.pathname.startsWith(prefixo));
            if (requisito && requisito[1] !== perfil) {
                window.location.replace(destinos[perfil] || "/");
                return null;
            }
            sessionStorage.setItem("chamapro_usuario", JSON.stringify(usuario));
            document.body.dataset.perfil = perfil.toLowerCase();
            document.querySelectorAll("[data-app-header]").forEach(el => window.ChamaproHeader?.render(el, usuario));
            document.querySelectorAll("[data-app-sidebar]").forEach(el => window.ChamaproSidebar?.render(el, usuario));
            window.ChamaproNotifications?.init();
            window.Chamapro?.preencherUsuario();
            return usuario;
        } catch (erro) {
            sessionStorage.removeItem("chamapro_token");
            sessionStorage.removeItem("chamapro_usuario");
            window.location.replace("/pages/autenticacao/login.html?expirada=1");
            return null;
        }
    }

    const ready = new Promise(resolve => {
        document.addEventListener("DOMContentLoaded", () => inicializar().then(resolve), { once: true });
    });
    window.ChamaproApp = { ready, destinos };
})();
