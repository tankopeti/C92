// /js/Doc/documentHistory.js
document.addEventListener('DOMContentLoaded', function () {
  console.log('documentHistory.js BETÖLTÖDÖTT');

  const modalEl = document.getElementById('documentHistoryModal');
  if (!modalEl) return;

  const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl, { focus: false });

  const el = {
    docIdLabel: document.getElementById('historyDocIdLabel'),
    list: document.getElementById('docHistoryList'),
    loading: document.getElementById('docHistoryLoading'),
    empty: document.getElementById('docHistoryEmpty'),
    error: document.getElementById('docHistoryErrorBox'),
    refreshBtn: document.getElementById('refreshDocHistoryBtn')
  };

  let currentDocId = null;

  function show(elm) { elm?.classList.remove('d-none'); }
  function hide(elm) { elm?.classList.add('d-none'); }

  function formatDateTime(iso) {
    if (!iso) return '';
    try {
      // hu-HU dátum/idő
      return new Date(iso).toLocaleString('hu-HU');
    } catch {
      return iso;
    }
  }

  function escapeHtml(s) {
    return String(s ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function badgeClass(action) {
    const a = String(action || '').toLowerCase();
    if (a.includes('create')) return 'bg-success';
    if (a.includes('delete')) return 'bg-danger';
    if (a.includes('update')) return 'bg-primary';
    return 'bg-secondary';
  }

  function renderItem(item) {
    const changedAt = formatDateTime(item.changedAt ?? item.ChangedAt);
    const by = item.changedByName ?? item.ChangedByName ?? '—';
    const action = item.action ?? item.Action ?? '—';
    const changes = item.changes ?? item.Changes ?? '';

    return `
      <div class="list-group-item">
        <div class="d-flex justify-content-between align-items-start gap-2">
          <div>
            <div class="fw-semibold">${escapeHtml(by)}</div>
            <div class="text-muted small">${escapeHtml(changedAt)}</div>
          </div>
          <span class="badge ${badgeClass(action)}">${escapeHtml(action)}</span>
        </div>
        <div class="mt-2">
          <div class="small">${escapeHtml(changes) || '<span class="text-muted">—</span>'}</div>
        </div>
      </div>
    `;
  }

  async function fetchHistory(docId) {
    // 🔧 itt állítsd át, ha más az endpointod
    const url = `/api/audit?entityType=Document&entityId=${encodeURIComponent(docId)}`;

    const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  }

  async function loadHistory(docId) {
    currentDocId = docId;
    el.docIdLabel.textContent = docId ? String(docId) : '—';

    hide(el.error);
    hide(el.empty);
    el.list.innerHTML = '';
    show(el.loading);

    try {
      const data = await fetchHistory(docId);
      hide(el.loading);

      const items = Array.isArray(data) ? data : (data.items || data.Items || []);
      if (!items || items.length === 0) {
        show(el.empty);
        return;
      }

      el.list.innerHTML = items.map(renderItem).join('');
    } catch (err) {
      console.error('History load failed:', err);
      hide(el.loading);
      show(el.error);
    }
  }

  // Dropdown click (delegation)
  document.addEventListener('click', (e) => {
    const btn = e.target.closest('.document-history-btn');
    if (!btn) return;

    e.preventDefault();

    const docId = btn.dataset.documentId;
    if (!docId) return;

    loadHistory(docId);
    bsModal.show();
  });

  // Refresh
  el.refreshBtn?.addEventListener('click', (e) => {
    e.preventDefault();
    if (!currentDocId) return;
    loadHistory(currentDocId);
  });

  // Cleanup
  modalEl.addEventListener('hidden.bs.modal', () => {
    currentDocId = null;
    el.docIdLabel.textContent = '—';
    el.list.innerHTML = '';
    hide(el.loading);
    hide(el.empty);
    hide(el.error);
  });
});
