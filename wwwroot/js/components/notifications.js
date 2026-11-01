(() => {
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
                    await window.Chamapro.api("/notificacoes/lidas", { method: "PATCH" });
                    await render();
                });
                panel.querySelectorAll("[data-notification-id]").forEach(item => item.addEventListener("click", async () => {
                    await window.Chamapro.api(`/notificacoes/${item.dataset.notificationId}/lida`, { method: "PATCH" });
                    item.classList.remove("unread");
                }));
            } catch (erro) {
                panel.innerHTML = `<p class="empty-state">${window.Chamapro.escaparHtml(erro.message)}</p>`;
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
    }
    window.ChamaproNotifications = { init };
})();
