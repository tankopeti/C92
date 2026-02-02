// wwwroot/js/Task/taskIntezkedesCreate.js
(function () {
  'use strict';

  console.log('[taskIntezkedesCreate] loaded');

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskIntezkedesCreate] DOM loaded');

    // ------------------------------------------------------------
    // Elements
    // ------------------------------------------------------------
    var modalEl = document.getElementById('newTaskModal');
    if (!modalEl) return;

    var formEl = modalEl.querySelector('form');
    if (!formEl) return;

    var submitBtn = formEl.querySelector('button[type="submit"]');
    var assignedEl = formEl.querySelector('#AssignedToId, [name="AssignedToId"]');
    var commMethodEl = formEl.querySelector('#TaskPMcomMethodID, [name="TaskPMcomMethodID"]');
    var taskTypeEl = formEl.querySelector('#TaskTypePMId, [name="TaskTypePMId"]');
    var taskStatusEl = formEl.querySelector('#TaskStatusPMId, [name="TaskStatusPMId"]');

    // Bejelentés = 1, Intézkedés = 2 (később bővíthető 3-ra is)
    var DISPLAY_TYPE = toInt(modalEl.getAttribute('data-display-type')) || 2;

    var isSubmitting = false;

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    function getCsrfToken(formEl) {
      var tokenInput = formEl.querySelector('input[name="__RequestVerificationToken"]');
      return tokenInput && tokenInput.value ? tokenInput.value : '';
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

    function toInt(v) {
      var n = parseInt(String(v == null ? '' : v), 10);
      return isFinite(n) ? n : null;
    }

    function setSubmitting(btn, submitting) {
      if (!btn) return;
      btn.disabled = !!submitting;
      btn.dataset._origText = btn.dataset._origText || btn.innerHTML;
      btn.innerHTML = submitting
        ? '<span class="spinner-border spinner-border-sm me-2"></span>Mentés...'
        : btn.dataset._origText;
    }

    // ✅ Date + Time -> ISO datetime string (local)
    // dateStr: "YYYY-MM-DD"
    // timeStr: "HH:mm" (vagy üres)
    // -> "YYYY-MM-DDTHH:mm:00"
    function combineDateTime(dateStr, timeStr) {
      var d = String(dateStr || '').trim();
      if (!d) return null;

      var t = String(timeStr || '').trim();
      if (!t) {
        // nincs idő megadva -> csak dátum (backend 00:00-ra értelmezi)
        return d;
      }

      // Ha valaki "HH:mm:ss"-t adna, azt is engedjük
      if (/^\d{2}:\d{2}$/.test(t)) t = t + ':00';
      if (!/^\d{2}:\d{2}:\d{2}$/.test(t)) {
        // invalid time -> csak dátum
        return d;
      }

      return d + 'T' + t;
    }

    async function loadCommMethodsSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/taskpm-communication-methods/select', {
          method: 'GET',
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });

        if (!res.ok) {
          var txt = await res.text().catch(function () { return ''; });
          throw new Error('HTTP ' + res.status + ' :: ' + txt);
        }

        var items = await res.json();
        if (!Array.isArray(items)) items = [];

        selectEl.innerHTML =
          '<option value="">-- Válasszon --</option>' +
          items.map(function (x) {
            return '<option value="' + String(x.id) + '">' + String(x.text) + '</option>';
          }).join('');

        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
      } catch (e) {
        console.error('[taskIntezkedesCreate] comm methods load failed', e);
        selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
      } finally {
        selectEl.disabled = false;
      }
    }

    async function loadAssigneesSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/assignees/select', {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });
        if (!res.ok) throw new Error('HTTP ' + res.status);

        var items = await res.json();
        if (!Array.isArray(items)) items = [];

        selectEl.innerHTML =
          '<option value="">-- Válasszon --</option>' +
          items.map(function (x) {
            return '<option value="' + String(x.id) + '">' + String(x.text) + '</option>';
          }).join('');

        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
      } catch (e) {
        console.error('[taskIntezkedesCreate] assignees load failed', e);
        selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
      } finally {
        selectEl.disabled = false;
      }
    }

    async function loadTaskTypesSelect(selectEl, selectedId, displayType) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/tasktypes/select?displayType=' + encodeURIComponent(displayType), {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });
        if (!res.ok) throw new Error('HTTP ' + res.status);

        var items = await res.json();
        if (!Array.isArray(items)) items = [];

        selectEl.innerHTML =
          '<option value="">-- Válasszon --</option>' +
          items.map(function (x) {
            return '<option value="' + String(x.id) + '">' + String(x.text) + '</option>';
          }).join('');

        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
      } catch (e) {
        console.error('[taskIntezkedesCreate] task types load failed', e);
        selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
      } finally {
        selectEl.disabled = false;
      }
    }

    async function loadTaskStatusesSelect(selectEl, selectedId, displayType) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/taskstatuses/select?displayType=' + encodeURIComponent(displayType), {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });
        if (!res.ok) throw new Error('HTTP ' + res.status);

        var items = await res.json();
        if (!Array.isArray(items)) items = [];

        selectEl.innerHTML =
          '<option value="">-- Válasszon --</option>' +
          items.map(function (x) {
            return '<option value="' + String(x.id) + '">' + String(x.text) + '</option>';
          }).join('');

        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
      } catch (e) {
        console.error('[taskIntezkedesCreate] task statuses load failed', e);
        selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
      } finally {
        selectEl.disabled = false;
      }
    }

    // ------------------------------------------------------------
    // Create modal lifecycle
    // ------------------------------------------------------------
    modalEl.addEventListener('hidden.bs.modal', function () {
      isSubmitting = false;
      setSubmitting(submitBtn, false);

      try { formEl.reset(); } catch (e) { }
      formEl.classList.remove('was-validated');
    });

    modalEl.addEventListener('shown.bs.modal', async function () {
      if (taskTypeEl && (!taskTypeEl.options || taskTypeEl.options.length <= 1)) {
        await loadTaskTypesSelect(taskTypeEl, taskTypeEl.value || '', DISPLAY_TYPE);
      }

      if (taskStatusEl && (!taskStatusEl.options || taskStatusEl.options.length <= 1)) {
        await loadTaskStatusesSelect(taskStatusEl, taskStatusEl.value || '', DISPLAY_TYPE);
      }

      if (assignedEl && (!assignedEl.options || assignedEl.options.length <= 1)) {
        await loadAssigneesSelect(assignedEl, assignedEl.value || '');
      }

      if (commMethodEl && (!commMethodEl.options || commMethodEl.options.length <= 1)) {
        await loadCommMethodsSelect(commMethodEl, commMethodEl.value || '');
      }
    });

    // ------------------------------------------------------------
    // CREATE submit
    // ------------------------------------------------------------
    formEl.addEventListener('submit', async function (e) {
      e.preventDefault();
      if (isSubmitting) return;

      if (!formEl.checkValidity()) {
        formEl.classList.add('was-validated');
        toast('Kérlek töltsd ki a kötelező mezőket.', 'warning');
        return;
      }

      var fd = new FormData(formEl);

      // ✅ ScheduledDate + ScheduledTime összefűzése
      var scheduledDateStr = fd.get('ScheduledDate');
      var scheduledTimeStr = fd.get('ScheduledTime');
      var scheduledIso = combineDateTime(scheduledDateStr, scheduledTimeStr);

var payload = {
  Title: String(fd.get('Title') || '').trim(),
  Description: String(fd.get('Description') || '').trim() || null,

  TaskPMcomMethodID: toInt(fd.get('TaskPMcomMethodID')),
  CommunicationDescription: String(fd.get('CommunicationDescription') || '').trim() || null,

  PartnerId: toInt(fd.get('PartnerId')),
  RelatedPartnerId: toInt(fd.get('RelatedPartnerId')),   // ✅ ÚJ
  SiteId: toInt(fd.get('SiteId')),
  TaskTypePMId: toInt(fd.get('TaskTypePMId')),

  TaskPriorityPMId: toInt(fd.get('TaskPriorityPMId')),
  TaskStatusPMId: toInt(fd.get('TaskStatusPMId')),
  AssignedToId: String(fd.get('AssignedToId') || '').trim() || null,

  ScheduledDate: scheduledIso
};

      // Guards
      if (!payload.Title) { toast('A tárgy megadása kötelező!', 'danger'); return; }
      if (!payload.SiteId) { toast('A telephely kiválasztása kötelező!', 'danger'); return; }
      if (!payload.PartnerId) { toast('A partner kiválasztása kötelező!', 'danger'); return; }
      if (!payload.TaskTypePMId) { toast('A feladat típusa kötelező!', 'danger'); return; }

      console.log('[taskIntezkedesCreate] payload', payload);

      isSubmitting = true;
      setSubmitting(submitBtn, true);

      try {
        var token = getCsrfToken(formEl);

        var res = await fetch('/api/tasks', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token,
            'Accept': 'application/json'
          },
          credentials: 'same-origin',
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          var err = await res.text().catch(function () { return ''; });
          console.error('[taskIntezkedesCreate] CREATE ERROR', err);
          toast('Hiba a mentés során.', 'danger');
          return;
        }

        var created = await res.json();

        toast('Intézkedés létrehozva!', 'success');
        var inst = bootstrap.Modal.getInstance(modalEl);
        if (inst) inst.hide();

        window.dispatchEvent(new CustomEvent('tasks:reload', { detail: { created: created } }));
      } catch (err) {
        console.error('[taskIntezkedesCreate] CREATE EXCEPTION', err);
        toast('Nem sikerült a mentés.', 'danger');
      } finally {
        isSubmitting = false;
        setSubmitting(submitBtn, false);
      }
    });
  });
})();
