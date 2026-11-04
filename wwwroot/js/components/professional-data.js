/* Shared state, validation and persistence for onboarding and profile editing. */
(() => {
    const clone = value => JSON.parse(JSON.stringify(value));
    const clock = time => String(time).split(':').reduce((sum, part, index) => sum + Number(part) * [3600, 60, 1][index], 0);
    const validTime = value => /^([01]\d|2[0-3]):[0-5]\d(?::[0-5]\d(?:\.\d{1,7})?)?$/.test(String(value));
    function validateIntervals(rows) {
        if (rows.length > 42) throw Error('Informe até 42 intervalos de horário.');
        for (const row of rows) if (!Number.isInteger(Number(row.diaSemana)) || row.diaSemana < 0 || row.diaSemana > 6 || !validTime(row.horaInicio) || !validTime(row.horaFim) || clock(row.horaFim) <= clock(row.horaInicio) || (!row.atendeImediato && !row.atendeAgendado)) throw Error('Confira o início, o fim e o tipo de atendimento de cada intervalo.');
        for (let day = 0; day < 7; day++) { const sorted = rows.filter(r => Number(r.diaSemana) === day).sort((a,b) => clock(a.horaInicio)-clock(b.horaInicio)); if (sorted.some((r,i)=>i && clock(sorted[i-1].horaFim)>clock(r.horaInicio))) throw Error('Os horários de um mesmo dia não podem se sobrepor.'); }
        return rows;
    }
    function applyCommon(current, days, interval, allowReplace = false) {
        if (!days.length) throw Error('Escolha os dias que receberão esse horário.');
        const additions = [...new Set(days.map(Number))].map(day => ({ ...interval, diaSemana: day })); validateIntervals(additions);
        const affected = current.filter(row => days.map(Number).includes(Number(row.diaSemana)));
        const same = affected.length === additions.length && affected.every(row => additions.some(a => a.diaSemana === Number(row.diaSemana) && clock(a.horaInicio) === clock(row.horaInicio) && clock(a.horaFim) === clock(row.horaFim) && a.atendeImediato === row.atendeImediato && a.atendeAgendado === row.atendeAgendado));
        if (same) return { needsConfirmation: false, intervals: clone(current) };
        if (affected.length && !allowReplace) return { needsConfirmation: true, affected: clone(affected) };
        const rows = [...current.filter(row => !days.map(Number).includes(Number(row.diaSemana))), ...additions];
        return { needsConfirmation: false, intervals: validateIntervals(rows) };
    }
    class Store {
        constructor(api) { this.api = api; this.loaded = false; this.saving = false; this.prices = new Map(); this.selected = new Set(); this.areas = []; }
        async load() {
            this.loaded = false;
            const [profile, categories, contact] = await Promise.all([this.api('/profissionais/me'), this.api('/categorias'), this.api('/whatsapp/contato/me').catch(()=>null)]);
            this.profile = clone(profile); this.categories = categories; this.contact = contact;
            this.selected = new Set(profile.servicos.map(s=>Number(s.categoriaId)));
            this.prices = new Map(profile.servicos.map(s=>[Number(s.categoriaId), { faixaPrecoMin:s.faixaPrecoMin, faixaPrecoMax:s.faixaPrecoMax }]));
            this.areas = clone(profile.areas); this.loaded = true; await this.refreshReadiness();
        }
        async refreshReadiness() { try { this.readiness = await this.api('/painel/profissional'); } catch { this.readiness = null; } }
        requireLoaded() { if (!this.loaded) throw Error('Aguarde o carregamento do perfil antes de salvar.'); }
        addCategory(id) { this.requireLoaded(); if (!this.categories.some(c=>Number(c.id)===Number(id))) throw Error('Escolha um serviço da lista.'); this.selected.add(Number(id)); if (!this.prices.has(Number(id))) this.prices.set(Number(id), { faixaPrecoMin:null, faixaPrecoMax:null }); }
        removeCategory(id) { this.selected.delete(Number(id)); }
        servicePayload() {
            if (!this.selected.size) throw Error('Escolha pelo menos um serviço que você realiza.');
            return [...this.selected].map(id => {
                if (!this.categories.some(c=>Number(c.id)===id)) throw Error('Um serviço anterior não está mais disponível. Revise a seleção antes de salvar.');
                const price = this.prices.get(id) || {}; const number = value => value === '' || value == null ? null : Number(value);
                const min = number(price.faixaPrecoMin), max = number(price.faixaPrecoMax);
                if ((min===null)!==(max===null) || (min!==null && (!Number.isFinite(min)||!Number.isFinite(max)||min<0||max<min||max>99999999.99))) throw Error('Preencha os dois preços, do menor para o maior, ou deixe ambos vazios.');
                return { categoriaId:id, faixaPrecoMin:min, faixaPrecoMax:max };
            });
        }
        addArea(city, neighborhood='') { this.requireLoaded(); if (!city || city.length>100 || neighborhood.length>100) throw Error('Selecione uma cidade e confira o bairro.'); const key=v=>v.trim().toLocaleLowerCase('pt-BR'); if (!this.areas.some(a=>key(a.cidade)===key(city)&&key(a.bairro||'')===key(neighborhood))) this.areas.push({cidade:city.trim(),bairro:neighborhood.trim()||null}); }
        removeArea(index) { this.areas.splice(index,1); }
        infoPayload(fields={}) {
            this.requireLoaded();
            const p={...this.profile,...fields}; const body={nome:p.nome,telefone:String(p.telefone||'').replace(/\D/g,''),descricao:p.descricao,fotoUrl:p.fotoUrl||null,fotosTrabalhos:p.fotosTrabalhos||[],experienciaAnos:p.experienciaAnos??null,distanciaMaxKm:p.distanciaMaxKm??null,atendeUrgencia:!!p.atendeUrgencia,atendeDomicilio:!!p.atendeDomicilio,atendeEstabelecimento:!!p.atendeEstabelecimento};
            if (!body.nome?.trim() || body.nome.trim().length<3 || body.nome.length>100 || !/^\d{10,11}$/.test(body.telefone) || !body.descricao?.trim() || body.descricao.length>500) throw Error('Confira seu nome, telefone com DDD e apresentação de até 500 caracteres.');
            if (body.distanciaMaxKm!==null && (!Number.isFinite(Number(body.distanciaMaxKm))||Number(body.distanciaMaxKm)<0||Number(body.distanciaMaxKm)>1000)) throw Error('Informe uma distância entre 0 e 1.000 km.');
            if (body.experienciaAnos!==null && (!Number.isInteger(Number(body.experienciaAnos))||Number(body.experienciaAnos)<0||Number(body.experienciaAnos)>80)) throw Error('Informe a experiência em anos, entre 0 e 80.');
            return body;
        }
        async write(route, body, update) { this.requireLoaded(); if (this.saving) throw Error('Aguarde o salvamento em andamento.'); this.saving=true; try { await this.api(route,{method:'PUT',body}); update(); await this.refreshReadiness(); } finally { this.saving=false; } }
        saveInfo(fields) { const body=this.infoPayload(fields); return this.write('/profissionais/me',body,()=>Object.assign(this.profile,clone(body))); }
        saveServices() { const body=this.servicePayload(); return this.write('/profissionais/me/servicos',body,()=>{this.profile.servicos=clone(body);}); }
        saveAreas() { if(!this.areas.length) throw Error('Adicione pelo menos uma cidade onde você atende.'); const body=clone(this.areas); return this.write('/profissionais/me/areas',body,()=>{this.profile.areas=clone(body);}); }
        saveAvailability(rows) { const body=clone(validateIntervals(rows)); return this.write('/profissionais/me/disponibilidade',body,()=>{this.profile.disponibilidade=clone(body);}); }
        async saveContact(fields) { if(!this.contact) throw Error('As preferências de contato não carregaram. Recarregue o perfil antes de alterá-las.'); const body={telefone:this.profile.telefone,whatsAppAtivo:!!fields.whatsAppAtivo,mensagemInicialWhatsApp:fields.mensagemInicialWhatsApp?.trim()||null}; await this.write('/whatsapp/contato/me',body,()=>{this.contact=clone(body);}); }
        missing() { const p=this.profile; const active=p.servicos.filter(s=>this.categories.some(c=>Number(c.id)===Number(s.categoriaId))); const result=[]; if(!p.descricao?.trim())result.push('Escreva uma apresentação curta.'); if(!active.length)result.push('Escolha os serviços que você realiza.'); if(active.some(s=>s.faixaPrecoMin==null||s.faixaPrecoMax==null))result.push('Complete os preços dos serviços: a regra atual exige mínimo e máximo para concluir.'); if(!p.areas.length)result.push('Informe onde você atende.'); if(!(p.distanciaMaxKm>0))result.push('Informe uma distância de atendimento maior que zero.'); if(!p.atendeDomicilio&&!p.atendeEstabelecimento)result.push('Informe se atende em domicílio ou em estabelecimento.'); return result; }
        canReceive() { return this.loaded && this.missing().length===0 && this.readiness?.cadastroCompleto===true && this.profile.verificado===true; }
    }
    window.ChamaproProfessionalData = { Store, validateIntervals, applyCommon };
})();
