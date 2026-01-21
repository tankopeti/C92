// /js/Doc/editDocument.js
// Dokumentum EDIT – TomSelect stabil scrollable Bootstrap modalban
// FIX:
// - dropdownParent: document.body
// - .ts-dropdown { position: fixed } (CSS-ben)
// - kézi dropdown pozicionálás (getBoundingClientRect)
// - bootstrap modal focus trap kikapcsolás: { focus:false }
//
// UPDATE (no page reload):
// - sikeres mentés után: window.dispatchEvent('documents:reload') a lista frissítéséhez

document.addEventListener('DOMContentLoaded', function () {
  console.log('editDocument.js BETÖLTÖDÖTT – STABIL MODAL TOMSELECT + METADATA');

  const API = {
    get: (id) => `/api/documents/${id}`,
    update: (id) => `/api/documents/${id}`,
    documentTypes: '/api/documenttypes/select?search=',
    partners: '/api/partners/select?search=',
    sitesByPartner: (id) => `/api/sites/by-partner/${id}?search=`
  };

  const modalEl = document.getElementById('editDocumentModal');
  const formEl = document.getElementById('editDocumentForm');
  if (!modalEl || !formEl) return;

  // 🔴 fontos: focus trap OFF, hogy ne rángassa a scrollt / ne zárja a dropdownot
  const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl, { focus: false });

  let currentId = null;

  // TomSelect instances
  let partnerTom = null;
  let siteTom = null;

  // Ha a site-ot is TomSelectből akarod: true
  const USE_SITE_TOMSELECT = false;

  const el = {
    id: document.getElementById('editDocId'),
    fileName: document.getElementById('editDocFileName'),
    documentType: document.getElementById('editDocDocumentTypeId'),
    status: document.getElementById('editDocStatus'),
    partner: document.getElementById('editDocPartnerId'),
    site: document.getElementById('editDocSiteId'),
    saveBtn: document.getElementById('saveEditDocumentBtn'),
    errorBox: document.getElementById('editDocErrorBox'),
    errorText: document.getElementById('editDocErrorText'),

    // metadata UI
    addMetaBtn: document.getElementById('addEditDocMetaRowBtn'),
    metaRowsContainer: document.getElementById('editDocMetaRows')
  };

  // ---- DEBUG export (DevToolsból) ----
  window.__editDoc = window.__editDoc || {};
  window.__editDoc.getPartnerTom = () => partnerTom;
  window.__editDoc.getSiteTom = () => siteTom;

  const statusDisplayMap = window.documentStatusDisplayMap || {
    "Beérkezett": "Beérkezett",
    "Függőben": "Függőben",
    "Elfogadott": "Elfogadott",
    "Lezárt": "Lezárt",
    "Jóváhagyandó": "Jóváhagyandó"
  };

  // -------------------------
  // Helpers
  // -------------------------
  function showError(msg) {
    if (el.errorText) el.errorText.textContent = msg || 'Hiba történt.';
    el.errorBox?.classList.remove('d-none');
  }

  function hideError() {
    el.errorBox?.classList.add('d-none');
  }

  async function fetchJson(url) {
    const r = await fetch(url, { headers: { 'Accept': 'application/json' } });
    if (!r.ok) {
      const t = await r.text().catch(() => '');
      console.error('fetchJson failed:', r.status, url, t);
      throw new Error(`HTTP ${r.status}`);
    }
    return r.json();
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

  function toIntOrNull(v) {
    const n = parseInt(v, 10);
    return Number.isFinite(n) ? n : null;
  }

  function resetSitesUI() {
    el.site.innerHTML = '<option value="">— Előbb válassz partnert —</option>';
    el.site.disabled = true;

    if (siteTom) {
      siteTom.clear(true);
      siteTom.clearOptions();
      siteTom.addOption({ value: "", text: "— Előbb válassz partnert —" });
      siteTom.setValue("", true);
      siteTom.disable();
    }
  }

  function loadStatusOptions() {
    el.status.innerHTML = '<option value="">— Válassz —</option>';
    Object.entries(statusDisplayMap).forEach(([enumValue, display]) => {
      const opt = document.createElement('option');
      opt.value = enumValue;
      opt.textContent = display;
      el.status.appendChild(opt);
    });
  }

  // -------------------------
  // TomSelect positioning (kritikus)
  // -------------------------
  function forceDropdownPosition(tom) {
    if (!tom) return;

    const dd = tom.dropdown;
    const ctrl = tom.control;
    if (!dd || !ctrl) return;

    const place = () => {
      if (!tom.isOpen) return;
      const r = ctrl.getBoundingClientRect();
      dd.style.left = `${r.left}px`;
      dd.style.top = `${r.bottom}px`;
      dd.style.width = `${r.width}px`;
    };

    tom.on('dropdown_open', () => {
      // fókusz, de ne scrollozzon
      try { tom.control_input?.focus({ preventScroll: true }); } catch {}

      place();
      requestAnimationFrame(place);
      setTimeout(place, 0);
    });

    // scroll/resize közben is tartsuk alatta
    window.addEventListener('scroll', place, true);
    window.addEventListener('resize', place);

    // modal-body scroll (scrollable modal)
    const modalBody = modalEl.querySelector('.modal-body');
    modalBody?.addEventListener('scroll', place, { passive: true });
  }

  // -------------------------
  // TomSelect init/destroy
  // -------------------------
  function destroyTomSelects() {
    try { partnerTom?.destroy(); } catch { }
    try { siteTom?.destroy(); } catch { }
    partnerTom = null;
    siteTom = null;
  }

  function initPartnerTomSelect() {
    if (typeof TomSelect === 'undefined') {
      console.warn('TomSelect nincs betöltve – partner marad natív');
      return;
    }

    partnerTom = new TomSelect(el.partner, {
      dropdownParent: document.body, // 🔴 stabil + fixed dropdown
      openOnFocus: true,
      closeAfterSelect: true
    });

    forceDropdownPosition(partnerTom);

    partnerTom.on('change', async (value) => {
      try {
        await loadSites(value, null);
      } catch (err) {
        console.error(err);
        resetSitesUI();
        showError('Telephelyek betöltése sikertelen.');
      }
    });
  }

  function initSiteTomSelectIfNeeded() {
    if (!USE_SITE_TOMSELECT) return;
    if (typeof TomSelect === 'undefined') return;

    siteTom = new TomSelect(el.site, {
      dropdownParent: document.body,
      openOnFocus: true,
      closeAfterSelect: true
    });

    forceDropdownPosition(siteTom);
    siteTom.disable();
  }

  // -------------------------
  // Metadata (EDIT)
  // -------------------------
  function ensureMetaUiExists() {
    if (!el.metaRowsContainer) {
      console.warn('EDIT metadata container missing: #editDocMetaRows');
      return false;
    }
    return true;
  }

  function escapeAttr(s) {
    return String(s ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;');
  }

  function renumberMetaRows() {
    if (!el.metaRowsContainer) return;

    const rows = Array.from(el.metaRowsContainer.querySelectorAll('.doc-meta-row'));
    rows.forEach((row, idx) => {
      row.dataset.index = String(idx);

      const keyInput = row.querySelector('.metadata-key');
      const valueInput = row.querySelector('.metadata-value');

      if (keyInput) keyInput.name = `customMetadata[${idx}].key`;
      if (valueInput) valueInput.name = `customMetadata[${idx}].value`;

      const rmBtn = row.querySelector('[data-action="remove-meta"]');
      if (rmBtn) rmBtn.disabled = (rows.length === 1);
    });
  }

  function createMetaRow(key = '', value = '') {
    const row = document.createElement('div');
    row.className = 'row g-2 align-items-center mb-2 doc-meta-row';
    row.innerHTML = `
      <div class="col-md-5">
        <input type="text" class="form-control metadata-key" placeholder="Kulcs" value="${escapeAttr(key)}" />
      </div>
      <div class="col-md-6">
        <input type="text" class="form-control metadata-value" placeholder="Érték" value="${escapeAttr(value)}" />
      </div>
      <div class="col-md-1 d-grid">
        <button type="button" class="btn btn-outline-danger btn-sm" data-action="remove-meta">&times;</button>
      </div>
    `;
    return row;
  }

  function resetMetaRowsToSingleEmpty() {
    if (!el.metaRowsContainer) return;
    el.metaRowsContainer.innerHTML = '';
    el.metaRowsContainer.appendChild(createMetaRow('', ''));
    renumberMetaRows();
  }

  function renderMetaRows(metadata) {
    if (!el.metaRowsContainer) return;

    const arr = Array.isArray(metadata) ? metadata : [];
    el.metaRowsContainer.innerHTML = '';

    if (arr.length === 0) {
      el.metaRowsContainer.appendChild(createMetaRow('', ''));
      renumberMetaRows();
      return;
    }

    arr.forEach(m => {
      el.metaRowsContainer.appendChild(
        createMetaRow(m.key ?? m.Key ?? '', m.value ?? m.Value ?? '')
      );
    });

    renumberMetaRows();
  }

  function collectMetaRows() {
    if (!el.metaRowsContainer) return [];

    const rows = Array.from(el.metaRowsContainer.querySelectorAll('.doc-meta-row'));
    return rows
      .map(r => {
        const key = (r.querySelector('.metadata-key')?.value || '').trim();
        const value = (r.querySelector('.metadata-value')?.value || '').trim();
        return { key, value };
      })
      .filter(x => !(x.key === '' && x.value === ''));
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
    resetSitesUI();
  }

  async function loadSites(partnerId, selected) {
    if (!partnerId) {
      resetSitesUI();
      return;
    }

    const sites = await fetchJson(API.sitesByPartner(partnerId));

    // natív
    el.site.disabled = false;
    setOptions(el.site, sites, '— Válassz telephelyet —');
    if (selected) el.site.value = String(selected);

    // opcionális: site TomSelect
    if (USE_SITE_TOMSELECT && siteTom) {
      siteTom.enable();
      siteTom.clearOptions();
      sites.forEach(s => siteTom.addOption({ value: String(s.id), text: String(s.text) }));
      siteTom.refreshOptions(false);

      if (selected) siteTom.setValue(String(selected), true);
      else siteTom.clear(true);
    }
  }

  // -------------------------
  // Open & Save
  // -------------------------
  async function openEdit(id) {
    currentId = id;
    hideError();

    // 1) natív listák betöltése
    await loadLists();

    // 2) doc betöltése
    const doc = await fetchJson(API.get(id));

    // 3) form mezők
    el.id.value = doc.documentId;
    el.fileName.value = doc.fileName || '';
    el.documentType.value = doc.documentTypeId || '';
    el.status.value = doc.status || '';

    const partnerId = doc.partnerId ? String(doc.partnerId) : '';
    const siteId = doc.siteId ? String(doc.siteId) : '';

    // partner natív value (TomSelect majd shown után)
    el.partner.value = partnerId;

    // metadata
    if (ensureMetaUiExists()) {
      const meta = doc.customMetadata ?? doc.CustomMetadata ?? doc.documentMetadata ?? doc.DocumentMetadata ?? [];
      renderMetaRows(meta);
    }

    // modal show
    bsModal.show();

    // TomSelect init csak akkor, amikor a modal már tényleg látszik
    modalEl.dataset.pendingPartnerId = partnerId;
    modalEl.dataset.pendingSiteId = siteId;
  }

  // modal shown: init TS + set values + site betöltés
  modalEl.addEventListener('shown.bs.modal', async () => {
    destroyTomSelects();

    initPartnerTomSelect();
    initSiteTomSelectIfNeeded();

    const partnerId = modalEl.dataset.pendingPartnerId || '';
    const siteId = modalEl.dataset.pendingSiteId || '';

    if (partnerTom) partnerTom.setValue(partnerId, true);
    else el.partner.value = partnerId;

    await loadSites(partnerId, siteId);

    delete modalEl.dataset.pendingPartnerId;
    delete modalEl.dataset.pendingSiteId;
  });

  async function saveEdit() {
    hideError();

    if (!formEl.checkValidity()) {
      formEl.classList.add('was-validated');
      showError('Hibás adatok.');
      return;
    }

    const partnerVal = partnerTom ? partnerTom.getValue() : el.partner.value;
    const siteVal = (USE_SITE_TOMSELECT && siteTom) ? siteTom.getValue() : el.site.value;

    const payload = {
      documentId: currentId,
      fileName: el.fileName.value.trim(),
      documentTypeId: toIntOrNull(el.documentType.value),
      partnerId: partnerVal ? toIntOrNull(partnerVal) : null,
      siteId: siteVal ? toIntOrNull(siteVal) : null,
      status: el.status.value,
      customMetadata: ensureMetaUiExists() ? collectMetaRows() : []
    };

    console.log('EDIT payload:', payload);

    el.saveBtn.disabled = true;

    try {
      const r = await fetch(API.update(currentId), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (!r.ok) {
        const t = await r.text().catch(() => '');
        console.error('Update failed:', r.status, t);
        throw new Error(`Update failed HTTP ${r.status}`);
      }

      bsModal.hide();

      // ✅ oldal újratöltés nélkül frissítjük a listát (docLoadMore.js figyeli)
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
  document.addEventListener('click', (e) => {
    const editBtn = e.target.closest('.edit-document-btn');
    if (editBtn) {
      e.preventDefault();
      openEdit(editBtn.dataset.documentId);
    }

    if (e.target && e.target.id === 'saveEditDocumentBtn') {
      e.preventDefault();
      saveEdit();
    }
  });

  // natív partner -> site fallback (ha TS nincs)
  el.partner.addEventListener('change', () => loadSites(el.partner.value, null));

  // + Sor meta
  if (el.addMetaBtn) {
    el.addMetaBtn.addEventListener('click', (e) => {
      e.preventDefault();
      if (!el.metaRowsContainer) return;
      el.metaRowsContainer.appendChild(createMetaRow('', ''));
      renumberMetaRows();
    });
  }

  // remove meta (delegation)
  modalEl.addEventListener('click', (e) => {
    const rm = e.target.closest('[data-action="remove-meta"]');
    if (!rm || !el.metaRowsContainer) return;

    e.preventDefault();

    const row = rm.closest('.doc-meta-row');
    if (!row) return;

    const rows = Array.from(el.metaRowsContainer.querySelectorAll('.doc-meta-row'));
    if (rows.length === 1) return;

    row.remove();
    renumberMetaRows();
  });

  // cleanup
  modalEl.addEventListener('hidden.bs.modal', () => {
    currentId = null;
    formEl.reset();
    formEl.classList.remove('was-validated');
    hideError();

    resetMetaRowsToSingleEmpty();
    destroyTomSelects();
    resetSitesUI();
  });
});
