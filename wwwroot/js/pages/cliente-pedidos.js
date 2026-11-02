(() => {
    const C = window.Chamapro;
    const labels = { PENDENTE: 'Aguardando propostas', ORCADO: 'Propostas recebidas', ACEITO: 'Profissional escolhido', AGENDADO: 'Serviço agendado', EM_ANDAMENTO: 'Em andamento', CONCLUIDO: 'Serviço concluído', CANCELADO: 'Pedido cancelado' };
    function next(order) {
        return ({ PENDENTE: 'Veja as propostas disponíveis e acompanhe as respostas.', ORCADO: 'Compare as propostas disponíveis.', ACEITO: 'Combine a data do serviço com o profissional.', AGENDADO: 'Confira a data e prepare-se para o atendimento.', EM_ANDAMENTO: 'Acompanhe o atendimento do profissional.', CONCLUIDO: 'Confira os detalhes e sua avaliação.', CANCELADO: 'Veja o histórico ou faça um novo pedido.' })[order.status] || 'Veja os detalhes do pedido.';
    }
    function card(order) {
        const safe = C.escaparHtml;
        return `<article class="app-row"><div><span class="badge badge-info">${safe(order.availableQuotes ? 'Propostas recebidas' : labels[order.status] || order.status)}</span><h2>${safe(order.titulo)}</h2><p class="app-muted">${safe(order.categoria)} · ${C.formatarData(order.criadoEm)}${order.cidade ? ' · ' + safe([order.cidade, order.bairro].filter(Boolean).join(' / ')) : ''}</p><p>${safe(order.nextAction || next(order))}</p></div><a class="btn btn-secondary" href="orcamentos.html?pedidoId=${order.id}">Ver pedido</a></article>`;
    }
    async function init() {
        const target = document.getElementById('ordersPageList');
        if (!target) return;
        document.getElementById('ordersPage').hidden = false;
        document.getElementById('orderContent').hidden = true;
        const previous = document.getElementById('previousOrders');
        const following = document.getElementById('nextOrders');
        const status = document.getElementById('ordersStatus');
        const pagination = document.getElementById('ordersPagination');
        const pageLabel = document.getElementById('ordersPageLabel');
        const requested = Number(new URLSearchParams(location.search).get('pagina'));
        let page = Number.isInteger(requested) && requested > 0 && requested <= 2147483647 ? requested : 1;
        const initialStatus = new URLSearchParams(location.search).get('status');
        if (['PENDENTE', 'ACEITO', 'AGENDADO', 'EM_ANDAMENTO', 'CONCLUIDO', 'CANCELADO'].includes(initialStatus)) status.value = initialStatus;
        let generation = 0;
        async function load(requestedPage) {
            const current = ++generation;
            previous.disabled = true; following.disabled = true;
            target.setAttribute('aria-busy', 'true');
            target.innerHTML = '<p class="empty-state" role="status">Carregando seus pedidos…</p>';
            try {
                const query = new URLSearchParams({ pagina: String(requestedPage), tamanho: '10' });
                if (status.value) query.set('status', status.value);
                const result = await C.api(`/pedidos?${query}`);
                if (current !== generation) return;
                const last = Math.max(1, Math.ceil(result.total / result.tamanho));
                if (requestedPage > last) { await load(last); return; }
                page = requestedPage;
                const url = new URLSearchParams({ pagina: String(page) });
                if (status.value) url.set('status', status.value);
                window.history?.replaceState(null, '', '?' + url.toString());
                pagination.hidden = result.total === 0;
                pageLabel.textContent = `Página ${page} de ${last} · ${result.total} pedidos`;
                previous.disabled = page <= 1; following.disabled = page >= last;
                target.innerHTML = result.itens.length ? result.itens.map(card).join('') : `<div class="empty-state"><h2>${status.value ? 'Nenhum pedido nesta situação' : 'Seu primeiro pedido começa aqui'}</h2><p>${status.value ? 'Escolha outra situação para ver seus pedidos.' : 'Conte o que precisa para receber propostas de profissionais.'}</p>${status.value ? '' : '<a class="btn btn-primary" href="criar-chamado.html">Pedir um serviço</a>'}</div>`;
            } catch (error) {
                if (current !== generation) return;
                pagination.hidden = true;
                target.innerHTML = `<div class="empty-state" role="alert"><p>Não foi possível carregar seus pedidos. ${C.escaparHtml(error.message)}</p><button class="btn btn-secondary" type="button" id="retryOrders">Tentar novamente</button></div>`;
                target.querySelector('#retryOrders').addEventListener('click', () => load(requestedPage));
            } finally { if (current === generation) target.setAttribute('aria-busy', 'false'); }
        }
        previous.addEventListener('click', () => load(page - 1));
        following.addEventListener('click', () => load(page + 1));
        status.addEventListener('change', () => load(1));
        await load(page);
    }
    window.ChamaproOrdersList = { init, card, next };
})();
