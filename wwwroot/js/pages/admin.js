(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const set = (id, value) => { const el = document.getElementById(id); if (el) el.textContent = value; };
    const error = (container, e) => { container.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; };

    function prepararAuditoria() {
        const orders = document.getElementById("adminOrders")?.closest("section");
        const content = document.querySelector(".app-content");
        if (!content) return null;

        const section = document.createElement("section");
        section.className = "app-card";
        section.style.marginTop = "20px";
        const heading = document.createElement("h2");
        heading.textContent = "Atividade administrativa";
        const list = document.createElement("div");
        list.id = "adminAudit";
        list.className = "app-stack";
        list.innerHTML = '<p class="empty-state">Carregando histórico…</p>';
        section.append(heading, list);
        if (orders) orders.after(section); else content.append(section);
        return list;
    }

    const auditList = prepararAuditoria();

    function prepararRelatorioProfissionais() {
        const grid = document.getElementById("categoryReport")?.closest(".app-grid-two");
        if (!grid) return null;
        const card = document.createElement("article");
        card.className = "app-card";
        const heading = document.createElement("h2");
        heading.textContent = "Profissionais mais avaliados";
        const list = document.createElement("div");
        list.id = "topRatedProfessionals";
        list.className = "app-stack";
        list.innerHTML = '<p class="empty-state">Carregando…</p>';
        card.append(heading, list);
        grid.append(card);
        return list;
    }

    const topRatedList = prepararRelatorioProfissionais();

    async function loadMetrics() {
        const data = await C.api("/admin/relatorios");
        set("metricUsers", data.usuarios); set("metricClients", `${data.clientes} clientes`);
        set("metricPros", data.profissionais); set("metricVerified", `${data.profissionaisVerificados} verificados`);
        set("metricOrders", data.chamados); set("metricOrderStates", `${data.chamadosConcluidos} concluídos · ${data.chamadosCancelados} cancelados`);
        const cats = document.getElementById("categoryReport");
        cats.innerHTML = data.categoriasMaisSolicitadas.length ? data.categoriasMaisSolicitadas.map(c => `<div class="app-row"><strong>${safe(c.categoria)}</strong><span>${c.chamados} chamados</span></div>`).join("") : '<p class="empty-state">Ainda não há chamados por categoria.</p>';
        if (topRatedList) topRatedList.innerHTML = data.profissionaisMaisAvaliados?.length
            ? data.profissionaisMaisAvaliados.map(p => `<div class="app-row"><strong>${safe(p.nome)}</strong><span>${C.formatarNota(p.media)} · ${p.avaliacoes} avaliações</span></div>`).join("")
            : '<p class="empty-state">Ainda não há avaliações.</p>';
    }
    async function loadVerifications() {
        const list = document.getElementById("verificationList");
        try {
            const items = await C.api("/verificacoes");
            list.innerHTML = items.length ? items.map(v => `<article class="app-row"><div><strong>Documento ${v.id} · profissional ${v.profissionalId}</strong><div class="app-muted">${safe(v.tipoDocumento)} · ${safe(v.status)} · ${C.formatarData(v.criadoEm)}</div><a href="${safe(v.urlDocumento)}" target="_blank" rel="noopener">Abrir documento protegido</a><div class="app-muted">${safe(v.observacao || "")}</div></div><div class="app-actions"><button class="btn btn-primary btn-small" data-verification="${v.id}" data-status="Aprovado">Aprovar</button><button class="btn btn-secondary btn-small" data-verification="${v.id}" data-status="DocumentacaoPendente">Solicitar correção</button><button class="btn btn-danger btn-small" data-verification="${v.id}" data-status="Rejeitado">Reprovar</button></div></article>`).join("") : '<p class="empty-state">Nenhuma verificação pendente.</p>';
            list.querySelectorAll("[data-verification]").forEach(button => button.addEventListener("click", async () => {
                const observacao = prompt("Observação para o profissional (opcional):") || null;
                try { await C.api(`/verificacoes/${button.dataset.verification}/avaliar`, { method: "PATCH", body: { status: button.dataset.status, observacao } }); await loadVerifications(); C.toast("Verificação atualizada.", "success"); }
                catch (e) { C.toast(e.message, "error"); }
            }));
        } catch (e) { error(list, e); }
    }
    async function loadReports() {
        const list = document.getElementById("reportsList");
        try {
            const items = await C.api("/denuncias");
            list.innerHTML = items.length ? items.map(r => `<article class="app-row"><div><strong>${safe(r.motivo)} · ${safe(r.tipoEntidade)} #${r.entidadeId}</strong><div class="app-muted">${safe(r.descricao)} · ${safe(r.status)}</div><span class="app-muted">${C.formatarData(r.criadoEm)}</span></div><div class="app-actions"><button class="btn btn-secondary btn-small" data-report="${r.id}" data-status="EmAnalise">Em análise</button><button class="btn btn-primary btn-small" data-report="${r.id}" data-status="Resolvido">Resolver</button><button class="btn btn-danger btn-small" data-report="${r.id}" data-status="Rejeitado">Rejeitar</button></div></article>`).join("") : '<p class="empty-state">Nenhuma denúncia pendente.</p>';
            list.querySelectorAll("[data-report]").forEach(button => button.addEventListener("click", async () => {
                const observacao = prompt("Registre a providência ou justificativa:") || "";
                try { await C.api(`/denuncias/${button.dataset.report}/resolver`, { method: "PATCH", body: { status: button.dataset.status, observacao } }); await loadReports(); C.toast("Denúncia atualizada.", "success"); }
                catch (e) { C.toast(e.message, "error"); }
            }));
        } catch (e) { error(list, e); }
    }
    async function loadUsers() {
        const list = document.getElementById("usersList");
        const query = new URLSearchParams();
        const search = document.getElementById("userSearch").value.trim();
        const role = document.getElementById("userRole").value;
        const active = document.getElementById("userActive").value;
        if (search) query.set("busca", search); if (role) query.set("perfil", role); if (active) query.set("ativo", active);
        try {
            const items = await C.api(`/admin/usuarios?${query}`);
            list.innerHTML = items.length ? items.map(u => `<article class="app-row"><div><strong>${safe(u.nome)}</strong><div class="app-muted">${safe(u.email)} · ${safe(u.perfil)} · ${u.ativo ? "Ativo" : "Bloqueado"}</div></div><button class="btn ${u.ativo ? "btn-danger" : "btn-primary"} btn-small" data-user="${u.id}" data-active="${!u.ativo}" ${u.perfil === "Admin" ? "disabled" : ""}>${u.ativo ? "Bloquear" : "Reativar"}</button></article>`).join("") : '<p class="empty-state">Nenhum usuário encontrado.</p>';
            list.querySelectorAll("[data-user]").forEach(button => button.addEventListener("click", async () => {
                try { await C.api(`/admin/usuarios/${button.dataset.user}/status`, { method: "PATCH", body: { ativo: button.dataset.active === "true" } }); await loadUsers(); C.toast("Status da conta atualizado.", "success"); }
                catch (e) { C.toast(e.message, "error"); }
            }));
        } catch (e) { error(list, e); }
    }
    async function loadOrders() {
        const list = document.getElementById("adminOrders");
        try {
            const items = await C.api("/admin/pedidos");
            list.innerHTML = items.length ? items.slice(0,50).map(p => `<div class="app-row"><div><strong>#${p.id} · ${safe(p.titulo)}</strong><div class="app-muted">${safe(p.categoria)} · ${safe(p.clienteNome)} · ${safe([p.cidade,p.bairro].filter(Boolean).join(" · "))}</div></div><span class="badge badge-info">${safe(p.status)}</span></div>`).join("") : '<p class="empty-state">Ainda não há chamados.</p>';
        } catch (e) { error(list, e); }
    }
    async function loadAudit() {
        if (!auditList) return;
        try {
            const items = await C.api("/admin/auditoria?limite=50");
            auditList.replaceChildren();
            if (!items.length) {
                const empty = document.createElement("p");
                empty.className = "empty-state";
                empty.textContent = "Nenhuma ação administrativa registrada.";
                auditList.append(empty);
                return;
            }

            for (const item of items) {
                const row = document.createElement("article");
                row.className = "app-row";
                const text = document.createElement("div");
                const title = document.createElement("strong");
                title.textContent = `${item.acao} · ${item.entidade} #${item.entidadeId}`;
                const detail = document.createElement("div");
                detail.className = "app-muted";
                detail.textContent = `${item.adminNome} · ${item.estadoAnterior || "—"} → ${item.estadoNovo || "—"} · ${C.formatarData(item.criadoEm)}`;
                text.append(title, detail);
                row.append(text);
                auditList.append(row);
            }
        } catch (e) { error(auditList, e); }
    }
    async function loadCategories() {
        const list = document.getElementById("categoryList");
        try {
            const items = await C.api("/categorias");
            list.innerHTML = items.length ? items.map(c => `<div class="app-row"><div><strong>${safe(c.nome)}</strong><div class="app-muted">${safe(c.icone || "Sem ícone")}</div></div><div class="app-actions"><button class="btn btn-secondary btn-small" data-edit-category="${c.id}" data-name="${safe(c.nome)}" data-icon="${safe(c.icone || "")}">Editar</button><button class="btn btn-danger btn-small" data-delete-category="${c.id}">Desativar</button></div></div>`).join("") : '<p class="empty-state">Nenhuma categoria ativa.</p>';
            list.querySelectorAll("[data-edit-category]").forEach(b => b.addEventListener("click", () => { document.getElementById("categoryId").value = b.dataset.editCategory; document.getElementById("categoryName").value = b.dataset.name; document.getElementById("categoryIcon").value = b.dataset.icon; }));
            list.querySelectorAll("[data-delete-category]").forEach(b => b.addEventListener("click", async () => { if (!confirm("Desativar esta categoria?")) return; try { await C.api(`/categorias/${b.dataset.deleteCategory}`, { method: "DELETE" }); await loadCategories(); } catch (e) { C.toast(e.message, "error"); } }));
        } catch (e) { error(list, e); }
    }
    function configureCategories() {
        const form = document.getElementById("categoryForm");
        form.addEventListener("submit", async event => {
            event.preventDefault();
            const id = document.getElementById("categoryId").value;
            const body = { nome: document.getElementById("categoryName").value.trim(), icone: document.getElementById("categoryIcon").value.trim() || null };
            try { await C.api(id ? `/categorias/${id}` : "/categorias", { method: id ? "PUT" : "POST", body }); form.reset(); document.getElementById("categoryId").value = ""; set("categoryMessage", "Categoria salva."); await Promise.all([loadCategories(), loadMetrics()]); }
            catch (e) { set("categoryMessage", e.message); }
        });
        document.getElementById("clearCategory").addEventListener("click", () => { form.reset(); document.getElementById("categoryId").value = ""; });
    }
    window.ChamaproApp?.ready.then(user => {
        if (!user || user.perfil !== "ADMIN") return;
        document.getElementById("searchUsers").addEventListener("click", loadUsers);
        document.getElementById("userSearch").addEventListener("keydown", e => { if (e.key === "Enter") { e.preventDefault(); loadUsers(); } });
        configureCategories();
        Promise.allSettled([loadMetrics(), loadVerifications(), loadReports(), loadUsers(), loadOrders(), loadCategories(), loadAudit()]);
    });
})();
