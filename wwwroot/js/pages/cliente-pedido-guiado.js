// Presentation only: controls stay mounted so files, values and validation survive navigation.
(() => {
    const form = document.getElementById('createOrderForm');
    if (!form) return;
    const steps = [...form.querySelectorAll('[data-service-step]')];
    const progress = [...form.querySelectorAll('[data-service-progress]')];
    const submit = form.querySelector('[data-service-submit]');
    function explainInvalid(field) {
        const message = document.getElementById('formMessage');
        if (message) {
            message.textContent = 'Confira o campo “' + (field.labels?.[0]?.textContent || 'obrigatório') + '” para continuar.';
            message.style.color = 'var(--color-danger)';
        }
    }
    function show(index, focus = true) {
        steps.forEach((step, i) => { step.hidden = i !== index; });
        progress.forEach((item, i) => {
            if (i === index) item.setAttribute('aria-current', 'step'); else item.removeAttribute('aria-current');
            item.classList.toggle('is-complete', i < index);
        });
        submit.hidden = index !== steps.length - 1;
        form.dispatchEvent?.(new CustomEvent('chamapro:stepchange', { detail: index }));
        if (focus) {
            const message = document.getElementById('formMessage');
            if (message) message.textContent = '';
            steps[index].focus();
        }
    }
    form.querySelectorAll('[data-service-next]').forEach(button => button.addEventListener('click', () => {
        const current = button.closest('[data-service-step]');
        const invalid = [...current.querySelectorAll('input, select, textarea')].find(field => !field.validity.valid);
        if (invalid) { explainInvalid(invalid); invalid.reportValidity(); return; }
        show(Number(button.dataset.serviceNext));
    }));
    form.querySelectorAll('[data-service-back]').forEach(button => button.addEventListener('click', () => show(Number(button.dataset.serviceBack))));
    // Full native form validation can also point to an earlier step (including Enter submission).
    form.noValidate = true;
    form.addEventListener('submit', event => {
        const invalid = [...form.querySelectorAll('input, select, textarea')].find(field => !field.validity.valid);
        if (!invalid) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const step = invalid.closest('[data-service-step]');
        if (step) show(steps.indexOf(step), false);
        explainInvalid(invalid);
        invalid.reportValidity();
    }, true);
    form.addEventListener('invalid', event => {
        const step = event.target.closest('[data-service-step]');
        if (step) show(steps.indexOf(step), false);
    }, true);
    form.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.target.tagName !== 'INPUT') return;
        const current = event.target.closest('[data-service-step]');
        const next = current?.querySelector('[data-service-next]');
        if (next) { event.preventDefault(); next.click(); }
    });

    show(0, false);
})();
