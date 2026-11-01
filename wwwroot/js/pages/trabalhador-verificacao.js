(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
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
        try {
            const items = await C.api("/verificacoes/me");
            target.innerHTML = items.length ? items.map(v => `<article class="app-row"><div><strong>${safe(v.tipoDocumento)}</strong><div class="app-muted">${safe(statusLabel(v.status))} · enviado ${C.formatarData(v.criadoEm)}</div><p>${safe(v.observacao || "Sem observações")}</p></div><a href="${safe(v.urlDocumento)}" target="_blank" rel="noopener">Abrir meu documento privado</a></article>`).join("") : '<p class="empty-state">Nenhum documento enviado. Se sua atividade exigir comprovação, envie os documentos solicitados pela equipe.</p>';
        } catch (e) { target.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
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
                    const profile = await C.api("/profissionais/me");
                    const complete = Boolean(profile.descricao
                        && profile.servicos?.length
                        && profile.servicos.every(item => item.faixaPrecoMin != null && item.faixaPrecoMax != null)
                        && profile.areas?.length
                        && profile.disponibilidade?.length
                        && Number(profile.distanciaMaxKm) > 0
                        && (profile.atendeDomicilio || profile.atendeEstabelecimento));
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
            event.preventDefault(); if (!form.reportValidity()) return;
            const file = document.getElementById("documentFile").files[0];
            if (!file || file.size > 10 * 1024 * 1024) { message("Selecione um arquivo válido de até 10 MB."); return; }
            const button = document.getElementById("submitVerification"); button.disabled = true; button.textContent = "Enviando…";
            try {
                const data = new FormData(); data.append("arquivo", file); data.append("privado", "true");
                const uploaded = await C.api("/arquivos", { method: "POST", body: data });
                await C.api("/verificacoes", { method: "POST", body: { tipoDocumento: document.getElementById("documentType").value, urlDocumento: uploaded.url, observacao: document.getElementById("documentNote").value.trim() || null } });
                message("Documento enviado para análise.", true); form.reset(); await loadHistory();
            } catch (e) { message(e.message); }
            finally { button.disabled = false; button.textContent = "Enviar para análise"; }
        });
        await loadHistory();
    }
    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "PROFISSIONAL") init(); });
})();
