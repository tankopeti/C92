// // wwwroot/js/Tasks.js
// // Tasks – LIST + VIEW / EDIT / DELETE
// // ✅ TomSelect marad EDIT-ben
// // ❌ CREATE LOGIKA NINCS ITT (se submit handler, se create modal init)

// window.Tasks = (function () {
//   'use strict';

//   const api = '/api/tasks';
//   const csrf =
//     document.querySelector('meta[name="csrf-token"]')?.content ||
//     document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];

//   const dropdownCacheStatic = {};

//   // Partner-függő selectek az EDIT modalban
//   // Megjegyzés: ezek az endpointok NEM q-t várnak, ezért a load nem fűz ?q= paramot!
//   const dependentSelects = {
//     PartnerId: [
//       {
//         target: 'SiteId',
//         url: (partnerId) => `/api/partners/${partnerId}/sites/select`,
//         queryParam: null
//       },
//       {
//         target: 'ContactId',
//         url: (partnerId) => `/api/partners/${partnerId}/contacts/select`,
//         queryParam: null
//       },
//       {
//         target: 'QuoteId',
//         url: (partnerId) => `/api/quotes/select?partnerId=${partnerId}`,
//         queryParam: null
//       },
//       {
//         target: 'OrderId',
//         url: (partnerId) => `/api/Orders/?partnerId=${partnerId}`,
//         queryParam: null
//       },
//       {
//         target: 'CustomerCommunicationId',
//         url: (partnerId) => `/api/customercommunication/select?partnerId=${partnerId}`,
//         queryParam: null
//       }
//     ]
//   };

//   // ---------------------------------------------------------------
//   // LOG
//   // ---------------------------------------------------------------
//   function log(message, ...args) {
//     console.log(`%c[Tasks] ${message}`, 'color: #28a745; font-weight: bold;', ...args);
//   }
//   function error(message, ...args) {
//     console.error(`%c[Tasks] ${message}`, 'color: #dc3545; font-weight: bold;', ...args);
//   }

//   // ---------------------------------------------------------------
//   // TOAST
//   // ---------------------------------------------------------------
//   function toast(message, type = 'info') {
//     const container = document.getElementById('toastContainer') || (() => {
//       const c = document.createElement('div');
//       c.id = 'toastContainer';
//       c.className = 'position-fixed bottom-0 end-0 p-3';
//       c.style.zIndex = '1100';
//       document.body.appendChild(c);
//       return c;
//     })();

//     const t = document.createElement('div');
//     t.className = `toast align-items-center text-white bg-${type} border-0`;
//     t.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
//     container.appendChild(t);
//     new bootstrap.Toast(t, { delay: 4000 }).show();
//   }

//   // ---------------------------------------------------------------
//   // SHOW ERRORS
//   // ---------------------------------------------------------------
//   function showErrors(form, errors) {
//     log('showErrors', errors);
//     form.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
//     Object.keys(errors).forEach(key => {
//       const input = form.querySelector(`[name="${key}"]`);
//       if (input) {
//         const err = input.closest('.mb-3')?.querySelector('.text-danger');
//         if (err) err.textContent = Array.isArray(errors[key]) ? errors[key].join(', ') : errors[key];
//       }
//     });
//   }

//   // ---------------------------------------------------------------
//   // POPULATE SELECT (TomSelect) - static + dynamic (EDIT-hez)
//   // ---------------------------------------------------------------
//   function populateSelect(selector, urlOrFn, selectedId = null) {
//     log(`populateSelect: ${selector}`, { urlOrFn, selectedId });

//     const select = document.querySelector(selector);
//     if (!select) return error(`Select not found: ${selector}`);

//     // Destroy existing instance if present
//     if (select.tomselect) {
//       try { select.tomselect.destroy(); } catch {}
//       select.tomselect = null;
//     }

//     const isMultiple = select.hasAttribute('multiple');
//     const valueField = 'id';
//     const labelField = 'text';

//     const config = {
//       valueField,
//       labelField,
//       searchField: [labelField],
//       placeholder: 'Válasszon...',
//       allowEmptyOption: true,
//       openOnFocus: true,
//       sortField: { field: labelField, direction: 'asc' },
//       maxOptions: 100,
//       plugins: isMultiple ? ['remove_button'] : []
//     };

//     function setTomSelectValue(ts, value) {
//       if (value === null || value === undefined) return;
//       const finalValue = isMultiple && !Array.isArray(value) ? [value] : value;
//       setTimeout(() => {
//         ts.setValue(finalValue);
//         log(`TomSelect value set: ${selector}`, finalValue);
//       }, 10);
//     }

//     function initWithData(data, valueToSet) {
//       config.options = Array.isArray(data) ? data : [];
//       const ts = new TomSelect(selector, config);
//       setTomSelectValue(ts, valueToSet);
//       log(`TomSelect initialized: ${selector}`, { optionsCount: config.options.length });
//       return ts;
//     }

//     // --- STATIC ---
//     if (typeof urlOrFn === 'string') {
//       const cacheKey = urlOrFn;

//       if (!dropdownCacheStatic[cacheKey]) {
//         log(`Fetching static data: ${urlOrFn}`);
//         fetch(urlOrFn)
//           .then(r => r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`)))
//           .then(data => {
//             dropdownCacheStatic[cacheKey] = data;
//             initWithData(data, selectedId);
//           })
//           .catch(err => {
//             error(`Failed to fetch ${urlOrFn}`, err);
//             dropdownCacheStatic[cacheKey] = [];
//             initWithData([], selectedId);
//           });
//       } else {
//         initWithData(dropdownCacheStatic[cacheKey], selectedId);
//       }
//       return;
//     }

//     // --- DYNAMIC ---
//     if (typeof urlOrFn === 'function') {
//       config.load = function (query, callback) {
//         let base = urlOrFn();
//         if (!base) return callback([]);

//         // FONTOS: itt alapból nem fűzünk q-t, mert a dependent endpointok többsége nem így keres.
//         // Ha egyszer mégis kell keresés, külön paraméterezd a dep.queryParam-mal.
//         const url = base;

//         log(`Dynamic load: ${url}`);
//         fetch(url)
//           .then(res => res.ok ? res.json() : Promise.reject(new Error(`HTTP ${res.status}`)))
//           .then(json => {
//             const items = Array.isArray(json) ? json : (json.items || json.results || []);
//             callback(items);
//           })
//           .catch(e => {
//             error('TomSelect load error:', e);
//             callback([]);
//           });
//       };
//     }

//     const ts = new TomSelect(selector, config);

//     if (selectedId !== null && selectedId !== undefined) {
//       setTomSelectValue(ts, selectedId);
//     }

//     // click -> open + load
//     ts.control.addEventListener('click', () => {
//       if (!ts.isOpen) {
//         ts.load('');
//         ts.open();
//       }
//     });

//     return ts;
//   }

//   // ---------------------------------------------------------------
//   // CASCADE PARTNER → SITE + CONTACT (EDIT MODAL)
//   // ---------------------------------------------------------------
//   function setupPartnerCascade(form, data = {}) {
//     const partnerSelect = form.querySelector('[name="PartnerId"]');
//     if (!partnerSelect) return;

//     const checkTs = setInterval(() => {
//       if (!partnerSelect.tomselect) return;

//       clearInterval(checkTs);
//       const ts = partnerSelect.tomselect;

//       ts.off('change');

//       ts.on('change', () => {
//         const partnerId = ts.getValue();
//         log(`Partner changed: ${partnerId}`);

//         (dependentSelects.PartnerId || []).forEach(dep => {
//           const targetSelect = form.querySelector(`[name="${dep.target}"]`);
//           if (!targetSelect) return;

//           if (targetSelect.tomselect) {
//             try {
//               targetSelect.tomselect.clear(true);
//               targetSelect.tomselect.clearOptions();
//               targetSelect.tomselect.destroy();
//             } catch {}
//           }

//           const selectedId = data[dep.target.toLowerCase()] || null;

//           // url fn: NO params beyond partnerId
//           const urlFn = partnerId ? () => dep.url(partnerId) : () => null;

//           populateSelect(`#${targetSelect.id}`, urlFn, selectedId);
//         });
//       });

//       if (ts.getValue()) ts.trigger('change');
//     }, 100);
//   }

//   // ---------------------------------------------------------------
//   // UPDATE (EDIT SUBMIT)
//   // ---------------------------------------------------------------
//   async function update(e) {
//     e.preventDefault();
//     log('update: started');

//     const form = e.target;
//     const formData = new FormData(form);
//     const data = {};

//     for (const [key, value] of formData.entries()) {
//       if (key === '__RequestVerificationToken') continue;

//       if (value === '') {
//         data[key] = null;
//       } else if (key === 'DueDate') {
//         const d = new Date(value);
//         data[key] = isNaN(d) ? null : d.toISOString();
//       } else if (!isNaN(value) && [
//         'Id', 'EstimatedHours', 'ActualHours', 'TaskTypePMId', 'TaskStatusPMId', 'TaskPriorityPMId',
//         'PartnerId', 'SiteId', 'ContactId', 'QuoteId', 'OrderId', 'CustomerCommunicationId', 'ScheduledDate'
//       ].includes(key)) {
//         data[key] = key.includes('Hours') ? parseFloat(value) : parseInt(value, 10);
//       } else if (key === 'ResourceIds' || key === 'EmployeeIds') {
//         data[key] = formData.getAll(key).map(v => parseInt(v, 10));
//       } else {
//         data[key] = value;
//       }
//     }

//     const taskId = document.getElementById('editTaskId')?.value || data.Id;
//     if (!taskId) return error('Task ID missing for update');

//     try {
//       log('update: sending', { id: taskId, data });
//       const res = await fetch(`${api}/${taskId}`, {
//         method: 'PUT',
//         headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
//         body: JSON.stringify(data)
//       });

//       if (res.ok) {
//         toast('Frissítve!', 'success');
//         bootstrap.Modal.getInstance(form.closest('.modal')).hide();
//         location.reload();
//       } else {
//         const err = await res.json().catch(() => ({ errors: { General: ['Hiba a frissítéskor.'] } }));
//         showErrors(form, err.errors || err);
//         error(`update: failed ${res.status}`, err);
//       }
//     } catch (err) {
//       error('update: exception', err);
//       toast('Hiba történt a frissítéskor.', 'danger');
//     }
//   }

//   // ---------------------------------------------------------------
//   // VIEW MODAL
//   // ---------------------------------------------------------------
//   async function openViewModal(taskId) {
//     log(`openViewModal: ${taskId}`);
//     const modalEl = document.getElementById('taskViewModal');
//     if (!modalEl) return error('taskViewModal not found');

//     const modal = new bootstrap.Modal(modalEl, { backdrop: 'static' });
//     const body = document.getElementById('taskModalBody');

//     try {
//       const res = await fetch(`${api}/${taskId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
//       if (!res.ok) throw new Error(`HTTP ${res.status}`);

//       const data = await res.json();

//       body.innerHTML = `
//         <div class="row">
//           <div class="col-md-6">
//             <p><strong>Cím:</strong> ${data.title}</p>
//             <p><strong>Leírás:</strong> ${data.description || '<em>Nincs</em>'}</p>
//             <p><strong>Típus:</strong> ${data.taskTypePMName || '-'}</p>
//             <p><strong>Státusz:</strong> <span class="badge bg-primary">${data.taskStatusPMName}</span></p>
//             <p><strong>Prioritás:</strong> <span class="badge bg-secondary">${data.taskPriorityPMName}</span></p>
//           </div>
//           <div class="col-md-6">
//             <p><strong>Partner:</strong> ${data.partnerName || '-'}</p>
//             <p><strong>Helyszín:</strong> ${data.siteName || '-'}</p>
//             <p><strong>Kapcsolattartó:</strong> ${data.contactName || '-'}</p>
//           </div>
//         </div>
//       `;

//       document.getElementById('editTaskBtn').style.display = 'inline-block';
//       document.getElementById('editTaskBtn').onclick = () => openEditModal(taskId);

//       modal.show();
//     } catch (err) {
//       error('openViewModal failed', err);
//       toast('Hiba az adatok betöltésekor.', 'danger');
//     }
//   }

//   // ---------------------------------------------------------------
//   // EDIT MODAL
//   // ---------------------------------------------------------------
//   async function openEditModal(taskId) {
//     log(`openEditModal: ${taskId}`);
//     const modalEl = document.getElementById('taskViewModal');
//     if (!modalEl) return error('taskViewModal not found');

//     const modal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
//     const body = document.getElementById('taskModalBody');

//     try {
//       const res = await fetch(`${api}/${taskId}`, { headers: { 'X-CSRF-TOKEN': csrf } });
//       if (!res.ok) throw new Error(`HTTP ${res.status}`);

//       const data = await res.json();

//       body.innerHTML = `
//         <form id="editTaskForm">
//           <input type="hidden" id="editTaskId" value="${taskId}" />
//           ${document.querySelector('#newTaskModal .modal-body')?.innerHTML || ''}
//         </form>
//       `;

//       const form = body.querySelector('#editTaskForm');

//       const setValue = (name, value) => {
//         const el = form.querySelector(`[name="${name}"]`);
//         if (el) el.value = value ?? '';
//       };

//       setValue('Title', data.title);
//       setValue('Description', data.description);

//       modalEl.addEventListener('shown.bs.modal', () => {
//         log('edit modal shown – initializing TomSelects');

//         // destroy (ha volt)
//         [
//           'TaskTypePMId', 'TaskStatusPMId', 'TaskPriorityPMId', 'AssignedToId', 'PartnerId',
//           'SiteId', 'ContactId', 'QuoteId', 'OrderId', 'CustomerCommunicationId'
//         ].forEach(name => {
//           const el = form.querySelector(`[name="${name}"]`);
//           if (el?.tomselect) { try { el.tomselect.destroy(); } catch {} }
//         });

//         // init
//         populateSelect('[name="TaskTypePMId"]', '/api/tasktypes/select', data.taskTypePMId);
//         populateSelect('[name="TaskStatusPMId"]', '/api/taskstatuses/select', data.taskStatusPMId);
//         populateSelect('[name="TaskPriorityPMId"]', '/api/taskpriorities/select', data.taskPriorityPMId);
//         populateSelect('[name="AssignedToId"]', '/api/users/select', data.assignedToId);

//         // Partner static lista (nem a tasks/partners/select!)
//         populateSelect('[name="PartnerId"]', '/api/partners/select', data.partnerId);

//         populateSelect('[name="QuoteId"]', '/api/quotes/select', data.quoteId);
//         populateSelect('[name="OrderId"]', '/api/orders', data.orderId);
//         populateSelect('[name="CustomerCommunicationId"]', '/api/customercommunication/select', data.customerCommunicationId);

//         setTimeout(() => {
//           setupPartnerCascade(form, { siteid: data.siteId, contactid: data.contactId });
//         }, 100);

//         form.onsubmit = update;
//       }, { once: true });

//       document.getElementById('editTaskBtn').style.display = 'none';
//       modal.show();
//     } catch (err) {
//       error('openEditModal failed', err);
//       toast('Hiba az adatok betöltésekor.', 'danger');
//     }
//   }

//   // ---------------------------------------------------------------
//   // DELETE MODAL
//   // ---------------------------------------------------------------
//   function openDeleteModal(taskId) {
//     log(`openDeleteModal: ${taskId}`);
//     const modalEl = document.getElementById('deleteTaskModal');
//     if (!modalEl) return error('deleteTaskModal not found');

//     document.getElementById('confirmDeleteBtn').onclick = async () => {
//       try {
//         const res = await fetch(`${api}/${taskId}`, {
//           method: 'DELETE',
//           headers: { 'X-CSRF-TOKEN': csrf }
//         });

//         if (!res.ok) throw new Error(`HTTP ${res.status}`);

//         toast('Törölve!', 'success');
//         bootstrap.Modal.getInstance(modalEl).hide();
//         location.reload();
//       } catch (err) {
//         error('delete failed', err);
//         toast('Hiba a törléskor.', 'danger');
//       }
//     };

//     new bootstrap.Modal(modalEl).show();
//   }

//   // ---------------------------------------------------------------
//   // EVENT DELEGATION (LISTA GOMBOK)
//   // ---------------------------------------------------------------
//   document.addEventListener('DOMContentLoaded', () => {
//     log('DOM loaded – setting up event delegation');

//     document.querySelector('table')?.addEventListener('click', (e) => {
//       const btn = e.target.closest('button, a');
//       if (!btn) return;

//       const taskId = btn.dataset.taskId;
//       if (!taskId) return;

//       if (btn.classList.contains('btn-view-task')) {
//         e.preventDefault();
//         openViewModal(taskId);
//       } else if (btn.classList.contains('btn-edit-task')) {
//         e.preventDefault();
//         openEditModal(taskId);
//       } else if (btn.classList.contains('btn-delete-task')) {
//         e.preventDefault();
//         openDeleteModal(taskId);
//       }
//     });
//   });

//   return {
//     openViewModal,
//     openEditModal,
//     openDeleteModal
//   };
// })();
