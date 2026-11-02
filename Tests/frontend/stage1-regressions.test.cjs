const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = name => fs.readFileSync(path.resolve(__dirname, '../../wwwroot/js', name), 'utf8');
const plain = value => JSON.parse(JSON.stringify(value));
const tick = () => new Promise(resolve => setImmediate(resolve));
const escape = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const camel = name => name.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
class Element {
    constructor(attrs = '') {
        this.attrs = {}; this.dataset = {}; this.events = {}; this.children = [];
        this.value = ''; this.files = []; this.style = {}; this.validity = { valid: true };
        this.classList = { toggle() {} }; this.selectedOptions = [{ text: 'Categoria' }];
        for (const [, name, value] of attrs.matchAll(/([\w-]+)(?:="([^"]*)")?/g)) {
            this.attrs[name] = value ?? true;
            if (name.startsWith('data-')) this.dataset[camel(name.slice(5))] = value;
            if (name === 'value') this.value = value;
        }
        this.checked = 'checked' in this.attrs; this.disabled = 'disabled' in this.attrs;
    }
    set innerHTML(html) {
        this.html = html;
        this.options = [...html.matchAll(/<option\b([^>]*)>/g)].map(m => new Element(m[1]));
        this.children = [...html.matchAll(/<(?:input|button)\b([^>]*)>/g)].map(m => new Element(m[1]));
    }
    get innerHTML() { return this.html || ''; }
    querySelectorAll(selector) { return this.children.filter(el => matches(el, selector)); }
    querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
    addEventListener(name, fn) { (this.events[name] ||= []).push(fn); }
    async emit(name, event = {}) { await Promise.all((this.events[name] || []).map(fn => fn(event))); }
    setAttribute(name, value) { this.attrs[name] = value; }
    removeAttribute(name) { delete this.attrs[name]; }
    reportValidity() { return true; }
    scrollIntoView() {}
    insertAdjacentHTML(_where, html) { this.html += html; }
}
function matches(el, selector) {
    if (selector.startsWith('#')) return el.attrs.id === selector.slice(1);
    if (selector.endsWith(':checked') && !el.checked) return false;
    const attrs = [...selector.matchAll(/\[([\w-]+)(?:="([^"]*)")?\]/g)];
    return attrs.length > 0 && attrs.every(([, name, value]) => name in el.attrs && (value === undefined || el.attrs[name] === value));
}
function environment(ids, role, api) {
    const elements = new Map(ids.map(id => [id, new Element()]));
    const all = () => [...elements.values()].flatMap(el => [el, ...el.children]);
    const document = { getElementById: id => elements.get(id) || null, querySelectorAll: selector => all().filter(el => matches(el, selector)), querySelector: selector => all().find(el => matches(el, selector)) || null };
    const context = { document, URLSearchParams, FormData, location: { search: '?pedidoId=1', assign() {}, reload() {} }, confirm: () => true, setTimeout, window: { scrollTo() {}, ChamaproApp: { ready: Promise.resolve({ perfil: role }) }, Chamapro: { api, escaparHtml: escape, formatarData: () => '', formatarNota: String, formatarMoeda: String, toast() {} } } };
    vm.createContext(context);
    return { elements, document, context, run: name => vm.runInContext(source(name), context) };
}

const intervals = [
    { diaSemana: 1, horaInicio: '08:00:00', horaFim: '12:00:00', atendeImediato: true, atendeAgendado: false },
    { diaSemana: 1, horaInicio: '13:00:15', horaFim: '18:00:30', atendeImediato: false, atendeAgendado: true },
    { diaSemana: 3, horaInicio: '09:00:00', horaFim: '17:00:00', atendeImediato: true, atendeAgendado: true }
];
test('all intervals and their flags/seconds survive render and save; edits affect only one row', () => {
    const env = environment([], '', async () => {}); env.run('components/availability.js');
    const grid = new Element(); const availability = env.context.window.ChamaproAvailability;
    availability.render(grid, intervals);
    assert.deepEqual(plain(availability.read(grid)), intervals);
    const time = grid.querySelector('[data-day-start="1-1"]'); time.value = '14:00:15';
    const payload = plain(availability.read(grid));
    assert.equal(payload.length, 3); assert.equal(payload[1].horaInicio, '14:00:15');
    assert.deepEqual(payload[0], intervals[0]); assert.deepEqual(payload[2], intervals[2]);
});
test('invalid/overlapping intervals are rejected and an explicit uncheck removes only that interval', () => {
    const env = environment([], '', async () => {}); env.run('components/availability.js');
    const grid = new Element(), availability = env.context.window.ChamaproAvailability;
    availability.render(grid, intervals);
    grid.querySelector('[data-day-start="1-1"]').value = '11:00';
    assert.throws(() => availability.read(grid), /sobrepor/);
    grid.querySelector('[data-day-start="1-1"]').value = '13:00:15';
    grid.querySelector('[data-day-enabled="1-0"]').checked = false;
    assert.deepEqual(plain(availability.read(grid)), intervals.slice(1));
    grid.querySelector('[data-day-scheduled="1-1"]').checked = false;
    assert.throws(() => availability.read(grid), /intervalo ativo/);
});

async function sharedProfessional() {
    const calls = [];
    const profile = { nome:'Teste', telefone:'11999999999', descricao:'Descrição profissional', servicos:[{categoriaId:1,faixaPrecoMin:0,faixaPrecoMax:250}], areas:[{cidade:'Cidade',bairro:null}], disponibilidade:intervals, fotosTrabalhos:[], distanciaMaxKm:20, atendeDomicilio:true, atendeEstabelecimento:false, verificado:true };
    const env = environment([], 'PROFISSIONAL', async (route, options) => {
        if(options) { calls.push([route, plain(options.body)]); return; }
        if(route==='/profissionais/me')return plain(profile);
        if(route==='/categorias')return [{id:1,nome:'Serviço'},{id:2,nome:'Outro'}];
        if(route==='/painel/profissional')return {cadastroCompleto:true};
        return {};
    });
    env.run('components/professional-data.js');
    const store = new env.context.window.ChamaproProfessionalData.Store(env.context.window.Chamapro.api);
    await store.load(); return { ...env, store, calls };
}
test('saving profession preserves saved prices including zero; new categories alone start with null prices', async () => {
    const env=await sharedProfessional();env.store.addCategory(2);await env.store.saveServices();
    assert.deepEqual(env.calls[0][1],[{categoriaId:1,faixaPrecoMin:0,faixaPrecoMax:250},{categoriaId:2,faixaPrecoMin:null,faixaPrecoMax:null}]);
});
test('unsaved price edits survive category rerender and saving an earlier step; schedule save sends every interval', async () => {
    const env=await sharedProfessional();env.store.prices.set(1,{faixaPrecoMin:'30',faixaPrecoMax:'300'});env.store.addCategory(2);env.store.removeCategory(2);
    await env.store.saveInfo({descricao:'Apresentação atualizada'});assert.equal(env.store.prices.get(1).faixaPrecoMin,'30');
    await env.store.saveServices();await env.store.saveAvailability(intervals);
    assert.deepEqual(env.calls.find(([route])=>route==='/profissionais/me/servicos')[1],[{categoriaId:1,faixaPrecoMin:30,faixaPrecoMax:300}]);
    assert.deepEqual(env.calls.find(([route])=>route==='/profissionais/me/disponibilidade')[1],intervals);
});

const orderIds = ['createOrderForm','category','serviceType','desiredDateGroup','professionalPicker','reviewPanel','searchProfessionals','professionalList','desiredDate','title','description','city','neighborhood','address','reviewSummary','formMessage','backToEdit','confirmOrder','photos','distribution'];
async function orderHarness() {
    const calls = [], env = environment(orderIds, 'CLIENTE', async (route, options) => {
        calls.push([route, options?.body ? plain(options.body) : null]);
        if (route === '/categorias') return [];
        if (route.startsWith('/profissionais?')) return { itens: [{ id: 1, nome: 'Verificado', verificado: true, notaMedia: 4 }, { id: 2, nome: 'Não verificado', verificado: false }] };
        return { id: 1 };
    });
    env.elements.get('distribution').innerHTML = '<input name="distribution" value="auto" checked><input name="distribution" value="manual">';
    for (const [id, value] of Object.entries({ category: '1', serviceType: 'IMEDIATO', title: 'Serviço de teste', description: 'Descrição do serviço', city: 'Cidade' })) env.elements.get(id).value = value;
    env.run('pages/cliente.js'); await tick();
    return { ...env, calls };
}
test('manual search requires verification and excludes unverified options; empty results explain availability', async () => {
    const env = await orderHarness(); await env.elements.get('searchProfessionals').emit('click');
    const query = new URL('http://localhost' + env.calls.find(([route]) => route.startsWith('/profissionais?'))[0]);
    assert.equal(query.searchParams.get('verificado'), 'true');
    assert.equal(query.searchParams.get('categoriaId'), '1');
    assert.equal(query.searchParams.get('cidade'), 'Cidade');
    assert.equal(query.searchParams.get('tipoAtendimento'), 'IMEDIATO');
    assert.match(env.elements.get('professionalList').innerHTML, /Verificado/);
    assert.doesNotMatch(env.elements.get('professionalList').innerHTML, /Não verificado/);
    env.context.window.Chamapro.api = async () => ({ itens: [] });
    await env.elements.get('searchProfessionals').emit('click');
    assert.match(env.elements.get('professionalList').innerHTML, /categoria, região e horário/);
});
test('repeated manual/automatic toggles never mix automatic distribution and selected IDs', async () => {
    for (const mode of ['manual', 'auto', 'manual', 'auto']) {
        const fresh = await orderHarness(); const [a, m] = fresh.elements.get('distribution').children;
        m.checked = true; a.checked = false;
        await m.emit('change'); await fresh.elements.get('searchProfessionals').emit('click');
        fresh.elements.get('professionalList').children[0].checked = true;
        for (const selected of ['auto', 'manual', 'auto', mode]) { a.checked = selected === 'auto'; m.checked = selected === 'manual'; await (a.checked ? a : m).emit('change'); }
        await fresh.elements.get('searchProfessionals').emit('click');
        fresh.elements.get('professionalList').children[0].checked = true;
        await fresh.elements.get('createOrderForm').emit('submit', { preventDefault() {} }); await fresh.elements.get('confirmOrder').emit('click');
        const payload = fresh.calls.filter(([route]) => route === '/pedidos').at(-1)[1];
        assert.deepEqual(payload.profissionalIds, mode === 'auto' ? [] : [1]);
        assert.equal(payload.distribuirParaCompativeis, mode === 'auto');
    }
});

test('quotes distinguish loading, API error, retry and a genuinely empty response', async () => {
    const ids = ['orderContent','orderActions','quotesSection','orderHistory','scheduleForm','reviewForm','scheduleBox','reviewBox'];
    let rejectFirst, resolveRetry, requests = 0;
    const env = environment(ids, 'CLIENTE', async route => {
        if (route === '/pedidos/1') return { titulo: 'Teste', status: 'PENDENTE', cidade: 'Cidade' };
        if (route.endsWith('/orcamentos')) { requests++; return new Promise((resolve, reject) => { if (requests === 1) rejectFirst = reject; else resolveRetry = resolve; }); }
        return [];
    });
    env.run('pages/cliente-orcamentos.js'); await tick();
    const quotes = env.elements.get('quotesSection');
    assert.match(quotes.innerHTML, /Carregando propostas/);
    rejectFirst(new Error('Falha de rede')); await tick();
    assert.match(quotes.innerHTML, /Não foi possível carregar/);
    assert.doesNotMatch(quotes.innerHTML, /Nenhum orçamento/);
    const retry = quotes.querySelector('#retryQuotes'); const pending = retry.emit('click'); await tick();
    assert.match(quotes.innerHTML, /Carregando propostas/);
    resolveRetry([]); await pending;
    assert.match(quotes.innerHTML, /Nenhum orçamento/);
    assert.equal(quotes.attrs['aria-busy'], 'false');
});

test('the profile editor submits every saved interval and rejects overlap before any PUT', async () => {
    const env=await sharedProfessional();await env.store.saveAvailability(intervals);assert.deepEqual(env.calls[0][1],intervals);env.calls.length=0;
    const invalid=plain(intervals);invalid[1].horaInicio='11:00';assert.throws(()=>env.store.saveAvailability(invalid),/sobrepor/);assert.equal(env.calls.length,0);
});

test('changing criteria invalidates selected professionals and ignores a late search response', async () => {
    const env = await orderHarness();
    const [auto, manual] = env.elements.get('distribution').children;
    auto.checked = false; manual.checked = true; await manual.emit('change');
    await env.elements.get('searchProfessionals').emit('click');
    env.elements.get('professionalList').children[0].checked = true;
    auto.checked = true; manual.checked = false; await auto.emit('change');
    assert.equal(env.elements.get('professionalList').children.length, 0);
    auto.checked = false; manual.checked = true; await manual.emit('change');
    let resolve;
    env.context.window.Chamapro.api = () => new Promise(done => { resolve = done; });
    const pending = env.elements.get('searchProfessionals').emit('click');
    env.elements.get('city').value = 'Outra cidade';
    await env.elements.get('city').emit('input');
    resolve({ itens: [{ id: 1, nome: 'Resposta antiga', verificado: true }] }); await pending;
    assert.equal(env.elements.get('professionalList').children.length, 0);
    assert.doesNotMatch(env.elements.get('professionalList').innerHTML, /Resposta antiga/);
});

test('availability cannot be saved before its data has loaded', () => {
    const env = environment([], '', async () => {}); env.run('components/availability.js');
    assert.throws(() => env.context.window.ChamaproAvailability.read(new Element()), /carregamento/);
});

test('repeated saves of the profession step do not reset existing price ranges', async () => {
    const env=await sharedProfessional();await env.store.saveServices();await env.store.saveServices();assert.equal(env.calls.length,2);
    for(const [,body] of env.calls)assert.deepEqual(body,[{categoriaId:1,faixaPrecoMin:0,faixaPrecoMax:250}]);
});
test('retrying quotes successfully restores the existing accept and reject actions', async () => {
    for (const action of ['accept','reject']) {
    const calls = [], ids = ['orderContent','orderActions','quotesSection','orderHistory','scheduleForm','reviewForm','scheduleBox','reviewBox'];
    let attempts = 0;
    const env = environment(ids, 'CLIENTE', async (route, options) => {
        if (options) { calls.push(route); return {}; }
        if (route === '/pedidos/1') return { titulo: 'Teste', status: 'PENDENTE', cidade: 'Cidade' };
        if (route.endsWith('/orcamentos')) {
            if (++attempts === 1) throw new Error('Sem conexão');
            return [{ id: 7, profissional: { id: 3, nome: 'Teste', verificado: true }, situacao: 'ENVIADO', valor: 100, prazoEstimado: '1 dia' }];
        }
        return [];
    });
    env.run('pages/cliente-orcamentos.js'); await tick();
    const target = env.elements.get('quotesSection');
    await target.querySelector('#retryQuotes').emit('click');
    assert.match(target.innerHTML, /Propostas recebidas \(1\)/);
    await target.querySelector(`[data-${action}-quote="7"]`).emit('click');
    assert.deepEqual(calls, [action === 'accept' ? '/orcamentos/7/aceitar' : '/orcamentos/7/recusar']);
    }
});
