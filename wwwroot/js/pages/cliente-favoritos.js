(() => {
    const C = window.Chamapro;
    const target = document.getElementById('favoritesList');
    async function load() {
        target.setAttribute('aria-busy', 'true');
        try {
            const favorites = await C.api('/favoritos');
            target.innerHTML = favorites.length ? favorites.map(f => `<article class="app-row"><div><strong>${C.escaparHtml(f.nome)}</strong><p class="app-muted">Avaliação: ${C.formatarNota(f.notaMedia)}</p></div><div class="app-actions"><a class="btn btn-primary" href="perfil-profissional.html?id=${f.profissionalId}">Ver profissional</a><a class="btn btn-secondary" href="criar-chamado.html?profissionalId=${f.profissionalId}">Pedir outro serviço</a><button type="button" class="btn btn-secondary" data-unfavorite="${f.profissionalId}">Remover dos favoritos</button></div></article>`).join('') : '<div class="empty-state"><h2>Salve quem você quer encontrar de novo</h2><p>Ao visitar o perfil de um profissional, toque em “Favoritar”. Ele aparecerá aqui.</p><a class="btn btn-primary" href="criar-chamado.html">Pedir um serviço</a></div>';
            target.querySelectorAll('[data-unfavorite]').forEach(button => button.addEventListener('click', async () => {
                if (button.disabled) return;
                button.disabled = true;
                try { await C.api(`/favoritos/${button.dataset.unfavorite}`, { method: 'DELETE' }); C.toast('Profissional removido dos favoritos.', 'success'); await load(); }
                catch (error) { C.toast(error.message, 'error'); button.disabled = false; }
            }));
        } catch (error) {
            target.innerHTML = `<div class="empty-state"><p>${C.escaparHtml(error.message)}</p><button class="btn btn-secondary" type="button" id="retryFavorites">Tentar novamente</button></div>`;
            target.querySelector('#retryFavorites').addEventListener('click', load);
        } finally { target.setAttribute('aria-busy', 'false'); }
    }
    window.ChamaproApp?.ready.then(user => { if (user?.perfil === 'CLIENTE') load(); });
})();
