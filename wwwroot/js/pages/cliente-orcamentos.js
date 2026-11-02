(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const label = value => ({ PENDENTE: 'Aguardando propostas', ORCADO: 'Propostas recebidas', ACEITO: 'Profissional escolhido', AGENDADO: 'Serviço agendado', EM_ANDAMENTO: 'Em andamento', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado', NAO_ESCOLHIDO: 'Não escolhida', RECUSADO: 'Recusada' })[String(value || '').toUpperCase()] || String(value || "").replaceAll("_", " ").toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
    const message = (id, text, ok = false) => { const el = document.getElementById(id); if (el) { el.textContent = text || ""; el.style.color = ok ? "var(--fg-success, var(--color-success))" : "var(--fg-danger, var(--color-danger))"; } };
    const dateTime = value => value ? new Date(value).toLocaleString('pt-BR') : '';
    const initials = name => String(name || '').trim().split(/\s+/).filter(part => /^\p{L}/u.test(part)).slice(0, 2).map(part => part[0].toUpperCase()).join('') || '?';

    // Ícones do Figma (Versão Vitoria)
    const ICON = {
        check: '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        star: '<svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true"><path d="M7 1.75L8.575 5.01667L12.1334 5.54167L9.5667 8.05L10.15 11.6083L7 9.91667L3.85 11.6083L4.43337 8.05L1.8667 5.54167L5.425 5.01667L7 1.75Z" stroke="currentColor" stroke-width="1.28333" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        chat: '<svg width="22" height="22" viewBox="0 0 22 22" fill="none" aria-hidden="true"><path d="M19.25 11C19.2431 12.2486 18.9175 13.4746 18.3042 14.5621C17.6908 15.6496 16.81 16.5625 15.7451 17.2143C14.6802 17.8661 13.4665 18.2352 12.219 18.2867C10.9715 18.3381 9.73159 18.0702 8.61663 17.5084L3.66663 18.3334L4.58329 14.1167C4.17 12.1718 4.54625 10.1424 5.62927 8.47484C6.71229 6.80733 8.41337 5.63835 10.3583 5.22505C12.3032 4.81176 14.3327 5.18801 16.0002 6.27103C17.6677 7.35405 18.8367 9.05513 19.25 11Z" stroke="currentColor" stroke-width="1.83333" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        external: '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M13.3333 6.66663V2.66663H9.33325M13.3333 2.66663L7.33325 8.66663" stroke="currentColor" stroke-width="1.46667" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 9.33333V12.6667C12 13.0349 11.7015 13.3333 11.3334 13.3333H3.33341C2.96522 13.3333 2.66675 13.0349 2.66675 12.6667V4.66667C2.66675 4.29848 2.96522 4 3.33341 4H6.66675" stroke="currentColor" stroke-width="1.46667" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    };

    // Data + horário visíveis (Figma) alimentam o campo usado no envio.
    function setupScheduleFields() {
        const day = document.getElementById('scheduleDay');
        const time = document.getElementById('scheduleTime');
        const target = document.getElementById('scheduleDate');
        if (!day || !time || !target) return;
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        const iso = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, '0')}-${String(tomorrow.getDate()).padStart(2, '0')}`;
        day.min = iso;
        if (!day.value) day.value = iso;
        const sync = () => { target.value = day.value && time.value ? `${day.value}T${time.value}` : ''; };
        day.addEventListener('change', sync);
        time.addEventListener('change', sync);
        sync();
    }

    function showScheduleStatus(text) {
        const box = document.getElementById('scheduleStatus');
        const body = document.getElementById('scheduleStatusText');
        if (!box || !body) return;
        body.innerHTML = text;
        box.hidden = false;
    }

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
            const controls = [content, document.getElementById('scheduleForm'), document.getElementById('reviewForm'), document.getElementById('scheduleStatus')].filter(Boolean).flatMap(root => [...root.querySelectorAll('button')]);
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
        setupScheduleFields();
        try {
            const p = await C.api(`/pedidos/${pedidoId}`);
            const status = String(p.status || "").toUpperCase();
            const heading = document.getElementById('ordersHeading');
            if (heading) heading.textContent = 'Compare propostas e combine o atendimento';
            const subtitle = document.getElementById('ordersSubtitle');
            if (subtitle) subtitle.textContent = 'Escolha o profissional que melhor atende às suas necessidades.';
            const guidance = {
                PENDENTE: 'Os profissionais estão respondendo. Confira as propostas abaixo.',
                ORCADO: 'Confira as propostas recebidas. As ações disponíveis aparecem junto de cada proposta.',
                ACEITO: 'Você escolheu um profissional. Combine a data e o horário do serviço.',
                AGENDADO: 'Confira o agendamento. O profissional iniciará o atendimento.',
                EM_ANDAMENTO: 'O profissional está realizando o serviço. A avaliação fica disponível após a conclusão.',
                CONCLUIDO: p.avaliacao ? 'Obrigado! Sua avaliação foi registrada.' : 'Como foi o atendimento? Avalie o profissional ao lado.',
                CANCELADO: 'Este pedido foi encerrado. Você pode consultar o histórico ou pedir um novo serviço.'
            };
            function updateNextStep(text) {
                const el = document.getElementById('orderNextStep');
                if (el) el.textContent = text;
            }
            const chosen = p.destinatarios?.find(item => Number(item.profissionalId) === Number(p.profissionalId));
            const local = [p.cidade, p.bairro, p.endereco].filter(Boolean).join(" · ");

            content.innerHTML = `<a class="fg-back" href="orcamentos.html">← Meus pedidos</a>`
                + `<section class="fg-card"><div class="fg-pedido-meta"><span class="fg-tag fg-tag-warning">${safe(label(p.status))}</span><span class="fg-small">Pedido feito em ${C.formatarData(p.criadoEm)}</span></div>`
                + `<h2 class="fg-title fg-title-lg">${safe(p.titulo)}</h2>`
                + `<p class="fg-text">${safe([p.categoria, local].filter(Boolean).join(" · "))}${p.dataDesejada ? ` · Data desejada: ${dateTime(p.dataDesejada)}` : ""}</p>`
                + (p.descricao ? `<p class="fg-proposta-descricao">${safe(p.descricao)}</p>` : "")
                + (p.fotoUrls?.length ? `<div class="fg-fotos">${p.fotoUrls.map(url => `<img src="${safe(url)}" alt="Foto do chamado" loading="lazy">`).join("")}</div>` : "")
                + `<p class="fg-note" id="orderNextStep">${safe(guidance[status] || "Acompanhe as informações deste pedido.")}</p>`
                + `<div class="fg-pedido-acoes" id="orderActions"></div><p id="orderMessage" class="app-message"></p></section>`
                + `<section class="fg-stack" id="quotesSection" aria-live="polite"><p class="empty-state">Carregando propostas…</p></section>`;

            if (p.profissionalId) content.insertAdjacentHTML('beforeend', `<section class="fg-card"><div class="fg-row" style="padding:0;border:none"><span class="fg-avatar fg-avatar-md">${safe(initials(chosen?.nome || 'P'))}</span><div class="fg-row-info"><span>Profissional escolhido</span><strong>${safe(chosen?.nome || 'Profissional do seu pedido')}</strong></div><a class="fg-btn fg-btn-sm" href="perfil-profissional.html?id=${p.profissionalId}">Ver perfil</a></div></section>`);
            if (p.avaliacao) content.insertAdjacentHTML('beforeend', `<section class="fg-card"><h2 class="fg-title">Sua avaliação</h2><p class="fg-proposta-avaliacao">${ICON.star} Nota: ${safe(String(p.avaliacao.nota))} / 5</p><p class="fg-text">${safe(p.avaliacao.comentario || 'Sem comentário.')}</p></section>`);
            if (status === 'CANCELADO' && p.motivoCancelamento) content.insertAdjacentHTML('beforeend', `<section class="fg-card"><p class="fg-text">Motivo do cancelamento: ${safe(p.motivoCancelamento)}</p><a class="fg-btn fg-btn-primary" href="criar-chamado.html">Pedir um serviço</a></section>`);

            function renderQuote(q, highlight) {
                const prof = q.profissional || {};
                const open = status === "PENDENTE" && q.situacao === "ENVIADO" && !q.expirado;
                const tag = q.situacao === "ACEITO"
                    ? `<span class="fg-tag fg-tag-success">${ICON.check} Proposta aceita</span>`
                    : q.expirado ? '<span class="fg-tag">Expirada</span>'
                    : q.situacao !== "ENVIADO" ? `<span class="fg-tag">${safe(label(q.situacao))}</span>` : "";
                // Como no Figma: a proposta em destaque pode ser aceita; as outras mostram "Ver proposta".
                const actions = open
                    ? (highlight
                        ? `<button class="fg-btn fg-btn-primary" data-accept-quote="${q.id}">Aceitar proposta</button><button class="fg-link" data-reject-quote="${q.id}">Recusar</button>`
                        : `<button class="fg-btn" data-view-quote="${q.id}">Ver proposta</button>`)
                    : tag;
                return `<article class="fg-proposta ${highlight ? "fg-destaque" : ""}">`
                    + `<span class="fg-avatar fg-avatar-md">${safe(initials(prof.nome))}</span>`
                    + `<div class="fg-proposta-info"><div class="fg-proposta-nome"><a href="perfil-profissional.html?id=${prof.id}">${safe(prof.nome)}</a>${prof.verificado ? `<span class="fg-tag fg-tag-success">${ICON.check} Verificado</span>` : ""}</div>`
                    + `<div class="fg-proposta-avaliacao">${ICON.star}<span>${C.formatarNota(prof.notaMedia)} · ${safe(p.categoria || "Serviço")} · Prazo: ${safe(q.prazoEstimado)}</span></div>`
                    + `<p class="fg-proposta-descricao">${safe(q.condicoes || "Sem observações adicionais.")}</p></div>`
                    + `<div class="fg-proposta-valor"><strong>${C.formatarMoeda(q.valor)}</strong><div class="fg-proposta-acoes">${actions}</div></div>`
                    + `</article>`;
            }

            async function carregarPropostas() {
                const target = document.getElementById('quotesSection');
                target.setAttribute('aria-busy', 'true');
                target.innerHTML = '<p class="empty-state" role="status">Carregando propostas…</p>';
                try {
                    const quotes = await C.api(`/pedidos/${pedidoId}/orcamentos`);
                    const available = quotes.filter(quote => quote.situacao === 'ENVIADO' && !quote.expirado);
                    if (status === 'PENDENTE') updateNextStep(available.length ? 'Você recebeu ' + available.length + (available.length === 1 ? ' proposta. Confira os detalhes e escolha um profissional.' : ' propostas. Compare os detalhes e escolha um profissional.') : guidance[status]);
                    const sorted = [...quotes].sort((a, b) => (Number(b.profissional?.notaMedia) || 0) - (Number(a.profissional?.notaMedia) || 0));
                    let highlightId = (sorted.find(q => q.situacao === 'ACEITO') || sorted.find(q => q.situacao === 'ENVIADO' && !q.expirado))?.id;
                    const count = quotes.length === 1 ? '1 proposta' : `${quotes.length} propostas`;
                    const draw = () => {
                        target.innerHTML = `<div class="fg-card-header"><h2 class="fg-title">${count} para “${safe(p.titulo)}”</h2><span class="fg-small">Ordenado por avaliação</span></div>`
                            + (quotes.length ? sorted.map(q => renderQuote(q, q.id === highlightId)).join("") : '<p class="empty-state">Nenhum orçamento está disponível para este pedido no momento.</p>');
                        target.querySelectorAll("[data-view-quote]").forEach(btn => btn.addEventListener("click", () => {
                            if (actionPending) return;
                            highlightId = Number(btn.dataset.viewQuote);
                            draw();
                        }));
                        target.querySelectorAll("[data-accept-quote]").forEach(btn => btn.addEventListener("click", async () => {
                            if (actionPending) return;
                            if (!confirm("Aceitar este orçamento? As demais propostas serão encerradas.")) return;
                            return mutate("orderMessage", () => C.api(`/orcamentos/${btn.dataset.acceptQuote}/aceitar`, { method: "POST" }));
                        }));
                        target.querySelectorAll("[data-reject-quote]").forEach(btn => btn.addEventListener("click", async () => {
                            return mutate("orderMessage", () => C.api(`/orcamentos/${btn.dataset.rejectQuote}/recusar`, { method: "POST" }));
                        }));
                    };
                    draw();

                } catch (error) {
                    target.innerHTML = `<div class="fg-card empty-state" role="alert"><h2 class="fg-title">Não foi possível carregar as propostas</h2><p class="fg-text">${safe(error.message)}</p><button class="fg-btn" type="button" id="retryQuotes">Tentar novamente</button></div>`;
                    target.querySelector('#retryQuotes').addEventListener('click', carregarPropostas);
                } finally { target.setAttribute('aria-busy', 'false'); }
            }
            if (status === 'CANCELADO') {
                document.getElementById('quotesSection').innerHTML = '<p class="empty-state">Este pedido foi cancelado. Não há propostas disponíveis para escolha.</p>';
            } else {
                carregarPropostas();
            }

            // WhatsApp (card do Figma): liberado depois da contratação
            if (status !== 'CANCELADO') {
                content.insertAdjacentHTML('beforeend', `<section class="fg-card fg-whatsapp"><span class="fg-whatsapp-icone">${ICON.chat}</span><div class="fg-whatsapp-texto"><strong>Converse diretamente pelo WhatsApp</strong><span class="fg-text" id="whatsappHint">Após a contratação, abra uma conversa para combinar os detalhes.</span></div><span id="whatsappAction"><button class="fg-btn" type="button" disabled>${ICON.external} Abrir WhatsApp</button></span></section>`);
            }

            const actions = document.getElementById("orderActions");
            content.insertAdjacentHTML("beforeend", '<section class="fg-card"><h2 class="fg-title">Histórico do chamado</h2><div id="orderHistory"><p class="empty-state">Carregando…</p></div></section>');
            try {
                const history = await C.api(`/pedidos/${pedidoId}/historico`);
                document.getElementById("orderHistory").innerHTML = history.length ? history.map(item => `<div class="fg-row"><div class="fg-row-info"><strong>${safe(label(item.statusNovo))}</strong><span>${safe(item.detalhe || "Atualização do chamado")}</span></div><time class="fg-small">${C.formatarData(item.criadoEm)}</time></div>`).join("") : '<p class="empty-state">Ainda não há eventos registrados.</p>';
            } catch (e) { document.getElementById("orderHistory").innerHTML = `<p class="empty-state">${safe(e.message)}</p>`; }
            if (["PENDENTE", "ACEITO", "AGENDADO"].includes(status)) actions.innerHTML += '<button class="fg-btn fg-btn-sm" id="cancelOrder">Cancelar serviço</button>';
            if (!["PENDENTE", "ORCADO", "CANCELADO"].includes(status)) {
                const hint = document.getElementById('whatsappHint');
                const slot = document.getElementById('whatsappAction');
                try {
                    const contact = await C.api(`/whatsapp/pedidos/${pedidoId}/contato`);
                    const number = String(contact.telefone || "").replace(/\D/g, "");
                    const text = encodeURIComponent(contact.mensagemInicialWhatsApp || `Olá! Estou entrando em contato pelo CHAMAPRO sobre o chamado “${p.titulo}”.`);
                    const link = `<a class="fg-btn" target="_blank" rel="noopener" href="https://wa.me/${number}?text=${text}">${ICON.external} Abrir WhatsApp</a>`;
                    if (slot) slot.innerHTML = link; else actions.insertAdjacentHTML("beforeend", link);
                } catch {
                    // Contato desativado ou ainda indisponível.
                    if (hint) hint.textContent = 'O profissional ainda não disponibilizou o WhatsApp para este pedido.';
                }
            }

            const scheduleBox = document.getElementById("scheduleBox");
            const reviewBox = document.getElementById('reviewBox');
            if (status === "ACEITO" && !p.agendamento && scheduleBox) scheduleBox.hidden = false;
            if (status === "CONCLUIDO" && !p.avaliacao && reviewBox) {
                reviewBox.hidden = false;
                if (location.hash === '#avaliar') reviewBox.scrollIntoView({ block: 'start' });
            }
            const appointmentOpen = p.agendamento && !['CANCELADO', 'CONCLUIDO'].includes(status) && ['AGENDADO', 'CONFIRMADO'].includes(String(p.agendamento.situacao).toUpperCase());

            // Card "Agendamento" da coluna lateral quando o formulário não está aberto
            if (p.agendamento) {
                showScheduleStatus(`<span class="fg-note fg-ok">${ICON.check} Agendado para ${safe(dateTime(p.agendamento.dataHora))}</span><span class="fg-small">Situação: ${safe(label(p.agendamento.situacao))}</span>${appointmentOpen ? '<button class="fg-btn fg-btn-primary fg-btn-block" id="reschedule">Remarcar</button><button class="fg-btn fg-btn-block" id="cancelAppointment">Cancelar agendamento</button>' : ""}`);
                if (!document.getElementById('scheduleStatus')) actions.insertAdjacentHTML("beforeend", `<span class="fg-small">Agendado para ${dateTime(p.agendamento.dataHora)} · ${safe(label(p.agendamento.situacao))}</span>${appointmentOpen ? '<button class="fg-btn fg-btn-sm" id="reschedule">Remarcar</button><button class="fg-btn fg-btn-sm" id="cancelAppointment">Cancelar agendamento</button>' : ""}`);
            } else if (scheduleBox?.hidden && reviewBox?.hidden) {
                showScheduleStatus(status === 'PENDENTE' || status === 'ORCADO'
                    ? 'Aceite uma proposta para escolher a data e o horário do atendimento.'
                    : 'Não há agendamento para este pedido.');
            }

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
            document.getElementById("reschedule")?.addEventListener("click", () => {
                if(actionPending)return;
                scheduleBox.hidden = false;
                const statusBox = document.getElementById('scheduleStatus');
                if (statusBox) statusBox.hidden = true;
                document.getElementById("scheduleForm").dataset.agendamentoId = p.agendamento.id;
            });
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
