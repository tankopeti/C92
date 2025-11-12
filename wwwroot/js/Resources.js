// wwwroot/js/resources.js
window.Resources = (function () {
    const api = '/api/resources';
    const csrf = document.querySelector('meta[name="csrf-token"]')?.content ||
        document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];

    const dropdownCacheStatic = {};

    const dependentSelects = {
        PartnerId: [
            { target: 'SiteId', url: (partnerId) => `/api/partners/${partnerId}/sites/select` },
            { target: 'ContactId', url: (partnerId) => `/api/partners/${partnerId}/contacts/select` }
        ]
    };

    // ---------------------------------------------------------------
    // LOG HELPER
    // ---------------------------------------------------------------
    function log(message, ...args) {
        console.log(`%c[Resources] ${message}`, 'color: #007bff; font-weight: bold;', ...args);
    }

    function error(message, ...args) {
        console.error(`%c[Resources] ${message}`, 'color: #dc3545; font-weight: bold;', ...args);
    }

    // ---------------------------------------------------------------
    // POPULATE SELECT
    // ---------------------------------------------------------------
    function populateSelect(selector, urlOrFn, selectedId = null) {
        log(`populateSelect: ${selector}`, { urlOrFn, selectedId });
        const select = document.querySelector(selector);
        if (!select) return error(`Select not found: ${selector}`);

        if (select.tomselect) {
            select.tomselect.destroy();
            select.tomselect = null;
        }

        const isEmployee = selector.includes('Employee');
        const valueField = isEmployee ? 'value' : 'id';
        const labelField = isEmployee ? 'label' : 'text';

        const config = {
            valueField: valueField,
            labelField: labelField,
            searchField: [labelField],
            placeholder: 'Válasszon...',
            allowEmptyOption: true,
            openOnFocus: true,
            sortField: { field: labelField, direction: 'asc' },
            plugins: ['remove_button'],
            maxOptions: 100
        };

        function initWithData(data) {
            config.options = data;
            const ts = new TomSelect(selector, config);
            if (selectedId) ts.setValue(selectedId);
            log(`TomSelect initialized: ${selector}`, { optionsCount: data.length });
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
                        const items = isEmployee
                            ? json.map(x => ({ id: x.value, text: x.label }))
                            : Array.isArray(json) ? json : (json.items || json.results || []);
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

        if (selectedId) ts.setValue(selectedId);
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

                        if (targetSelect.tomselect) targetSelect.tomselect.destroy();

                        const urlFn = partnerId
                            ? () => `/api/partners/${partnerId}/${dep.target === 'SiteId' ? 'sites' : 'contacts'}/select`
                            : () => null;

                        const selectedId = dep.target === 'SiteId' ? data.siteId : data.contactId;
                        populateSelect(`#${targetSelect.id}`, urlFn, selectedId);
                    });
                });

                if (ts.getValue()) ts.trigger('change');
            }
        }, 400);
    }

    // ---------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------
    async function create(e) {
        e.preventDefault();
        log('create: started');
        const form = e.target;
        const formData = new FormData(form);
        const data = { IsActive: true };

        for (const [key, value] of formData.entries()) {
            if (key === '__RequestVerificationToken') continue;
            if (value === '') {
                data[key] = null;
            } else if (key.match(/Date|NextService|ServiceDate|WarrantyExpireDate/)) {
                const d = new Date(value);
                data[key] = isNaN(d) ? null : d.toISOString();
            } else if (!isNaN(value) && ['Price', 'WarrantyPeriod', 'ResourceTypeId', 'ResourceStatusId', 'PartnerId', 'SiteId', 'ContactId', 'EmployeeId', 'WhoBuyId', 'WhoLastServicedId'].includes(key)) {
                data[key] = key === 'Price' ? parseFloat(value) : parseInt(value, 10);
            } else {
                data[key] = value;
            }
        }

        try {
            log('create: sending', data);
            const res = await fetch(api, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
                body: JSON.stringify(data)
            });

            if (!res.ok) {
                const err = await res.json().catch(() => ({ errors: { General: ['Szerver hiba.'] } }));
                showErrors(form, err.errors || err);
                error(`create: failed ${res.status}`, err);
                throw new Error(`HTTP ${res.status}`);
            }

            toast('Létrehozva!', 'success');
            bootstrap.Modal.getInstance(form.closest('.modal')).hide();
            location.reload();
        } catch (err) {
            error('create: exception', err);
            toast('Hiba történt a mentéskor.', 'danger');
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
        const data = Object.fromEntries(formData.entries());

        const resourceId = document.getElementById('editId').value;
        data.ResourceId = parseInt(resourceId, 10);

        Object.keys(data).forEach(key => {
            if (data[key] === '') data[key] = null;
            else if (key.match(/Date|NextService|ServiceDate|WarrantyExpireDate/) && data[key]) {
                data[key] = new Date(data[key]).toISOString();
            } else if (!isNaN(data[key]) && ['Price', 'WarrantyPeriod', 'ResourceTypeId', 'ResourceStatusId', 'PartnerId', 'SiteId', 'ContactId', 'EmployeeId', 'WhoBuyId', 'WhoLastServicedId'].includes(key)) {
                data[key] = key === 'Price' ? parseFloat(data[key]) : parseInt(data[key], 10);
            }
        });

        try {
            log('update: sending', { id: resourceId, data });
            const res = await fetch(`${api}/${resourceId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
                body: JSON.stringify(data)
            });
            const result = await res.json();
            if (res.ok) {
                toast('Frissítve!', 'success');
                bootstrap.Modal.getInstance(form.closest('.modal')).hide();
                location.reload();
            } else {
                showErrors(form, result.errors || { General: [result.message] });
                error(`update: failed ${res.status}`, result);
            }
        } catch (err) {
            error('update: exception', err);
            toast('Hiba történt a frissítéskor.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // OPEN VIEW MODAL
    // ---------------------------------------------------------------
    async function openViewModal(resourceId) {
        log(`openViewModal: ${resourceId}`);
        const modalEl = document.getElementById('viewResourceModal');
        if (!modalEl) return error('viewResourceModal not found');

        const modal = new bootstrap.Modal(modalEl, { backdrop: 'static' });
        const historyContainer = document.getElementById('viewHistoryContainer');

        try {
            log(`Fetching resource: ${api}/${resourceId}`);
            const res = await fetch(`${api}/${resourceId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            log('Resource data loaded', data);

            modalEl.addEventListener('shown.bs.modal', () => {
                log('view modal shown – populating fields');
                const set = (id, value) => {
                    const el = document.getElementById(id);
                    if (el) {
                        el.value = value ?? '';
                        log(`Set ${id} = ${value}`);
                    } else {
                        error(`Element not found: ${id}`);
                    }
                };
                const setDate = (id, iso) => {
                    const el = document.getElementById(id);
                    if (el && iso) {
                        el.value = iso.split('T')[0];
                        log(`Set date ${id} = ${iso.split('T')[0]}`);
                    } else if (!el) {
                        error(`Date element not found: ${id}`);
                    }
                };

                set('viewName', data.name);
                set('viewSerial', data.serial);
                set('viewPrice', data.price);
                setDate('viewDateOfPurchase', data.dateOfPurchase);
                set('viewType', data.resourceTypeName);
                set('viewStatus', data.resourceStatusName);
                set('viewComment1', data.comment1);
                set('viewComment2', data.comment2);
                setDate('viewNextService', data.nextService);
                setDate('viewServiceDate', data.serviceDate);
                set('viewWarrantyPeriod', data.warrantyPeriod);
                setDate('viewWarrantyExpireDate', data.warrantyExpireDate);
                set('viewWhoBuy', data.whoBuyName);
                set('viewWhoLastServiced', data.whoLastServicedName);
                set('viewPartner', data.partnerName);
                set('viewSite', data.siteName);
                set('viewContact', data.contactName);
                set('viewEmployee', data.employeeName);

                if (historyContainer) loadViewHistory(resourceId, historyContainer);
            }, { once: true });

            modal.show();
        } catch (err) {
            error('openViewModal failed', err);
            toast('Hiba az adatok betöltésekor.', 'danger');
        }
    }

function loadViewHistory(resourceId, container) {
    log(`Loading history for ${resourceId}`);
    fetch(`${api}/${resourceId}/history`, {
        headers: { 'X-CSRF-TOKEN': csrf }
    })
    .then(r => r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`)))
    .then(history => {
        log(`History loaded: ${history?.length || 0} items`, history);
        if (!history?.length) {
            container.innerHTML = '<div class="text-muted">Nincs előzmény.</div>';
            return;
        }

        const rows = history.map(h => {
            // 1. DÁTUM
            const dateStr = h.modifiedDate || h.ModifiedDate;
            let formattedDate = 'Ismeretlen dátum';
            if (dateStr) {
                const d = new Date(dateStr);
                if (!isNaN(d)) {
                    formattedDate = d.toLocaleString('hu-HU', {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                }
            }

            // 2. LEÍRÁS – FONTOS: camelCase!
            const rawDesc = h.changeDescription || h.ChangeDescription || '';
            const description = rawDesc.trim() || 'Módosítás';

            // 3. ÁR
            const price = h.servicePrice != null 
                ? `${Number(h.servicePrice).toLocaleString('hu-HU')} Ft`
                : '';

            return `
                <div class="border-bottom pb-2 mb-2">
                    <small class="text-muted d-block">${formattedDate}</small>
                    <div class="mt-1"><strong>${description}</strong></div>
                    ${price ? `<div class="text-muted small">${price}</div>` : ''}
                </div>
            `;
        }).join('');

        container.innerHTML = rows;
    })
    .catch(err => {
        error('History load failed', err);
        container.innerHTML = '<div class="text-danger">Hiba az előzmények betöltésekor.</div>';
    });
}

    // ---------------------------------------------------------------
    // SHOW HISTORY
    // ---------------------------------------------------------------
    function showHistory(resourceId) {
        log(`showHistory: ${resourceId}`);
        openViewModal(resourceId);
        setTimeout(() => {
            const tab = document.querySelector('[data-bs-target="#view-history"]');
            if (tab) new bootstrap.Tab(tab).show();
        }, 500);
    }

    // ---------------------------------------------------------------
    // OPEN DELETE MODAL
    // ---------------------------------------------------------------
    function openDeleteModal(resourceId) {
        log(`openDeleteModal: ${resourceId}`);
        const modalEl = document.getElementById('deleteResourceModal');
        if (!modalEl) return error('deleteResourceModal not found');

        const form = modalEl.querySelector('form');
        form.onsubmit = async (e) => {
            e.preventDefault();
            try {
                log(`Deactivating resource: ${resourceId}`);
                const res = await fetch(`${api}/${resourceId}/deactivate`, {
                    method: 'POST',
                    headers: { 'X-CSRF-TOKEN': csrf }
                });
                if (res.ok) {
                    toast('Eszköz deaktiválva!', 'success');
                    bootstrap.Modal.getInstance(modalEl).hide();
                    location.reload();
                } else {
                    const err = await res.json().catch(() => ({ message: 'Hiba a deaktiváláskor.' }));
                    error('Deactivate failed', err);
                    toast(err.message || 'Hiba történt.', 'danger');
                }
            } catch (err) {
                error('openDeleteModal exception', err);
                toast('Hiba történt.', 'danger');
            }
        };
        new bootstrap.Modal(modalEl).show();
    }

    // ---------------------------------------------------------------
    // OPEN EDIT MODAL – FINAL
    // ---------------------------------------------------------------
    async function openEditModal(resourceId) {
        log(`openEditModal: ${resourceId}`);
        const modalEl = document.getElementById('editResourceModal');
        if (!modalEl) return error('editResourceModal not found');

        const modal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
        const form = modalEl.querySelector('#editResourceForm');
        form.reset();
        document.getElementById('editId').value = resourceId;

        try {
            const res = await fetch(`${api}/${resourceId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            log('Edit data loaded', data);

            modalEl.addEventListener('shown.bs.modal', () => {
                log('edit modal shown – populating');

                // --- 1. ENABLE FIELDS ---
                form.querySelectorAll('input, select, textarea').forEach(el => {
                    el.disabled = false;
                    el.removeAttribute('readonly');
                });

                // --- 2. POPULATE TEXT FIELDS ---
                function capitalize(str) {
                    return str.charAt(0).toUpperCase() + str.slice(1);
                }

                Object.entries(data).forEach(([key, value]) => {
                    const input = form.querySelector(`[name="${key}"]`) || 
                                  form.querySelector(`[name="${capitalize(key)}"]`);
                    if (!input) {
                        if (key !== 'resourceId') log(`Input not found for key: ${key}`);
                        return;
                    }
                    if (input.tomselect) return;

                    if (input.type === 'date' && value) {
                        input.value = value.split('T')[0];
                    } else if (input.type === 'number' && value != null) {
                        input.value = value;
                    } else {
                        input.value = value ?? '';
                    }
                });

                // --- 3. INITIALIZE TOMSELECTS ---
                setTimeout(() => {
                    log('Initializing TomSelects');

                    // Destroy all
                    ['#editType', '#editStatus', '#editWhoBuy', '#editWhoLastServiced', 
                     '#editPartner', '#editSite', '#editContact', '#editEmployee'].forEach(sel => {
                        const el = document.querySelector(sel);
                        if (el?.tomselect) el.tomselect.destroy();
                    });

                    // Init all
                    populateSelect('#editType', '/api/resources/types', data.resourceTypeId);
                    populateSelect('#editStatus', '/api/resources/statuses', data.resourceStatusId);
                    populateSelect('#editWhoBuy', '/api/users/select', data.whoBuyId);
                    populateSelect('#editWhoLastServiced', '/api/users/select', data.whoLastServicedId);
                    populateSelect('#editEmployee', '/api/employee/tomselect', data.employeeId);
                    populateSelect('#editPartner', '/api/partners/select', data.partnerId);

                    // --- SITE & CONTACT: Load options + set value ---
                    if (data.partnerId) {
                        fetch(`/api/partners/${data.partnerId}/sites/select`)
                            .then(r => r.ok ? r.json() : Promise.reject())
                            .then(siteOptions => {
                                populateSelect('#editSite', () => null, null);
                                const siteSelect = document.querySelector('#editSite');
                                if (siteSelect?.tomselect) {
                                    siteSelect.tomselect.clear();
                                    siteSelect.tomselect.clearOptions();
                                    siteSelect.tomselect.addOptions(siteOptions);
                                    if (data.siteId) {
                                        siteSelect.tomselect.setValue(data.siteId);
                                        log(`Site TomSelect filled: ${data.siteId}`);
                                    }
                                }
                            })
                            .catch(() => populateSelect('#editSite', () => null, null));

                        fetch(`/api/partners/${data.partnerId}/contacts/select`)
                            .then(r => r.ok ? r.json() : Promise.reject())
                            .then(contactOptions => {
                                populateSelect('#editContact', () => null, null);
                                const contactSelect = document.querySelector('#editContact');
                                if (contactSelect?.tomselect) {
                                    contactSelect.tomselect.clear();
                                    contactSelect.tomselect.clearOptions();
                                    contactSelect.tomselect.addOptions(contactOptions);
                                    if (data.contactId) {
                                        contactSelect.tomselect.setValue(data.contactId);
                                        log(`Contact TomSelect filled: ${data.contactId}`);
                                    }
                                }
                            })
                            .catch(() => populateSelect('#editContact', () => null, null));
                    } else {
                        populateSelect('#editSite', () => null, null);
                        populateSelect('#editContact', () => null, null);
                    }

                    setupPartnerCascade(form);
                }, 200);

                // --- 4. SAVE BUTTON ---
                form.onsubmit = (e) => {
                    e.preventDefault();
                    update(e);
                };

                const saveBtn = form.querySelector('button[type="submit"]');
                if (saveBtn) {
                    saveBtn.disabled = false;
                    saveBtn.textContent = 'Mentés';
                }

                // --- 5. TAB FIX ---
                modalEl.querySelectorAll('.nav-link').forEach(tab => {
                    tab.addEventListener('click', e => e.stopPropagation());
                });

            }, { once: true });

            modal.show();
        } catch (err) {
            error('openEditModal failed', err);
            toast('Hiba az adatok betöltésekor.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // EVENT DELEGATION
    // ---------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        log('DOM loaded – setting up event delegation');
        const table = document.querySelector('table');
        if (table) {
            table.addEventListener('click', (e) => {
                const btn = e.target.closest('button, a');
                if (!btn) return;
                const resourceId = btn.dataset.resourceId;
                if (!resourceId) return;

                log(`Table button clicked: ${btn.className} (ID: ${resourceId})`);

                if (btn.classList.contains('btn-view-resource')) {
                    e.preventDefault();
                    openViewModal(resourceId);
                } else if (btn.classList.contains('btn-edit-resource')) {
                    e.preventDefault();
                    openEditModal(resourceId);
                } else if (btn.classList.contains('btn-show-history')) {
                    e.preventDefault();
                    showHistory(resourceId);
                } else if (btn.classList.contains('btn-delete-resource')) {
                    e.preventDefault();
                    openDeleteModal(resourceId);
                }
            });
        }

        const createForm = document.getElementById('createResourceForm');
        createForm?.addEventListener('submit', create);

        const createModal = document.getElementById('newResourceModal');
        if (createModal) {
            createModal.addEventListener('shown.bs.modal', () => {
                log('create modal shown – initializing selects');
                populateSelect('#createType', '/api/resources/types');
                populateSelect('#createStatus', '/api/resources/statuses');
                populateSelect('#createWhoBuy', '/api/users/select');
                populateSelect('#createWhoLastServiced', '/api/users/select');
                populateSelect('#createPartner', '/api/partners/select');
                populateSelect('#createEmployee', '/api/employee/tomselect');
                populateSelect('#createSite', () => '/api/partners/0/sites/select');
                populateSelect('#createContact', () => '/api/partners/0/contacts/select');
                setTimeout(() => setupPartnerCascade(createForm), 300);
            });
        }
    });

    return {
        create,
        update,
        openViewModal,
        showHistory,
        openDeleteModal,
        openEditModal
    };
})();