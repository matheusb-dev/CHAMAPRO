const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { api: C } = require('./helpers/api.cjs');

test('WhatsApp normalizes Brazilian local numbers, DDI and international prefixes', () => {
    assert.equal(C.normalizarWhatsApp('(11) 98765-4321'), '5511987654321');
    assert.equal(C.normalizarWhatsApp('+55 (11) 98765-4321'), '5511987654321');
    assert.equal(C.normalizarWhatsApp('0055 11 98765 4321'), '5511987654321');
    assert.equal(C.normalizarWhatsApp('5511987654321'), '5511987654321');
    assert.equal(C.normalizarWhatsApp('(11) 3456-7890'), '551134567890');
    assert.equal(C.normalizarWhatsApp('+351 912 345 678'), '351912345678');
});
test('missing or malformed WhatsApp numbers never produce a working link', () => {
    for (const number of [null, '', '123', '00000000000', '1191111111', '20987654321', 'phone=11987654321', '+55 11 12345-6789']) {
        assert.equal(C.normalizarWhatsApp(number), null, String(number));
        assert.doesNotMatch(C.contatoWhatsApp(number), /href=/);
    }
});
test('proposal contact uses safe wa.me link and preserves the message', () => {
    const html = C.contatoWhatsApp('11987654321', 'Olá! Preciso de um reparo & orçamento.');
    assert.match(html, /https:\/\/wa.me\/5511987654321\?text=Ol/);
    assert.match(html, /Entrar em contato/);
    assert.match(html, /noopener noreferrer/);
    assert.match(html, /%26/);
    assert.doesNotMatch(html, /token=|password=/);
});
test('logo destinations work for visitors and all authenticated roles', () => {
    assert.equal(C.inicioDoUsuario(null), '/');
    assert.equal(C.inicioDoUsuario({perfil:'CLIENTE'}), '/pages/cliente/painel.html');
    assert.equal(C.inicioDoUsuario({perfil:'PROFISSIONAL'}), '/pages/trabalhador/painel.html');
    assert.equal(C.inicioDoUsuario({perfil:'ADMIN'}), '/pages/admin/gestao.html');
});
test('initial visual rating is five and real ratings keep their true average', () => {
    for (const number of [0, null, undefined]) assert.equal(C.formatarNota(number), '5,0');
    assert.equal(C.formatarNota(4.25), '4,3');
    assert.equal(C.formatarNota(1), '1,0');
});
test('urgent order pages expose no date inputs or future scheduling controls', () => {
    for (const page of ['criar-chamado', 'orcamentos']) {
        const html = fs.readFileSync(`wwwroot/pages/cliente/${page}.html`, 'utf8');
        assert.doesNotMatch(html, /datetime-local|id="scheduleForm"|value="AGENDADO">Agendar/);
    }
});
test('API transport failure shows a Portuguese retry message instead of native browser text', async () => {
    const scope = { window: {}, document: { addEventListener() {} }, FormData, sessionStorage: { getItem: () => null }, fetch: async () => { throw new TypeError('Failed to fetch'); } };
    vm.runInNewContext(fs.readFileSync('wwwroot/js/api.js', 'utf8'), scope);
    await assert.rejects(scope.window.Chamapro.api('/profissionais/me', {method:'PUT',body:{descricao:'Rascunho'}}), error => {
        assert.equal(error.codigo, 'FALHA_REDE');
        assert.equal(error.status, 0);
        assert.match(error.message, /Não foi possível conectar/);
        assert.doesNotMatch(error.message, /Failed to fetch/);
        return true;
    });
});
