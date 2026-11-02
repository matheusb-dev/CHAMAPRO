// Run: node --test Tests/frontend/authenticated-layout.test.cjs
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8');
const shared = ['/css/variables.css', '/css/global.css', '/css/components.css', '/css/pages/app.css'];
const version = '20261102-simple1';
const stage1Scripts = new Set(['/js/components/availability.js', '/js/pages/cliente.js', '/js/pages/cliente-orcamentos.js', '/js/pages/cadastro-profissional.js', '/js/pages/trabalhador.js']);
const stage2Scripts = new Set(['/js/pages/cliente-pedidos.js', '/js/pages/cliente-inicio.js', '/js/pages/cliente-orcamentos.js', '/js/pages/cliente.js']);
const stage3Assets = new Set(['/css/pages/pedido.css', '/js/components/combobox.js', '/js/pages/cliente-pedido-guiado.js', '/js/pages/cliente-favoritos.js', '/js/pages/cliente-perfil-profissional.js']);
const stage4Assets = new Set(['/js/components/professional-data.js', '/js/components/professional-editor.js', '/js/components/availability-editor.js', '/js/pages/cadastro-profissional.js', '/css/pages/professional-editor.css']);
const stage5Assets = new Set(['/js/components/sidebar.js','/js/components/notifications.js','/js/components/professional-work.js','/js/pages/trabalhador.js','/js/pages/painel-trabalhador.js']);
const stage6Assets = new Set(['/js/pages/admin.js','/css/pages/admin.css']);
const stage7Assets = new Set(['/js/components/professional-work.js','/js/components/professional-editor.js','/js/app.js','/js/components/notifications.js','/js/components/private-document.js','/js/pages/admin.js','/js/pages/trabalhador-verificacao.js','/js/pages/cliente-orcamentos.js']);
const expectedVersion = (page, url) => stage7Assets.has(url) ? '20261102-stage7' : stage6Assets.has(url) ? '20261102-stage6' : stage5Assets.has(url) ? '20261102-stage5' : stage4Assets.has(url) ? '20261102-stage4' : stage3Assets.has(url) || (page.endsWith('/criar-chamado.html') && ['/js/api.js', '/js/pages/cliente.js'].includes(url)) ? '20261102-stage3' : page.includes('/cliente/') && (stage2Scripts.has(url) || url === '/js/components/sidebar.js' || url === '/css/pages/cliente.css') ? '20261102-stage2' : stage1Scripts.has(url) ? '20261102-stage1' : version;
const stylesheet = `/css/pages/authenticated.css?v=${version}`;
const pages = ['cliente', 'trabalhador', 'admin'].flatMap(role => fs.readdirSync(path.join(root, 'wwwroot/pages', role)).filter(f => f.endsWith('.html')).map(f => `wwwroot/pages/${role}/${f}`));

test('all authenticated pages load the same versioned shared styles, shell and scripts', () => {
    assert.equal(pages.length, 13);
    for (const page of pages) {
        const html = read(page);
        assert.match(html, /<body class="app-auth">/, page);
        const styles = [...html.matchAll(/<link[^>]*rel="stylesheet"[^>]*href="([^"]+)"/g)].map(m => m[1]);
        assert.deepEqual(styles.slice(0, 4), shared.map(url => `${url}?v=${version}`), page);
        assert.ok(styles.includes(stylesheet), page);
        assert.equal(styles.at(-1), page.includes('/admin/') ? '/css/pages/admin.css?v=20261102-stage6' : page.endsWith('/criar-chamado.html') ? '/css/pages/pedido.css?v=20261102-stage3' : page.includes('/trabalhador/') && ['perfil.html', 'cadastro-profissional.html'].some(name=>page.endsWith('/'+name)) ? '/css/pages/professional-editor.css?v=20261102-stage4' : stylesheet, page);
        for (const match of html.matchAll(/(?:href|src)="((?:\/css\/|\/js\/)[^"]+)"/g)) {
            const url = new URL(match[1], 'http://localhost');
            assert.equal(url.searchParams.get('v'), expectedVersion(page, url.pathname), `${page}: ${url.pathname}`);
            assert.ok(fs.existsSync(path.join(root, 'wwwroot', url.pathname)), `${page}: ${url.pathname}`);
        }
        for (const selector of ['data-app-header', 'data-app-sidebar', 'class="app-layout"', 'class="app-main"', 'class="app-content']) assert.ok(html.includes(selector), `${page}: ${selector}`);
        const scripts = [...html.matchAll(/<script src="([^"]+)" defer/g)].map(m => m[1]);
        assert.deepEqual(scripts.slice(0, 5), ['api.js', 'components/header.js', 'components/sidebar.js', 'components/notifications.js', 'app.js'].map(f => `/js/${f}?v=${expectedVersion(page, '/js/' + f)}`), page);
    }
});

function renderHeader({ legacy = false, existing = true } = {}) {
    const links = existing ? [{ href: `http://localhost/css/pages/authenticated.css${legacy ? '' : '?v=' + version}` }] : [];
    const classes = new Set(legacy ? [] : ['app-auth']);
    const document = {
        body: { classList: { add: name => classes.add(name) } },
        querySelectorAll: () => links,
        createElement: () => ({}),
        head: { append: link => links.push(link) }
    };
    const context = { URL, document, window: { location: { origin: 'http://localhost', href: 'http://localhost/pages/cliente/criar-chamado.html' }, Chamapro: { escaparHtml: text => text } } };
    vm.createContext(context);
    vm.runInContext(read('wwwroot/js/components/header.js'), context);
    const container = { querySelector: () => ({ addEventListener() {} }) };
    context.window.ChamaproHeader.render(container, { nome: 'Teste de layout', perfil: 'CLIENTE' });
    context.window.ChamaproHeader.render(container, { nome: 'Teste de layout', perfil: 'CLIENTE' });
    return { links, classes, html: container.innerHTML };
}

test('cached legacy HTML recovers its scope and missing stylesheet without duplicate links', () => {
    for (const existing of [false, true]) {
        const result = renderHeader({ legacy: true, existing });
        assert.ok(result.classes.has('app-auth'));
        assert.equal(result.links.length, 1);
        assert.equal(result.links[0].href, `http://localhost${stylesheet}`);
    }
});

test('current dashboard keeps the same stylesheet without reloading it', () => {
    const result = renderHeader();
    assert.equal(result.links.length, 1);
    assert.equal(result.links[0].href, `http://localhost${stylesheet}`);
});

test('header and every role menu have intrinsic SVG dimensions and preserve destinations', () => {
    const header = renderHeader().html;
    for (const svg of header.matchAll(/<svg\b[^>]*>/g)) assert.match(svg[0], /width="20" height="20" viewBox="0 0 24 24"/);
    for (const [role, count, pathname] of [['CLIENTE', 5, '/pages/cliente/painel.html'], ['PROFISSIONAL', 7, '/pages/trabalhador/painel.html'], ['ADMIN', 1, '/pages/admin/gestao.html']]) {
        let anchors;
        const container = { querySelectorAll: () => anchors, set innerHTML(html) {
            this.html = html;
            anchors = [...html.matchAll(/<a class="sidebar-link" href="([^"]+)"/g)].map(([, href]) => {
                const url = new URL(href, 'http://localhost');
                return { pathname: url.pathname, hash: url.hash, classList: { toggle: (_class, active) => { url.active = active; } }, setAttribute: (name, value) => { url[name] = value; }, removeAttribute: name => { delete url[name]; }, url };
            });
        }};
        const context = { location: { pathname, hash: '' }, document: { getElementById: () => null }, window: { addEventListener() {} } };
        vm.createContext(context);
        vm.runInContext(read('wwwroot/js/components/sidebar.js'), context);
        context.window.ChamaproSidebar.render(container, { perfil: role });
        assert.equal(anchors.length, count);
        assert.equal(anchors.filter(a => a.url.active).length, 1);
        assert.equal(anchors.filter(a => a.url['aria-current'] === 'page').length, 1);
        const svgs = [...container.html.matchAll(/<svg\b[^>]*>/g)];
        assert.equal(svgs.length, count);
        for (const svg of svgs) assert.match(svg[0], /width="19" height="20" viewBox="0 0 24 24"/);
        for (const anchor of anchors) assert.ok(fs.existsSync(path.join(root, 'wwwroot', anchor.pathname)));
    }
});
