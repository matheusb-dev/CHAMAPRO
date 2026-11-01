(() => {
    const links = {
        CLIENTE: [
            ["Painel", "/pages/cliente/painel.html"],
            ["Criar chamado", "/pages/cliente/criar-chamado.html"],
            ["Orçamentos", "/pages/cliente/orcamentos.html"],
            ["Favoritos", "/pages/cliente/painel.html#favoritos"],
            ["Perfil", "/pages/cliente/perfil.html"]
        ],
        PROFISSIONAL: [
            ["Fila de chamados", "/pages/trabalhador/fila-chamados.html"],
            ["Meu perfil", "/pages/trabalhador/perfil.html"],
            ["Verificação", "/pages/trabalhador/verificacao.html"]
        ],
        ADMIN: [["Gestão", "/pages/admin/gestao.html"]]
    };
    function render(container, user) {
        const perfil = String(user.perfil || "").toUpperCase();
        const items = links[perfil] || [];
        container.className = "app-sidebar";
        container.innerHTML = `<nav aria-label="Menu principal"><span class="sidebar-label">MENU</span>${items.map(([nome, href]) =>
            `<a class="sidebar-link ${location.pathname === href ? "active" : ""}" href="${href}">${nome}</a>`).join("")}</nav>`;
    }
    window.ChamaproSidebar = { render };
})();
