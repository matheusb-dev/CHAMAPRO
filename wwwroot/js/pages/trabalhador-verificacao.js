(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const documents = window.ChamaproPrivateDocuments.create();
    let sending = false;
    const uploadedFiles = new WeakMap();
    const message = (text, ok = false) => { const el = document.getElementById("verificationMessage"); el.textContent = text || ""; el.style.color = ok ? "var(--color-success)" : "var(--color-danger)"; };
    const statusLabel = value => ({
        Pendente: "Aguardando verificação",
        EmAnalise: "Em análise",
        Aprovado: "Aprovado",
        Rejeitado: "Reprovado",
        DocumentacaoPendente: "Documentação pendente",
        Suspenso: "Suspenso"
    }[value] || value);
    async function loadHistory() {
        const target = document.getElementById("verificationHistory");
        documents.release();
        target.innerHTML = '<p role="status">Carregando documentos…</p>';
        try {
            const items = await C.api("/verificacoes/me");
            target.innerHTML = items.length ? items.map(v => `<article class="app-row"><div><strong>${safe(v.tipoDocumento)}</strong><div class="app-muted">${safe(statusLabel(v.status))} · enviado ${C.formatarData(v.criadoEm)}</div><p>${safe(v.observacao || "Sem observações")}</p><button type="button" class="btn btn-secondary" data-private-document="${v.id}">Consultar meu documento</button><div data-document-result="${v.id}" role="status"></div></div></article>`).join("") : '<p class="empty-state">Nenhum documento enviado. Consulte as orientações da equipe antes de enviar documentos.</p>';
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
                    output.innerHTML = `<a class="btn btn-secondary" href="${url}" target="_blank" rel="noopener">Abrir meu documento autenticado</a>`;
                } catch (error) { output.textContent = error.message || 'Não foi possível consultar o documento. Tente novamente.'; }
                finally { button.disabled = false; }
            }));
        } catch { target.innerHTML = '<p role="alert">Não foi possível carregar os documentos. Nenhuma informação foi apagada.</p><button id="retryVerificationHistory" type="button" class="btn btn-secondary">Tentar novamente</button>'; target.querySelector('#retryVerificationHistory').addEventListener('click', loadHistory); }
    }
    async function init() {
        const form = document.getElementById("verificationForm");
        const onboarding = new URLSearchParams(location.search).get("onboarding") === "1";
        const onboardingPanel = document.getElementById("verificationOnboarding");
        const finishButton = document.getElementById("finishProfessionalSetup");
        if (onboarding && onboardingPanel && finishButton) {
            onboardingPanel.hidden = false;
            finishButton.addEventListener("click", async () => {
                finishButton.disabled = true;
                finishButton.textContent = "Conferindo perfil…";
                const result = document.getElementById("onboardingFinishMessage");
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
        form.addEventListener("submit", async event => {
            event.preventDefault(); if (sending || !form.reportValidity()) return;
            const file = document.getElementById("documentFile").files[0];
            if (!file || file.size > 10 * 1024 * 1024) { message("Selecione um arquivo válido de até 10 MB."); return; }
            const button = document.getElementById("submitVerification"); button.disabled = true; button.textContent = "Enviando…";
            sending = true;
            try {
                if (!uploadedFiles.has(file)) {
                    const data = new FormData(); data.append("arquivo", file); data.append("privado", "true");
                    uploadedFiles.set(file, await C.api("/arquivos", { method: "POST", body: data }));
                }
                const uploaded = uploadedFiles.get(file);
                await C.api("/verificacoes", { method: "POST", body: { tipoDocumento: document.getElementById("documentType").value, urlDocumento: uploaded.url, observacao: document.getElementById("documentNote").value.trim() || null } });
                message("Documento enviado para análise.", true); form.reset(); await loadHistory();
            } catch (e) { message(e.message); }
            finally { sending = false; button.disabled = false; button.textContent = "Enviar para análise"; }
        });
        await loadHistory();
    }
    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "PROFISSIONAL") init(); });
})();
