(() => {
    window.ChamaproApp?.ready.then(user => {
        if (user?.perfil !== 'PROFISSIONAL') return;
        const root = document.getElementById('professionalEditor');
        if (root) window.ChamaproProfessionalEditor.mount(root, root.dataset.guided === 'true');
    });
})();
