(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const target = document.getElementById("professionalProfile");
    const reviewsTarget = document.getElementById("professionalReviews");
    const reportDialog = document.getElementById("reportDialog");
    const reportForm = document.getElementById("reportForm");
    const id = Number(new URLSearchParams(location.search).get("id"));
    let denuncia = null;

    function abrirDenuncia(tipoEntidade, entidadeId, denunciadoId) {
        denuncia = { tipoEntidade, entidadeId, denunciadoId };
        document.getElementById("reportMessage").textContent = "";
        reportForm.reset();
        reportDialog.showModal();
    }

    async function init() {
        if (!id) { target.innerHTML = '<p class="empty-state">Escolha um profissional na busca de compatíveis de um chamado.</p>'; return; }
        try {
            const p = await C.api(`/profissionais/${id}`);
            target.innerHTML = `<article class="app-card"><div class="app-row"><div><span class="badge ${p.verificado ? "badge-success" : "badge-warning"}">${p.verificado ? "Verificado" : "Em verificação"}</span><h1 style="margin:12px 0">${safe(p.nome)}</h1><p>${safe(p.descricao || "Este profissional ainda não incluiu uma descrição.")}</p><span class="app-muted">Nota ${C.formatarNota(p.notaMedia)} · ${p.totalAvaliacoes} avaliações · membro desde ${C.formatarData(p.membroDesde)}</span></div><div class="app-actions"><button class="btn btn-secondary" id="favoriteButton">Favoritar</button><a class="btn btn-primary" href="criar-chamado.html">Criar chamado</a><button class="btn btn-secondary" id="reportProfile">Denunciar perfil</button></div></div><p class="app-muted">${p.experienciaAnos != null ? `${p.experienciaAnos} anos de experiência` : "Experiência não informada"} · ${p.atendeUrgencia ? "Atendimento urgente" : "Sem urgência"} · ${p.atendeDomicilio ? "Atende em domicílio" : ""} ${p.atendeEstabelecimento ? "· Atende em estabelecimento" : ""}</p><h2>Serviços</h2><div class="app-stack">${p.especialidades.length ? p.especialidades.map(x => `<div class="app-row"><strong>${safe(x.categoria)}</strong><span>${x.faixaPrecoMin != null ? `${C.formatarMoeda(x.faixaPrecoMin)} – ${C.formatarMoeda(x.faixaPrecoMax)}` : "Orçamento sob consulta"}</span></div>`).join("") : '<p class="empty-state">Nenhuma categoria informada.</p>'}</div><h2 style="margin-top:20px">Áreas de atendimento</h2><p>${safe((p.areasAtendimento || []).join(" · ") || "Não informadas")}</p>${p.fotosTrabalhos?.length ? `<h2 style="margin-top:20px">Portfólio</h2><div class="app-grid">${p.fotosTrabalhos.map(url => `<img src="${safe(url)}" alt="Trabalho realizado pelo profissional" loading="lazy">`).join("")}</div>` : ""}</article>`;
            document.getElementById("favoriteButton").addEventListener("click", async event => {
                const button = event.currentTarget;
                try { await C.api(`/favoritos/${id}`, { method: "POST" }); button.textContent = "Adicionado aos favoritos"; button.disabled = true; }
                catch (e) { C.toast(e.message, "error"); }
            });
            document.getElementById("reportProfile").addEventListener("click", () => abrirDenuncia("PROFISSIONAL", p.id));

            const reviewResult = await C.api(`/profissionais/${id}/avaliacoes?pagina=1&tamanho=20`);
            const reviews = reviewResult.itens || [];
            reviewsTarget.innerHTML = reviews.length ? reviews.map(r => `<article class="app-row"><div><strong>${C.estrelas(r.nota, 11)} ${r.nota}/5</strong><p>${safe(r.comentario || "Sem comentário")}</p><button class="btn btn-secondary btn-small" data-report-review="${r.id}">Denunciar avaliação</button></div><span class="app-muted">${safe(r.nomeCliente)} · ${C.formatarData(r.data)}</span></article>`).join("") : '<p class="empty-state">Este profissional ainda não recebeu avaliações.</p>';
            reviewsTarget.querySelectorAll("[data-report-review]").forEach(button => button.addEventListener("click", () =>
                abrirDenuncia("AVALIACAO", Number(button.dataset.reportReview))));
        } catch (e) { target.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
    }

    document.getElementById("closeReport").addEventListener("click", () => reportDialog.close());
    reportForm.addEventListener("submit", async event => {
        event.preventDefault();
        if (!denuncia || !reportForm.reportValidity()) return;
        const button = reportForm.querySelector('[type="submit"]');
        button.disabled = true;
        try {
            await C.api("/denuncias", { method: "POST", body: {
                ...denuncia,
                motivo: document.getElementById("reportReason").value.trim(),
                descricao: document.getElementById("reportDescription").value.trim()
            }});
            reportDialog.close();
            C.toast("Denúncia enviada para análise.", "success");
        } catch (e) { document.getElementById("reportMessage").textContent = e.message; }
        finally { button.disabled = false; }
    });

    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "CLIENTE") init(); });
})();
