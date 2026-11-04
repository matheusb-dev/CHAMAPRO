/* Real browser + real GET API. Mutations are aborted before they reach the server. */
const { chromium } = require('playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');

(async () => {
    let input = '';
    for await (const chunk of process.stdin) input += chunk;
    const setup = JSON.parse(input);
    const results = [], pages = [], mutationsBlocked = [], javascriptErrors = [], serverErrors = [];
    const evidence = path.resolve(setup.evidence, 'screenshots');
    await fs.mkdir(evidence, { recursive: true });
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    const context = await browser.newContext({ locale: 'pt-BR', reducedMotion: 'reduce' });
    await context.route('**/*', async route => {
        const request = route.request();
        if (!request.url().startsWith(setup.baseUrl)) return route.abort();
        if (new URL(request.url()).pathname.startsWith('/api/') && !['GET','HEAD','OPTIONS'].includes(request.method())) {
            mutationsBlocked.push({ method: request.method(), path: new URL(request.url()).pathname });
            return route.abort('failed');
        }
        return route.continue();
    });
    const page = await context.newPage();
    page.on('response',response=>{if(response.status()>=500&&response.url().includes('/api/'))serverErrors.push({status:response.status(),path:new URL(response.url()).pathname});});
    page.on('pageerror', error => javascriptErrors.push({ page: page.url().replace(setup.baseUrl, '/'), message: error.message }));
    async function check(name, action) {
        try { await action(); results.push({ name, status: 'PASS' }); console.log('PASS ' + name); }
        catch (error) { results.push({ name, status: 'FAIL', detail: error.message.slice(0,500) }); console.log('FAIL ' + name + ': ' + error.message.slice(0,300)); }
    }
    async function session(role) {
        await page.goto(setup.baseUrl);
        await page.evaluate(account => {
            sessionStorage.clear();
            if(account){ sessionStorage.setItem('chamapro_token', account.token); sessionStorage.setItem('chamapro_usuario', JSON.stringify(account.usuario)); }
        }, setup.accounts[role] || null);
    }
    async function open(url) {
        await page.goto(new URL(url, setup.baseUrl).href);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(100);
    }
    const rolePages = {
        VISITANTE: ['/', '/pages/autenticacao/login.html', '/pages/autenticacao/cadastro.html'],
        CLIENTE: ['/pages/cliente/painel.html','/pages/cliente/criar-chamado.html','/pages/cliente/orcamentos.html','/pages/cliente/favoritos.html','/pages/cliente/perfil.html', ...(setup.professionalId ? ['/pages/cliente/perfil-profissional.html?id='+setup.professionalId] : []), ...(setup.orderId ? ['/pages/cliente/orcamentos.html?pedidoId='+setup.orderId] : [])],
        PROFISSIONAL: ['/pages/trabalhador/painel.html','/pages/trabalhador/fila-chamados.html','/pages/trabalhador/perfil.html','/pages/trabalhador/cadastro-profissional.html','/pages/trabalhador/verificacao.html'],
        ADMIN: ['/pages/admin/gestao.html#inicio','/pages/admin/gestao.html#verificacoes','/pages/admin/gestao.html#usuarios','/pages/admin/gestao.html#pedidos']
    };
    for (const width of [360,390,768,1366]) {
        await page.setViewportSize({width,height:900});
        for (const [role, urls] of Object.entries(rolePages)) {
            if(role !== 'VISITANTE' && !setup.accounts[role]) continue;
            await session(role);
            for(const [index,url] of urls.entries()) await check(`${width}px ${role} ${url}`, async () => {
                const before = javascriptErrors.length;
                await open(url);
                assert.equal(javascriptErrors.length, before, 'JavaScript error');
                const layout = await page.evaluate(() => ({ width: document.documentElement.clientWidth, scroll: document.documentElement.scrollWidth, body: document.body.innerText.slice(0,160), main: document.querySelector('main')?.innerText.length || 0 }));
                assert.ok(layout.scroll <= layout.width+1, `Horizontal overflow: ${layout.scroll} > ${layout.width}`);
                assert.ok(layout.body.length > 20 && layout.main > 20, 'Empty page');
                if(role !== 'VISITANTE') {
                    assert.ok(!page.url().includes('/autenticacao/'), 'Unexpected login redirect');
                    assert.equal(await page.locator('[data-app-header] a.brand').count(),1);
                }
                const file = `${width}-${role.toLowerCase()}-${index}.png`;
                await page.screenshot({path:path.join(evidence,file),fullPage:true});
                pages.push({width,role,url,screenshot:file,...layout});
            });
        }
    }
    for(const role of ['VISITANTE','CLIENTE','PROFISSIONAL','ADMIN']) {
        if(role !== 'VISITANTE' && !setup.accounts[role]) continue;
        await check('Logo keyboard navigation ' + role, async () => {
            await session(role);
            const destination=({CLIENTE:'/pages/cliente/painel.html',PROFISSIONAL:'/pages/trabalhador/painel.html',ADMIN:'/pages/admin/gestao.html'})[role]||'/';
            await open(role==='VISITANTE'?'/pages/autenticacao/login.html':rolePages[role][0]);
            const logo=page.locator('a.brand:visible').first();
            await logo.focus(); await logo.press('Enter'); await page.waitForLoadState('networkidle');
            assert.ok(new URL(page.url()).pathname===destination || (role==='VISITANTE'&&new URL(page.url()).pathname==='/index.html'));
        });
    }
    if(setup.accounts.CLIENTE) {
        await session('CLIENTE');
        await check('Client forbidden administrative API and phone reads',async()=>{
            for(const url of ['admin/usuarios','admin/relatorios','whatsapp/profissionais/'+setup.professionalId+'/contato']) {
                const response=await page.request.get(setup.baseUrl+'api/v1/'+url,{headers:{Authorization:'Bearer '+setup.accounts.CLIENTE.token}});
                assert.equal(response.status(),403,url);
            }
        });
        await check('Urgent wizard survives step navigation and has no date fields',async()=>{
            await open('/pages/cliente/criar-chamado.html');
            assert.equal(await page.locator('input[type="date"],input[type="datetime-local"]').count(),0);
            const categories = await (await page.request.get(setup.baseUrl+'api/v1/categorias')).json();
            if(!categories.length) throw Error('No category fixture');
            await page.locator('#categorySearch').fill(categories[0].nome);
            await page.locator('#categoryOptions li').first().click();
            await page.locator('#description').fill('Rascunho de homologação: nenhuma solicitação será enviada.');
            await page.locator('#description').blur();
            await page.locator('#title').fill('Rascunho sem gravação');
            await page.locator('[data-service-next="1"]').click();
            assert.ok(await page.locator('[data-service-step="1"]').isVisible());
            await page.locator('[data-service-back="0"]').click();
            assert.equal(await page.locator('#title').inputValue(),'Rascunho sem gravação');
        });
        if(setup.orderId) await check('Real proposal contact follows registered phone validity',async()=>{
            await open('/pages/cliente/orcamentos.html?pedidoId='+setup.orderId);
            const quotes=await (await page.request.get(setup.baseUrl+`api/v1/pedidos/${setup.orderId}/orcamentos`,{headers:{Authorization:'Bearer '+setup.accounts.CLIENTE.token}})).json();
            assert.ok(quotes.length>0,'No proposal fixture');
            const links=page.locator('#quotesSection a[href^="https://wa.me/"]');
            const expected=await page.evaluate(qs=>qs.filter(q=>window.Chamapro.normalizarWhatsApp(q.profissional.telefone)).length,quotes);
            assert.equal(await links.count(),expected);
            for(let i=0;i<await links.count();i++) {
                const link=links.nth(i); assert.match(await link.getAttribute('href'),/^https:\/\/wa\.me\/[1-9]\d{7,14}\?text=/);
                const positions=await link.evaluate(a=>({link:a.getBoundingClientRect().top,price:a.parentElement.previousElementSibling?.getBoundingClientRect().bottom}));
                assert.ok(positions.link>=positions.price-1,'Contact above price');
            }
            assert.equal(await page.locator('input[type="datetime-local"]').count(),0);
        });
    }
    if(setup.accounts.PROFISSIONAL) {
        await session('PROFISSIONAL');
        await check('Professional save network failure preserves draft without reaching API',async()=>{
            await open('/pages/trabalhador/perfil.html');
            await page.locator('#information > summary').click();
            await page.locator('#description').fill('Rascunho preservado durante falha simulada de rede.');
            await page.locator('#saveInfo').click();
            await page.waitForFunction(()=>!document.querySelector('#professionalFields').inert);
            assert.equal(await page.locator('#description').inputValue(),'Rascunho preservado durante falha simulada de rede.');
            assert.match(await page.locator('#professionalMessage').innerText(),/Não foi possível|não foi concluída/);
            assert.ok(mutationsBlocked.some(x=>x.method==='PUT'&&x.path==='/api/v1/profissionais/me'));
        });
        await check('Professional registration has no availability controls',async()=>{
            await open('/pages/trabalhador/cadastro-profissional.html');
            assert.equal(await page.locator('#availabilityEditor,input[type="time"],input[type="datetime-local"]').count(),0);
            assert.ok(await page.locator('#saveStep0').isVisible());
        });
    }
    if(setup.accounts.CLIENTE && setup.orderId) {
        await session('CLIENTE');
        for(const [kind,phone] of [['valid','+55 (11) 98765-4321'],['missing',null],['invalid','123']]) await check('Browser contact '+kind+' (simulated response phone, real HTML)',async()=>{
            const pattern='**/api/v1/pedidos/'+setup.orderId+'/orcamentos';
            await page.route(pattern,async route=>{
                const response=await route.fetch(); const data=await response.json();
                for(const quote of data)quote.profissional.telefone=phone;
                await route.fulfill({response,json:data});
            });
            await open('/pages/cliente/orcamentos.html?pedidoId='+setup.orderId);
            const link=page.locator('#quotesSection a[href^="https://wa.me/"]').first();
            if(kind==='valid'){
                assert.match(await link.getAttribute('href'),/^https:\/\/wa.me\/5511987654321\?text=/);
                const geometry=await link.evaluate(a=>({top:a.getBoundingClientRect().top,price:a.parentElement.previousElementSibling.getBoundingClientRect().bottom}));
                assert.ok(geometry.top>=geometry.price-1);
                const accept=page.locator('[data-accept-quote]').first();
                if(await accept.count()){
                    const dialog=page.waitForEvent('dialog'); const clicked=accept.click(); const prompt=await dialog;
                    assert.match(prompt.message(),/solicitação urgente/);assert.match(prompt.message(),/WhatsApp/);await prompt.dismiss(); await clicked;
                }
            } else {assert.equal(await page.locator('#quotesSection a[href^="https://wa.me/"]').count(),0);assert.match(await page.locator('#quotesSection').innerText(),/WhatsApp não disponível/);}
            await page.unroute(pattern);
        });
    }
    if(setup.accounts.ADMIN && setup.professionalId) await check('Existing administrator can read professional contact',async()=>{
        const response=await page.request.get(setup.baseUrl+'api/v1/whatsapp/profissionais/'+setup.professionalId+'/contato',{headers:{Authorization:'Bearer '+setup.accounts.ADMIN.token}});
        assert.equal(response.status(),200);const body=await response.json();assert.ok(Object.hasOwn(body,'telefone'));
    });
    if(setup.accounts.BLOCKED) await check('Existing blocked professional token cannot read opportunities',async()=>{
        const response=await page.request.get(setup.baseUrl+'api/v1/pedidos',{headers:{Authorization:'Bearer '+setup.accounts.BLOCKED.token}});assert.equal(response.status(),401);
    });
    if(setup.accounts.UNVERIFIED) await check('Existing unverified professional has no matching opportunities',async()=>{
        const response=await page.request.get(setup.baseUrl+'api/v1/pedidos?status=PENDENTE&pagina=1&tamanho=50',{headers:{Authorization:'Bearer '+setup.accounts.UNVERIFIED.token}});assert.equal(response.status(),200);assert.equal((await response.json()).total,0);
    });
    await check('No unhandled JavaScript errors or real API server errors',async()=>{assert.equal(javascriptErrors.length,0);assert.equal(serverErrors.length,0,JSON.stringify(serverErrors));});
    await context.close(); await browser.close();
    const summary={mode:'real-browser-real-readonly-api',pass:results.filter(r=>r.status==='PASS').length,fail:results.filter(r=>r.status==='FAIL').length,results,pages,mutationsBlocked,javascriptErrors,serverErrors,missingRoles:['CLIENTE','PROFISSIONAL','ADMIN'].filter(r=>!setup.accounts[r])};
    await fs.writeFile(path.join(setup.evidence,'browser-results.json'),JSON.stringify(summary,null,2));
    console.log(`BROWSER ${summary.pass} passed; ${summary.fail} failed; ${pages.length} screenshots. Writes never reached API.`);
    process.exitCode=summary.fail?1:0;
})().catch(error=>{console.error(error.message.slice(0,600));process.exitCode=1;});
