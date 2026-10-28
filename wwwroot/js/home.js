const $ = s => document.querySelector(s);
let pagina = 1;
async function init() {
  const u = Api.user();
  $('#navAuth').innerHTML = u ? `<a class="link-button" href="${Api.home()}">${Api.esc(u.nome)}</a> <button class="secondary-btn" id="sair">Sair</button>` : '<a class="link-button" href="/login.html">Entrar</a> <a class="link-button" href="/cadastro.html">Cadastrar</a>';
  $('#sair')?.addEventListener('click', Api.logout);
  try { const cats = await Api.get('/categorias'); $('#categoria').innerHTML = '<option value="">Categoria…</option>' + cats.map(c => `<option value="${c.id}">${Api.esc(c.nome)}</option>`).join(''); }
  catch (e) { Api.toast(e.message); }
  $('#busca').addEventListener('submit', e => { e.preventDefault(); buscar(1); });
}
async function buscar(p) {
  pagina = p;
  const f = new FormData($('#busca'));
  if (!f.get('categoriaId') || !f.get('cidade').trim()) return Api.toast('Escolha uma categoria e informe a cidade (a API exige os dois).');
  $('#resultado').innerHTML = '<p class="subtitle">Carregando…</p>';
  try {
    const r = await Api.get('/profissionais', { query: { categoriaId: f.get('categoriaId'), cidade: f.get('cidade').trim(), ordenar: f.get('ordenar'), verificado: f.get('verificado') ? 'true' : '', pagina: p, tamanho: 9 } });
    Api.toast('', ''); $('#alert').className = 'alert';
    $('#resultado').innerHTML = r.itens.length ? `<div class="cards">${r.itens.map(card).join('')}</div>` : '<p class="subtitle">Nenhum profissional encontrado para esse filtro.</p>';
    const paginas = Math.max(1, Math.ceil(r.total / r.tamanho));
    $('#pager').innerHTML = paginas > 1 ? `<button class="secondary-btn" ${p <= 1 ? 'disabled' : ''} onclick="buscar(${p - 1})">‹</button> Página ${p}/${paginas} <button class="secondary-btn" ${p >= paginas ? 'disabled' : ''} onclick="buscar(${p + 1})">›</button>` : '';
  } catch (e) { $('#resultado').innerHTML = ''; Api.toast(e.message); }
}
const card = p => `<article class="panel"><h3>${Api.esc(p.nome)} ${p.verificado ? '<span class="badge" title="Identidade verificada">✔ Verificado</span>' : ''}</h3>
  <p class="subtitle">${p.especialidades.map(Api.esc).join(' · ')}</p>
  <p>★ ${Number(p.notaMedia).toFixed(1)} (${p.totalAvaliacoes} avaliações)</p>
  <p>${Api.money(p.faixaPrecoMin)} – ${Api.money(p.faixaPrecoMax)}</p></article>`;
document.addEventListener('DOMContentLoaded', init);
