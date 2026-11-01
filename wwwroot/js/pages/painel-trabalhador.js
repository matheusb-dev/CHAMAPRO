(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const statusText = value => String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const set = (id, value) => { const el = document.getElementById(id); if (el) el.textContent = value; };
    const toLocalInput = value => {
        const date = new Date(value);
        return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
    };

    async function carregarPainel() {
        const list = document.getElementById("appointmentsList");
        try {
            const [painel, agendamentos] = await Promise.all([
                C.api("/painel/profissional"), C.api("/agendamentos/me")
            ]);
            set("newOrders", painel.novosChamados);
            set("acceptedOrders", painel.chamadosAceitos);
            set("sentQuotes", painel.orcamentosEnviados);
            set("futureAppointments", painel.agendamentosFuturos);
            set("servicesInProgress", painel.servicosEmAndamento);
            set("servicesCompleted", painel.servicosConcluidos);
            set("averageRating", C.formatarNota(painel.mediaAvaliacao));
            set("verificationStatus", statusText(painel.statusVerificacao));

            if (!agendamentos.length) {
                list.innerHTML = '<p class="empty-state">Você ainda não tem agendamentos ativos.</p>';
                return;
            }
            list.innerHTML = agendamentos.map(a => {
                const status = String(a.statusAgendamento).toUpperCase();
                return `<article class="app-row"><div><strong>${safe(a.titulo)}</strong><div class="app-muted">Cliente: ${safe(a.clienteNome)} · ${new Date(a.dataHora).toLocaleString("pt-BR")} · ${safe(statusText(a.statusAgendamento))}</div><a href="detalhes-chamado.html?id=${a.pedidoId}">Abrir chamado</a></div><div class="app-stack"><form data-reschedule="${a.id}" class="app-actions"><label class="app-muted">Remarcar <input type="datetime-local" required value="${toLocalInput(a.dataHora)}"></label><button class="btn btn-secondary btn-small" type="submit">Salvar horário</button></form><div class="app-actions">${status === "AGENDADO" ? `<button class="btn btn-primary btn-small" data-confirm="${a.id}">Confirmar</button>` : ""}<button class="btn btn-danger btn-small" data-cancel="${a.id}">Cancelar</button></div></div></article>`;
            }).join("");

            list.querySelectorAll("[data-reschedule]").forEach(form => form.addEventListener("submit", async event => {
                event.preventDefault();
                const dataHora = new Date(form.querySelector("input").value);
                if (!Number.isFinite(dataHora.getTime())) return;
                try {
                    await C.api(`/agendamentos/${form.dataset.reschedule}`, { method: "PUT", body: { dataHora: dataHora.toISOString() } });
                    C.toast("Agendamento remarcado.", "success");
                    await carregarPainel();
                } catch (e) { C.toast(e.message, "error"); }
            }));
            list.querySelectorAll("[data-confirm]").forEach(button => button.addEventListener("click", async () => {
                try {
                    await C.api(`/agendamentos/${button.dataset.confirm}/status`, { method: "PATCH", body: { status: "Confirmado" } });
                    C.toast("Agendamento confirmado.", "success");
                    await carregarPainel();
                } catch (e) { C.toast(e.message, "error"); }
            }));
            list.querySelectorAll("[data-cancel]").forEach(button => button.addEventListener("click", async () => {
                if (!window.confirm("Deseja cancelar este agendamento?")) return;
                try {
                    await C.api(`/agendamentos/${button.dataset.cancel}/status`, { method: "PATCH", body: { status: "Cancelado" } });
                    C.toast("Agendamento cancelado.", "success");
                    await carregarPainel();
                } catch (e) { C.toast(e.message, "error"); }
            }));
        } catch (e) {
            list.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`;
        }
    }

    window.ChamaproApp?.ready.then(user => {
        if (user?.perfil === "PROFISSIONAL" && document.getElementById("appointmentsList")) carregarPainel();
    });
})();
