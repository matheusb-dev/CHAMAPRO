const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const scope = { window: {}, document: { addEventListener() {} } };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../../wwwroot/js/api.js'), 'utf8'), scope);
exports.install = context => {
    context.window ||= {};
    context.window.Chamapro ||= {};
    for (const name of ['normalizarWhatsApp', 'contatoWhatsApp', 'inicioDoUsuario'])
        context.window.Chamapro[name] = scope.window.Chamapro[name];
};
exports.api = scope.window.Chamapro;
