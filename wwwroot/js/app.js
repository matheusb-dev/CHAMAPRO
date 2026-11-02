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

    let sessionPanel, protectedContent, contentWasHidden;
    function restoreContent() {
        if (sessionPanel) sessionPanel.hidden = true;
        if (protectedContent) protectedContent.hidden = contentWasHidden;
    }
    function waitForRetry() {
        protectedContent ||= document.querySelector('.app-content');
        if (protectedContent && contentWasHidden === undefined) contentWasHidden = protectedContent.hidden;
        if (protectedContent) protectedContent.hidden = true;
        if (!sessionPanel) {
            sessionPanel = document.createElement('section');
            sessionPanel.className = 'app-card';
            sessionPanel.setAttribute('role', 'alert');
            if (protectedContent) protectedContent.before(sessionPanel); else document.body.append(sessionPanel);
        }
        sessionPanel.hidden = false;
        sessionPanel.innerHTML = '<h1>Não foi possível confirmar sua sessão</h1><p>Confira sua conexão e tente novamente. Você não precisa preencher o login outra vez por causa desta falha.</p><button id="retrySession" class="btn btn-primary" type="button">Tentar novamente</button> <a href="/pages/autenticacao/login.html">Ir para o login</a>';
        return new Promise(resolve => sessionPanel.querySelector('#retrySession').addEventListener('click', event => {
            const button = event.currentTarget;
            if (button.disabled) return;
            button.disabled = true;
            button.textContent = 'Conferindo sessão…';
            resolve();
        }));
    }
    async function inicializar() {
      while (true) {
        if (!window.Chamapro?.temSessao()) {
            window.location.replace("/pages/autenticacao/login.html");
            return null;
        }

        try {
            const usuario = await window.Chamapro.api("/auth/me");
            if (usuario.ativo === false) throw { status: 401 };
            const perfil = String(usuario.perfil || "").toUpperCase();
            const requisito = exigidosPorCaminho.find(([prefixo]) => location.pathname.startsWith(prefixo));
            if (requisito && requisito[1] !== perfil) {
                window.location.replace(destinos[perfil] || "/");
                return null;
            }
            restoreContent();
            sessionStorage.setItem("chamapro_usuario", JSON.stringify(usuario));
            document.body.dataset.perfil = perfil.toLowerCase();
            document.querySelectorAll("[data-app-header]").forEach(el => window.ChamaproHeader?.render(el, usuario));
            document.querySelectorAll("[data-app-sidebar]").forEach(el => window.ChamaproSidebar?.render(el, usuario));
            window.ChamaproNotifications?.init();
            window.Chamapro?.preencherUsuario();
            return usuario;
        } catch (erro) {
            const invalidSession = [401, 403].includes(erro.status) || (erro.status === 404 && erro.codigo === 'USUARIO_NAO_ENCONTRADO');
            if (!invalidSession) {
                await waitForRetry();
                continue;
            }
            sessionStorage.removeItem("chamapro_token");
            sessionStorage.removeItem("chamapro_usuario");
            window.location.replace("/pages/autenticacao/login.html?expirada=1");
            return null;
        }
      }
    }

    const ready = new Promise(resolve => {
        document.addEventListener("DOMContentLoaded", () => inicializar().then(resolve), { once: true });
    });
    window.ChamaproApp = { ready, destinos };
})();
