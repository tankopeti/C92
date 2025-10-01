(function () {
    // Centralized API endpoints
    const API_ENDPOINTS = {
        partners: '/api/partners/select',
        users: '/api/users',
        orders: '/api/orders',
        communication: '/api/customercommunication',
        communicationTypes: '/api/customercommunication/types',
        communicationStatuses: '/api/customercommunication/statuses'
    };

    // Utility Functions
    function showToast(message, type = 'danger', options = {}) {
        const { retryCallback } = options;
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                    ${retryCallback ? '<button type="button" class="btn btn-link text-white p-0 ms-2 retry-btn">Újra</button>' : ''}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            console.error('Toast container not found');
            return;
        }
        toastContainer.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast, { autohide: true, delay: 5000 });
        if (retryCallback) {
            toast.querySelector('.retry-btn')?.addEventListener('click', () => {
                retryCallback();
                bsToast.hide();
            });
        }
        bsToast.show();
    }

    // Initialize TomSelect
    function initializeTomSelect(select, communicationId, options = {}) {
        const { endpoint = API_ENDPOINTS.partners, dataKey = 'items', placeholder = '-- Válasszon --' } = options;
        const selectedId = select?.dataset.selectedId || '';
        const selectedText = select?.dataset.selectedText || '';
        console.log('TomSelect Init for:', communicationId, 'endpoint:', endpoint, 'dataKey:', dataKey, 'selectedId:', selectedId);
        if (!select) {
            console.error('Select element is null for communicationId:', communicationId);
            showToast(`Hiba: A ${dataKey} kiválasztó elem nem található.`, 'danger');
            return null;
        }
        if (select.tomselect) {
            select.tomselect.destroy();
        }
        return new TomSelect(select, {
            valueField: 'id',
            labelField: 'text',
            searchField: 'text',
            placeholder: placeholder,
            allowEmptyOption: true,
            maxOptions: 50,
            load: async (query, callback) => {
                try {
                    const url = `${endpoint}${query ? `?search=${encodeURIComponent(query)}` : ''}`;
                    console.log('TomSelect Query:', url);
                    const response = await fetch(url, {
                        headers: {
                            'Authorization': `Bearer ${localStorage.getItem('token') || ''}`,
                            'Accept': 'application/json'
                        }
                    });
                    if (!response.ok) {
                        if (response.status === 401) {
                            throw new Error('Nincs jogosultság. Kérjük, jelentkezzen be újra.');
                        }
                        throw new Error(`HTTP hiba: ${response.status}`);
                    }
                    const data = await response.json();
                    console.log('TomSelect Response:', data);
                    let items = [];
                    if (endpoint === API_ENDPOINTS.partners) {
                        if (dataKey === 'partners') {
                            items = data.map(partner => ({ id: partner.id, text: partner.text }));
                        }
                        //      else if (dataKey === 'contacts') {
                        //         items = data.flatMap(partner =>
                        //             partner.contacts?.length ? partner.contacts.map(contact => ({ id: contact.id, text: contact.text })) : []
                        //         );
                        //     } else if (dataKey === 'quotes') {
                        //         items = data.flatMap(partner =>
                        //             partner.quotes?.length ? partner.quotes.map(quote => ({ id: quote.id, text: quote.text })) : []
                        //         );
                        //     }
                        // } else if (endpoint === API_ENDPOINTS.orders) {
                        //     items = data.map(order => ({
                        //         id: order.orderId,
                        //         text: order.orderNumber || `Order ${order.orderId}` + (order.description ? ` (${order.description})` : '')
                        //     }));
                    } else if (endpoint === API_ENDPOINTS.users) {
                        items = data.map(user => ({
                            id: user.id,
                            text: (user.userName + ' (' + user.email + ')')
                        }));
                    } else if (endpoint === API_ENDPOINTS.communicationTypes || endpoint === API_ENDPOINTS.communicationStatuses) {
                        items = data.map(item => ({ id: item.id, text: item.text }));
                    }
                    if (selectedId && selectedText && !items.find(item => item.id == selectedId)) {
                        items.unshift({ id: selectedId, text: selectedText });
                    }
                    if (!items.length) {
                        console.warn(`No items found for ${dataKey} from ${endpoint}`);
                        showToast(`Figyelmeztetés: Nem található ${dataKey} adat a szerveren.`, 'warning');
                        callback([]);
                    } else {
                        callback(items);
                    }
                } catch (error) {
                    console.error('TomSelect Error:', error, 'endpoint:', endpoint, 'dataKey:', dataKey);
                    showToast(`Hiba: Nem sikerült betölteni a ${dataKey} adatokat.`, 'danger', {
                        retryCallback: () => initializeTomSelect(select, communicationId, options)
                    });
                    callback([]);
                }
            },
            shouldLoad: () => true,
            render: {
                option: (data, escape) => `<div>${escape(data.text)}</div>`,
                item: (data, escape) => `<div>${escape(data.text)}</div>`,
                no_results: (data, escape) => `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`
            },
            onInitialize: function () {
                console.log('TomSelect Initialized for select:', select.name);
                if (selectedId && selectedText) {
                    this.addOption({ id: selectedId, text: selectedText });
                    this.setValue(selectedId);
                }
                this.load('');
            }
        });
    }

    // Load Communication History
    async function loadCommunicationHistory(communicationId) {
        const historyContainer = document.getElementById(`history_${communicationId}`);
        if (!historyContainer) {
            console.error('History container not found:', communicationId);
            showToast('Hiba: Az előzmény konténer nem található.', 'danger');
            return;
        }
        historyContainer.innerHTML = '<p class="text-muted">Előzmények betöltése...</p>';
        try {
            const response = await fetch(`${API_ENDPOINTS.communication}/${communicationId}/history`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });
            if (!response.ok) {
                if (response.status === 404) {
                    historyContainer.innerHTML = '<p class="text-muted">Nincs előzmény.</p>';
                    return;
                }
                const errorText = await response.text();
                let errorMessage = `HTTP hiba: ${response.status}`;
                try {
                    errorMessage = JSON.parse(errorText).error || errorText;
                } catch {
                    errorMessage = errorText;
                }
                throw new Error(errorMessage);
            }
            const data = await response.json();
            console.log('History loaded:', data);
            if (!data || (!data.posts?.length && !data.responsibleHistory?.length)) {
                historyContainer.innerHTML = '<p class="text-muted">Nincs előzmény.</p>';
                return;
            }
            let html = '<ul class="list-group">';
            if (data.posts?.length) {
                data.posts.forEach(post => {
                    html += `
                        <li class="list-group-item">
                            <strong>Hozzászólás</strong> (${post.createdAt ? new Date(post.createdAt).toLocaleString('hu-HU') : 'N/A'}): ${post.content || ''}<br>
                            <small>Írta: ${post.createdByName || 'Ismeretlen'}</small>
                        </li>`;
                });
            }
            if (data.responsibleHistory?.length) {
                data.responsibleHistory.forEach(responsible => {
                    html += `
                        <li class="list-group-item">
                            <strong>Felelős kijelölve</strong> (${responsible.assignedAt ? new Date(responsible.assignedAt).toLocaleString('hu-HU') : 'N/A'}): ${responsible.responsibleName || 'Ismeretlen'}<br>
                            <small>Kijelölte: ${responsible.assignedByName || 'Ismeretlen'}</small>
                        </li>`;
                });
            }
            html += '</ul>';
            historyContainer.innerHTML = html;
        } catch (error) {
            console.error('History load error:', error);
            historyContainer.innerHTML = '<p class="text-danger">Hiba az előzmények betöltésekor.</p>';
            showToast(`Hiba: ${error.message}`, 'danger', {
                retryCallback: () => loadCommunicationHistory(communicationId)
            });
        }
    }

    // Save Communication
    async function saveCommunication(communicationId) {
        const form = document.getElementById(`communicationForm_${communicationId}`);
        if (!form) {
            console.error('Form not found for communicationId:', communicationId);
            showToast('Hiba: Az űrlap nem található.', 'danger');
            return;
        }
        if (!form.checkValidity()) {
            form.reportValidity();
            showToast('Kérjük, töltse ki az összes kötelező mezőt helyesen.', 'danger');
            return;
        }
        const partnerSelect = form.querySelector('select[name="PartnerId"]');
        // const contactSelect = form.querySelector('select[name="ContactId"]');
        const responsibleSelect = form.querySelector('select[name="ResponsibleContactId"]');
        const typeSelect = form.querySelector('select[name="CommunicationTypeId"]');
        const statusSelect = form.querySelector('select[name="StatusId"]');
        // const quoteSelect = form.querySelector('select[name="QuoteId"]');
        // const orderSelect = form.querySelector('select[name="OrderId"]');
        if (!partnerSelect || !partnerSelect.value) {
            console.error('Partner selection is empty or not initialized');
            showToast('Kérjük, válasszon ki egy partnert.', 'danger');
            return;
        }
        // if (!contactSelect || !contactSelect.value) {
        //     console.error('Contact selection is empty or not initialized');
        //     showToast('Kérjük, válasszon ki egy kapcsolattartót.', 'danger');
        //     return;
        // }
        if (!responsibleSelect || !responsibleSelect.value) {
            console.error('Responsible selection is empty or not initialized');
            showToast('Kérjük, válasszon ki egy felelőst.', 'danger');
            return;
        }
        if (!typeSelect || !typeSelect.value) {
            console.error('Invalid or missing CommunicationTypeId:', typeSelect?.value);
            showToast('Kérjük, válasszon ki egy érvényes kommunikációs típust.', 'danger');
            return;
        }
        if (!statusSelect || !statusSelect.value) {
            console.error('Invalid or missing StatusId:', statusSelect?.value);
            showToast('Kérjük, válasszon ki egy érvényes státuszt.', 'danger');
            return;
        }
        const formData = new FormData(form);
        const partnerText = partnerSelect.selectedOptions[0]?.text || 'Unknown';
        const responsibleText = responsibleSelect.selectedOptions[0]?.text || 'Unknown';
        const username = localStorage.getItem('username') || 'System';
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Anti-forgery token not found');
            showToast('Hiba: Biztonsági token hiányzik.', 'danger');
            return;
        }
        const data = {
            CustomerCommunicationId: communicationId === 'new' ? 0 : parseInt(communicationId),
            CommunicationTypeId: parseInt(formData.get('CommunicationTypeId')) || null,
            // ContactId: parseInt(formData.get('ContactId')) || null,
            Subject: formData.get('Subject')?.trim() || null,
            Date: formData.get('Date') ? new Date(formData.get('Date')).toISOString() : null,
            Note: formData.get('Note')?.trim() || null,
            StatusId: parseInt(formData.get('StatusId')) || null,
            PartnerId: parseInt(formData.get('PartnerId')) || null,
            // LeadId: parseInt(formData.get('LeadId')) || null,
            // QuoteId: parseInt(formData.get('QuoteId')) || null,
            // OrderId: parseInt(formData.get('OrderId')) || null,
            Metadata: formData.get('Metadata')?.trim() || null,
            Posts: formData.get('InitialPost')?.trim() ? [{
                Content: formData.get('InitialPost').trim(),
                CreatedByName: username,
                CreatedAt: new Date().toISOString()
            }] : [],
            CurrentResponsible: {
                ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                ResponsibleName: responsibleText.includes('(') ? responsibleText.split('(')[0].trim() : responsibleText || 'Unknown',
                AssignedByName: username,
                AssignedAt: new Date().toISOString()
            },
            ResponsibleHistory: [{
                ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                ResponsibleName: responsibleText.includes('(') ? responsibleText.split('(')[0].trim() : responsibleText || 'Unknown',
                AssignedByName: username,
                AssignedAt: new Date().toISOString()
            }]
        };
        if (isNaN(data.CommunicationTypeId) || data.CommunicationTypeId <= 0 ||
            isNaN(data.StatusId) || data.StatusId <= 0 ||
            !data.Subject || !data.Date || !data.PartnerId || !data.CurrentResponsible.ResponsibleId) {
            console.error('Validation failed:', data);
            showToast('Kérjük, töltse ki az összes kötelező mezőt.', 'danger');
            return;
        }
        try {
            const url = communicationId === 'new' ? API_ENDPOINTS.communication : `${API_ENDPOINTS.communication}/${communicationId}`;
            const method = communicationId === 'new' ? 'POST' : 'PUT';
            const response = await fetch(url, {
                method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                },
                body: JSON.stringify(data)
            });
            if (!response.ok) {
                const errorText = await response.text();
                let errorMessage = `Hiba: ${response.status} ${response.statusText}`;
                try {
                    const errorJson = JSON.parse(errorText);
                    errorMessage = errorJson.errors
                        ? `Érvényesítési hibák: ${Object.entries(errorJson.errors).map(([field, messages]) => `${field}: ${messages.join(', ')}`).join('; ')}`
                        : errorJson.title || errorText;
                    if (errorMessage.includes('Invalid CommunicationTypeId') || errorMessage.includes('Invalid StatusId')) {
                        errorMessage = 'Érvénytelen típus vagy státusz. A rendszer konfigurációja hibás lehet.';
                    }
                    if (response.status === 404) {
                        errorMessage = 'A kommunikáció mentési végpont nem található.';
                    }
                } catch {
                    errorMessage += ` Szerver hiba: ${errorText}`;
                }
                throw new Error(errorMessage);
            }
            console.log('Save communication success:', response.status);
            showToast('Kommunikáció sikeresen mentve!', 'success');
            window.location.reload();
        } catch (error) {
            console.error('Save communication error:', error);
            showToast(`Kommunikáció mentése sikertelen: ${error.message}`, 'danger');
        }
    }

    // Add Post
    async function addPost(communicationId) {
        const form = document.getElementById(`addPostForm_${communicationId}`);
        if (!form) {
            console.error('Post form not found for communicationId:', communicationId);
            showToast('Hiba: A hozzászólás űrlap nem található.', 'danger');
            return;
        }
        const formData = new FormData(form);
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Anti-forgery token not found');
            showToast('Hiba: Biztonsági token hiányzik.', 'danger');
            return;
        }
        const data = { Content: formData.get('Content')?.trim() || '' };
        if (!data.Content) {
            showToast('Kérjük, adja meg a hozzászólás tartalmát.', 'danger');
            return;
        }
        try {
            const response = await fetch(`${API_ENDPOINTS.communication}/${communicationId}/post`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                },
                body: JSON.stringify(data)
            });
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || `Hiba: ${response.status}`);
            }
            console.log('Post added successfully');
            showToast('Hozzászólás sikeresen hozzáadva!', 'success');
            form.reset();
            await loadCommunicationHistory(communicationId);
        } catch (error) {
            console.error('Post add error:', error);
            showToast(`Hozzászólás hozzáadása sikertelen: ${error.message}`, 'danger');
        }
    }

// Assign Responsible
async function assignResponsible(communicationId) {
    const form = document.getElementById(`assignResponsibleForm_${communicationId}`);
    if (!form) {
        console.error('Responsible form not found for communicationId:', communicationId);
        showToast('Hiba: A felelős kijelölés űrlap nem található.', 'danger');
        return;
    }
    const select = form.querySelector('select[name="ResponsibleId"]');
    const responsibleId = select ? select.value.trim() : '';
    if (!responsibleId) {
        showToast('Kérjük, válasszon ki egy érvényes felelőst.', 'danger');
        return;
    }
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.error('Anti-forgery token not found');
        showToast('Hiba: Biztonsági token hiányzik.', 'danger');
        return;
    }
    const data = {
        ResponsibleUserId: responsibleId // Send as string (GUID)
    };
    try {
        const response = await fetch(`${API_ENDPOINTS.communication}/${communicationId}/assign-responsible`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const errorText = await response.text();
            console.error('AssignResponsible full response:', errorText);
            let errorMessage = `Hiba: ${response.status}`;
            try {
                const errorJson = JSON.parse(errorText);
                if (errorJson.errors) {
                    // Join validation errors into a single message
                    const validationErrors = Object.values(errorJson.errors).flat().join('; ');
                    errorMessage = `Érvényesítési hiba: ${validationErrors}`;
                } else if (errorJson.error || errorJson.title) {
                    errorMessage = errorJson.error || errorJson.title;
                } else {
                    errorMessage = errorText;
                }
            } catch (parseErr) {
                console.error('Failed to parse error JSON:', parseErr);
                errorMessage = errorText.substring(0, 200) + '...';
            }
            if (response.status === 401) {
                errorMessage = 'Nincs jogosultság. Kérjük, jelentkezzen be újra.';
                // Optionally redirect: window.location.href = '/login';
            }
            throw new Error(errorMessage);
        }
        const result = await response.json();
        console.log('Responsible assigned successfully:', result);
        showToast(result.message || 'Felelős sikeresen kijelölve!', 'success');
        form.reset();
        await loadCommunicationHistory(communicationId);
        // Optionally reload only the responsible display instead of full page
        // window.location.reload();
    } catch (error) {
        console.error('Responsible assignment error:', error);
        showToast(`Felelős kijelölése sikertelen: ${error.message}`, 'danger');
    }
}

    // Delete Communication
    async function deleteCommunication(communicationId) {
        const form = document.getElementById(`deleteCommunicationForm_${communicationId}`);
        if (!form) {
            console.error('Form not found for communicationId:', communicationId);
            showToast('Hiba: Törlési űrlap nem található.', 'danger');
            return;
        }
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Anti-forgery token not found for communicationId:', communicationId);
            showToast('Hiba: Biztonsági token hiányzik.', 'danger');
            return;
        }
        const button = document.querySelector(`.confirm-delete-communication[data-communication-id="${communicationId}"]`);
        if (button) button.disabled = true;
        try {
            const response = await fetch(`${API_ENDPOINTS.communication}/${communicationId}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': token,
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });
            if (!response.ok) {
                const errorText = await response.text();
                let message = `Hiba: ${response.status}`;
                try {
                    const errorJson = JSON.parse(errorText);
                    message = errorJson.message || errorJson.error || errorText || message;
                    if (message.includes('FK_CommunicationPosts_CustomerCommunication')) {
                        message = 'Nem lehet törölni a kommunikációt, mert kapcsolódó bejegyzések léteznek.';
                    }
                } catch {
                    message = errorText || message;
                }
                throw new Error(message);
            }
            console.log('Delete response:', response.status);
            showToast(`Kommunikáció azonosító: ${communicationId} sikeresen törölve!`, 'success');
            const modal = document.getElementById(`deleteCommunicationModal_${communicationId}`);
            if (modal) bootstrap.Modal.getInstance(modal)?.hide();
            const row = document.querySelector(`tr[data-communication-id="${communicationId}"]`);
            if (row) row.remove();
        } catch (error) {
            console.error('Delete error for communicationId:', communicationId, 'error:', error.message);
            showToast(`Hiba történt a törlés során: ${error.message}`, 'danger');
        } finally {
            if (button) button.disabled = false;
        }
    }

    // Copy Communication
    async function copyCommunication(communicationId) {
        const dropdownItem = document.querySelector(`a[onclick="copyCommunication(${communicationId})"]`);
        if (dropdownItem) {
            dropdownItem.classList.add('disabled');
            dropdownItem.style.pointerEvents = 'none';
        }
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Anti-forgery token not found');
            showToast('Hiba: Biztonsági token hiányzik.', 'danger');
            if (dropdownItem) {
                dropdownItem.classList.remove('disabled');
                dropdownItem.style.pointerEvents = 'auto';
            }
            return;
        }
        try {
            const response = await fetch(`${API_ENDPOINTS.communication}/${communicationId}/copy`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });
            if (!response.ok) {
                const errorText = await response.text();
                let errorMessage = `Hiba: ${response.status}`;
                try {
                    const errorJson = JSON.parse(errorText);
                    errorMessage = errorJson.error || errorText;
                } catch {
                    errorMessage = errorText || errorMessage;
                }
                throw new Error(errorMessage);
            }
            const data = await response.json();
            console.log('Communication copied successfully:', data);
            showToast(`Kommunikáció azonosító: ${data.communicationId} sikeresen másolva!`, 'success');
            window.location.reload();
        } catch (error) {
            console.error('Copy communication failed:', error);
            showToast(`Nem sikerült másolni a kommunikációt: ${error.message}`, 'danger');
        } finally {
            if (dropdownItem) {
                dropdownItem.classList.remove('disabled');
                dropdownItem.style.pointerEvents = 'auto';
            }
        }
    }

    // Initialize Event Listeners
    document.addEventListener('DOMContentLoaded', () => {
        console.log('Main script loaded');
const responsibleSelect = document.querySelector('select[name="ResponsibleId"]');
    if (responsibleSelect) {
        new TomSelect(responsibleSelect, {
            valueField: 'id',
            labelField: 'text',
            searchField: 'text',
            load: function (query, callback) {
                fetch('/api/users?searchTerm=' + encodeURIComponent(query))
                    .then(response => response.json())
                    .then(data => {
                        callback(data.map(user => ({
                            id: user.id,
                            text: user.userName || user.email || 'Unknown'
                        })));
                    })
                    .catch(() => callback());
            },
            placeholder: '-- Válasszon felelőst --'
        });
    }
        // New Communication Modal
        const newCommunicationModal = document.getElementById('newCommunicationModal');
        if (newCommunicationModal) {
            newCommunicationModal.addEventListener('shown.bs.modal', () => {
                console.log('New Communication modal shown');
                const selects = [
                    {
                        selector: '#communicationForm_new select[name="PartnerId"]',
                        options: { endpoint: API_ENDPOINTS.partners, dataKey: 'partners', placeholder: '-- Válasszon partnert --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="ResponsibleContactId"]',
                        options: { endpoint: API_ENDPOINTS.users, dataKey: 'users', placeholder: '-- Válasszon felelőst --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="CommunicationTypeId"]',
                        options: { endpoint: API_ENDPOINTS.communicationTypes, dataKey: 'communicationTypes', placeholder: '-- Válasszon típust --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="StatusId"]',
                        options: { endpoint: API_ENDPOINTS.communicationStatuses, dataKey: 'communicationStatuses', placeholder: '-- Válasszon státuszt --' }
                    }
                ];
                selects.forEach(({ selector, options }) => {
                    const select = document.querySelector(selector);
                    if (select) {
                        initializeTomSelect(select, 'new', options);
                    } else {
                        console.error(`${selector} not found`);
                        showToast(`Hiba: ${selector} nem található.`, 'danger');
                    }
                });
            });
            newCommunicationModal.addEventListener('hidden.bs.modal', () => {
                console.log('New Communication modal hidden');
                const form = document.querySelector('#communicationForm_new');
                if (form) form.reset();
                document.querySelectorAll('#communicationForm_new .tom-select').forEach(select => {
                    if (select.tomselect) select.tomselect.destroy();
                });
            });
        }

        // Save Communication
        async function saveCommunication(communicationId) {
            const form = document.getElementById(`communicationForm_${communicationId}`);
            if (!form) {
                console.error('Form not found for communicationId:', communicationId);
                showToast('Hiba: Az űrlap nem található.', 'danger');
                return;
            }
            // Enforce browser validation
            if (!form.checkValidity()) {
                form.reportValidity();
                showToast('Kérjük, töltse ki az összes kötelező mezőt helyesen.', 'danger');
                return;
            }
            const partnerSelect = form.querySelector('select[name="PartnerId"]');
            const responsibleSelect = form.querySelector('select[name="ResponsibleContactId"]');
            const typeSelect = form.querySelector('select[name="CommunicationTypeId"]');
            const statusSelect = form.querySelector('select[name="StatusId"]');
            if (!partnerSelect || !partnerSelect.value) {
                console.error('Partner selection is empty or not initialized');
                showToast('Kérjük, válasszon ki egy partnert.', 'danger');
                return;
            }
            if (!responsibleSelect || !responsibleSelect.value) {
                console.error('Responsible selection is empty or not initialized');
                showToast('Kérjük, válasszon ki egy felelőst.', 'danger');
                return;
            }
            if (!typeSelect || !typeSelect.value) {
                console.error('Invalid or missing CommunicationTypeId:', typeSelect?.value);
                showToast('Kérjük, válasszon ki egy érvényes kommunikációs típust.', 'danger');
                return;
            }
            if (!statusSelect || !statusSelect.value) {
                console.error('Invalid or missing StatusId:', statusSelect?.value);
                showToast('Kérjük, válasszon ki egy érvényes státuszt.', 'danger');
                return;
            }
            const formData = new FormData(form);
            const partnerText = partnerSelect.selectedOptions[0]?.text || 'Unknown';
            const responsibleText = responsibleSelect.selectedOptions[0]?.text || 'Unknown';
            const username = localStorage.getItem('username') || 'System';
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                console.error('Anti-forgery token not found');
                showToast('Hiba: Biztonsági token hiányzik.', 'danger');
                return;
            }
            const communicationTypeId = parseInt(formData.get('CommunicationTypeId'));
            const statusId = parseInt(formData.get('StatusId'));
            const data = {
                CustomerCommunicationId: communicationId === 'new' ? 0 : parseInt(communicationId),
                CommunicationTypeId: communicationTypeId,
                Subject: formData.get('Subject')?.trim() || null,
                Date: formData.get('Date') ? new Date(formData.get('Date')).toISOString() : null,
                Note: formData.get('Note')?.trim() || null,
                StatusId: statusId,
                PartnerId: parseInt(formData.get('PartnerId')) || null,
                LeadId: null,
                QuoteId: null,
                OrderId: null,
                Metadata: formData.get('Metadata')?.trim() || null,
                Posts: formData.get('InitialPost')?.trim() ? [{
                    Content: formData.get('InitialPost').trim(),
                    CreatedByName: username,
                    CreatedAt: new Date().toISOString()
                }] : [],
                CurrentResponsible: {
                    ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                    ResponsibleName: responsibleText.includes('(') ? responsibleText.split('(')[0].trim() : responsibleText || 'Unknown',
                    AssignedByName: username,
                    AssignedAt: new Date().toISOString()
                },
                ResponsibleHistory: [{
                    ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                    ResponsibleName: responsibleText.includes('(') ? responsibleText.split('(')[0].trim() : responsibleText || 'Unknown',
                    AssignedByName: username,
                    AssignedAt: new Date().toISOString()
                }]
            };
            console.log('Form data before submission:', JSON.stringify(data, null, 2));
            if (isNaN(data.CommunicationTypeId) || data.CommunicationTypeId <= 0) {
                console.error('Validation failed: Invalid CommunicationTypeId:', data.CommunicationTypeId);
                showToast('Érvénytelen kommunikációs típus. Kérjük, válasszon egy érvényes típust.', 'danger');
                return;
            }
            if (isNaN(data.StatusId) || data.StatusId <= 0) {
                console.error('Validation failed: Invalid StatusId:', data.StatusId);
                showToast('Érvénytelen státusz. Kérjük, válasszon egy érvényes státuszt.', 'danger');
                return;
            }
            if (!data.Subject || !data.Date || !data.PartnerId || !data.CurrentResponsible.ResponsibleId) {
                console.error('Validation failed:', data);
                showToast('Kérjük, töltse ki az összes kötelező mezőt.', 'danger');
                return;
            }
            try {
                const url = communicationId === 'new' ? API_ENDPOINTS.communication : `${API_ENDPOINTS.communication}/${communicationId}`;
                const method = communicationId === 'new' ? 'POST' : 'PUT';
                const response = await fetch(url, {
                    method,
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                    },
                    body: JSON.stringify(data)
                });
                if (!response.ok) {
                    const errorText = await response.text();
                    console.error('Save Response:', errorText);
                    let errorMessage = `Hiba: ${response.status} ${response.statusText}`;
                    try {
                        const errorJson = JSON.parse(errorText);
                        errorMessage = errorJson.errors
                            ? `Érvényesítési hibák: ${Object.entries(errorJson.errors).map(([field, messages]) => `${field}: ${messages.join(', ')}`).join('; ')}`
                            : errorJson.title || errorJson.error || errorText;
                        if (errorMessage.includes('Invalid CommunicationTypeId') || errorMessage.includes('Invalid StatusId')) {
                            errorMessage = 'Érvénytelen típus vagy státusz. A rendszer konfigurációja hibás lehet. Kérjük, lépjen kapcsolatba az adminisztrátorral.';
                        }
                        if (response.status === 404) {
                            errorMessage = 'A kommunikáció mentési végpont nem található. Kérjük, lépjen kapcsolatba az adminisztrátorral.';
                        }
                    } catch {
                        errorMessage += ` Szerver hiba: ${errorText}`;
                    }
                    throw new Error(errorMessage);
                }
                console.log('Save communication success:', response.status);
                showToast('Kommunikáció sikeresen mentve!', 'success');
                window.location.reload();
            } catch (error) {
                console.error('Save communication error:', error);
                showToast(`Kommunikáció mentése sikertelen: ${error.message}`, 'danger');
            }
        }

        // Edit Communication Modals
        // Edit Communication Modals
        document.querySelectorAll('.modal[id^="editCommunicationModal_"]').forEach(modalEl => {
            modalEl.addEventListener('show.bs.modal', function () {
                const commId = this.id.split('_')[1];
                // Delay initialization to ensure modal is fully rendered
                setTimeout(() => {
                    const selects = [
                        {
                            selector: `select[name="PartnerId"]`,
                            options: { endpoint: API_ENDPOINTS.partners, dataKey: 'partners', placeholder: '-- Válasszon partnert --' }
                        },
                        {
                            selector: `select[name="ResponsibleContactId"]`,
                            options: { endpoint: API_ENDPOINTS.users, dataKey: 'users', placeholder: '-- Válasszon felelőst --' }
                        },
                        {
                            selector: `select[name="CommunicationTypeId"]`,
                            options: { endpoint: API_ENDPOINTS.communicationTypes, dataKey: 'communicationTypes', placeholder: '-- Válasszon típust --' }
                        },
                        {
                            selector: `select[name="StatusId"]`,
                            options: { endpoint: API_ENDPOINTS.communicationStatuses, dataKey: 'communicationStatuses', placeholder: '-- Válasszon státuszt --' }
                        }
                    ];
                    selects.forEach(({ selector, options }) => {
                        const select = this.querySelector(selector);
                        if (select) {
                            initializeTomSelect(select, commId, options);
                        } else {
                            console.error(`${selector} not found in modal for communicationId: ${commId}`);
                            showToast(`Hiba: ${selector} nem található.`, 'danger');
                        }
                    });
                }, 100); // 100ms delay to ensure modal rendering
            });
            modalEl.addEventListener('hidden.bs.modal', function () {
                const commId = this.id.split('_')[1];
                const form = this.querySelector(`#communicationForm_${commId}`);
                if (form) form.reset();
                this.querySelectorAll('.tom-select').forEach(select => {
                    if (select.tomselect) select.tomselect.destroy();
                });
            });
        });

        // View Communication Buttons
        document.querySelectorAll('.view-communication-btn').forEach(button => {
            button.addEventListener('click', () => {
                const communicationId = button.closest('tr')?.dataset.communicationId;
                if (!communicationId) return;
                const responsibleSelect = document.querySelector(`#assignResponsibleForm_${communicationId} select[name="ResponsibleId"]`);
                if (responsibleSelect) {
                    initializeTomSelect(responsibleSelect, communicationId, {
                        endpoint: API_ENDPOINTS.users,
                        dataKey: 'users',
                        placeholder: '-- Válasszon felelőst --'
                    });
                }
                loadCommunicationHistory(communicationId);
            });
        });

        // Save Communication Buttons
        document.querySelectorAll('.save-communication').forEach(button => {
            button.addEventListener('click', () => {
                const communicationId = button.dataset.communicationId;
                if (communicationId) {
                    saveCommunication(communicationId);
                } else {
                    console.error('Communication ID not found for save button');
                    showToast('Hiba: Kommunikáció azonosító nem található.', 'danger');
                }
            });
        });

        // Add Post Buttons
        document.querySelectorAll('.add-post').forEach(button => {
            button.addEventListener('click', () => {
                const communicationId = button.dataset.communicationId;
                if (communicationId) addPost(communicationId);
            });
        });

        // Assign Responsible Buttons
        document.querySelectorAll('.assign-responsible').forEach(button => {
            button.addEventListener('click', () => {
                const communicationId = button.dataset.communicationId;
                if (communicationId) assignResponsible(communicationId);
            });
        });

        // Delete Communication Buttons
        document.querySelectorAll('.confirm-delete-communication').forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const communicationId = button.dataset.communicationId;
                if (communicationId) deleteCommunication(communicationId);
            });
        });

        // Filter/Sort Dropdown Logic
        document.querySelectorAll('.dropdown-menu a').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const href = item.getAttribute('href');
                window.location.href = href;
            });
        });

        // Initialize Tooltips
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(tooltipTriggerEl => {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });

    // Expose copyCommunication globally
    window.copyCommunication = copyCommunication;
})();