(() => {
    const C = window.Chamapro, safe = C.escaparHtml;
    const normalize = value => String(value || '').replaceAll('_', '').toUpperCase();
    const labels = {PENDENTE:'Pendente',EMANALISE:'Em análise',DOCUMENTACAOPENDENTE:'Documentação pendente',APROVADO:'Aprovado',REJEITADO:'Rejeitado',SUSPENSO:'Suspenso',RESOLVIDO:'Resolvido',ACEITO:'Aceito',AGENDADO:'Agendado',EMANDAMENTO:'Em andamento',CONCLUIDO:'Concluído',CANCELADO:'Cancelado',ORCADO:'Orçado',CLIENTE:'Cliente',PROFISSIONAL:'Profissional',ADMIN:'Administrador'};
    const label = value => labels[normalize(value)] || String(value || 'Não informado');
    const date = value => value ? new Date(value).toLocaleString('pt-BR') : 'Não informada';
    const states = ['PENDENTE','ORCADO','ACEITO','AGENDADO','EM_ANDAMENTO','CONCLUIDO','CANCELADO'];
    const verificationStates = ['Pendente','EmAnalise','DocumentacaoPendente'];
    const sections = {inicio:'Início',verificacoes:'Verificações',denuncias:'Denúncias',usuarios:'Usuários',pedidos:'Pedidos',categorias:'Categorias',atividade:'Atividade administrativa'};
    const aliases = {verificationList:'verificacoes',reportsList:'denuncias',usersList:'usuarios',adminOrders:'pedidos',categoryList:'categorias',categoryForm:'categorias',adminAudit:'atividade',metrics:'inicio',categoryReport:'inicio',topRatedProfessionals:'inicio'};
    function errorText(error) {
        if(error?.status === 401) return 'Sua sessão expirou. Entre novamente para continuar.';
        if(error?.status === 403) return 'Você não tem permissão para acessar ou alterar estas informações.';
        if(error?.status === 404) return 'O registro não está mais disponível. Atualize a consulta.';
        // Business/validation errors are safe messages from the API contract. Never echo arbitrary server failures.
        if([400,409,422].includes(error?.status)) {
            const text=String(error.message || '');
            if(text.length<=300 && !/stack|exception|npgsql|select\s|password|connection|token|trace/i.test(text))return text;
            return 'A operação foi recusada. Confira os campos e a situação atual do registro.';
        }
        return 'Não foi possível concluir a solicitação. Tente novamente.';
    }
    function categoryPayload(name,icon) {
        const nome=name.trim(),icone=icon.trim()||null;
        if(nome.length<3||nome.length>60)throw Error('O nome deve ter entre 3 e 60 caracteres.');
        if(icone?.length>60)throw Error('O ícone deve ter até 60 caracteres.');
        return {nome,icone};
    }
    const documentPath = (value, origin) => window.ChamaproPrivateDocuments.path(value, origin);
    async function mount(root,user) {
        if(user?.perfil!=='ADMIN'){root.innerHTML='<p role="alert">Acesso restrito à administração.</p>';return;}
        let active='',sequence=0,busy=false;
        const privateDocuments=window.ChamaproPrivateDocuments.create();
        root.innerHTML=`<nav class="app-toolbar" aria-label="Seções administrativas">${Object.entries(sections).map(([id,name])=>`<a class="btn btn-secondary" href="#${id}" data-admin-nav="${id}">${name}</a>`).join('')}</nav><p id="adminMessage" role="status" aria-live="polite" class="app-message"></p><div id="adminView"></div>`;
        const view=root.querySelector('#adminView'),message=root.querySelector('#adminMessage');
        const $=id=>view.querySelector('#'+id);
        function releaseDocument(){privateDocuments.release();}
        function fail(target,error,retry){
            target.innerHTML=`<div role="alert"><p>${safe(errorText(error))}</p>${error?.status===401?'<a href="/pages/autenticacao/login.html">Entrar novamente</a>':error?.status===403?'':'<button type="button" class="btn btn-secondary" data-retry>Tentar novamente</button>'}</div>`;
            target.querySelector('[data-retry]')?.addEventListener('click',retry);
        }
        async function change({path,method='PATCH',body,question,reload,after}) {
            if(busy||user.perfil!=='ADMIN')return;
            if(!window.confirm(question))return;
            busy=true;message.textContent='Salvando alteração…';view.setAttribute('aria-busy','true');
            view.querySelectorAll('[data-admin-controls]').forEach(el=>el.disabled=true);
            try {
                await C.api(path,{method,body});
                after?.();
                const refreshed=await reload();
                message.textContent=refreshed===false?'A alteração foi salva, mas a consulta não pôde ser atualizada. Atualize antes de executar outra ação.':'Alteração salva. A consulta foi atualizada.';
            }catch(error){message.textContent=errorText(error);}
            finally{busy=false;view.setAttribute('aria-busy','false');view.querySelectorAll('[data-admin-controls]').forEach(el=>el.disabled=false);}
        }
        function heading(text,hint){view.innerHTML=`<h1 id="adminHeading" tabindex="-1">${text}</h1><p>${hint}</p>`;}
        async function home(ticket) {
            heading('O que precisa da sua atenção agora?','Abra uma fila para analisar pendências. Os números abaixo vêm de consultas reais, sem estimar totais a partir de amostras.');
            view.innerHTML+='<div class="app-grid-two"><section class="app-card"><h2>Verificações a analisar</h2><div id="verificationSummary" aria-live="polite"></div><a href="#verificacoes" class="btn btn-primary">Analisar verificações</a></section><section class="app-card"><h2>Denúncias a analisar</h2><div id="reportSummary" aria-live="polite"></div><a href="#denuncias" class="btn btn-secondary">Revisar denúncias</a></section></div><details class="app-card"><summary>Informações gerais da plataforma</summary><div id="platformSummary" aria-live="polite"></div></details><p>Não há indicador automático de pedidos que exigem intervenção ou de pendências de usuários. Consulte os registros antes de decidir.</p>';
            async function summary(targetId,path,render){const target=$(targetId);target.innerHTML='<p role="status">Carregando…</p>';try{const data=await C.api(path);if(ticket!==sequence)return;target.innerHTML=render(data);}catch(e){if(ticket===sequence)fail(target,e,()=>summary(targetId,path,render));}}
            await Promise.allSettled([
                summary('verificationSummary','/verificacoes',items=>`<p>${items.length} solicitações na fila de verificação.</p><p>Inclui pendentes, em análise e documentação pendente. Uma pessoa pode ter mais de uma solicitação.</p>`),
                summary('reportSummary','/denuncias',items=>`<p>${items.length} denúncias pendentes ou em análise.</p>`),
                summary('platformSummary','/admin/relatorios',data=>`<p>${safe(data.usuarios)} usuários · ${safe(data.profissionais)} profissionais · ${safe(data.profissionaisVerificados)} profissionais verificados.</p><p>${safe(data.chamados)} pedidos · ${safe(data.chamadosPendentes)} pendentes · ${safe(data.chamadosConcluidos)} concluídos · ${safe(data.chamadosCancelados)} cancelados.</p><h2>Categorias mais solicitadas</h2>${data.categoriasMaisSolicitadas.map(c=>`<p>${safe(c.categoria)}: ${safe(c.chamados)} pedidos</p>`).join('')||'<p>Ainda não há pedidos por categoria.</p>'}<h2>Profissionais com mais avaliações</h2>${(data.profissionaisMaisAvaliados||[]).map(p=>`<p>${safe(p.nome)} · ${safe(p.media)} / 5 · ${safe(p.avaliacoes)} avaliações</p>`).join('')||'<p>Ainda não há avaliações.</p>'}`)
            ]);
        }
        // All local pagination describes its actual source. Unsupported query parameters are never sent.
        async function collection({title,hint,path,filter,selectOptions,card,enrich,setup,extra='',id,searchLabel='Buscar nos registros carregados',searchValues},ticket) {
            heading(title,hint);
            view.innerHTML+=`${extra}<form id="adminFilters" class="app-toolbar"><label for="adminSearch">${safe(searchLabel)}</label><input id="adminSearch" type="search" maxlength="100"><label for="adminFilter" ${selectOptions.length?'':'hidden'}>Situação</label><select id="adminFilter" ${selectOptions.length?'':'hidden'}><option value="">Todas</option>${selectOptions.map(([v,t])=>`<option value="${v}">${t}</option>`).join('')}</select><button type="submit" class="btn btn-secondary">Atualizar consulta</button></form><p class="form-hint">Busca e paginação abaixo são locais, sobre os registros retornados por esta consulta.</p><div id="${id}" class="app-stack" aria-live="polite"></div><div class="app-actions"><button id="adminPrev" type="button" class="btn btn-secondary">Anterior</button><span id="adminPage" role="status"></span><button id="adminNext" type="button" class="btn btn-secondary">Próxima</button></div>`;
            let records=[],page=1,revision=0,ready=false;
            const target=$(id);
            function filtered(){const search=$('adminSearch').value.trim().toLocaleLowerCase('pt-BR'),status=$('adminFilter').value;return records.filter(r=>(!status||filter(r)===status)&&(!search||(searchValues?searchValues(r):[r.titulo,r.nome,r.adminNome,r.acao,r.motivo,r.categoria,r.cidade,r.bairro,r.tipoDocumento]).join(' ').toLocaleLowerCase('pt-BR').includes(search)));}
            async function draw(){
                if(!ready)return;
                const revisionId=++revision,items=filtered(),slice=items.slice((page-1)*10,page*10);
                releaseDocument();target.innerHTML='<p role="status">Preparando registros…</p>';
                if(enrich){await Promise.allSettled(slice.map(enrich));if(ticket!==sequence||revisionId!==revision)return;}
                target.innerHTML=slice.length?slice.map(card).join(''):'<p class="empty-state">Nenhum registro corresponde a esta consulta. Não há pendências nesta seleção.</p>';
                $('adminPrev').disabled=page===1;$('adminNext').disabled=page*10>=items.length;
                $('adminPage').textContent=`Página ${page} de ${Math.max(1,Math.ceil(items.length/10))} · ${items.length} registros nesta seleção`;
                setup?.(target,load);
            }
            async function load(){const loadId=++revision;ready=false;records=[];releaseDocument();target.innerHTML='<p role="status">Carregando…</p>';$('adminPrev').disabled=true;$('adminNext').disabled=true;try{const data=await C.api(path);if(ticket!==sequence||loadId!==revision)return false;records=data;ready=true;page=1;await draw();return true;}catch(e){if(ticket===sequence&&loadId===revision)fail(target,e,load);return false;}}
            $('adminFilters').addEventListener('submit',e=>{e.preventDefault();if(!busy)return load();});
            for(const id of ['adminSearch','adminFilter'])$(id).addEventListener(id==='adminSearch'?'input':'change',()=>{if(!busy){page=1;draw();}});
            $('adminPrev').addEventListener('click',()=>{if(!busy&&page>1){page--;draw();}});$('adminNext').addEventListener('click',()=>{if(!busy&&page*10<filtered().length){page++;draw();}});
            await load();return load;
        }
        async function verifications(ticket){
            await collection({title:'Verificações de profissionais',hint:'Esta fila contém somente solicitações pendentes, em análise ou com documentação pendente. Aprovações e rejeições encerradas não estão disponíveis nesta consulta.',path:'/verificacoes',id:'verificationList',searchLabel:'Buscar tipo de documento nesta fila',searchValues:r=>[r.tipoDocumento],filter:r=>r.status,selectOptions:verificationStates.map(s=>[s,label(s)]),
                enrich:async r=>{if(!r.profissional){try{r.profissional=await C.api(`/profissionais/${r.profissionalId}`);}catch{r.profileUnavailable=true;}}},
                card:r=>`<article class="app-card"><h2>${safe(r.profissional?.nome||'Perfil público indisponível')}</h2><p>${safe(label(r.status))} · Solicitado em ${safe(date(r.criadoEm))}</p><p>Serviços: ${safe(r.profissional?.especialidades.map(s=>s.categoria).join(', ')||'Não disponíveis nesta consulta')}</p><p>Regiões: ${safe(r.profissional?.areasAtendimento.join(', ')||'Não disponíveis nesta consulta')}</p><details><summary>Detalhes e decisão sobre ${safe(r.tipoDocumento)}</summary><p>Documento informado: ${safe(r.tipoDocumento)}</p><p>${safe(r.observacao||'Nenhuma observação informada.')}</p><p>${r.profissional?'Identidade atualmente '+(r.profissional.verificado?'verificada':'não verificada')+'.':'Não foi possível consultar o perfil público. A solicitação não foi omitida; atualize a consulta para tentar novamente.'}</p><p>Esta solicitação confirma o envio do documento. Esta consulta não confirma a conclusão do cadastro nem todos os requisitos profissionais.</p><button type="button" class="btn btn-secondary" data-document="${r.id}">Consultar documento protegido</button><div data-document-result="${r.id}" aria-live="polite"></div><fieldset data-admin-controls><legend>Decisão sobre esta solicitação</legend><label for="verificationNote${r.id}">Observação ao profissional (opcional)</label><textarea id="verificationNote${r.id}" maxlength="500" aria-describedby="verificationError${r.id}"></textarea><p id="verificationError${r.id}" role="status"></p><div class="app-actions">${[['EmAnalise','Iniciar análise'],['Aprovado','Aprovar'],['DocumentacaoPendente','Solicitar correção'],['Rejeitado','Rejeitar']].map(([s,t])=>`<button type="button" class="btn ${s==='Aprovado'?'btn-primary':'btn-secondary'}" data-verification="${r.id}" data-decision="${s}" data-name="${safe(r.profissional?.nome||r.tipoDocumento)}">${t}</button>`).join('')}</div></fieldset></details></article>`,
                setup:(target,reload)=>{
                    target.querySelectorAll('[data-verification]').forEach(b=>b.addEventListener('click',()=>{const rId=Number(b.dataset.verification),note=$('verificationNote'+rId).value.trim();if(note.length>500){$('verificationError'+rId).textContent='Use até 500 caracteres.';return;}
                        return change({path:`/verificacoes/${rId}/avaliar`,body:{status:b.dataset.decision,observacao:note||null},question:`Marcar a solicitação ${rId} de ${b.dataset.name} como ${label(b.dataset.decision)}? A decisão será comunicada ao profissional e registrada. Aprovar pode verificar a identidade; rejeitar ou pedir correção não apaga outras aprovações existentes. Confira o nome e o documento desta solicitação antes de confirmar.`,reload});
                    }));
                    target.querySelectorAll('[data-document]').forEach(b=>b.addEventListener('click',async()=>{
                        if(b.disabled)return;b.disabled=true;const output=target.querySelector(`[data-document-result="${b.dataset.document}"]`);output.textContent='Consultando documento protegido…';
                        try{const items=await C.api('/verificacoes');const item=items.find(r=>r.id===Number(b.dataset.document));if(!item)throw{status:404};const path=documentPath(item.urlDocumento,location.origin);if(!path)throw{status:422,message:'O documento precisa usar a rota privada deste servidor.'};
                            const blobUrl=await privateDocuments.load(path);if(ticket!==sequence||!target.contains(output)){privateDocuments.release(blobUrl);return;}if(output.dataset.blobUrl)privateDocuments.release(output.dataset.blobUrl);output.dataset.blobUrl=blobUrl;output.innerHTML=`<a class="btn btn-secondary" href="${blobUrl}" target="_blank" rel="noopener">Abrir documento autenticado</a><p>Documento privado: use apenas para esta análise.</p>`;
                        }catch(e){output.textContent=errorText(e);}finally{b.disabled=false;}
                    }));
                }
            },ticket);
        }
        async function reports(ticket){await collection({title:'Denúncias a analisar',hint:'Fluxo de denúncias existente: pendentes e em análise. Decisões encerradas saem desta fila.',path:'/denuncias',id:'reportsList',filter:r=>r.status,selectOptions:[['Pendente','Pendente'],['EmAnalise','Em análise']],
            card:r=>`<article class="app-card"><h2>${safe(r.motivo)}</h2><p>${safe(label(r.status))} · ${safe(date(r.criadoEm))}</p><details><summary>Analisar denúncia</summary><p>Referência: ${safe(r.tipoEntidade)} ${r.entidadeId}</p><p>${safe(r.descricao)}</p><p>${safe(r.evidencias||'Sem evidências textuais informadas.')}</p><fieldset data-admin-controls><legend>Providência administrativa</legend><label for="reportNote${r.id}">Observação (opcional)</label><textarea id="reportNote${r.id}" maxlength="2000" aria-describedby="reportError${r.id}">${safe(r.observacaoAdmin||'')}</textarea><p id="reportError${r.id}" role="status"></p><div class="app-actions">${[['EmAnalise','Em análise'],['Resolvido','Resolver'],['Rejeitado','Rejeitar']].map(([s,t])=>`<button class="btn btn-secondary" type="button" data-report="${r.id}" data-decision="${s}">${t}</button>`).join('')}</div></fieldset></details></article>`,
            setup:(target,reload)=>target.querySelectorAll('[data-report]').forEach(b=>b.addEventListener('click',()=>{const note=$('reportNote'+b.dataset.report).value.trim();if(note.length>2000){$('reportError'+b.dataset.report).textContent='Use até 2000 caracteres.';return;}return change({path:`/denuncias/${b.dataset.report}/resolver`,body:{status:b.dataset.decision,observacao:note||null},question:`Marcar a denúncia ${b.dataset.report} como ${label(b.dataset.decision)}? A decisão será registrada e comunicada a quem denunciou. Esta ação não bloqueia contas automaticamente.`,reload});}))},ticket);}
        async function users(ticket){
            heading('Consulta de usuários','A pesquisa por nome/e-mail e os filtros consideram a base de usuários. São exibidos até 200 resultados, sem próximas páginas: refine a pesquisa quando atingir esse limite.');
            view.innerHTML+='<form id="userFilters" class="app-toolbar"><label for="userSearch">Nome ou e-mail</label><input id="userSearch" type="search" maxlength="100"><label for="userRole">Tipo de conta</label><select id="userRole"><option value="">Todos</option><option value="Cliente">Clientes</option><option value="Profissional">Profissionais</option><option value="Admin">Administradores</option></select><label for="userActive">Situação</label><select id="userActive"><option value="">Todas</option><option value="true">Ativas</option><option value="false">Bloqueadas</option></select><button type="submit" class="btn btn-primary">Buscar usuários</button></form><p id="userLimit" role="status"></p><div id="usersList" class="app-stack" aria-live="polite"></div>';
            let revision=0;
            async function load(){const request=++revision,list=$('usersList');list.innerHTML='<p role="status">Carregando usuários…</p>';const query=new URLSearchParams();for(const [id,key]of [['userSearch','busca'],['userRole','perfil'],['userActive','ativo']]){const value=$(id).value.trim();if(value)query.set(key,value);}if($('userSearch').value.trim().length>100){message.textContent='Use até 100 caracteres na busca.';return false;}
                try{const items=await C.api('/admin/usuarios?'+query);if(ticket!==sequence||request!==revision)return false;
                    $('userLimit').textContent=items.length===200?'Limite de 200 resultados atingido. Há possibilidade de outros registros. Refine a busca.':`${items.length} resultados encontrados nesta consulta.`;
                    list.innerHTML=items.length?items.map(u=>`<article class="app-card"><h2>${safe(u.nome)}</h2><p>${safe(label(u.perfil))} · ${u.ativo?'Conta ativa':'Conta bloqueada'}</p><details><summary>Contato e ações da conta de ${safe(u.nome)}</summary><p>E-mail: ${safe(u.email)}</p>${u.id===user.id?'<p>Você não pode alterar a situação da sua própria conta administrativa.</p>':`<fieldset data-admin-controls><legend>Alterar acesso</legend><button class="btn btn-secondary" type="button" data-user="${u.id}">${u.ativo?'Bloquear conta':'Reativar conta'}</button></fieldset>`}</details></article>`).join(''):'<p class="empty-state">Nenhum usuário encontrado para esta pesquisa.</p>';
                    list.querySelectorAll('[data-user]').forEach(b=>b.addEventListener('click',()=>{const u=items.find(u=>u.id===Number(b.dataset.user));if(!u||u.id===user.id)return;return change({path:`/admin/usuarios/${u.id}/status`,body:{ativo:!u.ativo},question:`${u.ativo?'Bloquear':'Reativar'} a conta de ${u.nome} (${label(u.perfil)})? ${u.ativo?'O acesso dessa conta será bloqueado conforme as regras da plataforma.':'A conta voltará ao estado ativo.'} O tipo de conta e suas permissões não serão alterados.`,reload:load});}));return true;
                }catch(e){if(ticket===sequence&&request===revision)fail(list,e,load);return false;}}
            $('userFilters').addEventListener('submit',e=>{e.preventDefault();if(!busy)return load();});await load();
        }
        async function orders(ticket){await collection({title:'Consulta de pedidos',hint:'Os pedidos recebidos são divididos em páginas apenas nesta tela. A consulta não disponibiliza endereço completo, proposta ou agenda.',path:'/admin/pedidos',id:'adminOrders',filter:r=>r.status,selectOptions:states.map(s=>[s,label(s)]),
            card:p=>`<article class="app-card"><h2>${safe(p.titulo)}</h2><p>${safe(label(p.status))} · ${safe(p.categoria)} · ${safe([p.cidade,p.bairro].filter(Boolean).join(' · '))}</p><details><summary>Dados disponíveis e intervenção</summary><p>Criado em ${safe(date(p.criadoEm))}</p><p>Cliente: ${safe(p.clienteNome)} · Profissional escolhido: ${safe(p.profissionalNome||'Ainda não escolhido')}</p><fieldset data-admin-controls><legend>Alteração administrativa de status</legend><p>Esta ação altera diretamente a situação do pedido, sem ajustar automaticamente agendamento, proposta ou profissional escolhido. Verifique essas implicações antes de intervir.</p><label for="orderState${p.id}">Novo status</label><select id="orderState${p.id}">${states.map(s=>`<option value="${s}">${label(s)}</option>`).join('')}</select><button class="btn btn-secondary" type="button" data-order="${p.id}" data-current="${p.status}" data-title="${safe(p.titulo)}">Revisar alteração</button></fieldset></details></article>`,
            setup:(target,reload)=>target.querySelectorAll('[data-order]').forEach(b=>{const select=$('orderState'+b.dataset.order);select.value=b.dataset.current;b.addEventListener('click',()=>{const status=select.value;if(!states.includes(status)){message.textContent='Selecione um status válido.';return;}if(status===b.dataset.current){message.textContent='Escolha um status diferente do atual para alterar o pedido.';return;}return change({path:`/admin/pedidos/${b.dataset.order}/status`,body:{status},question:`Alterar o pedido ${b.dataset.order} (${b.dataset.title}) de ${label(b.dataset.current)} para ${label(status)}? Esta intervenção será registrada. Esta alteração não sincroniza agendamento ou proposta e não atribui profissional. Confirme somente após analisar essas consequências.`,reload});});})},ticket);}
        async function categories(ticket){
            let load,editedName='';
            const extra='<form id="categoryForm" class="app-card app-stack"><h2 id="categoryHeading">Nova categoria</h2><input id="categoryId" type="hidden"><fieldset data-admin-controls class="app-stack"><legend>Dados da categoria</legend><label for="categoryName">Nome</label><input id="categoryName" required minlength="3" maxlength="60" aria-describedby="categoryMessage"><label for="categoryIcon">Ícone (opcional)</label><input id="categoryIcon" maxlength="60" aria-describedby="categoryMessage"><p id="categoryMessage" role="status"></p><div class="app-actions"><button type="submit" class="btn btn-primary">Salvar categoria</button><button id="clearCategory" type="button" class="btn btn-secondary">Cancelar edição</button></div></fieldset></form>';
            load=await collection({title:'Categorias ativas',hint:'Criar, editar e retirar categorias são operações já existentes. São exibidas somente categorias ativas. Esta lista não permite consultar ou reativar categorias inativas.',path:'/categorias',id:'categoryList',filter:()=>'',selectOptions:[],extra,
                card:c=>`<article class="app-card"><h2>${safe(c.nome)}</h2><p>Ativa · Ícone: ${safe(c.icone||'Não informado')}</p><fieldset data-admin-controls><legend>Ações da categoria</legend><button class="btn btn-secondary" type="button" data-category="${c.id}" data-name="${safe(c.nome)}" data-icon="${safe(c.icone||'')}">Editar categoria</button><button class="btn btn-secondary" type="button" data-remove-category="${c.id}" data-name="${safe(c.nome)}">Retirar categoria</button></fieldset></article>`,
                setup:(target,reload)=>{
                    target.querySelectorAll('[data-category]').forEach(b=>b.addEventListener('click',()=>{if(busy)return;editedName=b.dataset.name;$('categoryId').value=b.dataset.category;$('categoryName').value=b.dataset.name;$('categoryIcon').value=b.dataset.icon;$('categoryHeading').textContent='Editar '+b.dataset.name;$('categoryName').focus();}));
                    target.querySelectorAll('[data-remove-category]').forEach(b=>b.addEventListener('click',()=>change({path:`/categorias/${b.dataset.removeCategory}`,method:'DELETE',question:`Retirar a categoria ${b.dataset.name}? Se houver vínculos com pedidos ou profissionais, ela será desativada; sem vínculos, será excluída. Ela deixará a lista ativa.`,reload})));}
            },ticket);
            if(ticket!==sequence)return;
            function clear(){editedName='';for(const id of ['categoryId','categoryName','categoryIcon'])$(id).value='';$('categoryHeading').textContent='Nova categoria';$('categoryMessage').textContent='';}
            $('clearCategory').addEventListener('click',()=>{if(!busy)clear();});
            $('categoryForm').addEventListener('submit',e=>{e.preventDefault();let body;try{body=categoryPayload($('categoryName').value,$('categoryIcon').value);}catch(error){$('categoryMessage').textContent=error.message;$('categoryName').focus();return;}const id=$('categoryId').value;return change({path:id?`/categorias/${id}`:'/categorias',method:id?'PUT':'POST',body,question:id?`Alterar a categoria ${editedName} para ${body.nome}? O nome e o ícone serão usados no catálogo de serviços.`:`Criar a categoria ${body.nome}? O nome e o ícone serão usados no catálogo de serviços.`,reload:load,after:clear});});
        }
        async function audit(ticket){await collection({title:'Atividade administrativa',hint:'Últimas 50 ações retornadas pela auditoria. Esta consulta não representa todo o histórico.',path:'/admin/auditoria?limite=50',id:'adminAudit',filter:r=>r.entidade,selectOptions:[['USUARIO','Usuários'],['PEDIDO','Pedidos'],['CATEGORIA','Categorias'],['VERIFICACAO_PROFISSIONAL','Verificações'],['DENUNCIA','Denúncias']],card:r=>`<article class="app-card"><h2>${safe({USUARIO_ATIVADO:'Conta reativada',USUARIO_BLOQUEADO:'Conta bloqueada',STATUS_ALTERADO:'Status de pedido alterado',VERIFICACAO_AVALIADA:'Verificação avaliada',DENUNCIA_RESOLVIDA:'Denúncia avaliada',CATEGORIA_CRIADA:'Categoria criada',CATEGORIA_EDITADA:'Categoria editada',CATEGORIA_DESATIVADA:'Categoria desativada',CATEGORIA_REMOVIDA:'Categoria removida'}[r.acao]||'Ação administrativa')}</h2><p>${safe(r.adminNome)} · ${safe(date(r.criadoEm))}</p><p>${safe(label(r.estadoAnterior))} → ${safe(label(r.estadoNovo))}</p><details><summary>Referência do registro</summary><p>${safe(r.entidade)} ${r.entidadeId}</p></details></article>`},ticket);}
        const handlers={inicio:home,verificacoes:verifications,denuncias:reports,usuarios:users,pedidos:orders,categorias:categories,atividade:audit};
        async function navigate(focus=true){
            const hash=location.hash.slice(1),section=sections[hash]?hash:aliases[hash]||'inicio';
            if(busy){location.hash='#'+active;return;}
            active=section;const ticket=++sequence;releaseDocument();message.textContent='';
            root.querySelectorAll('[data-admin-nav]').forEach(a=>{if(a.dataset.adminNav===section)a.setAttribute('aria-current','page');else a.removeAttribute('aria-current');});
            await handlers[section](ticket);if(ticket===sequence&&focus)$('adminHeading')?.focus();
        }
        root.querySelectorAll('[data-admin-nav]').forEach(a=>a.addEventListener('click',e=>{if(busy)e.preventDefault();}));
        window.addEventListener('hashchange',()=>navigate());window.addEventListener('pagehide',releaseDocument);
        await navigate(false);
    }
    window.ChamaproAdmin={mount,errorText,categoryPayload,documentPath};
    window.ChamaproApp?.ready.then(user=>{const root=document.getElementById('adminWorkspace');if(root)mount(root,user);});
})();
