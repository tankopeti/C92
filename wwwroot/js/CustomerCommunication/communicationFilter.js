// wwwroot/js/CustomerCommunication/communicationFilter.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  const loadMoreBtn = document.getElementById('loadMoreBtn');
  const loadMoreSpinner = document.getElementById('loadMoreSpinner');
  const loadMoreContainer = document.getElementById('loadMoreContainer');
  const searchInput = document.getElementById('communicationSearchInput');
  const searchForm = searchInput?.closest('form');

  // ✅ Advanced filter modal elemek
  const filterModalEl = document.getElementById('filterModal');
  const openFilterBtn = document.getElementById('openFilterButton'); // csak sync-hez, ha kell
  const applyFiltersBtn = document.getElementById('applyFiltersButton');
  const clearFiltersBtn = document.getElementById('clearFiltersButton');

  // ✅ Advanced filter mezők (modal)
  const filterPartnerId = document.getElementById('filterPartnerId');
  const filterSiteId = document.getElementById('filterSiteId');
  const filterStatusId = document.getElementById('filterStatusId');
  const filterTypeId = document.getElementById('filterTypeId');
  const filterResponsibleId = document.getElementById('filterResponsibleId');
  const filterDateFrom = document.getElementById('filterDateFrom');
  const filterDateTo = document.getElementById('filterDateTo');
  const filterSearchText = document.getElementById('filterSearchText');

  if (!tbody) {
    console.error('Hiányzik a communicationsTableBody');
    return;
  }

  console.log('✅ communicationFilter.js betöltve', {
    hasLoadMoreBtn: !!loadMoreBtn,
    hasSearchInput: !!searchInput,
    hasFilterModal: !!filterModalEl,
    hasApplyFiltersBtn: !!applyFiltersBtn
  });

  let currentPage = 1;

  // ✅ Sites mintára: fix pageSize (vagy vedd a data-page-size-ból)
  const pageSize = Number(document.querySelector('.table-dynamic-height')?.getAttribute('data-page-size') || 20);

  let isLoading = false;
  let hasMore = true;
  let totalCount = 0;

  // ✅ Minden szűrő egy helyen (legacy + advanced)
  let filters = {
    // legacy
    search: '',
    typeFilter: 'all',            // all | E-mail | Telefonhívás | Találkozó
    sortBy: 'CommunicationDate',  // CommunicationDate | CommunicationId | PartnerName

    // advanced
    partnerId: '',
    siteId: '',
    statusId: '',
    communicationTypeId: '',
    responsibleId: '',
    dateFrom: '',
    dateTo: '',
    searchText: ''
  };

  function buildUrl(page) {
    const p = new URLSearchParams({
      pageNumber: String(page),
      pageSize: String(pageSize),

      // legacy
      search: filters.search || '',
      typeFilter: filters.typeFilter || 'all',
      sortBy: filters.sortBy || 'CommunicationDate',

      // advanced
      partnerId: filters.partnerId || '',
      siteId: filters.siteId || '',
      statusId: filters.statusId || '',
      communicationTypeId: filters.communicationTypeId || '',
      responsibleId: filters.responsibleId || '',
      dateFrom: filters.dateFrom || '',
      dateTo: filters.dateTo || '',
      searchText: filters.searchText || ''
    });

    const url = `/api/CustomerCommunicationIndex?${p.toString()}`;
    console.log('🔎 buildUrl:', url);
    return url;
  }

  function setLoadMoreText() {
    if (!loadMoreBtn) return;
    const loaded = tbody.querySelectorAll('tr[data-communication-id]').length;
    const total = totalCount || loaded;

    loadMoreBtn.innerHTML =
      `<span class="me-2">Betöltve <strong>${loaded}</strong> / <strong>${total}</strong></span> ` +
      `<span class="opacity-75">Több betöltése</span>`;
  }

  async function loadCommunications(reset = false) {
    if (isLoading) return;
    isLoading = true;

    loadMoreSpinner?.classList.remove('d-none');

    if (reset) {
      currentPage = 1;
      hasMore = true;
      totalCount = 0;
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center py-5 text-muted">Betöltés...</td>
        </tr>`;
    }

    try {
      const res = await fetch(buildUrl(currentPage), {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      console.log('📡 /api/CustomerCommunicationIndex status:', res.status);

      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ API error raw:', raw);
        throw new Error(`HTTP ${res.status}`);
      }

      totalCount = parseInt(res.headers.get('X-Total-Count') || '0', 10);
      const data = await res.json();

      console.log('✅ Rows received:', Array.isArray(data) ? data.length : data, {
        totalCount,
        currentPage,
        pageSize,
        filters: { ...filters }
      });

      if (reset) tbody.innerHTML = '';
      data.forEach(addRow);

      const loaded = tbody.querySelectorAll('tr[data-communication-id]').length;
      hasMore = loaded < totalCount;

      loadMoreContainer?.classList.toggle('d-none', !hasMore);
      setLoadMoreText();

      if (loaded === 0) {
        tbody.innerHTML = `
          <tr>
            <td colspan="7" class="text-center py-5 text-muted">Nincs találat</td>
          </tr>`;
      }
    } catch (err) {
      console.error('Communication load error:', err);
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center text-danger py-5">Hiba a kommunikációk betöltésekor</td>
        </tr>`;
      loadMoreContainer?.classList.add('d-none');
    } finally {
      isLoading = false;
      loadMoreSpinner?.classList.add('d-none');
    }
  }

  function badgeHtml(statusName, statusDisplayName) {
    const cls =
      statusName === 'Nyitott' ? 'badge bg-primary' :
      statusName === 'Folyamatban' ? 'badge bg-warning' :
      statusName === 'Eskalálva' ? 'badge bg-danger flash' :
      statusName === 'Megoldva' ? 'badge bg-success' :
      'badge bg-secondary';

    return `<span class="${cls}">${escapeHtml(statusDisplayName || statusName || '—')}</span>`;
  }

  function typeHtml(typeName) {
    if (typeName === 'E-mail') return `<i class="bi bi-envelope me-1"></i>E-mail`;
    if (typeName === 'Telefonhívás') return `<i class="bi bi-telephone me-1"></i>Telefon`;
    if (typeName === 'Találkozó') return `<i class="bi bi-calendar-event me-1"></i>Találkozó`;
    return `<i class="bi bi-chat me-1"></i>Egyéb`;
  }

  function addRow(c) {
    // elvárt mezők (API):
    // customerCommunicationId, communicationTypeName, partnerName, responsibleName, dateText, subject,
    // statusName, statusDisplayName
    const id = c.customerCommunicationId;
    const subject = c.subject || '—';

    tbody.insertAdjacentHTML('beforeend', `
<tr data-communication-id="${escapeAttr(id)}">
  <td class="text-nowrap">${typeHtml(c.communicationTypeName)}</td>
  <td class="text-nowrap"><i class="bi bi-person me-1"></i>${escapeHtml(c.partnerName || '—')}</td>
  <td class="text-nowrap"><i class="bi bi-person-check me-1"></i>${escapeHtml(c.responsibleName || '—')}</td>
  <td class="text-nowrap"><i class="bi bi-calendar me-1"></i>${escapeHtml(c.dateText || '—')}</td>
  <td class="text-nowrap">${escapeHtml(subject)}</td>
  <td class="text-nowrap">${badgeHtml(c.statusName, c.statusDisplayName)}</td>
  <td class="text-center">
    <div class="btn-group btn-group-sm" role="group">
      <button type="button" class="btn btn-outline-info view-communication-btn" data-communication-id="${escapeAttr(id)}">
        <i class="bi bi-eye"></i>
      </button>

      <div class="dropdown">
        <button class="btn btn-outline-secondary dropdown-toggle btn-sm" type="button" data-bs-toggle="dropdown">
          <i class="bi bi-three-dots-vertical"></i>
        </button>

        <ul class="dropdown-menu dropdown-menu-end">
          <li>
            <a class="dropdown-item edit-communication-btn" href="#" data-communication-id="${escapeAttr(id)}">
              <i class="bi bi-pencil-square me-2"></i>Szerkesztés
            </a>
          </li>
          <li>
            <a class="dropdown-item history-communication-btn" href="#"
              data-communication-id="${escapeAttr(id)}"
              data-subject="${escapeAttr(subject)}">
              <i class="bi bi-clock-history me-2"></i>Előzmények
            </a>
          </li>
          <li><hr class="dropdown-divider"></li>
          <li>
            <a class="dropdown-item text-danger delete-communication-btn" href="#"
               data-communication-id="${escapeAttr(id)}"
               data-subject="${escapeAttr(subject)}">
              <i class="bi bi-trash me-2"></i>Törlés
            </a>
          </li>
        </ul>
      </div>
    </div>
  </td>
</tr>`);
  }

  // ✅ kereső (debounce)
  if (searchInput) {
    searchInput.addEventListener('input', debounce((e) => {
      filters.search = (e.target.value || '').trim();
      console.log('⌨️ search changed:', filters.search);
      loadCommunications(true);
    }, 300));
  }

  // ✅ Enter / kereső gomb: ne reloadoljon
  if (searchForm) {
    searchForm.addEventListener('submit', (e) => {
      e.preventDefault();
      filters.search = (searchInput?.value || '').trim();
      console.log('🟨 form submit search=', filters.search);
      loadCommunications(true);
    });
  }

  // ✅ dropdown: typeFilter + opcionális sortBy (Sites mintára data-*)
  document.querySelectorAll('[data-typefilter]').forEach(a => {
    a.addEventListener('click', e => {
      e.preventDefault();
      filters.typeFilter = a.getAttribute('data-typefilter') || 'all';
      const sortBy = a.getAttribute('data-sortby');
      if (sortBy) filters.sortBy = sortBy;
      console.log('🧰 filter/sort changed:', { typeFilter: filters.typeFilter, sortBy: filters.sortBy });
      loadCommunications(true);
    });
  });

  loadMoreBtn?.addEventListener('click', () => {
    if (!hasMore) return;
    currentPage++;
    loadCommunications(false);
  });

  // ✅ külső fájlok (edit/delete/copy) tudják újratölteni a listát
  window.addEventListener('cc:reload', () => {
    console.log('🔁 cc:reload event -> reload list');
    loadCommunications(true);
  });

  // -------------------------
  // ✅ Advanced filter: Apply / Clear
  // -------------------------
  function getTsValueOrSelectValue(selectEl) {
    if (!selectEl) return '';
    if (selectEl.tomselect && typeof selectEl.tomselect.getValue === 'function') {
      return selectEl.tomselect.getValue() || '';
    }
    return selectEl.value || '';
  }

  function hideFilterModal() {
    if (!filterModalEl) return;
    if (window.bootstrap?.Modal) {
      const instance =
        window.bootstrap.Modal.getInstance(filterModalEl) ||
        new window.bootstrap.Modal(filterModalEl);
      instance.hide();
    }
  }

  function syncFiltersToModal() {
    // opcionális: ha openFilterButton-ra akarsz előtöltést
    if (filterPartnerId?.tomselect) filterPartnerId.tomselect.setValue(filters.partnerId || '', true);
    else if (filterPartnerId) filterPartnerId.value = filters.partnerId || '';

    if (filterSiteId?.tomselect) filterSiteId.tomselect.setValue(filters.siteId || '', true);
    else if (filterSiteId) filterSiteId.value = filters.siteId || '';

    if (filterStatusId?.tomselect) filterStatusId.tomselect.setValue(filters.statusId || '', true);
    else if (filterStatusId) filterStatusId.value = filters.statusId || '';

    if (filterTypeId?.tomselect) filterTypeId.tomselect.setValue(filters.communicationTypeId || '', true);
    else if (filterTypeId) filterTypeId.value = filters.communicationTypeId || '';

    if (filterResponsibleId?.tomselect) filterResponsibleId.tomselect.setValue(filters.responsibleId || '', true);
    else if (filterResponsibleId) filterResponsibleId.value = filters.responsibleId || '';

    if (filterDateFrom) filterDateFrom.value = filters.dateFrom || '';
    if (filterDateTo) filterDateTo.value = filters.dateTo || '';
    if (filterSearchText) filterSearchText.value = filters.searchText || '';
  }

  // modal megnyitáskor töltse vissza az aktuális state-et
  openFilterBtn?.addEventListener('click', () => {
    syncFiltersToModal();
  });

  applyFiltersBtn?.addEventListener('click', () => {
    filters.partnerId = getTsValueOrSelectValue(filterPartnerId);
    filters.siteId = getTsValueOrSelectValue(filterSiteId);
    filters.statusId = getTsValueOrSelectValue(filterStatusId);
    filters.communicationTypeId = getTsValueOrSelectValue(filterTypeId);
    filters.responsibleId = getTsValueOrSelectValue(filterResponsibleId);

    filters.dateFrom = (filterDateFrom?.value || '').trim();
    filters.dateTo = (filterDateTo?.value || '').trim();
    filters.searchText = (filterSearchText?.value || '').trim();

    console.log('🧩 advanced filters applied:', {
      partnerId: filters.partnerId,
      siteId: filters.siteId,
      statusId: filters.statusId,
      communicationTypeId: filters.communicationTypeId,
      responsibleId: filters.responsibleId,
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
      searchText: filters.searchText
    });

    hideFilterModal();
    loadCommunications(true);
  });

  clearFiltersBtn?.addEventListener('click', () => {
    filters.partnerId = '';
    filters.siteId = '';
    filters.statusId = '';
    filters.communicationTypeId = '';
    filters.responsibleId = '';
    filters.dateFrom = '';
    filters.dateTo = '';
    filters.searchText = '';

    if (filterPartnerId?.tomselect) filterPartnerId.tomselect.clear(true); else if (filterPartnerId) filterPartnerId.value = '';
    if (filterSiteId?.tomselect) filterSiteId.tomselect.clear(true); else if (filterSiteId) filterSiteId.value = '';
    if (filterStatusId?.tomselect) filterStatusId.tomselect.clear(true); else if (filterStatusId) filterStatusId.value = '';
    if (filterTypeId?.tomselect) filterTypeId.tomselect.clear(true); else if (filterTypeId) filterTypeId.value = '';
    if (filterResponsibleId?.tomselect) filterResponsibleId.tomselect.clear(true); else if (filterResponsibleId) filterResponsibleId.value = '';

    if (filterDateFrom) filterDateFrom.value = '';
    if (filterDateTo) filterDateTo.value = '';
    if (filterSearchText) filterSearchText.value = '';

    hideFilterModal();
    loadCommunications(true);
  });

  // ✅ initial load
  loadCommunications(true);

  function debounce(fn, delay) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), delay);
    };
  }

  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  function escapeAttr(str) {
    return escapeHtml(str).replaceAll('`', '&#096;');
  }
});
