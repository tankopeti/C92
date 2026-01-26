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
  const filterCity = document.getElementById('filterCity');
  const filterPostalCode = document.getElementById('filterPostalCode');
  const filterActiveOnly = document.getElementById('filterActiveOnly');

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
    city: '',
    postalCode: '',
    activeOnly: true
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
    if (filters.city) p.set('city', filters.city);
    if (filters.postalCode) p.set('postalCode', filters.postalCode);

    return `/api/Partners?${p.toString()}`;
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
    // ha üres/placeholder sor van, töröljük
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
    if (!tr) return; // lehet más oldalon van -> inkább reload
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

  // created
  if (action === 'created') {
    // ha nincs partner objektum, inkább reload
    if (!p || !p.partnerId) {
      window.c92?.partners?.reload?.();
      return;
    }

    // ha van bármilyen aktív filter/keresés, biztonságosabb a reload
    const hasFiltering =
      !!filters.search || !!filters.name || !!filters.taxId || !!filters.statusId ||
      !!filters.city || !!filters.postalCode || (filters.activeOnly === false);

    if (hasFiltering) {
      window.c92.partners.reload();
      return;
    }

    // nincs filter -> simán beszúrjuk felülre
    window.c92.partners.prependRow(p);
    return;
  }

  // updated
  if (action === 'updated') {
    if (!p || !p.partnerId) return;
    // ha nincs a táblában (másik oldalon van), akkor reload
    const tr = tbody.querySelector(`tr[data-partner-id="${p.partnerId}"]`);
    if (!tr) window.c92.partners.reload();
    else window.c92.partners.patchRow(p);
    return;
  }

  // deleted
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
    filters.city = (filterCity?.value || '').trim();
    filters.postalCode = (filterPostalCode?.value || '').trim();
    filters.activeOnly = filterActiveOnly?.checked ?? true;

    loadPartners({ reset: true });
    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
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
