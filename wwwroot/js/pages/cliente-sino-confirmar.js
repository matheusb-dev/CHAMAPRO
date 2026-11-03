// Sino de confirmação: o cliente segura o sino por 5 segundos para enviar o pedido.
// A lógica de envio continua no botão original (#confirmOrder, em cliente.js):
// o sino só o aciona depois da contagem, então validação, upload das fotos e erros não mudam.
(() => {
    const original = document.getElementById('confirmOrder');
    if (!original) return;

    const HOLD_MS = 5000;
    const RADIUS = 44, CIRCUMFERENCE = 2 * Math.PI * RADIUS;
    const IDLE_HINT = 'Segure o sino por 5 segundos';

    const wrap = document.createElement('div');
    wrap.className = 'hold-bell-wrap';
    wrap.dataset.state = 'idle';
    wrap.innerHTML = `
        <button type="button" class="hold-bell" aria-label="Confirmar pedido. Segure por 5 segundos." aria-describedby="holdBellHint">
            <svg class="hold-ring" viewBox="0 0 100 100" aria-hidden="true" focusable="false">
                <circle class="ring-track" cx="50" cy="50" r="${RADIUS}"/>
                <circle class="ring-fill" cx="50" cy="50" r="${RADIUS}" stroke-dasharray="${CIRCUMFERENCE}" stroke-dashoffset="${CIRCUMFERENCE}"/>
            </svg>
            <svg class="hold-bell-icon" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
                <g class="bell-swing">
                    <circle class="bell-top" cx="32" cy="8" r="3.5"/>
                    <path class="bell-body" d="M32 10c-8.3 0-15 6.7-15 15v10l-5 7h40l-5-7V25c0-8.3-6.7-15-15-15Z"/>
                    <path class="bell-shine" d="M23 24c0-4.5 3.4-8 8-8.8" />
                    <circle class="bell-clapper" cx="32" cy="49" r="4.5"/>
                </g>
            </svg>
        </button>
        <div class="hold-bell-text"><strong id="holdBellTitle">Confirmar pedido</strong><span id="holdBellHint">${IDLE_HINT}</span></div>
        <span class="hold-bell-live" role="status"></span>`;
    original.insertAdjacentElement('beforebegin', wrap);
    // O botão original continua no DOM (é ele que envia), mas quem o usuário vê é o sino.
    original.style.display = 'none';
    original.setAttribute('aria-hidden', 'true');
    original.tabIndex = -1;

    const bell = wrap.querySelector('.hold-bell');
    const ring = wrap.querySelector('.ring-fill');
    const title = wrap.querySelector('#holdBellTitle');
    const hint = wrap.querySelector('#holdBellHint');
    const live = wrap.querySelector('.hold-bell-live');

    let active = false, done = false, source = null, startedAt = 0, frame = 0;

    // Feedback tátil: vibra em pulsos que ficam mais longos e mais juntos até os 5 segundos.
    // Funciona em celulares com suporte (ex.: Chrome no Android); em PC e iPhone o navegador não vibra.
    const vibrate = pattern => { try { navigator.vibrate?.(pattern); } catch { /* sem suporte */ } };
    const rumble = () => {
        const pattern = [];
        for (let t = 0; t < HOLD_MS;) {
            const progress = t / HOLD_MS, on = Math.round(10 + progress * 40), off = Math.round(180 - progress * 140);
            pattern.push(on, off); t += on + off;
        }
        return pattern;
    };

    function paint(progress) {
        ring.style.strokeDashoffset = String(CIRCUMFERENCE * (1 - progress));
        const level = Math.min(4, Math.floor(progress * 5));
        wrap.style.setProperty('--p', progress.toFixed(3));
        wrap.style.setProperty('--amp', String(4 + level * 4));          // o sino balança cada vez mais forte
        wrap.style.setProperty('--dur', (0.3 - level * 0.04).toFixed(2) + 's'); // e mais rápido
    }
    function setIdle(message) {
        active = false; done = false; source = null;
        cancelAnimationFrame(frame);
        wrap.dataset.state = 'idle';
        title.textContent = 'Confirmar pedido'; hint.textContent = IDLE_HINT;
        paint(0);
        if (message) live.textContent = message;
    }
    function tick(now) {
        if (!active) return;
        const progress = Math.min(1, (now - startedAt) / HOLD_MS);
        paint(progress);
        hint.textContent = 'Continue segurando… ' + Math.max(1, Math.ceil((1 - progress) * 5)) + 's';
        if (progress >= 1) complete(); else frame = requestAnimationFrame(tick);
    }
    function start(by) {
        if (active || done || original.disabled) return;
        active = true; source = by; startedAt = performance.now();
        wrap.dataset.state = 'holding';
        live.textContent = 'Segurando. Continue por 5 segundos para confirmar.';
        vibrate(rumble());
        frame = requestAnimationFrame(tick);
    }
    function cancel() {
        if (!active) return;
        vibrate(0);
        setIdle('Cancelado. Segure por 5 segundos para confirmar.');
    }
    function complete() {
        active = false; done = true;
        vibrate([70, 40, 160]); // "toc" final de confirmação
        wrap.dataset.state = 'sending';
        paint(1);
        title.textContent = 'Enviando pedido…'; hint.textContent = '';
        live.textContent = 'Confirmado. Enviando pedido.';
        original.click();
        setTimeout(sync, 0);
    }
    // Se o envio não começou (ex.: faltou um campo) ou falhou, o sino volta ao início.
    function sync() {
        if (original.disabled) { wrap.dataset.state = 'sending'; title.textContent = 'Enviando pedido…'; hint.textContent = ''; }
        else if (done) setIdle();
    }
    new MutationObserver(sync).observe(original, { attributes: true, attributeFilter: ['disabled'] });

    // Mouse, toque e caneta.
    bell.addEventListener('pointerdown', event => {
        if (event.pointerType === 'mouse' && event.button !== 0) return;
        event.preventDefault();
        bell.setPointerCapture?.(event.pointerId);
        start('pointer');
    });
    bell.addEventListener('pointermove', event => {
        if (!active || source !== 'pointer') return;
        const box = bell.getBoundingClientRect(), margin = 14;
        if (event.clientX < box.left - margin || event.clientX > box.right + margin || event.clientY < box.top - margin || event.clientY > box.bottom + margin) cancel();
    });
    for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) bell.addEventListener(type, () => { if (source === 'pointer') cancel(); });
    // Teclado: segurar Espaço ou Enter.
    bell.addEventListener('keydown', event => {
        if (event.key !== ' ' && event.key !== 'Enter') return;
        event.preventDefault();
        if (!event.repeat) start('key');
    });
    bell.addEventListener('keyup', event => {
        if ((event.key === ' ' || event.key === 'Enter') && source === 'key') cancel();
    });
    bell.addEventListener('blur', () => { if (source === 'key') cancel(); });
    document.addEventListener('visibilitychange', () => { if (document.hidden) cancel(); });
    // Um clique rápido não confirma nada: é preciso segurar.
    bell.addEventListener('click', event => { event.preventDefault(); event.stopImmediatePropagation(); });
    bell.addEventListener('contextmenu', event => event.preventDefault());
})();