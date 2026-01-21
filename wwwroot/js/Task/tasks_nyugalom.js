// wwwroot/js/tasks_nyugalom.js
// NYUGALOM modul: status / priority quick change + history
// ✅ NINCS table click handler itt
// ✅ csak event-alapú nyitás (tasks:openStatus / tasks:openPriority / tasks:openHistory)

document.addEventListener('DOMContentLoaded', () => {
  console.log('%c[NYUGALOM] JS aktív (event-alap, table click nélkül)', 'color:#9c27b0;font-weight:bold;font-size:14px');

  // ====================== STÁTUSZ GYORS MÓDOSÍTÁS ======================
  const changeStatusModalEl = document.getElementById('changeStatusModal');
  const currentStatusBadge = document.getElementById('currentStatusBadge');
  const newStatusSelect = document.getElementById('newStatusSelect');
  const saveStatusBtn = document.getElementById('saveStatusBtn');

  let currentTaskId = null;

  function showToastSafe(msg, type) {
    if (typeof window.showToast === 'function') return window.showToast(msg, type);
    alert(msg);
  }

  function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  // ====================== KOMMUNIKÁCIÓS MÓD SELECT (TaskPMcomMethod) ======================
  // Egyszerű <select> feltöltése (TomSelect nélkül)
  // Endpoint: GET /api/tasks/taskpm-communication-methods/select
  // Válasz: [{ id: 1, text: "Mobiltelefon" }, ...]
  let _comMethodsCache = null;

  async function loadComMethods() {
    if (_comMethodsCache) return _comMethodsCache;

    const res = await fetch('/api/tasks/taskpm-communication-methods/select', { method: 'GET' });
    if (!res.ok) throw new Error('Kommunikációs módok betöltése sikertelen (HTTP ' + res.status + ')');

    _comMethodsCache = await res.json();
    return _comMethodsCache;
  }

  function fillSimpleSelect(selectEl, items, selectedId) {
    if (!selectEl) return;

    const placeholderText = selectEl.getAttribute('data-placeholder') || '-- Válasszon --';
    selectEl.innerHTML = '';

    const opt0 = document.createElement('option');
    opt0.value = '';
    opt0.textContent = placeholderText;
    selectEl.appendChild(opt0);

    (items || []).forEach(x => {
      const opt = document.createElement('option');
      opt.value = String(x.id);
      opt.textContent = x.text;
      selectEl.appendChild(opt);
    });

    if (selectedId != null && selectedId !== '') {
      selectEl.value = String(selectedId);
    }
  }

  // Create modal: #newTaskModal select#TaskPMcomMethodID
  const newTaskModalEl = document.getElementById('newTaskModal');
  if (newTaskModalEl) {
    newTaskModalEl.addEventListener('shown.bs.modal', async () => {
      try {
        const selectEl = newTaskModalEl.querySelector('#TaskPMcomMethodID, select[name="TaskPMcomMethodID"]');
        if (!selectEl) return;

        // csak egyszer töltsük fel a DOM-ba
        if (selectEl.dataset.loaded === '1') return;

        const items = await loadComMethods();
        fillSimpleSelect(selectEl, items);
        selectEl.dataset.loaded = '1';
      } catch (err) {
        console.error('[NYUGALOM] Communication methods load failed (create)', err);
        showToastSafe('Nem sikerült betölteni a kommunikációs módokat.', 'danger');
      }
    });

    // opcionális: ha mindig frisset akarsz, állítsd vissza a loaded flaget hide-kor
    newTaskModalEl.addEventListener('hidden.bs.modal', () => {
      const selectEl = newTaskModalEl.querySelector('#TaskPMcomMethodID, select[name="TaskPMcomMethodID"]');
      if (selectEl) {
        // selectEl.dataset.loaded = '0';
      }
    });
  }

  // Edit támogatás: kívülről meghívható helper (ha az edit script betölt taskot és setelni akar)
  // Példa:
  //   const sel = document.querySelector('#EditTaskPMcomMethodID');
  //   await window.TaskPmComMethod.ensureLoadedAndSet(sel, task.taskPMcomMethodID);
  window.TaskPmComMethod = window.TaskPmComMethod || {};
  window.TaskPmComMethod.ensureLoadedAndSet = async function (selectEl, selectedId) {
    const items = await loadComMethods();
    fillSimpleSelect(selectEl, items, selectedId);
    if (selectEl) selectEl.dataset.loaded = '1';
  };

  // ✅ NYITÁS EVENT-RE (badge elemmel)
  window.addEventListener('tasks:openStatus', function (e) {
    const badge = e?.detail?.badgeEl;
    const taskId = parseInt(e?.detail?.taskId, 10);

    if (!changeStatusModalEl || !badge) return;
    if (!Number.isFinite(taskId) || taskId <= 0) return;

    currentTaskId = taskId;

    // reset
    if (currentStatusBadge) {
      currentStatusBadge.textContent = '';
      currentStatusBadge.style.backgroundColor = '';
    }
    if (newStatusSelect) newStatusSelect.value = '';

    // fill from clicked badge
    if (currentStatusBadge) {
      currentStatusBadge.textContent = (badge.textContent || '').trim();
      currentStatusBadge.style.backgroundColor = badge.style.backgroundColor || '';
    }

    const currentStatusId = badge.dataset.statusId || '';
    if (currentStatusId && newStatusSelect) {
      newStatusSelect.value = currentStatusId;

      const selectedOption = newStatusSelect.options[newStatusSelect.selectedIndex];
      if (selectedOption && currentStatusBadge) {
        currentStatusBadge.style.backgroundColor = selectedOption.dataset.color || '#6c757d';
      }
    }

    bootstrap.Modal.getOrCreateInstance(changeStatusModalEl).show();
  });

  if (newStatusSelect) {
    newStatusSelect.addEventListener('change', function () {
      const selected = this.options[this.selectedIndex];
      const color = selected?.dataset?.color || '#6c757d';
      if (currentStatusBadge) currentStatusBadge.style.backgroundColor = color;
    });
  }

  if (saveStatusBtn) {
    saveStatusBtn.addEventListener('click', async () => {
      if (!currentTaskId) return;

      const newStatusId = parseInt(newStatusSelect?.value || '', 10);
      if (Number.isNaN(newStatusId)) {
        showToastSafe('Érvénytelen státusz', 'danger');
        return;
      }

      try {
        const response = await fetch('/api/nyugalom/taskstatuses/change', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getToken()
          },
          body: JSON.stringify({ taskId: currentTaskId, newStatusId })
        });

        const result = await response.json().catch(() => ({}));

        if (result.success) {
          const badge = document.querySelector(`tr[data-task-id="${currentTaskId}"] .clickable-status-badge`);
          if (badge) {
            badge.textContent = result.statusName;
            badge.style.backgroundColor = result.colorCode;
            badge.dataset.statusId = String(newStatusId);
          }

          bootstrap.Modal.getInstance(changeStatusModalEl)?.hide();
          showToastSafe(`Státusz módosítva: ${result.statusName}`, 'success');
        } else {
          showToastSafe('Hiba: ' + (result.message || 'Ismeretlen hiba'), 'danger');
        }
      } catch (err) {
        console.error('Státusz módosítás hiba:', err);
        showToastSafe('Nem sikerült a módosítás', 'danger');
      }
    });
  }

  if (changeStatusModalEl) {
    changeStatusModalEl.addEventListener('hidden.bs.modal', () => {
      if (currentStatusBadge) {
        currentStatusBadge.textContent = '';
        currentStatusBadge.style.backgroundColor = '';
      }
      if (newStatusSelect) newStatusSelect.value = '';
      currentTaskId = null;
    });
  }

  // ====================== PRIORITÁS GYORS MÓDOSÍTÁS ======================
  const changePriorityModalEl = document.getElementById('changePriorityModal');
  const currentPriorityBadge = document.getElementById('currentPriorityBadge');
  const newPrioritySelect = document.getElementById('newPrioritySelect');
  const savePriorityBtn = document.getElementById('savePriorityBtn');

  let currentTaskIdForPriority = null;

  // ✅ NYITÁS EVENT-RE (badge elemmel)
  window.addEventListener('tasks:openPriority', function (e) {
    const badge = e?.detail?.badgeEl;
    const taskId = parseInt(e?.detail?.taskId, 10);

    if (!changePriorityModalEl || !badge) return;
    if (!Number.isFinite(taskId) || taskId <= 0) return;

    currentTaskIdForPriority = taskId;

    // reset
    if (currentPriorityBadge) {
      currentPriorityBadge.textContent = '';
      currentPriorityBadge.style.backgroundColor = '';
    }
    if (newPrioritySelect) newPrioritySelect.value = '';

    // fill from clicked badge
    if (currentPriorityBadge) {
      currentPriorityBadge.textContent = (badge.textContent || '').trim();
      currentPriorityBadge.style.backgroundColor = badge.style.backgroundColor || '';
    }

    const currentPriorityId = badge.dataset.priorityId || '';
    if (currentPriorityId && newPrioritySelect) {
      newPrioritySelect.value = currentPriorityId;

      const selectedOption = newPrioritySelect.options[newPrioritySelect.selectedIndex];
      if (selectedOption && currentPriorityBadge) {
        currentPriorityBadge.style.backgroundColor = selectedOption.dataset.color || '#82D4BB';
      }
    }

    bootstrap.Modal.getOrCreateInstance(changePriorityModalEl).show();
  });

  if (newPrioritySelect) {
    newPrioritySelect.addEventListener('change', function () {
      const selected = this.options[this.selectedIndex];
      const color = selected?.dataset?.color || '#82D4BB';
      if (currentPriorityBadge) currentPriorityBadge.style.backgroundColor = color;
    });
  }

  if (savePriorityBtn) {
    savePriorityBtn.addEventListener('click', async () => {
      if (!currentTaskIdForPriority) return;

      const newPriorityId = parseInt(newPrioritySelect?.value || '', 10);
      if (Number.isNaN(newPriorityId)) {
        showToastSafe('Érvénytelen prioritás', 'danger');
        return;
      }

      try {
        const response = await fetch('/api/nyugalom/taskpriorities/change', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getToken()
          },
          body: JSON.stringify({ taskId: currentTaskIdForPriority, newPriorityId })
        });

        const result = await response.json().catch(() => ({}));

        if (result.success) {
          const badge = document.querySelector(`tr[data-task-id="${currentTaskIdForPriority}"] .clickable-priority-badge`);
          if (badge) {
            badge.textContent = result.priorityName;
            badge.style.backgroundColor = result.colorCode || '#6c757d';
            badge.dataset.priorityId = String(newPriorityId);
          }

          bootstrap.Modal.getInstance(changePriorityModalEl)?.hide();
          showToastSafe(`Prioritás módosítva: ${result.priorityName}`, 'success');
        } else {
          showToastSafe('Hiba: ' + (result.message || 'Ismeretlen hiba'), 'danger');
        }
      } catch (err) {
        console.error('Prioritás módosítás hiba:', err);
        showToastSafe('Nem sikerült a módosítás', 'danger');
      }
    });
  }

  if (changePriorityModalEl) {
    changePriorityModalEl.addEventListener('hidden.bs.modal', () => {
      if (currentPriorityBadge) {
        currentPriorityBadge.textContent = '';
        currentPriorityBadge.style.backgroundColor = '';
      }
      if (newPrioritySelect) newPrioritySelect.value = '';
      currentTaskIdForPriority = null;
    });
  }

  // ====================== FELADAT ELŐZMÉNYEK MEGJELENÍTÉSE ======================
  const taskHistoryModalEl = document.getElementById('taskHistoryModal');
  const historyTaskTitle = document.getElementById('historyTaskTitle');
  const historyLoading = document.getElementById('historyLoading');
  const historyContent = document.getElementById('historyContent');
  const historyEmpty = document.getElementById('historyEmpty');
  const historyList = document.getElementById('historyList');

  // ✅ NYITÁS EVENT-RE
  window.addEventListener('tasks:openHistory', function (e) {
    const taskId = parseInt(e?.detail?.taskId, 10);
    const taskTitle = (e?.detail?.taskTitle || 'Ismeretlen feladat').trim();

    if (!taskHistoryModalEl) return;
    if (!Number.isFinite(taskId) || taskId <= 0) return;

    if (historyTaskTitle) historyTaskTitle.textContent = taskTitle;
    if (historyLoading) historyLoading.classList.remove('d-none');
    if (historyContent) historyContent.classList.add('d-none');
    if (historyEmpty) historyEmpty.classList.add('d-none');
    if (historyList) historyList.innerHTML = '';

    bootstrap.Modal.getOrCreateInstance(taskHistoryModalEl).show();

    fetch(`/api/tasks/${taskId}/history`)
      .then(response => {
        if (!response.ok) throw new Error(`HTTP hiba: ${response.status}`);
        return response.json();
      })
      .then(data => {
        if (historyLoading) historyLoading.classList.add('d-none');
        if (historyContent) historyContent.classList.remove('d-none');

        const histories = Array.isArray(data) ? data : [];
        if (histories.length === 0) {
          if (historyEmpty) historyEmpty.classList.remove('d-none');
          return;
        }

        histories.sort((a, b) =>
          new Date(b.modifiedDate || b.ModifiedDate) - new Date(a.modifiedDate || a.ModifiedDate)
        );

        histories.forEach(h => {
          const item = document.createElement('div');
          item.className = 'timeline-item mb-4';

          const date = new Date(h.modifiedDate || h.ModifiedDate || new Date());
          const formattedDate = date.toLocaleString('hu-HU', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
          });

          const userName = h.modifiedByName || h.ModifiedByName || h.modifiedById || 'Rendszer';
          const description = h.changeDescription || h.ChangeDescription || 'Nincs leírás';

          item.innerHTML = `
            <div class="d-flex">
              <div class="timeline-dot bg-info me-3 mt-1"></div>
              <div class="flex-grow-1">
                <div class="fw-medium text-primary">${userName}</div>
                <div class="text-muted small">${formattedDate}</div>
                <div class="mt-2 text-dark">${description}</div>
              </div>
            </div>
          `;

          if (historyList) historyList.appendChild(item);
        });
      })
      .catch(err => {
        console.error('Előzmények betöltési hiba:', err);
        if (historyLoading) historyLoading.classList.add('d-none');
        if (historyContent) historyContent.classList.remove('d-none');
        if (historyEmpty) {
          historyEmpty.classList.remove('d-none');
          const p = historyEmpty.querySelector('p');
          if (p) p.textContent = 'Hiba történt az előzmények betöltésekor.';
        }
      });
  });
});
