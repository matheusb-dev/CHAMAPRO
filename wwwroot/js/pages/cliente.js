(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const statusText = value => String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const toLocalInput = value => { const date = new Date(value); return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16); };
    const setMessage = (id, text, success = false) => {
        const el = document.getElementById(id);
        if (el) { el.textContent = text || ""; el.style.color = success ? "var(--color-success)" : "var(--color-danger)"; }
    };

    function prepararAgendamentos() {
        const orders = document.getElementById("ordersList")?.closest("section");
        if (!orders) return null;
        const section = document.createElement("section");
        section.className = "app-card";
        section.style.marginTop = "18px";
        const title = document.createElement("h2");
        title.textContent = "Próximos agendamentos";
        const list = document.createElement("div");
        list.id = "appointmentsList";
        list.className = "app-stack";
        list.innerHTML = '<p class="empty-state">Carregando agenda…</p>';
        section.append(title, list);
        orders.after(section);
        return list;
    }

    const appointmentsList = prepararAgendamentos();

    async function carregarPainel() {
        const [ordersResult, reviewsResult, favoritesResult, appointmentsResult] = await Promise.allSettled([
            C.api("/pedidos?pagina=1&tamanho=50"), C.api("/avaliacoes/pendentes"), C.api("/favoritos"), C.api("/agendamentos/me")
        ]);
        const orders = ordersResult.status === "fulfilled" ? (ordersResult.value.itens || []) : [];
        const reviews = reviewsResult.status === "fulfilled" ? reviewsResult.value : [];
        const favorites = favoritesResult.status === "fulfilled" ? favoritesResult.value : [];
        const active = orders.filter(o => !["CONCLUIDO", "CANCELADO"].includes(String(o.status).toUpperCase()));
        document.getElementById("countActive") && (document.getElementById("countActive").textContent = active.length);
        document.getElementById("countReviews") && (document.getElementById("countReviews").textContent = reviews.length);
        document.getElementById("countFavorites") && (document.getElementById("countFavorites").textContent = favorites.length);
        const list = document.getElementById("ordersList");
        if (list) {
            if (ordersResult.status === "rejected") list.innerHTML = `<p class="empty-state">${safe(ordersResult.reason.message)}</p>`;
            else if (!orders.length) list.innerHTML = '<p class="empty-state">Você ainda não criou chamados. Quando precisar de um serviço, comece por “Criar chamado”.</p>';
            else list.innerHTML = orders.map(o => `<article class="app-row"><div><strong>${safe(o.titulo)}</strong><div class="app-muted">${safe(o.categoria)} · ${safe([o.cidade,o.bairro].filter(Boolean).join(" · "))} · ${C.formatarData(o.criadoEm)}</div></div><div class="app-actions"><span class="badge badge-info">${safe(statusText(o.status))}</span><a class="btn btn-secondary btn-small" href="orcamentos.html?pedidoId=${o.id}">Abrir</a></div></article>`).join("");
        }
        if (appointmentsList) {
            if (appointmentsResult.status === "rejected") appointmentsList.innerHTML = `<p class="empty-state">${safe(appointmentsResult.reason.message)}</p>`;
            else if (!appointmentsResult.value.length) appointmentsList.innerHTML = '<p class="empty-state">Você ainda não tem agendamentos ativos.</p>';
            else appointmentsList.innerHTML = appointmentsResult.value.map(a => `<article class="app-row"><div><strong>${safe(a.titulo)}</strong><div class="app-muted">Profissional: ${safe(a.profissionalNome)} · ${new Date(a.dataHora).toLocaleString("pt-BR")} · ${safe(statusText(a.statusAgendamento))}</div><a href="orcamentos.html?pedidoId=${a.pedidoId}">Abrir chamado</a></div><div class="app-stack"><form data-reschedule="${a.id}" class="app-actions"><label class="app-muted">Remarcar <input type="datetime-local" required value="${toLocalInput(a.dataHora)}"></label><button class="btn btn-secondary btn-small" type="submit">Salvar horário</button></form><button class="btn btn-danger btn-small" data-cancel-appointment="${a.id}">Cancelar agendamento</button></div></article>`).join("");
            appointmentsList.querySelectorAll("[data-reschedule]").forEach(form => form.addEventListener("submit", async event => {
                event.preventDefault();
                const date = new Date(form.querySelector("input").value);
                if (!Number.isFinite(date.getTime())) return;
                try { await C.api(`/agendamentos/${form.dataset.reschedule}`, { method: "PUT", body: { dataHora: date.toISOString() } }); C.toast("Agendamento remarcado.", "success"); await carregarPainel(); }
                catch (e) { C.toast(e.message, "error"); }
            }));
            appointmentsList.querySelectorAll("[data-cancel-appointment]").forEach(button => button.addEventListener("click", async () => {
                if (!window.confirm("Deseja cancelar este agendamento?")) return;
                try { await C.api(`/agendamentos/${button.dataset.cancelAppointment}/status`, { method: "PATCH", body: { status: "Cancelado" } }); C.toast("Agendamento cancelado.", "success"); await carregarPainel(); }
                catch (e) { C.toast(e.message, "error"); }
            }));
        }
        const fav = document.getElementById("favoritesList");
        if (fav) fav.innerHTML = favorites.length ? favorites.map(f => `<article class="app-row"><div><strong>${safe(f.nome)}</strong><div class="app-muted">Nota ${C.formatarNota(f.notaMedia)} · profissional</div></div><div class="app-actions"><a class="btn btn-secondary btn-small" href="perfil-profissional.html?id=${f.profissionalId}">Perfil</a><button class="btn btn-secondary btn-small" data-unfavorite="${f.profissionalId}">Remover</button></div></article>`).join("") : '<p class="empty-state">Seus profissionais favoritos aparecerão aqui.</p>';
        fav?.querySelectorAll("[data-unfavorite]").forEach(btn => btn.addEventListener("click", async () => {
            await C.api(`/favoritos/${btn.dataset.unfavorite}`, { method: "DELETE" });
            await carregarPainel();
        }));
        const reviewList = document.getElementById("pendingReviews");
        if (reviewList) reviewList.innerHTML = reviews.length ? reviews.map(r => `<article class="app-row"><div><strong>${safe(r.titulo)}</strong><div class="app-muted">Atendimento de ${safe(r.profissionalNome)}</div></div><a class="btn btn-primary btn-small" href="orcamentos.html?pedidoId=${r.pedidoId}#avaliar">Avaliar</a></article>`).join("") : '<p class="empty-state">Nenhuma avaliação pendente.</p>';
    }

    async function iniciarCriacao() {
        const form = document.getElementById("createOrderForm");
        const categorySelect = document.getElementById("category");
        const serviceType = document.getElementById("serviceType");
        const dateGroup = document.getElementById("desiredDateGroup");
        const picker = document.getElementById("professionalPicker");
        const reviewPanel = document.getElementById("reviewPanel");
        let draft = null;
        try {
            const categories = await C.api("/categorias");
            categorySelect.innerHTML = '<option value="">Selecione uma categoria</option>' + categories.map(c => `<option value="${c.id}">${safe(c.nome)}</option>`).join("");
        } catch (e) { categorySelect.innerHTML = '<option value="">Não foi possível carregar categorias</option>'; setMessage("formMessage", e.message); }

        document.querySelectorAll('input[name="distribution"]').forEach(r => r.addEventListener("change", () => {
            const manual = document.querySelector('input[name="distribution"]:checked')?.value === "manual";
            picker.hidden = !manual;
        }));
        serviceType.addEventListener("change", () => {
            const scheduled = serviceType.value === "AGENDADO";
            dateGroup.hidden = !scheduled;
            document.getElementById("desiredDate").required = scheduled;
        });
        document.getElementById("searchProfessionals").addEventListener("click", async () => {
            const categoryId = categorySelect.value;
            const city = document.getElementById("city").value.trim();
            const bairro = document.getElementById("neighborhood").value.trim();
            if (!categoryId || !city) { setMessage("formMessage", "Informe categoria e cidade antes de buscar profissionais."); return; }
            if (serviceType.value === "AGENDADO" && !document.getElementById("desiredDate").value) { setMessage("formMessage", "Informe a data e o horário desejados."); return; }
            const target = document.getElementById("professionalList");
            target.innerHTML = '<p class="empty-state">Buscando profissionais…</p>';
            try {
                const query = new URLSearchParams({ categoriaId: categoryId, cidade: city, pagina: "1", tamanho: "50", tipoAtendimento: serviceType.value });
                if (bairro) query.set("bairro", bairro);
                if (serviceType.value === "AGENDADO") query.set("dataDesejada", new Date(document.getElementById("desiredDate").value).toISOString());
                const result = await C.api(`/profissionais?${query}`);
                const list = result.itens || [];
                target.innerHTML = list.length ? list.map(p => `<label class="app-row"><span><input type="checkbox" name="professionalId" value="${p.id}"> <strong>${safe(p.nome)}</strong><small class="app-muted"> · nota ${C.formatarNota(p.notaMedia)} ${p.verificado ? "· verificado" : ""}</small></span><a href="perfil-profissional.html?id=${p.id}" target="_blank" rel="noopener">Ver perfil</a></label>`).join("") : '<p class="empty-state">Não encontramos profissionais verificados que atendam esta região.</p>';
            } catch (e) { target.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
        });
        form.addEventListener("submit", event => {
            event.preventDefault();
            if (!form.reportValidity()) return;
            const distribution = document.querySelector('input[name="distribution"]:checked')?.value;
            const ids = [...document.querySelectorAll('input[name="professionalId"]:checked')].map(x => Number(x.value));
            if (distribution === "manual" && (!ids.length || ids.length > 5)) { setMessage("formMessage", "Selecione de um a cinco profissionais compatíveis."); return; }
            const type = serviceType.value;
            const dateValue = document.getElementById("desiredDate").value;
            draft = {
                categoriaId: Number(categorySelect.value), titulo: document.getElementById("title").value.trim(),
                descricao: document.getElementById("description").value.trim(), cidade: document.getElementById("city").value.trim(),
                bairro: document.getElementById("neighborhood").value.trim() || null,
                endereco: document.getElementById("address").value.trim() || null,
                tipoAtendimento: type, dataDesejada: type === "AGENDADO" ? new Date(dateValue).toISOString() : null,
                distribuirParaCompativeis: distribution === "auto", profissionalIds: ids, fotoUrls: []
            };
            const summary = [`<p><strong>Categoria:</strong> ${safe(categorySelect.selectedOptions[0].text)}</p>`,`<p><strong>Serviço:</strong> ${safe(draft.titulo)}</p>`,`<p><strong>Descrição:</strong> ${safe(draft.descricao)}</p>`,`<p><strong>Região:</strong> ${safe([draft.cidade,draft.bairro,draft.endereco].filter(Boolean).join(" · "))}</p>`,`<p><strong>Atendimento:</strong> ${safe(type === "AGENDADO" ? new Date(dateValue).toLocaleString("pt-BR") : "Imediato")}</p>`,`<p><strong>Direcionamento:</strong> ${distribution === "auto" ? "até cinco profissionais compatíveis" : `${ids.length} profissionais selecionados`}</p>`].join("");
            document.getElementById("reviewSummary").innerHTML = summary;
            reviewPanel.hidden = false;
            reviewPanel.scrollIntoView({ behavior: "smooth", block: "center" });
            setMessage("formMessage", "Confira os dados e confirme a criação do chamado.", true);
        });
        document.getElementById("backToEdit").addEventListener("click", () => { reviewPanel.hidden = true; form.scrollIntoView({ behavior: "smooth" }); });
        document.getElementById("confirmOrder").addEventListener("click", async () => {
            if (!draft) return;
            const button = document.getElementById("confirmOrder"); button.disabled = true; button.textContent = "Enviando…";
            try {
                const files = [...(document.getElementById("photos")?.files || [])];
                if (files.length > 5) throw new Error("Envie no máximo cinco fotos.");
                draft.fotoUrls = [];
                for (const file of files) {
                    if (file.size > 5 * 1024 * 1024) throw new Error("Cada imagem deve ter até 5 MB.");
                    const formData = new FormData();
                    formData.append("arquivo", file);
                    formData.append("privado", "false");
                    const uploaded = await C.api("/arquivos", { method: "POST", body: formData });
                    draft.fotoUrls.push(uploaded.url);
                }
                const created = await C.api("/pedidos", { method: "POST", body: draft });
                C.toast("Chamado criado e direcionado aos profissionais compatíveis.", "success");
                location.assign(`painel.html?pedidoId=${created.id}`);
            } catch (e) { setMessage("reviewMessage", e.message); button.disabled = false; button.textContent = "Confirmar chamado"; }
        });
    }

    async function carregarPerfil() {
        try {
            const p = await C.api("/clientes/me");
            const contato = await C.api("/whatsapp/contato/me");
            document.getElementById("name").value = p.nome || "";
            document.getElementById("phone").value = p.telefone || "";
            document.getElementById("city").value = p.cidade || "";
            document.getElementById("neighborhood").value = p.bairro || "";
            document.getElementById("address").value = p.enderecoPrincipal || "";
            document.getElementById("contact").value = p.preferenciaContato || "";
            document.getElementById("photo").value = p.fotoUrl || "";
            document.getElementById("whatsappActive").checked = Boolean(contato.whatsAppAtivo);
            document.getElementById("whatsappMessage").value = contato.mensagemInicialWhatsApp || "";
        } catch (e) { setMessage("formMessage", e.message); }
        document.getElementById("clientProfileForm").addEventListener("submit", async event => {
            event.preventDefault();
            try {
                const photoFile = document.getElementById("photoFile").files[0];
                let photoUrl = document.getElementById("photo").value.trim() || null;
                if (photoFile) {
                    if (photoFile.size > 5 * 1024 * 1024) throw new Error("A foto deve ter no máximo 5 MB.");
                    const formData = new FormData(); formData.append("arquivo", photoFile); formData.append("privado", "false");
                    photoUrl = (await C.api("/arquivos", { method: "POST", body: formData })).url;
                }
                const body = { nome: document.getElementById("name").value.trim(), telefone: document.getElementById("phone").value.replace(/\D/g,""), cidade: document.getElementById("city").value.trim(), bairro: document.getElementById("neighborhood").value.trim() || null, enderecoPrincipal: document.getElementById("address").value.trim() || null, preferenciaContato: document.getElementById("contact").value || null, fotoUrl };
                await C.api("/clientes/me", { method: "PUT", body });
                await C.api("/whatsapp/contato/me", { method: "PUT", body: {
                    telefone: body.telefone,
                    whatsAppAtivo: document.getElementById("whatsappActive").checked,
                    mensagemInicialWhatsApp: document.getElementById("whatsappMessage").value.trim() || null
                }});
                document.getElementById("photo").value = photoUrl || "";
                document.getElementById("photoFile").value = "";
                setMessage("formMessage", "Perfil e preferência de WhatsApp atualizados.", true);
            }
            catch (e) { setMessage("formMessage", e.message); }
        });
    }

    window.ChamaproApp?.ready.then(user => {
        if (!user || user.perfil !== "CLIENTE") return;
        if (document.getElementById("createOrderForm")) iniciarCriacao().catch(e => setMessage("formMessage", e.message));
        if (document.getElementById("clientProfileForm")) carregarPerfil();
        if (document.getElementById("ordersList")) carregarPainel();
    });
})();
