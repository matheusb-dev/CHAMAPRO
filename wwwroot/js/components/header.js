(() => {
    // Keep the shared shell styled even when navigating from an older cached HTML.
    const stylesheetUrl = new URL('/css/pages/authenticated.css?v=20261102-simple1', window.location.origin);
    function ensureShellStyles() {
        document.body.classList.add('app-auth');
        const stylesheet = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).find(link =>
            new URL(link.href, window.location.href).pathname === stylesheetUrl.pathname);
        if (stylesheet) {
            if (stylesheet.href !== stylesheetUrl.href) stylesheet.href = stylesheetUrl.href;
        } else {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = stylesheetUrl.href;
            document.head.append(link);
        }
    }
    function render(container, user) {
        ensureShellStyles();
        container.className = "app-topbar";
        container.innerHTML = `
            <a class="brand" href="${window.Chamapro.inicioDoUsuario(user)}" aria-label="CHAMAPRO início"><span class="brand-symbol">C</span><span class="brand-name">CHAMA<span>PRO</span></span></a>
            <div class="topbar-actions"><button class="icon-button menu-toggle" type="button" id="navigationButton" aria-label="Alternar menu de navegação" aria-controls="appNavigation" aria-expanded="false"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16"/></svg></button>
                <button class="icon-button" type="button" id="notificationsButton" aria-label="Notificações" aria-expanded="false" aria-controls="notificationPanel"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/></svg></button>
                <div class="topbar-user"><span class="avatar" data-usuario-iniciais></span><div class="account-text"><strong>${window.Chamapro.escaparHtml(user.nome)}</strong><span class="account-role">${({ CLIENTE: "Área do cliente", PROFISSIONAL: "Área do profissional", ADMIN: "Administração" })[String(user.perfil || "").toUpperCase()] || "Minha conta"}</span></div></div>
                <button class="btn btn-secondary btn-small" type="button" id="logoutButton">Sair</button>
            </div>
            <section class="notification-panel" id="notificationPanel" hidden aria-label="Notificações"></section>`;
        container.querySelector("#logoutButton").addEventListener("click", () => {
            sessionStorage.removeItem("chamapro_token");
            sessionStorage.removeItem("chamapro_usuario");
            window.location.assign("/pages/autenticacao/login.html");
        });
    }
    window.ChamaproHeader = { render };
})();
