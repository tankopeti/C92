// /js/Doc/doc-create.js
// Dokumentum CREATE – Partner TomSelect (DEFAULT) + Site cascade + Status select FIX + CustomMetadata rows
// UPDATE (no page reload):
// - sikeres mentés után: documents:reload event (AJAX lista frissítés)
// - TomSelect value olvasása biztosítva

document.addEventListener('DOMContentLoaded', function () {
    console.log('doc-create.js BETÖLTÖDÖTT – Partner TomSelect DEFAULT + Site endpoint + Status FIX');

    const API = {
        documentTypes: '/api/documenttypes/select?search=',
        partners: '/api/partners/select?search=',
        sitesByPartner: (partnerId) => `/api/sites/by-partner/${partnerId}`,
        create: '/api/documents'
    };

    const modalEl = document.getElementById('createDocumentModal');
    const formEl = document.getElementById('createDocumentForm');
    if (!modalEl || !formEl) return;

    const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);

    const el = {
        fileName: document.getElementById('docFileName'),
        file: document.getElementById('docFile'),
        documentType: document.getElementById('docDocumentTypeId'),
        status: document.getElementById('docStatus'),
        partner: document.getElementById('docPartnerId'),
        site: document.getElementById('docSiteId'),
        saveBtn: document.getElementById('saveNewDocumentBtn'),
        errorBox: document.getElementById('createDocErrorBox'),
        errorText: document.getElementById('createDocErrorText'),

        // metadata UI
        addMetaBtn: document.getElementById('addDocMetaRowBtn'),
    };

    // csak a display miatt – VALUE = enum név
    const statusDisplayMap = window.documentStatusDisplayMap || {
        "Beérkezett": "Beérkezett",
        "Függőben": "Függőben",
        "Elfogadott": "Elfogadott",
        "Lezárt": "Lezárt",
        "Jóváhagyandó": "Jóváhagyandó"
    };

    let partnerTomSelect = null;

    // -------------------------
    // Helpers
    // -------------------------
    async function fetchJson(url) {
        const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
        if (!res.ok) {
            const t = await res.text().catch(() => '');
            console.error('fetchJson failed:', res.status, url, t);
            throw new Error(`HTTP ${res.status}`);
        }
        return res.json();
    }

    function setOptions(select, items, placeholder) {
        select.innerHTML = '';
        const ph = document.createElement('option');
        ph.value = '';
        ph.textContent = placeholder;
        select.appendChild(ph);

        (items || []).forEach(i => {
            const opt = document.createElement('option');
            opt.value = String(i.id ?? '');
            opt.textContent = String(i.text ?? '');
            select.appendChild(opt);
        });
    }

    function showError(msg) {
        if (!el.errorBox || !el.errorText) return;
        el.errorText.textContent = msg || 'Hiba történt.';
        el.errorBox.classList.remove('d-none');
    }

    function hideError() {
        el.errorBox?.classList.add('d-none');
    }

    function resetSites() {
        el.site.innerHTML = '<option value="">— Előbb válassz partnert —</option>';
        el.site.disabled = true;
    }

    function loadStatusOptions() {
        // VALUE = enum név, TEXT = display
        el.status.innerHTML = '<option value="">— Válassz státuszt —</option>';

        Object.entries(statusDisplayMap).forEach(([enumValue, display]) => {
            const opt = document.createElement('option');
            opt.value = enumValue;       // <-- BACKEND ENUM parse erre számít
            opt.textContent = display;   // <-- ezt látja a user
            el.status.appendChild(opt);
        });
    }

    const toIntOrNull = (v) => {
        const n = parseInt(v, 10);
        return Number.isFinite(n) ? n : null;
    };

    // TomSelect miatt: innen olvassuk a partnerId-t
    function getPartnerIdValue() {
        // TomSelect esetén ez a biztos
        if (partnerTomSelect) {
            const v = partnerTomSelect.getValue();
            return (v ?? '').toString();
        }
        return (el.partner.value ?? '').toString();
    }

    // -------------------------
    // Custom metadata UI
    // -------------------------
    function getMetaRowsContainer() {
        const firstRow = modalEl.querySelector('.doc-meta-row');
        return firstRow ? firstRow.parentElement : null;
    }

    function getMetaRows() {
        return Array.from(modalEl.querySelectorAll('.doc-meta-row'));
    }

    function renumberMetaRows() {
        const rows = getMetaRows();
        rows.forEach((row, idx) => {
            row.dataset.index = String(idx);

            const keyInput = row.querySelector('input[name$=".key"]') || row.querySelector('input[placeholder="Kulcs"]');
            const valueInput = row.querySelector('input[name$=".value"]') || row.querySelector('input[placeholder="Érték"]');

            if (keyInput) keyInput.name = `customMetadata[${idx}].key`;
            if (valueInput) valueInput.name = `customMetadata[${idx}].value`;

            const rmBtn = row.querySelector('[data-action="remove-meta"]');
            if (rmBtn) rmBtn.disabled = (rows.length === 1 && idx === 0);
        });
    }

    function createMetaRow() {
        const row = document.createElement('div');
        row.className = 'row g-2 align-items-center mb-2 doc-meta-row';
        row.innerHTML = `
            <div class="col-md-5">
                <input type="text" class="form-control" placeholder="Kulcs" />
            </div>
            <div class="col-md-6">
                <input type="text" class="form-control" placeholder="Érték" />
            </div>
            <div class="col-md-1 d-grid">
                <button type="button" class="btn btn-outline-danger btn-sm" data-action="remove-meta">&times;</button>
            </div>
        `;
        return row;
    }

    function addMetaRow() {
        const container = getMetaRowsContainer();
        if (!container) {
            console.warn('Meta rows container not found (no .doc-meta-row parent).');
            return;
        }
        container.appendChild(createMetaRow());
        renumberMetaRows();
    }

    function collectCustomMetadata() {
        const rows = getMetaRows();
        const entries = rows.map(row => {
            const keyInput = row.querySelector('input[name$=".key"]') || row.querySelector('input[placeholder="Kulcs"]');
            const valueInput = row.querySelector('input[name$=".value"]') || row.querySelector('input[placeholder="Érték"]');

            const key = (keyInput?.value || '').trim();
            const value = (valueInput?.value || '').trim();

            return { key, value };
        }).filter(x => !(x.key === '' && x.value === ''));

        return entries;
    }

    // -------------------------
    // Loaders
    // -------------------------
    async function loadLists() {
        hideError();

        const [types, partners] = await Promise.all([
            fetchJson(API.documentTypes),
            fetchJson(API.partners)
        ]);

        setOptions(el.documentType, types, '— Válassz —');
        setOptions(el.partner, partners, '— Válassz —');

        loadStatusOptions();
        resetSites();
    }

    async function loadSites(partnerId) {
        if (!partnerId) {
            resetSites();
            return;
        }

        const sites = await fetchJson(API.sitesByPartner(partnerId)); // [{id,text}]

        if (!Array.isArray(sites) || sites.length === 0) {
            el.site.innerHTML = '<option value="">— Nincs telephely ehhez a partnerhez —</option>';
            el.site.disabled = true;
            return;
        }

        el.site.disabled = false;
        setOptions(el.site, sites, '— Válassz telephelyet —');
    }

    // -------------------------
    // TomSelect (DEFAULT) - partner
    // -------------------------
    function initPartnerTomSelect() {
        if (typeof TomSelect === 'undefined') {
            console.warn('TomSelect nincs betöltve – natív select');
            return;
        }

        partnerTomSelect = new TomSelect(el.partner); // DEFAULT

        // TomSelect change esemény → site load
        partnerTomSelect.on('change', async (value) => {
            try {
                await loadSites(value);
            } catch (err) {
                console.error(err);
                showError('Telephelyek betöltése sikertelen.');
                resetSites();
            }
        });
    }

    function destroyPartnerTomSelect() {
        try { partnerTomSelect?.destroy(); } catch { }
        partnerTomSelect = null;
    }

    // -------------------------
    // Submit
    // -------------------------
    async function submitCreate() {
        hideError();

        if (!formEl.checkValidity()) {
            formEl.classList.add('was-validated');
            showError('Töltsd ki a kötelező mezőket.');
            return;
        }

        if (!el.file.files.length) {
            showError('Fájl kiválasztása kötelező.');
            return;
        }

        const fd = new FormData();
        fd.append('file', el.file.files[0]);

        // ✅ partnerId biztosan TomSelectből jön
        const partnerIdStr = getPartnerIdValue();

        const payload = {
            fileName: el.fileName.value.trim(),
            documentTypeId: toIntOrNull(el.documentType.value),
            partnerId: partnerIdStr ? toIntOrNull(partnerIdStr) : null,
            siteId: el.site.value ? toIntOrNull(el.site.value) : null,
            status: el.status.value,
            customMetadata: collectCustomMetadata()
        };

        console.log('CREATE payload:', payload);

        fd.append('payloadJson', JSON.stringify(payload));

        el.saveBtn.disabled = true;

        try {
            const res = await fetch(API.create, { method: 'POST', body: fd });
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error('Create failed:', res.status, text);
                throw new Error(`Create failed (HTTP ${res.status})`);
            }

            bsModal.hide();

            // ✅ oldal újratöltés nélkül frissítjük a listát
            window.dispatchEvent(new CustomEvent('documents:reload'));

        } catch (err) {
            console.error(err);
            showError('Mentés sikertelen.');
        } finally {
            el.saveBtn.disabled = false;
        }
    }

    // -------------------------
    // Events
    // -------------------------
    // mentés gomb (direkt handler, ne delegation – kevesebb dupla bind)
    el.saveBtn?.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        submitCreate();
    });

    // + Sor gomb
    if (el.addMetaBtn) {
        el.addMetaBtn.addEventListener('click', function (e) {
            e.preventDefault();
            addMetaRow();
        });
    } else {
        console.warn('addDocMetaRowBtn nem található a DOM-ban.');
    }

    // remove meta sor (event delegation)
    modalEl.addEventListener('click', function (e) {
        const rmBtn = e.target.closest('[data-action="remove-meta"]');
        if (!rmBtn) return;

        e.preventDefault();

        const row = rmBtn.closest('.doc-meta-row');
        if (!row) return;

        const rows = getMetaRows();
        if (rows.length === 1) return;

        row.remove();
        renumberMetaRows();
    });

    // fallback (ha nincs TomSelect)
    el.partner.addEventListener('change', () => loadSites(el.partner.value));

    modalEl.addEventListener('shown.bs.modal', async function () {
        destroyPartnerTomSelect();
        await loadLists();
        initPartnerTomSelect();

        // meta sorok indexelése (ha a modal többször nyílik)
        renumberMetaRows();

        // ha már van kiválasztva partner, töltsük hozzá a telephelyeket
        const pid = getPartnerIdValue();
        if (pid) await loadSites(pid);
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        destroyPartnerTomSelect();
        formEl.reset();
        resetSites();
        hideError();
        formEl.classList.remove('was-validated');

        // meta sorok reset: hagyjuk meg csak az első sort
        const rows = getMetaRows();
        rows.forEach((r, idx) => { if (idx > 0) r.remove(); });
        renumberMetaRows();
    });
});
