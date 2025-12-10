// wwwroot/js/tasks_nyugalom.js
// NYUGALOM Kft. – Csak telephely választás, minden telephely elérhető!
// 2025-ös verzió – gyors, kereshető, szép

window.TasksNyugalom = (function () {
    const CSRF = document.querySelector('meta[name="csrf-token"]')?.content ||
                 document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];

    const log = (msg, ...args) => console.log(`%c[NYUGALOM] ${msg}`, 'color: #9c27b0; font-weight: bold;', ...args);
    const error = (msg, ...args) => console.error(`%c[NYUGALOM] HIBA: ${msg}`, 'color: #f44336; font-weight: bold;', ...args);

    const toast = (message, type = 'info') => {
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
        new bootstrap.Toast(t, { delay: 5000 }).show();
    };

    // ------------------------------------------------------------
    // EGYSZERŰ TomSelect minden telephelyhez (kereshető, gyors)
    // ------------------------------------------------------------
function initSiteTomSelect() {
    const siteSelect = document.querySelector('[name="SiteId"]');
    if (!siteSelect) return;

    if (siteSelect.tomselect) siteSelect.tomselect.destroy();

    new TomSelect(siteSelect, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        placeholder: 'Keresés telephely név vagy ID szerint...',
        maxOptions: 500,
        load: function(query, callback) {
            const url = `/api/nyugalom/sites/all/select${query ? '?search=' + encodeURIComponent(query) : ''}`;
            fetch(url)
                .then(r => r.ok ? r.json() : Promise.reject())
                .then(data => callback(data))
                .catch(() => {
                    toast('Hiba a telephelyek betöltésekor!', 'danger');
                    callback([]);
                });
        },
        loadThrottle: 300,
        shouldLoad: () => true,
        render: {
            option: function(data, escape) {
                const parts = data.text.split(' – ');
                return `<div class="py-2 px-3">
                            <strong class="text-primary">${escape(parts[0])}</strong>
                            <span class="text-muted"> – </span>
                            ${escape(parts[1] || '')}
                        </div>`;
            },
            item: function(data, escape) {
                const parts = data.text.split(' – ');
                return `<div class="py-1 px-2">
                            <strong class="text-primary">${escape(parts[0])}</strong>
                            <span class="text-muted"> – </span>
                            ${escape(parts[1] || '')}
                        </div>`;
            }
        }
    });

    log("Telephely TomSelect betöltve – ID látható!");
}


    // ------------------------------------------------------------
    // Modal megnyitáskor inicializálás
    // ------------------------------------------------------------
    function initNyugalomCreateModal() {
        const modalEl = document.getElementById('newTaskModal');
        if (!modalEl) return;

        modalEl.addEventListener('shown.bs.modal', () => {
            log('Nyugalom modal megnyitva – inicializálás');

            setTimeout(() => {
                // Csak a Nyugalom státuszok
                const statusSelect = document.querySelector('[name="TaskStatusPMId"]');
                if (statusSelect && !statusSelect.tomselect) {
                    fetch('/api/tasks/taskstatuses/nyugalombejelentes')
                        .then(r => r.json())
                        .then(data => {
                            new TomSelect(statusSelect, {
                                options: data,
                                valueField: 'id',
                                labelField: 'text',
                                placeholder: 'Válasszon státuszt...',
                            }).setValue(1002); // pl. "Folyamatban"
                        });
                }

                // Prioritás, Felelős, stb. (ha kellenek)
                // populateSelect('[name="TaskPriorityPMId"]', '/api/tasks/taskpriorities/select', 2);
                // populateSelect('[name="AssignedToId"]', '/api/users/select');

                // A LÉNYEG: minden telephely betöltése
                initSiteTomSelect();

            }, 300);
        });
    }

    // ------------------------------------------------------------
    // Indítás
    // ------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        log('tasks_nyugalom.js betöltve');
        initNyugalomCreateModal();
    });

    return {
        init: initNyugalomCreateModal,
        initSiteTomSelect,
        toast
    };
})();