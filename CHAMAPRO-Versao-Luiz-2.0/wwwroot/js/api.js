// Cliente HTTP central + sessão + mocks. Mocks ficam isolados em js/mocks.js.
const Api = (() => {
  const cfg = window.CHAMAPRO_CONFIG || {};
  const qs = new URLSearchParams(location.search);
  if (qs.has('mock')) localStorage.setItem('chamapro_mock', qs.get('mock'));
  const useMocks = () => { const m = localStorage.getItem('chamapro_mock'); return m !== null ? (m === '1' || m === 'true') : !!cfg.USE_MOCKS; };
  const TK = 'chamapro_token', UK = 'chamapro_user';
  const user = () => { try { return JSON.parse(localStorage.getItem(UK)); } catch { return null; } };
  const perfil = () => (user()?.perfil || '').toUpperCase();   // /admin/usuarios devolve "Cliente"; normalizamos
  const home = p => ({ ADMIN: '/admin.html', CLIENTE: '/painel.html', PROFISSIONAL: '/painel.html' })[(p || perfil())] || '/login.html';
  const save = (t, u) => { localStorage.setItem(TK, t); localStorage.setItem(UK, JSON.stringify({ ...u, perfil: (u.perfil || '').toUpperCase() })); };
  const logout = () => { localStorage.removeItem(TK); localStorage.removeItem(UK); location.href = '/login.html'; };

  const MSG = { 401: 'Sessão expirada ou não autenticado. Entre novamente.', 403: 'Você não tem permissão para esta ação.', 404: 'Não encontrado.', 409: 'Conflito: o recurso já existe ou está em uso.', 422: 'Operação não permitida pelas regras do sistema.', 500: 'Erro interno do servidor. Tente novamente mais tarde.' };

  async function request(method, path, { body, query, form } = {}) {
    if (useMocks()) return window.ChamaproMocks.handle(method, path, { body, query });
    const url = new URL(cfg.API_URL + path, location.href);
    Object.entries(query || {}).forEach(([k, v]) => v !== '' && v != null && url.searchParams.set(k, v));
    const headers = {};
    const t = localStorage.getItem(TK);
    if (t) headers.Authorization = 'Bearer ' + t;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    let res;
    try { res = await fetch(url, { method, headers, body: form || (body !== undefined ? JSON.stringify(body) : undefined) }); }
    catch { throw new ApiError(0, 'REDE', 'Não foi possível conectar à API. Verifique a URL em js/config.js e se o backend está rodando.'); }
    const ct = res.headers.get('content-type') || '';
    const data = res.status !== 204 && ct.includes('json') ? await res.json().catch(() => null) : null;
    if (!res.ok) {
      if (res.status === 401 && t && !path.startsWith('/auth/login')) { localStorage.removeItem(TK); localStorage.removeItem(UK); setTimeout(() => location.href = '/login.html', 1200); }
      const campos = (data?.erros || []).map(e => `${e.campo}: ${e.mensagem}`).join(' · ');
      throw new ApiError(res.status, data?.codigo, (data?.detalhe || MSG[res.status] || 'Erro ao processar a requisição.') + (campos ? ' (' + campos + ')' : ''));
    }
    return data;
  }
  class ApiError extends Error { constructor(status, codigo, msg) { super(msg); this.status = status; this.codigo = codigo; } }

  function guard(perfis) {
    if (!localStorage.getItem(TK) || !perfis.includes(perfil())) { location.href = '/login.html'; return false; }
    return true;
  }
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const toast = (msg, type = 'error') => { const a = document.getElementById('alert'); if (!a) return; a.textContent = msg; a.className = 'alert visible ' + type; };
  const money = v => v == null ? '—' : Number(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

  return { get: (p, o) => request('GET', p, o), post: (p, b) => request('POST', p, { body: b }), put: (p, b) => request('PUT', p, { body: b }), patch: (p, b) => request('PATCH', p, { body: b }),
    save, logout, user, perfil, home, guard, esc, toast, money, useMocks, ApiError };
})();
