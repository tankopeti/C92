document.addEventListener('DOMContentLoaded', () => {
    const tbody = document.getElementById('partnersTableBody');
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const loadMoreContainer = document.getElementById('loadMoreContainer');
    const searchInput = document.getElementById('searchInput');

    // Advanced filter
    const modalEl = document.getElementById('advancedFilterModal');
    const applyFilterBtn = document.getElementById('applyFilterBtn');

    const filterName = document.getElementById('filterName');
    const filterTaxId = document.getElementById('filterTaxId');
    const filterStatus = document.getElementById('filterStatus');
    const filterCity = document.getElementById('filterCity');
    const filterPostalCode = document.getElementById('filterPostalCode');
    const filterEmailDomain = document.getElementById('filterEmailDomain');
    const filterActiveOnly = document.getElementById('filterActiveOnly');

    let currentPage = 1;
    const pageSize = 20;
    let isLoading = false;
    let hasMore = true;
    let totalCount = 0;

    const filters = {
        search: '',
        name: '',
        taxId: '',
        statusId: '',
        city: '',
        postalCode: '',
        emailDomain: '',
        activeOnly: true
    };

    /* ---------------- TOMSELECT ---------------- */

    function ensureTomSelect(selectEl) {
        if (!selectEl || selectEl.tomselect || !window.TomSelect) return;

        new TomSelect(selectEl, {
            create: false,
            allowEmptyOption: true,
            closeAfterSelect: true,
            dropdownParent: 'body',
            placeholder: selectEl.getAttribute('data-placeholder') || 'Válasszon...'
        });
    }

    function refreshTomSelect(selectEl) {
        if (!selectEl?.tomselect) return;
        selectEl.tomselect.sync();
        selectEl.tomselect.refreshOptions(false);
    }

    modalEl?.addEventListener('shown.bs.modal', () => {
        ensureTomSelect(filterStatus);
        setTimeout(() => refreshTomSelect(filterStatus), 80);
    });

    /* ---------------- API ---------------- */

    function buildUrl(page) {
        const p = new URLSearchParams({
            page: page,
            pageSize: pageSize,
            activeOnly: filters.activeOnly ? 'true' : 'false'
        });

        Object.entries(filters).forEach(([k, v]) => {
            if (!v || k === 'activeOnly') return;
            p.append(k, v);
        });

        return `/api/Partners?${p.toString()}`;
    }

    function resetTable() {
        tbody.innerHTML = `
            <tr>
                <td colspan="13" class="text-center py-5 text-muted">
                    Betöltés...
                </td>
            </tr>`;
        loadMoreBtn.disabled = false;
        loadMoreBtn.textContent = 'Több betöltése';
    }

    function updateLoadMore() {
        if (!hasMore) {
            loadMoreBtn.textContent = 'Nincs több találat';
            loadMoreBtn.disabled = true;
            return;
        }

        const rendered = tbody.querySelectorAll('tr[data-partner-id]').length;
        const remaining = Math.max(0, totalCount - rendered);
        const next = Math.min(pageSize, remaining);

        loadMoreBtn.textContent =
            remaining > 0
                ? `Több betöltése (+${next}) • ${rendered}/${totalCount}`
                : 'Nincs több találat';

        loadMoreBtn.disabled = remaining <= 0;
    }

    async function loadPartners(reset = false) {
        if (isLoading) return;
        isLoading = true;

        if (reset) {
            currentPage = 1;
            hasMore = true;
            resetTable();
        }

        try {
            const res = await fetch(buildUrl(currentPage), {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            totalCount = parseInt(res.headers.get('X-Total-Count') || '0');
            const data = await res.json();

            if (reset) tbody.innerHTML = '';

            data.forEach(addRow);

            hasMore = (currentPage * pageSize) < totalCount;
            loadMoreContainer?.classList.remove('d-none');
            updateLoadMore();
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
        <button class="btn btn-sm btn-outline-info view-partner-btn" data-partner-id="${p.partnerId}">
            <i class="bi bi-eye"></i>
        </button>
    </td>
</tr>`);
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

/* ---------------- UTILS ---------------- */

function debounce(fn, delay) {
    let t;
    return (...args) => {
        clearTimeout(t);
        t = setTimeout(() => fn(...args), delay);
    };
}
