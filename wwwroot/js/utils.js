console.log('utils.js loaded, defining window.c92.addItemRow and window.c92.showToast');

window.c92 = window.c92 || {};
window.c92.showToast = function (type, message) {
    console.log(`Showing toast: ${type} ${message}`);
    const toastContainer = document.getElementById('toastContainer') || (() => {
        console.log('Creating toastContainer with centered styles');
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.style.position = 'fixed';
        container.style.top = '50%';
        container.style.left = '50%';
        container.style.transform = 'translate(-50%, -50%)';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    })();

    const toastId = `toast_${Date.now()}`;
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'warning'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    console.log('Appending toast HTML to #toastContainer');
    toastContainer.insertAdjacentHTML('beforeend', toastHTML);

    const toastElement = document.getElementById(toastId);
    if (toastElement) {
        console.log(`Toast element found, initializing: ${toastId}`);
        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();
        toastElement.addEventListener('hidden.bs.toast', () => {
            console.log(`Toast hidden, removing element: ${toastId}`);
            toastElement.remove();
        });
    } else {
        console.error('Toast element not found after appending:', toastId);
    }
};