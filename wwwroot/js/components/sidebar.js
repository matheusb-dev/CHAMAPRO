(() => {
    const icons = {
        Painel: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
        'Criar chamado': '<rect x="4" y="4" width="16" height="16" rx="4"/><path d="M12 8v8m-4-4h8"/>',
        'Orçamentos': '<rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 8h6m-6 4h6m-6 4h3"/>',
        Favoritos: '<path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z"/>',
        profile: '<circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/>',
        'Fila de chamados': '<path d="M9 5h12M9 12h12M9 19h12M3 5h1m-1 7h1m-1 7h1"/>',
        'Cadastro profissional': '<rect x="3" y="7" width="18" height="14" rx="2"/><path d="M8 7V3h8v4M3 12h18m-9-2v4"/>',
        'Verificação': '<path d="m12 2 9 4v6c0 5-9 10-9 10S3 17 3 12V6l9-4Z"/><path d="m8 12 3 3 5-6"/>',
        'Gestão': '<path d="M4 20V10m8 10V4m8 16v-7M2 20h20"/>'
    };
    icons['Início'] = icons.Painel;
    icons['Encontrar serviços'] = icons['Fila de chamados'];
    icons['Minhas propostas'] = icons['Orçamentos'];
    icons['Meus trabalhos'] = icons['Cadastro profissional'];
    icons['Pedir um serviço'] = icons['Criar chamado'];
    icons['Meus pedidos'] = icons['Orçamentos'];
    icons['Meus profissionais'] = icons.Favoritos;
    const links = {
        CLIENTE: [['Início','/pages/cliente/painel.html'],['Pedir um serviço','/pages/cliente/criar-chamado.html'],['Meus pedidos','/pages/cliente/orcamentos.html'],['Meus profissionais','/pages/cliente/favoritos.html'],['Minha conta','/pages/cliente/perfil.html']],
        PROFISSIONAL: [['Início','/pages/trabalhador/painel.html'],['Encontrar serviços','/pages/trabalhador/fila-chamados.html#oportunidades'],['Minhas propostas','/pages/trabalhador/fila-chamados.html#propostas'],['Meus trabalhos','/pages/trabalhador/fila-chamados.html#trabalhos'],['Cadastro profissional','/pages/trabalhador/cadastro-profissional.html'],['Meu perfil','/pages/trabalhador/perfil.html'],['Verificação','/pages/trabalhador/verificacao.html']],
        ADMIN: [['Gestão','/pages/admin/gestao.html']]
    };
    function render(container, user) {
        const perfil = String(user.perfil || '').toUpperCase();
        const items = links[perfil] || [];
        container.className = 'app-sidebar';
        container.id = 'appNavigation';
        container.innerHTML = `<nav aria-label="Menu principal"><span class="sidebar-label">SEU ESPAÇO</span>${items.map(([nome,href]) => `<a class="sidebar-link" href="${href}"><svg width="19" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${icons[nome] || icons.profile}</svg><span>${nome}</span></a>`).join('')}</nav><div class="sidebar-footer"><strong>CHAMAPRO</strong>Conectando quem precisa<br>a quem sabe fazer.</div>`;
        function updateActive() {
            container.querySelectorAll('.sidebar-link').forEach(link => {
                const sectionLink = items.some(([, href]) => href === location.pathname + location.hash && href.includes('#'));
                const currentHash = perfil === 'PROFISSIONAL' && location.pathname.endsWith('/fila-chamados.html') && !['#propostas','#trabalhos'].includes(location.hash) ? '#oportunidades' : location.hash;
                const active = location.pathname === link.pathname && (sectionLink || currentHash === '#oportunidades' ? currentHash === link.hash : !link.hash);
                link.classList.toggle('active', active);
                if (active) link.setAttribute('aria-current', 'page'); else link.removeAttribute('aria-current');
            });
        }
        updateActive();
        window.addEventListener('hashchange', updateActive);
        const toggle = document.getElementById('navigationButton');
        if (toggle) {
            const media = window.matchMedia('(max-width: 850px)');
            function syncMenu() { container.classList.toggle('menu-collapsed', media.matches); toggle.setAttribute('aria-expanded', String(!media.matches)); }
            syncMenu();
            media.addEventListener('change', syncMenu);
            toggle.addEventListener('click', () => { const closed = container.classList.toggle('menu-collapsed'); toggle.setAttribute('aria-expanded', String(!closed)); });
            container.addEventListener('keydown', event => { if (event.key === 'Escape' && media.matches) { container.classList.add('menu-collapsed'); toggle.setAttribute('aria-expanded', 'false'); toggle.focus(); } });
        }
    }
    window.ChamaproSidebar = { render };
})();
