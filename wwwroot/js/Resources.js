// wwwroot/js/Resources.js
window.Resources = (function () {
    const api = '/api/resources';
    const csrf = document.querySelector('meta[name="csrf-token"]')?.content ||
                 document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];

    // ---------------------------------------------------------------
    // 1. CACHES & DEPENDENCIES
    // ---------------------------------------------------------------
    const dropdownCacheStatic = {};

    const dependentSelects = {
        PartnerId: [
            { target: 'SiteId',    url: (partnerId) => `/api/partners/${partnerId}/sites/select` },
            { target: 'ContactId', url: (partnerId) => `/api/partners/${partnerId}/contacts/select` }
        ]
    };

    // ---------------------------------------------------------------
    // 2. POPULATE SELECT - FIXED: value/label → id/text in load()
    // ---------------------------------------------------------------
    function populateSelect(selector, urlOrFn, selectedId = null) {
        const select = document.querySelector(selector);
        if (!select) return console.warn(`Select not found: ${selector}`);

        if (select.tomselect) select.tomselect.destroy();

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
            sortField: { field: labelField, direction: "asc" },
            plugins: ['remove_button'],
            maxOptions: 100
        };

        // Static data
        if (typeof urlOrFn === 'string') {
            const cacheKey = urlOrFn;
            if (!dropdownCacheStatic[cacheKey]) {
                fetch(urlOrFn)
                    .then(r => r.ok ? r.json() : Promise.reject())
                    .then(data => {
                        dropdownCacheStatic[cacheKey] = data;
                        initWithData(data);
                    })
                    .catch(() => {
                        dropdownCacheStatic[cacheKey] = [];
                        initWithData([]);
                    });
            } else {
                initWithData(dropdownCacheStatic[cacheKey]);
            }

function initWithData(data) {
    config.options = data;
    const ts = new TomSelect(selector, config);
    if (selectedId) ts.setValue(selectedId);
}

            return;
        }

        // Dynamic (function) - FIXED: Convert value/label → id/text
        if (typeof urlOrFn === 'function') {
            config.load = function (query, callback) {
                const url = urlOrFn(this.control) + (query.trim() ? `?q=${encodeURIComponent(query)}` : '');
                fetch(url)
                    .then(res => {
                        if (!res.ok) throw new Error(`HTTP ${res.status}`);
                        return res.json();
                    })
                    .then(json => {
                        const items = isEmployee 
                            ? json.map(x => ({ id: x.value, text: x.label }))
                            : Array.isArray(json) ? json : (json.items || json.results || []);
                        callback(items);
                    })
                    .catch(e => {
                        console.error('TomSelect load error:', e);
                        callback([]);
                    });
            };
        }

        const ts = new TomSelect(selector, config);

        // OPEN ON CLICK
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
    // 3. TOAST
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
    // 4. SHOW ERRORS
    // ---------------------------------------------------------------

    function showErrors(form, errors) {
        form.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
        Object.keys(errors).forEach(key => {
            const input = form.querySelector(`[name="${key}"]`);
            if (input) {
                const err = input.closest('.col-md-6, .col-12')?.querySelector('.text-danger');
                if (err) err.textContent = Array.isArray(errors[key]) ? errors[key].join(', ') : errors[key];
            }
        });
    }

    // ---------------------------------------------------------------
    // 5. SETUP PARTNER → SITE + CONTACT CASCADE
    // ---------------------------------------------------------------
    function setupPartnerCascade(form) {
        const partnerSelect = form.querySelector('[name="PartnerId"]');
        if (!partnerSelect) return;

        const checkTs = setInterval(() => {
            if (partnerSelect.tomselect) {
                clearInterval(checkTs);
                const ts = partnerSelect.tomselect;
                ts.off('change');

                ts.on('change', () => {
                    const partnerId = ts.getValue();

                    dependentSelects.PartnerId.forEach(dep => {
                        const targetSelect = form.querySelector(`[name="${dep.target}"]`);
                        if (targetSelect && targetSelect.tomselect) {
                            const tomselect = targetSelect.tomselect;
                            tomselect.destroy();

                            const urlFn = partnerId
                                ? () => `/api/partners/${partnerId}/${dep.target === 'SiteId' ? 'sites' : 'contacts'}/select`
                                : () => null;

                            populateSelect(`#${targetSelect.id}`, urlFn);
                        }
                    });
                });

                if (ts.getValue()) ts.trigger('change');
            }
        }, 50);
    }

    // ---------------------------------------------------------------
    // 6. CREATE
    // ---------------------------------------------------------------
    async function create(e) {
        e.preventDefault();
        const form = e.target;
        const data = Object.fromEntries(new FormData(form));
        try {
            const res = await fetch(api, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
                body: JSON.stringify(data)
            });
            const result = await res.json();
            if (res.ok) {
                toast('Létrehozva!', 'success');
                bootstrap.Modal.getInstance(form.closest('.modal')).hide();
                location.reload();
            } else {
                showErrors(form, result.errors || { General: [result.message] });
            }
        } catch (err) {
            console.error('Create error:', err);
            toast('Hiba.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // 7. UPDATE
    // ---------------------------------------------------------------
    async function update(e) {
        e.preventDefault();
        const form = e.target;
        const data = Object.fromEntries(new FormData(form));
        const id = data.ResourceId;
        try {
            const res = await fetch(`${api}/${id}`, {
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
            }
        } catch (err) {
            console.error('Update error:', err);
            toast('Hiba.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // 8. EDIT – LOAD FORM
    // ---------------------------------------------------------------
    function edit(id) {
        fetch(`${api}/${id}`)
            .then(r => r.ok ? r.json() : Promise.reject())
            .then(data => {
                const body = document.getElementById('editModalBody');
                body.innerHTML = `
                    <input type="hidden" name="ResourceId" value="${id}" />
                    <div class="row g-3">
                        <div class="col-md-6">
                            <div class="mb-3"><label>Név *</label><input name="Name" class="form-control" value="${data.Name || ''}" required /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Sorozatszám</label><input name="Serial" class="form-control" value="${data.Serial || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Ár</label><input name="Price" type="number" step="0.01" class="form-control" value="${data.Price || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Vétel dátuma</label><input name="DateOfPurchase" type="date" class="form-control" value="${data.DateOfPurchase?.split('T')[0] || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Következő szerviz</label><input name="NextService" type="date" class="form-control" value="${data.NextService?.split('T')[0] || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Szerviz dátuma</label><input name="ServiceDate" type="date" class="form-control" value="${data.ServiceDate?.split('T')[0] || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Garancia (hónap)</label><input name="WarrantyPeriod" type="number" class="form-control" value="${data.WarrantyPeriod || ''}" /><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Garancia lejárat</label><input name="WarrantyExpireDate" type="date" class="form-control" value="${data.WarrantyExpireDate?.split('T')[0] || ''}" /><span class="text-danger"></span></div>
                        </div>
                        <div class="col-md-6">
                            <div class="mb-3"><label>Típus</label><select name="ResourceTypeId" id="editType_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Státusz</label><select name="ResourceStatusId" id="editStatus_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Vásárló</label><select name="WhoBuyId" id="editWhoBuy_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Utolsó szervizelő</label><select name="WhoLastServicedId" id="editWhoLastServiced_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Partner</label><select name="PartnerId" id="editPartner_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Telephely</label><select name="SiteId" id="editSite_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Kapcsolattartó</label><select name="ContactId" id="editContact_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Munkatárs</label><select name="EmployeeId" id="editEmployee_${id}" class="form-control tomselect-dropdown"></select><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Megjegyzés 1</label><textarea name="Comment1" class="form-control" rows="2">${data.Comment1 || ''}</textarea><span class="text-danger"></span></div>
                            <div class="mb-3"><label>Megjegyzés 2</label><textarea name="Comment2" class="form-control" rows="2">${data.Comment2 || ''}</textarea><span class="text-danger"></span></div>
                        </div>
                    </div>
                `;

                // STATIC
                populateSelect(`#editType_${id}`, '/api/resources/types', data.ResourceTypeId);
                populateSelect(`#editStatus_${id}`, '/api/resources/statuses', data.ResourceStatusId);
                populateSelect(`#editWhoBuy_${id}`, '/api/users/select', data.WhoBuyId);
                populateSelect(`#editWhoLastServiced_${id}`, '/api/users/select', data.WhoLastServicedId);
                populateSelect(`#editPartner_${id}`, '/api/partners/select', data.PartnerId);
                populateSelect(`#editEmployee_${id}`, '/api/employee/tomselect', data.EmployeeId);

                // DEPENDENT
                populateSelect(`#editSite_${id}`, () => '/api/partners/0/sites/select');
                populateSelect(`#editContact_${id}`, () => '/api/partners/0/contacts/select');

                if (data.PartnerId) {
                    setTimeout(() => {
                        populateSelect(`#editSite_${id}`, () => `/api/partners/${data.PartnerId}/sites/select`, data.SiteId);
                        populateSelect(`#editContact_${id}`, () => `/api/partners/${data.PartnerId}/contacts/select`, data.ContactId);
                    }, 200);
                }

                const editForm = document.getElementById('editResourceForm');
                setTimeout(() => setupPartnerCascade(editForm), 300);

                new bootstrap.Modal(document.getElementById('editResourceModal')).show();
            })
            .catch(err => {
                console.error('Edit load error:', err);
                toast('Hiba.', 'danger');
            });
    }

    // ---------------------------------------------------------------
    // 9. SHOW HISTORY
    // ---------------------------------------------------------------
    function showHistory(id) {
        const container = document.getElementById('historyContainer_' + id);
        if (!container) return;
        container.innerHTML = '<p class="text-muted">Betöltés...</p>';
        fetch(`${api}/${id}/history`)
            .then(r => r.ok ? r.json() : Promise.reject())
            .then(data => {
                if (!data.length) return container.innerHTML = '<p class="text-muted">Nincs előzmény.</p>';
                const rows = data.map(h => `
                    <tr>
                        <td>${new Date(h.ModifiedDate).toLocaleString('hu-HU')}</td>
                        <td>${h.ModifiedByName || 'Ismeretlen'}</td>
                        <td>${h.ChangeDescription || '-'}</td>
                        <td>${h.ServicePrice ? h.ServicePrice.toFixed(2) + ' Ft' : '-'}</td>
                    </tr>
                `).join('');
                container.innerHTML = `<div class="table-responsive"><table class="table table-sm table-bordered"><thead class="table-light"><tr><th>Dátum</th><th>Felhasználó</th><th>Leírás</th><th>Szerviz ár</th></tr></thead><tbody>${rows}</tbody></table></div>`;
            })
            .catch(() => container.innerHTML = '<p class="text-danger">Hiba.</p>')
            .finally(() => {
                new bootstrap.Modal(document.getElementById('viewResourceModal_' + id)).show();
            });
    }

    // ---------------------------------------------------------------
    // 10. DEACTIVATE
    // ---------------------------------------------------------------
    function deactivate(form, id, name) {
        return confirm(`Biztosan deaktiválod a(z) "${name}" eszközt?`);
    }

    // ---------------------------------------------------------------
    // 11. INIT
    // ---------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        const createForm = document.getElementById('createResourceForm');
        createForm?.addEventListener('submit', create);

        document.querySelectorAll('[id^="editResourceForm_"]').forEach(f => f.addEventListener('submit', update));

        const createModal = document.getElementById('newResourceModal');
        if (createModal) {
            createModal.addEventListener('shown.bs.modal', () => {
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

    return { create, update, edit, showHistory, deactivate };
})();