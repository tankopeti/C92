// /wwwroot/js/Task/taskDelete.js
(function () {
  'use strict';

  console.log('[taskDelete] init');

  let isDeleting = false;
  let pendingId = null;

  function ensureModal() {
    let modalEl = document.getElementById('taskDeleteConfirmModal');
    if (modalEl) return modalEl;

    const wrapper = document.createElement('div');
    wrapper.innerHTML = `
<div class="modal fade" id="taskDeleteConfirmModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Feladat törlése</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div class="alert alert-warning mb-3">
          Biztosan törlöd ezt a feladatot?
        </div>
        <div class="small text-muted">
          Task ID: <span id="taskDeleteIdLabel"></span>
        </div>
        <div id="taskDeleteError" class="text-danger small mt-2 d-none"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-outline-secondary" data-bs-dismiss="modal">Mégse</button>
        <button class="btn btn-danger" id="taskDeleteConfirmBtn">Törlés</button>
      </div>
    </div>
  </div>
</div>`;
    document.body.appendChild(wrapper.firstElementChild);
    return document.getElementById('taskDeleteConfirmModal');
  }

  function removeRow(id) {
    const row = document.querySelector(`tr[data-task-id="${CSS.escape(String(id))}"]`);
    if (row) row.remove();
  }

  async function deleteTask(id) {
    const res = await fetch(`/api/tasks/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers: { 'Accept': 'application/json' }
    });

    if (res.status !== 204 && !res.ok) {
      const txt = await res.text().catch(() => '');
      throw new Error(txt || `HTTP ${res.status}`);
    }
  }

  function openModal(id) {
    pendingId = id;
    ensureModal();
    document.getElementById('taskDeleteIdLabel').textContent = id;
    bootstrap.Modal.getOrCreateInstance(
      document.getElementById('taskDeleteConfirmModal')
    ).show();
  }

  async function onConfirm() {
    if (isDeleting) return;
    isDeleting = true;

    try {
      await deleteTask(pendingId);
      removeRow(pendingId);

      bootstrap.Modal.getOrCreateInstance(
        document.getElementById('taskDeleteConfirmModal')
      ).hide();

      // ✅ NEW: finom, reload nélküli értesítés
      window.dispatchEvent(new CustomEvent('tasks:deleted', {
        detail: { id: pendingId }
      }));

    } catch (e) {
      console.error(e);
      document.getElementById('taskDeleteError').textContent = 'Törlés sikertelen';
      document.getElementById('taskDeleteError').classList.remove('d-none');
    } finally {
      isDeleting = false;
    }
  }

  window.addEventListener('tasks:openDelete', e => {
    const id = Number(e.detail?.id);
    if (!Number.isFinite(id)) return;
    openModal(id);
  });

  document.addEventListener('click', e => {
    if (e.target.closest('#taskDeleteConfirmBtn')) onConfirm();
  });
})();
