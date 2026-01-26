(function () {
  'use strict';

  console.log('[taskBejelentesHistory] file loaded');

  const modalEl = document.getElementById('taskHistoryModal');
  if (!modalEl) {
    console.warn('[taskBejelentesHistory] modal not found (#taskHistoryModal)');
    return;
  }

  const elLoading = document.getElementById('historyLoading');
  const elContent = document.getElementById('historyContent');
  const elTitle = document.getElementById('historyTaskTitle');
  const elEmpty = document.getElementById('historyEmpty');
  const elList = document.getElementById('historyList');

  if (!elLoading || !elContent || !elTitle || !elEmpty || !elList) {
    console.warn('[taskBejelentesHistory] missing inner elements', {
      elLoading: !!elLoading, elContent: !!elContent, elTitle: !!elTitle, elEmpty: !!elEmpty, elList: !!elList
    });
    return;
  }

  const cache = new Map();

  function setVisible(node, visible) {
    if (!node) return;
    node.classList.toggle('d-none', !visible);
  }

  function showLoading() {
    setVisible(elLoading, true);
    setVisible(elContent, false);
  }

  function showContent() {
    setVisible(elLoading, false);
    setVisible(elContent, true);
  }

  function showEmpty(isEmpty) {
    setVisible(elEmpty, isEmpty);
  }

  function esc(str) {
    const d = document.createElement('div');
    d.textContent = str == null ? '' : String(str);
    return d.innerHTML;
  }

  function formatHuDate(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    if (isNaN(d.getTime())) return String(iso);
    return d.toLocaleString('hu-HU', {
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit'
    }).replace(',', '');
  }

  function metaForAction(action) {
    const a = (action || '').toLowerCase();
    if (a === 'created') return { icon: 'bi-plus-circle-fill', badge: 'bg-success', label: 'Létrehozva' };
    if (a === 'updated') return { icon: 'bi-pencil-square', badge: 'bg-primary', label: 'Módosítva' };
    if (a === 'deleted') return { icon: 'bi-trash-fill', badge: 'bg-danger', label: 'Törölve' };
    return { icon: 'bi-info-circle-fill', badge: 'bg-secondary', label: action || 'Esemény' };
  }

  function clearList() {
    elList.innerHTML = '';
  }

  function renderError(msg) {
    elList.innerHTML = `
      <div class="alert alert-danger d-flex align-items-start gap-2 mb-0" role="alert">
        <i class="bi bi-exclamation-triangle fs-5"></i>
        <div>
          <div class="fw-semibold">Nem sikerült betölteni az előzményeket.</div>
          <div class="small">${esc(msg || 'Ismeretlen hiba')}</div>
        </div>
      </div>
    `;
  }

  function render(items) {
    console.log('[taskBejelentesHistory] render items:', items);

    if (!Array.isArray(items) || items.length === 0) {
      clearList();
      showEmpty(true);
      return;
    }

    showEmpty(false);

    const rows = items.map(x => {
      const m = metaForAction(x.action);
      const when = formatHuDate(x.changedAt);
      const who = x.changedByName ? esc(x.changedByName) : '—';
      const changes = x.changes ? esc(x.changes) : '—';

      return `
        <div class="list-group-item py-3">
          <div class="d-flex align-items-start gap-3">
            <span class="badge ${m.badge} rounded-pill p-2 mt-1">
              <i class="bi ${m.icon}"></i>
            </span>

            <div class="flex-grow-1">
              <div class="d-flex flex-wrap align-items-center gap-2">
                <span class="fw-semibold">${esc(m.label)}</span>
                <span class="text-muted small">• ${esc(when)}</span>
                <span class="text-muted small">• ${who}</span>
              </div>

              <div class="mt-2 small text-body" style="white-space: pre-wrap;">
                ${changes}
              </div>
            </div>
          </div>
        </div>
      `;
    }).join('');

    elList.innerHTML = `<div class="list-group">${rows}</div>`;
  }

async function fetchAudit(taskId) {
  const url = `/api/tasks/${encodeURIComponent(taskId)}/audit?_=${Date.now()}`; // cache-bust
  console.log('[taskBejelentesHistory] fetch:', url);

  const res = await fetch(url, {
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const txt = await res.text().catch(() => '');
    throw new Error(`HTTP ${res.status} ${res.statusText}${txt ? ' – ' + txt : ''}`);
  }

  return await res.json();
}


  function openModal() {
    try {
      let inst = bootstrap.Modal.getInstance(modalEl);
      if (!inst) inst = new bootstrap.Modal(modalEl);
      inst.show();
    } catch (e) {
      console.warn('[taskBejelentesHistory] bootstrap modal open failed', e);
    }
  }

  async function load(taskId) {
    elTitle.textContent = taskId ? `Feladat #${taskId}` : '';
    clearList();
    showLoading();
    showEmpty(false);

    try {
      const items = await fetchAudit(taskId);
      showContent();
      render(items);
    } catch (e) {
      showContent();
      showEmpty(false);
      renderError(e?.message || String(e));
      console.error('[taskBejelentesHistory] load failed:', e);
    }
  }

  // ✅ Event from taskBejelentesLoadMore.js
  window.addEventListener('tasks:history', function (e) {
    console.log('[taskBejelentesHistory] tasks:history event:', e);

    const id = e?.detail?.id ?? e?.detail?.taskId;
    const taskId = parseInt(id, 10);

    console.log('[taskBejelentesHistory] parsed taskId:', taskId);

    if (!Number.isFinite(taskId) || taskId <= 0) return;

    openModal();
    load(taskId);
  });

  // reset on close
  modalEl.addEventListener('hidden.bs.modal', function () {
    elTitle.textContent = '';
    clearList();
    showLoading();
    showEmpty(false);
  });

})();
