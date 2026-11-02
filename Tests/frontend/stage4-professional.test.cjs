const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const {document,Event}=require('./helpers/dom.cjs');
const read=file=>fs.readFileSync(path.resolve(__dirname,'../../wwwroot/js',file),'utf8');
const plain=v=>JSON.parse(JSON.stringify(v));
const tick=()=>new Promise(resolve=>setImmediate(resolve));
const intervals=[{diaSemana:1,horaInicio:'08:00:00',horaFim:'12:00:00',atendeImediato:true,atendeAgendado:false},{diaSemana:1,horaInicio:'13:00:15',horaFim:'18:00:30',atendeImediato:false,atendeAgendado:true},{diaSemana:3,horaInicio:'09:00:00',horaFim:'17:00:00',atendeImediato:false,atendeAgendado:true}];
async function fixture(guided=false,overrides={}){
 const doc=document(),root=doc.createElement('div');root.id='professionalEditor';doc.root.append(root);const calls=[];
 const saved={nome:'Profissional fictício',telefone:'11999999999',descricao:'Apresentação profissional',fotosTrabalhos:['https://example.invalid/portfolio.png'],fotoUrl:null,servicos:[{categoriaId:1,faixaPrecoMin:0,faixaPrecoMax:250}],areas:[{cidade:'São Paulo',bairro:null},{cidade:'Cidade anterior',bairro:'Bairro antigo'}],disponibilidade:plain(intervals),distanciaMaxKm:20,atendeDomicilio:true,atendeEstabelecimento:false,atendeUrgencia:true,verificado:true,...overrides};
 let failRoute;
 const api=async(route,options)=>{calls.push({route,body:options?.body&&plain(options.body)});if(route===failRoute)throw Object.assign(new Error('offline'),{status:503});if(options){if(route==='/profissionais/me')Object.assign(saved,plain(options.body));if(route.endsWith('/servicos'))saved.servicos=plain(options.body);if(route.endsWith('/areas'))saved.areas=plain(options.body);if(route.endsWith('/disponibilidade'))saved.disponibilidade=plain(options.body);return {};}
 if(route==='/profissionais/me')return plain(saved);if(route==='/categorias')return[{id:1,nome:'Pedreiro'},{id:2,nome:'Técnico em informática'}];if(route==='/painel/profissional')return{cadastroCompleto:saved.servicos.length>0&&saved.servicos.every(s=>s.faixaPrecoMin!=null&&s.faixaPrecoMax!=null)&&saved.areas.length>0&&saved.disponibilidade.length>0&&saved.distanciaMaxKm>0};return{whatsAppAtivo:false};};
 const context={document:doc,Event,FormData,fetch:async()=>({ok:true,json:async()=>[{id:3550308,nome:'São Paulo',uf:'SP'},{id:4104303,nome:'Campo Mourão',uf:'PR'}]}),window:{confirm:()=>false,Chamapro:{api,escaparHtml:value=>String(value??'').replaceAll('"','&quot;')}}};vm.createContext(context);
 for(const file of ['components/combobox.js','components/availability.js','components/professional-data.js','components/availability-editor.js','components/professional-editor.js'])vm.runInContext(read(file),context);
 await context.window.ChamaproProfessionalEditor.mount(root,guided);await tick();return{doc,root,context,saved,calls,fail:route=>{failRoute=route;},$:id=>doc.getElementById(id)};
}
test('guided onboarding and profile use the same editor and retain saved prices and complex schedules on open',async()=>{
 const e=await fixture(true);assert.ok(e.$('saveStep0'));assert.equal(e.$('description').value,'Apresentação profissional');assert.equal(e.root.querySelector('[data-price-min="1"]').value,'0');assert.equal(e.root.querySelector('[data-hours-advanced]').open,true);
 await e.$('saveStep0').emit('click');assert.equal(e.root.querySelector('[data-pro-step="1"]').hidden,false);
 assert.deepEqual(e.calls.filter(c=>c.route.endsWith('/servicos')&&c.body).at(-1).body,[{categoriaId:1,faixaPrecoMin:0,faixaPrecoMax:250}]);
});
test('category search, multiple selection, removal and readdition preserve prices',async()=>{
 const e=await fixture();const input=e.$('serviceChoiceSearch');input.value='TECNICO';await input.emit('input');await input.emit('keydown',{key:'ArrowDown'});await input.emit('keydown',{key:'Enter'});await e.$('addService').emit('click');
 await e.$('saveServices').emit('click');const body=e.calls.filter(c=>c.route.endsWith('/servicos')&&c.body).at(-1).body;assert.equal(body.length,2);assert.equal(body[0].faixaPrecoMin,0);assert.equal(body[1].faixaPrecoMin,null);
 await e.root.querySelector('[data-remove-service="1"]').emit('click');e.context.window.ChamaproCombobox.normalize('São Paulo');e.$('serviceChoice').value='1';await e.$('addService').emit('click');assert.equal(e.root.querySelector('[data-price-min="1"]').value,'0');
});
test('city with UF adds a textual region; removing one region does not alter other existing strings',async()=>{
 const e=await fixture();assert.match(e.$('regionList').innerHTML,/Cidade anterior/);const input=e.$('regionCitySearch');input.value='campo mourao';await input.emit('input');await input.emit('keydown',{key:'ArrowDown'});await input.emit('keydown',{key:'Enter'});e.$('regionNeighborhood').value='Bairro novo';await e.$('addRegion').emit('click');await e.root.querySelector('[data-remove-area="0"]').emit('click');await e.$('saveAreas').emit('click');
 const body=e.calls.find(c=>c.route.endsWith('/areas')&&c.body).body;assert.deepEqual(body,[{cidade:'Cidade anterior',bairro:'Bairro antigo'},{cidade:'Campo Mourão',bairro:'Bairro novo'}]);assert.ok(!('uf'in body[1]));
});
test('common schedule refuses silent replacement, keeps unselected days and saves all intervals',async()=>{
 const e=await fixture();e.root.querySelector('[data-common-day="1"]').checked=true;await e.root.querySelector('[data-apply-common]').emit('click');await e.$('saveHours').emit('click');assert.deepEqual(e.calls.find(c=>c.route.endsWith('/disponibilidade')&&c.body).body,intervals);
 e.context.window.confirm=()=>true;await e.root.querySelector('[data-apply-common]').emit('click');await e.$('saveHours').emit('click');const body=e.calls.filter(c=>c.route.endsWith('/disponibilidade')&&c.body).at(-1).body;assert.equal(body.length,2);assert.deepEqual(body.find(r=>r.diaSemana===3),intervals[2]);
});
test('several empty days receive a common interval, while advanced editor supports extra intervals',async()=>{
 const e=await fixture(false,{disponibilidade:[]});for(const day of [1,2,3,4,5])e.root.querySelector(`[data-common-day="${day}"]`).checked=true;await e.root.querySelector('[data-apply-common]').emit('click');await e.$('saveHours').emit('click');assert.equal(e.saved.disponibilidade.length,5);
 e.root.querySelector('[data-extra-day]').value='6';await e.root.querySelector('[data-add-interval]').emit('click');await e.$('saveHours').emit('click');assert.equal(e.saved.disponibilidade.length,6);
});
test('API failure keeps drafts editable, avoids claiming success and permits retry',async()=>{
 const e=await fixture();e.fail('/profissionais/me/servicos');e.root.querySelector('[data-price-max="1"]').value='350';await e.$('saveServices').emit('click');assert.match(e.$('professionalMessage').textContent,/Não foi possível/);assert.equal(e.$('professionalFields').inert,false);assert.equal(e.saved.servicos[0].faixaPrecoMax,250);e.fail(null);await e.$('saveServices').emit('click');assert.equal(e.saved.servicos[0].faixaPrecoMax,350);
});
test('completion never hides price requirement or promises eligibility without identity verification',async()=>{
 const e=await fixture(true,{servicos:[{categoriaId:1,faixaPrecoMin:null,faixaPrecoMax:null}],verificado:false});assert.match(e.$('professionalReadiness').innerHTML,/preços/);assert.match(e.$('professionalReadiness').innerHTML,/não está verificada/);assert.doesNotMatch(e.$('professionalReadiness').innerHTML,/pode receber pedidos/);
});
test('saving other sections leaves photos and contact intact and uses only the existing contracts',async()=>{
 const e=await fixture();e.$('description').value='Nova apresentação';await e.$('saveInfo').emit('click');const body=e.calls.find(c=>c.route==='/profissionais/me'&&c.body).body;assert.deepEqual(body.fotosTrabalhos,['https://example.invalid/portfolio.png']);assert.equal(e.calls.filter(c=>c.route==='/whatsapp/contato/me'&&c.body).length,0);assert.deepEqual(Object.keys(body).sort(),['nome','telefone','descricao','fotoUrl','fotosTrabalhos','experienciaAnos','distanciaMaxKm','atendeUrgencia','atendeDomicilio','atendeEstabelecimento'].sort());
});

test('required fields reject saves without requests; new categories may stay unpriced but remain incomplete',async()=>{
 const e=await fixture(true,{servicos:[],descricao:''});const input=e.$('serviceChoiceSearch');input.focus();await input.emit('keydown',{key:'ArrowDown'});await input.emit('keydown',{key:'Enter'});await e.$('addService').emit('click');
 await e.$('saveStep0').emit('click');assert.equal(e.calls.filter(c=>c.body).length,0);e.$('description').value='Faço serviços residenciais';await e.$('saveStep0').emit('click');assert.equal(e.saved.servicos.length,1);assert.equal(e.saved.servicos[0].faixaPrecoMin,null);assert.match(e.$('professionalReadiness').innerHTML,/preços/);
});
test('moving back in guided registration retains unsaved prices and regions',async()=>{
 const e=await fixture(true);e.root.querySelector('[data-price-max="1"]').value='325';await e.$('saveStep0').emit('click');await e.root.querySelector('[data-pro-back="0"]').emit('click');assert.equal(e.root.querySelector('[data-price-max="1"]').value,'325');assert.deepEqual(e.saved.areas,[{cidade:'São Paulo',bairro:null},{cidade:'Cidade anterior',bairro:'Bairro antigo'}]);
});
test('advanced editing and deliberate removal affect only their target interval',async()=>{
 const e=await fixture();e.root.querySelector('[data-day-start="1-1"]').value='14:00:15';e.root.querySelector('[data-day-enabled="1-0"]').checked=false;await e.$('saveHours').emit('click');assert.equal(e.saved.disponibilidade.length,2);assert.equal(e.saved.disponibilidade.find(r=>r.diaSemana===1).horaInicio,'14:00:15');assert.deepEqual(e.saved.disponibilidade.find(r=>r.diaSemana===3),intervals[2]);
});
test('a fresh profile load reflects previously persisted prices and every saved interval',async()=>{
 const first=await fixture();first.root.querySelector('[data-price-max="1"]').value='500';await first.$('saveServices').emit('click');const reopened=await fixture(false,plain(first.saved));assert.equal(reopened.root.querySelector('[data-price-max="1"]').value,'500');await reopened.$('saveHours').emit('click');assert.deepEqual(reopened.saved.disponibilidade,intervals);
});

test('removing every interval requires explicit confirmation and never marks an empty schedule complete',async()=>{
 const e=await fixture();for(const toggle of e.root.querySelectorAll('[data-day-enabled]:checked'))toggle.checked=false;await e.$('saveHours').emit('click');assert.deepEqual(e.saved.disponibilidade,intervals);
 e.context.window.confirm=()=>true;await e.$('saveHours').emit('click');assert.deepEqual(e.saved.disponibilidade,[]);assert.match(e.$('professionalReadiness').innerHTML,/Defina seus horários/);
});

test('region write followed by profile failure reports partial save, retains drafts and recovers on retry',async()=>{
 const e=await fixture();await e.root.querySelector('[data-remove-area="0"]').emit('click');e.$('distance').value='35';e.fail('/profissionais/me');await e.$('saveAreas').emit('click');
 assert.match(e.$('professionalMessage').textContent,/Algumas informações já foram salvas/);assert.equal(e.saved.areas.length,1);assert.equal(e.saved.distanciaMaxKm,20);assert.equal(e.$('distance').value,'35');
 e.fail('');await e.$('saveAreas').emit('click');assert.equal(e.saved.distanciaMaxKm,35);assert.equal(e.saved.areas.length,1);assert.equal(e.saved.servicos[0].faixaPrecoMin,0);
});

test('hours write followed by urgency failure is explicit and keeps every other saved interval and price',async()=>{
 const e=await fixture();e.root.querySelector('[data-day-start="1-0"]').value='09:00';e.fail('/profissionais/me');await e.$('saveHours').emit('click');
 assert.match(e.$('professionalMessage').textContent,/Algumas informações já foram salvas/);assert.equal(e.saved.disponibilidade.length,3);assert.equal(e.saved.disponibilidade[0].horaInicio,'09:00');assert.deepEqual(e.saved.disponibilidade.slice(1),intervals.slice(1));assert.equal(e.saved.servicos[0].faixaPrecoMin,0);
});
