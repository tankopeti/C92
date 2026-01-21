// /js/Doc/docLoadMore.js
// Dokumentum lista betöltése + "Több betöltése" gomb
// FONTOS: a filter pushState-t használ, ezért a querystringet
// MINDIG a buildApiUrl()-ban olvassuk újra

document.addEventListener('DOMContentLoaded', function () {
    console.log('docLoadMore.js BETÖLTÖDÖTT – Dokumentum lista inicializálása');

    const tableBody = document.getElementById('documentsTableBody');
    const loadMoreBtn = document.getElementById('loadMoreDocsBtn');
    const loadMoreContainer = document.getElementById('loadMoreDocsContainer');

    if (!tableBody || !loadMoreBtn) {
        console.warn('documentsTableBody vagy loadMoreDocsBtn hiányzik – kilépés');
        return;
    }

    // ---- Állapot ----
    let isLoading = false;
    let skip = 0;

    // pageSize: HTML data attribútum vagy default
    const pageSize =
        document.querySelector('.table-dynamic-height')?.dataset?.pageSize
            ? parseInt(document.querySelector('.table-dynamic-height').dataset.pageSize, 10)
            : 20;

    // ---- Helpers ----
    function buildApiUrl() {
        // 🔥 MINDIG aktuális URL (pushState miatt!)
        const qs = new URLSearchParams(window.location.search);

        const searchTerm = (qs.get('searchTerm') || '').trim();
        const statusFilter = (qs.get('statusFilter') || 'all').trim();
        const sortBy = (qs.get('sortBy') || 'uploaddate').trim();
        const sortDir = (qs.get('sortDir') || 'desc').trim();

        const documentTypeId = (qs.get('documentTypeId') || '').trim();

        const partnerId = (
            qs.get('partnerId') ||
            qs.get('PartnerId') ||
            qs.get('filterPartnerId') ||
            ''
        ).trim();

        const siteId = (
            qs.get('siteId') ||
            qs.get('SiteId') ||
            qs.get('filterSiteId') ||
            ''
        ).trim();

        const dateFrom = (qs.get('dateFrom') || '').trim();
        const dateTo = (qs.get('dateTo') || '').trim();

        const includeInactiveRaw = (qs.get('includeInactive') || '').trim();
        const includeInactive =
            includeInactiveRaw === 'true' ||
            includeInactiveRaw === '1' ||
            includeInactiveRaw === 'on';

        const params = new URLSearchParams();

        // backend paramok
        if (searchTerm) params.append('search', searchTerm);
        if (statusFilter && statusFilter !== 'all') params.append('status', statusFilter);

        if (documentTypeId) params.append('documentTypeId', documentTypeId);
        if (partnerId) params.append('partnerId', partnerId);
        if (siteId) params.append('siteId', siteId);

        if (dateFrom) params.append('dateFrom', dateFrom);
        if (dateTo) params.append('dateTo', dateTo);

        if (includeInactive) params.append('includeInactive', 'true');

        params.append('sortBy', sortBy);
        params.append('sortDir', sortDir);

        params.append('skip', skip);
        params.append('take', pageSize);

        return `/api/documents?${params.toString()}`;
    }

    function formatDate(dateString) {
        if (!dateString) return 'N/A';
        try {
            return new Date(dateString).toLocaleDateString('hu-HU');
        } catch {
            return 'N/A';
        }
    }

    function renderRow(doc) {
        return `
<tr>
    <td class="text-nowrap">
        <i class="bi bi-file-earmark-text me-1"></i> ${doc.documentId}
    </td>

    <td class="text-nowrap">
        <i class="bi bi-file-earmark me-1"></i>
        <a href="/file/${doc.documentId}" target="_blank">
            ${doc.fileName && doc.fileName.length > 40
                ? doc.fileName.substring(0, 40) + '...'
                : (doc.fileName || 'N/A')}
        </a>
    </td>

    <td class="text-nowrap">
        <i class="bi bi-person me-1"></i> ${doc.partnerName || 'N/A'}
    </td>

    <td class="text-nowrap">
        <i class="bi bi-calendar me-1"></i> ${formatDate(doc.uploadDate)}
    </td>

    <td class="text-nowrap">
        <span class="badge bg-secondary">
            ${doc.status || 'N/A'}
        </span>
    </td>

    <td class="text-center">
        <div class="btn-group btn-group-sm">

            <button type="button"
                    class="btn btn-outline-info view-document-btn"
                    title="Megtekintés"
                    data-document-id="${doc.documentId}">
                <i class="bi bi-eye"></i>
            </button>

            <button class="btn btn-outline-secondary dropdown-toggle"
                    type="button"
                    data-bs-toggle="dropdown">
                <i class="bi bi-three-dots-vertical"></i>
            </button>

            <ul class="dropdown-menu dropdown-menu-end">
                <li>
                    <a class="dropdown-item edit-document-btn"
                       href="#"
                       data-document-id="${doc.documentId}">
                        <i class="bi bi-pencil me-2"></i>Szerkesztés
                    </a>
                </li>

                <li>
                    <a class="dropdown-item document-history-btn"
                       href="#"
                       data-document-id="${doc.documentId}">
                        <i class="bi bi-clock-history me-2"></i>Előzmények
                    </a>
                </li>

                <li><hr class="dropdown-divider"></li>

                <li>
                    <a class="dropdown-item text-danger delete-document-btn"
                       href="#"
                       data-document-id="${doc.documentId}">
                        <i class="bi bi-trash me-2"></i>Törlés
                    </a>
                </li>
            </ul>

        </div>
    </td>
</tr>`;
    }

    function setLoadingState(loading) {
        isLoading = loading;

        loadMoreBtn.disabled = loading;
        loadMoreBtn.textContent = loading ? 'Betöltés...' : 'Több betöltése';
    }

    function hideLoadMore() {
        if (loadMoreContainer) loadMoreContainer.style.display = 'none';
    }

    function showLoadMore() {
        if (loadMoreContainer) loadMoreContainer.style.display = 'block';
    }

    // ---- Betöltés ----
    async function loadDocuments() {
        if (isLoading) return;

        setLoadingState(true);

        try {
            const url = buildApiUrl();
            console.log('Documents fetch:', url);

            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const docs = await res.json();

            if (skip === 0) tableBody.innerHTML = '';

            if (!Array.isArray(docs) || docs.length === 0) {
                hideLoadMore();
                if (skip === 0) {
                    tableBody.innerHTML = `
<tr>
    <td colspan="6" class="text-center py-5 text-muted">
        Nincs megjeleníthető dokumentum
    </td>
</tr>`;
                }
                return;
            }

            docs.forEach(doc => {
                tableBody.insertAdjacentHTML('beforeend', renderRow(doc));
            });

            skip += docs.length;

            docs.length < pageSize ? hideLoadMore() : showLoadMore();

        } catch (err) {
            console.error('Dokumentumok betöltése sikertelen:', err);
        } finally {
            setLoadingState(false);
        }
    }

    // ---- Események ----
    loadMoreBtn.addEventListener('click', loadDocuments);

    // Filter apply/reset -> újratöltés (pushState)
    window.addEventListener('documents:reload', () => {
        console.log('documents:reload – lista újratöltése');

        if (isLoading) return;

        skip = 0;
        tableBody.innerHTML = '';
        showLoadMore();

        loadDocuments();
    });

    // Browser back/forward
    window.addEventListener('popstate', () => {
        console.log('popstate – lista újratöltése');

        if (isLoading) return;

        skip = 0;
        tableBody.innerHTML = '';
        showLoadMore();

        loadDocuments();
    });

    // első automatikus betöltés
    loadDocuments();
});
