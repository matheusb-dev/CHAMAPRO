// One row for every saved interval. The existing controls and replacement API contract stay intact.
(() => {
    const days = ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'];
    const originals = new WeakMap();
    const displayTime = time => String(time).slice(0, 8);
    const clock = time => String(time).split(':').reduce((total, part, index) => total + Number(part) * [3600, 60, 1][index], 0);
    function render(container, intervals) {
        const saved = new Map();
        container.innerHTML = days.map((day, dayIndex) => {
            const matches = intervals.filter(item => Number(item.diaSemana) === dayIndex);
            return (matches.length ? matches : [null]).map((item, index) => {
                const key = `${dayIndex}-${index}`;
                const start = item ? displayTime(item.horaInicio) : '09:00';
                const end = item ? displayTime(item.horaFim) : '17:00';
                if (item) saved.set(key, { ...item, displayStart: start, displayEnd: end });
                const disabled = item ? '' : ' disabled';
                const label = day + (matches.length > 1 ? ` · intervalo ${index + 1}` : '');
                return `<div class="availability-row"><label><input type="checkbox" data-day-enabled="${key}" data-day-week="${dayIndex}"${item ? ' checked' : ''}> ${label}</label><input type="time" step="1" aria-label="Início ${label}" data-day-start="${key}" value="${start}"${disabled}><input type="time" step="1" aria-label="Fim ${label}" data-day-end="${key}" value="${end}"${disabled}><label><input type="checkbox" data-day-immediate="${key}"${item?.atendeImediato ? ' checked' : ''}${disabled}> Imediato</label><label><input type="checkbox" data-day-scheduled="${key}"${item?.atendeAgendado !== false ? ' checked' : ''}${disabled}> Agendado</label></div>`;
            }).join('');
        }).join('');
        originals.set(container, saved);
        container.querySelectorAll('[data-day-enabled]').forEach(toggle => toggle.addEventListener('change', () => {
            for (const part of ['start', 'end', 'immediate', 'scheduled']) container.querySelector(`[data-day-${part}="${toggle.dataset.dayEnabled}"]`).disabled = !toggle.checked;
        }));
    }
    function read(container) {
        if (!originals.has(container)) throw new Error('Aguarde o carregamento dos horários antes de salvar.');
        const saved = originals.get(container);
        const result = [...container.querySelectorAll('[data-day-enabled]:checked')].map(toggle => {
            const key = toggle.dataset.dayEnabled;
            const original = saved.get(key);
            const start = container.querySelector(`[data-day-start="${key}"]`);
            const end = container.querySelector(`[data-day-end="${key}"]`);
            const immediate = container.querySelector(`[data-day-immediate="${key}"]`).checked;
            const scheduled = container.querySelector(`[data-day-scheduled="${key}"]`).checked;
            if (!start.validity.valid || !end.validity.valid || !start.value || !end.value || clock(end.value) <= clock(start.value) || (!immediate && !scheduled)) throw new Error('Confira os horários e escolha imediato ou agendado para cada intervalo ativo.');
            return { diaSemana: Number(toggle.dataset.dayWeek), horaInicio: original && clock(start.value) === clock(original.displayStart) ? original.horaInicio : start.value, horaFim: original && clock(end.value) === clock(original.displayEnd) ? original.horaFim : end.value, atendeImediato: immediate, atendeAgendado: scheduled };
        });
        if (result.length > 42) throw new Error('Informe até 42 intervalos de disponibilidade.');
        for (let day = 0; day < 7; day++) {
            const rows = result.filter(item => item.diaSemana === day).sort((a, b) => clock(a.horaInicio) - clock(b.horaInicio));
            if (rows.some((row, index) => index > 0 && clock(rows[index - 1].horaFim) > clock(row.horaInicio))) throw new Error('Os intervalos de um mesmo dia não podem se sobrepor.');
        }
        return result;
    }
    window.ChamaproAvailability = { render, read };
})();
