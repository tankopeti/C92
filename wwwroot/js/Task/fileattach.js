
    document.addEventListener('DOMContentLoaded', function () {
        const attachBtn = document.getElementById('attachDocumentsBtn');
        const pickerModalEl = document.getElementById('documentPickerModal');
        const pickerModal = new bootstrap.Modal(pickerModalEl);
        const confirmBtn = document.getElementById('confirmAttachDocuments');
        const searchInput = document.getElementById('documentSearch');
        const selectAllCheckbox = document.getElementById('selectAllDocs');
        const tableBody = document.getElementById('documentsTableBody');
        const loadingDiv = document.getElementById('documentsLoading');
        const attachedList = document.getElementById('attachedDocumentsList');
        const attachedCount = document.getElementById('attachedCount');
        const hiddenInput = document.getElementById('attachedDocumentIdsInput');

        let allDocuments = [];           // Full list from API
        let selectedDocumentIds = new Set();  // Currently selected for attachment

        // Open document picker
        attachBtn.addEventListener('click', function () {
            loadDocuments();
            pickerModal.show();
        });

        // Load documents from your existing Document API
        async function loadDocuments() {
            loadingDiv.style.display = 'block';
            tableBody.innerHTML = '';
            selectedDocumentIds.clear();

            try {
                const response = await fetch('/api/documents'); // Your existing document list endpoint
                if (!response.ok) throw new Error('Hiba a dokumentumok betöltésekor');

                allDocuments = await response.json();

                renderDocuments(allDocuments);
            } catch (err) {
                tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger py-4">
                    Nem sikerült betölteni a dokumentumokat: ${err.message}
                </td></tr>`;
            } finally {
                loadingDiv.style.display = 'none';
            }
        }

        // Render document rows
        function renderDocuments(docs) {
            if (docs.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">Nincs elérhető dokumentum</td></tr>';
                return;
            }

            tableBody.innerHTML = docs.map(doc => `
                <tr>
                    <td>
                        <input class="form-check-input document-checkbox" type="checkbox" value="${doc.documentId}" 
                               data-filename="${doc.fileName || 'Ismeretlen fájl'}">
                    </td>
                    <td><strong>${escapeHtml(doc.fileName || 'Nincs név')}</strong></td>
                    <td>${escapeHtml(doc.documentTypeName || '-')}</td>
                    <td>${formatDate(doc.uploadDate)}</td>
                    <td>${escapeHtml(doc.uploadedBy || '-')}</td>
                </tr>
            `).join('');

            // Re-attach checkbox listeners
            document.querySelectorAll('.document-checkbox').forEach(cb => {
                cb.addEventListener('change', updateSelection);
            });
            selectAllCheckbox.addEventListener('change', toggleSelectAll);
        }

        // Search filter
        searchInput.addEventListener('input', function () {
            const term = this.value.toLowerCase();
            const filtered = allDocuments.filter(doc =>
                (doc.fileName?.toLowerCase().includes(term)) ||
                (doc.documentTypeName?.toLowerCase().includes(term)) ||
                (doc.uploadedBy?.toLowerCase().includes(term))
            );
            renderDocuments(filtered);
        });

        // Select all toggle
        function toggleSelectAll() {
            const checked = selectAllCheckbox.checked;
            document.querySelectorAll('.document-checkbox').forEach(cb => {
                cb.checked = checked;
            });
            updateSelection();
        }

        // Update selected IDs and UI
        function updateSelection() {
            selectedDocumentIds.clear();
            document.querySelectorAll('.document-checkbox:checked').forEach(cb => {
                selectedDocumentIds.add(parseInt(cb.value));
            });

            const count = selectedDocumentIds.size;
            confirmBtn.textContent = count > 0 
                ? `Kiválasztottak csatolása (${count})`
                : 'Kiválasztottak csatolása';
            confirmBtn.disabled = count === 0;
        }

        // Confirm attachment
        confirmBtn.addEventListener('click', function () {
            if (selectedDocumentIds.size === 0) return;

            // Add selected documents to the task form
            allDocuments
                .filter(doc => selectedDocumentIds.has(doc.documentId))
                .forEach(doc => {
                    if (!document.getElementById('attachedDoc_' + doc.documentId)) {
                        const item = document.createElement('div');
                        item.id = 'attachedDoc_' + doc.documentId;
                        item.className = 'd-flex justify-content-between align-items-center p-2 mb-2 bg-white border rounded shadow-sm';
                        item.innerHTML = `
                            <div>
                                <i class="bi bi-file-earmark-text me-2 text-primary"></i>
                                <strong>${escapeHtml(doc.fileName)}</strong>
                                <small class="text-muted ms-2">(${doc.documentTypeName || 'Dokumentum'})</small>
                            </div>
                            <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeAttachedDoc(${doc.documentId})">
                                <i class="bi bi-x"></i>
                            </button>
                        `;
                        attachedList.appendChild(item);
                    }
                });

            updateAttachedCount();
            pickerModal.hide();
        });

        // Remove attached document
        window.removeAttachedDoc = function (docId) {
            const el = document.getElementById('attachedDoc_' + docId);
            if (el) el.remove();
            updateAttachedCount();
        };

        // Update badge + hidden input
        function updateAttachedCount() {
            const items = attachedList.querySelectorAll('[id^="attachedDoc_"]');
            const ids = Array.from(items).map(div => div.id.split('_')[1]);

            attachedCount.textContent = items.length;
            attachedCount.className = items.length > 0 ? 'badge bg-success ms-2' : 'badge bg-secondary ms-2';

            if (items.length === 0) {
                attachedList.innerHTML = '<div class="text-muted small">Még nincs csatolt dokumentum. Kattintson a fenti gombra a hozzáadáshoz.</div>';
            }

            hiddenInput.value = ids.join(',');
        }

        // Helper: safe HTML
        function escapeHtml(text) {
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        function formatDate(dateStr) {
            if (!dateStr) return '-';
            const d = new Date(dateStr);
            return d.toLocaleDateString('hu-HU');
        }

        // Reset on modal close
        pickerModalEl.addEventListener('hidden.bs.modal', function () {
            searchInput.value = '';
            selectAllCheckbox.checked = false;
            updateSelection();
        });
    });
