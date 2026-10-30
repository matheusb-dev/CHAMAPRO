// MOCKS — usados só com USE_MOCKS=true / ?mock=1. Formatos copiados dos DTOs reais do backend.
window.ChamaproMocks = (() => {
  const cats = [{ id: 1, nome: 'Eletricista', icone: 'bolt' }, { id: 2, nome: 'Encanador', icone: 'wrench' }];
  const pros = [
    { id: 1, nome: 'Profissional Exemplo', fotoUrl: null, especialidades: ['Eletricista', 'Encanador'], notaMedia: 4.8, totalAvaliacoes: 12, verificado: true, faixaPrecoMin: 120, faixaPrecoMax: 350 },
    { id: 2, nome: 'Maria Souza', fotoUrl: null, especialidades: ['Eletricista'], notaMedia: 4.2, totalAvaliacoes: 3, verificado: false, faixaPrecoMin: 90, faixaPrecoMax: 200 }];
  const pedidos = [{ id: 1, titulo: 'Trocar chuveiro', categoria: 'Eletricista', status: 'PENDENTE', criadoEm: new Date().toISOString(), cidade: 'São Paulo', bairro: 'Centro', minhaSituacao: 'PENDENTE' }];
  const orcs = [{ id: 1, pedidoId: 1, valor: 180, prazoEstimado: '1 dia', condicoes: 'Material incluso', validoAte: new Date(Date.now() + 6048e5).toISOString(), expirado: false, situacao: 'ENVIADO', profissional: { id: 1, nome: 'Profissional Exemplo', notaMedia: 4.8, verificado: true } }];
  const pag = itens => ({ itens, pagina: 1, tamanho: 10, total: itens.length });
  const login = perfil => ({ token: 'mock', expiraEm: new Date(Date.now() + 72e5).toISOString(), usuario: { id: 1, nome: perfil + ' (mock)', email: 'mock@chamapro.com', perfil, ativo: true } });
  async function handle(m, path, { body, query }) {
    await new Promise(r => setTimeout(r, 250));
    const k = m + ' ' + path;
    if (k === 'GET /categorias') return cats;
    if (k === 'GET /profissionais') return pag(pros);
    if (k === 'POST /auth/login') return login(body.email.startsWith('admin') ? 'ADMIN' : body.email.startsWith('prof') ? 'PROFISSIONAL' : 'CLIENTE');
    if (k === 'GET /pedidos') return pag(pedidos);
    if (/^GET \/pedidos\/\d+\/orcamentos$/.test(k)) return orcs;
    if (m === 'POST' || m === 'PATCH') return null;
    throw new Api.ApiError(404, 'MOCK', 'Endpoint sem mock: ' + k);
  }
  return { handle };
})();
