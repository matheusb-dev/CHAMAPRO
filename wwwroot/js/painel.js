// Painel de CLIENTE e PROFISSIONAL (mesmos endpoints GET /pedidos; ações mudam por perfil).
const $ = s => document.querySelector(s);
const ST = { PENDENTE: 'Pendente', ACEITO: 'Aceito', AGENDADO: 'Agendado', EM_ANDAMENTO: 'Em andamento', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado', RECUSADO: 'Recusado' };
const badge = s => `<span class="badge st-${String(s).toLowerCase()}">${ST[s] || Api.esc(s)}</span>`;
let pagina = 1;
const act = async (fn, ok) => { try { await fn(); Api.toast(ok, 'success'); await carregar(pagina); } catch (e) { Api.toast(e.message); } };

async function carregar(p = 1) {
  pagina = p; $('#lista').innerHTML = '<p class="subtitle">Carregando…</p>';
  try {
    const r = await Api.get('/pedidos', { query: { status: $('#filtro').value, pagina: p, tamanho: 10 } });
    // Se o backend ainda devolver {} (PagedResponse sem propriedades públicas), r.itens não existe:
    if (!Array.isArray(r?.itens)) throw new Error('A API devolveu a lista sem o campo "itens" (veja o ajuste em Services/PagedResponse.cs).');
    $('#lista').innerHTML = r.itens.length ? r.itens.map(linha).join('') : '<p class="subtitle">Nenhum pedido por aqui ainda.</p>';
    const n = Math.max(1, Math.ceil(r.total / r.tamanho));
    $('#pager').innerHTML = n > 1 ? `<button class="secondary-btn" ${p <= 1 ? 'disabled' : ''} onclick="carregar(${p - 1})">‹</button> ${p}/${n} <button class="secondary-btn" ${p >= n ? 'disabled' : ''} onclick="carregar(${p + 1})">›</button>` : '';
  } catch (e) { $('#lista').innerHTML = ''; Api.toast(e.message); }
}
function linha(p) {
  const pro = Api.perfil() === 'PROFISSIONAL', b = (t, fn, c = 'secondary-btn') => `<button class="${c}" onclick="${fn}">${t}</button>`;
  let a = '';
  if (!pro) { if (p.status === 'PENDENTE') a += b('Ver orçamentos', `orcamentos(${p.id})`); if (!['CONCLUIDO', 'CANCELADO'].includes(p.status)) a += b('Cancelar', `cancelar(${p.id})`); }
  else {
    if (p.status === 'PENDENTE' && p.minhaSituacao === 'PENDENTE') a += b('Aceitar', `aceitarPedido(${p.id})`, 'primary-btn') + b('Recusar', `recusarPedido(${p.id})`);
    if (p.status === 'PENDENTE' && p.minhaSituacao === 'ACEITO') a += b('Enviar orçamento', `enviarOrcamento(${p.id})`, 'primary-btn');
    if (p.status === 'AGENDADO') a += b('Iniciar serviço', `status(${p.id},'EM_ANDAMENTO')`);
    if (p.status === 'EM_ANDAMENTO') a += b('Concluir', `status(${p.id},'CONCLUIDO')`, 'primary-btn');
  }
  return `<div class="panel"><strong>#${p.id} ${Api.esc(p.titulo)}</strong> ${badge(p.status)} ${pro && p.minhaSituacao ? '· você: ' + badge(p.minhaSituacao) : ''}
    <p class="subtitle">${Api.esc(p.categoria)} · ${Api.esc(p.cidade)}${p.bairro ? ' / ' + Api.esc(p.bairro) : ''} · ${new Date(p.criadoEm).toLocaleDateString('pt-BR')}</p>
    <div class="inline-actions">${a}</div><div id="o${p.id}"></div></div>`;
}
const aceitarPedido = id => act(() => Api.post(`/pedidos/${id}/aceitar`), 'Pedido aceito. Agora envie o orçamento.');
const recusarPedido = id => { const m = prompt('Motivo (opcional):'); if (m !== null) act(() => Api.post(`/pedidos/${id}/recusar`, m.trim() ? { motivo: m.trim() } : {}), 'Pedido recusado.'); };
const status = (id, s) => act(() => Api.patch(`/pedidos/${id}/status`, { status: s }), 'Status atualizado.');
const cancelar = id => { const m = prompt('Motivo do cancelamento (5 a 200 caracteres):'); if (m) act(() => Api.post(`/pedidos/${id}/cancelar`, { motivo: m.trim() }), 'Pedido cancelado.'); };
function enviarOrcamento(id) {
  const v = parseFloat((prompt('Valor (R$):') || '').replace(',', '.')); if (!(v > 0)) return Api.toast('Informe um valor maior que zero.');
  const prazo = prompt('Prazo estimado (ex.: 2 dias):'); if (!prazo) return Api.toast('Informe o prazo estimado.');
  act(() => Api.post(`/pedidos/${id}/orcamentos`, { valor: v, prazoEstimado: prazo.trim(), condicoes: prompt('Condições (opcional):') || null }), 'Orçamento enviado.');
}
async function orcamentos(id) {
  const box = $('#o' + id); box.innerHTML = 'Carregando…';
  try {
    const l = await Api.get(`/pedidos/${id}/orcamentos`);
    box.innerHTML = l.length ? `<div class="table-wrap"><table><thead><tr><th>Profissional</th><th>Valor</th><th>Prazo</th><th>Nota</th><th></th></tr></thead><tbody>${l.map(o => `<tr><td>${Api.esc(o.profissional.nome)} ${o.profissional.verificado ? '✔' : ''}</td><td>${Api.money(o.valor)}</td><td>${Api.esc(o.prazoEstimado)}</td><td>★ ${Number(o.profissional.notaMedia).toFixed(1)}</td>
      <td>${o.situacao === 'ENVIADO' && !o.expirado ? `<button class="primary-btn" onclick="act(()=>Api.post('/orcamentos/${o.id}/aceitar'),'Orçamento aceito.')">Aceitar</button> <button class="secondary-btn" onclick="act(()=>Api.post('/orcamentos/${o.id}/recusar'),'Orçamento recusado.')">Recusar</button>` : badge(o.expirado ? 'EXPIRADO' : o.situacao)}</td></tr>`).join('')}</tbody></table></div>` : '<p class="subtitle">Nenhum orçamento recebido ainda.</p>';
  } catch (e) { box.innerHTML = ''; Api.toast(e.message); }
}
document.addEventListener('DOMContentLoaded', () => {
  if (!Api.guard(['CLIENTE', 'PROFISSIONAL'])) return;
  $('#userLabel').textContent = `${Api.user().nome} · ${Api.perfil()}`;
  $('#logoutBtn').onclick = Api.logout; $('#filtro').onchange = () => carregar(1);
  Api.get('/auth/me').catch(() => {}); // revalida a sessão (401 → volta ao login)
  carregar(1);
});
