document.addEventListener('DOMContentLoaded', () => {
    const tbody = document.getElementById('partnersTableBody');
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const loadMoreSpinner = document.getElementById('loadMoreSpinner');
    const loadMoreContainer = document.getElementById('loadMoreContainer');
    const searchInput = document.getElementById('searchInput');

    // Advanced filter mezők
    const modalEl = document.getElementById('advancedFilterModal');
    const applyFilterBtn = document.getElementById('applyFilterBtn');

    const filterName = document.getElementById('filterName');
    const filterTaxId = document.getElementById('filterTaxId');
    const filterStatus = document.getElementById('filterStatus');

    // ⚠️ Itt állítsd be a partner típus select ID-ját, ha nálad más:
    const partnerTypeSelect = document.getElementById('filterType');

    const filterCity = document.getElementById('filterCity');
    const filterPostalCode = document.getElementById('filterPostalCode');
    const filterEmailDomain = document.getElementById('filterEmailDomain');
    const filterActiveOnly = document.getElementById('filterActiveOnly');

    let currentPage = 1;
    const pageSize = 20;
    let isLoading = false;
    let hasMore = true;

    let filters = {
        search: '',
        name: '',
        taxId: '',
        statusId: '',
        partnerTypeId: '',   // <- ha nincs ilyen paramétered backendben, ezt később kiveheted
        city: '',
        postalCode: '',
        emailDomain: '',
        activeOnly: true
    };

    /* ---------------- TOMSELECT INIT (MODAL SHOWN) ---------------- */

    function ensureTomSelect(selectEl) {
        if (!selectEl) return;

        // már initelve?
        if (selectEl.tomselect) return;

        // TomSelect nincs betöltve? akkor hagyjuk sima selectként
        if (!window.TomSelect) {
            console.warn('TomSelect nincs betöltve, a select natív marad:', selectEl.id);
            return;
        }

        new TomSelect(selectEl, {
            create: false,
            allowEmptyOption: true,
            closeAfterSelect: true,

            // MODAL FIX: a dropdown ne a modal DOM-jába kerüljön, mert ott el tud romlani
            dropdownParent: 'body',

            // opcionális
            placeholder: selectEl.getAttribute('data-placeholder') || 'Válasszon...'
        });
    }

    function refreshTomSelect(selectEl) {
        if (!selectEl?.tomselect) return;

        // DOM optionök szinkronizálása + frissítés
        selectEl.tomselect.sync();
        selectEl.tomselect.refreshOptions(false);
    }

    if (modalEl) {
        modalEl.addEventListener('shown.bs.modal', () => {
            // Státusz és Partner típus dropdownok init + refresh
            ensureTomSelect(filterStatus);
            ensureTomSelect(partnerTypeSelect);

            // ha az optionök fetch után kerülnek be (loadStatuses.js), ez segít
            setTimeout(() => {
                refreshTomSelect(filterStatus);
                refreshTomSelect(partnerTypeSelect);
            }, 80);
        });
    }

    /* ---------------- API ---------------- */

    function buildUrl(page) {
        const p = new URLSearchParams({
            page: String(page),
            pageSize: String(pageSize),
            activeOnly: filters.activeOnly ? 'true' : 'false'
        });

        Object.entries(filters).forEach(([k, v]) => {
            if (!v) return;
            if (k === 'activeOnly') return;
            p.append(k, v);
        });

        return `/api/Partners?${p.toString()}`;
    }
    
    function updateLoadMoreText(total) {
        if (!loadMoreBtn) return;

        // csak a valódi partner sorokat számoljuk
        const rendered = tbody.querySelectorAll('tr[data-partner-id]').length;
        const remaining = Math.max(0, total - rendered);

        if (remaining <= 0) {
            loadMoreBtn.textContent = 'Nincs több találat';
            loadMoreBtn.disabled = true;
            return;
        }

        const nextBatch = Math.min(pageSize, remaining);
        loadMoreBtn.disabled = false;

        // ugyanaz a minta, mint Contactnál
        loadMoreBtn.textContent = `Betöltve: ${rendered}/${total}`;
    }


    async function loadPartners(reset = false) {
        if (isLoading) return;
        isLoading = true;

        loadMoreSpinner?.classList.remove('d-none');

        if (reset) {
            currentPage = 1;
            hasMore = true;
            tbody.innerHTML = `
                <tr>
                    <td colspan="13" class="text-center py-5 text-muted">
                        Betöltés...
                    </td>
                </tr>`;
        }

        try {
            const res = await fetch(buildUrl(currentPage), {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const total = parseInt(res.headers.get('X-Total-Count') || '0');
            const data = await res.json();

            if (reset) tbody.innerHTML = '';

            data.forEach(addRow);

            hasMore = (currentPage * pageSize) < total;
            loadMoreContainer?.classList.remove('d-none');
            if (!hasMore) {
                loadMoreBtn.disabled = true;
                loadMoreBtn.textContent = 'Nincs több találat';
            }


            updateLoadMoreText(total);

        }
        catch (err) {
            console.error('Partner load error:', err);
            tbody.innerHTML = `
                <tr>
                    <td colspan="13" class="text-center text-danger py-5">
                        Hiba a partnerek betöltésekor
                    </td>
                </tr>`;
        }
        finally {
            isLoading = false;
            loadMoreSpinner?.classList.add('d-none');
        }
    }

    function addRow(p) {
        const statusColor = p.status?.color || '#6c757d';
        const statusTextColor = statusColor === '#ffc107' ? 'black' : 'white';

        tbody.insertAdjacentHTML('beforeend', `
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
        Történet megtekintése
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
        `);
    }

    /* ---------------- EVENTS ---------------- */

    searchInput?.addEventListener('input', debounce(e => {
        filters.search = e.target.value.trim();
        loadPartners(true);
    }, 400));

    applyFilterBtn?.addEventListener('click', () => {
        filters.name = filterName?.value?.trim() || '';
        filters.taxId = filterTaxId?.value?.trim() || '';
        filters.statusId = filterStatus?.value || '';

        // partner típus (ha van ilyen)
        filters.partnerTypeId = partnerTypeSelect?.value || '';

        filters.city = filterCity?.value?.trim() || '';
        filters.postalCode = filterPostalCode?.value?.trim() || '';
        filters.emailDomain = filterEmailDomain?.value?.trim() || '';
        filters.activeOnly = filterActiveOnly?.checked ?? true;

        loadPartners(true);
        bootstrap.Modal.getInstance(modalEl)?.hide();
    });

    loadMoreBtn?.addEventListener('click', () => {
        if (!hasMore) return;
        currentPage++;
        loadPartners(false);
    });

    /* ---------------- INIT ---------------- */

    loadPartners(true);
});

function debounce(fn, delay) {
    let t;
    return (...args) => {
        clearTimeout(t);
        t = setTimeout(() => fn(...args), delay);
    };
}
