(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const target = document.getElementById("professionalProfile");
    const reviewsTarget = document.getElementById("professionalReviews");
    const reportDialog = document.getElementById("reportDialog");
    const reportForm = document.getElementById("reportForm");
    const id = Number(new URLSearchParams(location.search).get("id"));
    let denuncia = null;

    const initials = name => String(name || '').trim().split(/\s+/).filter(part => /^\p{L}/u.test(part)).slice(0, 2).map(part => part[0].toUpperCase()).join('') || '?';
    // ["Hidráulica", "Instalação"] → "Hidráulica e instalação"
    const joinWithE = list => {
        const names = list.map((name, i) => i ? name.toLowerCase() : name);
        return names.length <= 1 ? names[0] || '' : `${names.slice(0, -1).join(', ')} e ${names[names.length - 1]}`;
    };

    // Ícones do Figma (Versão Vitoria)
    const ICON = {
        check: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        pin: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M7 12.25C7 12.25 2.91663 8.75 2.91663 5.83333C2.91663 4.75037 3.34683 3.71175 4.11261 2.94598C4.87838 2.18021 5.91699 1.75 7 1.75C8.08293 1.75 9.12154 2.18021 9.88731 2.94598C10.6531 3.71175 11.0833 4.75037 11.0833 5.83333C11.0833 8.75 7 12.25 7 12.25Z" stroke="currentColor" stroke-width="1.16667" stroke-linecap="round" stroke-linejoin="round"/><path d="M8.45829 5.83333C8.45829 6.63875 7.80537 7.29167 7 7.29167C6.19454 7.29167 5.54163 6.63875 5.54163 5.83333C5.54163 5.02792 6.19454 4.375 7 4.375C7.80537 4.375 8.45829 5.02792 8.45829 5.83333Z" stroke="currentColor" stroke-width="1.16667" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        heart: '<svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true"><path d="M9 15C9 15 3.75 11.625 3.75 7.5C3.75689 6.89364 3.94743 6.30353 4.29648 5.80756C4.64554 5.3116 5.13671 4.93307 5.70525 4.7219C6.27378 4.51074 6.89296 4.47684 7.48115 4.62469C8.06933 4.77254 8.59889 5.09519 9 5.55009C9.40111 5.09519 9.93067 4.77254 10.5189 4.62469C11.107 4.47684 11.7262 4.51074 12.2948 4.7219C12.8633 4.93307 13.3545 5.3116 13.7035 5.80756C14.0526 6.30353 14.2431 6.89364 14.25 7.5C14.25 11.625 9 15 9 15Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        arrow: '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M3.33325 8H12.6666M8.66659 12L12.6666 8L8.66659 4" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        star14: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M7 1.75L8.575 5.01667L12.1334 5.54167L9.5667 8.05L10.15 11.6083L7 9.91667L3.85 11.6083L4.43337 8.05L1.8667 5.54167L5.425 5.01667L7 1.75Z" fill="currentColor" stroke="currentColor" stroke-width="1.16667" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        star11: '<svg width="11" height="11" viewBox="0 0 11 11" fill="none" aria-hidden="true"><path d="M5.5 1.375L6.73739 3.94167L9.53322 4.35417L7.51655 6.325L7.97489 9.12083L5.5 7.79167L3.02489 9.12083L3.48322 6.325L1.46655 4.35417L4.26239 3.94167L5.5 1.375Z" fill="currentColor" stroke="currentColor" stroke-width="0.916667" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    };
    const stars = (nota, icon = ICON.star14) => Array.from({ length: 5 }, (_, i) =>
        i < Math.round(Number(nota) || 0) ? icon : `<span class="star-off">${icon}</span>`).join('');

    function abrirDenuncia(tipoEntidade, entidadeId, denunciadoId) {
        denuncia = { tipoEntidade, entidadeId, denunciadoId };
        document.getElementById("reportMessage").textContent = "";
        reportForm.reset();
        reportDialog.showModal();
    }

    function setFavorite(button, active) {
        button.setAttribute('aria-pressed', String(active));
        button.setAttribute('aria-label', active ? 'Remover dos favoritos' : 'Favoritar profissional');
        button.title = active ? 'Remover dos favoritos' : 'Favoritar profissional';
    }

    async function init() {
        if (!id) { target.innerHTML = '<div class="fg-card"><p class="empty-state">Escolha um profissional na busca de compatíveis de um chamado.</p></div>'; reviewsTarget.innerHTML = ''; return; }
        try {
            const p = await C.api(`/profissionais/${id}`);
            const categorias = (p.especialidades || []).map(x => x.categoria);
            const areas = p.areasAtendimento || [];
            const local = [areas.slice(0, 2).join(', ') + (areas.length > 2 ? ` +${areas.length - 2}` : ''), joinWithE(categorias)].filter(Boolean).join(' · ');
            const total = p.totalAvaliacoes === 1 ? '1 avaliação' : `${p.totalAvaliacoes} avaliações`;
            const atendimento = [p.atendeUrgencia ? 'Atende urgências' : 'Atendimento agendado', p.atendeDomicilio ? 'em domicílio' : '', p.atendeEstabelecimento ? 'no estabelecimento' : ''].filter(Boolean).join(' · ');
            const extras = [p.experienciaAnos != null ? `${p.experienciaAnos} anos de experiência` : '', p.membroDesde ? `Membro desde ${C.formatarData(p.membroDesde)}` : ''].filter(Boolean).join(' · ');

            document.title = `CHAMAPRO | ${p.nome}`;
            document.getElementById('breadcrumbName').textContent = `Perfil de ${p.nome}`;
            document.getElementById('profileTitle').textContent = p.nome;

            target.innerHTML = `<section class="fg-card fg-card-lg" style="gap:22px">`
                + `<div class="fg-perfil-id"><span class="fg-avatar fg-avatar-lg">${safe(initials(p.nome))}</span>`
                + `<div class="fg-perfil-info"><div class="fg-proposta-nome"><h2>${safe(p.nome)}</h2>${p.verificado ? `<span class="fg-tag fg-tag-success">${ICON.check} VERIFICADO</span>` : '<span class="fg-tag fg-tag-warning">EM VERIFICAÇÃO</span>'}</div>`
                + `<div class="fg-linha-icone" style="color:var(--fg-text);font-weight:500"><span class="fg-stars">${stars(p.notaMedia)}</span>${C.formatarNota(p.notaMedia)} · ${total}</div>`
                + (local ? `<div class="fg-linha-icone">${ICON.pin}<span>${safe(local)}</span></div>` : '')
                + `</div><button class="fg-btn fg-favoritar" id="favoriteButton" type="button" aria-pressed="false" aria-label="Favoritar profissional">${ICON.heart}</button></div>`
                + `<p class="fg-descricao">${safe(p.descricao || "Este profissional ainda não incluiu uma descrição.")}</p>`
                + (extras ? `<p class="fg-small" style="margin:-10px 0 0">${safe(extras)}</p>` : '')
                + `<div class="fg-secao"><span class="fg-secao-titulo">ÁREAS DE ATUAÇÃO</span><div class="fg-chips">${p.especialidades?.length ? p.especialidades.map(x => `<span class="fg-chip">${safe(x.categoria)}${x.faixaPrecoMin != null ? ` <small>${C.formatarMoeda(x.faixaPrecoMin)} – ${C.formatarMoeda(x.faixaPrecoMax)}</small>` : ''}</span>`).join('') : '<span class="fg-small">Nenhuma categoria informada.</span>'}</div></div>`
                + (p.fotosTrabalhos?.length ? `<div class="fg-secao"><span class="fg-secao-titulo">PORTFÓLIO</span><div class="fg-portfolio">${p.fotosTrabalhos.map(url => `<img src="${safe(url)}" alt="Trabalho realizado pelo profissional" loading="lazy">`).join('')}</div></div>` : '')
                + `<div class="fg-secao fg-disponibilidade"><div class="fg-stack" style="gap:6px"><span class="fg-secao-titulo">DISPONIBILIDADE</span><span class="fg-valor-destaque"><span class="fg-ponto-online" aria-hidden="true"></span>${safe(atendimento)}</span></div>`
                + `<a class="fg-btn fg-btn-primary" href="criar-chamado.html?profissionalId=${p.id}">Solicitar orçamento ${ICON.arrow}</a></div>`
                + `<button class="fg-link" id="reportProfile" type="button" style="align-self:flex-start">Denunciar perfil</button>`
                + `</section>`;

            // Favoritar / desfavoritar (rotas de favoritos da API)
            const favoriteButton = document.getElementById("favoriteButton");
            try {
                const favorites = await C.api('/favoritos');
                setFavorite(favoriteButton, (favorites || []).some(f => Number(f.profissionalId) === id));
            } catch { setFavorite(favoriteButton, false); }
            favoriteButton.addEventListener("click", async () => {
                if (favoriteButton.disabled) return;
                const active = favoriteButton.getAttribute('aria-pressed') === 'true';
                favoriteButton.disabled = true;
                try {
                    await C.api(`/favoritos/${id}`, { method: active ? "DELETE" : "POST" });
                    setFavorite(favoriteButton, !active);
                    C.toast(active ? "Removido dos favoritos." : "Adicionado aos favoritos.", "success");
                } catch (e) { C.toast(e.message, "error"); }
                finally { favoriteButton.disabled = false; }
            });
            document.getElementById("reportProfile").addEventListener("click", () => abrirDenuncia("PROFISSIONAL", p.id));

            // Resumo das avaliações (nota grande do Figma)
            const summary = document.getElementById('reviewsSummary');
            if (summary) {
                document.getElementById('reviewsAverage').textContent = C.formatarNota(p.notaMedia);
                document.getElementById('reviewsStars').innerHTML = stars(p.notaMedia);
                document.getElementById('reviewsTotal').textContent = p.totalAvaliacoes === 1 ? '1 avaliação verificada' : `${p.totalAvaliacoes} avaliações verificadas`;
                summary.hidden = false;
            }

            const reviewResult = await C.api(`/profissionais/${id}/avaliacoes?pagina=1&tamanho=20`);
            const reviews = reviewResult.itens || [];
            reviewsTarget.innerHTML = reviews.length ? reviews.map(r => `<article class="fg-avaliacao"><div class="fg-avaliacao-topo"><strong>${safe(r.nomeCliente)}</strong><span class="fg-stars" aria-label="Nota ${r.nota} de 5">${stars(r.nota, ICON.star11)}</span></div><p class="fg-text">${safe(r.comentario || "Avaliou sem deixar comentário.")}</p><div class="fg-avaliacao-topo"><span class="fg-small">${C.formatarData(r.data)}</span><button class="fg-link" data-report-review="${r.id}" type="button">Denunciar avaliação</button></div></article>`).join("") : '<p class="empty-state">Este profissional ainda não recebeu avaliações.</p>';
            reviewsTarget.querySelectorAll("[data-report-review]").forEach(button => button.addEventListener("click", () =>
                abrirDenuncia("AVALIACAO", Number(button.dataset.reportReview))));
        } catch (e) { target.innerHTML = `<div class="fg-card"><p class="empty-state">${safe(e.message)}</p></div>`; reviewsTarget.innerHTML = ''; }
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
