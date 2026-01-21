// /js/Doc/viewDocument.js – View modal kezelés (Partner minta stílus)
document.addEventListener('DOMContentLoaded', function () {
    console.log('viewDocument.js BETÖLTÖDÖTT – View modal kész');

    const modalEl = document.getElementById('viewDocumentModal');
    if (!modalEl) {
        console.warn('viewDocumentModal hiányzik – kilépés');
        return;
    }

    const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);

    // View fields
    const el = {
        id: document.getElementById('viewDocId'),
        fileName: document.getElementById('viewDocFileName'),
        docType: document.getElementById('viewDocType'),
        partner: document.getElementById('viewDocPartner'),
        site: document.getElementById('viewDocSite'),
        status: document.getElementById('viewDocStatus'),
        uploadDate: document.getElementById('viewDocUploadDate'),
        uploadedBy: document.getElementById('viewDocUploadedBy'),
        metadataTbody: document.getElementById('viewDocMetadataTbody'),
        openFileLink: document.getElementById('viewDocOpenFileLink'),
        errorBox: document.getElementById('viewDocErrorBox'),
        errorText: document.getElementById('viewDocErrorText'),
        openEditBtn: document.getElementById('openEditDocumentBtn')
    };

    function showError(msg) {
        if (el.errorText) el.errorText.textContent = msg || 'Hiba történt.';
        el.errorBox?.classList.remove('d-none');
    }
    function hideError() {
        el.errorBox?.classList.add('d-none');
    }

    function fmtDate(d) {
        if (!d) return 'N/A';
        try {
            return new Date(d).toLocaleDateString('hu-HU');
        } catch {
            return 'N/A';
        }
    }

    function clearView() {
        hideError();
        if (el.id) el.id.textContent = '—';
        if (el.fileName) el.fileName.textContent = '—';
        if (el.docType) el.docType.textContent = '—';
        if (el.partner) el.partner.textContent = '—';
        if (el.site) el.site.textContent = '—';
        if (el.status) el.status.textContent = '—';
        if (el.uploadDate) el.uploadDate.textContent = '—';
        if (el.uploadedBy) el.uploadedBy.textContent = '—';

        if (el.openFileLink) {
            el.openFileLink.classList.add('d-none');
            el.openFileLink.setAttribute('href', '#');
        }

        if (el.metadataTbody) {
            el.metadataTbody.innerHTML = `
                <tr>
                    <td colspan="2" class="text-muted text-center py-3">Nincs metaadat</td>
                </tr>`;
        }

        if (el.openEditBtn) {
            el.openEditBtn.dataset.documentId = '';
        }
    }

    function renderMetadata(metadata) {
        if (!el.metadataTbody) return;

        const arr = Array.isArray(metadata) ? metadata : [];
        if (arr.length === 0) {
            el.metadataTbody.innerHTML = `
                <tr>
                    <td colspan="2" class="text-muted text-center py-3">Nincs metaadat</td>
                </tr>`;
            return;
        }

        el.metadataTbody.innerHTML = arr.map(m => `
            <tr>
                <td>${escapeHtml(m.key ?? '')}</td>
                <td>${escapeHtml(m.value ?? '')}</td>
            </tr>
        `).join('');
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, (ch) => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[ch]);
    }

    async function loadAndShow(docId) {
        clearView();
        hideError();

        try {
            const res = await fetch(`/api/documents/${docId}`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const doc = await res.json();

            if (el.id) el.id.textContent = doc.documentId ?? docId;
            if (el.fileName) el.fileName.textContent = doc.fileName ?? 'N/A';
            if (el.docType) el.docType.textContent = doc.documentTypeName ?? 'N/A';
            if (el.partner) el.partner.textContent = doc.partnerName ?? 'N/A';

            // Site név nincs a DocumentDto-ban -> marad N/A (később bővíthető)
            if (el.site) el.site.textContent = doc.siteName ?? 'N/A';

            if (el.status) el.status.textContent = doc.status ?? 'N/A';
            if (el.uploadDate) el.uploadDate.textContent = fmtDate(doc.uploadDate);
            if (el.uploadedBy) el.uploadedBy.textContent = doc.uploadedBy ?? 'N/A';

            // fájl link
            if (el.openFileLink) {
                el.openFileLink.classList.remove('d-none');
                el.openFileLink.setAttribute('href', `/file/${doc.documentId ?? docId}`);
            }

            // meta (a DTO-ban most nincs, de ha később lesz, itt megjelenik)
            const meta =
                    doc.customMetadata ??
                    doc.CustomMetadata ??
                    doc.documentMetadata ??
                    doc.DocumentMetadata ??
                    [];

                renderMetadata(meta);


            if (el.openEditBtn) {
                el.openEditBtn.dataset.documentId = String(doc.documentId ?? docId);
            }

            bsModal.show();
        } catch (err) {
            console.error(err);
            showError('Nem sikerült betölteni a dokumentumot.');
            bsModal.show();
        }
    }

    // Delegált click: a táblázat dinamikus
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.view-document-btn');
        if (!btn) return;

        e.preventDefault();
        const docId = btn.dataset.documentId;
        if (!docId) return;

        loadAndShow(docId);
    });
});
