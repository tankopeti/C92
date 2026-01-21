// wwwroot/js/fileattach.js
document.addEventListener('DOMContentLoaded', function () {

  // ============================================================
  // Reusable attach initializer (Create + Edit)
  // ============================================================
  function initFileAttach(opts) {

    const attachBtn = document.getElementById(opts.attachBtnId);
    const pickerModalEl = document.getElementById(opts.pickerModalId || 'documentPickerModal');

    if (!attachBtn || !pickerModalEl) {
      console.warn('fileattach.js: hiányzó attachBtn vagy picker modal – skip', opts);
      return;
    }

    const pickerModal = bootstrap.Modal.getOrCreateInstance(pickerModalEl);

    const confirmBtn = document.getElementById(opts.confirmBtnId || 'confirmAttachDocuments');
    const searchInput = document.getElementById(opts.searchInputId || 'documentSearch');
    const selectAllCheckbox = document.getElementById(opts.selectAllId || 'selectAllDocs');
    const tableBody = document.getElementById(opts.tableBodyId || 'documentsTableBody');
    const loadingDiv = document.getElementById(opts.loadingDivId || 'documentsLoading');

    const attachedList = document.getElementById(opts.attachedListId);
    const attachedCount = document.getElementById(opts.attachedCountId);
    const hiddenInput = document.getElementById(opts.hiddenInputId);

    if (!confirmBtn || !searchInput || !selectAllCheckbox || !tableBody ||
        !loadingDiv || !attachedList || !attachedCount || !hiddenInput) {
      console.warn('fileattach.js: hiányzó belső elem – skip', opts);
      return;
    }

    let allDocuments = [];
    let selectedDocumentIds = new Set();

    // ------------------------------------------------------------
    // Open picker
    // ------------------------------------------------------------
    attachBtn.addEventListener('click', function () {
      loadDocuments();
      pickerModal.show();
    });

    // ------------------------------------------------------------
    // Load documents
    // ------------------------------------------------------------
    async function loadDocuments() {
      loadingDiv.style.display = 'block';
      tableBody.innerHTML = '';
      selectedDocumentIds.clear();
      confirmBtn.disabled = true;

      try {
        const response = await fetch('/api/tasks/documents/picker?take=200', {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });

        if (!response.ok)
          throw new Error('Dokumentumok betöltése sikertelen (HTTP ' + response.status + ')');

        const data = await response.json();
        allDocuments = Array.isArray(data) ? data : [];
        renderDocuments(allDocuments);

      } catch (err) {
        tableBody.innerHTML = `
          <tr>
            <td colspan="5" class="text-center text-danger py-4">
              ${escapeHtml(err.message)}
            </td>
          </tr>`;
      } finally {
        loadingDiv.style.display = 'none';
      }
    }

    // ------------------------------------------------------------
    // Render table
    // ------------------------------------------------------------
    function renderDocuments(docs) {
      if (!Array.isArray(docs) || docs.length === 0) {
        tableBody.innerHTML =
          '<tr><td colspan="5" class="text-center text-muted py-4">Nincs elérhető dokumentum</td></tr>';
        return;
      }

      tableBody.innerHTML = docs.map(doc => {
        const id = Number(doc.id ?? doc.documentId ?? doc.DocumentId);
        const fileName = doc.fileName ?? doc.FileName ?? 'Nincs név';

        return `
          <tr>
            <td>
              <input class="form-check-input document-checkbox"
                     type="checkbox"
                     value="${id}">
            </td>
            <td><strong>${escapeHtml(fileName)}</strong></td>
          </tr>`;
      }).join('');

      tableBody.querySelectorAll('.document-checkbox').forEach(cb => {
        cb.addEventListener('change', updateSelection);
      });

      selectAllCheckbox.onchange = toggleSelectAll;
    }

    // ------------------------------------------------------------
    // Search
    // ------------------------------------------------------------
    searchInput.addEventListener('input', function () {
      const term = (this.value || '').toLowerCase();
      const filtered = allDocuments.filter(doc =>
        (doc.fileName ?? doc.FileName ?? '').toLowerCase().includes(term)
      );
      renderDocuments(filtered);
    });

    // ------------------------------------------------------------
    // Selection
    // ------------------------------------------------------------
    function toggleSelectAll() {
      const checked = selectAllCheckbox.checked;
      tableBody.querySelectorAll('.document-checkbox').forEach(cb => {
        cb.checked = checked;
      });
      updateSelection();
    }

    function updateSelection() {
      selectedDocumentIds.clear();

      tableBody.querySelectorAll('.document-checkbox:checked').forEach(cb => {
        const id = Number(cb.value);
        if (Number.isFinite(id)) selectedDocumentIds.add(id);
      });

      confirmBtn.disabled = selectedDocumentIds.size === 0;
      confirmBtn.textContent = selectedDocumentIds.size > 0
        ? `Kiválasztottak csatolása (${selectedDocumentIds.size})`
        : 'Kiválasztottak csatolása';
    }

    // ------------------------------------------------------------
    // Confirm attach
    // ------------------------------------------------------------
    confirmBtn.addEventListener('click', function () {
      if (selectedDocumentIds.size === 0) return;

      const chosenIds = Array.from(selectedDocumentIds);

      allDocuments
        .filter(doc => chosenIds.includes(Number(doc.id ?? doc.documentId ?? doc.DocumentId)))
        .forEach(doc => {
          const docId = Number(doc.id ?? doc.documentId ?? doc.DocumentId);
          if (!Number.isFinite(docId)) return;

          if (attachedList.querySelector('[data-doc-id="' + docId + '"]')) return;

          const fileName = doc.fileName ?? doc.FileName ?? 'Nincs név';

          const item = document.createElement('div');
          item.className =
            'd-flex justify-content-between align-items-center p-2 mb-2 bg-white border rounded shadow-sm';
          item.dataset.docId = String(docId);

          item.innerHTML = `
            <div>
              <i class="bi bi-file-earmark-text me-2 text-primary"></i>
              <strong>${escapeHtml(fileName)}</strong>
            </div>
            <button type="button"
                    class="btn btn-sm btn-outline-danger"
                    data-remove-doc="${docId}">
              <i class="bi bi-x"></i>
            </button>
          `;

          attachedList.appendChild(item);
        });

      updateAttachedCount();

      // 🔔 értesítjük az Edit/Create logikát
      window.dispatchEvent(new CustomEvent('documents:selected', {
        detail: {
          ids: chosenIds,
          source: opts.namespace
        }
      }));

      pickerModal.hide();
    });

    // ------------------------------------------------------------
    // Remove attached (delegált)
    // ------------------------------------------------------------
    attachedList.addEventListener('click', function (e) {
      const btn = e.target.closest('[data-remove-doc]');
      if (!btn) return;

      const item = btn.closest('[data-doc-id]');
      if (item) item.remove();

      updateAttachedCount();
    });

    // ------------------------------------------------------------
    // Update count + hidden input
    // ------------------------------------------------------------
    function updateAttachedCount() {
      const items = attachedList.querySelectorAll('[data-doc-id]');
      const ids = Array.from(items).map(x => x.dataset.docId);

      attachedCount.textContent = String(items.length);
      attachedCount.className = items.length > 0
        ? 'badge bg-success ms-2'
        : 'badge bg-secondary ms-2';

      if (items.length === 0) {
        attachedList.innerHTML =
          '<div class="text-muted small">Még nincs csatolt dokumentum.</div>';
      }

      hiddenInput.value = ids.join(',');
    }

    // ------------------------------------------------------------
    // Utils
    // ------------------------------------------------------------
    function escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text ?? '';
      return div.innerHTML;
    }

    pickerModalEl.addEventListener('hidden.bs.modal', function () {
      searchInput.value = '';
      selectAllCheckbox.checked = false;
      selectedDocumentIds.clear();
      confirmBtn.disabled = true;
    });
  }

  // ============================================================
  // INIT CREATE
  // ============================================================
  initFileAttach({
    namespace: 'create',
    attachBtnId: 'attachDocumentsBtn',
    attachedListId: 'attachedDocumentsList',
    attachedCountId: 'attachedCount',
    hiddenInputId: 'attachedDocumentIdsInput',
    pickerModalId: 'documentPickerModal'
  });

  // ============================================================
  // INIT EDIT
  // ============================================================
  initFileAttach({
    namespace: 'edit',
    attachBtnId: 'editAttachDocumentsBtn',
    attachedListId: 'editAttachedDocumentsList',
    attachedCountId: 'editAttachedCount',
    hiddenInputId: 'editAttachedDocumentIdsInput',
    pickerModalId: 'documentPickerModal'
  });

});
