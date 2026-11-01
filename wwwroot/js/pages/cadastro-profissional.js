(() => {
    const C = window.Chamapro;
    const $ = id => document.getElementById(id);
    const safe = C.escaparHtml;
    const dias = ["Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado"];
    let perfil = null;
    let categorias = [];
    let contatoWhatsApp = null;
    let categoriasSelecionadasAtuais = [];

    function exibirMensagem(texto, sucesso = false) {
        const el = $("wizardMessage");
        el.textContent = texto || "";
        el.classList.toggle("success", Boolean(texto) && sucesso);
        el.classList.toggle("error", Boolean(texto) && !sucesso);
    }

    function idsCategoriasSalvos() {
        return categoriasSelecionadasAtuais;
    }

    function mostrarEtapa(etapa) {
        document.querySelectorAll("[data-wizard-panel]").forEach(painel => {
            painel.hidden = Number(painel.dataset.wizardPanel) !== etapa;
        });
        document.querySelectorAll("[data-progress-step]").forEach(item => {
            const numero = Number(item.dataset.progressStep);
            item.classList.toggle("is-complete", numero < etapa);
            if (numero === etapa) item.setAttribute("aria-current", "step");
            else item.removeAttribute("aria-current");
        });
        $("currentStep").textContent = String(etapa);
        exibirMensagem("");
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function dadosPerfil(overrides = {}) {
        return {
            nome: perfil.nome,
            telefone: String(perfil.telefone || "").replace(/\D/g, ""),
            descricao: $("description").value.trim(),
            fotoUrl: perfil.fotoUrl || null,
            fotosTrabalhos: perfil.fotosTrabalhos || [],
            experienciaAnos: $("experience").value ? Number($("experience").value) : null,
            distanciaMaxKm: $("distance").value ? Number($("distance").value) : null,
            atendeUrgencia: $("urgent").checked,
            atendeDomicilio: $("homeService").checked,
            atendeEstabelecimento: $("shopService").checked,
            ...overrides
        };
    }

    async function salvarPerfil(overrides = {}) {
        await C.api("/profissionais/me", { method: "PUT", body: dadosPerfil(overrides) });
    }

    async function enviarImagem(arquivo) {
        if (!arquivo) return null;
        if (arquivo.size > 5 * 1024 * 1024) throw new Error("Cada imagem deve ter até 5 MB.");
        const data = new FormData();
        data.append("arquivo", arquivo);
        data.append("privado", "false");
        const resultado = await C.api("/arquivos", { method: "POST", body: data });
        return resultado.url;
    }

    function categoriasSelecionadas() {
        return [...document.querySelectorAll("[data-category-id]:checked")]
            .map(input => Number(input.dataset.categoryId))
            .filter(Number.isInteger);
    }

    function renderizarCategorias() {
        const selecionadas = new Set(idsCategoriasSalvos());
        const lista = $("categoryList");
        lista.innerHTML = categorias.map(categoria =>
            '<label class="category-option"><input type="checkbox" data-category-id="' +
            Number(categoria.id) + '"' + (selecionadas.has(Number(categoria.id)) ? " checked" : "") +
            '> <span>' + safe(categoria.nome) + "</span></label>"
        ).join("");
        lista.querySelectorAll("[data-category-id]").forEach(input => input.addEventListener("change", () => {
            categoriasSelecionadasAtuais = categoriasSelecionadas();
            renderizarPrecos();
        }));
    }

    function renderizarPrecos() {
        const selecionadas = new Set(idsCategoriasSalvos());
        const precosAtuais = new Map((perfil?.servicos || []).map(item => [Number(item.categoriaId), item]));
        const escolhidas = categorias.filter(categoria => selecionadas.has(Number(categoria.id)));
        $("categoryPrices").innerHTML = escolhidas.length ? escolhidas.map(categoria => {
            const atual = precosAtuais.get(Number(categoria.id));
            return '<div class="category-price-row"><strong>' + safe(categoria.nome) +
                '</strong><label>Mínimo (R$)<input type="number" min="0" step="0.01" required data-price-min="' +
                Number(categoria.id) + '" value="' + (atual?.faixaPrecoMin ?? "") +
                '"></label><label>Máximo (R$)<input type="number" min="0" step="0.01" required data-price-max="' +
                Number(categoria.id) + '" value="' + (atual?.faixaPrecoMax ?? "") + '"></label></div>';
        }).join("") : '<p class="empty-state">Volte à etapa Profissão e selecione ao menos uma categoria.</p>';
    }

    function renderizarDisponibilidade() {
        const atuais = perfil.disponibilidade || [];
        $("availabilityGrid").innerHTML = dias.map((dia, indice) => {
            const atual = atuais.find(item => Number(item.diaSemana) === indice);
            const ativo = Boolean(atual);
            return '<div class="availability-row"><label><input type="checkbox" data-day-enabled="' + indice +
                '"' + (ativo ? " checked" : "") + '> ' + dia + '</label><input type="time" aria-label="Início ' +
                dia + '" data-day-start="' + indice + '" value="' + (atual?.horaInicio?.slice(0, 5) || "09:00") +
                '"' + (ativo ? "" : " disabled") + '><input type="time" aria-label="Fim ' + dia +
                '" data-day-end="' + indice + '" value="' + (atual?.horaFim?.slice(0, 5) || "17:00") +
                '"' + (ativo ? "" : " disabled") + '><label><input type="checkbox" data-day-immediate="' +
                indice + '"' + (atual?.atendeImediato ? " checked" : "") + (ativo ? "" : " disabled") +
                '> Imediato</label><label><input type="checkbox" data-day-scheduled="' + indice + '"' +
                (atual?.atendeAgendado !== false && ativo ? " checked" : "") + (ativo ? "" : " disabled") +
                '> Agendado</label></div>';
        }).join("");
        document.querySelectorAll("[data-day-enabled]").forEach(toggle => toggle.addEventListener("change", () => {
            const indice = toggle.dataset.dayEnabled;
            const habilitar = toggle.checked;
            ["start", "end", "immediate", "scheduled"].forEach(tipo => {
                const el = document.querySelector('[data-day-' + tipo + '="' + indice + '"]');
                el.disabled = !habilitar;
                if (tipo === "scheduled" && habilitar && !el.checked) el.checked = true;
            });
        }));
    }

    async function carregar() {
        try {
            const resultados = await Promise.all([
                C.api("/profissionais/me"),
                C.api("/categorias"),
                C.api("/whatsapp/contato/me").catch(() => null)
            ]);
            perfil = resultados[0];
            categorias = resultados[1] || [];
            contatoWhatsApp = resultados[2];
            categoriasSelecionadasAtuais = (perfil.servicos || []).map(item => Number(item.categoriaId));
            $("description").value = perfil.descricao || "";
            $("experience").value = perfil.experienciaAnos ?? "";
            $("distance").value = perfil.distanciaMaxKm ?? "20";
            $("homeService").checked = perfil.atendeDomicilio !== false;
            $("shopService").checked = Boolean(perfil.atendeEstabelecimento);
            $("urgent").checked = Boolean(perfil.atendeUrgencia);
            $("portfolioUrls").value = (perfil.fotosTrabalhos || []).join("\n");
            $("areas").value = (perfil.areas || []).map(area => area.cidade + " | " + (area.bairro || "")).join("\n");
            $("whatsappActive").checked = Boolean(contatoWhatsApp?.whatsAppAtivo);
            $("whatsappMessage").value = contatoWhatsApp?.mensagemInicialWhatsApp || "";
            renderizarCategorias();
            renderizarPrecos();
            renderizarDisponibilidade();
        } catch (erro) {
            exibirMensagem(erro.message || "Não foi possível carregar seu perfil.");
            document.querySelectorAll("[data-wizard-panel] button").forEach(button => button.disabled = true);
        }
    }

    async function executarComBotao(id, textoCarregando, acao) {
        const botao = $(id);
        botao.disabled = true;
        const anterior = botao.textContent;
        botao.textContent = textoCarregando;
        exibirMensagem("");
        try {
            await acao();
        } catch (erro) {
            exibirMensagem(erro.message || "Não foi possível salvar esta etapa.");
        } finally {
            botao.disabled = false;
            botao.textContent = anterior;
        }
    }

    async function salvarEtapaProfissional() {
        const descricao = $("description").value.trim();
        const ids = categoriasSelecionadas();
        if (descricao.length < 10) throw new Error("Descreva sua experiência em pelo menos 10 caracteres.");
        if (!ids.length) throw new Error("Escolha ao menos uma categoria de serviço.");

        const novaFoto = await enviarImagem($("photoFile").files[0]);
        if (novaFoto) perfil.fotoUrl = novaFoto;
        const urlsExistentes = $("portfolioUrls").value.split(/\r?\n/).map(item => item.trim()).filter(Boolean);
        const arquivos = [...$("portfolioFiles").files];
        if (urlsExistentes.length + arquivos.length > 10) throw new Error("O portfólio aceita até dez imagens ou links.");
        for (const arquivo of arquivos) urlsExistentes.push(await enviarImagem(arquivo));
        perfil.fotosTrabalhos = urlsExistentes;
        await salvarPerfil({ fotoUrl: perfil.fotoUrl, fotosTrabalhos: perfil.fotosTrabalhos });
        const categoriasRascunho = ids.map(categoriaId => ({
            categoriaId,
            faixaPrecoMin: null,
            faixaPrecoMax: null
        }));
        await C.api("/profissionais/me/servicos", { method: "PUT", body: categoriasRascunho });
        perfil.servicos = categoriasRascunho;
        categoriasSelecionadasAtuais = ids;
        renderizarPrecos();
        await C.api("/whatsapp/contato/me", { method: "PUT", body: {
            telefone: String(perfil.telefone || "").replace(/\D/g, ""),
            whatsAppAtivo: $("whatsappActive").checked,
            mensagemInicialWhatsApp: $("whatsappMessage").value.trim() || null
        }});
        mostrarEtapa(3);
    }

    function lerAreas() {
        return $("areas").value.split(/\r?\n/).map(linha => linha.trim()).filter(Boolean).map(linha => {
            const partes = linha.split("|");
            const cidade = partes[0].trim();
            if (!cidade) throw new Error("Informe a cidade em cada linha de atendimento.");
            return { cidade, bairro: partes.slice(1).join("|").trim() || null };
        });
    }

    async function salvarEtapaLocalizacao() {
        const areas = lerAreas();
        if (!areas.length) throw new Error("Informe ao menos uma cidade ou região de atendimento.");
        if (!$("distance").value || Number($("distance").value) < 1 || Number($("distance").value) > 1000) {
            throw new Error("Informe uma distância máxima entre 1 e 1.000 km.");
        }
        if (!$("homeService").checked && !$("shopService").checked) {
            throw new Error("Selecione atendimento em domicílio, em estabelecimento ou os dois.");
        }
        await C.api("/profissionais/me/areas", { method: "PUT", body: areas });
        await salvarPerfil();
        perfil.areas = areas;
        mostrarEtapa(4);
    }

    function lerPrecos() {
        return [...document.querySelectorAll("[data-price-min]")].map(minimo => {
            const id = Number(minimo.dataset.priceMin);
            const maximo = document.querySelector('[data-price-max="' + id + '"]');
            const faixaPrecoMin = Number(minimo.value);
            const faixaPrecoMax = Number(maximo.value);
            if (!minimo.value || !maximo.value || !Number.isFinite(faixaPrecoMin) ||
                !Number.isFinite(faixaPrecoMax) || faixaPrecoMin < 0 || faixaPrecoMax < faixaPrecoMin) {
                throw new Error("Confira os preços mínimos e máximos de cada categoria.");
            }
            return { categoriaId: id, faixaPrecoMin, faixaPrecoMax };
        });
    }

    function lerDisponibilidade() {
        return [...document.querySelectorAll("[data-day-enabled]:checked")].map(toggle => {
            const dia = toggle.dataset.dayEnabled;
            const horaInicio = document.querySelector('[data-day-start="' + dia + '"]').value;
            const horaFim = document.querySelector('[data-day-end="' + dia + '"]').value;
            const atendeImediato = document.querySelector('[data-day-immediate="' + dia + '"]').checked;
            const atendeAgendado = document.querySelector('[data-day-scheduled="' + dia + '"]').checked;
            if (!horaInicio || !horaFim || horaFim <= horaInicio || (!atendeImediato && !atendeAgendado)) {
                throw new Error("Confira o horário e o tipo de atendimento para cada dia selecionado.");
            }
            return { diaSemana: Number(dia), horaInicio, horaFim, atendeImediato, atendeAgendado };
        });
    }

    async function salvarEtapaPrecos() {
        const servicos = lerPrecos();
        const disponibilidade = lerDisponibilidade();
        if (!servicos.length) throw new Error("Selecione uma categoria na etapa Profissão.");
        if (!disponibilidade.length) throw new Error("Informe ao menos um dia e horário disponível.");
        await C.api("/profissionais/me/servicos", { method: "PUT", body: servicos });
        await C.api("/profissionais/me/disponibilidade", { method: "PUT", body: disponibilidade });
        await salvarPerfil();
        perfil.servicos = servicos;
        perfil.disponibilidade = disponibilidade;
        window.location.assign("verificacao.html?onboarding=1");
    }

    $("saveProfessionalStep").addEventListener("click", () =>
        executarComBotao("saveProfessionalStep", "Salvando…", salvarEtapaProfissional));
    $("saveLocationStep").addEventListener("click", () =>
        executarComBotao("saveLocationStep", "Salvando…", salvarEtapaLocalizacao));
    $("saveScheduleStep").addEventListener("click", () =>
        executarComBotao("saveScheduleStep", "Salvando…", salvarEtapaPrecos));
    document.querySelectorAll("[data-wizard-back]").forEach(button =>
        button.addEventListener("click", () => mostrarEtapa(Number(button.dataset.wizardBack))));

    window.ChamaproApp?.ready.then(usuario => {
        if (usuario?.perfil === "PROFISSIONAL") carregar();
    });
})();
