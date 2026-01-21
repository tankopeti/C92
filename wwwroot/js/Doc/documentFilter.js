// /js/Doc/documentFilter.js
// Dokumentum szűrő modal – Partner TomSelect (remote) + Site cascade + Apply/Reset URL build + querystring preload

document.addEventListener('DOMContentLoaded', function () {
  console.log('documentFilter.js BETÖLTÖDÖTT – Dokumentum szűrő modal inicializálása');

  // --- Elems ---
  const form = document.getElementById('docFilterForm');

  const searchInput = document.getElementById('filterSearch');
  const statusSelect = document.getElementById('filterStatus');
  const includeInactiveCb = document.getElementById('filterIncludeInactive');

  const docTypeSelect = document.getElementById('filterDocumentTypeId');

  const partnerSelect = document.getElementById('filterPartnerId');
  const siteSelect = document.getElementById('filterSiteId');

  const dateFromInput = document.getElementById('filterDateFrom');
  const dateToInput = document.getElementById('filterDateTo');

  const sortBySelect = document.getElementById('filterSortBy');
  const sortDirSelect = document.getElementById('filterSortDir');

  const applyBtn = document.getElementById('filterApplyBtn');
  const resetBtn = document.getElementById('filterResetBtn');

  if (!form || !applyBtn || !resetBtn) {
    console.warn('docFilterForm / filterApplyBtn / filterResetBtn hiányzik');
    return;
  }

  if (!partnerSelect || !siteSelect) {
    console.warn('filterPartnerId vagy filterSiteId hiányzik (modal ID mismatch)');
    return;
  }

  if (typeof TomSelect === 'undefined') {
    console.error('TomSelect nincs betöltve ezen az oldalon! (TomSelect is undefined)');
    return;
  }

  // --- Querystring preload ---
  const qs = new URLSearchParams(window.location.search);

  function getParamAny(...keys) {
    for (const k of keys) {
      const v = qs.get(k);
      if (v !== null && v !== undefined && v !== '') return v;
    }
    return '';
  }

  // Preload sima mezők
  if (searchInput) searchInput.value = getParamAny('searchTerm', 'SearchTerm', 'search') || '';
  if (statusSelect) statusSelect.value = getParamAny('statusFilter', 'StatusFilter', 'status') || 'all';

  if (docTypeSelect) docTypeSelect.value = getParamAny('documentTypeId', 'DocumentTypeId') || '';
  if (dateFromInput) dateFromInput.value = getParamAny('dateFrom', 'DateFrom') || '';
  if (dateToInput) dateToInput.value = getParamAny('dateTo', 'DateTo') || '';

  if (sortBySelect) sortBySelect.value = getParamAny('sortBy', 'SortBy') || 'uploaddate';
  if (sortDirSelect) sortDirSelect.value = getParamAny('sortDir', 'SortDir') || 'desc';

  if (includeInactiveCb) {
    const inc = getParamAny('includeInactive', 'includeDeleted', 'IncludeInactive');
    includeInactiveCb.checked = (inc === 'true' || inc === '1' || inc === 'on');
  }

  const initialPartnerId = getParamAny('partnerId', 'PartnerId');
  const initialSiteId = getParamAny('siteId', 'SiteId');

  // --- TomSelect: Site ---
  const siteTS = siteSelect.tomselect || new TomSelect(siteSelect, {
    create: false,
    allowEmptyOption: true,
    placeholder: 'Site...',
    valueField: 'id',
    labelField: 'text',
    searchField: ['text'],
    preload: false
  });

  function setSiteEnabled(enabled) {
    siteSelect.disabled = !enabled;
    if (enabled) siteTS.enable();
    else siteTS.disable();
  }

  function resetSitesToEmpty() {
    siteTS.clear(true);
    siteTS.clearOptions();
    siteTS.addOption({ id: '', text: '— Összes site —' });
    siteTS.refreshOptions(false);
  }

  function normalizeSiteItem(x) {
    // támogatott formák:
    // {id,text} | {value,text} | {siteId,name} | {id,name}
    const id = x?.id ?? x?.value ?? x?.siteId ?? '';
    const text = x?.text ?? x?.name ?? x?.label ?? String(id);
    return { id: String(id), text: String(text) };
  }

async function loadSitesForPartner(partnerId, preselectSiteId) {
  // nincs partner -> üres + disable
  if (!partnerId) {
    resetSitesToEmpty();
    setSiteEnabled(false);
    return;
  }

  setSiteEnabled(true);
  resetSitesToEmpty(); // legyen “— Összes site —” alapból, amíg tölt

  const url = `/api/sites/by-partner/${encodeURIComponent(partnerId)}?search=`;
  console.log('Sites fetch:', url);

  try {
    const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);

    const data = await res.json();
    const items = Array.isArray(data) ? data.map(normalizeSiteItem) : [];

    // options feltöltés
    siteTS.clearOptions();
    siteTS.addOption({ id: '', text: '— Összes site —' });
    siteTS.addOptions(items);
    siteTS.refreshOptions(false);

    // preselect (ha van)
    if (preselectSiteId) {
      siteTS.setValue(String(preselectSiteId), true);
    } else {
      siteTS.setValue('', true); // maradjon “összes”
    }
  } catch (err) {
    console.error('Sites endpoint HTTP error:', err);
    // hiba esetén is legyen használható (legalább “összes”)
    resetSitesToEmpty();
    setSiteEnabled(true);
  }
}

  // --- TomSelect: Partner (remote search) ---
  const partnerTS = partnerSelect.tomselect || new TomSelect(partnerSelect, {
    create: false,
    allowEmptyOption: true,
    placeholder: 'Partner keresés...',
    valueField: 'id',
    labelField: 'text',
    searchField: ['text'],
    preload: false,
    loadThrottle: 300,
    maxOptions: 50,

render: {
  option: function (item, escape) {
    const name = escape(item.partnerName || item.text || '');
    const details = escape(item.partnerDetails || '');
    const id = escape(String(item.id || ''));

    return `
      <div style="display:flex;flex-direction:column;gap:2px">
        <div>${name} <small style="opacity:.7">(#${id})</small></div>
        ${details ? `<div style="font-size:12px;opacity:.75">${details}</div>` : ''}
      </div>
    `;
  },
  item: function (item, escape) {
    const name = escape(item.partnerName || item.text || '');
    const id = escape(String(item.id || ''));
    return `<div>${name} <small style="opacity:.7">(#${id})</small></div>`;
  }
}
,

    load: async function (query, callback) {
      try {
        const q = (query || '').trim();
        console.log('PartnerTS load query =', q);

        if (q.length < 2) return callback();

        // ✅ többféle param-név próbálkozás (ez szokta megmenteni az ilyen helyzeteket)
        const urls = [
          `/api/partners/select?search=${encodeURIComponent(q)}`,
          `/api/partners/select?searchTerm=${encodeURIComponent(q)}`,
          `/api/partners/select?term=${encodeURIComponent(q)}`,
          `/api/partners/select?q=${encodeURIComponent(q)}`
        ];

        let items = null;

        for (const url of urls) {
          console.log('Partner fetch:', url);
          const res = await fetch(url, { method: 'GET' });

          if (!res.ok) {
            console.warn('Partner endpoint HTTP:', res.status, 'url:', url);
            continue;
          }

          const data = await res.json();
          if (Array.isArray(data) && data.length > 0) {
            items = data;
            break;
          }

          // ha üres listát ad, próbáljuk a következőt
          if (Array.isArray(data) && data.length === 0) {
            continue;
          }

          // ha más formátum, akkor is lépjünk tovább
        }

        callback(Array.isArray(items) ? items : []);
      } catch (err) {
        console.error('Partner search failed:', err);
        callback();
      }
    }
  });

  // Partner változás -> site-ok betöltése
  partnerTS.on('change', async (value) => {
    try {
      console.log('Partner changed:', value);
      await loadSitesForPartner(value, null);
    } catch (err) {
      console.error('Site load failed:', err);
    }
  });

  // --- Inicializálás ---
  resetSitesToEmpty();
  setSiteEnabled(false);

  if (initialPartnerId) {
    partnerTS.addOption({ id: String(initialPartnerId), text: `#${initialPartnerId}` });
    partnerTS.setValue(String(initialPartnerId), true);
    loadSitesForPartner(String(initialPartnerId), initialSiteId).catch(console.error);
  } else {
    if (initialSiteId) {
      console.warn('siteId van a querystringben, de partnerId nincs – site nem tölthető be.');
    }
  }

  // --- Apply / Reset URL ---
function buildPageUrlFromForm(reset = false) {
  const url = new URL(window.location.href);
  const p = url.searchParams;

  // mindig első oldal
  p.set('pageNumber', '1');

  /* =========================
     RESET ÁG
     ========================= */
  if (reset) {
    p.set('statusFilter', 'all');
    p.set('sortBy', 'uploaddate');
    p.set('sortDir', 'desc');

    [
      'searchTerm',
      'documentTypeId',
      'partnerId',
      'PartnerId',
      'filterPartnerId',
      'siteId',
      'SiteId',
      'filterSiteId',
      'dateFrom',
      'dateTo',
      'includeInactive'
    ].forEach(k => p.delete(k));

    return url.toString();
  }

  /* =========================
     FORM ÉRTÉKEK
     ========================= */
  const search = (searchInput?.value || '').trim();
  const status = (statusSelect?.value || 'all').trim();
  const docTypeId = (docTypeSelect?.value || '').trim();

  const partnerId = (partnerTS.getValue() || '').trim();
  const siteId = (siteTS.getValue() || '').trim();

  const dateFrom = (dateFromInput?.value || '').trim();
  const dateTo = (dateToInput?.value || '').trim();

  const sortBy = (sortBySelect?.value || 'uploaddate').trim();
  const sortDir = (sortDirSelect?.value || 'desc').trim();

  const includeInactive = includeInactiveCb?.checked === true;

  /* =========================
     SEARCH / STATUS
     ========================= */
  if (search) p.set('searchTerm', search);
  else p.delete('searchTerm');

  p.set('statusFilter', status || 'all');

  /* =========================
     DOCUMENT TYPE
     ========================= */
  if (docTypeId) p.set('documentTypeId', docTypeId);
  else p.delete('documentTypeId');

  /* =========================
     PARTNER – TELJES BEBIZTOSÍTÁS
     ========================= */
  [
    'partnerId',
    'PartnerId',
    'filterPartnerId'
  ].forEach(k => p.delete(k));

  if (partnerId) {
    p.set('partnerId', partnerId);        // frontend standard
    p.set('PartnerId', partnerId);        // tipikus backend DTO
    p.set('filterPartnerId', partnerId);  // sok filter így várja
  }

  /* =========================
     SITE – TELJES BEBIZTOSÍTÁS
     ========================= */
  [
    'siteId',
    'SiteId',
    'filterSiteId'
  ].forEach(k => p.delete(k));

  if (siteId) {
    p.set('siteId', siteId);
    p.set('SiteId', siteId);
    p.set('filterSiteId', siteId);
  }

  /* =========================
     DÁTUM
     ========================= */
  if (dateFrom) p.set('dateFrom', dateFrom);
  else p.delete('dateFrom');

  if (dateTo) p.set('dateTo', dateTo);
  else p.delete('dateTo');

  /* =========================
     RENDEZÉS
     ========================= */
  p.set('sortBy', sortBy || 'uploaddate');
  p.set('sortDir', (sortDir === 'asc') ? 'asc' : 'desc');

  /* =========================
     INAKTÍV
     ========================= */
  if (includeInactive) p.set('includeInactive', 'true');
  else p.delete('includeInactive');

  /* ========================= */
  return url.toString();
}

applyBtn.addEventListener('click', function () {
  const newUrl = buildPageUrlFromForm(false);

  // URL frissítés újratöltés nélkül
  window.history.pushState({}, '', newUrl);

  // custom event: dokumentum lista újratöltése
  window.dispatchEvent(new CustomEvent('documents:reload'));
});


resetBtn.addEventListener('click', function () {
  const newUrl = buildPageUrlFromForm(true);
  window.history.pushState({}, '', newUrl);
  window.dispatchEvent(new CustomEvent('documents:reload'));
});

});
