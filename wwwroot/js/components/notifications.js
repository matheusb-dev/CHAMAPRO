(() => {
    function destination(item, role) {
        if (!['CLIENTE','PROFISSIONAL'].includes(role)) return null;
        try {
            const data = typeof item.dados === 'string' ? JSON.parse(item.dados) : item.dados;
            const id = data?.pedidoId;
            if (Number.isSafeInteger(id) && id > 0) return role === 'CLIENTE' ? `/pages/cliente/orcamentos.html?pedidoId=${id}` : `/pages/trabalhador/detalhes-chamado.html?id=${id}`;
            return role === 'PROFISSIONAL' && Number.isSafeInteger(data?.verificacaoId) && data.verificacaoId > 0 ? '/pages/trabalhador/verificacao.html' : null;
        } catch { return null; }
    }
    async function init() {
        const button = document.getElementById("notificationsButton");
        const panel = document.getElementById("notificationPanel");
        if (!button || !panel) return;
        const render = async () => {
            panel.hidden = false;
            panel.innerHTML = '<div class="notification-heading"><strong>Notificações</strong><button type="button" id="markAllNotifications">Marcar todas como lidas</button></div><p class="empty-state">Carregando…</p>';
            try {
                const items = await window.Chamapro.api("/notificacoes");
                panel.innerHTML = `<div class="notification-heading"><strong>Notificações</strong><button type="button" id="markAllNotifications">Marcar todas como lidas</button></div>` +
                    (items.length ? items.map(item => `<button class="notification-item ${item.lidaEm ? "" : "unread"}" type="button" data-notification-id="${item.id}"><strong>${window.Chamapro.escaparHtml(item.titulo)}</strong><span>${window.Chamapro.escaparHtml(item.mensagem)}</span><time>${window.Chamapro.formatarData(item.criadaEm)}</time></button>`).join("") : '<p class="empty-state">Você não tem notificações.</p>');
                panel.querySelector("#markAllNotifications")?.addEventListener("click", async () => {
                    try { await window.Chamapro.api("/notificacoes/lidas", { method: "PATCH" }); await render(); }
                    catch (e) { window.Chamapro.toast(e.message, 'error'); }
                });
                panel.querySelectorAll("[data-notification-id]").forEach(item => item.addEventListener("click", async () => {
                    if (item.disabled) return;
                    item.disabled = true;
                    try {
                        await window.Chamapro.api(`/notificacoes/${item.dataset.notificationId}/lida`, { method: "PATCH" });
                        item.classList.remove("unread");
                        const notification = items.find(n => String(n.id) === item.dataset.notificationId);
                        const url = destination(notification, window.Chamapro.obterUsuario()?.perfil);
                        if (url) window.location.assign(url);
                    } catch (e) { window.Chamapro.toast(e.message, 'error'); }
                    finally { item.disabled = false; }
                }));
            } catch (erro) {
                panel.innerHTML = `<p class="empty-state">${window.Chamapro.escaparHtml(erro.message)}</p><button id="retryNotifications" type="button">Tentar novamente</button>`;
                panel.querySelector('#retryNotifications').addEventListener('click', render);
            }
        };
        button.addEventListener("click", async () => {
            const abrir = panel.hidden;
            button.setAttribute("aria-expanded", String(abrir));
            if (abrir) await render(); else panel.hidden = true;
        });
        document.addEventListener("click", event => {
            if (!panel.contains(event.target) && !button.contains(event.target)) {
                panel.hidden = true;
                button.setAttribute("aria-expanded", "false");
            }
        });
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && !panel.hidden) {
                panel.hidden = true;
                button.setAttribute('aria-expanded', 'false');
                button.focus();
            }
        });
    }
    window.ChamaproNotifications = { init, destination };
})();
