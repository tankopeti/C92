window.c92 = window.c92 || {};

// Toast notification utility (used by quotes, orders, and other features)
window.c92.showToast = function(type, message) {
    if (typeof $ === 'undefined' || typeof bootstrap === 'undefined') {
        console.error('jQuery or Bootstrap not loaded for showToast');
        return;
    }
    console.log('Showing toast:', type, message);
    const toastId = `toast_${Date.now()}`;
    // Use Bootstrap classes with inline fallback
    const headerBgClass = type === 'success' ? 'bg-success' : type === 'warning' ? 'bg-warning text-dark' : 'bg-danger';
    const headerTextClass = type === 'warning' ? 'text-dark' : 'text-white';
    const headerStyle = `
        background-color: ${type === 'success' ? '#28a745' : type === 'warning' ? '#ffc107' : '#dc3545'};
        color: ${type === 'warning' ? 'CanvasText' : 'CanvasText'};
    `;
    const bodyStyle = `
        background-color: Canvas;
        color: CanvasText;
    `;
    const toastHtml = `
        <div id="${toastId}" class="toast custom-toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="5000" style="${bodyStyle}">
            <div class="toast-header ${headerBgClass} ${headerTextClass}" style="${headerStyle}">
                <strong class="me-auto">${type === 'success' ? 'Siker' : type === 'warning' ? 'Figyelmeztetés' : 'Hiba'}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">${message}</div>
        </div>
    `;
    // Remove existing toastContainer to avoid duplicates
    $('#toastContainer').remove();
    // Create new centered toastContainer
    console.log('Creating toastContainer with centered styles');
    $('body').append(`
        <div id="toastContainer" class="toast-container position-fixed" style="top: 50% !important; left: 50% !important; transform: translate(-50%, -50%) !important; z-index: 1055 !important;">
        </div>
    `);
    console.log('Appending toast HTML to #toastContainer');
    $('#toastContainer').append(toastHtml);
    const toastElement = $(`#${toastId}`);
    if (toastElement.length) {
        console.log('Toast element found, initializing:', toastId);
        try {
            const toast = new bootstrap.Toast(toastElement[0]);
            toast.show();
            toastElement.on('hidden.bs.toast', () => {
                console.log('Toast hidden, removing element:', toastId);
                toastElement.remove();
            });
        } catch (error) {
            console.error('Failed to initialize Bootstrap Toast:', error);
        }
    } else {
        console.error('Toast element not found:', `#${toastId}`);
    }
};