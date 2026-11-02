(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const label = value => ({ PENDENTE: 'Aguardando propostas', ORCADO: 'Propostas recebidas', ACEITO: 'Profissional escolhido', AGENDADO: 'Serviço agendado', EM_ANDAMENTO: 'Em andamento', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado' })[String(value || '').toUpperCase()] || String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const message = (id, text, ok = false) => { const el = document.getElementById(id); if (el) { el.textContent = text || ""; el.style.color = ok ? "var(--color-success)" : "var(--color-danger)"; } };

    const dateTime = value => value ? new Date(value).toLocaleString('pt-BR') : '';
    async function init() {
        const query = new URLSearchParams(location.search);
        let pedidoId = query.get("pedidoId");
        const content = document.getElementById("orderContent");
        if (!pedidoId) {
            await window.ChamaproOrdersList?.init();
            return;
        }

        if (!/^\d+$/.test(pedidoId) || Number(pedidoId) < 1) { content.innerHTML = '<p role="alert">Pedido não informado. Volte a Meus pedidos e escolha um serviço.</p>'; return; }
        let actionPending = false;
        async function mutate(messageId, action) {
            if (actionPending) return;
            actionPending = true;
            const controls = [content, document.getElementById('scheduleForm'), document.getElementById('reviewForm')].filter(Boolean).flatMap(root => [...root.querySelectorAll('button')]);
            const previous = controls.map(button => button.disabled);
            controls.forEach(button => button.disabled = true);
            for (const id of ['scheduleForm','reviewForm']) { const form=document.getElementById(id); if(form)form.inert=true; }
            message(messageId, 'Enviando…');
            try { await action(); message(messageId, 'Alteração salva. Atualizando o pedido…', true); location.reload(); }
            catch (error) {
                message(messageId, error.message || 'Não foi possível salvar. Seus dados continuam aqui. Tente novamente.');
                actionPending = false;
                controls.forEach((button,i) => button.disabled = previous[i]);
                for (const id of ['scheduleForm','reviewForm']) { const form=document.getElementById(id); if(form)form.inert=false; }
            }
        }
        try {
            const p = await C.api(`/pedidos/${pedidoId}`);
            const status = String(p.status || "").toUpperCase();
            const heading = document.getElementById('ordersHeading');
            if (heading) heading.textContent = 'Detalhe do pedido';
            const guidance = {
                PENDENTE: 'Os profissionais estão respondendo. Confira as propostas abaixo.',
                ORCADO: 'Confira as propostas recebidas. As ações disponíveis aparecem junto de cada proposta.',
                ACEITO: 'Você escolheu um profissional. Combine a data e o horário do serviço.',
                AGENDADO: 'Confira o agendamento. O profissional iniciará o atendimento.',
                EM_ANDAMENTO: 'O profissional está realizando o serviço. A avaliação fica disponível após a conclusão.',
                CONCLUIDO: p.avaliacao ? 'Obrigado! Sua avaliação foi registrada.' : 'Como foi o atendimento? Avalie o profissional abaixo.',
                CANCELADO: 'Este pedido foi encerrado. Você pode consultar o histórico ou pedir um novo serviço.'
            };
            function updateNextStep(text) {
                const el = document.getElementById('orderNextStep');
                if (el) el.textContent = text;
            }
            const chosen = p.destinatarios?.find(item => Number(item.profissionalId) === Number(p.profissionalId));
            content.innerHTML = `<a class="back-link" href="orcamentos.html">← Meus pedidos</a><section class="app-card"><h2>O que acontece agora</h2><p id="orderNextStep">${safe(guidance[status] || "Acompanhe as informações deste pedido.")}</p></section><article class="app-card"><span class="badge badge-info">${safe(label(p.status))}</span><h2 style="margin:12px 0">${safe(p.titulo)}</h2><p>${safe(p.categoria)} · ${safe([p.cidade,p.bairro,p.endereco].filter(Boolean).join(" · "))}</p><p style="margin-top:12px">${safe(p.descricao)}</p><p class="app-muted">Pedido feito em ${C.formatarData(p.criadoEm)}</p>${p.dataDesejada ? `<p class="app-muted">Data desejada: ${dateTime(p.dataDesejada)}</p>` : ""}${p.fotoUrls?.length ? `<div class="app-grid-two" style="margin-top:16px">${p.fotoUrls.map(url => `<img src="${safe(url)}" alt="Foto do chamado" loading="lazy">`).join("")}</div>` : ""}<div class="app-actions" id="orderActions"></div><p id="orderMessage" class="app-message"></p></article><section class="app-stack" id="quotesSection" aria-live="polite"><p class="empty-state">Carregando propostas…</p></section>`;
            if (p.profissionalId) content.insertAdjacentHTML('beforeend', `<section class="app-card"><h2>Profissional escolhido</h2><p>${safe(chosen?.nome || 'Profissional do seu pedido')}</p><a class="btn btn-secondary" href="perfil-profissional.html?id=${p.profissionalId}">Ver perfil</a></section>`);
            if (p.avaliacao) content.insertAdjacentHTML('beforeend', `<section class="app-card"><h2>Sua avaliação</h2><p>Nota: ${safe(String(p.avaliacao.nota))} / 5</p><p>${safe(p.avaliacao.comentario || 'Sem comentário.')}</p></section>`);
            if (status === 'CANCELADO' && p.motivoCancelamento) content.insertAdjacentHTML('beforeend', `<p class="app-muted">Motivo: ${safe(p.motivoCancelamento)}</p><a class="btn btn-primary" href="criar-chamado.html">Pedir um serviço</a>`);
            async function carregarPropostas() {
                const target = document.getElementById('quotesSection');
                target.setAttribute('aria-busy', 'true');
                target.innerHTML = '<p class="empty-state" role="status">Carregando propostas…</p>';
                try {
                    const quotes = await C.api(`/pedidos/${pedidoId}/orcamentos`);
                    const available = quotes.filter(quote => quote.situacao === 'ENVIADO' && !quote.expirado);
                    if (status === 'PENDENTE') updateNextStep(available.length ? 'Você recebeu ' + available.length + (available.length === 1 ? ' proposta. Confira os detalhes e escolha um profissional.' : ' propostas. Compare os detalhes e escolha um profissional.') : guidance[status]);
                    target.innerHTML = `<section class="app-stack"><h2>Propostas recebidas (${quotes.length})</h2>${quotes.length ? quotes.map(q => `<article class="app-card"><div class="app-row"><div><strong>${safe(q.profissional.nome)}</strong><div class="app-muted">Nota ${C.formatarNota(q.profissional.notaMedia)} ${q.profissional.verificado ? "· perfil verificado" : ""} · ${safe(q.prazoEstimado)}</div><p class="app-muted">${safe(q.condicoes || "Sem observações adicionais")}</p><span class="badge ${q.situacao === "ACEITO" ? "badge-success" : "badge-info"}">${safe(label(q.situacao))}${q.expirado ? " · expirado" : ""}</span></div><div><strong>${C.formatarMoeda(q.valor)}</strong><div class="app-actions">${status === "PENDENTE" && q.situacao === "ENVIADO" && !q.expirado ? `<button class="btn btn-primary btn-small" data-accept-quote="${q.id}">Aceitar</button><button class="btn btn-secondary btn-small" data-reject-quote="${q.id}">Recusar</button>` : ""}<a class="btn btn-secondary btn-small" href="perfil-profissional.html?id=${q.profissional.id}">Ver perfil</a></div></div></div></article>`).join("") : '<p class="empty-state">Nenhum orçamento está disponível para este pedido no momento.</p>'}</section>`;
                    target.querySelectorAll("[data-accept-quote]").forEach(btn => btn.addEventListener("click", async () => {
                        if (actionPending) return;
                        if (!confirm("Aceitar este orçamento? As demais propostas serão encerradas.")) return;
                        return mutate("orderMessage", () => C.api(`/orcamentos/${btn.dataset.acceptQuote}/aceitar`, { method: "POST" }));
                    }));
                    target.querySelectorAll("[data-reject-quote]").forEach(btn => btn.addEventListener("click", async () => {
                        return mutate("orderMessage", () => C.api(`/orcamentos/${btn.dataset.rejectQuote}/recusar`, { method: "POST" }));
                    }));

                } catch (error) {
                    target.innerHTML = `<div class="empty-state" role="alert"><h2>Não foi possível carregar as propostas</h2><p>${safe(error.message)}</p><button class="btn btn-secondary" type="button" id="retryQuotes">Tentar novamente</button></div>`;
                    target.querySelector('#retryQuotes').addEventListener('click', carregarPropostas);
                } finally { target.setAttribute('aria-busy', 'false'); }
            }
            if (status === 'CANCELADO') {
                document.getElementById('quotesSection').innerHTML = '<p class="empty-state">Este pedido foi cancelado. Não há propostas disponíveis para escolha.</p>';
            } else {
                carregarPropostas();
            }
            const actions = document.getElementById("orderActions");
            content.insertAdjacentHTML("beforeend", '<section class="app-card"><h2>Histórico do chamado</h2><div id="orderHistory" class="app-stack"><p class="empty-state">Carregando…</p></div></section>');
            try {
                const history = await C.api(`/pedidos/${pedidoId}/historico`);
                document.getElementById("orderHistory").innerHTML = history.length ? history.map(item => `<div class="app-row"><div><strong>${safe(label(item.statusNovo))}</strong><div class="app-muted">${safe(item.detalhe || "Atualização do chamado")}</div></div><time class="app-muted">${C.formatarData(item.criadoEm)}</time></div>`).join("") : '<p class="empty-state">Ainda não há eventos registrados.</p>';
            } catch (e) { document.getElementById("orderHistory").innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
            if (["PENDENTE", "ACEITO", "AGENDADO"].includes(status)) actions.innerHTML += '<button class="btn btn-secondary" id="cancelOrder">Cancelar serviço</button>';
            if (!["PENDENTE", "ORCADO", "CANCELADO"].includes(status)) {
                try {
                    const contact = await C.api(`/whatsapp/pedidos/${pedidoId}/contato`);
                    const number = String(contact.telefone || "").replace(/\D/g, "");
                    const text = encodeURIComponent(contact.mensagemInicialWhatsApp || `Olá! Estou entrando em contato pelo CHAMAPRO sobre o chamado “${p.titulo}”.`);
                    actions.insertAdjacentHTML("beforeend", `<a class="btn btn-outline" target="_blank" rel="noopener" href="https://wa.me/${number}?text=${text}">Conversar pelo WhatsApp</a>`);
                } catch { /* Contato desativado ou ainda indisponível. */ }
            }
            if (status === "ACEITO" && !p.agendamento) document.getElementById("scheduleBox").hidden = false;
            if (status === "CONCLUIDO" && !p.avaliacao) {
                const review = document.getElementById('reviewBox');
                review.hidden = false;
                if (location.hash === '#avaliar') review.scrollIntoView({ block: 'start' });
            }
            const appointmentOpen = p.agendamento && !['CANCELADO', 'CONCLUIDO'].includes(status) && ['AGENDADO', 'CONFIRMADO'].includes(String(p.agendamento.situacao).toUpperCase());
            if (p.agendamento) actions.insertAdjacentHTML("beforeend", `<span class="app-muted">Agendado para ${dateTime(p.agendamento.dataHora)} · ${safe(label(p.agendamento.situacao))}</span>${appointmentOpen ? '<button class="btn btn-secondary btn-small" id="reschedule">Remarcar</button><button class="btn btn-secondary btn-small" id="cancelAppointment">Cancelar agendamento</button>' : ""}`);

            document.getElementById("cancelOrder")?.addEventListener("click", async () => {
                if (actionPending) return;
                const motivo = prompt("Informe o motivo do cancelamento:")?.trim(); if (!motivo) return;
                if(motivo.length<5||motivo.length>200){message('orderMessage','Explique o motivo com 5 a 200 caracteres.');return;}
                return mutate('orderMessage',()=>C.api(`/pedidos/${pedidoId}/cancelar`,{method:'POST',body:{motivo}}));
            });
            document.getElementById('cancelAppointment')?.addEventListener('click', async () => {
                if(actionPending||!confirm('Cancelar este agendamento também cancela o pedido. Deseja continuar?'))return;
                return mutate('orderMessage',()=>C.api(`/agendamentos/${p.agendamento.id}/status`,{method:'PATCH',body:{status:'Cancelado'}}));
            });
            document.getElementById("reschedule")?.addEventListener("click", () => { if(actionPending)return;document.getElementById("scheduleBox").hidden = false; document.getElementById("scheduleForm").dataset.agendamentoId = p.agendamento.id; });
            document.getElementById("scheduleForm").addEventListener("submit", async event => {
                event.preventDefault();if(actionPending)return;
                const appointmentId = event.currentTarget.dataset.agendamentoId;
                if(appointmentId?!appointmentOpen:status!=='ACEITO'||p.agendamento){message('scheduleMessage','Este pedido não permite esse agendamento. Atualize os detalhes.');return;}
                const date = new Date(document.getElementById("scheduleDate").value);
                if(!Number.isFinite(date.getTime())||date.getTime()<=Date.now()){message('scheduleMessage','Escolha uma data e horário futuros. O que você preencheu foi mantido.');return;}
                return mutate('scheduleMessage',()=>appointmentId?C.api(`/agendamentos/${appointmentId}`,{method:'PUT',body:{dataHora:date.toISOString()}}):C.api('/agendamentos',{method:'POST',body:{pedidoId:Number(pedidoId),dataHora:date.toISOString()}}));
            });
            document.getElementById("reviewForm").addEventListener("submit", async event => {
                event.preventDefault();if(actionPending)return;
                if(status!=='CONCLUIDO'||p.avaliacao){message('reviewMessage','A avaliação fica disponível depois que o serviço é concluído e só pode ser enviada uma vez.');return;}
                const nota=Number(document.getElementById('reviewRating').value),comentario=document.getElementById('reviewComment').value.trim()||null;
                if(!Number.isInteger(nota)||nota<1||nota>5||(comentario?.length||0)>1000){message('reviewMessage','Escolha uma nota de 1 a 5 e escreva até 1.000 caracteres.');return;}
                return mutate('reviewMessage',()=>C.api('/avaliacoes',{method:'POST',body:{pedidoId:Number(pedidoId),nota,comentario}}));
            });
        } catch (e) { content.innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
    }

    window.ChamaproApp?.ready.then(user => { if (user?.perfil === "CLIENTE") init(); });
})();
