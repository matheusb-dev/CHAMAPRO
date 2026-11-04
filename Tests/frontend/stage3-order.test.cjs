const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const read = p => fs.readFileSync(path.resolve(__dirname, '../..', p), 'utf8');
const tick = () => new Promise(r => setImmediate(r));
class UIEvent { constructor(type, options = {}) { this.type = type; Object.assign(this, options); } preventDefault() { this.prevented = true; } stopPropagation() { this.stopped = true; } }
class Element {
    constructor(tag = 'div', doc) { this.tagName = tag.toUpperCase(); this.doc = doc; this.children = []; this.events = {}; this.attrs = {}; this.dataset = {}; this.value = ''; this.files = []; this.style = {}; this.classList = { toggle() {} }; this.labels = []; this.validity = { valid: true }; this.options = []; }
    append(...nodes) { for (const n of nodes) { n.parent = this; this.children.push(n); } }
    before(node) { if (this.parent) { node.parent = this.parent; this.parent.children.splice(this.parent.children.indexOf(this), 0, node); } }
    replaceChildren() { this.children = []; }
    setAttribute(k,v) { this.attrs[k] = String(v); }
    removeAttribute(k) { delete this.attrs[k]; }
    setCustomValidity(v) { this.validity.valid = !v; this.validationMessage = v; }
    reportValidity() { return this.validity.valid; }
    addEventListener(k,fn) { (this.events[k] ||= []).push(fn); }
    dispatchEvent(event) { event.target ||= this; for (const fn of this.events[event.type] || []) fn(event); if (event.bubbles && !event.stopped) this.parent?.dispatchEvent(event); }
    async emit(type, options={}) { const event = new UIEvent(type, options); event.target=this; await Promise.all((this.events[type]||[]).map(fn=>fn(event))); return event; }
    focus() { if (this.doc.activeElement !== this) { this.doc.activeElement = this; this.dispatchEvent(new UIEvent('focus')); } }
    scrollIntoView() { this.scrolled = true; }
    contains(node) { return node === this || this.children.some(c=>c.contains(node)); }
    all() { return this.children.flatMap(c=>[c,...c.all()]); }
    matches(selector) {
        if (selector.startsWith('#')) return this.id === selector.slice(1);
        if (selector.endsWith(':checked') && !this.checked) return false;
        const pairs = [...selector.matchAll(/\[([\w-]+)(?:="([^"]*)")?\]/g)];
        return pairs.length && pairs.every(([,key,value])=>key in this.attrs && (value===undefined||this.attrs[key]===value));
    }
    querySelectorAll(selector) { return this.all().filter(e=>e.matches(selector)); }
    querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
    get selectedOptions() { return this.options.filter(o=>o.value===this.value); }
    set innerHTML(html) {
        this.html = html; this.children = [];
        for(const [,tag,attributes] of html.matchAll(/<(input|option|button)\b([^>]*)>/g)) {
            const node=new Element(tag,this.doc);
            for(const [,key,value] of attributes.matchAll(/([\w-]+)(?:="([^"]*)")?/g)) { node.attrs[key]=value??true; if(key==='value')node.value=value; if(key==='id')node.id=value; if(key.startsWith('data-'))node.dataset[key.slice(5).replace(/-([a-z])/g,(_,c)=>c.toUpperCase())]=value; }
            node.checked='checked'in node.attrs; this.append(node);
        }
        this.options=this.children.filter(e=>e.tagName==='OPTION'); this.options.forEach(o=>o.text='Categoria real');
    }
    get innerHTML() { return this.html || ''; }
}
function environment({ search='', profile={cidade:'São Paulo',bairro:'Centro',enderecoPrincipal:'Rua de teste'}, pros, failCreate, failPros, failCategories }={}) {
    const root=new Element(), ids=['createOrderForm','category','categoryHelp','city','cityWarning','locationMessage','serviceType','desiredDateGroup','professionalPicker','reviewPanel','reviewSummary','formMessage','reviewMessage','searchProfessionals','professionalList','desiredDate','title','description','neighborhood','address','backToEdit','confirmOrder','photos','favoriteMessage','useAutomatic'];
    const nodes=new Map(); const doc={ activeElement:null, createElement:tag=>new Element(tag,doc), addEventListener(){}, getElementById:id=>nodes.get(id)||root.all().find(n=>n.id===id)||null, querySelectorAll:s=>root.querySelectorAll(s), querySelector:s=>root.querySelector(s) };
    const form=new Element('form',doc); form.id='createOrderForm';root.append(form); nodes.set(form.id,form);
    for(const id of ids.slice(1)){ const e=new Element(id==='category'?'select':'input',doc); e.id=id; nodes.set(id,e); form.append(e); }
    nodes.get('category').required=true; nodes.get('city').required=true;
    form.reportValidity=()=>Boolean(nodes.get('category').value && nodes.get('city').value);
    const radios=new Element('div',doc);radios.innerHTML='<input name="distribution" value="auto" checked><input name="distribution" value="manual">';form.append(radios);
    Object.assign(nodes.get('serviceType'),{value:'IMEDIATO'});nodes.get('title').value='Serviço de teste';nodes.get('description').value='Descrição do serviço de teste';
    const calls=[],redirects=[]; let createResolver;
    const api=async(route,options)=>{
        calls.push({route,body:options?.body});
        if(route==='/categorias'){if(failCategories)throw Error('offline');return [{id:1,nome:'Pedreiro'},{id:2,nome:'Técnico em informática'}];}
        if(route==='/clientes/me')return profile;
        if(route.startsWith('/profissionais?')){if(failPros)throw Error('offline');return {itens:pros||[{id:7,nome:'Profissional fictício',verificado:true,notaMedia:4}],total:1};}
        if(route==='/pedidos'){if(failCreate)throw failCreate;if(createResolver)return await createResolver;return {id:99};}
        return {};
    };
    const cities=[{id:3550308,nome:'São Paulo',uf:'SP'},{id:3509502,nome:'Campinas',uf:'SP'},{id:4104303,nome:'Campo Mourão',uf:'PR'},{id:1,nome:'Bom Jesus',uf:'PI'},{id:2,nome:'Bom Jesus',uf:'RS'}];
    const context={document:doc,Event:UIEvent,CustomEvent:UIEvent,URLSearchParams,FormData,location:{search,assign:url=>redirects.push(url)},fetch:async()=>({ok:true,json:async()=>cities}),window:{Chamapro:{api,escaparHtml:v=>String(v??'').replaceAll('<','&lt;'),formatarNota:String,toast(){}},ChamaproApp:{ready:Promise.resolve({perfil:'CLIENTE'})}}};
    vm.createContext(context);require('./helpers/api.cjs').install(context);
    vm.runInContext(read('wwwroot/js/components/combobox.js'),context);
    vm.runInContext(read('wwwroot/js/pages/cliente.js'),context);
    return {nodes,doc,context,calls,redirects,radios,cities,setPending:p=>{createResolver=p;}};
}
async function ready(options) { const e=environment(options);await tick(); e.doc.getElementById('categorySearch').focus(); e.doc.getElementById('categoryOptions').children[0].dispatchEvent(new UIEvent('click'));return e; }

test('categories come from API and combobox searches accents, selects and closes with keyboard',async()=>{
    const e=environment();await tick();const search=e.doc.getElementById('categorySearch');search.value='TECNICO';await search.emit('input');
    const list=e.doc.getElementById('categoryOptions');assert.equal(list.children.length,1);assert.equal(list.children[0].textContent,'Técnico em informática');
    await search.emit('keydown',{key:'ArrowDown'});await search.emit('keydown',{key:'Enter'});
    assert.equal(e.nodes.get('category').value,'2');assert.equal(list.hidden,true);assert.equal(search.attrs['aria-expanded'],'false');
    await search.emit('keydown',{key:'ArrowDown'});await search.emit('keydown',{key:'Escape'});assert.equal(list.hidden,true);
    assert.ok(e.calls.some(c=>c.route==='/categorias'));
});
test('city selection displays UF but sends only the canonical accented name; neighborhood remains free text',async()=>{
    const e=await ready();const city=e.doc.getElementById('citySearch');city.value='campo mourao';await city.emit('input');await city.emit('keydown',{key:'ArrowDown'});await city.emit('keydown',{key:'Enter'});
    e.nodes.get('neighborhood').value='Bairro novo';await e.nodes.get('confirmOrder').emit('click');
    const body=e.calls.find(c=>c.route==='/pedidos').body;assert.equal(body.cidade,'Campo Mourão');assert.equal(body.bairro,'Bairro novo');assert.equal(e.nodes.get('city').dataset.uf,'PR');assert.equal('uf'in body,false);
});
test('city not found does not leave a stale canonical selection; homonyms warn about backend limitation',async()=>{
    const e=await ready();const input=e.doc.getElementById('citySearch');input.value='cidade inexistente xyz';await input.emit('input');assert.equal(e.nodes.get('city').value,'');assert.match(e.doc.getElementById('cityStatus').textContent,/Nenhuma opção/);
    input.value='Bom Jesus';await input.emit('input');await input.emit('keydown',{key:'ArrowDown'});await input.emit('keydown',{key:'Enter'});assert.match(e.nodes.get('cityWarning').textContent,/sem distinguir a UF/);
});
test('profile location is reused and changing city clears dependent address while keeping editable fields',async()=>{
    const e=await ready();assert.equal(e.nodes.get('city').value,'São Paulo');assert.equal(e.nodes.get('neighborhood').value,'Centro');assert.equal(e.nodes.get('address').value,'Rua de teste');
    const input=e.doc.getElementById('citySearch');input.value='Campinas';await input.emit('input');await input.emit('keydown',{key:'ArrowDown'});await input.emit('keydown',{key:'Enter'});assert.equal(e.nodes.get('city').value,'Campinas');assert.equal(e.nodes.get('neighborhood').value,'');assert.equal(e.nodes.get('address').value,'');
});
test('favorite is preselected only after compatible lookup; manual payload keeps its ID',async()=>{
    const e=await ready({search:'?profissionalId=7'});await e.nodes.get('createOrderForm').emit('chamapro:stepchange',{detail:2});await tick();assert.match(e.nodes.get('favoriteMessage').textContent,/compatível e está selecionado/);
    await e.nodes.get('confirmOrder').emit('click');const payload=e.calls.find(c=>c.route==='/pedidos').body;assert.equal(payload.distribuirParaCompativeis,false);assert.deepEqual([...payload.profissionalIds],[7]);
});
test('incompatible favorite is not silently selected and user can return to automatic distribution',async()=>{
    const e=await ready({search:'?profissionalId=99'});await e.nodes.get('createOrderForm').emit('chamapro:stepchange',{detail:2});await tick();assert.match(e.nodes.get('favoriteMessage').textContent,/não está disponível/);
    await e.nodes.get('confirmOrder').emit('click');assert.ok(!e.calls.some(c=>c.route==='/pedidos'));
    await e.nodes.get('useAutomatic').emit('click');await e.nodes.get('confirmOrder').emit('click');const payload=e.calls.find(c=>c.route==='/pedidos').body;assert.equal(payload.distribuirParaCompativeis,true);assert.equal(payload.profissionalIds.length,0);
});
test('professional search failure stays distinct from no matches and can be retried',async()=>{
    const e=await ready({failPros:true});await e.nodes.get('searchProfessionals').emit('click');assert.match(e.nodes.get('professionalList').innerHTML,/Não foi possível buscar/);assert.ok(!e.calls.some(c=>c.route==='/pedidos'));
});
test('creation errors preserve values, explain the actual rule and never expose internal messages',async()=>{
    const e=await ready({failCreate:{status:422,codigo:'SEM_PROFISSIONAIS_COMPATIVEIS',message:'Operação não permitida'}});await e.nodes.get('confirmOrder').emit('click');assert.match(e.nodes.get('reviewMessage').textContent,/Não encontramos profissionais disponíveis/);assert.equal(e.nodes.get('title').value,'Serviço de teste');assert.equal(e.nodes.get('confirmOrder').disabled,false);assert.equal(e.nodes.get('createOrderForm').inert,false);
    assert.doesNotMatch(e.context.window.ChamaproOrderErrors.message({status:500,message:'SQL password=secret'}),/SQL|secret/);
});
test('automatic creation blocks duplicate requests and redirects to the created detail',async()=>{
    const e=await ready();let resolve;const pending=new Promise(done=>{resolve=done;});e.setPending(pending);
    const sending=e.nodes.get('confirmOrder').emit('click');await e.nodes.get('confirmOrder').emit('click');assert.equal(e.calls.filter(c=>c.route==='/pedidos').length,1);
    resolve({id:123});await sending;assert.deepEqual(e.redirects,['orcamentos.html?pedidoId=123']);const payload=e.calls.find(c=>c.route==='/pedidos').body;assert.equal(payload.distribuirParaCompativeis,true);assert.equal(payload.profissionalIds.length,0);assert.equal(payload.dataDesejada,null);
});
test('long selectors cap DOM options, provide internal scrolling and preserve selected values on refresh',async()=>{
    const e=await ready();const control=e.doc.createElement('input');control.id='large';e.nodes.get('createOrderForm').append(control);const combo=e.context.window.ChamaproCombobox.mount(control,{label:'Lista'});
    const items=Array.from({length:200},(_,i)=>({value:String(i),label:'Opção '+i}));combo.setItems(items);combo.select('150');combo.input.focus();await combo.input.emit('keydown',{key:'ArrowDown'});assert.equal(e.doc.getElementById('largeOptions').children.length,80);assert.match(e.doc.getElementById('largeStatus').textContent,/200/);combo.setItems(items);assert.equal(control.value,'150');assert.match(read('wwwroot/css/pages/pedido.css'),/max-height: 240px;\s+overflow-y: auto/);
});
test('official local snapshot contains all 27 UFs, unique IBGE identifiers and Campo Mourão',()=>{
    const rows=JSON.parse(read('wwwroot/data/municipios-ibge.json'));assert.ok(rows.length>=5500);assert.equal(new Set(rows.map(r=>r.uf)).size,27);assert.equal(new Set(rows.map(r=>r.id)).size,rows.length);assert.ok(rows.some(r=>r.id===4104303&&r.nome==='Campo Mourão'&&r.uf==='PR'));
});

test('categories failure offers retry and recovers without a duplicated category catalog',async()=>{
    const e=environment({failCategories:true});await tick();assert.match(e.doc.getElementById('categoryStatus').textContent,/Não foi possível/);
    const retry=e.doc.getElementById('categorySearch').parent.children.at(-1);assert.equal(retry.hidden,false);
    const old=e.context.window.Chamapro.api;e.context.window.Chamapro.api=async route=>route==='/categorias'?[{id:55,nome:'Serviço da API'}]:old(route);
    await retry.emit('click');await tick();const input=e.doc.getElementById('categorySearch');input.focus();assert.equal(e.doc.getElementById('categoryOptions').children[0].textContent,'Serviço da API');
});
test('urgent mode always sends no desired date, even with stale legacy controls',async()=>{
    const e=await ready();e.nodes.get('serviceType').value='AGENDADO';e.nodes.get('desiredDate').value='2099-11-03T10:00';await e.nodes.get('confirmOrder').emit('click');const body=e.calls.find(c=>c.route==='/pedidos').body;assert.equal(body.tipoAtendimento,'IMEDIATO');assert.equal(body.dataDesejada,null);assert.equal(body.cidade,'São Paulo');
});
test('favorite selection, editable data and file references survive back/forward without a redundant search',async()=>{
    const e=await ready({search:'?profissionalId=7'});const file={name:'foto.png'};e.nodes.get('photos').files=[file];const form=e.nodes.get('createOrderForm');
    await form.emit('chamapro:stepchange',{detail:2});await tick();const queries=e.calls.filter(c=>c.route.startsWith('/profissionais?')).length;
    await form.emit('chamapro:stepchange',{detail:1});await form.emit('chamapro:stepchange',{detail:2});await tick();assert.equal(e.calls.filter(c=>c.route.startsWith('/profissionais?')).length,queries);assert.equal(e.nodes.get('photos').files[0],file);assert.equal(e.nodes.get('description').value,'Descrição do serviço de teste');
});
test('API helper preserves status and business code for confirmation error handling',async()=>{
    const context={FormData,Intl,sessionStorage:{getItem:()=>null},document:{addEventListener(){}},window:{},fetch:async()=>({ok:false,status:422,text:async()=>JSON.stringify({codigo:'SEM_PROFISSIONAIS_COMPATIVEIS',detalhe:'Não há profissionais',titulo:'Operação não permitida'})})};vm.createContext(context);require('./helpers/api.cjs').install(context);vm.runInContext(read('wwwroot/js/api.js'),context);
    await assert.rejects(context.window.Chamapro.api('/pedidos',{method:'POST',body:{}}),error=>error.status===422&&error.codigo==='SEM_PROFISSIONAIS_COMPATIVEIS'&&error.message==='Não há profissionais');
});
