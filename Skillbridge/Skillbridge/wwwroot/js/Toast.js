const mostrarToast = function(titulo, mensagem, tipo = "info") {
    
    const toastEl = document.createElement('div');
    
    toastEl.className = `toast align-items-center text-bg-${tipo} border-0`;
    toastEl.setAttribute('role', 'alert');
    
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <strong>${titulo}:</strong> ${mensagem}
            </div>
            <button type="button"
                    class="btn-close btn-close-white me-2 m-auto"
                    data-bs-dismiss="toast">
            </button>
        </div>
    `;

    const container = document.getElementById('toast-container');
    if (!container) return;
    container.appendChild(toastEl);
    
    const toast = new bootstrap.Toast(toastEl, {
    delay: 4000
});

    toast.show();

    toastEl.addEventListener('hidden.bs.toast', () => {
    toastEl.remove();
});
}