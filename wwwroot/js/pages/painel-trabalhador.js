(() => {
    window.ChamaproApp?.ready.then(user => {
        if (user?.perfil === 'PROFISSIONAL') window.ChamaproProfessionalWork.home(document.getElementById('professionalHome'));
    });
})();
