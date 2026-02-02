// /js/Partner/advancedFilter.js
document.addEventListener('DOMContentLoaded', function () {
  console.log('[advancedFilter] loaded');

  const modalEl = document.getElementById('advancedFilterModal');
  const form = document.getElementById('advancedFilterForm');
  const applyBtn = document.getElementById('applyFilterBtn');
  const clearBtn = document.getElementById('clearFilterBtn');

  /* ================== HELPERS ================== */

  const toNumOrNull = (v) => {
    const s = (v ?? '').toString().trim();
    if (!s) return null;
    const n = Number(s);
    return Number.isFinite(n) ? n : null;
  };

  async function fetchJson(url) {
    const res = await fetch(url, {
      credentials: 'same-origin',
      headers: { 'Accept': 'application/json' }
    });

    const contentType = res.headers.get('content-type') || '';
    const raw = await res.text().catch(() => '');

    if (!res.ok) {
      throw new Error(`HTTP ${res.status} ${raw.slice(0, 200)}`);
    }

    if (!contentType.includes('application/json')) {
      // tipikusan login redirect HTML
      throw new Error(`Non-JSON response: ${contentType} ${raw.slice(0, 200)}`);
    }

    return raw ? JSON.parse(raw) : [];
  }

  function fillSelect(selectEl, items, emptyLabel) {
    if (!selectEl) return;

    selectEl.innerHTML =
      `<option value="">${emptyLabel}</option>` +
      (Array.isArray(items)
        ? items.map(x =>
            `<option value="${Number(x.id)}">${(x.name ?? '').toString()}</option>`
          ).join('')
        : '');
  }

  /* ================== DROPDOWN LOADERS ================== */

  let statusesLoaded = false;
  let gfosLoaded = false;
  let partnerTypesLoaded = false;

  async function ensureDropdownsLoaded() {
    // mindig frissen kérjük le a DOM-ból
    const statusSelect = document.getElementById('filterStatus');
    const gfoSelect = document.getElementById('filterGfoId');
    const partnerTypeSelect = document.getElementById('filterPartnerTypeId');

    if (!statusSelect) console.warn('[advancedFilter] #filterStatus not found');
    if (!gfoSelect) console.warn('[advancedFilter] #filterGfoId not found');
    if (!partnerTypeSelect) console.warn('[advancedFilter] #filterPartnerTypeId not found');

    try {
      if (statusSelect && !statusesLoaded) {
        const statuses = await fetchJson('/api/Partners/statuses');
        console.log('[advancedFilter] statuses:', statuses);
        fillSelect(statusSelect, statuses, '-- Mindegy --');
        statusesLoaded = true;
      }
    } catch (e) {
      console.error('[advancedFilter] statuses load failed', e);
      window.c92?.showToast?.('error', 'Státusz lista nem tölthető be');
    }

    try {
      if (gfoSelect && !gfosLoaded) {
        const gfos = await fetchJson('/api/Partners/gfos');
        console.log('[advancedFilter] gfos:', gfos);
        fillSelect(gfoSelect, gfos, '-- Mindegy --');
        gfosLoaded = true;
      }
    } catch (e) {
      console.error('[advancedFilter] gfos load failed', e);
      window.c92?.showToast?.('error', 'GFO lista nem tölthető be');
    }

    try {
      if (partnerTypeSelect && !partnerTypesLoaded) {
        const types = await fetchJson('/api/Partners/partnerTypes');
        console.log('[advancedFilter] partnerTypes:', types);
        fillSelect(partnerTypeSelect, types, '-- Mindegy --');
        partnerTypesLoaded = true;
      }
    } catch (e) {
      console.error('[advancedFilter] partnerTypes load failed', e);
      window.c92?.showToast?.('error', 'Partner típus lista nem tölthető be');
    }
  }

  // 1️⃣ első betöltési kísérlet
  ensureDropdownsLoaded();

  // 2️⃣ biztos betöltés modal megnyitáskor
  if (modalEl) {
    modalEl.addEventListener('shown.bs.modal', function () {
      ensureDropdownsLoaded();
    });
  }

  /* ================== APPLY ================== */

  applyBtn?.addEventListener('click', function () {
    const statusSelect = document.getElementById('filterStatus');
    const gfoSelect = document.getElementById('filterGfoId');
    const partnerTypeSelect = document.getElementById('filterPartnerTypeId');

    const filters = {
      name: document.getElementById('filterName')?.value.trim() || '',
      taxId: document.getElementById('filterTaxId')?.value.trim() || '',

      partnerCode: document.getElementById('filterPartnerCode')?.value.trim() || '',
      ownId: document.getElementById('filterOwnId')?.value.trim() || '',

      statusId: statusSelect?.value || '',
      gfoId: toNumOrNull(gfoSelect?.value),
      partnerTypeId: toNumOrNull(partnerTypeSelect?.value),

      city: document.getElementById('filterCity')?.value.trim() || '',
      postalCode: document.getElementById('filterPostalCode')?.value.trim() || '',
      emailDomain: document.getElementById('filterEmailDomain')?.value.trim() || '',
      activeOnly: document.getElementById('filterActiveOnly')?.checked ?? true
    };

    console.log('[advancedFilter] apply filters:', filters);

    if (typeof loadPartners === 'function') {
      loadPartners(filters);
    }

    bootstrap.Modal.getInstance(modalEl)?.hide();
  });

  /* ================== CLEAR ================== */

  clearBtn?.addEventListener('click', function () {
    form?.reset();

    const statusSelect = document.getElementById('filterStatus');
    const gfoSelect = document.getElementById('filterGfoId');
    const partnerTypeSelect = document.getElementById('filterPartnerTypeId');

    if (statusSelect) statusSelect.value = '';
    if (gfoSelect) gfoSelect.value = '';
    if (partnerTypeSelect) partnerTypeSelect.value = '';

    console.log('[advancedFilter] cleared');

    if (typeof loadPartners === 'function') {
      loadPartners({});
    }
  });
});
