(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const documents = window.ChamaproPrivateDocuments.create();
    let sending = false;
    const uploadedFiles = new WeakMap();
    const $ = id => document.getElementById(id);
    const message = (text, ok = false) => { const el = $("verificationMessage"); el.textContent = text || ""; el.style.color = ok ? "var(--fg-success, var(--color-success))" : "var(--fg-danger, var(--color-danger))"; };
    const statusLabel = value => ({
        Pendente: "Aguardando verificação",
        EmAnalise: "Em análise",
        Aprovado: "Aprovado",
        Rejeitado: "Reprovado",
        DocumentacaoPendente: "Documentação pendente",
        Suspenso: "Suspenso"
    }[value] || value);

    // Ícones do Figma (Versão Vitoria)
    const ICON = {
        doc: '<svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true"><path d="M10.5 2.25H4.5V15.75H13.5V5.25L10.5 2.25Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M10.5 2.25V5.25H13.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        check: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        clock: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M10.5 6C10.5 8.48528 8.48528 10.5 6 10.5C3.51472 10.5 1.5 8.48528 1.5 6C1.5 3.51472 3.51472 1.5 6 1.5C8.48528 1.5 10.5 3.51472 10.5 6Z" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/><path d="M6 3.5V6L7.5 7" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    };

    function tagFor(status) {
        if (status === 'Aprovado') return `<span class="fg-tag fg-tag-success">${ICON.check} Validado</span>`;
        if (['Rejeitado', 'DocumentacaoPendente', 'Suspenso'].includes(status)) return `<span class="fg-tag fg-tag-danger">${safe(statusLabel(status))}</span>`;
        return `<span class="fg-tag fg-tag-warning">${ICON.clock} ${safe(statusLabel(status))}</span>`;
    }

    // Card de status do Figma: Enviado → Em análise → Aprovado
    function renderStatus(items, profile) {
        const tag = $('statusTag'), text = $('statusTagText'), body = $('statusText'), sentAt = $('statusSentAt');
        if (!tag || !text || !body) return;
        const latest = [...items].sort((a, b) => new Date(b.criadoEm) - new Date(a.criadoEm))[0];
        const overall = profile?.verificado ? 'Aprovado' : (profile?.statusVerificacao || latest?.status || '');
        const states = {
            '': { step: 0, label: 'PENDENTE', tag: '', text: 'Envie seus documentos para começar a validação. A equipe analisa cada envio.' },
            Pendente: { step: 2, label: 'AGUARDANDO ANÁLISE', tag: 'fg-tag-warning', text: 'Recebemos seus documentos. A equipe vai analisá-los em breve.' },
            EmAnalise: { step: 2, label: 'EM ANÁLISE', tag: 'fg-tag-warning', text: 'A equipe está verificando as informações. Você poderá enviar propostas nesta categoria após a aprovação.' },
            DocumentacaoPendente: { step: 1, label: 'DOCUMENTAÇÃO PENDENTE', tag: 'fg-tag-danger', text: 'A equipe pediu uma correção. Confira a observação no documento e envie uma nova versão.' },
            Rejeitado: { step: 1, label: 'REPROVADO', tag: 'fg-tag-danger', text: 'Um documento foi reprovado. Confira a observação e envie uma nova versão.' },
            Suspenso: { step: 1, label: 'SUSPENSO', tag: 'fg-tag-danger', text: 'Sua verificação está suspensa. Fale com a equipe para entender os próximos passos.' },
            Aprovado: { step: 3, label: 'APROVADO', tag: 'fg-tag-success', text: 'Perfil validado! O selo de verificado já aparece para os clientes.' }
        };
        const state = states[overall] || states.Pendente;
        tag.className = `fg-tag ${state.tag}`;
        text.textContent = state.label;
        body.textContent = state.text;
        if (sentAt) {
            const first = items.map(v => new Date(v.criadoEm)).filter(d => Number.isFinite(d.getTime())).sort((a, b) => a - b)[0];
            sentAt.textContent = first ? `Enviado em ${C.formatarData(first)}` : '';
        }
        const dots = document.querySelectorAll ? [...document.querySelectorAll('.fg-etapas .fg-ponto')] : [];
        const lines = document.querySelectorAll ? [...document.querySelectorAll('.fg-etapas .fg-linha')] : [];
        const labels = document.querySelectorAll ? [...document.querySelectorAll('#statusSteps li')] : [];
        dots.forEach((dot, i) => {
            dot.classList.toggle('fg-feito', i + 1 < state.step || state.step === 3);
            dot.classList.toggle('fg-atual', i + 1 === state.step && state.step !== 3);
        });
        lines.forEach((line, i) => line.classList.toggle('fg-feito', i + 1 < state.step));
        labels.forEach((item, i) => item.classList.toggle('fg-atual', i + 1 === state.step));
    }

    async function loadProfile() {
        try {
            const profile = await C.api('/profissionais/me');
            const categoryIds = (profile?.servicos || []).map(s => s.categoriaId);
            if (categoryIds.length && $('statusCategory')) {
                const categories = await C.api('/categorias');
                const names = Array.isArray(categories) ? categories.filter(c => categoryIds.includes(c.id)).map(c => c.nome) : [];
                if (names.length) $('statusCategory').textContent = names.join(' · ');
            }
            return profile && typeof profile === 'object' ? profile : null;
        } catch { return null; }
    }

    async function loadHistory(profile) {
        const target = $("verificationHistory");
        documents.release();
        target.innerHTML = '<p role="status">Carregando documentos…</p>';
        try {
            const items = await C.api("/verificacoes/me");
            renderStatus(items, profile);
            target.innerHTML = items.length ? items.map(v => `<article class="fg-row"><span class="fg-doc-icone">${ICON.doc}</span><div class="fg-row-info"><strong>${safe(v.tipoDocumento)}</strong><span>${safe(statusLabel(v.status))} · enviado ${C.formatarData(v.criadoEm)}</span>${v.observacao ? `<span>${safe(v.observacao)}</span>` : ''}<div data-document-result="${v.id}" role="status"></div></div><div class="fg-doc-acoes">${tagFor(v.status)}<button type="button" class="fg-link" data-private-document="${v.id}">Consultar meu documento</button></div></article>`).join("") : '<p class="empty-state">Nenhum documento enviado. Consulte as orientações da equipe antes de enviar documentos.</p>';
            target.querySelectorAll('[data-private-document]').forEach(button => button.addEventListener('click', async () => {
                if (button.disabled) return;
                button.disabled = true;
                const output = target.querySelector(`[data-document-result="${button.dataset.privateDocument}"]`);
                const item = items.find(v => String(v.id) === button.dataset.privateDocument);
                output.textContent = 'Consultando documento…';
                try {
                    const url = await documents.load(item.urlDocumento);
                    if (!target.contains(output)) { documents.release(url); return; }
                    if (output.dataset.blobUrl) documents.release(output.dataset.blobUrl);
                    output.dataset.blobUrl = url;
                    output.innerHTML = `<a class="fg-btn fg-btn-sm" href="${url}" target="_blank" rel="noopener">Abrir meu documento autenticado</a>`;
                } catch (error) { output.textContent = error.message || 'Não foi possível consultar o documento. Tente novamente.'; }
                finally { button.disabled = false; }
            }));
        } catch { target.innerHTML = '<p role="alert">Não foi possível carregar os documentos. Nenhuma informação foi apagada.</p><button id="retryVerificationHistory" type="button" class="fg-btn">Tentar novamente</button>'; target.querySelector('#retryVerificationHistory').addEventListener('click', () => loadHistory(profile)); }
    }

    // "Relatar um problema": a API aceita denúncia de avaliação recebida
    // (perfil e chamado de cliente ainda não têm um identificador disponível aqui).
    function setupProblemForm(profile) {
        const form = $('problemForm');
        if (!form) return;
        const type = $('problemType'), field = $('problemTargetField'), select = $('problemTarget'), msg = $('problemMessage'), submit = $('problemSubmit');
        const say = (text, ok = false) => { msg.textContent = text; msg.style.color = ok ? 'var(--fg-success)' : 'var(--fg-danger)'; };
        let reviewsLoaded = false;
        type.addEventListener('change', async () => {
            say('');
            field.hidden = type.value !== 'AVALIACAO';
            if (type.value !== 'AVALIACAO' || reviewsLoaded) return;
            select.innerHTML = '<option value="">Carregando avaliações…</option>';
            try {
                if (!profile?.id) throw new Error('Perfil profissional indisponível.');
                const result = await C.api(`/profissionais/${profile.id}/avaliacoes?pagina=1&tamanho=50`);
                const reviews = result?.itens || [];
                select.innerHTML = reviews.length
                    ? '<option value="">Selecione a avaliação</option>' + reviews.map(r => `<option value="${r.id}">${safe(r.nomeCliente)} · ${r.nota}/5 · ${safe((r.comentario || 'sem comentário').slice(0, 40))}</option>`).join('')
                    : '<option value="">Você ainda não recebeu avaliações</option>';
                reviewsLoaded = reviews.length > 0;
            } catch (error) { select.innerHTML = '<option value="">Não foi possível carregar</option>'; say(error.message); }
        });
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (submit.disabled) return;
            const descricao = $('problemDescription').value.trim();
            if (type.value !== 'AVALIACAO') { say('Escolha o que deseja denunciar.'); return; }
            if (!select.value) { say('Escolha a avaliação que deseja denunciar.'); return; }
            if (descricao.length < 10) { say('Descreva o que aconteceu (mínimo de 10 caracteres).'); return; }
            submit.disabled = true;
            try {
                await C.api('/denuncias', { method: 'POST', body: { tipoEntidade: 'AVALIACAO', entidadeId: Number(select.value), motivo: $('problemReason').value, descricao } });
                form.reset(); field.hidden = true;
                say('Denúncia enviada. A equipe vai analisar com sigilo.', true);
            } catch (error) { say(error.message); }
            finally { submit.disabled = false; }
        });
    }

    async function init() {
        const form = $("verificationForm");
        const dialog = $('documentDialog');
        $('openDocumentDialog')?.addEventListener('click', () => { message(''); dialog?.showModal(); });
        $('closeDocumentDialog')?.addEventListener('click', () => dialog?.close());
        const onboarding = new URLSearchParams(location.search).get("onboarding") === "1";
        const onboardingPanel = $("verificationOnboarding");
        const finishButton = $("finishProfessionalSetup");
        if (onboarding && onboardingPanel && finishButton) {
            onboardingPanel.hidden = false;
            finishButton.addEventListener("click", async () => {
                finishButton.disabled = true;
                finishButton.textContent = "Conferindo perfil…";
                const result = $("onboardingFinishMessage");
                result.textContent = "";
                try {
                    const panel = await C.api("/painel/profissional");
                    const complete = panel.cadastroCompleto === true;
                    if (!complete) {
                        throw new Error("Conclua profissão, localização, preços e horários antes de finalizar.");
                    }
                    window.location.assign("painel.html");
                } catch (error) {
                    result.textContent = error.message || "Não foi possível concluir o cadastro.";
                    result.classList.add("error");
                    finishButton.disabled = false;
                    finishButton.textContent = "Concluir cadastro e abrir painel";
                }
            });
        }
        let profile = null;
        form.addEventListener("submit", async event => {
            event.preventDefault(); if (sending || !form.reportValidity()) return;
            const file = $("documentFile").files[0];
            if (!file || file.size > 10 * 1024 * 1024) { message("Selecione um arquivo válido de até 10 MB."); return; }
            const button = $("submitVerification"); button.disabled = true; button.textContent = "Enviando…";
            sending = true;
            try {
                if (!uploadedFiles.has(file)) {
                    const data = new FormData(); data.append("arquivo", file); data.append("privado", "true");
                    uploadedFiles.set(file, await C.api("/arquivos", { method: "POST", body: data }));
                }
                const uploaded = uploadedFiles.get(file);
                await C.api("/verificacoes", { method: "POST", body: { tipoDocumento: $("documentType").value, urlDocumento: uploaded.url, observacao: $("documentNote").value.trim() || null } });
                message("Documento enviado para análise.", true); form.reset(); dialog?.close(); await loadHistory(profile);
            } catch (e) { message(e.message); }
            finally { sending = false; button.disabled = false; button.textContent = "Enviar para análise"; }
        });
        profile = await loadProfile();
        await loadHistory(profile);
        setupProblemForm(profile);
    }
    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "PROFISSIONAL") init(); });
})();
