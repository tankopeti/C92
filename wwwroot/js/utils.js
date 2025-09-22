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

window.c92.refreshToken = async function () {
    try {
        const response = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            credentials: 'include'
        });
        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('token', data.token);
            console.log('Token refreshed:', data.token);
            return data.token;
        } else {
            console.error('Token refresh failed:', response.status);
            localStorage.removeItem('token');
            return null;
        }
    } catch (error) {
        console.error('Token refresh error:', error);
        return null;
    }
};

// Universal email sending
window.c92.sendEmail = async function(entityType, id) {
    try {
        // Fetch entity to get partner email
        let toEmail = '';
        if (entityType === 'order') {
            const orderResponse = await fetch(`/api/orders/${id}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            if (orderResponse.ok) {
                const order = await orderResponse.json();
                toEmail = order.partner?.email || '';
            }
        } else if (entityType === 'quote') {
            const quoteResponse = await fetch(`/api/quotes/${id}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            if (quoteResponse.ok) {
                const quote = await quoteResponse.json();
                toEmail = quote.partner?.email || '';
            }
        }

        toEmail = prompt('Kérem adja meg a címzett e-mail címét:', toEmail);
        if (!toEmail || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(toEmail)) {
            window.c92.showToast('Érvénytelen e-mail cím', 'error');
            return;
        }

        window.c92.showToast('E-mail küldése...', 'info');
        const response = await fetch(`/api/email/send/${entityType}/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({ toEmail })
        });

        if (!response.ok) {
            let errorMessage = 'Nem sikerült az e-mail küldése';
            try {
                const errorData = await response.json();
                errorMessage = errorData.error || errorData.title || `Hiba: ${response.status} ${response.statusText}`;
            } catch {
                errorMessage = `Hiba: ${response.status} ${response.statusText}`;
            }
            throw new Error(errorMessage);
        }

        window.c92.showToast('E-mail sikeresen elküldve', 'success');
    } catch (error) {
        console.error(`Hiba az ${entityType} e-mail küldésekor:`, error, { entityType, id });
        window.c92.showToast(`Nem sikerült az e-mail küldése: ${error.message}`, 'error');
    }
};