(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const label = value => String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const message = (id, text, ok = false) => { const el = document.getElementById(id); if (el) { el.textContent = text || ""; el.style.color = ok ? "var(--color-success)" : "var(--color-danger)"; } };

    async function init() {
        const query = new URLSearchParams(location.search);
        let pedidoId = query.get("pedidoId");
        const content = document.getElementById("orderContent");
        if (!pedidoId) {
            try {
                const result = await C.api("/pedidos?pagina=1&tamanho=50");
                const items = result.itens || [];
                if (!items.length) { content.innerHTML = '<section class="app-card empty-state"><h2>Você ainda não tem chamados</h2><p>Crie um chamado para receber propostas.</p><a class="btn btn-primary" href="criar-chamado.html">Criar chamado</a></section>'; return; }
                const picker = document.getElementById("orderPicker");
                const select = document.getElementById("orderSelect"); picker.hidden = false;
                select.innerHTML = items.map(p => `<option value="${p.id}">#${p.id} · ${safe(p.titulo)} · ${safe(label(p.status))}</option>`).join("");
                select.addEventListener("change", () => location.assign(`orcamentos.html?pedidoId=${select.value}`));
                select.value = items[0].id; pedidoId = String(items[0].id);
            } catch (e) { content.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; return; }
        }
        try {
            const p = await C.api(`/pedidos/${pedidoId}`);
            const status = String(p.status || "").toUpperCase();
            let quotes = [];
            if (status !== "CANCELADO") {
                try { quotes = await C.api(`/pedidos/${pedidoId}/orcamentos`); } catch { quotes = []; }
            }
            content.innerHTML = `<article class="app-card"><span class="badge badge-info">${safe(label(p.status))}</span><h2 style="margin:12px 0">${safe(p.titulo)}</h2><p>${safe(p.categoria)} · ${safe([p.cidade,p.bairro,p.endereco].filter(Boolean).join(" · "))}</p><p style="margin-top:12px">${safe(p.descricao)}</p>${p.dataDesejada ? `<p class="app-muted">Data desejada: ${C.formatarData(p.dataDesejada)}</p>` : ""}${p.fotoUrls?.length ? `<div class="app-grid-two" style="margin-top:16px">${p.fotoUrls.map(url => `<img src="${safe(url)}" alt="Foto do chamado" loading="lazy">`).join("")}</div>` : ""}<div class="app-actions" id="orderActions"></div><p id="orderMessage" class="app-message"></p></article><section class="app-stack"><h2>Propostas recebidas (${quotes.length})</h2>${quotes.length ? quotes.map(q => `<article class="app-card"><div class="app-row"><div><strong>${safe(q.profissional.nome)}</strong><div class="app-muted">Nota ${C.formatarNota(q.profissional.notaMedia)} ${q.profissional.verificado ? "· perfil verificado" : ""} · ${safe(q.prazoEstimado)}</div><p class="app-muted">${safe(q.condicoes || "Sem observações adicionais")}</p><span class="badge ${q.situacao === "ACEITO" ? "badge-success" : "badge-info"}">${safe(label(q.situacao))}${q.expirado ? " · expirado" : ""}</span></div><div><strong>${C.formatarMoeda(q.valor)}</strong><div class="app-actions">${status === "PENDENTE" && q.situacao === "ENVIADO" && !q.expirado ? `<button class="btn btn-primary btn-small" data-accept-quote="${q.id}">Aceitar</button><button class="btn btn-secondary btn-small" data-reject-quote="${q.id}">Recusar</button>` : ""}<a class="btn btn-secondary btn-small" href="perfil-profissional.html?id=${q.profissional.id}">Ver perfil</a></div></div></div></article>`).join("") : '<p class="empty-state">Nenhum orçamento foi enviado ainda. O chamado continua aberto para profissionais compatíveis.</p>'}</section>`;
            const actions = document.getElementById("orderActions");
            content.insertAdjacentHTML("beforeend", '<section class="app-card"><h2>Histórico do chamado</h2><div id="orderHistory" class="app-stack"><p class="empty-state">Carregando…</p></div></section>');
            try {
                const history = await C.api(`/pedidos/${pedidoId}/historico`);
                document.getElementById("orderHistory").innerHTML = history.length ? history.map(item => `<div class="app-row"><div><strong>${safe(label(item.statusNovo))}</strong><div class="app-muted">${safe(item.detalhe || "Atualização do chamado")}</div></div><time class="app-muted">${C.formatarData(item.criadoEm)}</time></div>`).join("") : '<p class="empty-state">Ainda não há eventos registrados.</p>';
            } catch (e) { document.getElementById("orderHistory").innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
            if (["PENDENTE", "ACEITO", "AGENDADO"].includes(status)) actions.innerHTML += '<button class="btn btn-secondary" id="cancelOrder">Cancelar chamado</button>';
            if (!["PENDENTE", "ORCADO", "CANCELADO"].includes(status)) {
                try {
                    const contact = await C.api(`/whatsapp/pedidos/${pedidoId}/contato`);
                    const number = String(contact.telefone || "").replace(/\D/g, "");
                    const text = encodeURIComponent(contact.mensagemInicialWhatsApp || `Olá! Estou entrando em contato pelo CHAMAPRO sobre o chamado “${p.titulo}”.`);
                    actions.insertAdjacentHTML("beforeend", `<a class="btn btn-outline" target="_blank" rel="noopener" href="https://wa.me/${number}?text=${text}">Conversar pelo WhatsApp</a>`);
                } catch { /* Contato desativado ou ainda indisponível. */ }
            }
            if (status === "ACEITO" && !p.agendamento) document.getElementById("scheduleBox").hidden = false;
            if (status === "CONCLUIDO" && !p.avaliacao) document.getElementById("reviewBox").hidden = false;
            if (p.agendamento) actions.insertAdjacentHTML("beforeend", `<span class="app-muted">Agendado para ${C.formatarData(p.agendamento.dataHora)} · ${safe(label(p.agendamento.situacao))}</span><button class="btn btn-secondary btn-small" id="reschedule">Remarcar</button>`);

            content.querySelectorAll("[data-accept-quote]").forEach(btn => btn.addEventListener("click", async () => {
                if (!confirm("Aceitar este orçamento? As demais propostas serão encerradas.")) return;
                try { await C.api(`/orcamentos/${btn.dataset.acceptQuote}/aceitar`, { method: "POST" }); location.reload(); }
                catch (e) { message("orderMessage", e.message); }
            }));
            content.querySelectorAll("[data-reject-quote]").forEach(btn => btn.addEventListener("click", async () => {
                try { await C.api(`/orcamentos/${btn.dataset.rejectQuote}/recusar`, { method: "POST" }); location.reload(); }
                catch (e) { message("orderMessage", e.message); }
            }));
            document.getElementById("cancelOrder")?.addEventListener("click", async () => {
                const motivo = prompt("Informe o motivo do cancelamento:"); if (!motivo?.trim()) return;
                try { await C.api(`/pedidos/${pedidoId}/cancelar`, { method: "POST", body: { motivo } }); location.reload(); }
                catch (e) { message("orderMessage", e.message); }
            });
            document.getElementById("reschedule")?.addEventListener("click", () => { document.getElementById("scheduleBox").hidden = false; document.getElementById("scheduleForm").dataset.agendamentoId = p.agendamento.id; });
            document.getElementById("scheduleForm").addEventListener("submit", async event => {
                event.preventDefault();
                const date = new Date(document.getElementById("scheduleDate").value).toISOString();
                const appointmentId = event.currentTarget.dataset.agendamentoId;
                try { if (appointmentId) await C.api(`/agendamentos/${appointmentId}`, { method: "PUT", body: { dataHora: date } }); else await C.api("/agendamentos", { method: "POST", body: { pedidoId: Number(pedidoId), dataHora: date } }); location.reload(); }
                catch (e) { message("scheduleMessage", e.message); }
            });
            document.getElementById("reviewForm").addEventListener("submit", async event => {
                event.preventDefault();
                try { await C.api("/avaliacoes", { method: "POST", body: { pedidoId: Number(pedidoId), nota: Number(document.getElementById("reviewRating").value), comentario: document.getElementById("reviewComment").value.trim() || null } }); message("reviewMessage", "Obrigado pela avaliação.", true); setTimeout(() => location.reload(), 800); }
                catch (e) { message("reviewMessage", e.message); }
            });
        } catch (e) { content.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
    }

    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "CLIENTE") init(); });
})();
