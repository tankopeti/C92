// wwwroot/js/Task/taskDocumentsModal.js
(function () {
  'use strict';

  console.log('[taskDocumentsModal] file loaded');

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskDocumentsModal] DOM loaded');

    var modalEl = document.getElementById('taskDocumentsModal');
    if (!modalEl) {
      console.warn('[taskDocumentsModal] #taskDocumentsModal not found');
      return;
    }

    // Elements
    var taskIdEl = document.getElementById('taskDocsTaskId');
    var listEl = document.getElementById('taskDocsList');
    var countEl = document.getElementById('taskDocsCount');

    var addBtn = document.getElementById('taskDocsAddBtn');
    var fileInput = document.getElementById('taskDocsFileInput');
    var noteEl = document.getElementById('taskDocsNote');

    var busy = false;

    // -----------------------------
    // Helpers
    // -----------------------------
    function escHtml(s) {
      var div = document.createElement('div');
      div.textContent = s == null ? '' : String(s);
      return div.innerHTML;
    }

    function pick(obj, keys) {
      for (var i = 0; i < keys.length; i++) {
        var k = keys[i];
        if (obj && obj[k] !== undefined && obj[k] !== null) return obj[k];
      }
      return null;
    }

    function toast(message, type) {
      type = type || 'info';
      var container = document.getElementById('toastContainer');
      if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '1100';
        document.body.appendChild(container);
      }

      var t = document.createElement('div');
      t.className = 'toast align-items-center text-white bg-' + type + ' border-0';
      t.innerHTML =
        '<div class="d-flex">' +
        '  <div class="toast-body">' + message + '</div>' +
        '  <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
        '</div>';

      container.appendChild(t);
      new bootstrap.Toast(t, { delay: 3500 }).show();
    }

    function getCsrfToken() {
      var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
      if (tokenInput && tokenInput.value) return tokenInput.value;

      var meta = document.querySelector('meta[name="csrf-token"]');
      if (meta && meta.content) return meta.content;

      var m = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
      if (m && m[1]) return decodeURIComponent(m[1]);

      return '';
    }

    function setBusy(isBusy) {
      busy = !!isBusy;
      if (addBtn) addBtn.disabled = busy;
      if (listEl) listEl.style.opacity = busy ? '0.6' : '1';
    }

    function getTaskId() {
      var v = taskIdEl ? String(taskIdEl.value || '').trim() : '';
      var n = parseInt(v, 10);
      return isFinite(n) && n > 0 ? n : null;
    }

    function formatDate(v) {
      if (!v) return '';
      try {
        var d = new Date(v);
        if (isNaN(d.getTime())) return String(v);
        return d.toLocaleString('hu-HU').replace(',', '');
      } catch (_) {
        return String(v);
      }
    }

    // -----------------------------
    // Rendering
    // -----------------------------
    function renderList(items) {
      items = Array.isArray(items) ? items : [];

      if (countEl) countEl.textContent = String(items.length);
      if (!listEl) return;

      if (!items.length) {
        listEl.innerHTML = '<div class="text-muted small">Még nincs csatolt fájl a feladathoz.</div>';
        return;
      }

      listEl.innerHTML = items.map(function (a) {
        var linkId = pick(a, ['id', 'Id']); // TaskDocumentLink.Id
        var docId = pick(a, ['documentId', 'DocumentId']);
        var name = pick(a, ['fileName', 'FileName']) || ('#' + docId);
        var path = pick(a, ['filePath', 'FilePath']) || '';
        var note = pick(a, ['note', 'Note']) || '';
        var by = pick(a, ['linkedByName', 'LinkedByName']) || '';
        var dt = pick(a, ['linkedDate', 'LinkedDate']);

        // ha file:// vagy http(s), akkor közvetlen link
        var href = path ? String(path) : '#';

        return (
          '<div class="d-flex justify-content-between align-items-start border rounded p-2 mb-2" data-link-id="' + escHtml(linkId) + '">' +
          '  <div class="me-3">' +
          '    <div class="fw-semibold"><i class="bi bi-file-earmark-text me-2"></i>' +
          '      <a href="' + escHtml(href) + '" target="_blank" rel="noopener">' + escHtml(name) + '</a>' +
          '    </div>' +
          '    <div class="small text-muted">' +
          (by ? ('<span class="me-2">' + escHtml(by) + '</span>') : '') +
          (dt ? ('<span>' + escHtml(formatDate(dt)) + '</span>') : '') +
          '    </div>' +
          (note ? ('<div class="small mt-1"><span class="text-muted">Megj:</span> ' + escHtml(note) + '</div>') : '') +
          '  </div>' +
          '  <button type="button" class="btn btn-outline-danger btn-sm js-task-doc-delete" data-link-id="' + escHtml(linkId) + '">' +
          '    <i class="bi bi-trash"></i>' +
          '  </button>' +
          '</div>'
        );
      }).join('');
    }

    // -----------------------------
    // API calls
    // -----------------------------
    async function apiGetAttachments(taskId) {
      var res = await fetch('/api/tasks/' + encodeURIComponent(taskId) + '/attachments', {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        credentials: 'same-origin'
      });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('GET attachments failed HTTP ' + res.status + ' :: ' + txt);
      }

      return await res.json(); // TaskDocumentDto[]
    }

    async function apiUploadAndAttach(taskId, file, note) {
      var fd = new FormData();
      fd.append('file', file);

      var token = getCsrfToken();
      var headers = token ? { 'RequestVerificationToken': token } : undefined;

      var url = '/api/tasks/' + encodeURIComponent(taskId) + '/documents/upload';
      if (note) url += '?note=' + encodeURIComponent(note);

      var res = await fetch(url, {
        method: 'POST',
        headers: headers,
        credentials: 'same-origin',
        body: fd
      });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('UPLOAD failed HTTP ' + res.status + ' :: ' + txt);
      }

      // javaslat: controller adja vissza a friss attachments listát
      // de ha task DTO-t ad vissza, azt is kezeljük
      var json = await res.json();
      if (Array.isArray(json)) return json;
      return (json.attachments || json.Attachments || []);
    }

    async function apiDeleteAttachment(taskId, linkId) {
      var token = getCsrfToken();
      var headers = token ? { 'RequestVerificationToken': token } : undefined;

      var res = await fetch('/api/tasks/' + encodeURIComponent(taskId) + '/attachments/' + encodeURIComponent(linkId), {
        method: 'DELETE',
        headers: headers,
        credentials: 'same-origin'
      });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('DELETE failed HTTP ' + res.status + ' :: ' + txt);
      }

      var ct = (res.headers.get('content-type') || '').toLowerCase();
      if (ct.indexOf('application/json') >= 0) return await res.json();
      return null;
    }

    async function refresh() {
      var taskId = getTaskId();
      if (!taskId) {
        renderList([]);
        return;
      }

      setBusy(true);
      try {
        var items = await apiGetAttachments(taskId);
        renderList(items);
      } catch (e) {
        console.error('[taskDocumentsModal] refresh failed', e);
        if (listEl) listEl.innerHTML = '<div class="text-danger small">Nem sikerült betölteni a fájlokat.</div>';
        toast('Nem sikerült betölteni a feladat fájlokat.', 'danger');
      } finally {
        setBusy(false);
      }
    }

    // -----------------------------
    // Open modal
    // -----------------------------
    window.addEventListener('tasks:openDocuments', function (e) {
      var tid = e && e.detail && e.detail.taskId;
      var id = parseInt(String(tid || ''), 10);

      if (!Number.isFinite(id) || id <= 0) return;
      if (taskIdEl) taskIdEl.value = String(id);

      bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });

    modalEl.addEventListener('shown.bs.modal', function () {
      refresh();
    });

    // -----------------------------
    // Add file
    // -----------------------------
    if (addBtn && fileInput) {
      addBtn.addEventListener('click', function () {
        if (busy) return;
        fileInput.value = '';
        fileInput.click();
      });

      fileInput.addEventListener('change', async function () {
        var taskId = getTaskId();
        var file = fileInput.files && fileInput.files[0];
        if (!taskId || !file) return;

        var note = noteEl ? String(noteEl.value || '').trim() : '';

        setBusy(true);
        try {
          toast('Fájl mentése...', 'info');
          var items = await apiUploadAndAttach(taskId, file, note);
          renderList(items);
          if (noteEl) noteEl.value = '';
          toast('Fájl csatolva.', 'success');
        } catch (e) {
          console.error('[taskDocumentsModal] upload failed', e);
          toast('Nem sikerült a feltöltés/csatolás.', 'danger');
        } finally {
          setBusy(false);
        }
      });
    }

    // -----------------------------
    // Delete attach
    // -----------------------------
    if (listEl) {
      listEl.addEventListener('click', async function (e) {
        var btn = e.target.closest('.js-task-doc-delete');
        if (!btn) return;

        var taskId = getTaskId();
        if (!taskId) return;

        var linkId = btn.getAttribute('data-link-id');
        if (!linkId) return;

        if (!confirm('Biztosan törlöd a csatolást?')) return;

        setBusy(true);
        try {
          var result = await apiDeleteAttachment(taskId, linkId);

          // ha delete visszaad listát:
          if (Array.isArray(result)) renderList(result);
          else await refresh();

          toast('Csatolás törölve.', 'success');
        } catch (err) {
          console.error('[taskDocumentsModal] delete failed', err);
          toast('Nem sikerült törölni a csatolást.', 'danger');
        } finally {
          setBusy(false);
        }
      });
    }
  });
})();
