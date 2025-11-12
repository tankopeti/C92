// wwwroot/js/Tasks.js
window.Tasks = (function () {
    const api = '/api/tasks';
    const csrf = document.querySelector('meta[name="csrf-token"]')?.content ||
        document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];

    const dropdownCacheStatic = {};

const dependentSelects = {
    PartnerId: [
        {
            target: 'SiteId',
            url: (partnerId) => `/api/partners/${partnerId}/sites/select`
        },
        {
            target: 'ContactId',
            url: (partnerId) => `/api/partners/${partnerId}/contacts/select`
        },
        {
            target: 'QuoteId',
            url: (partnerId) => `/api/quotes/select?partnerId=${partnerId}`
        },
        {
            target: 'OrderId',
            url: (partnerId) => `/api/Orders/?partnerId=${partnerId}`
        },
        {
            target: 'CustomerCommunicationId',
            url: (partnerId) => `/api/customercommunication/select?partnerId=${partnerId}`
        }
    ]
};

    // ---------------------------------------------------------------
    // LOG HELPER
    // ---------------------------------------------------------------
    function log(message, ...args) {
        console.log(`%c[Tasks] ${message}`, 'color: #28a745; font-weight: bold;', ...args);
    }

    function error(message, ...args) {
        console.error(`%c[Tasks] ${message}`, 'color: #dc3545; font-weight: bold;', ...args);
    }

    // ---------------------------------------------------------------
    // POPULATE SELECT (TomSelect)
    // ---------------------------------------------------------------
    function populateSelect(selector, urlOrFn, selectedId = null) {
        log(`populateSelect: ${selector}`, { urlOrFn, selectedId });
        const select = document.querySelector(selector);
        if (!select) return error(`Select not found: ${selector}`);

        if (select.tomselect) {
            select.tomselect.destroy();
            select.tomselect = null;
        }

        const isMultiple = select.hasAttribute('multiple');
        const valueField = 'id';
        const labelField = 'text';

        const config = {
            valueField: valueField,
            labelField: labelField,
            searchField: [labelField],
            placeholder: 'Válasszon...',
            allowEmptyOption: true,
            openOnFocus: true,
            sortField: { field: labelField, direction: 'asc' },
            maxOptions: 100,
            plugins: isMultiple ? ['remove_button'] : []
        };

        function initWithData(data) {
            config.options = data;
            const ts = new TomSelect(selector, config);
            if (selectedId !== null && selectedId !== undefined) {
                if (isMultiple && Array.isArray(selectedId)) {
                    ts.setValue(selectedId);
                } else {
                    ts.setValue(selectedId);
                }
            }
            log(`TomSelect initialized: ${selector}`, { optionsCount: data.length });
            return ts;
        }

        if (typeof urlOrFn === 'string') {
            const cacheKey = urlOrFn;
            if (!dropdownCacheStatic[cacheKey]) {
                log(`Fetching static data: ${urlOrFn}`);
                fetch(urlOrFn)
                    .then(r => r.ok ? r.json() : Promise.reject())
                    .then(data => {
                        dropdownCacheStatic[cacheKey] = data;
                        initWithData(data);
                    })
                    .catch(err => {
                        error(`Failed to fetch ${urlOrFn}`, err);
                        dropdownCacheStatic[cacheKey] = [];
                        initWithData([]);
                    });
            } else {
                initWithData(dropdownCacheStatic[cacheKey]);
            }
            return;
        }

        if (typeof urlOrFn === 'function') {
            config.load = function (query, callback) {
                const url = urlOrFn(this.control) + (query.trim() ? `?q=${encodeURIComponent(query)}` : '');
                log(`Dynamic load: ${url}`);
                fetch(url)
                    .then(res => res.ok ? res.json() : Promise.reject())
                    .then(json => {
                        const items = Array.isArray(json) ? json : (json.items || json.results || []);
                        callback(items);
                    })
                    .catch(e => {
                        error('TomSelect load error:', e);
                        callback([]);
                    });
            };
        }

        const ts = new TomSelect(selector, config);
        ts.control.addEventListener('click', () => {
            if (!ts.isOpen) {
                ts.load('');
                ts.open();
            }
        });

        if (selectedId !== null && selectedId !== undefined) {
            if (isMultiple && Array.isArray(selectedId)) {
                ts.setValue(selectedId);
            } else {
                ts.setValue(selectedId);
            }
        }
        return ts;
    }

    // ---------------------------------------------------------------
    // TOAST
    // ---------------------------------------------------------------
    function toast(message, type = 'info') {
        const container = document.getElementById('toastContainer') || (() => {
            const c = document.createElement('div');
            c.id = 'toastContainer';
            c.className = 'position-fixed bottom-0 end-0 p-3';
            c.style.zIndex = '1100';
            document.body.appendChild(c);
            return c;
        })();
        const t = document.createElement('div');
        t.className = `toast align-items-center text-white bg-${type} border-0`;
        t.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
        container.appendChild(t);
        new bootstrap.Toast(t, { delay: 4000 }).show();
    }

    // ---------------------------------------------------------------
    // SHOW ERRORS
    // ---------------------------------------------------------------
    function showErrors(form, errors) {
        log('showErrors', errors);
        form.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
        Object.keys(errors).forEach(key => {
            const input = form.querySelector(`[name="${key}"]`);
            if (input) {
                const err = input.closest('.mb-3')?.querySelector('.text-danger');
                if (err) err.textContent = Array.isArray(errors[key]) ? errors[key].join(', ') : errors[key];
            }
        });
    }

    // ---------------------------------------------------------------
    // CASCADE PARTNER → SITE + CONTACT
    // ---------------------------------------------------------------
function setupPartnerCascade(form, data = {}) {
    const partnerSelect = form.querySelector('[name="PartnerId"]');
    if (!partnerSelect) return;

    const checkTs = setInterval(() => {
        if (partnerSelect.tomselect) {
            clearInterval(checkTs);
            const ts = partnerSelect.tomselect;
            ts.off('change');

            ts.on('change', () => {
                const partnerId = ts.getValue();
                log(`Partner changed: ${partnerId}`);

                dependentSelects.PartnerId.forEach(dep => {
                    const targetSelect = form.querySelector(`[name="${dep.target}"]`);
                    if (!targetSelect) return;

                    // Destroy existing TomSelect
                    if (targetSelect.tomselect) {
                        targetSelect.tomselect.clear();
                        targetSelect.tomselect.clearOptions();
                        targetSelect.tomselect.destroy();
                    }

                    // Re-init with dynamic URL
                    const selectedId = data[dep.target.toLowerCase()] || null;
                    const urlFn = partnerId ? () => dep.url(partnerId) : () => null;

                    populateSelect(`#${targetSelect.id}`, urlFn, selectedId);
                });
            });

            // Trigger initial load if partner already selected
            if (ts.getValue()) {
                ts.trigger('change');
            }
        }
    }, 100);
}

// ---------------------------------------------------------------
// CREATE (CORRECTED)
// ---------------------------------------------------------------
async function create(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    // Initialize the data object using PascalCase to match the C# DTO
    const data = { IsActive: true }; 

    // Loop through form data entries
    for (const [key, value] of formData.entries()) {
        if (key === '__RequestVerificationToken') continue;

        // CRITICAL FIX: Use the original form field name (key), which is PascalCase (e.g., "Title", "TaskTypePMId")
        const targetKey = key; 

        if (value === '' || value == null) {
            data[targetKey] = null;
        } 
        // Handle Date conversion
        else if (targetKey === 'DueDate') {
            const d = new Date(value);
            data[targetKey] = isNaN(d.getTime()) ? null : d.toISOString();
        } 
        // Handle numeric fields (Hours and IDs)
        else if (!isNaN(value) && [
            'EstimatedHours', 'ActualHours',
            'TaskTypePMId', 'TaskStatusPMId', 'TaskPriorityPMId',
            'PartnerId', 'SiteId', 'ContactId', 'QuoteId', 'OrderId', 'CustomerCommunicationId'
        ].includes(targetKey)) {
            // Hours are decimal (float), IDs are integer
            data[targetKey] = targetKey.includes('Hours') ? parseFloat(value) : parseInt(value, 10);
        } 
        // Handle multi-select arrays (Resources and Employees)
        else if (targetKey === 'ResourceIds' || targetKey === 'EmployeeIds') {
            data[targetKey] = formData.getAll(key)
                .map(v => parseInt(v, 10))
                .filter(v => !isNaN(v));
        } 
        // Default assignment for strings (Title, Description, AssignedToId)
        else {
            data[targetKey] = value;
        }
    }

    // Client-side required field check (Title is required by DTO/DB)
    if (!data.Title || data.Title.trim() === '') {
        toast('A cím megadása kötelező!', 'danger');
        return;
    }
    
    // Client-side required field check (TaskTypePMId is required by Service)
    if (!data.TaskTypePMId) {
        toast('A feladat típusa kötelező!', 'danger');
        return;
    }

    try {
        const res = await fetch(api, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': csrf  
            },
            body: JSON.stringify(data)
        });

        if (!res.ok) {
            // Attempt to parse server validation errors (400) or generic 500 error
            const err = await res.json().catch(() => ({}));
            
            // Note: The C# controller returns errors under the 'errors' key
            showErrors(form, err.errors || { General: ['Szerver hiba.'] });
            
            // Log the detailed failure for debugging
            error(`create: failed ${res.status}`, err);
            throw new Error(`HTTP ${res.status}`);
        }

        toast('Feladat létrehozva!', 'success');
        // Close modal and reload page on success
        bootstrap.Modal.getInstance(form.closest('.modal')).hide();
        setTimeout(() => location.reload(), 500);
    } catch (err) {
        console.error(err);
        toast('Hiba a mentéskor.', 'danger');
    }
}

    // ---------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------
    async function update(e) {
        e.preventDefault();
        log('update: started');
        const form = e.target;
        const formData = new FormData(form);
        const data = {};

        for (const [key, value] of formData.entries()) {
            if (key === '__RequestVerificationToken') continue;
            if (value === '') {
                data[key] = null;
            } else if (key === 'DueDate') {
                const d = new Date(value);
                data[key] = isNaN(d) ? null : d.toISOString();
            } else if (!isNaN(value) && ['Id', 'EstimatedHours', 'ActualHours', 'TaskTypePMId', 'TaskStatusPMId', 'TaskPriorityPMId', 'PartnerId', 'SiteId', 'ContactId', 'QuoteId', 'OrderId', 'CustomerCommunicationId'].includes(key)) {
                data[key] = key.includes('Hours') ? parseFloat(value) : parseInt(value, 10);
            } else if (key === 'ResourceIds' || key === 'EmployeeIds') {
                data[key] = formData.getAll(key).map(v => parseInt(v, 10));
            } else {
                data[key] = value;
            }
        }

        const taskId = document.getElementById('editTaskId')?.value || data.Id;
        if (!taskId) return error('Task ID missing for update');

        try {
            log('update: sending', { id: taskId, data });
            const res = await fetch(`${api}/${taskId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
                body: JSON.stringify(data)
            });

            if (res.ok) {
                toast('Frissítve!', 'success');
                bootstrap.Modal.getInstance(form.closest('.modal')).hide();
                location.reload();
            } else {
                const err = await res.json().catch(() => ({ errors: { General: ['Hiba a frissítéskor.'] } }));
                showErrors(form, err.errors || err);
                error(`update: failed ${res.status}`, err);
            }
        } catch (err) {
            error('update: exception', err);
            toast('Hiba történt a frissítéskor.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // OPEN VIEW MODAL
    // ---------------------------------------------------------------
    async function openViewModal(taskId) {
        log(`openViewModal: ${taskId}`);
        const modalEl = document.getElementById('taskViewModal');
        if (!modalEl) return error('taskViewModal not found');

        const modal = new bootstrap.Modal(modalEl, { backdrop: 'static' });
        const body = document.getElementById('taskModalBody');

        try {
            const res = await fetch(`${api}/${taskId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            log('Task data loaded', data);

            body.innerHTML = `
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>Cím:</strong> ${data.title}</p>
                        <p><strong>Leírás:</strong> ${data.description || '<em>Nincs</em>'}</p>
                        <p><strong>Típus:</strong> ${data.taskTypePMName || '-'}</p>
                        <p><strong>Státusz:</strong> <span class="badge bg-primary">${data.taskStatusPMName}</span></p>
                        <p><strong>Prioritás:</strong> <span class="badge bg-secondary">${data.taskPriorityPMName}</span></p>
                        <p><strong>Határidő:</strong> ${data.dueDate ? new Date(data.dueDate).toLocaleDateString('hu-HU') : '-'}</p>
                        <p><strong>Becsült órák:</strong> ${data.estimatedHours ?? '-'}</p>
                        <p><strong>Tényleges órák:</strong> ${data.actualHours ?? '-'}</p>
                        <p><strong>Felelős:</strong> ${data.assignedToName || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <p><strong>Partner:</strong> ${data.partnerName || '-'}</p>
                        <p><strong>Helyszín:</strong> ${data.siteName || '-'}</p>
                        <p><strong>Kapcsolattartó:</strong> ${data.contactName || '-'}</p>
                        <p><strong>Árajánlat:</strong> ${data.quoteNumber || '-'}</p>
                        <p><strong>Megrendelés:</strong> ${data.orderNumber || '-'}</p>
                        <p><strong>Ügyfélkommunikáció:</strong> ${data.customerCommunicationSubject || '-'}</p>
                        <p><strong>Létrehozva:</strong> ${new Date(data.createdDate).toLocaleString('hu-HU')} által ${data.createdByName}</p>
                        <p><strong>Módosítva:</strong> ${data.updatedDate ? new Date(data.updatedDate).toLocaleString('hu-HU') : '-'}</p>
                    </div>
                </div>
            `;

            document.getElementById('editTaskBtn').style.display = 'inline-block';
            document.getElementById('editTaskBtn').onclick = () => openEditModal(taskId);
            modal.show();
        } catch (err) {
            error('openViewModal failed', err);
            toast('Hiba az adatok betöltésekor.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // OPEN EDIT MODAL
    // ---------------------------------------------------------------
    async function openEditModal(taskId) {
        log(`openEditModal: ${taskId}`);
        const modalEl = document.getElementById('taskViewModal');
        if (!modalEl) return error('taskViewModal not found');

        const modal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
        const body = document.getElementById('taskModalBody');

        try {
            const res = await fetch(`${api}/${taskId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            log('Edit data loaded', data);

            body.innerHTML = `
                <form id="editTaskForm">
                    <input type="hidden" id="editTaskId" value="${taskId}" />
                    ${document.querySelector('#newTaskModal .modal-body').innerHTML}
                </form>
            `;

            const form = body.querySelector('#editTaskForm');

            // Populate fields
            const setValue = (name, value) => {
                const el = form.querySelector(`[name="${name}"]`);
                if (el) el.value = value ?? '';
            };
            setValue('Title', data.title);
            setValue('Description', data.description);
            setValue('DueDate', data.dueDate?.split('T')[0]);
            setValue('EstimatedHours', data.estimatedHours);
            setValue('ActualHours', data.actualHours);

            modalEl.addEventListener('shown.bs.modal', () => {
                log('edit modal shown – initializing TomSelects');
                setTimeout(() => {
                    // Destroy existing
                    ['TaskTypePMId', 'TaskStatusPMId', 'TaskPriorityPMId', 'AssignedToId', 'PartnerId', 'SiteId', 'ContactId', 'QuoteId', 'OrderId', 'CustomerCommunicationId'].forEach(name => {
                        const el = form.querySelector(`[name="${name}"]`);
                        if (el?.tomselect) el.tomselect.destroy();
                    });

                    // Re-init
                    populateSelect('[name="TaskTypePMId"]', '/api/tasktypes/select', data.taskTypePMId);
                    populateSelect('[name="TaskStatusPMId"]', '/api/taskstatuses/select', data.taskStatusPMId);
                    populateSelect('[name="TaskPriorityPMId"]', '/api/taskpriorities/select', data.taskPriorityPMId);
                    populateSelect('[name="AssignedToId"]', '/api/users/select', data.assignedToId);
                    populateSelect('[name="PartnerId"]', '/api/partners/select', data.partnerId);
                    populateSelect('[name="QuoteId"]', '/api/quotes/select', data.quoteId);
                    populateSelect('[name="OrderId"]', '/api/orders', data.orderId);
                    populateSelect('[name="CustomerCommunicationId"]', '/api/communications/select', data.customerCommunicationId);

                    // Resources & Employees
                    populateSelect('[name="ResourceIds"]', '/api/resources/select', data.resourceIds);
                    populateSelect('[name="EmployeeIds"]', '/api/employee/select', data.employeeIds);

                    // Cascade
                    setupPartnerCascade(form, { siteId: data.siteId, contactId: data.contactId });

                    form.onsubmit = update;
                }, 200);
            }, { once: true });

            document.getElementById('editTaskBtn').style.display = 'none';
            modal.show();
        } catch (err) {
            error('openEditModal failed', err);
            toast('Hiba az adatok betöltésekor.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // OPEN DELETE MODAL
    // ---------------------------------------------------------------
    function openDeleteModal(taskId) {
        log(`openDeleteModal: ${taskId}`);
        const modalEl = document.getElementById('deleteTaskModal');
        if (!modalEl) return error('deleteTaskModal not found');

        document.getElementById('confirmDeleteBtn').onclick = async () => {
            try {
                const res = await fetch(`${api}/${taskId}`, {
                    method: 'DELETE',
                    headers: { 'X-CSRF-TOKEN': csrf }
                });
                if (res.ok) {
                    toast('Törölve!', 'success');
                    bootstrap.Modal.getInstance(modalEl).hide();
                    location.reload();
                } else {
                    throw new Error('Delete failed');
                }
            } catch (err) {
                error('delete failed', err);
                toast('Hiba a törléskor.', 'danger');
            }
        };
        new bootstrap.Modal(modalEl).show();
    }

    // ---------------------------------------------------------------
    // EVENT DELEGATION
    // ---------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        log('DOM loaded – setting up event delegation');

        // Table buttons
        document.querySelector('table')?.addEventListener('click', (e) => {
            const btn = e.target.closest('button, a');
            if (!btn) return;
            const taskId = btn.dataset.taskId;
            if (!taskId) return;

            if (btn.classList.contains('btn-view-task')) {
                e.preventDefault();
                openViewModal(taskId);
            } else if (btn.classList.contains('btn-edit-task')) {
                e.preventDefault();
                openEditModal(taskId);
            } else if (btn.classList.contains('btn-delete-task')) {
                e.preventDefault();
                openDeleteModal(taskId);
            }
        });

        // Create form
        const createForm = document.querySelector('#newTaskModal form');
        createForm?.addEventListener('submit', create);

        // Init Create Modal TomSelects
        const createModal = document.getElementById('newTaskModal');
        createModal?.addEventListener('shown.bs.modal', () => {
            log('create modal shown – initializing TomSelects');
            setTimeout(() => {
                populateSelect('[name="TaskTypePMId"]', '/api/tasks/tasktypes/select');
                populateSelect('[name="TaskStatusPMId"]', '/api/tasks/taskstatuses/select', 1);
                populateSelect('[name="TaskPriorityPMId"]', '/api/tasks/taskpriorities/select', 2);
                populateSelect('[name="AssignedToId"]', '/api/users/select');
                populateSelect('[name="PartnerId"]', '/api/partners/select');
                populateSelect('[name="QuoteId"]', '/api/quotes/select');
                populateSelect('[name="OrderId"]', '/api/orders');
                populateSelect('[name="CustomerCommunicationId"]', '/api/customercommunication/select');
                populateSelect('[name="ResourceIds"]', '/api/resources/select');
                populateSelect('[name="EmployeeIds"]', '/api/employee/select');
                setupPartnerCascade(createForm);
            }, 300);
        });
    });

    return {
        create,
        update,
        openViewModal,
        openEditModal,
        openDeleteModal
    };
})();