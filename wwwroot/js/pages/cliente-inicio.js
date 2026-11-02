(() => {
    const C = window.Chamapro;
    async function load() {
        const target = document.getElementById('homeOrdersList');
        target.setAttribute('aria-busy', 'true');
        target.innerHTML = '<p class="empty-state" role="status">Carregando pedidos em andamento…</p>';
        try {
            const pages = await Promise.all(['PENDENTE', 'ORCADO', 'ACEITO', 'AGENDADO', 'EM_ANDAMENTO'].map(status => C.api(`/pedidos?status=${status}&pagina=1&tamanho=3`)));
            const orders = pages.flatMap(page => page.itens).sort((a, b) => new Date(b.criadoEm) - new Date(a.criadoEm)).slice(0, 3);
            const quotes = await Promise.allSettled(orders.map(order => order.status === 'PENDENTE' ? C.api(`/pedidos/${order.id}/orcamentos`) : Promise.resolve([])));
            orders.forEach((order, index) => {
                const response = quotes[index];
                if (response.status === 'fulfilled') {
                    const available = response.value.filter(quote => quote.situacao === 'ENVIADO' && !quote.expirado).length;
                    if (available) { order.availableQuotes = available; order.nextAction = 'Você tem ' + available + (available === 1 ? ' proposta disponível. Confira os detalhes.' : ' propostas disponíveis. Compare e escolha.'); }
                }
            });
            document.getElementById('firstServiceGuide').hidden = true;
            target.innerHTML = orders.length ? orders.map(order => window.ChamaproOrdersList.card(order)).join('') : '<div class="empty-state"><h2>Nenhum pedido em andamento</h2><p>Quando precisar, toque em “Pedir um serviço”. Seus pedidos anteriores continuam em “Meus pedidos”.</p></div>';
        } catch (error) {
            target.innerHTML = `<div class="empty-state" role="alert"><p>Não foi possível carregar os pedidos. ${C.escaparHtml(error.message)}</p><button class="btn btn-secondary" type="button" id="retryHome">Tentar novamente</button></div>`;
            target.querySelector('#retryHome').addEventListener('click', load);
        } finally { target.setAttribute('aria-busy', 'false'); }
    }
    async function notices() {
        const target = document.getElementById('homeNotices');
        const results = await Promise.allSettled([C.api('/avaliacoes/pendentes'), C.api('/agendamentos/me')]);
        const messages = [];
        if (results[0].status === 'fulfilled') messages.push(...results[0].value.slice(0, 3).map(review => `<article class="app-row"><div><strong>Como foi o atendimento?</strong><p>${C.escaparHtml(review.titulo)}</p></div><a class="btn btn-secondary" href="orcamentos.html?pedidoId=${review.pedidoId}#avaliar">Avaliar serviço</a></article>`));
        if (results[1].status === 'fulfilled') messages.push(...results[1].value.slice(0, 3).map(appointment => `<article class="app-row"><div><strong>${C.escaparHtml(appointment.titulo)}</strong><p>${C.formatarData(appointment.dataHora)} · ${C.escaparHtml(appointment.profissionalNome)}</p></div><a class="btn btn-secondary" href="orcamentos.html?pedidoId=${appointment.pedidoId}">Ver agendamento</a></article>`));
        if (results.some(result => result.status === 'rejected')) messages.push('<p class="app-muted">Não foi possível verificar todos os avisos. Consulte os detalhes em Meus pedidos.</p>');
        target.innerHTML = messages.join('');
        document.getElementById('homeNoticesSection').hidden = messages.length === 0;
    }
    window.ChamaproApp?.ready.then(user => { if (user?.perfil === 'CLIENTE') { load(); notices(); } });
})();
