const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8');

function guidedHarness() {
    const events = {};
    const progress = Array.from({ length: 3 }, () => ({ setAttribute() {}, removeAttribute() {}, classList: { toggle() {} } }));
    const file = { name: 'servico.jpg' };
    const fields = Array.from({ length: 3 }, (_, i) => ({ value: `etapa ${i}`, files: i === 2 ? [file] : [], validity: { valid: true }, reportValidity() { this.reported = true; }, closest() { return steps[i]; } }));
    const steps = fields.map(field => ({ hidden: false, focused: false, focus() { this.focused = true; }, querySelectorAll: () => [field] }));
    const buttons = kind => Array.from({ length: 2 }, (_, i) => {
        const step = kind === 'next' ? i : i + 1;
        return { dataset: kind === 'next' ? { serviceNext: String(step + 1) } : { serviceBack: String(step - 1) }, addEventListener(_name, fn) { this.click = fn; }, closest: () => steps[step] };
    });
    const next = buttons('next'), back = buttons('back'), submit = {}, edit = { addEventListener(_name, fn) { this.click = fn; } };
    const form = { querySelector: () => submit, querySelectorAll(selector) {
        return ({ '[data-service-step]': steps, '[data-service-progress]': progress, '[data-service-next]': next, '[data-service-back]': back, 'input, select, textarea': fields })[selector];
    }, addEventListener(name, fn) { events[name] = fn; } };
    const context = { document: { getElementById: id => id === 'createOrderForm' ? form : id === 'backToEdit' ? edit : null } };
    vm.createContext(context);require('./helpers/api.cjs').install(context);
    vm.runInContext(read('wwwroot/js/pages/cliente-pedido-guiado.js'), context);
    return { fields, steps, next, back, submit, events, file };
}

test('guided order validates each step and keeps the same values and attachment objects', () => {
    const h = guidedHarness();
    assert.deepEqual(h.steps.map(s => s.hidden), [false, true, true]);
    h.fields[0].validity.valid = false;
    h.next[0].click();
    assert.equal(h.fields[0].reported, true);
    assert.equal(h.steps[1].hidden, true);
    h.fields[0].validity.valid = true;
    h.next[0].click(); h.next[1].click();
    assert.equal(h.submit.hidden, false);
    h.back[1].click(); h.back[0].click();
    assert.equal(h.fields[0].value, 'etapa 0');
    assert.equal(h.fields[2].files[0], h.file);
});

test('full submission reveals the first invalid field before native validation', () => {
    const h = guidedHarness(); h.next[0].click(); h.next[1].click();
    h.fields[0].validity.valid = false;
    let prevented = false, stopped = false;
    h.events.submit({ preventDefault() { prevented = true; }, stopImmediatePropagation() { stopped = true; } });
    assert.ok(prevented && stopped);
    assert.deepEqual(h.steps.map(s => s.hidden), [false, true, true]);
    assert.ok(h.fields[0].reported);
});

test('order confirmation ignores duplicate clicks while the actual API request is pending', async () => {
    const elements = new Map();
    for (const id of ['createOrderForm', 'category', 'serviceType', 'desiredDateGroup', 'professionalPicker', 'reviewPanel', 'searchProfessionals', 'desiredDate', 'title', 'description', 'city', 'neighborhood', 'address', 'reviewSummary', 'formMessage', 'backToEdit', 'confirmOrder', 'photos']) elements.set(id, { value: '', files: [], style: {}, handlers: {}, addEventListener(name, fn) { this.handlers[name] = fn; }, reportValidity: () => true, scrollIntoView() {}, selectedOptions: [{ text: 'Categoria' }] });
    Object.assign(elements.get('category'), { value: '1' });
    elements.get('serviceType').value = 'IMEDIATO';
    elements.get('city').value = 'Cidade';
    elements.get('title').value = 'Serviço de teste';
    elements.get('description').value = 'Descrição do serviço';
    let calls = 0;
    const document = { getElementById: id => elements.get(id) || null, querySelectorAll: () => [], querySelector: () => ({ value: 'auto' }) };
    const context = { document, URLSearchParams, FormData, location: { assign() {} }, window: { Chamapro: { escaparHtml: v => v, api: async route => { if (route === '/categorias') return []; if (route === '/pedidos') { calls++; await new Promise(() => {}); } }, toast() {} }, ChamaproApp: { ready: Promise.resolve({ perfil: 'CLIENTE' }) } } };
    vm.createContext(context);require('./helpers/api.cjs').install(context); vm.runInContext(read('wwwroot/js/pages/cliente.js'), context);
    await new Promise(resolve => setImmediate(resolve));
    elements.get('createOrderForm').handlers.submit({ preventDefault() {} });
    elements.get('confirmOrder').handlers.click();
    elements.get('confirmOrder').handlers.click();
    assert.equal(calls, 1);
    assert.equal(elements.get('confirmOrder').disabled, true);
    assert.equal(elements.get('createOrderForm').inert, true);
});

test('favorites uses the existing API and has its own menu destination', () => {
    assert.ok(read('wwwroot/js/components/sidebar.js').includes("['Meus profissionais','/pages/cliente/favoritos.html']"));
    const page = read('wwwroot/pages/cliente/favoritos.html');
    assert.ok(page.includes('/js/pages/cliente-favoritos.js?'));
    assert.ok(!page.includes('id="ordersList"'));
    assert.match(read('wwwroot/js/pages/cliente-favoritos.js'), /C\.api\('\/favoritos'\)/);
});

test('favorites loads and removes a saved professional using the existing endpoint', async () => {
    const calls = [], handlers = {};
    const button = { dataset: { unfavorite: '12' }, addEventListener: (name, fn) => { handlers[name] = fn; } };
    const target = { innerHTML: '', setAttribute() {}, querySelectorAll: () => [button] };
    const context = { document: { getElementById: () => target }, window: { ChamaproApp: { ready: Promise.resolve({ perfil: 'CLIENTE' }) }, Chamapro: {
        escaparHtml: s => s, formatarNota: n => String(n), toast() {}, api: async (route, options) => {
            calls.push([route, options?.method || 'GET']);
            return options?.method === 'DELETE' ? null : [{ profissionalId: 12, nome: 'Profissional de teste', notaMedia: 4 }];
        }
    } } };
    vm.createContext(context);require('./helpers/api.cjs').install(context); vm.runInContext(read('wwwroot/js/pages/cliente-favoritos.js'), context);
    await new Promise(resolve => setImmediate(resolve));
    assert.match(target.innerHTML, /perfil-profissional.html\?id=12/);
    await handlers.click();
    assert.deepEqual(calls, [['/favoritos', 'GET'], ['/favoritos/12', 'DELETE'], ['/favoritos', 'GET']]);
});
