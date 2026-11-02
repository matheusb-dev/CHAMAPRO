/* Availability UI layered on the interval-preserving renderer from stage 1. */
(() => {
    const days=['Domingo','Segunda','Terça','Quarta','Quinta','Sexta','Sábado'];
    function mount(container, initial) {
        const safe=window.Chamapro.escaparHtml;
        container.innerHTML=`<div class="common-days" role="group" aria-label="Dias para o horário comum">${days.map((day,i)=>`<label class="day-choice"><input type="checkbox" data-common-day="${i}"><span>${day}</span></label>`).join('')}</div><div class="app-form-grid"><label>Das<input type="time" data-common-start value="08:00"></label><label>Até<input type="time" data-common-end value="18:00"></label></div><label><input type="checkbox" data-common-immediate> Aceito pedidos urgentes nesse horário</label><label><input type="checkbox" data-common-scheduled checked> Aceito pedidos agendados</label><button type="button" class="btn btn-secondary" data-apply-common>Aplicar horário aos dias selecionados</button><p data-hours-message role="status" class="app-message"></p><div data-hours-summary class="hours-summary" aria-live="polite"></div><details data-hours-advanced><summary>Personalizar dias e intervalos</summary><p class="form-hint">Todos os intervalos existentes estão abaixo. Desmarque um intervalo para removê-lo.</p><div data-hours-grid></div><div class="app-toolbar"><label>Dia<select data-extra-day>${days.map((day,i)=>`<option value="${i}">${day}</option>`).join('')}</select></label><label>Início<input type="time" data-extra-start value="12:00"></label><label>Fim<input type="time" data-extra-end value="13:00"></label><button type="button" class="btn btn-secondary" data-add-interval>Adicionar intervalo agendado</button></div></details>`;
        const $=selector=>container.querySelector(selector), grid=$('[data-hours-grid]'), advanced=$('[data-hours-advanced]');
        const api=window.ChamaproAvailability;
        const message=text=>{ $('[data-hours-message]').textContent=text; };
        const read=()=>api.read(grid);
        function summary() { try { const rows=read(); $('[data-hours-summary]').innerHTML=rows.length?days.map((day,i)=>{const values=rows.filter(r=>Number(r.diaSemana)===i);return values.length?`<p><strong>${day}:</strong> ${values.map(r=>safe(r.horaInicio.slice(0,8))+'–'+safe(r.horaFim.slice(0,8))+(r.atendeImediato?' · urgente':'')+(r.atendeAgendado?' · agendado':'')).join('; ')}</p>`:'';}).join(''):'<p class="app-muted">Nenhum horário definido ainda.</p>'; } catch(error) { message(error.message); } }
        function render(rows) { api.render(grid,rows); summary(); }
        render(initial);
        advanced.open=days.some((_,i)=>initial.filter(r=>Number(r.diaSemana)===i).length>1);
        grid.addEventListener('input',summary);grid.addEventListener('change',summary);
        $('[data-apply-common]').addEventListener('click',()=>{
            try {
                const selected=[...container.querySelectorAll('[data-common-day]:checked')].map(e=>Number(e.dataset.commonDay));
                const interval={horaInicio:$('[data-common-start]').value,horaFim:$('[data-common-end]').value,atendeImediato:$('[data-common-immediate]').checked,atendeAgendado:$('[data-common-scheduled]').checked};
                let result=window.ChamaproProfessionalData.applyCommon(read(),selected,interval);
                if(result.needsConfirmation) { if(!window.confirm('Os horários existentes nos dias selecionados serão substituídos. Outros dias serão mantidos. Deseja continuar?'))return;result=window.ChamaproProfessionalData.applyCommon(read(),selected,interval,true); }
                render(result.intervals);message('Horário aplicado. Salve a seção para confirmar as alterações.');
            }catch(error){message(error.message);}
        });
        $('[data-add-interval]').addEventListener('click',()=>{
            try { const rows=[...read(),{diaSemana:Number($('[data-extra-day]').value),horaInicio:$('[data-extra-start]').value,horaFim:$('[data-extra-end]').value,atendeImediato:false,atendeAgendado:true}];window.ChamaproProfessionalData.validateIntervals(rows);render(rows);advanced.open=true;message('Intervalo adicionado. Salve os horários para confirmar.'); }catch(error){message(error.message);}
        });
        return {read};
    }
    window.ChamaproAvailabilityEditor={mount};
})();
