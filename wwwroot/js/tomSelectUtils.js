(function () {
    // Centralized API endpoints
    const API_ENDPOINTS = {
        partners: '/api/partners',
        users: '/api/users',
        orders: '/api/orders',
        communication: '/api/customercommunication'
    };

    // --- Utility Functions ---
    function showToast(message, type = 'danger') {
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
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
        bsToast.show();
    }

    // Define window.c92.showToast if not already defined
    window.c92 = window.c92 || {};
    window.c92.showToast = window.c92.showToast || ((type, message) => {
        showToast(message, type === 'error' ? 'danger' : type);
    });

    // --- Load Communication History ---
    async function loadCommunicationHistory(communicationId) {
        // (Unchanged, included for context)
        const historyContainer = document.getElementById(`history_${communicationId}`);
        if (!historyContainer) {
            console.error('History container not found:', communicationId);
            showToast('Hiba: Az előzmény konténer nem található.', 'danger');
            return;
        }
        historyContainer.innerHTML = '<p class="text-muted">Előzmények betöltése...</p>';
        try {
            const response = await fetch(`/api/customercommunication/${communicationId}/history`, {
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
                let errorMessage = `HTTP error: ${response.status}`;
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
                            <strong>Hozzászólás</strong> (${post.createdAt ? new Date(post.createdAt).toLocaleString() : 'N/A'}): ${post.content || ''}<br>
                            <small>Írta: ${post.createdByName || 'Ismeretlen'}</small>
                        </li>`;
                });
            }
            if (data.responsibleHistory?.length) {
                data.responsibleHistory.forEach(responsible => {
                    html += `
                        <li class="list-group-item">
                            <strong>Felelős kijelölve</strong> (${responsible.assignedAt ? new Date(responsible.assignedAt).toLocaleString() : 'N/A'}): ${responsible.responsibleName || 'Ismeretlen'}<br>
                            <small>Kijelölte: ${responsible.assignedByName || 'Ismeretlen'}</small>
                        </li>`;
                });
            }
            html += '</ul>';
            historyContainer.innerHTML = html;
        } catch (error) {
            console.error('History load error:', error);
            historyContainer.innerHTML = '<p class="text-danger">Hiba az előzmények betöltésekor.</p>';
            showToast(`Hiba: ${error.message}`, 'danger');
        }
    }

    // --- Save Communication ---
    async function saveCommunication(communicationId) {
        // (Unchanged, included for context)
        const form = document.getElementById(`communicationForm_${communicationId}`);
        if (!form) {
            console.error('Form not found for communicationId:', communicationId);
            showToast('Hiba: Az űrlap nem található.', 'danger');
            return;
        }
        const contactSelect = form.querySelector('select[name="ContactId"]');
        const responsibleSelect = form.querySelector('select[name="ResponsibleContactId"]');
        if (!contactSelect || !contactSelect.value) {
            console.error('Contact selection is empty or not initialized');
            showToast('Kérjük, válasszon ki egy kapcsolattartót.', 'danger');
            return;
        }
        if (!responsibleSelect || !responsibleSelect.value) {
            console.error('Responsible selection is empty or not initialized');
            showToast('Kérjük, válasszon ki egy felelőst.', 'danger');
            return;
        }
        const formData = new FormData(form);
        const contactText = contactSelect.selectedOptions[0]?.text || 'Unknown';
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
            ContactId: parseInt(formData.get('ContactId')) || null,
            Subject: formData.get('Subject')?.trim() || null,
            Date: formData.get('Date') ? new Date(formData.get('Date')).toISOString() : null,
            Note: formData.get('Note')?.trim() || null,
            StatusId: parseInt(formData.get('StatusId')) || null,
            PartnerId: parseInt(formData.get('PartnerId')) || null,
            LeadId: parseInt(formData.get('LeadId')) || null,
            QuoteId: parseInt(formData.get('QuoteId')) || null,
            OrderId: parseInt(formData.get('OrderId')) || null,
            Metadata: formData.get('Metadata')?.trim() || null,
            Posts: formData.get('InitialPost')?.trim() ? [{
                Content: formData.get('InitialPost').trim(),
                CreatedByName: username,
                CreatedAt: new Date().toISOString()
            }] : [],
            CurrentResponsible: {
                ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                ResponsibleName: responsibleText.split('(')[0].trim() || null,
                AssignedByName: username,
                AssignedAt: new Date().toISOString()
            },
            ResponsibleHistory: [{
                ResponsibleId: formData.get('ResponsibleContactId')?.trim() || null,
                ResponsibleName: responsibleText.split('(')[0].trim() || null,
                AssignedByName: username,
                AssignedAt: new Date().toISOString()
            }]
        };
        if (!data.CommunicationTypeId || !data.ContactId || !data.Subject || !data.Date || !data.StatusId || !data.CurrentResponsible.ResponsibleId) {
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

    // (Other functions like addPost, assignResponsible, deleteCommunication, copyCommunication remain unchanged)

    // --- Initialize Event Listeners ---
    document.addEventListener('DOMContentLoaded', () => {
        console.log('Main script loaded');

        // Initialize TomSelect for Responsible dropdown
        const responsibleSelect = document.querySelector('select[name="ResponsibleId"]');
        if (responsibleSelect) {
            window.c92.initializeCurrencyTomSelect(responsibleSelect, 'communication').catch(error => {
                console.error('Failed to initialize Responsible TomSelect:', error);
                showToast('Hiba a felelős választó inicializálásakor.', 'danger');
            });
        }

        // New Communication Modal
        const newCommunicationModal = document.getElementById('newCommunicationModal');
        if (newCommunicationModal) {
            newCommunicationModal.addEventListener('shown.bs.modal', () => {
                console.log('New Communication modal shown');
                const selects = [
                    {
                        selector: '#communicationForm_new select[name="ContactId"]',
                        initFunction: window.c92.initializePartnerTomSelect,
                        options: { context: 'communication', dataKey: 'contacts', placeholder: '-- Válasszon kapcsolattartót --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="ResponsibleContactId"]',
                        initFunction: window.c92.initializeCurrencyTomSelect,
                        options: { context: 'communication', placeholder: '-- Válasszon felelőst --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="PartnerId"]',
                        initFunction: window.c92.initializePartnerTomSelect,
                        options: { context: 'communication', dataKey: 'partners', placeholder: '-- Válasszon partnert --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="QuoteId"]',
                        initFunction: window.c92.initializeQuoteTomSelect,
                        options: { context: 'communication', placeholder: '-- Válasszon árajánlatot --' }
                    },
                    {
                        selector: '#communicationForm_new select[name="OrderId"]',
                        initFunction: window.c92.initializePartnerTomSelect, // Assuming orders use partner-like API
                        options: { context: 'communication', dataKey: 'orders', placeholder: '-- Válasszon rendelést --' }
                    }
                ];
                selects.forEach(({ selector, initFunction, options }) => {
                    const select = document.querySelector(selector);
                    if (select) {
                        initFunction(select, 'new', options.context, select.dataset.selectedId).catch(error => {
                            console.error(`Failed to initialize TomSelect for ${selector}:`, error);
                            showToast(`Hiba: ${selector} inicializálása sikertelen.`, 'danger');
                        });
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
                    if (select.tomselect) {
                        select.tomselect.destroy();
                        select.dataset.tomSelectInitialized = 'false';
                    }
                });
            });
        }

        // Edit Communication Modals
        document.querySelectorAll('.modal[id^="editCommunicationModal_"]').forEach(modalEl => {
            modalEl.addEventListener('show.bs.modal', function () {
                const commId = this.id.split('_')[1];
                const selects = [
                    {
                        selector: `select[name="PartnerId"]`,
                        initFunction: window.c92.initializePartnerTomSelect,
                        options: { context: 'communication', dataKey: 'partners', placeholder: '-- Válasszon partnert --' }
                    },
                    {
                        selector: `select[name="ContactId"]`,
                        initFunction: window.c92.initializePartnerTomSelect,
                        options: { context: 'communication', dataKey: 'contacts', placeholder: '-- Válasszon kapcsolattartót --' }
                    },
                    {
                        selector: `select[name="ResponsibleContactId"]`,
                        initFunction: window.c92.initializeCurrencyTomSelect,
                        options: { context: 'communication', placeholder: '-- Válasszon felelőst --' }
                    },
                    {
                        selector: `select[name="QuoteId"]`,
                        initFunction: window.c92.initializeQuoteTomSelect,
                        options: { context: 'communication', placeholder: '-- Válasszon árajánlatot --' }
                    },
                    {
                        selector: `select[name="OrderId"]`,
                        initFunction: window.c92.initializePartnerTomSelect, // Assuming orders use partner-like API
                        options: { context: 'communication', dataKey: 'orders', placeholder: '-- Válasszon rendelést --' }
                    }
                ];
                selects.forEach(({ selector, initFunction, options }) => {
                    const select = this.querySelector(selector);
                    if (select) {
                        initFunction(select, commId, options.context, select.dataset.selectedId).catch(error => {
                            console.error(`Failed to initialize TomSelect for ${selector} in modal ${commId}:`, error);
                            showToast(`Hiba: ${selector} inicializálása sikertelen.`, 'danger');
                        });
                    } else {
                        console.error(`${selector} not found in modal for communicationId: ${commId}`);
                        showToast(`Hiba: ${selector} nem található.`, 'danger');
                    }
                });
            });
            modalEl.addEventListener('hidden.bs.modal', () => {
                const commId = this.id.split('_')[1];
                const form = this.querySelector(`#communicationForm_${commId}`);
                if (form) form.reset();
                this.querySelectorAll('.tom-select').forEach(select => {
                    if (select.tomselect) {
                        select.tomselect.destroy();
                        select.dataset.tomSelectInitialized = 'false';
                    }
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
                    window.c92.initializeCurrencyTomSelect(responsibleSelect, 'communication').catch(error => {
                        console.error('Failed to initialize Responsible TomSelect:', error);
                        showToast('Hiba a felelős választó inicializálásakor.', 'danger');
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
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });

        // In utils.js
        window.c92.initializeTomSelect = async function (select, id, options = {}) {
            const { endpoint = '/api/partners/select', dataKey = 'items', placeholder = '-- Válasszon --' } = options;
            const selectedId = select?.dataset.selectedId || '';
            const selectedText = select?.dataset.selectedText || '';
            if (!select) {
                console.error('Select element is null for id:', id);
                window.c92.showToast(`Hiba: A ${dataKey} kiválasztó elem nem található.`, 'danger');
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
                        const response = await fetch(url, {
                            headers: {
                                'Authorization': `Bearer ${localStorage.getItem('token') || ''}`,
                                'Accept': 'application/json'
                            }
                        });
                        if (!response.ok) {
                            throw new Error(`Failed to fetch ${dataKey}: ${response.status}`);
                        }
                        const data = await response.json();
                        let items = data.map(item => ({ id: item.id, text: item.text }));
                        if (selectedId && selectedText && !items.find(item => item.id == selectedId)) {
                            items.unshift({ id: selectedId, text: selectedText });
                        }
                        callback(items);
                    } catch (error) {
                        console.error(`Error fetching ${dataKey}:`, error);
                        window.c92.showToast(`Hiba: Nem sikerült betölteni a ${dataKey} adatokat.`, 'danger');
                        callback([]);
                    }
                },
                render: {
                    option: (data, escape) => `<div>${escape(data.text)}</div>`,
                    item: (data, escape) => `<div>${escape(data.text)}</div>`,
                    no_results: (data, escape) => `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`
                },
                onInitialize: function () {
                    if (selectedId && selectedText) {
                        this.addOption({ id: selectedId, text: selectedText });
                        this.setValue(selectedId);
                    }
                    this.load('');
                }
            });
        };
    });

    // Expose copyCommunication globally
    window.copyCommunication = copyCommunication;
})();