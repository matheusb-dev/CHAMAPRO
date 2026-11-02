(() => {
    window.ChamaproApp?.ready.then(user => {
        if (user?.perfil !== 'PROFISSIONAL') return;
        const list = document.getElementById('professionalOrders');
        const detail = document.getElementById('orderDetail');
        if (list) window.ChamaproProfessionalWork.list(list);
        if (detail) window.ChamaproProfessionalWork.detail(detail);
    });
})();
