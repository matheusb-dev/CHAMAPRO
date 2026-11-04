(() => {
    const C = window.Chamapro, safe = C.escaparHtml;
    const norm = value => String(value || '').replaceAll('_', '').toUpperCase();
    const labels = { PENDENTE: 'Disponível', ORCADO: 'Orçado', ACEITO: 'Escolhido · combinar atendimento', AGENDADO: 'Agendado', EMANDAMENTO: 'Em andamento', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado', RECUSADO: 'Recusada', NAOESCOLHIDO: 'Não escolhida', ENVIADO: 'Aguardando resposta', CONFIRMADO: 'Confirmado' };
    const label = value => labels[norm(value)] || String(value || 'Não informado');
    const detailUrl = id => `detalhes-chamado.html?id=${encodeURIComponent(id)}`;
    const date = value => value ? new Date(value).toLocaleString('pt-BR') : 'A combinar';
    const money = value => Number(value).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    function mine(p, profile) { return profile?.id != null && p.profissionalId === profile.id; }
    function quoteState(p, now = Date.now()) {
        const q = p.meuOrcamento;
        if (!q) return null;
        if (norm(p.status) === 'CANCELADO') return 'Pedido cancelado';
        if (norm(q.situacao) === 'ENVIADO' && new Date(q.validoAte).getTime() <= now) return 'Validade encerrada';
        if (norm(q.situacao) === 'PENDENTE') return 'Proposta pendente';
        return norm(q.situacao) === 'ACEITO' ? 'Aceita' : label(q.situacao);
    }
    function allowed(p, profile) {
        const status = norm(p.status), own = mine(p, profile);
        const recipient = p.destinatarios?.find(d => d.profissionalId === profile?.id);
        return {
            interest: status === 'PENDENTE' && norm(recipient?.situacao) === 'PENDENTE',
            quote: status === 'PENDENTE' && norm(recipient?.situacao) === 'ACEITO',
            schedule: false,
            start: own && ['ACEITO', 'AGENDADO'].includes(status), complete: own && status === 'EMANDAMENTO',
            cancel: own && ['ACEITO', 'AGENDADO'].includes(status),
            appointment: false
        };
    }
    function quotePayload(value, deadline, conditions) {
        if (!/^\d+(?:[.,]\d{1,2})?$/.test(String(value).trim())) throw Error('Informe um valor com até duas casas decimais.');
        const valor = Number(String(value).replace(',', '.'));
        if (!(valor >= 0.01 && valor <= 99999999.99)) throw Error('Informe um valor entre R$ 0,01 e R$ 99.999.999,99.');
        const prazoEstimado = deadline.trim(), condicoes = conditions.trim() || null;
        if (!prazoEstimado || prazoEstimado.length > 100) throw Error('Informe o prazo estimado, com até 100 caracteres.');
        if (condicoes?.length > 500) throw Error('Use até 500 caracteres na mensagem.');
        return { valor, prazoEstimado, condicoes };
    }
    const tabs = '<nav class="app-actions" aria-label="Atividades profissionais"><a class="btn btn-secondary" href="fila-chamados.html#oportunidades">Encontrar serviços</a><a class="btn btn-secondary" href="fila-chamados.html#propostas">Minhas propostas</a><a class="btn btn-secondary" href="fila-chamados.html#trabalhos">Meus trabalhos</a></nav>';
    function error(target, retry, detail) {
        target.innerHTML = `<section class="app-card" role="alert"><p>Não foi possível carregar suas informações. Tente novamente.</p>${detail ? `<p>${safe(detail)}</p>` : ''}<button type="button" id="retryWork" class="btn btn-secondary">Tentar novamente</button></section>`;
        target.querySelector('#retryWork').addEventListener('click', retry);
    }
    async function page(api, status, number) {
        const query = new URLSearchParams({ pagina: String(number), tamanho: '10' });
        if (status) query.set('status', status);
        const result = await api(`/pedidos?${query}`);
        // No failed detail may disappear as an empty opportunity/proposal/work list.
        const details = await Promise.all((result.itens || []).map(p => api(`/pedidos/${p.id}`)));
        return { items: details, total: result.total };
    }
    // Metáfora do prego: cada oportunidade é um prego. Dois cliques no martelo
    // (duas marteladas) cravam o prego e abrem os detalhes do pedido.
    function hammerButton(p) {
        const titulo = safe(p.titulo);
        return `<a class="hammer-cta" href="${detailUrl(p.id)}" data-hammer data-hits="0" title="Dê dois cliques para ver os detalhes" aria-label="Ver detalhes do pedido: ${titulo}. Dê dois cliques."><svg class="hammer-svg" viewBox="0 0 120 90" aria-hidden="true" focusable="false"><g class="hammer-nail"><rect class="nail-shaft" x="96" y="46" width="4" height="22"/><rect class="nail-head" x="92" y="43" width="12" height="4" rx="2"/></g><rect class="hammer-wood" x="70" y="60" width="48" height="24" rx="4"/><rect class="hammer-wood-top" x="70" y="60" width="48" height="4" rx="2"/><path class="hammer-grain" d="M77 72h14M96 77h14M108 69h6"/><circle class="hammer-spark" cx="100" cy="45" r="7"/><g class="hammer-tool"><rect class="hammer-handle" x="36" y="28" width="8" height="58" rx="4"/><rect class="hammer-grip" x="35" y="60" width="10" height="26" rx="5"/><path class="hammer-claw" d="M16 10C6 10 2 18 1 29L9 26C10 21 13 20 16 21Z"/><rect class="hammer-head" x="14" y="10" width="52" height="20" rx="3"/><rect class="hammer-shine" x="17" y="12.5" width="44" height="4" rx="2"/><rect class="hammer-face" x="58" y="10" width="8" height="20" rx="2"/></g></svg><span class="hammer-hint" aria-hidden="true"><span class="hint-0">Dois cliques para abrir</span><span class="hint-1">Mais um clique!</span><span class="hint-2">Abrindo…</span></span><span class="hammer-live" role="status"></span></a>`;
    }
    document.addEventListener('click', event => {
        const link = event.target.closest?.('a[data-hammer]');
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        if (link.classList.contains('is-opening')) return;
        const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const hits = Math.min(2, Number(link.dataset.hits || 0) + 1);
        link.dataset.hits = hits;
        link.classList.remove('hit-a', 'hit-b');
        void link.offsetWidth; // reinicia a animação a cada martelada
        link.classList.add(hits === 1 ? 'hit-a' : 'hit-b');
        const live = link.querySelector('.hammer-live');
        if (hits < 2) {
            if (live) live.textContent = 'Clique mais uma vez para abrir os detalhes.';
            return;
        }
        link.classList.add('is-opening');
        if (live) live.textContent = 'Abrindo detalhes…';
        setTimeout(() => { location.href = link.href; }, reduceMotion ? 0 : 600);
    });
    function card(p, mode, profile) {
        const q = p.meuOrcamento, own = mine(p, profile);
        const situation = mode === 'propostas' ? quoteState(p) : mode === 'oportunidades' ? q ? 'Sua proposta: ' + quoteState(p) : norm(p.destinatarios?.find(d => d.profissionalId === profile.id)?.situacao) === 'ACEITO' ? 'Prepare sua proposta' : 'Disponível' : label(p.status);
        return `<article class="app-card"><span class="badge badge-info">${safe(situation)}</span><h2>${safe(p.titulo)}</h2><p>${safe(p.categoria)} · ${safe([p.cidade, p.bairro].filter(Boolean).join(' · '))}</p><p>${p.agendamento ? 'Registro histórico: ' + safe(date(p.agendamento.dataHora)) : norm(p.tipoAtendimento) === 'IMEDIATO' ? 'Solicitação urgente · combine os detalhes com o cliente' : 'Data histórica: ' + safe(date(p.dataDesejada))}</p>${mode === 'propostas' ? `<p>${money(q.valor)} · Prazo: ${safe(q.prazoEstimado)}</p><p class="app-muted">Válida até ${safe(date(q.validoAte))}. Data de envio indisponível.</p>` : `<p>${safe(String(p.descricao || '').slice(0, 180))}</p>`}${mode === 'propostas' && own ? `<a class="btn btn-primary" href="${detailUrl(p.id)}">Ver trabalho</a>` : mode === 'trabalhos' ? `<a class="btn btn-primary" href="${detailUrl(p.id)}">Acompanhar trabalho</a>` : hammerButton(p)}</article>`;
    }
    async function list(root) {
        let number = 1, sequence = 0, profile, loaded = [], total = 0;
        const mode = () => ['propostas', 'trabalhos'].includes(location.hash.slice(1)) ? location.hash.slice(1) : 'oportunidades';
        function structure() {
            const area = mode(); number = 1;
            root.innerHTML = `${tabs}<h1>${area === 'propostas' ? 'Minhas propostas' : area === 'trabalhos' ? 'Meus trabalhos' : 'Encontrar serviços'}</h1><p>${area === 'oportunidades' ? 'Pedidos direcionados a você. Demonstre interesse para enviar uma proposta.' : area === 'propostas' ? 'Acompanhe a última proposta de cada pedido recebido.' : 'Veja somente os pedidos em que você foi o profissional escolhido.'}</p><div class="app-toolbar"><label>Situação do pedido<select id="workStatus">${(area === 'oportunidades' ? [['PENDENTE', 'Disponíveis']] : area === 'trabalhos' ? [['ACEITO', 'Próximos · combinar data'], ['AGENDADO', 'Próximos · agendados'], ['EM_ANDAMENTO', 'Em andamento'], ['CONCLUIDO', 'Concluídos'], ['CANCELADO', 'Cancelados']] : [['', 'Todos'], ['PENDENTE', 'Aguardando escolha'], ['ACEITO', 'Com proposta escolhida'], ['AGENDADO', 'Agendados'], ['EM_ANDAMENTO', 'Em andamento'], ['CONCLUIDO', 'Concluídos'], ['CANCELADO', 'Cancelados']]).map(([v, t]) => `<option value="${v}">${t}</option>`).join('')}</select></label><label>Buscar nesta página<input id="workSearch" type="search" placeholder="Serviço, categoria, cidade ou bairro"></label>${area === 'propostas' ? '<label>Proposta nesta página<select id="proposalFilter"><option value="">Todas</option><option>Aguardando resposta</option><option>Aceita</option><option>Recusada</option><option>Não escolhida</option><option>Validade encerrada</option><option>Pedido cancelado</option></select></label>' : ''}<button type="button" id="refreshWork" class="btn btn-secondary">Atualizar</button></div><p class="form-hint">A paginação percorre os pedidos recebidos, 10 por página. Busca e seleção de propostas se aplicam apenas à página atual; continue nas próximas páginas para consultar os demais.</p><div id="workResults" class="app-stack" aria-live="polite"></div><div class="app-actions"><button type="button" id="previousWork" class="btn btn-secondary">Anterior</button><span id="workPage" role="status"></span><button type="button" id="nextWork" class="btn btn-secondary">Próxima</button></div>`;
            const initialStatus = new URLSearchParams(location.search).get('status');
            if (area === 'trabalhos' && ['ACEITO', 'AGENDADO', 'EM_ANDAMENTO', 'CONCLUIDO', 'CANCELADO'].includes(initialStatus)) root.querySelector('#workStatus').value = initialStatus;
            root.querySelector('#workStatus').addEventListener('change', () => { number = 1; load(); });
            root.querySelector('#workSearch').addEventListener('input', draw);
            root.querySelector('#proposalFilter')?.addEventListener('change', draw);
            root.querySelector('#refreshWork').addEventListener('click', load);
            root.querySelector('#previousWork').addEventListener('click', () => { if (number > 1) { number--; load(); } });
            root.querySelector('#nextWork').addEventListener('click', () => { if (number * 10 < total) { number++; load(); } });
        }
        function draw() {
            const area = mode(), search = root.querySelector('#workSearch').value.trim().toLocaleLowerCase('pt-BR');
            const filter = root.querySelector('#proposalFilter')?.value;
            const visible = loaded.filter(p => area === 'propostas' ? !!p.meuOrcamento : area === 'trabalhos' ? mine(p, profile) : ['PENDENTE', 'ACEITO'].includes(norm(p.destinatarios?.find(d => d.profissionalId === profile.id)?.situacao)))
                .filter(p => [p.titulo, p.categoria, p.cidade, p.bairro].join(' ').toLocaleLowerCase('pt-BR').includes(search))
                .filter(p => !filter || quoteState(p) === filter);
            const empty = area === 'oportunidades' ? 'Não encontramos serviços disponíveis para você nesta página.' : area === 'propostas' ? 'Nenhuma proposta corresponde a esta página e seleção. Você pode consultar outras páginas ou encontrar serviços.' : 'Nenhum trabalho corresponde a esta página e situação. Quando um cliente escolher sua proposta, o serviço aparecerá aqui.';
            root.querySelector('#workResults').innerHTML = visible.length ? visible.map(p => card(p, area, profile)).join('') : `<section class="app-card"><p>${empty}</p><a href="${area === 'oportunidades' ? 'perfil.html#regions' : 'fila-chamados.html#oportunidades'}">${area === 'oportunidades' ? 'Revisar serviços e regiões' : 'Encontrar serviços'}</a></section>`;
        }
        async function load() {
            const request = ++sequence, target = root.querySelector('#workResults');
            target.innerHTML = '<p role="status">Carregando…</p>';
            root.querySelector('#previousWork').disabled = true; root.querySelector('#nextWork').disabled = true;
            try {
                const [data, me] = await Promise.all([page(C.api, root.querySelector('#workStatus').value, number), profile ? Promise.resolve(profile) : C.api('/profissionais/me')]);
                if (request !== sequence) return; profile = me; loaded = data.items; total = data.total; draw();
                root.querySelector('#workPage').textContent = `Página ${number} de ${Math.max(1, Math.ceil(total / 10))} de pedidos recebidos`;
                root.querySelector('#previousWork').disabled = number === 1; root.querySelector('#nextWork').disabled = number * 10 >= total;
            } catch (e) { if (request === sequence) { root.querySelector('#workPage').textContent = 'Carregamento não concluído'; error(target, load, e.message); } }
        }
        structure(); window.addEventListener('hashchange', () => { structure(); load(); }); await load();
    }
    async function home(root) {
        root.innerHTML = '<p role="status">Carregando suas próximas ações…</p>';
        try {
            const [panel, profile, accepted] = await Promise.all([C.api('/painel/profissional'), C.api('/profissionais/me'), page(C.api, 'ACEITO', 1)]);
            const chosenWork = accepted.items.filter(p => mine(p, profile));
            let action = ['Ver serviços disponíveis', 'fila-chamados.html#oportunidades'];
            if (!panel.cadastroCompleto) action = ['Completar meu cadastro', 'cadastro-profissional.html'];
            else if (!profile.verificado) action = ['Acompanhar minha verificação', 'verificacao.html'];
            else if (panel.servicosEmAndamento > 0) action = ['Continuar trabalho', 'fila-chamados.html?status=EM_ANDAMENTO#trabalhos'];
            else if (chosenWork.length) action = ['Acompanhar serviço aceito', detailUrl(chosenWork[0].id)];
            else if (panel.novosChamados > 0) action = ['Ver serviços disponíveis', 'fila-chamados.html#oportunidades'];
            else if (panel.orcamentosEnviados > 0) action = ['Ver minhas propostas', 'fila-chamados.html#propostas'];
            else if (panel.chamadosAceitos > 0) action = ['Preparar uma proposta', 'fila-chamados.html#oportunidades'];
            root.innerHTML = `<section class="app-card"><h1>O que fazer agora?</h1>${!panel.cadastroCompleto ? '<p>Seu cadastro está incompleto. Revise serviços, preços, regiões.</p>' : !profile.verificado ? '<p>Sua identidade ainda não está verificada. Isso impede novos direcionamentos compatíveis.</p>' : '<p>Confira as oportunidades recebidas e acompanhe seus atendimentos. Cada novo pedido depende da compatibilidade de serviço e região.</p>'}<a class="btn btn-primary" href="${action[1]}">${action[0]}</a></section>${tabs}<section class="app-card"><h2>Suas atividades</h2><p>${safe(panel.novosChamados)} oportunidades aguardam sua resposta.</p><p>${safe(panel.chamadosAceitos)} interesses demonstrados em pedidos ainda abertos.</p><p>${safe(panel.orcamentosEnviados)} propostas enviadas no total — inclui propostas encerradas.</p><p>${safe(panel.servicosEmAndamento)} trabalhos em andamento.</p></section><section class="app-card"><h2>Serviços urgentes aceitos</h2>${chosenWork.length ? chosenWork.slice(0, 3).map(p => `<p>${safe(p.titulo)} <a href="${detailUrl(p.id)}">Combinar atendimento</a></p>`).join('') : '<p>Nenhum serviço aceito na primeira página de pedidos aceitos.</p>'}<a href="fila-chamados.html#trabalhos">Ver próximos trabalhos e outras páginas</a></section>`;
        } catch (e) { error(root, () => home(root), e.message); }
    }
    async function detail(root) {
        const id = new URLSearchParams(location.search).get('id');
        if (!/^\d+$/.test(id || '') || Number(id) <= 0) { root.innerHTML = '<p role="alert">Serviço não informado.</p>'; return; }
        let current, profile, sending = false, refreshRequired = false;
        async function load() {
            root.innerHTML = '<p role="status">Carregando serviço…</p>';
            try { [current, profile] = await Promise.all([C.api(`/pedidos/${id}`), C.api('/profissionais/me')]); render(); } catch (e) { error(root, load, e.message); }
        }
        function render() {
            const p = current, a = allowed(p, profile), q = p.meuOrcamento;
            root.innerHTML = `${tabs}<article class="app-card"><span class="badge badge-info">${safe(label(p.status))}</span><h1>${safe(p.titulo)}</h1><p>${safe(p.categoria)} · ${safe([p.cidade, p.bairro, p.endereco].filter(Boolean).join(' · '))}</p><p>${safe(p.descricao)}</p><p>${norm(p.tipoAtendimento) === 'IMEDIATO' ? 'Solicitação urgente' : 'Data histórica: ' + safe(date(p.dataDesejada))}</p>${p.nomeCliente ? `<p>Cliente: ${safe(p.nomeCliente)}</p>` : ''}${(p.fotoUrls || []).map(url => `<img src="${safe(url)}" alt="Foto do serviço solicitado" loading="lazy" style="max-width:100%;max-height:240px">`).join('')}${p.agendamento ? `<p>Registro histórico: ${safe(date(p.agendamento.dataHora))} · ${safe(label(p.agendamento.situacao))}</p>` : ''}${p.motivoCancelamento ? `<p>Motivo do cancelamento: ${safe(p.motivoCancelamento)}</p>` : ''}${q ? `<section><h2>Sua proposta: ${safe(quoteState(p))}</h2><p>${money(q.valor)} · ${safe(q.prazoEstimado)}</p><p>${safe(q.condicoes || '')}</p><p>Válida até ${safe(date(q.validoAte))}</p></section>` : ''}<p id="workMessage" role="status" class="app-message"></p><fieldset id="workActions" class="app-stack"><legend>Ações disponíveis</legend>${a.interest ? '<p>Demonstrar interesse libera o formulário de proposta. O trabalho só será seu quando o cliente escolher sua proposta.</p><button type="button" class="btn btn-primary" id="interestWork">Preparar proposta</button><button type="button" class="btn btn-secondary" id="rejectWork">Não tenho interesse</button>' : ''}${a.quote ? `${q ? '<button type="button" class="btn btn-secondary" id="editQuote">Revisar ou reenviar proposta</button>' : ''}<form class="app-stack" id="workQuoteForm" ${q ? 'hidden' : ''}><label>Quanto você cobra por esse serviço? (R$)<input id="workQuoteValue" type="number" min="0.01" max="99999999.99" step="0.01" required></label><label>Qual o prazo estimado?<input id="workQuoteDeadline" maxlength="100" required placeholder="Ex.: 2 horas"></label><label>Quer explicar alguma coisa ao cliente? (opcional)<textarea id="workQuoteConditions" maxlength="500"></textarea></label><button type="submit" class="btn btn-primary">Enviar proposta</button></form>` : ''}${a.start ? '<button type="button" class="btn btn-primary" id="startWork">Iniciar serviço</button>' : ''}${a.complete ? '<button type="button" class="btn btn-primary" id="completeWork">Concluir serviço</button>' : ''}${a.cancel ? '<form class="app-stack" id="workCancelForm"><label>Motivo do cancelamento<input id="workCancelReason" minlength="5" maxlength="200" required></label><button type="submit" class="btn btn-secondary">Cancelar serviço</button></form>' : ''}</fieldset><div id="workContact"></div></article><section class="app-card"><h2>Histórico do serviço</h2><div id="workHistory" aria-live="polite"></div></section>`;
            const $ = id => root.querySelector('#' + id);
            if (a.quote) { $('workQuoteValue').value = q?.valor ?? ''; $('workQuoteDeadline').value = q?.prazoEstimado || ''; $('workQuoteConditions').value = q?.condicoes || ''; }
            function button(id, fn) { $(id)?.addEventListener('click', fn); }
            button('editQuote', () => { $('workQuoteForm').hidden = false; $('workQuoteValue').focus(); });
            button('interestWork', () => send('interest', `/pedidos/${id}/aceitar`, 'POST', null));
            button('rejectWork', () => { if (window.confirm('Não deseja enviar uma proposta para este serviço?')) send('interest', `/pedidos/${id}/recusar`, 'POST', {}); });
            button('startWork', () => { if (window.confirm('Confirmar o início do serviço?')) send('start', `/pedidos/${id}/status`, 'PATCH', { status: 'EM_ANDAMENTO' }); });
            button('completeWork', () => { if (window.confirm('O serviço foi realizado e pode ser concluído?')) send('complete', `/pedidos/${id}/status`, 'PATCH', { status: 'CONCLUIDO' }); });
            $('workQuoteForm')?.addEventListener('submit', event => { event.preventDefault(); try { const body = quotePayload($('workQuoteValue').value, $('workQuoteDeadline').value, $('workQuoteConditions').value); return send('quote', `/pedidos/${id}/orcamentos`, 'POST', body); } catch (e) { $('workMessage').textContent = e.message; } });
            $('workCancelForm')?.addEventListener('submit', event => { event.preventDefault(); const motivo = $('workCancelReason').value.trim(); if (motivo.length < 5 || motivo.length > 200) { $('workMessage').textContent = 'Informe o motivo com 5 a 200 caracteres.'; return; } if (window.confirm('Cancelar este serviço?')) return send('cancel', `/pedidos/${id}/cancelar`, 'POST', { motivo }); });
            history(); contact();
        }
        async function send(action, path, method, body) {
            if (sending || refreshRequired) return;
            const msg = root.querySelector('#workMessage'), fields = root.querySelector('#workActions');
            if (!allowed(current, profile)[action]) { msg.textContent = 'Esta ação não está disponível para a situação atual.'; return; }
            sending = true; fields.disabled = true; fields.setAttribute('aria-busy', 'true'); msg.textContent = 'Enviando…';
            try {
                const result = await C.api(path, { method, body });
                // Hide the successful form immediately, even if the subsequent GET fails.
                if (action === 'quote') { current.meuOrcamento = result; root.querySelector('#workQuoteForm').hidden = true; }
                try { current = await C.api(`/pedidos/${id}`); render(); root.querySelector('#workMessage').textContent = 'Informações atualizadas.' + (result?.aviso ? ' ' + result.aviso : ''); }
                catch { refreshRequired = true; fields.disabled = true; msg.innerHTML = `A ação foi salva, mas não foi possível atualizar os detalhes. <a href="${detailUrl(id)}">Recarregar detalhes</a> antes de realizar outra ação.`; return; }
            } catch (e) { msg.textContent = e.message || 'Não foi possível enviar. Seus dados continuam aqui.'; fields.disabled = false; }
            finally { sending = false; fields.setAttribute('aria-busy', 'false'); }
        }
        async function history() { const target = root.querySelector('#workHistory'); target.innerHTML = '<p>Carregando histórico…</p>'; try { const items = await C.api(`/pedidos/${id}/historico`); target.innerHTML = items.length ? items.map(h => `<p><strong>${safe(label(h.statusNovo))}</strong> · ${safe(date(h.criadoEm))}<br>${safe(h.detalhe || 'Atualização do serviço')}</p>`).join('') : '<p>Ainda não há eventos registrados.</p>'; } catch (e) { error(target, history, e.message); } }
        async function contact() { if (!mine(current, profile) || !['ACEITO', 'AGENDADO', 'EMANDAMENTO'].includes(norm(current.status))) return; try { const data = await C.api(`/whatsapp/pedidos/${id}/contato`); const phone = String(data.telefone || '').replace(/\D/g, ''); if (phone) root.querySelector('#workContact').innerHTML = `<a class="btn btn-secondary" target="_blank" rel="noopener" href="https://wa.me/${phone}?text=${encodeURIComponent(data.mensagemInicialWhatsApp || 'Olá! Sobre nosso serviço no CHAMAPRO.')}" >Conversar pelo WhatsApp</a>`; } catch {/* Contact remains governed by its endpoint and saved preferences. */ } }
        await load();
    }
    window.ChamaproProfessionalWork = { norm, label, mine, allowed, quoteState, quotePayload, page, list, home, detail, detailUrl };
})();