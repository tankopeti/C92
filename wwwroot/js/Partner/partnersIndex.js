// /wwwroot/js/Partner/partnersIndex.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('partnersTableBody');
  const loadMoreBtn = document.getElementById('loadMoreBtn');
  const loadMoreContainer = document.getElementById('loadMoreContainer');
  const searchInput = document.getElementById('searchInput');

  // Advanced filter modal
  const modalEl = document.getElementById('advancedFilterModal');
  const applyFilterBtn = document.getElementById('applyFilterBtn');

  const filterName = document.getElementById('filterName');
  const filterTaxId = document.getElementById('filterTaxId');
  const filterStatus = document.getElementById('filterStatus');

  // ÚJ advanced mezők
  const filterGfoId = document.getElementById('filterGfoId');
  const filterPartnerTypeId = document.getElementById('filterPartnerTypeId');
  const filterPartnerCode = document.getElementById('filterPartnerCode');
  const filterOwnId = document.getElementById('filterOwnId');

  const filterCity = document.getElementById('filterCity');
  const filterPostalCode = document.getElementById('filterPostalCode');
  const filterEmailDomain = document.getElementById('filterEmailDomain');
  const filterActiveOnly = document.getElementById('filterActiveOnly');

  const exportExcelBtn = document.getElementById('exportExcelBtn');


  let currentPage = 1;
  const pageSize = 20;
  let isLoading = false;
  let totalCount = 0;

  // csak olyan paramok, amiket a controllered kezel
  const filters = {
    search: '',
    name: '',
    taxId: '',
    statusId: '',

    // ÚJ
    gfoId: null,
    partnerTypeId: null,
    partnerCode: '',
    ownId: '',
    emailDomain: '',

    city: '',
    postalCode: '',
    activeOnly: true
  };

  const toNumOrNull = (v) => {
    const s = (v ?? '').toString().trim();
    if (!s) return null;
    const n = Number(s);
    return Number.isFinite(n) ? n : null;
  };

  function debounce(fn, delay) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), delay);
    };
  }

  function buildUrl(page) {
    const p = new URLSearchParams();
    p.set('page', String(page));
    p.set('pageSize', String(pageSize));
    p.set('activeOnly', filters.activeOnly ? 'true' : 'false');

    if (filters.search) p.set('search', filters.search);
    if (filters.name) p.set('name', filters.name);
    if (filters.taxId) p.set('taxId', filters.taxId);
    if (filters.statusId) p.set('statusId', filters.statusId);

    // ÚJ: advanced
    if (filters.partnerCode) p.set('partnerCode', filters.partnerCode);
    if (filters.ownId) p.set('ownId', filters.ownId);
    if (filters.emailDomain) p.set('emailDomain', filters.emailDomain);

    if (filters.gfoId != null) p.set('gfoId', String(filters.gfoId));
    if (filters.partnerTypeId != null) p.set('partnerTypeId', String(filters.partnerTypeId));

    if (filters.city) p.set('city', filters.city);
    if (filters.postalCode) p.set('postalCode', filters.postalCode);

    return `/api/Partners?${p.toString()}`;
  }

  function buildExportUrl() {
    const p = new URLSearchParams();
    p.set('activeOnly', filters.activeOnly ? 'true' : 'false');

    if (filters.search) p.set('search', filters.search);
    if (filters.name) p.set('name', filters.name);
    if (filters.taxId) p.set('taxId', filters.taxId);
    if (filters.statusId) p.set('statusId', filters.statusId);

    // advanced
    if (filters.partnerCode) p.set('partnerCode', filters.partnerCode);
    if (filters.ownId) p.set('ownId', filters.ownId);
    if (filters.emailDomain) p.set('emailDomain', filters.emailDomain);

    if (filters.gfoId != null) p.set('gfoId', String(filters.gfoId));
    if (filters.partnerTypeId != null) p.set('partnerTypeId', String(filters.partnerTypeId));

    if (filters.city) p.set('city', filters.city);
    if (filters.postalCode) p.set('postalCode', filters.postalCode);

    return `/api/Partners/export?${p.toString()}`;
  }


  function setLoadingRow() {
    tbody.innerHTML = `
      <tr>
        <td colspan="13" class="text-center py-5 text-muted">
          Betöltés...
        </td>
      </tr>`;
  }

  function setEmptyRow() {
    tbody.innerHTML = `
      <tr>
        <td colspan="13" class="text-center py-5 text-muted">
          Nincs találat
        </td>
      </tr>`;
  }

  function setErrorRow() {
    tbody.innerHTML = `
      <tr>
        <td colspan="13" class="text-center py-5 text-danger">
          Hiba a partnerek betöltésekor
        </td>
      </tr>`;
  }

  function updateLoadMore() {
    if (!loadMoreBtn || !loadMoreContainer) return;

    const rendered = tbody.querySelectorAll('tr[data-partner-id]').length;
    const hasMore = rendered < totalCount;

    loadMoreContainer.classList.toggle('d-none', totalCount === 0);
    loadMoreBtn.disabled = !hasMore;

    if (totalCount === 0) {
      loadMoreBtn.textContent = 'Nincs találat';
      return;
    }

    if (!hasMore) {
      loadMoreBtn.textContent = `Betöltve: ${rendered}/${totalCount} (kész)`;
      return;
    }

    loadMoreBtn.textContent = `Betöltve: ${rendered}/${totalCount} – Több betöltése`;
  }

  function rowHtml(p) {
    const statusColor = p.status?.color || '#6c757d';
    const statusTextColor = statusColor === '#ffc107' ? 'black' : 'white';

    return `
      <tr data-partner-id="${p.partnerId}">
        <td>${p.name || '—'}</td>
        <td>${p.email || '—'}</td>
        <td>${p.phoneNumber || '—'}</td>
        <td>${p.taxId || '—'}</td>
        <td>${p.addressLine1 || ''}</td>
        <td>${p.addressLine2 || ''}</td>
        <td>${p.city || ''}</td>
        <td>${p.state || ''}</td>
        <td>${p.postalCode || ''}</td>
        <td>
          <span class="badge" style="background:${statusColor};color:${statusTextColor}">
            ${p.status?.name || 'N/A'}
          </span>
        </td>
        <td>${p.preferredCurrency || ''}</td>
        <td>${p.assignedTo || ''}</td>
        <td class="text-center">
          <div class="btn-group btn-group-sm" role="group">
            <button type="button"
                    class="btn btn-outline-info view-partner-btn"
                    data-partner-id="${p.partnerId}">
              <i class="bi bi-eye"></i>
            </button>

            <div class="dropdown">
              <button class="btn btn-outline-secondary dropdown-toggle btn-sm"
                      type="button"
                      data-bs-toggle="dropdown">
                <i class="bi bi-three-dots-vertical"></i>
              </button>

              <ul class="dropdown-menu dropdown-menu-end">
                <li>
                  <a class="dropdown-item edit-partner-btn"
                     href="#"
                     data-partner-id="${p.partnerId}">
                    <i class="bi bi-pencil-square me-2"></i>
                    Szerkesztés
                  </a>
                </li>

                <li>
                  <a class="dropdown-item view-history-btn"
                     href="#"
                     data-bs-toggle="modal"
                     data-bs-target="#partnerHistoryModal"
                     data-partner-id="${p.partnerId}"
                     data-partner-name="${p.name || 'Partner'}">
                    <i class="bi bi-clock-history me-2"></i>
                    Történet
                  </a>
                </li>

                <li><hr class="dropdown-divider"></li>

                <li>
                  <a class="dropdown-item text-danger"
                     href="#"
                     data-bs-toggle="modal"
                     data-bs-target="#deletePartnerModal"
                     data-partner-id="${p.partnerId}"
                     data-partner-name="${p.name || 'Partner'}">
                    Törlés
                  </a>
                </li>
              </ul>
            </div>
          </div>
        </td>
      </tr>
    `;
  }

  function appendRow(p) {
    if (tbody.querySelector('tr td[colspan="13"]')) tbody.innerHTML = '';
    tbody.insertAdjacentHTML('beforeend', rowHtml(p));
  }

  function prependRow(p) {
    if (tbody.querySelector('tr td[colspan="13"]')) tbody.innerHTML = '';
    tbody.insertAdjacentHTML('afterbegin', rowHtml(p));
    totalCount = Math.max(0, totalCount + 1);
    updateLoadMore();
  }

  function patchRow(p) {
    const tr = tbody.querySelector(`tr[data-partner-id="${p.partnerId}"]`);
    if (!tr) return;
    tr.outerHTML = rowHtml(p);
  }

  function removeRow(id) {
    const tr = tbody.querySelector(`tr[data-partner-id="${id}"]`);
    if (!tr) return;
    tr.remove();
    totalCount = Math.max(0, totalCount - 1);

    const rendered = tbody.querySelectorAll('tr[data-partner-id]').length;
    if (rendered === 0) setEmptyRow();

    updateLoadMore();
  }

  async function loadPartners({ reset } = { reset: false }) {
    if (isLoading) return;
    isLoading = true;

    if (reset) {
      currentPage = 1;
      totalCount = 0;
      setLoadingRow();
    }

    try {
      const url = buildUrl(currentPage);
      console.log('[partnersIndex] load:', url);

      const res = await fetch(url, {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      totalCount = parseInt(res.headers.get('X-Total-Count') || '0', 10);
      const data = await res.json();

      if (reset) tbody.innerHTML = '';
      if (reset && data.length === 0) {
        setEmptyRow();
      } else {
        data.forEach(appendRow);
      }

      updateLoadMore();
    } catch (e) {
      console.error('Partners load error:', e);
      setErrorRow();
      totalCount = 0;
      updateLoadMore();
    } finally {
      isLoading = false;
    }
  }

  // --- expose API for other scripts (create/edit/delete) ---
  window.c92 = window.c92 || {};
  window.c92.partners = {
    reload: () => loadPartners({ reset: true }),
    prependRow,
    patchRow,
    removeRow,
    getState: () => ({ currentPage, pageSize, totalCount, filters: { ...filters } })
  };

  // --- listen for changes from create/edit/delete scripts ---
  document.addEventListener('partners:changed', (ev) => {
    const d = ev.detail || {};
    const action = d.action;
    const p = d.partner;

    if (action === 'created') {
      if (!p || !p.partnerId) {
        window.c92?.partners?.reload?.();
        return;
      }

      const hasFiltering =
        !!filters.search || !!filters.name || !!filters.taxId || !!filters.statusId ||
        !!filters.city || !!filters.postalCode || !!filters.partnerCode || !!filters.ownId ||
        (filters.gfoId != null) || (filters.partnerTypeId != null) ||
        !!filters.emailDomain || (filters.activeOnly === false);

      if (hasFiltering) {
        window.c92.partners.reload();
        return;
      }

      window.c92.partners.prependRow(p);
      return;
    }

    if (action === 'updated') {
      if (!p || !p.partnerId) return;
      const tr = tbody.querySelector(`tr[data-partner-id="${p.partnerId}"]`);
      if (!tr) window.c92.partners.reload();
      else window.c92.partners.patchRow(p);
      return;
    }

    if (action === 'deleted') {
      const id = d.partnerId || p?.partnerId;
      if (!id) return;
      window.c92.partners.removeRow(id);
    }
  });

  // --- events ---
  searchInput?.addEventListener('input', debounce((e) => {
    filters.search = (e.target.value || '').trim();
    loadPartners({ reset: true });
  }, 350));

  applyFilterBtn?.addEventListener('click', () => {
    filters.name = (filterName?.value || '').trim();
    filters.taxId = (filterTaxId?.value || '').trim();
    filters.statusId = filterStatus?.value || '';

    // ÚJ advanced
    filters.partnerCode = (filterPartnerCode?.value || '').trim();
    filters.ownId = (filterOwnId?.value || '').trim();
    filters.gfoId = toNumOrNull(filterGfoId?.value);
    filters.partnerTypeId = toNumOrNull(filterPartnerTypeId?.value);
    filters.emailDomain = (filterEmailDomain?.value || '').trim();

    filters.city = (filterCity?.value || '').trim();
    filters.postalCode = (filterPostalCode?.value || '').trim();
    filters.activeOnly = filterActiveOnly?.checked ?? true;

    loadPartners({ reset: true });
    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
  });

  exportExcelBtn?.addEventListener('click', () => {
    const url = buildExportUrl();
    console.log('[partnersIndex] export:', url);
    window.location.href = url; // file letöltés
  });


  loadMoreBtn?.addEventListener('click', () => {
    const rendered = tbody.querySelectorAll('tr[data-partner-id]').length;
    if (rendered >= totalCount) return;
    currentPage++;
    loadPartners({ reset: false });
  });

  // init
  loadPartners({ reset: true });
});
