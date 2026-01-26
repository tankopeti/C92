// wwwroot/js/Task/taskBejelentesCreate.js
(function () {
  'use strict';

  console.log('[taskBejelentesCreate] loaded');

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskBejelentesCreate] DOM loaded');

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
        console.error('[create] comm methods load failed', e);
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
        console.error('[create] assignees load failed', e);
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

      var payload = {
        Title: String(fd.get('Title') || '').trim(),
        Description: String(fd.get('Description') || '').trim() || null,

        TaskPMcomMethodID: toInt(fd.get('TaskPMcomMethodID')),
        CommunicationDescription: String(fd.get('CommunicationDescription') || '').trim() || null,

        PartnerId: toInt(fd.get('PartnerId')),
        SiteId: toInt(fd.get('SiteId')),
        TaskTypePMId: toInt(fd.get('TaskTypePMId')),

        TaskPriorityPMId: toInt(fd.get('TaskPriorityPMId')),
        TaskStatusPMId: toInt(fd.get('TaskStatusPMId')),
        AssignedToId: String(fd.get('AssignedToId') || '').trim() || null,
        ScheduledDate: fd.get('ScheduledDate') || null

        // ✅ file csatolás kivéve
        // AttachedDocumentIds: []
      };

      // Guards
      if (!payload.Title) { toast('A tárgy megadása kötelező!', 'danger'); return; }
      if (!payload.SiteId) { toast('A telephely kiválasztása kötelező!', 'danger'); return; }
      if (!payload.PartnerId) { toast('A partner kiválasztása kötelező!', 'danger'); return; }
      if (!payload.TaskTypePMId) { toast('A feladat típusa kötelező!', 'danger'); return; }

      console.log('[CREATE payload]', payload);

      isSubmitting = true;
      setSubmitting(submitBtn, true);

      try {
        var token = getCsrfToken(formEl);

        // Create task
        var res = await fetch('/api/tasks', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
          },
          credentials: 'same-origin',
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          var err = await res.text().catch(function () { return ''; });
          console.error('[CREATE ERROR]', err);
          toast('Hiba a mentés során.', 'danger');
          return;
        }

        var created = await res.json();

        toast('Bejelentés létrehozva!', 'success');
        bootstrap.Modal.getInstance(modalEl) && bootstrap.Modal.getInstance(modalEl).hide();

        window.dispatchEvent(new CustomEvent('tasks:reload', { detail: { created: created } }));
      } catch (err) {
        console.error('[CREATE EXCEPTION]', err);
        toast('Nem sikerült a mentés.', 'danger');
      } finally {
        isSubmitting = false;
        setSubmitting(submitBtn, false);
      }
    });
  });
})();
