(() => {
    function render(container, user) {
        container.className = "app-topbar";
        container.innerHTML = `
            <a class="brand" href="/" aria-label="CHAMAPRO início"><span class="brand-symbol">C</span><span class="brand-name">CHAMA<span>PRO</span></span></a>
            <div class="topbar-actions">
                <button class="icon-button" type="button" id="notificationsButton" aria-label="Notificações" aria-expanded="false">♧</button>
                <div class="topbar-user"><span class="avatar" data-usuario-iniciais></span><strong>${window.Chamapro.escaparHtml(user.nome)}</strong></div>
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
