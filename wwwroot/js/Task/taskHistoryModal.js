(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    var modalEl = document.getElementById('taskHistoryModal');
    if (!modalEl) return;

    var historyTaskTitle = document.getElementById('historyTaskTitle');
    var historyLoading = document.getElementById('historyLoading');
    var historyContent = document.getElementById('historyContent');
    var historyEmpty = document.getElementById('historyEmpty');
    var historyList = document.getElementById('historyList');

    // ✅ ÁLLÍTSD BE A HELYES API ORIGINT (ha UI nem 8081-en fut)
    var API_BASE = (window.TASKS_API_BASE || 'https://localhost:8081').replace(/\/$/, '');

    function esc(s) {
      var d = document.createElement('div');
      d.textContent = s == null ? '' : String(s);
      return d.innerHTML;
    }

    function fmtDateHu(v) {
      if (!v) return '–';
      var d = new Date(v);
      if (isNaN(d.getTime())) return '–';
      return d.toLocaleString('hu-HU', {
        year: 'numeric', month: 'long', day: 'numeric',
        hour: '2-digit', minute: '2-digit'
      });
    }

    function showLoading() {
      if (historyTaskTitle) historyTaskTitle.textContent = '';
      if (historyList) historyList.innerHTML = '';
      if (historyLoading) historyLoading.classList.remove('d-none');
      if (historyContent) historyContent.classList.add('d-none');
      if (historyEmpty) historyEmpty.classList.add('d-none');
    }

    function showMessage(msg, kind) {
      // kind: 'empty' | 'error'
      if (historyLoading) historyLoading.classList.add('d-none');
      if (historyContent) historyContent.classList.remove('d-none');
      if (historyList) historyList.innerHTML = '';

      if (!historyEmpty) return;
      historyEmpty.classList.remove('d-none');

      var p = historyEmpty.querySelector('p');
      if (p) p.textContent = msg || (kind === 'error'
        ? 'Hiba történt az előzmények betöltésekor.'
        : 'Nincs rögzített előzmény ehhez a feladathoz.');
    }

    function showList() {
      if (historyLoading) historyLoading.classList.add('d-none');
      if (historyContent) historyContent.classList.remove('d-none');
      if (historyEmpty) historyEmpty.classList.add('d-none');
    }

    // ✅ Event
    window.addEventListener('tasks:openHistory', async function (e) {
      var taskId = parseInt(e && e.detail && e.detail.taskId, 10);
      var title = (e && e.detail && e.detail.taskTitle) ? String(e.detail.taskTitle).trim() : '';

      if (!Number.isFinite(taskId) || taskId <= 0) return;

      if (historyTaskTitle) historyTaskTitle.textContent = title || ('Feladat #' + taskId);

      showLoading();
      bootstrap.Modal.getOrCreateInstance(modalEl).show();

      var url = API_BASE + '/api/tasks/' + taskId + '/audit';
      console.log('[taskHistoryModal] GET', url);

      try {
        var res = await fetch(url, {
          method: 'GET',
          headers: { 'Accept': 'application/json' },
          credentials: 'include' // fontos, ha más origin/port!
        });

        if (!res.ok) {
          var txt = await res.text().catch(function () { return ''; });
          console.error('[taskHistoryModal] HTTP', res.status, txt);
          showMessage('Nem sikerült betölteni az előzményeket. HTTP ' + res.status, 'error');
          return;
        }

        var data = await res.json().catch(function () { return null; });
        var items = Array.isArray(data) ? data : [];

        if (!items.length) {
          showMessage('Nincs rögzített előzmény ehhez a feladathoz.', 'empty');
          return;
        }

        // newest first
        items.sort(function (a, b) {
          return new Date(b.changedAt || b.ChangedAt) - new Date(a.changedAt || a.ChangedAt);
        });

        showList();

        items.forEach(function (h) {
          var who = h.changedByName || h.ChangedByName || 'Rendszer';
          var when = fmtDateHu(h.changedAt || h.ChangedAt);
          var action = h.action || h.Action || '';
          var changes = h.changes || h.Changes || '';

          var item = document.createElement('div');
          item.className = 'timeline-item mb-4';
          item.innerHTML = `
            <div class="d-flex">
              <div class="timeline-dot bg-info me-3 mt-1"></div>
              <div class="flex-grow-1">
                <div class="fw-medium text-primary">${esc(who)}</div>
                <div class="text-muted small">${esc(when)}${action ? ' • ' + esc(action) : ''}</div>
                <div class="mt-2 text-dark">${esc(changes)}</div>
              </div>
            </div>
          `;
          if (historyList) historyList.appendChild(item);
        });

      } catch (err) {
        console.error('[taskHistoryModal] fetch exception', err);
        showMessage('Hiba történt az előzmények betöltésekor.', 'error');
      }
    });
  });
})();
