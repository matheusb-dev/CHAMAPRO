(() => {
    const C = window.Chamapro;
    const safe = C.escaparHtml;
    const setMessage = (id, text, success = false) => {
        const el = document.getElementById(id);
        if (el) { el.textContent = text || ""; el.style.color = success ? "var(--color-success)" : "var(--color-danger)"; }
    };

    function orderError(error) {
        const rules = {
            SEM_PROFISSIONAIS_COMPATIVEIS: 'Não encontramos profissionais disponíveis para este serviço e local. Confira a cidade e tente novamente.',
            PROFISSIONAL_NAO_ATENDE: 'Um dos profissionais escolhidos não está disponível para este pedido. Atualize a busca ou deixe a plataforma procurar por você.',
            LIMITE_PEDIDOS_PENDENTES: 'Você já tem cinco pedidos aguardando resposta. Acompanhe ou cancele um deles em Meus pedidos antes de pedir outro serviço.',
            CATEGORIA_INVALIDA: 'Esta categoria não está mais disponível. Escolha outra e tente novamente.',
            VALIDACAO: 'Confira os campos do pedido e tente novamente.',
            ARQUIVO_MUITO_GRANDE: 'Use até cinco fotos JPG, PNG ou WEBP, de até 5 MB cada.'
        };
        if (rules[error.codigo]) return rules[error.codigo];
        if (error.status === 401) return 'Sua sessão expirou. Entre novamente para enviar o pedido.';
        if (error.status === 403) return 'Esta conta não pode pedir serviços. Entre com uma conta de cliente.';
        if (error.status === 422 || error.status === 400) return 'Não foi possível confirmar com esses dados. Confira o serviço e a localização e tente novamente.';
        return 'Não foi possível enviar agora. Seus dados continuam aqui. Verifique a conexão e tente novamente.';
    }
    window.ChamaproOrderErrors = { message: orderError };

    async function iniciarCriacao() {
        const form = document.getElementById('createOrderForm');
        const categorySelect = document.getElementById('category');
        const city = document.getElementById('city');
        const picker = document.getElementById('professionalPicker');
        const reviewPanel = document.getElementById('reviewPanel');
        const button = document.getElementById('confirmOrder');
        let draft = null, submitting = false, professionalSearchVersion = 0, finalStep = false, professionalSelectionLoaded = false;
        const params = new URLSearchParams(location.search || '');
        let favoriteId = Number(params.get('profissionalId')) || null;
        if (!Number.isSafeInteger(favoriteId) || favoriteId < 1) favoriteId = null;
        const uploadedFiles = new WeakMap(), dirty = new Set();
        let municipalities = [];
        let lastCity = null;
        const categoryPicker = window.ChamaproCombobox?.mount(categorySelect, { label: 'Categoria', placeholder: 'Ex.: pedreiro, eletricista…' });
        const cityPicker = window.ChamaproCombobox?.mount(city, { label: 'Cidade e estado', placeholder: 'Pesquise sua cidade…' });
        const notify = (id, text) => { const el = document.getElementById(id); if (el) el.textContent = text; };
        const selectedMode = () => document.querySelector('input[name="distribution"]:checked')?.value;
        function summary() {
            const category = categoryPicker?.selection?.label || categorySelect.selectedOptions?.[0]?.text || '';
            document.getElementById('reviewSummary').innerHTML = [
                ['Serviço', document.getElementById('title').value], ['Categoria', category], ['O que precisa', document.getElementById('description').value],
                ['Local', [cityPicker?.selection?.label || city.value, document.getElementById('neighborhood').value, document.getElementById('address').value].filter(Boolean).join(' · ')],
                ['Atendimento', 'Solicitação urgente, sem garantia de atendimento imediato'],
                ['Envio', selectedMode() === 'manual' ? 'Para os profissionais que você selecionar abaixo.' : 'Vamos procurar profissionais compatíveis com o seu serviço.']
            ].map(([label, value]) => `<p><strong>${label}:</strong> ${safe(value || 'Ainda não informado')}</p>`).join('');
            if (finalStep) reviewPanel.hidden = false;
        }
        function changed() { if (!submitting) { draft = null; setMessage('reviewMessage', ''); if (finalStep) summary(); } }
        form.addEventListener('input', changed); form.addEventListener('change', changed);
        for (const id of ['city', 'neighborhood', 'address']) document.getElementById(id).addEventListener('input', () => dirty.add(id));
        async function categories() {
            categoryPicker?.setState('loading', 'Carregando categorias…');
            try {
                const rows = await C.api('/categorias');
                const old = categorySelect.value;
                categorySelect.innerHTML = '<option value="">Escolha o serviço</option>' + rows.map(row => `<option value="${row.id}">${safe(row.nome)}</option>`).join('');
                if (rows.some(row => String(row.id) === old)) categorySelect.value = old;
                categoryPicker?.setItems(rows.map(row => ({ value: String(row.id), label: row.nome })));
                if (!rows.length) notify('categoryHelp', 'Nenhuma categoria disponível. Tente novamente mais tarde.');
            } catch {
                categoryPicker?.setState('error', 'Não foi possível carregar as categorias.', categories);
                if (!categoryPicker) setMessage('formMessage', 'Não foi possível carregar categorias. Atualize a página e tente novamente.');
            }
        }
        async function cities() {
            if (!cityPicker) return;
            cityPicker.setState('loading', 'Carregando cidades…');
            try {
                const response = await fetch('/data/municipios-ibge.json?v=20261102');
                if (!response.ok) throw new Error('cities');
                municipalities = await response.json();
                cityPicker.setItems(municipalities.map(row => ({ value: String(row.id), payload: row.nome, label: `${row.nome} — ${row.uf}`, uf: row.uf })));
            } catch { cityPicker.setState('error', 'Não foi possível carregar as cidades.', cities); }
        }
        city.addEventListener('change', () => {
            const key = city.dataset?.municipioId || city.value;
            if (lastCity && lastCity !== key) {
                document.getElementById('neighborhood').value = '';
                document.getElementById('address').value = '';
                notify('locationMessage', 'Cidade alterada. Confira o bairro e o endereço do serviço.');
            }
            lastCity = key;
            const normalize = window.ChamaproCombobox?.normalize;
            const matches = normalize ? municipalities.filter(row => normalize(row.nome) === normalize(city.value)) : [];
            notify('cityWarning', matches.length > 1 ? 'Atenção: este nome existe em mais de um estado. A busca atual identifica a cidade somente pelo nome, sem distinguir a UF. Confirme a região com o profissional.' : '');
        });
        async function locationFromProfile() {
            try {
                const profile = await C.api('/clientes/me');
                if (!dirty.has('city') && profile?.cidade && cityPicker && municipalities.length) {
                    const matches = municipalities.filter(row => window.ChamaproCombobox.normalize(row.nome) === window.ChamaproCombobox.normalize(profile.cidade));
                    if (matches.length === 1) cityPicker.select(String(matches[0].id));
                    else { cityPicker.search(profile.cidade); notify('locationMessage', 'Escolha a cidade com a UF correta para confirmar sua localização.'); }
                } else if (!dirty.has('city') && profile?.cidade && !cityPicker) city.value = profile.cidade;
                for (const [id, value] of [['neighborhood', profile?.bairro], ['address', profile?.enderecoPrincipal]]) if (!dirty.has(id) && value) document.getElementById(id).value = value;
                if (!profile?.cidade) notify('locationMessage', 'Informe onde será o serviço. Você também pode salvar sua localização em Minha conta.');
            } catch { notify('locationMessage', 'Não foi possível recuperar sua localização. Informe o local do serviço abaixo.'); }
        }
        const clearProfessionalSelection = () => { professionalSearchVersion++; professionalSelectionLoaded = false; document.getElementById('professionalList').innerHTML = ''; if (favoriteId) notify('favoriteMessage', 'Vamos conferir se o favorito atende ao serviço e local escolhidos.'); changed(); };
        document.querySelectorAll('input[name="distribution"]').forEach(radio => radio.addEventListener('change', () => { picker.hidden = selectedMode() !== 'manual'; clearProfessionalSelection(); }));
        [categorySelect, city, document.getElementById('neighborhood')].forEach(input => {
            input.addEventListener('change', clearProfessionalSelection); input.addEventListener('input', clearProfessionalSelection);
        });
        async function searchProfessionals() {
            const searchVersion = ++professionalSearchVersion;
            const categoryId = categorySelect.value, region = city.value.trim(), neighborhood = document.getElementById('neighborhood').value.trim();
            if (!categoryId || !region) { setMessage('formMessage', 'Escolha o serviço e a cidade antes de buscar profissionais.'); return; }
            const target = document.getElementById('professionalList');
            target.innerHTML = '<p class="empty-state" role="status">Buscando profissionais…</p>';
            try {
                const query = new URLSearchParams({ categoriaId: categoryId, cidade: region, pagina: '1', tamanho: '50', tipoAtendimento: 'IMEDIATO', verificado: 'true' });
                if (neighborhood) query.set('bairro', neighborhood);
                    let result = await C.api(`/profissionais?${query}`);
                if (searchVersion !== professionalSearchVersion) return;
                const rows = [...(result.itens || [])];
                let page = 1;
                while (favoriteId && !rows.some(row => row.id === favoriteId) && page * 50 < result.total) {
                    query.set('pagina', String(++page)); result = await C.api(`/profissionais?${query}`);
                    if (searchVersion !== professionalSearchVersion) return; rows.push(...result.itens);
                }
                const compatible = rows.filter(row => row.verificado === true);
                professionalSelectionLoaded = true;
                target.innerHTML = compatible.length ? compatible.map(pro => `<label class="app-row"><span><input type="checkbox" name="professionalId" value="${pro.id}" ${favoriteId === pro.id ? 'checked' : ''}> <strong>${safe(pro.nome)}</strong><small class="app-muted"> · nota ${C.formatarNota(pro.notaMedia)}${pro.totalAvaliacoes ? "" : " · nota inicial, sem avaliações"} · verificado</small></span><a href="perfil-profissional.html?id=${pro.id}" target="_blank" rel="noopener">Ver perfil</a></label>`).join('') : '<p class="empty-state">Não há profissionais verificados disponíveis para esta categoria e região. Confira a localização ou use a busca automática para salvar seu pedido e aguardar novas propostas.</p>';
                if (favoriteId) notify('favoriteMessage', compatible.some(row => row.id === favoriteId) ? 'Seu favorito é compatível e está selecionado. Você pode mudar a escolha.' : 'Seu favorito não está disponível para esta categoria, região, ou não está verificado. Escolha outro profissional ou use a busca automática.');
            } catch {
                if (searchVersion === professionalSearchVersion) {
                    target.innerHTML = '<p class="empty-state" role="alert">Não foi possível buscar os profissionais. Use o botão de busca para tentar novamente ou deixe a plataforma procurar por você.</p>';
                }
            }
        }
        document.getElementById('searchProfessionals').addEventListener('click', searchProfessionals);
        document.getElementById('useAutomatic')?.addEventListener('click', () => {
            favoriteId = null;
            const auto = document.querySelector('input[name="distribution"][value="auto"]');
            const manual = document.querySelector('input[name="distribution"][value="manual"]');
            auto.checked = true; manual.checked = false; auto.dispatchEvent(new Event('change', { bubbles: true })); notify('favoriteMessage', 'Vamos procurar profissionais compatíveis com o seu serviço.');
        });
        if (favoriteId) {
            const manual = document.querySelector('input[name="distribution"][value="manual"]');
            const auto = document.querySelector('input[name="distribution"][value="auto"]');
            if (manual && auto) { manual.checked = true; auto.checked = false; picker.hidden = false; }
            notify('favoriteMessage', 'Você veio de um profissional favorito. Vamos conferir se ele atende ao serviço e local escolhidos.');
        }
        form.addEventListener('chamapro:stepchange', event => {
            finalStep = event.detail === 2;
            if (finalStep) { summary(); if (favoriteId && selectedMode() === 'manual' && !professionalSelectionLoaded) searchProfessionals(); }
        });
        function prepareDraft() {
            if (!form.reportValidity()) return false;
            const distribution = selectedMode();
            if (!['auto', 'manual'].includes(distribution)) { setMessage('formMessage', 'Escolha como receber as propostas.'); return false; }
            const ids = distribution === 'manual' ? [...document.querySelectorAll('input[name="professionalId"]:checked')].map(input => Number(input.value)) : [];
            if (distribution === 'manual' && (!ids.length || ids.length > 5)) { setMessage('formMessage', 'Selecione de um a cinco profissionais compatíveis.'); return false; }
            draft = { categoriaId: Number(categorySelect.value), titulo: document.getElementById('title').value.trim(), descricao: document.getElementById('description').value.trim(), cidade: city.value.trim(), bairro: document.getElementById('neighborhood').value.trim() || null, endereco: document.getElementById('address').value.trim() || null, tipoAtendimento: 'IMEDIATO', dataDesejada: null, distribuirParaCompativeis: distribution === 'auto', profissionalIds: ids, fotoUrls: [] };
            summary(); reviewPanel.hidden = false; return true;
        }
        form.addEventListener('submit', event => { event.preventDefault(); if (prepareDraft()) button.focus?.(); });
        document.getElementById('backToEdit').addEventListener('click', () => { reviewPanel.hidden = true; form.scrollIntoView({ behavior: 'auto' }); });
        button.addEventListener('click', async () => {
            if (submitting || !prepareDraft()) return;
            submitting = true; form.inert = true; button.disabled = true; button.textContent = 'Enviando pedido…'; document.getElementById('backToEdit').disabled = true;
            try {
                const files = [...(document.getElementById('photos').files || [])];
                if (files.length > 5) throw { codigo: 'ARQUIVO_MUITO_GRANDE' };
                for (const file of files) if (file.size > 5 * 1024 * 1024 || (file.type && !['image/jpeg', 'image/png', 'image/webp'].includes(file.type))) throw { codigo: 'ARQUIVO_MUITO_GRANDE' };
                for (const file of files) {
                    if (!uploadedFiles.has(file)) { const data = new FormData(); data.append('arquivo', file); data.append('privado', 'false'); const uploaded = await C.api('/arquivos', { method: 'POST', body: data }); uploadedFiles.set(file, uploaded.url); }
                    draft.fotoUrls.push(uploadedFiles.get(file));
                }
                const created = await C.api('/pedidos', { method: 'POST', body: draft });
                C.toast(created.destinatarios?.length === 0 ? 'Pedido salvo. Ainda não há profissionais compatíveis. Seu pedido continuará aberto para receber propostas.' : 'Pedido urgente enviado! Acompanhe as propostas.', 'success'); location.assign(`orcamentos.html?pedidoId=${created.id}`);
            } catch (error) {
                setMessage('reviewMessage', orderError(error)); submitting = false; form.inert = false; button.disabled = false; button.textContent = 'Confirmar pedido'; document.getElementById('backToEdit').disabled = false;
            }
        });
        document.getElementById('description').addEventListener('change', () => {
            const title = document.getElementById('title');
            const description = document.getElementById('description').value.trim();
            if (!title.value && description.length >= 10) title.value = description.split(/[.!?\n]/)[0].slice(0, 80);
        });
        categories();
        await cities(); await locationFromProfile();
    }

    async function carregarPerfil() {
        document.getElementById('photo').addEventListener('invalid', () => {
            const advanced = document.getElementById('photoAdvanced');
            if (advanced) advanced.open = true;
        });
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

    });
})();
