// /wwwroot/js/Partner/partnerQuickStatusDropdown.js
// Dropdown gyorsszűrő: Aktív = statusId=1, Minden = statusId üres
// Közvetlen API-hívás (nem függ a partnerFilter.js-től)

document.addEventListener('DOMContentLoaded', () => {
    const tbody = document.getElementById('partnersTableBody');
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const toggleBtn = document.getElementById('partnerQuickFilterToggle');

    if (!tbody || !toggleBtn) {
        console.warn('partnerQuickStatusDropdown: hiányzik partnersTableBody vagy partnerQuickFilterToggle');
        return;
    }

    let currentPage = 1;
    const pageSize = 20;
    let isLoading = false;
    let hasMore = true;

    // null = minden, 1 = Aktív
    let statusIdFilter = null;

    function buildUrl(page) {
        const p = new URLSearchParams({
            page: String(page),
            pageSize: String(pageSize),
        });

        if (statusIdFilter != null) {
            p.set('statusId', String(statusIdFilter));
        }

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
                    Nincs találat.
                </td>
            </tr>`;
    }

    function renderRow(p) {
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
        <i class="bi bi-pencil-square me-2 text-primary"></i>
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
        <i class="bi bi-clock-history me-2 text-secondary"></i>
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
        <i class="bi bi-trash me-2"></i>
        Törlés
    </a>
</li>

                </ul>
            </div>
        </div>
    </td>
</tr>`;
    }

    async function loadPartners({ reset }) {
        if (isLoading) return;
        isLoading = true;

        if (reset) {
            currentPage = 1;
            hasMore = true;
            setLoadingRow();
        }

        try {
            const url = buildUrl(currentPage);
            console.log('GET', url); // 🔎 Ezt nézd meg a Console-ban!

            const res = await fetch(url, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const total = parseInt(res.headers.get('X-Total-Count') || '0');
            const data = await res.json();

            if (reset) tbody.innerHTML = '';

            if (!data.length && reset) {
                setEmptyRow();
            } else {
                tbody.insertAdjacentHTML('beforeend', data.map(renderRow).join(''));
            }

            hasMore = (currentPage * pageSize) < total;
            if (loadMoreBtn) loadMoreBtn.classList.toggle('d-none', !hasMore);
        } catch (err) {
            console.error(err);
            if (reset) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="13" class="text-center py-5 text-danger">
                            Hiba a betöltéskor
                        </td>
                    </tr>`;
            }
        } finally {
            isLoading = false;
        }
    }

    // Dropdown kezelése
    document.addEventListener('click', (e) => {
        const item = e.target.closest('.dropdown-item[data-filter]');
        if (!item) return;

        e.preventDefault();
        const f = item.dataset.filter;

        if (f === 'active') {
            statusIdFilter = 1;
            toggleBtn.innerHTML = `<i class="bi bi-funnel me-1"></i>Aktív partnerek`;
        } else if (f === 'all') {
            statusIdFilter = null;
            toggleBtn.innerHTML = `<i class="bi bi-funnel me-1"></i>Minden partner`;
        } else {
            return;
        }

        // UI active jelölés
        document.querySelectorAll('.dropdown-item[data-filter]').forEach(x => x.classList.remove('active'));
        item.classList.add('active');

        loadPartners({ reset: true });
    });

    // Load more
    loadMoreBtn?.addEventListener('click', (e) => {
        e.preventDefault();
        if (!hasMore) return;
        currentPage++;
        loadPartners({ reset: false });
    });

    // első betöltés
    loadPartners({ reset: true });
});
