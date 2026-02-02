// wwwroot/js/Task/taskIntezkedesEdit.js
(function () {
  'use strict';

  console.log('[taskIntezkedesEdit] loaded');

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskIntezkedesEdit] DOM loaded');

    // ------------------------------------------------------------
    // CONFIG
    // ------------------------------------------------------------
    // Bejelentés = 1, Intézkedés = 2
    var DISPLAY_TYPE = 2; // <-- itt Intézkedés edit modal

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    function formatHuDateTime(v) {
      if (!v) return '';
      try {
        var d = new Date(v);
        if (isNaN(d.getTime())) return String(v);
        return d.toLocaleString('hu-HU');
      } catch (e) {
        return String(v);
      }
    }

    function getCsrfToken(formEl) {
      var tokenInput = formEl.querySelector('input[name="__RequestVerificationToken"]');
      if (tokenInput && tokenInput.value) return tokenInput.value;

      var meta = document.querySelector('meta[name="csrf-token"]');
      if (meta && meta.content) return meta.content;

      var m = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
      if (m && m[1]) return decodeURIComponent(m[1]);

      return '';
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
      var n = parseInt(String(v == null ? '' : v), 10);
      return isFinite(n) ? n : null;
    }

    function pick(obj, keys) {
      for (var i = 0; i < keys.length; i++) {
        var k = keys[i];
        if (obj && obj[k] !== undefined && obj[k] !== null) return obj[k];
      }
      return null;
    }

    function setSubmitting(btn, isSubmitting) {
      if (!btn) return;
      btn.disabled = !!isSubmitting;
      btn.dataset._origText = btn.dataset._origText || btn.innerHTML;
      btn.innerHTML = isSubmitting
        ? '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Mentés...'
        : btn.dataset._origText;
    }

    function fmtForInputDate(iso) {
      if (!iso) return '';
      try {
        var d = new Date(iso);
        if (isNaN(d.getTime())) return '';
        var pad = function (n) { return String(n).padStart(2, '0'); };
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
      } catch (e) {
        return '';
      }
    }

    function sleep(ms) {
      return new Promise(function (resolve) { setTimeout(resolve, ms); });
    }

    async function waitForTomSelect(selectEl, timeoutMs) {
      timeoutMs = timeoutMs || 2000;
      var step = 50;
      var tries = Math.ceil(timeoutMs / step);

      for (var i = 0; i < tries; i++) {
        if (selectEl && selectEl.tomselect) return selectEl.tomselect;
        await sleep(step);
      }
      return selectEl && selectEl.tomselect ? selectEl.tomselect : null;
    }

    async function waitAndSetSelectValue(selectEl, value) {
      if (!selectEl) return;
      var v = value == null ? '' : String(value);
      if (!v) return;

      for (var i = 0; i < 60; i++) {
        var has = Array.from(selectEl.options || []).some(function (o) { return String(o.value) === v; });
        if (has) {
          selectEl.value = v;
          try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          return;
        }
        await sleep(50);
      }

      selectEl.value = v;
      try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
    }

    // ------------------------------------------------------------
    // SELECT LOADERS
    // ------------------------------------------------------------

    async function loadAssigneesSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/assignees/select', {
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
      } catch (err) {
        console.error('[taskIntezkedesEdit] loadAssigneesSelect failed', err);
        if (!selectEl.querySelector('option')) {
          selectEl.innerHTML = '<option value="">-- Válasszon --</option>';
        }
      } finally {
        selectEl.disabled = false;
      }
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
      } catch (err) {
        console.error('[taskIntezkedesEdit] loadCommMethodsSelect failed', err);
        if (!selectEl.querySelector('option')) {
          selectEl.innerHTML = '<option value="">-- Válasszon --</option>';
        }
      } finally {
        selectEl.disabled = false;
      }
    }

    // ✅ TaskType select (DisplayType szűréssel) + TomSelect kompatibilis
    async function loadTaskTypesSelect(selectEl, selectedId, displayType) {
      if (!selectEl) return;

      var wanted = displayType == null ? '' : String(displayType);
      var ts = selectEl.tomselect || null;

      if (!ts) {
        selectEl.disabled = true;
        selectEl.innerHTML = '<option value="">Betöltés...</option>';
      } else {
        ts.disable();
        ts.clearOptions();
        ts.addOption({ id: '', text: 'Betöltés...' });
        ts.refreshOptions(false);
      }

      try {
        var url = '/api/tasks/tasktypes/select?displayType=' + encodeURIComponent(wanted);
        var res = await fetch(url, {
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin'
        });
        if (!res.ok) throw new Error('HTTP ' + res.status);

        var items = await res.json();
        if (!Array.isArray(items)) items = [];

        var opts = items.map(function (x) {
          return { id: String(x.id), text: String(x.text) };
        });

        if (ts) {
          ts.clearOptions();
          ts.addOption({ id: '', text: '-- Válasszon --' });
          ts.addOptions(opts);
          ts.refreshOptions(false);

          if (selectedId != null && String(selectedId)) {
            ts.setValue(String(selectedId), true);
          } else {
            ts.setValue('', true);
          }

          ts.enable();
        } else {
          selectEl.innerHTML =
            '<option value="">-- Válasszon --</option>' +
            opts.map(function (x) {
              return '<option value="' + x.id + '">' + x.text + '</option>';
            }).join('');

          await waitAndSetSelectValue(selectEl, selectedId);
          selectEl.disabled = false;
        }
      } catch (e) {
        console.error('[taskIntezkedesEdit] task types load failed', e);

        if (ts) {
          ts.clearOptions();
          ts.addOption({ id: '', text: '-- Nem sikerült betölteni --' });
          ts.setValue('', true);
          ts.enable();
        } else {
          selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
          selectEl.disabled = false;
        }
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

        await waitAndSetSelectValue(selectEl, selectedId);
      } catch (e) {
        console.error('[taskIntezkedesEdit] task statuses load failed', e);
        selectEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
      } finally {
        selectEl.disabled = false;
      }
    }

    // ------------------------------------------------------------
    // Row update helpers
    // ------------------------------------------------------------
    function rowElById(id) {
      return document.querySelector('tr[data-task-id="' + CSS.escape(String(id)) + '"]');
    }

    function updateRowFromTask(t) {
      var id = pick(t, ['id', 'Id']);
      if (id == null) return;

      var tr = rowElById(id);
      if (!tr) return;

      var tds = tr.querySelectorAll('td');
      if (!tds || tds.length < 12) return;

      tds[4].textContent = (pick(t, ['title', 'Title']) || '');

      var prioBadge =
        tds[5].querySelector('.clickable-priority-badge') ||
        tds[5].querySelector('.badge');

      if (prioBadge) {
        prioBadge.textContent = (pick(t, ['taskPriorityPMName', 'TaskPriorityPMName']) || '');
        prioBadge.style.backgroundColor = (pick(t, ['priorityColorCode', 'PriorityColorCode']) || '#6c757d');
        prioBadge.dataset.priorityId = (pick(t, ['taskPriorityPMId', 'TaskPriorityPMId']) || '');
      }

      tds[6].textContent = formatHuDateTime(pick(t, ['dueDate', 'DueDate']));

      var statusBadge =
        tds[7].querySelector('.clickable-status-badge') ||
        tds[7].querySelector('.badge');

      if (statusBadge) {
        statusBadge.textContent = (pick(t, ['taskStatusPMName', 'TaskStatusPMName']) || '');
        statusBadge.style.backgroundColor = (pick(t, ['colorCode', 'ColorCode']) || '#6c757d');
        statusBadge.dataset.statusId = (pick(t, ['taskStatusPMId', 'TaskStatusPMId']) || '');
      }

      tds[9].textContent = formatHuDateTime(pick(t, ['updatedDate', 'UpdatedDate']));

      var assignedEmail = pick(t, ['assignedToEmail', 'AssignedToEmail']) || '';
      var assignedName = pick(t, ['assignedToName', 'AssignedToName']) || '';

      if (assignedEmail) {
        tds[10].innerHTML =
          '<a class="js-assigned-mail" href="mailto:' + assignedEmail + '">' + assignedName + '</a>';
      } else {
        tds[10].textContent = assignedName;
      }
    }

    // ------------------------------------------------------------
    // Elements (EDIT MODAL)
    // ------------------------------------------------------------
    var modalEl = document.getElementById('editTaskModal');
    if (!modalEl) {
      console.warn('[taskIntezkedesEdit] #editTaskModal not found -> skip');
      return;
    }

    var formEl = modalEl.querySelector('form');
    if (!formEl) {
      console.warn('[taskIntezkedesEdit] form not found in modal -> skip');
      return;
    }

    var submitBtn = formEl.querySelector('button[type="submit"]');

    var idEl = formEl.querySelector('[name="Id"], #EditId, #Id');
    var titleEl = formEl.querySelector('[name="Title"]');
    var descEl = formEl.querySelector('[name="Description"]');

    var siteEl = formEl.querySelector('#EditSiteId, select[name="SiteId"]');

    var taskTypeEl = formEl.querySelector('#EditTaskTypePMId, #TaskTypePMId, select[name="TaskTypePMId"]');
    var statusEl = formEl.querySelector('#EditTaskStatusPMId, #TaskStatusPMId, select[name="TaskStatusPMId"]');

    var priorityEl = formEl.querySelector('#EditTaskPriorityPMId, #TaskPriorityPMId, select[name="TaskPriorityPMId"]');
    var assignedEl = formEl.querySelector('#EditAssignedToId, #AssignedToId, select[name="AssignedToId"]');

    var commMethodEl = formEl.querySelector('#EditTaskPMcomMethodID, #TaskPMcomMethodID, select[name="TaskPMcomMethodID"]');
    var commDescEl = formEl.querySelector('[name="CommunicationDescription"]');

    var scheduledDateEl = formEl.querySelector('[name="ScheduledDate"]');
    var partnerHiddenEl = formEl.querySelector('#editAutoPartnerId, input[name="PartnerId"]');

    var relatedPartnerEl = formEl.querySelector('#EditRelatedPartnerId, #RelatedPartnerId, select[name="RelatedPartnerId"]');

    var isSubmitting = false;
    var currentId = null;
    var isPickerSwitch = false;

    // ------------------------------------------------------------
    // API
    // ------------------------------------------------------------
    async function loadTask(id) {
      var res = await fetch('/api/tasks/' + encodeURIComponent(id), {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        credentials: 'same-origin'
      });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('HTTP ' + res.status + ' :: ' + txt);
      }

      return await res.json();
    }

    async function refreshRow(id) {
      try {
        var fresh = await loadTask(id);
        updateRowFromTask(fresh);
      } catch (e) {
        console.warn('[taskIntezkedesEdit] refreshRow failed', e);
      }
    }

    // ------------------------------------------------------------
    // Modal show/hide hooks
    // ------------------------------------------------------------
    modalEl.addEventListener('hidden.bs.modal', function () {
      if (isPickerSwitch) return;

      isSubmitting = false;
      setSubmitting(submitBtn, false);
      currentId = null;

      try { formEl.reset(); } catch (e) { }
      formEl.classList.remove('was-validated');
    });

    modalEl.addEventListener('shown.bs.modal', async function () {
      if (taskTypeEl && (!taskTypeEl.options || taskTypeEl.options.length <= 1)) {
        await loadTaskTypesSelect(taskTypeEl, taskTypeEl.value || '', DISPLAY_TYPE);
      }

      if (statusEl && (!statusEl.options || statusEl.options.length <= 1)) {
        await loadTaskStatusesSelect(statusEl, statusEl.value || '', DISPLAY_TYPE);
      }

      if (assignedEl && (!assignedEl.options || assignedEl.options.length <= 1)) {
        await loadAssigneesSelect(assignedEl, assignedEl.value || '');
      }

      if (commMethodEl && (!commMethodEl.options || commMethodEl.options.length <= 1)) {
        await loadCommMethodsSelect(commMethodEl, commMethodEl.value || '');
      }
    });

    // ------------------------------------------------------------
    // Open modal from event: tasks:openEdit { id }
    // ------------------------------------------------------------
    async function openEditModal(id) {
      currentId = id;
      formEl.classList.remove('was-validated');

      bootstrap.Modal.getOrCreateInstance(modalEl).show();
      setSubmitting(submitBtn, true);

      try {
        var task = await loadTask(id);

        var taskIdVal = pick(task, ['id', 'Id']) != null ? pick(task, ['id', 'Id']) : id;

        var titleVal = pick(task, ['title', 'Title']) || '';
        var descVal = pick(task, ['description', 'Description']) || '';

        var statusIdVal = pick(task, ['taskStatusPMId', 'TaskStatusPMId']);
        var priorityIdVal = pick(task, ['taskPriorityPMId', 'TaskPriorityPMId']);
        var assignedToIdVal = pick(task, ['assignedToId', 'AssignedToId']) || '';

        var scheduledVal = pick(task, ['scheduledDate', 'ScheduledDate']);

        var partnerIdVal = pick(task, ['partnerId', 'PartnerId']);
        var relatedPartnerIdVal = pick(task, ['relatedPartnerId', 'RelatedPartnerId']);
        var siteIdVal = pick(task, ['siteId', 'SiteId']);
        var siteNameVal = pick(task, ['siteName', 'SiteName']);
        var partnerNameVal = pick(task, ['partnerName', 'PartnerName']);

        var taskTypeIdVal = pick(task, ['taskTypePMId', 'TaskTypePMId']);

        var commMethodIdVal = pick(task, ['taskPMcomMethodID', 'TaskPMcomMethodID']);
        var commDescVal = pick(task, ['communicationDescription', 'CommunicationDescription']) || '';

        if (idEl) idEl.value = String(taskIdVal);
        if (titleEl) titleEl.value = titleVal;
        if (descEl) descEl.value = descVal;

        if (scheduledDateEl) scheduledDateEl.value = fmtForInputDate(scheduledVal);

        if (partnerHiddenEl) partnerHiddenEl.value = partnerIdVal != null ? String(partnerIdVal) : '';

        await loadTaskTypesSelect(taskTypeEl, taskTypeIdVal, DISPLAY_TYPE);
        await loadTaskStatusesSelect(statusEl, statusIdVal, DISPLAY_TYPE);

        if (priorityEl && priorityIdVal != null) {
          priorityEl.value = String(priorityIdVal);
          try { priorityEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
        }

        await loadAssigneesSelect(assignedEl, assignedToIdVal);
        await loadCommMethodsSelect(commMethodEl, commMethodIdVal);
        if (commDescEl) commDescEl.value = String(commDescVal || '');

        // ✅ RelatedPartner beállítása (sima select vagy TomSelect)
        if (relatedPartnerEl) {
          var rts = await waitForTomSelect(relatedPartnerEl, 2000);
          var val = relatedPartnerIdVal != null ? String(relatedPartnerIdVal) : '';

          if (rts) {
            rts.setValue(val, true);
          } else {
            relatedPartnerEl.value = val;
            try { relatedPartnerEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          }
        }

        // ✅ RelatedPartner beállítása (sima select vagy TomSelect)
        if (relatedPartnerEl) {
          var rts = await waitForTomSelect(relatedPartnerEl, 2000);
          var val = relatedPartnerIdVal != null ? String(relatedPartnerIdVal) : '';

          if (rts) {
            rts.setValue(val, true);
          } else {
            relatedPartnerEl.value = val;
            try { relatedPartnerEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          }
        }


        if (siteEl && siteIdVal != null) {
          var siteIdStr = String(siteIdVal);
          var ts = await waitForTomSelect(siteEl, 2500);

          if (ts) {
            ts.addOption({
              id: siteIdStr,
              text: siteNameVal || ('#' + siteIdStr),
              partnerId: partnerIdVal || null,
              partnerName: partnerNameVal || '',
              partnerDetails: partnerNameVal || ''
            });

            if (partnerHiddenEl && partnerIdVal != null) partnerHiddenEl.value = String(partnerIdVal);

            ts.setValue(siteIdStr, true);
            ts.refreshOptions(false);
          } else {
            siteEl.value = siteIdStr;
            try { siteEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          }
        }

      } catch (err) {
        console.error('[taskIntezkedesEdit] open failed', err);
        toast('Nem sikerült betölteni a feladatot (Task ID: ' + id + ').', 'danger');
        try { bootstrap.Modal.getOrCreateInstance(modalEl).hide(); } catch (e) { }
      } finally {
        setSubmitting(submitBtn, false);
      }
    }

    window.addEventListener('tasks:openEdit', function (e) {
      var id = toInt(e && e.detail && e.detail.id);
      if (!id) return;
      openEditModal(id);
    });

    // ------------------------------------------------------------
    // AJAX UPDATE submit (no page reload)
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

      var id = toInt(fd.get('Id')) || currentId;
      if (!id) {
        toast('Hiányzik a Task ID a szerkesztéshez.', 'danger');
        return;
      }

      var partnerId = toInt(fd.get('PartnerId'));
      if (!partnerId && partnerHiddenEl) partnerId = toInt(partnerHiddenEl.value);

      var siteId = toInt(fd.get('SiteId'));
      if (!siteId && siteEl && siteEl.tomselect) siteId = toInt(siteEl.tomselect.getValue());

      var taskTypeId = toInt(fd.get('TaskTypePMId'));
      if (!taskTypeId && taskTypeEl && taskTypeEl.tomselect) {
        taskTypeId = toInt(taskTypeEl.tomselect.getValue());
      }

      // ✅ RelatedPartnerId kiolvasása (sima select vagy TomSelect)
      var relatedPartnerId = toInt(fd.get('RelatedPartnerId'));
      if (!relatedPartnerId && relatedPartnerEl && relatedPartnerEl.tomselect) {
        relatedPartnerId = toInt(relatedPartnerEl.tomselect.getValue());
      }


      var sd = fd.get('ScheduledDate') ? String(fd.get('ScheduledDate')) : null;
      if (sd) sd = sd + 'T00:00:00';

      var payload = {
        Id: id,

        Title: String(fd.get('Title') || '').trim(),
        Description: String(fd.get('Description') || '').trim() || null,

        PartnerId: partnerId,
        RelatedPartnerId: relatedPartnerId,
        SiteId: siteId,

        TaskTypePMId: taskTypeId,

        TaskPriorityPMId: toInt(fd.get('TaskPriorityPMId')),
        TaskStatusPMId: toInt(fd.get('TaskStatusPMId')),
        AssignedToId: (fd.get('AssignedToId') || '').toString().trim() || null,

        TaskPMcomMethodID: toInt(fd.get('TaskPMcomMethodID')),
        CommunicationDescription: (fd.get('CommunicationDescription') || '').toString().trim() || null,

        ScheduledDate: sd
      };

      console.log('[taskIntezkedesEdit] PUT url=', '/api/tasks/' + encodeURIComponent(id));
      console.log('[taskIntezkedesEdit] PUT payload=', payload);

      if (!payload.Title) { toast('A tárgy (cím) megadása kötelező!', 'danger'); return; }
      if (!payload.SiteId) { toast('A telephely kiválasztása kötelező!', 'danger'); return; }
      if (!payload.TaskTypePMId) { toast('A feladat típusa kötelező!', 'danger'); return; }
      if (!payload.PartnerId) { toast('Telephely kiválasztásakor Partner kötelező (Site választásból jön).', 'danger'); return; }

      isSubmitting = true;
      setSubmitting(submitBtn, true);

      try {
        var token = getCsrfToken(formEl);
        var headers = { 'Content-Type': 'application/json' };
        if (token) headers['RequestVerificationToken'] = token;

        var res = await fetch('/api/tasks/' + encodeURIComponent(id), {
          method: 'PUT',
          headers: headers,
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          var errText = await res.text().catch(function () { return ''; });
          console.error('[taskIntezkedesEdit] update failed', res.status, errText);
          toast('Hiba a mentéskor (HTTP ' + res.status + ').', 'danger');
          throw new Error('HTTP ' + res.status);
        }

        var updated = await res.json().catch(function () { return null; });

        toast('Intézkedés frissítve!', 'success');
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();

        if (updated && (updated.id || updated.Id)) {
          try { updateRowFromTask(updated); } catch (_) { await refreshRow(id); }
        } else {
          await refreshRow(id);
        }

      } catch (err) {
        console.error('[taskIntezkedesEdit] update exception', err);
      } finally {
        isSubmitting = false;
        setSubmitting(submitBtn, false);
      }
    });
  });
})();
