(() => {
    function path(value, origin) {
        try {
            const url = new URL(value, origin);
            return url.origin === origin && !url.search && !url.hash && /^\/api\/v1\/arquivos\/privados\/[a-zA-Z0-9_.-]+$/.test(url.pathname) ? url.pathname : null;
        } catch { return null; }
    }
    function create() {
        const urls = new Set();
        let generation = 0;
        function release(url) {
            if (url) { if (urls.delete(url)) URL.revokeObjectURL(url); return; }
            generation++;
            for (const value of urls) URL.revokeObjectURL(value);
            urls.clear();
        }
        async function load(value) {
            const route = path(value, location.origin);
            if (!route) throw { status: 422, message: 'O documento precisa usar a rota privada deste servidor.' };
            const token = window.Chamapro.obterToken();
            if (!token) throw { status: 401, message: 'Sua sessão expirou. Entre novamente para consultar o documento.' };
            const request = generation;
            const response = await fetch(route, { headers: { Authorization: 'Bearer ' + token }, cache: 'no-store', redirect: 'error' });
            if (!response.ok) throw { status: response.status, message: response.status === 401 ? 'Sua sessão expirou. Entre novamente.' : 'Não foi possível abrir o documento. Confira sua permissão e tente novamente.' };
            const blob = await response.blob();
            if (!['application/pdf','image/jpeg','image/png','image/webp'].includes(blob.type)) throw { status: 422, message: 'Formato de documento não permitido para visualização.' };
            if (request !== generation) throw { message: 'A consulta foi cancelada. Abra o documento novamente.' };
            const url = URL.createObjectURL(blob); urls.add(url); return url;
        }
        window.addEventListener('pagehide', () => release());
        return { load, release };
    }
    window.ChamaproPrivateDocuments = { path, create };
})();
