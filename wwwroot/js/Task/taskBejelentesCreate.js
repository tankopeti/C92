(function () {
  'use strict';

  console.log('[taskBejelentesCreate] loaded');

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskBejelentesCreate] DOM loaded');

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    function getCsrfToken(formEl) {
      return formEl.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
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
      new bootstrap.Toast(t, { delay: 4000 }).show();
    }

    function toInt(v) {
      var n = parseInt(String(v ?? ''), 10);
      return Number.isFinite(n) ? n : null;
    }

    function setSubmitting(btn, isSubmitting) {
      if (!btn) return;
      btn.disabled = !!isSubmitting;
      btn.dataset._origText = btn.dataset._origText || btn.innerHTML;
      btn.innerHTML = isSubmitting
        ? '<span class="spinner-border spinner-border-sm me-2"></span>Mentés...'
        : btn.dataset._origText;
    }

        // ------------------------------------------------------------
    // Assignees select loader
    // ------------------------------------------------------------
    async function loadAssigneesSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        const res = await fetch('/api/tasks/assignees/select', {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin' // fontos, ha cookie auth van
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const items = await res.json();
        selectEl.innerHTML =
          '<option value="">-- Válasszon --</option>' +
          (items || []).map(x => `<option value="${x.id}">${x.text}</option>`).join('');

        // create-nél nem muszáj, de nem árt
        // ✅ set selected (ha van)
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
    // Elements
    // ------------------------------------------------------------
    const modalEl = document.getElementById('newTaskModal');
    if (!modalEl) return;

    const formEl = modalEl.querySelector('form');
    if (!formEl) return;

    const submitBtn = formEl.querySelector('button[type="submit"]');
        const assignedEl = formEl.querySelector('#AssignedToId, [name="AssignedToId"]');
    let isSubmitting = false;

    // ------------------------------------------------------------
    // Modal lifecycle
    // ------------------------------------------------------------
    modalEl.addEventListener('hidden.bs.modal', () => {
      isSubmitting = false;
      setSubmitting(submitBtn, false);
      formEl.reset();
      formEl.classList.remove('was-validated');
    });

modalEl.addEventListener('shown.bs.modal', async () => {
  if (assignedEl && (!assignedEl.options || assignedEl.options.length <= 1)) {
    await loadAssigneesSelect(assignedEl, assignedEl.value || '');
  }
});



    // ------------------------------------------------------------
    // CREATE SUBMIT
    // ------------------------------------------------------------
    formEl.addEventListener('submit', async function (e) {
      e.preventDefault();
      if (isSubmitting) return;

      if (!formEl.checkValidity()) {
        formEl.classList.add('was-validated');
        toast('Kérlek töltsd ki a kötelező mezőket.', 'warning');
        return;
      }

      const fd = new FormData(formEl);

      const payload = {
        Title: (fd.get('Title') || '').toString().trim(),
        Description: (fd.get('Description') || '').toString().trim() || null,

        // ✅ ÚJ – kommunikáció
        TaskPMcomMethodID: toInt(fd.get('TaskPMcomMethodID')),
        CommunicationDescription: (fd.get('CommunicationDescription') || '').toString().trim() || null,

        // kötelező kapcsolatok
        PartnerId: toInt(fd.get('PartnerId')),
        SiteId: toInt(fd.get('SiteId')),
        TaskTypePMId: toInt(fd.get('TaskTypePMId')),

        // egyéb mezők
        TaskPriorityPMId: toInt(fd.get('TaskPriorityPMId')),
        TaskStatusPMId: toInt(fd.get('TaskStatusPMId')),
        AssignedToId: (fd.get('AssignedToId') || '').toString().trim() || null,
        ScheduledDate: fd.get('ScheduledDate') || null,

        AttachedDocumentIds: (() => {
          const raw = (fd.get('AttachedDocumentIds') || '').toString().trim();
          if (!raw) return [];
          return raw.split(',')
            .map(x => parseInt(x.trim(), 10))
            .filter(n => Number.isFinite(n));
        })()
      };

      // ------------------------------------------------------------
      // Guards
      // ------------------------------------------------------------
      if (!payload.Title) {
        toast('A tárgy megadása kötelező!', 'danger');
        return;
      }
      if (!payload.SiteId) {
        toast('A telephely kiválasztása kötelező!', 'danger');
        return;
      }
      if (!payload.PartnerId) {
        toast('A partner kiválasztása kötelező!', 'danger');
        return;
      }
      if (!payload.TaskTypePMId) {
        toast('A feladat típusa kötelező!', 'danger');
        return;
      }

      console.log('[CREATE payload]', payload);

      isSubmitting = true;
      setSubmitting(submitBtn, true);

      try {
        const res = await fetch('/api/tasks', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCsrfToken(formEl)
          },
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          const err = await res.text();
          console.error('[CREATE ERROR]', err);
          toast('Hiba a mentés során.', 'danger');
          return;
        }

        const created = await res.json();
        toast('Bejelentés létrehozva!', 'success');

        bootstrap.Modal.getInstance(modalEl)?.hide();
        window.dispatchEvent(new CustomEvent('tasks:reload', { detail: { created } }));

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
