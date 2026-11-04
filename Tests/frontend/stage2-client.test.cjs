const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const script = file => fs.readFileSync(path.resolve(__dirname, '../../wwwroot/js', file), 'utf8');
const tick = () => new Promise(resolve => setImmediate(resolve));
class Node {
    constructor() { this.events = {}; this.dataset = {}; this.style = {}; this.attrs = {}; this.value = ''; this.hidden = true; }
    set innerHTML(value) { this.html = value; this.retry = new Node(); }
    get innerHTML() { return this.html || ''; }
    setAttribute(name, value) { this.attrs[name] = value; }
    addEventListener(name, fn) { this.events[name] = fn; }
    querySelector() { return this.retry; }
    querySelectorAll() { return []; }
    insertAdjacentHTML(_where, value) { this.html += value; }
}
function setup(ids, api, search = '') {
    const nodes = new Map(ids.map(id => [id, new Node()]));
    const context = { URLSearchParams, location: { search, reload() {}, assign() {} }, setTimeout, document: { getElementById: id => nodes.get(id) || null }, window: { Chamapro: { api, escaparHtml: value => String(value ?? '').replaceAll('<', '&lt;'), formatarData: value => String(value || ''), formatarMoeda: String, formatarNota: String }, ChamaproApp: { ready: Promise.resolve({ perfil: 'CLIENTE' }) } } };
    vm.createContext(context);require('./helpers/api.cjs').install(context);
    return { nodes, context, run: file => vm.runInContext(script(file), context) };
}
const listIds = ['ordersPageList','ordersPage','orderContent','previousOrders','nextOrders','ordersStatus','ordersPagination','ordersPageLabel'];
test('orders listing accesses pages beyond 50 records and never automatically opens a detail', async () => {
    const calls = [];
    const env = setup(listIds, async route => {
        calls.push(route); const q = new URL('http://local' + route).searchParams; const page = Number(q.get('pagina'));
        return { pagina: page, tamanho: 10, total: 65, itens: [{ id: page * 10, titulo: 'Pedido ' + page, status: 'PENDENTE', categoria: 'Serviço', criadoEm: '2026-11-02', cidade: 'Cidade' }] };
    });
    env.run('pages/cliente-pedidos.js'); await env.context.window.ChamaproOrdersList.init();
    for (let page = 2; page <= 7; page++) await env.nodes.get('nextOrders').events.click();
    assert.match(calls.at(-1), /pagina=7/);
    assert.match(env.nodes.get('ordersPageList').innerHTML, /pedidoId=70/);
    assert.match(env.nodes.get('ordersPageLabel').textContent, /Página 7 de 7/);
    assert.equal(env.nodes.get('nextOrders').disabled, true);
    assert.ok(calls.every(route => route.startsWith('/pedidos?')));
    await env.nodes.get('previousOrders').events.click();
    assert.match(calls.at(-1), /pagina=6/);
});
test('real status filter resets pagination; loading, empty and failed pages remain distinct with retry', async () => {
    let resolve, fail = false;
    const calls = [];
    const env = setup(listIds, async route => {
        calls.push(route);
        if (fail) throw new Error('Sem conexão');
        return new Promise(done => { resolve = done; });
    }, '?pagina=6');
    env.run('pages/cliente-pedidos.js');
    const loading = env.context.window.ChamaproOrdersList.init();
    assert.match(env.nodes.get('ordersPageList').innerHTML, /Carregando/);
    resolve({ itens: [], total: 60, tamanho: 10 }); await loading;
    env.nodes.get('ordersStatus').value = 'CONCLUIDO';
    fail = true; await env.nodes.get('ordersStatus').events.change();
    assert.match(calls.at(-1), /pagina=1/); assert.match(calls.at(-1), /status=CONCLUIDO/);
    assert.match(env.nodes.get('ordersPageList').innerHTML, /Não foi possível/);
    const retry = env.nodes.get('ordersPageList').retry;
    fail = false; const retrying = retry.events.click();
    resolve({ itens: [], total: 0, tamanho: 10 }); await retrying;
    assert.match(env.nodes.get('ordersPageList').innerHTML, /Nenhum pedido nesta situação/);
    assert.equal(env.nodes.get('ordersPagination').hidden, true);
});
test('legacy detail links still work; completed orders expose evaluation but no reschedule/cancel controls', async () => {
    const ids = ['orderContent','orderActions','quotesSection','orderHistory','scheduleForm','reviewForm','scheduleBox','reviewBox','ordersHeading'];
    const env = setup(ids, async route => {
        if (route === '/pedidos/42') return { id: 42, titulo: 'Serviço concluído', status: 'CONCLUIDO', criadoEm: '2026-11-02', profissionalId: 9, destinatarios: [{ profissionalId: 9, nome: 'Profissional escolhido' }], agendamento: { id: 5, situacao: 'Concluido', dataHora: '2026-11-02' } };
        return [];
    }, '?pedidoId=42');
    env.run('pages/cliente-orcamentos.js'); await tick();
    assert.match(env.nodes.get('orderContent').innerHTML, /Profissional escolhido/);
    assert.equal(env.nodes.get('reviewBox').hidden, false);
    assert.equal(env.nodes.get('scheduleBox').hidden, true);
    assert.doesNotMatch(env.nodes.get('orderActions').innerHTML, /id="(?:reschedule|cancelOrder|cancelAppointment)"/);
});
test('list entry delegates to listing without fetching any arbitrary first order', async () => {
    let listed = false;
    const env = setup(['orderContent'], async () => { throw new Error('Unexpected detail fetch'); });
    env.context.window.ChamaproOrdersList = { init: async () => { listed = true; } };
    env.run('pages/cliente-orcamentos.js'); await tick(); assert.equal(listed, true);
});
test('home fetches active statuses and shows only real available proposal notices', async () => {
    const calls = [];
    const env = setup(['homeOrdersList','homeNotices','homeNoticesSection','firstServiceGuide'], async route => {
        calls.push(route);
        if (route.startsWith('/pedidos?status=PENDENTE')) return { itens: [{ id: 7, titulo: 'Pedido ativo', status: 'PENDENTE' }] };
        if (route.startsWith('/pedidos?')) return { itens: [] };
        if (route === '/pedidos/7/orcamentos') return [{ situacao: 'ENVIADO', expirado: false }, { situacao: 'ENVIADO', expirado: true }];
        return [];
    });
    env.run('pages/cliente-pedidos.js'); env.run('pages/cliente-inicio.js'); await tick();
    assert.match(env.nodes.get('homeOrdersList').innerHTML, /1 proposta disponível/);
    assert.ok(calls.some(route => route.includes('status=EM_ANDAMENTO')));
    assert.equal(env.nodes.get('homeNoticesSection').hidden, true);
});
test('client navigation has exactly the five approved areas while creation and permissions stay linked', () => {
    const source = script('components/sidebar.js');
    const links = source.match(/CLIENTE: \[([^\n]+)/)[1];
    for (const name of ['Início','Pedir um serviço','Meus pedidos','Meus profissionais','Minha conta']) assert.ok(links.includes("['" + name + "'"));
    assert.equal([...links.matchAll(/\['/g)].length, 5);
    assert.match(script('app.js'), /\["\/pages\/cliente\/", "CLIENTE"\]/);
});
