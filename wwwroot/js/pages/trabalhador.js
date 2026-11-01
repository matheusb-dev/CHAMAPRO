(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const statusText = value => String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const message = (id, text, ok = false) => { const el = document.getElementById(id); if (el) { el.textContent = text || ""; el.style.color = ok ? "var(--color-success)" : "var(--color-danger)"; } };

    async function carregarFila() {
        const target = document.getElementById("professionalOrders");
        const status = document.getElementById("statusFilter").value;
        target.innerHTML = '<p class="empty-state">Carregando…</p>';
        try {
            const query = new URLSearchParams({ pagina: "1", tamanho: "50" });
            if (status) query.set("status", status);
            const result = await C.api(`/pedidos?${query}`);
            const itens = result.itens || [];
            target.innerHTML = itens.length ? itens.map(p => `<article class="app-card"><div class="app-row"><div><span class="badge badge-info">${safe(statusText(p.status))}</span><h2 style="margin-top:8px">${safe(p.titulo)}</h2><p>${safe(p.categoria)} · ${safe([p.cidade,p.bairro].filter(Boolean).join(" · "))}</p><span class="app-muted">Recebido em ${C.formatarData(p.criadoEm)} · Sua resposta: ${safe(statusText(p.minhaSituacao))}</span></div><a class="btn btn-primary" href="detalhes-chamado.html?id=${p.id}">Abrir chamado</a></div></article>`).join("") : '<section class="app-card empty-state"><h2>Nenhum chamado nesta fila</h2><p>Novos chamados compatíveis aparecerão aqui quando forem direcionados a você.</p></section>';
        } catch (e) { target.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
    }

    async function carregarDetalhe() {
        const id = new URLSearchParams(location.search).get("id");
        const target = document.getElementById("orderDetail");
        if (!id) { target.innerHTML = '<p class="empty-state">Chamado não informado.</p>'; return; }
        try {
            const p = await C.api(`/pedidos/${id}`);
            const situacao = String(p.destinatarios?.[0]?.situacao || "").toUpperCase();
            const status = String(p.status || "").toUpperCase();
            const contratado = p.profissionalId != null && ["ACEITO", "AGENDADO", "EM_ANDAMENTO", "CONCLUIDO"].includes(status);
            target.innerHTML = `<article class="app-card"><span class="badge badge-info">${safe(statusText(p.status))}</span><h1 style="margin:12px 0">${safe(p.titulo)}</h1><p>${safe(p.categoria)} · ${safe(p.tipoAtendimento || "Atendimento")}</p><div class="divider"></div><p>${safe(p.descricao)}</p><p class="app-muted" style="margin-top:12px">${safe([p.cidade,p.bairro,p.endereco].filter(Boolean).join(" · "))}</p>${p.dataDesejada ? `<p class="app-muted">Data desejada: ${C.formatarData(p.dataDesejada)}</p>` : ""}<p class="app-muted">Cliente: ${safe(p.nomeCliente || "")}</p>${p.fotoUrls?.length ? `<div class="app-grid-two">${p.fotoUrls.map(url => `<img src="${safe(url)}" alt="Foto enviada com o chamado" loading="lazy">`).join("")}</div>` : ""}<div id="detailMessage" class="app-message"></div><div class="app-actions" id="detailActions"></div><div class="app-muted" id="appointmentInfo"></div></article><section class="app-card" style="margin-top:18px"><h2>Histórico do chamado</h2><div id="orderHistory" class="app-stack"><p class="empty-state">Carregando…</p></div></section>`;
            try {
                const historico = await C.api(`/pedidos/${id}/historico`);
                const historyTarget = document.getElementById("orderHistory");
                historyTarget.innerHTML = historico.length ? historico.map(item => `<div class="app-row"><div><strong>${safe(statusText(item.statusNovo))}</strong><div class="app-muted">${safe(item.detalhe || "Atualização do chamado")}</div></div><time class="app-muted">${C.formatarData(item.criadoEm)}</time></div>`).join("") : '<p class="empty-state">Ainda não há eventos registrados.</p>';
            } catch (e) { document.getElementById("orderHistory").innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
            const actions = document.getElementById("detailActions");
            if (status === "PENDENTE" && situacao === "PENDENTE") {
                actions.innerHTML = '<button class="btn btn-primary" id="acceptOrder">Tenho interesse</button><button class="btn btn-secondary" id="rejectOrder">Recusar</button>';
                document.getElementById("acceptOrder").onclick = () => alterarPedido(`/pedidos/${id}/aceitar`, "POST", null, "Chamado aceito. Você já pode enviar uma proposta.");
                document.getElementById("rejectOrder").onclick = () => alterarPedido(`/pedidos/${id}/recusar`, "POST", {}, "Chamado recusado.");
            }
            if (situacao === "ACEITO" && status === "PENDENTE") document.getElementById("quoteSection").hidden = false;
            if (contratado && ["ACEITO", "AGENDADO"].includes(status) && !p.agendamento) document.getElementById("scheduleSection").hidden = false;
            if (p.agendamento) document.getElementById("appointmentInfo").textContent = `Agendamento: ${C.formatarData(p.agendamento.dataHora)} · ${statusText(p.agendamento.situacao)}`;
            if (status === "AGENDADO") actions.innerHTML += '<button class="btn btn-primary" id="startService">Iniciar serviço</button>';
            if (status === "EM_ANDAMENTO") actions.innerHTML += '<button class="btn btn-primary" id="completeService">Marcar como concluído</button>';
            if (status === "AGENDADO" || status === "EM_ANDAMENTO") {
                try {
                    const contact = await C.api(`/whatsapp/pedidos/${id}/contato`);
                    const number = String(contact.telefone || "").replace(/\D/g, "");
                    const text = encodeURIComponent(contact.mensagemInicialWhatsApp || `Olá! Estou entrando em contato pelo CHAMAPRO sobre o chamado “${p.titulo}”.`);
                    actions.insertAdjacentHTML("beforeend", `<a class="btn btn-outline" target="_blank" rel="noopener" href="https://wa.me/${number}?text=${text}">Conversar pelo WhatsApp</a>`);
                } catch { /* Contato desativado ou ainda indisponível. */ }
            }
            document.getElementById("startService")?.addEventListener("click", () => atualizarStatus(id, "EM_ANDAMENTO"));
            document.getElementById("completeService")?.addEventListener("click", () => atualizarStatus(id, "CONCLUIDO"));
            document.getElementById("quoteForm")?.addEventListener("submit", async event => {
                event.preventDefault();
                const body = { valor: Number(document.getElementById("quoteValue").value), prazoEstimado: document.getElementById("quoteDeadline").value.trim(), condicoes: document.getElementById("quoteConditions").value.trim() || null };
                try { await C.api(`/pedidos/${id}/orcamentos`, { method: "POST", body }); C.toast("Orçamento enviado.", "success"); location.reload(); }
                catch (e) { message("quoteMessage", e.message); }
            });
            document.getElementById("scheduleForm")?.addEventListener("submit", async event => {
                event.preventDefault();
                try { await C.api("/agendamentos", { method: "POST", body: { pedidoId: Number(id), dataHora: new Date(document.getElementById("scheduleDate").value).toISOString() } }); C.toast("Agendamento criado.", "success"); location.reload(); }
                catch (e) { message("scheduleMessage", e.message); }
            });
        } catch (e) { target.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }

        async function alterarPedido(path, method, body, ok) {
            try { await C.api(path, { method, body }); C.toast(ok, "success"); location.reload(); }
            catch (e) { message("detailMessage", e.message); }
        }
        async function atualizarStatus(orderId, novoStatus) {
            try { await C.api(`/pedidos/${orderId}/status`, { method: "PATCH", body: { status: novoStatus } }); C.toast("Status atualizado.", "success"); location.reload(); }
            catch (e) { message("detailMessage", e.message); }
        }
    }

    async function enviarImagem(file) {
        if (file.size > 5 * 1024 * 1024) throw new Error("Cada imagem deve ter até 5 MB.");
        const data = new FormData(); data.append("arquivo", file); data.append("privado", "false");
        const result = await C.api("/arquivos", { method: "POST", body: data });
        return result.url;
    }

    async function carregarPerfil() {
        let photoUrl = null;
        let servicosAtuais = [];
        try {
            const p = await C.api("/profissionais/me");
            const contato = await C.api("/whatsapp/contato/me");
            photoUrl = p.fotoUrl || null;
            servicosAtuais = p.servicos || [];
            document.getElementById("name").value = p.nome || "";
            document.getElementById("phone").value = p.telefone || "";
            document.getElementById("whatsappActive").checked = Boolean(contato.whatsAppAtivo);
            document.getElementById("whatsappMessage").value = contato.mensagemInicialWhatsApp || "";
            document.getElementById("description").value = p.descricao || "";
            document.getElementById("portfolio").value = (p.fotosTrabalhos || []).join("\n");
            document.getElementById("experience").value = p.experienciaAnos ?? "";
            document.getElementById("distance").value = p.distanciaMaxKm ?? "";
            document.getElementById("urgent").checked = Boolean(p.atendeUrgencia);
            document.getElementById("homeService").checked = p.atendeDomicilio !== false;
            document.getElementById("shopService").checked = Boolean(p.atendeEstabelecimento);
            const preview = document.getElementById("photoPreview");
            if (photoUrl) { preview.src = photoUrl; preview.hidden = false; }
            const categories = await C.api("/categorias");
            const select = document.getElementById("categoryIds");
            select.innerHTML = categories.map(c => `<option value="${c.id}">${safe(c.nome)}</option>`).join("");
            const selected = new Map(servicosAtuais.map(s => [Number(s.categoriaId), s]));
            for (const option of select.options) if (selected.has(Number(option.value))) option.selected = true;
            document.getElementById("categoryPrices").innerHTML = categories.map(c => {
                const current = selected.get(Number(c.id));
                return `<div class="app-row"><strong>${safe(c.nome)}</strong><div class="app-form-grid"><input aria-label="Preço mínimo de ${safe(c.nome)}" data-price-min="${c.id}" type="number" min="0" step="0.01" placeholder="Mínimo" value="${current?.faixaPrecoMin ?? ""}"><input aria-label="Preço máximo de ${safe(c.nome)}" data-price-max="${c.id}" type="number" min="0" step="0.01" placeholder="Máximo" value="${current?.faixaPrecoMax ?? ""}"></div></div>`;
            }).join("");
            document.getElementById("areas").value = (p.areas || []).map(a => `${a.cidade} | ${a.bairro || ""}`).join("\n");
            const dias = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];
            const disponibilidade = p.disponibilidade || [];
            document.getElementById("availabilityGrid").innerHTML = dias.map((dia, index) => {
                const atual = disponibilidade.find(x => Number(x.diaSemana) === index);
                return `<div class="availability-row"><label><input type="checkbox" data-day-enabled="${index}" ${atual ? "checked" : ""}> ${dia}</label><input type="time" aria-label="Início ${dia}" data-day-start="${index}" value="${atual?.horaInicio?.slice(0,5) || "09:00"}" ${atual ? "" : "disabled"}><input type="time" aria-label="Fim ${dia}" data-day-end="${index}" value="${atual?.horaFim?.slice(0,5) || "17:00"}" ${atual ? "" : "disabled"}><label><input type="checkbox" data-day-immediate="${index}" ${atual?.atendeImediato ? "checked" : ""} ${atual ? "" : "disabled"}> Imediato</label><label><input type="checkbox" data-day-scheduled="${index}" ${atual?.atendeAgendado !== false ? "checked" : ""} ${atual ? "" : "disabled"}> Agendado</label></div>`;
            }).join("");
            document.querySelectorAll("[data-day-enabled]").forEach(toggle => toggle.addEventListener("change", () => {
                const day = toggle.dataset.dayEnabled;
                for (const selector of [`[data-day-start="${day}"]`, `[data-day-end="${day}"]`, `[data-day-immediate="${day}"]`, `[data-day-scheduled="${day}"]`]) document.querySelector(selector).disabled = !toggle.checked;
            }));
        } catch (e) { message("profileMessage", e.message); }
        document.getElementById("professionalProfileForm").addEventListener("submit", async event => {
            event.preventDefault();
            try {
                const photoFile = document.getElementById("photoFile").files[0];
                if (photoFile) photoUrl = await enviarImagem(photoFile);
                const portfolio = document.getElementById("portfolio").value.split(/\r?\n/).map(x => x.trim()).filter(Boolean);
                const portfolioFiles = [...document.getElementById("portfolioFiles").files];
                if (portfolioFiles.length + portfolio.length > 10) throw new Error("O portfólio aceita até dez imagens.");
                for (const file of portfolioFiles) portfolio.push(await enviarImagem(file));
                const body = { nome: document.getElementById("name").value.trim(), telefone: document.getElementById("phone").value.replace(/\D/g,""), descricao: document.getElementById("description").value.trim(), fotoUrl: photoUrl, fotosTrabalhos: portfolio, experienciaAnos: document.getElementById("experience").value ? Number(document.getElementById("experience").value) : null, distanciaMaxKm: document.getElementById("distance").value ? Number(document.getElementById("distance").value) : null, atendeUrgencia: document.getElementById("urgent").checked, atendeDomicilio: document.getElementById("homeService").checked, atendeEstabelecimento: document.getElementById("shopService").checked };
                await C.api("/profissionais/me", { method: "PUT", body }); message("profileMessage", "Perfil atualizado.", true);
                await C.api("/whatsapp/contato/me", { method: "PUT", body: {
                    telefone: body.telefone,
                    whatsAppAtivo: document.getElementById("whatsappActive").checked,
                    mensagemInicialWhatsApp: document.getElementById("whatsappMessage").value.trim() || null
                }});
                message("profileMessage", "Perfil e preferência de WhatsApp atualizados.", true);
            }
            catch (e) { message("profileMessage", e.message); }
        });
        document.getElementById("serviceAreaForm").addEventListener("submit", async event => {
            event.preventDefault();
            const servicos = [...document.getElementById("categoryIds").selectedOptions].map(option => { const id = option.value; const min = document.querySelector(`[data-price-min="${id}"]`).value; const max = document.querySelector(`[data-price-max="${id}"]`).value; return { categoriaId: Number(id), faixaPrecoMin: min ? Number(min) : 0, faixaPrecoMax: max ? Number(max) : 0 }; });
            const areas = document.getElementById("areas").value.split(/\r?\n/).map(line => line.trim()).filter(Boolean).map(line => { const [cidade,bairro] = line.split("|"); return { cidade: cidade.trim(), bairro: bairro?.trim() || null }; });
            const disponibilidade = [...document.querySelectorAll("[data-day-enabled]:checked")].map(toggle => { const day = toggle.dataset.dayEnabled; return { diaSemana: Number(day), horaInicio: document.querySelector(`[data-day-start="${day}"]`).value, horaFim: document.querySelector(`[data-day-end="${day}"]`).value, atendeImediato: document.querySelector(`[data-day-immediate="${day}"]`).checked, atendeAgendado: document.querySelector(`[data-day-scheduled="${day}"]`).checked }; });
            try {
                if (!servicos.length || !areas.length) throw new Error("Escolha ao menos uma categoria e informe uma área.");
                if (disponibilidade.some(x => !x.horaInicio || !x.horaFim || x.horaFim <= x.horaInicio || (!x.atendeImediato && !x.atendeAgendado))) throw new Error("Confira os horários e escolha imediato ou agendado para cada dia ativo.");
                await C.api("/profissionais/me/servicos", { method: "PUT", body: servicos });
                await C.api("/profissionais/me/areas", { method: "PUT", body: areas });
                await C.api("/profissionais/me/disponibilidade", { method: "PUT", body: disponibilidade });
                message("serviceMessage", "Serviços, áreas e disponibilidade atualizados.", true);
            } catch (e) { message("serviceMessage", e.message); }
        });
    }

    window.ChamaproApp?.ready.then(user => {
        if (!user || user.perfil !== "PROFISSIONAL") return;
        if (document.getElementById("professionalOrders")) { document.getElementById("statusFilter").addEventListener("change", carregarFila); document.getElementById("refreshOrders").addEventListener("click", carregarFila); carregarFila(); }
        if (document.getElementById("orderDetail")) carregarDetalhe();
        if (document.getElementById("professionalProfileForm")) carregarPerfil();
    });
})();
