/* Dependency-free single-choice combobox. Native control remains the API value source. */
(() => {
    const normalize = text => String(text || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR').trim();
    function mount(control, { label, placeholder = 'Pesquisar…' } = {}) {
        const root = document.createElement('div'); root.className = 'chama-combobox';
        const input = document.createElement('input'); input.id = control.id + 'Search'; input.type = 'text'; input.autocomplete = 'off'; input.placeholder = placeholder; input.required = control.required;
        input.setAttribute('role', 'combobox'); input.setAttribute('aria-autocomplete', 'list'); input.setAttribute('aria-expanded', 'false'); input.setAttribute('aria-haspopup', 'listbox');
        const toggle = document.createElement('button'); toggle.type = 'button'; toggle.className = 'combobox-toggle'; toggle.textContent = '⌄'; toggle.setAttribute('aria-label', 'Mostrar opções de ' + label);
        const list = document.createElement('ul'); list.id = control.id + 'Options'; list.className = 'combobox-options'; list.hidden = true; list.setAttribute('role', 'listbox'); list.setAttribute('aria-label', label);
        input.setAttribute('aria-controls', list.id);
        const status = document.createElement('p'); status.id = control.id + 'Status'; status.className = 'form-hint'; status.setAttribute('role', 'status');
        input.setAttribute('aria-describedby', status.id);
        const retry = document.createElement('button'); retry.type = 'button'; retry.className = 'btn btn-secondary btn-small'; retry.textContent = 'Tentar novamente'; retry.hidden = true;
        const fieldLabel = control.labels?.[0]; if (fieldLabel) fieldLabel.htmlFor = input.id;
        control.before(root); root.append(input, toggle, list, status, retry); control.hidden = true;
        let items = [], results = [], selected = null, active = -1, ready = false, retryAction;
        function close() { list.hidden = true; input.setAttribute('aria-expanded', 'false'); input.removeAttribute('aria-activedescendant'); }
        function render(query = '') {
            list.replaceChildren();
            const matches = items.filter(item => normalize(item.label).includes(normalize(query)));
            results = matches.slice(0, 80);
            if (!query && selected && !results.includes(selected)) results = [selected, ...results.slice(0, 79)];
            active = results.findIndex(item => item === selected);
            results.forEach((item, index) => {
                const option = document.createElement('li'); option.id = list.id + '-' + index; option.setAttribute('role', 'option'); option.setAttribute('aria-selected', String(item === selected)); option.textContent = item.label;
                option.addEventListener('pointerdown', event => event.preventDefault()); option.addEventListener('click', () => choose(item)); list.append(option);
            });
            status.textContent = matches.length === 0 ? 'Nenhuma opção encontrada. Tente outro nome.' : matches.length > results.length ? `Mostrando ${results.length} de ${matches.length}. Digite mais para refinar a busca.` : `${matches.length} opções encontradas.`;
        }
        function open(query = '') { if (!ready) return; render(query); list.hidden = false; input.setAttribute('aria-expanded', 'true'); }
        function choose(item, focus = true) {
            selected = item; input.value = item.label; control.value = String(item.payload ?? item.value); input.setCustomValidity('');
            if (control.tagName === 'INPUT') { control.dataset.municipioId = String(item.value); control.dataset.uf = item.uf || ''; }
            close(); status.textContent = item.label + ' selecionado.';
            control.dispatchEvent(new Event('input', { bubbles: true })); control.dispatchEvent(new Event('change', { bubbles: true })); if (focus) { input.focus(); close(); }
        }
        input.addEventListener('focus', () => { if (list.hidden) open(); });
        input.addEventListener('input', () => {
            selected = null; control.value = ''; delete control.dataset.municipioId; delete control.dataset.uf;
            input.setCustomValidity('Escolha uma opção da lista.'); control.dispatchEvent(new Event('input', { bubbles: true })); open(input.value);
        });
        input.addEventListener('keydown', event => {
            if (event.key === 'Escape') { close(); return; }
            if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
                event.preventDefault(); if (list.hidden) open(input.value && !selected ? input.value : '');
                if (!results.length) return;
                active = event.key === 'Home' ? 0 : event.key === 'End' ? results.length - 1 : Math.max(0, Math.min(results.length - 1, active + (event.key === 'ArrowUp' ? -1 : 1)));
                const option = list.children[active]; input.setAttribute('aria-activedescendant', option.id); [...list.children].forEach((node, i) => node.classList.toggle('is-active', i === active)); option.scrollIntoView({ block: 'nearest' });
            }
            if (event.key === 'Enter' && !list.hidden) { event.preventDefault(); event.stopPropagation(); if (active >= 0) choose(results[active]); }
            if (event.key === 'Tab') close();
        });
        toggle.addEventListener('click', () => { const wasOpen = !list.hidden; input.focus(); if (wasOpen) close(); else open(); });
        root.addEventListener('focusout', event => { if (!root.contains(event.relatedTarget)) close(); });
        document.addEventListener('pointerdown', event => { if (!root.contains(event.target)) close(); });
        control.addEventListener('invalid', event => { event.preventDefault(); input.setCustomValidity('Escolha uma opção da lista.'); input.focus(); input.reportValidity(); });
        retry.addEventListener('click', () => retryAction?.());
        return {
            input,
            get selection() { return selected; },
            setItems(next) { const oldValue = selected?.value; items = next; ready = true; input.readOnly = false; toggle.disabled = false; retry.hidden = true; close(); if (oldValue !== undefined) { const item = items.find(item => String(item.value) === String(oldValue)); if (item) choose(item, false); else { selected = null; control.value = ''; input.value = ''; input.setCustomValidity('Escolha uma opção da lista.'); } } status.textContent = items.length ? 'Digite para pesquisar ou abra a lista.' : 'Nenhuma opção disponível.'; },
            setState(state, message, action) { ready = state === 'ready'; input.readOnly = !ready; toggle.disabled = !ready; status.textContent = message; retryAction = action; retry.hidden = state !== 'error'; close(); },
            select(value) { const item = items.find(item => String(item.value) === String(value)); if (item) choose(item, false); return Boolean(item); },
            search(text) { input.value = text; open(text); }
        };
    }
    window.ChamaproCombobox = { mount, normalize };
})();
