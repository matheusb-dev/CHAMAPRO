const TOKEN_KEY = 'chamapro_token';
const USER_KEY = 'chamapro_user';

function homeFor(perfil) {
  return ({ ADMIN: '/admin.html', CLIENTE: '/painel.html', PROFISSIONAL: '/painel.html' })[String(perfil).toUpperCase()] || '/login.html';
}

function showAlert(message, type = 'error') {
  const alert = document.getElementById('alert');
  if (!alert) return;
  alert.textContent = message;
  alert.className = `alert visible ${type}`;
}

function clearAlert() {
  const alert = document.getElementById('alert');
  if (!alert) return;
  alert.className = 'alert';
  alert.textContent = '';
}

function getToken() {
  return localStorage.getItem(TOKEN_KEY) || '';
}

function saveSession(token, user) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  window.location.href = '/login.html';
}

async function apiFetch(url, options = {}) {
  const token = getToken();
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers
  });

  const contentType = response.headers.get('content-type') || '';
  const data = contentType.includes('application/json') ? await response.json() : null;

  if (!response.ok) {
    const message = data?.detalhe || data?.message || 'Erro ao processar a requisição.';
    throw new Error(message);
  }

  return data;
}

async function loginSubmit(event) {
  event.preventDefault();
  clearAlert();

  const payload = {
    email: document.getElementById('email').value,
    senha: document.getElementById('senha').value
  };

  try {
    const result = await apiFetch('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    saveSession(result.token, result.usuario);
    showAlert('Login realizado com sucesso!', 'success');

    setTimeout(() => window.location.href = homeFor(result.usuario.perfil), 600);
  } catch (error) {
    showAlert(error.message || 'Erro ao entrar.', 'error');
  }
}

async function cadastroSubmit(event) {
  event.preventDefault();
  clearAlert();

  const tipo = document.getElementById('tipo').value;
  const payload = {
    nome: document.getElementById('nome').value,
    email: document.getElementById('email').value,
    senha: document.getElementById('senha').value
  };

  try {
    const endpoint = tipo === 'profissional'
      ? '/api/v1/auth/cadastro/profissional'
      : '/api/v1/auth/cadastro/cliente';

    const result = await apiFetch(endpoint, {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    saveSession(result.token, result.usuario);
    showAlert('Cadastro realizado com sucesso!', 'success');
    setTimeout(() => window.location.href = homeFor(result.usuario.perfil), 700);
  } catch (error) {
    showAlert(error.message || 'Erro ao cadastrar.', 'error');
  }
}

function applyUserLabel() {
  const user = JSON.parse(localStorage.getItem(USER_KEY) || 'null');
  const label = document.getElementById('userLabel');
  if (label && user) {
    label.textContent = `${user.nome} · ${user.perfil}`;
  }
}

function requireAdmin() {
  const token = getToken();
  const user = JSON.parse(localStorage.getItem(USER_KEY) || 'null');

  if (!token || !user || user.perfil !== 'ADMIN') {
    window.location.href = '/login.html';
    return false;
  }

  return true;
}

async function loadAdminData() {
  if (!requireAdmin()) return;
  applyUserLabel();

  try {
    const usuarios = await apiFetch('/api/v1/admin/usuarios');
    const pedidos = await apiFetch('/api/v1/admin/pedidos');

    document.getElementById('usuariosTotal').textContent = String(usuarios.length || 0);
    document.getElementById('pedidosTotal').textContent = String(pedidos.length || 0);
    document.getElementById('verificacoesTotal').textContent = '0';

    const usuariosBody = document.getElementById('usuariosTableBody');
    usuariosBody.innerHTML = usuarios.length
      ? usuarios.map(u => `
        <tr>
          <td>${u.nome}</td>
          <td>${u.email}</td>
          <td><span class="badge ${u.perfil.toLowerCase()}">${u.perfil}</span></td>
          <td>${u.ativo ? 'Ativo' : 'Bloqueado'}</td>
        </tr>
      `).join('')
      : '<tr><td colspan="4">Nenhum usuário encontrado.</td></tr>';

    const pedidosBody = document.getElementById('pedidosTableBody');
    pedidosBody.innerHTML = pedidos.length
      ? pedidos.map(p => `
        <tr>
          <td>#${p.id}</td>
          <td>${p.cliente?.nome || 'Cliente'}</td>
          <td>${p.status || 'Sem status'}</td>
        </tr>
      `).join('')
      : '<tr><td colspan="3">Nenhum pedido encontrado.</td></tr>';
  } catch (error) {
    showAlert(error.message || 'Não foi possível carregar os dados do administrador.', 'error');
  }
}

function initLoginPage() {
  const form = document.getElementById('loginForm');
  if (form) {
    form.addEventListener('submit', loginSubmit);
  }
}

function initCadastroPage() {
  const form = document.getElementById('cadastroForm');
  if (form) {
    form.addEventListener('submit', cadastroSubmit);
  }
}

function initAdminPage() {
  const logoutBtn = document.getElementById('logoutBtn');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', logout);
  }
  loadAdminData();
}

document.addEventListener('DOMContentLoaded', () => {
  if (document.getElementById('loginForm')) initLoginPage();
  if (document.getElementById('cadastroForm')) initCadastroPage();
  if (document.getElementById('usuariosTableBody')) initAdminPage();
});
