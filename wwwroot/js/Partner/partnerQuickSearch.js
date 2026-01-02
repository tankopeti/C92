// /wwwroot/js/Partner/partnerQuickSearch.js
// Cél: szabad szavas keresés + üres keresésre visszatöltés (API-first táblázat)

document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('searchInput');
    if (!input) return;

    // Form és gomb megkeresése (id-val vagy anélkül)
    const form =
        document.getElementById('partnerQuickSearchForm') ||
        input.closest('form');

    const submitBtn = form?.querySelector('button[type="submit"]');

    const tbody = document.getElementById('partnersTableBody');
    if (!tbody) return;

    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const loadMoreContainer = document.getElementById('loadMoreContainer'); // ha van
    const loadMoreSpinner = document.getElementById('loadMoreSpinner');     // ha van

    let currentPage = 1;
    const pageSize = 20;
    let isLoading = false;
    let hasMore = true;

    function buildUrl(page, searchTerm) {
        const p = new URLSearchParams({
            page,
            pageSize,
            activeOnly: 'true'
        });

        if (searchTerm && searchTerm.trim().length > 0) {
            // a controllerben "search" paraméter van
            p.set('search', searchTerm.trim());
        }

        return `/api/Partners?${p.toString()}`;
    }

    function setLoadingUI(isOn) {
        if (loadMoreSpinner) loadMoreSpinner.classList.toggle('d-none', !isOn);
        if (submitBtn) submitBtn.disabled = isOn;
        if (loadMoreBtn) loadMoreBtn.disabled = isOn;
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
            <button type="button" class="btn btn-outline-info view-partner-btn" data-partner-id="${p.partnerId}">
                <i class="bi bi-eye"></i>
            </button>
            <div class="dropdown">
                <button class="btn btn-outline-secondary dropdown-toggle btn-sm" type="button" data-bs-toggle="dropdown">
                    <i class="bi bi-three-dots-vertical"></i>
                </button>
                <ul class="dropdown-menu dropdown-menu-end">
                    <li><a class="dropdown-item edit-partner-btn" href="#" data-partner-id="${p.partnerId}">Szerkesztés</a></li>
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

    async function loadPartners({ reset, searchTerm }) {
        if (isLoading) return;
        isLoading = true;
        setLoadingUI(true);

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
            const res = await fetch(buildUrl(currentPage, searchTerm), {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const total = parseInt(res.headers.get('X-Total-Count') || '0');
            const data = await res.json();

            if (reset) tbody.innerHTML = '';

            if (!data.length && reset) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="13" class="text-center py-5 text-muted">
                            Nincs találat.
                        </td>
                    </tr>`;
            } else {
                tbody.insertAdjacentHTML('beforeend', data.map(renderRow).join(''));
            }

            hasMore = (currentPage * pageSize) < total;

            if (loadMoreContainer) {
                loadMoreContainer.classList.toggle('d-none', !hasMore);
            } else if (loadMoreBtn) {
                loadMoreBtn.classList.toggle('d-none', !hasMore);
            }
        } catch (err) {
            console.error(err);
            if (reset) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="13" class="text-center py-5 text-danger">
                            Hiba a partnerek betöltésekor
                        </td>
                    </tr>`;
            }
        } finally {
            isLoading = false;
            setLoadingUI(false);
        }
    }

    // Aktuális keresési állapot (mindig inputból)
    function getTerm() {
        return (input.value || '').trim();
    }

    // 1) Form submit kezelése: ne legyen oldal reload, hanem API hívás
    form?.addEventListener('submit', (e) => {
        e.preventDefault();

        // Üres esetben: visszatöltjük az összeset
        // Nem-üres esetben: szűrünk
        loadPartners({ reset: true, searchTerm: getTerm() });
    });

    // 2) Load more kezelése ugyanazzal a keresési termmel
    loadMoreBtn?.addEventListener('click', (e) => {
        e.preventDefault();
        if (!hasMore) return;
        currentPage++;
        loadPartners({ reset: false, searchTerm: getTerm() });
    });

    // 3) (Opcionális) Ha kitörlöd teljesen és Enter nélkül szeretnéd vissza:
    // itt nem automatikus, mert te azt kérted: üresen rákattintva töltse vissza.
    // De ha mégis szeretnéd: uncomment
    /*
    input.addEventListener('input', debounce(() => {
        if (getTerm() === '') loadPartners({ reset: true, searchTerm: '' });
    }, 400));
    */

    // első betöltés
    loadPartners({ reset: true, searchTerm: '' });
});

// Ha később kellene a debounce opcióhoz
function debounce(fn, delay) {
    let t;
    return (...args) => {
        clearTimeout(t);
        t = setTimeout(() => fn(...args), delay);
    };
}
