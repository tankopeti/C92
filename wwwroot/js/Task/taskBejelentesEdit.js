// wwwroot/js/Task/taskBejelentesEdit.js
(function () {
  'use strict';

  console.log('[taskBejelentesEdit] loaded');


  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskBejelentesEdit] DOM loaded');
    function renderAttachmentsList(attachments) {
      var listEl = document.getElementById('editAttachedDocumentsList');
      if (!listEl) return;

      attachments = Array.isArray(attachments) ? attachments : [];

      if (!attachments.length) {
        listEl.innerHTML = '<div class="text-muted small">Még nincs csatolt dokumentum. Kattintson a fenti gombra a hozzáadáshoz.</div>';
        return;
      }

      listEl.innerHTML = attachments.map(function (a) {
        var linkId = pick(a, ['id', 'Id']);                 // TaskDocumentLink.Id
        var docId = pick(a, ['documentId', 'DocumentId']); // DocumentId
        var name = pick(a, ['fileName', 'FileName']) || ('#' + docId);
        var path = pick(a, ['filePath', 'FilePath']) || '';

        // link: ha a FilePath már egy letöltési url, akkor jó
        var href = path ? String(path) : ('/documents/download/' + docId);

        return `
      <div class="d-flex justify-content-between align-items-center border rounded p-2 mb-2" data-doc-id="${docId}" data-link-id="${linkId}">
        <div class="me-3">
          <i class="bi bi-file-earmark-text me-2"></i>
          <a href="${href}" target="_blank">${escHtml(name)}</a>
        </div>
        <button type="button" class="btn btn-outline-danger btn-sm js-remove-attach" data-doc-id="${docId}">
          <i class="bi bi-trash"></i>
        </button>
      </div>
    `;
      }).join('');
    }

    // ---- Date format fallback (ha nincs global formatHuDateTime) ----
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

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    function parseIds(raw) {
      raw = String(raw || '').trim();
      if (!raw) return [];
      return raw.split(',')
        .map(function (x) { return parseInt(x.trim(), 10); })
        .filter(function (n) { return isFinite(n) && n > 0; });
    }

    function setIds(ids) {
      ids = (ids || [])
        .map(function (x) { return parseInt(String(x), 10); })
        .filter(function (n) { return isFinite(n) && n > 0; });

      // uniq
      var uniq = Array.from(new Set(ids));
      if (attachedIdsEl) attachedIdsEl.value = uniq.join(',');

      var cnt = document.getElementById('editAttachedCount');
      if (cnt) cnt.textContent = String(uniq.length);

      return uniq;
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

    // camelCase + PascalCase kompatibilis olvasás
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

      for (var i = 0; i < 60; i++) { // ~3 sec
        var has = Array.from(selectEl.options || []).some(function (o) { return String(o.value) === v; });
        if (has) {
          selectEl.value = v;
          try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          return;
        }
        await sleep(50);
      }

      // fallback
      selectEl.value = v;
      try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
    }

    // ✅ Assignees: sima select feltöltés
    async function loadAssigneesSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      selectEl.innerHTML = '<option value="">Betöltés...</option>';

      try {
        var res = await fetch('/api/tasks/assignees/select', {
          method: 'GET',
          headers: { 'Accept': 'application/json' },
          credentials: 'same-origin' // fontos cookie authnál
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

        // ✅ set selected
        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }

      } catch (err) {
        console.error('[taskBejelentesEdit] loadAssigneesSelect failed', err);
        if (!selectEl.querySelector('option')) {
          selectEl.innerHTML = '<option value="">-- Válasszon --</option>';
        }
      } finally {
        selectEl.disabled = false;
      }
    }

    // ✅ Sima select feltöltés (kommunikációs módhoz)
    async function loadCommMethodsSelect(selectEl, selectedId) {
      if (!selectEl) return;

      selectEl.disabled = true;
      try {
        var res = await fetch('/api/tasks/taskpm-communication-methods/select', {
          method: 'GET',
          headers: { 'Accept': 'application/json' }
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

        // set selected
        selectEl.value = selectedId != null ? String(selectedId) : '';
        try { selectEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }

      } catch (err) {
        console.error('[taskBejelentesEdit] loadCommMethodsSelect failed', err);
        // fallback placeholder
        if (!selectEl.querySelector('option')) {
          selectEl.innerHTML = '<option value="">-- Válasszon --</option>';
        }
      } finally {
        selectEl.disabled = false;
      }
    }

    function rowElById(id) {
      return document.querySelector('tr[data-task-id="' + CSS.escape(String(id)) + '"]');
    }

    function escHtml(s) {
      var div = document.createElement('div');
      div.textContent = s == null ? '' : String(s);
      return div.innerHTML;
    }

    function updateRowFromTask(t) {
      var id = pick(t, ['id', 'Id']);
      if (id == null) return;

      var tr = rowElById(id);
      if (!tr) return;

      var tds = tr.querySelectorAll('td');
      if (!tds || tds.length < 12) return;

      tds[4].textContent = (pick(t, ['title', 'Title']) || '');

      var prioBadge = tds[5].querySelector('.clickable-priority-badge');
      if (prioBadge) {
        prioBadge.textContent = (pick(t, ['taskPriorityPMName', 'TaskPriorityPMName']) || '');
        prioBadge.style.backgroundColor = (pick(t, ['priorityColorCode', 'PriorityColorCode']) || '#6c757d');
        prioBadge.dataset.priorityId = (pick(t, ['taskPriorityPMId', 'TaskPriorityPMId']) || '');
      }

      tds[6].textContent = formatHuDateTime(pick(t, ['dueDate', 'DueDate']));

      var statusBadge = tds[7].querySelector('.clickable-status-badge');
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
          '<a class="js-assigned-mail" href="mailto:' + escHtml(assignedEmail) + '">' + escHtml(assignedName) + '</a>';
      } else {
        tds[10].textContent = assignedName;
      }
    }

    // ------------------------------------------------------------
    // Elements
    // ------------------------------------------------------------
    var modalEl = document.getElementById('editTaskModal');
    if (!modalEl) {
      console.warn('[taskBejelentesEdit] #editTaskModal not found -> skip');
      return;
    }

    var formEl = modalEl.querySelector('form');
    if (!formEl) {
      console.warn('[taskBejelentesEdit] form not found in modal -> skip');
      return;
    }

    var submitBtn = formEl.querySelector('button[type="submit"]');

    // Inputs/selects
    var idEl = formEl.querySelector('[name="Id"], #EditId, #Id');
    var titleEl = formEl.querySelector('[name="Title"]');
    var descEl = formEl.querySelector('[name="Description"]');

    // ✅ Telephely: TOMSELECT marad
    var siteEl = formEl.querySelector('#EditSiteId, select[name="SiteId"]');

    // TaskType async opciók
    var taskTypeEl = formEl.querySelector('#EditTaskTypePMId, select[name="TaskTypePMId"]');

    var statusEl = formEl.querySelector('[name="TaskStatusPMId"]');
    var priorityEl = formEl.querySelector('[name="TaskPriorityPMId"]');
    var assignedEl = formEl.querySelector('[name="AssignedToId"]');

    // ✅ Kommunikáció: sima select + input
    var commMethodEl = formEl.querySelector('#EditTaskPMcomMethodID, select[name="TaskPMcomMethodID"]');
    var commDescEl = formEl.querySelector('[name="CommunicationDescription"]');

    var scheduledDateEl = formEl.querySelector('[name="ScheduledDate"]');

    var partnerHiddenEl = formEl.querySelector('#editAutoPartnerId, input[name="PartnerId"]');

    // ✅ EDIT: csatolmány ID-k (hidden input)
    var attachedIdsEl = formEl.querySelector('#editAttachedDocumentIdsInput, [name="AttachedDocumentIds"]');

    // ✅ EDIT: csatolmány UI (ha van a modal HTML-ben)
    // Ha nincs ilyen ID-d, simán marad null és nem fog futni a UI rész.
    var attachedListEl = document.getElementById('editAttachedDocumentsList');
    var attachedCountEl = document.getElementById('editAttachedCount');

    var isSubmitting = false;
    var currentId = null;
    var isPickerSwitch = false;

    var attachBtn = document.getElementById('editAttachDocumentsBtn');
    // ⚠️ NE deklaráld újra attachedListEl-t itt, ha már fentebb egyszer megvan
    // var attachedListEl = document.getElementById('editAttachedDocumentsList');

    window.Tasks = window.Tasks || {};

    // ✅ Ezt fogja hívni a dokumentum picker, amikor kiválasztottál valamit
    window.Tasks.onEditDocumentsSelected = async function (selectedDocIds) {
      console.log('[EDIT] onEditDocumentsSelected CALLED', selectedDocIds, 'currentId=', currentId);

      if (!Array.isArray(selectedDocIds)) selectedDocIds = [];
      selectedDocIds = selectedDocIds
        .map(function (x) { return parseInt(String(x), 10); })
        .filter(function (n) { return isFinite(n) && n > 0; });

      if (!currentId) {
        toast('Nincs Task ID (currentId).', 'warning');
        return;
      }

      // UI: mutassunk betöltést
      if (attachedListEl) attachedListEl.innerHTML = '<div class="text-muted small">Dokumentumok mentése...</div>';

      try {
        var token = getCsrfToken(formEl);
        var headers = { 'Content-Type': 'application/json' };
        if (token) headers['RequestVerificationToken'] = token;

        // ✅ 1) DB-be mentjük a linkeket
        var res = await fetch('/api/tasks/' + encodeURIComponent(currentId) + '/documents/attach', {
          method: 'POST',
          headers: headers,
          credentials: 'same-origin',
          body: JSON.stringify({ documentIds: selectedDocIds })
        });


        if (!res.ok) {
          var t = await res.text().catch(function () { return ''; });
          console.error('[edit] attach failed', res.status, t);
          toast('Nem sikerült csatolni a dokumentumokat (HTTP ' + res.status + ').', 'danger');
          return;
        }

        // ✅ 2) Friss GET és render (már DB-ből jönnek az Attachments)
        var fresh = await loadTask(currentId);

        var atts = pick(fresh, ['attachments', 'Attachments']) || [];
        if (!Array.isArray(atts)) atts = [];

        renderAttachmentsList(atts);

        // hidden input sync (DocumentId-k)
        var docIds = atts
          .map(function (a) { return pick(a, ['documentId', 'DocumentId']); })
          .filter(function (x) { return x != null; })
          .map(function (x) { return parseInt(String(x), 10); })
          .filter(function (n) { return isFinite(n) && n > 0; });

        setIds(docIds);

        toast('Dokumentumok csatolva.', 'success');
      } catch (e) {
        console.error('[edit] onEditDocumentsSelected exception', e);
        toast('Hiba a csatolás közben.', 'danger');
      }
    };

    // ✅ A document picker így jelzi vissza a kiválasztott dokumentumokat
    window.addEventListener('documents:selected', function (e) {
      try {
        var ids = (e && e.detail && e.detail.ids) || [];
        console.log('[EDIT] documents:selected received', ids);

        // picker bezárása (ha nyitva van)
        var pickerModalEl = document.getElementById('documentPickerModal');
        if (pickerModalEl) {
          try { bootstrap.Modal.getOrCreateInstance(pickerModalEl).hide(); } catch (_) { }
        }

        window.Tasks.onEditDocumentsSelected(ids);
      } catch (err) {
        console.error('[EDIT] documents:selected handler failed', err);
      }
    });



    if (attachBtn) {
      attachBtn.addEventListener('click', function () {
        console.log('[taskBejelentesEdit] attach click');

        // ✅ 1) Ha van openPicker, azt használjuk!
        if (window.Documents && typeof window.Documents.openPicker === 'function') {
          isPickerSwitch = true;
          bootstrap.Modal.getOrCreateInstance(modalEl).hide();

          window.Documents.openPicker({
            onSelected: function (ids) {
              window.Tasks.onEditDocumentsSelected(ids);

              setTimeout(function () {
                isPickerSwitch = false;
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
              }, 50);
            },
            onCancel: function () {
              setTimeout(function () {
                isPickerSwitch = false;
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
              }, 50);
            }
          });

          return;
        }

        // ✅ 2) Fallback: ha nincs openPicker, csak akkor nyissuk a modalt
        var pickerModalEl = document.getElementById('documentPickerModal');
        if (!pickerModalEl) {
          toast('Dokumentum választó nincs bekötve (nem találom a pickert).', 'warning');
          return;
        }

        isPickerSwitch = true;
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();

        var pickerModal = bootstrap.Modal.getOrCreateInstance(pickerModalEl, {
          backdrop: 'static',
          keyboard: true,
          focus: true
        });
        pickerModal.show();

        pickerModalEl.addEventListener('hidden.bs.modal', function onPickerHidden() {
          pickerModalEl.removeEventListener('hidden.bs.modal', onPickerHidden);
          setTimeout(function () {
            isPickerSwitch = false;
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
          }, 50);
        });
      });
    }





    // ------------------------------------------------------------
    // Modal show/hide hooks
    // ------------------------------------------------------------
    modalEl.addEventListener('hidden.bs.modal', function () {

      // ✅ HA PICKER MIATT REJTETTÜK EL, NE RESETELJ!
      if (isPickerSwitch) return;

      isSubmitting = false;
      setSubmitting(submitBtn, false);
      currentId = null;

      try { formEl.reset(); } catch (e) { }
      formEl.classList.remove('was-validated');

      // UI reset (csatolmányok)
      if (attachedListEl) attachedListEl.innerHTML = '';
      if (attachedCountEl) attachedCountEl.textContent = '0';
      if (attachedIdsEl) attachedIdsEl.value = '';
    });


    modalEl.addEventListener('shown.bs.modal', async function () {
      // ha még nincs feltöltve, töltjük
      if (assignedEl && (!assignedEl.options || assignedEl.options.length <= 1)) {
        await loadAssigneesSelect(assignedEl, assignedEl.value || '');
      }
    });

    async function refreshRow(id) {
      try {
        var fresh = await loadTask(id);
        updateRowFromTask(fresh);
      } catch (e) {
        console.warn('[taskBejelentesEdit] refreshRow failed -> fallback reload', e);
        // ✅ helyes: a tasks táblát kérjük újratölteni
        window.dispatchEvent(new CustomEvent('tasks:reload', { detail: { updatedId: id } }));
      }
    }


    // ------------------------------------------------------------
    // Open modal from event: tasks:openEdit { id }
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

    function renderEditAttachments(task) {
      // GET-ben nálad: attachments: [{ documentId, fileName, filePath, ... }]
      var atts = pick(task, ['attachments', 'Attachments']) || [];
      if (!Array.isArray(atts)) atts = [];

      // hidden inputba DocumentId-k
      if (attachedIdsEl) {
        var docIds = atts
          .map(function (a) { return pick(a, ['documentId', 'DocumentId']); })
          .filter(function (x) { return x != null; })
          .map(function (x) { return parseInt(String(x), 10); })
          .filter(function (n) { return isFinite(n) && n > 0; });

        attachedIdsEl.value = docIds.join(',');
      }

      // UI lista (ha van)
      if (!attachedListEl || !attachedCountEl) return;

      attachedListEl.innerHTML = '';
      if (!atts.length) {
        attachedListEl.innerHTML = '<div class="text-muted small">Nincs csatolt dokumentum.</div>';
        attachedCountEl.textContent = '0';
        return;
      }

      attachedCountEl.textContent = String(atts.length);

      atts.forEach(function (a) {
        var docId = pick(a, ['documentId', 'DocumentId']);
        var fileName = pick(a, ['fileName', 'FileName']) || ('#' + docId);
        var filePath = pick(a, ['filePath', 'FilePath']) || '';

        // ✅ ha van letöltő endpointod, ide írd be. ha nincs, marad filePath
        var href = filePath || ('/documents/' + docId);

        var row = document.createElement('div');
        row.className = 'd-flex justify-content-between align-items-center p-2 mb-2 bg-white border rounded shadow-sm';
        row.innerHTML =
          '<div>' +
          ' <i class="bi bi-file-earmark-text me-2 text-primary"></i>' +
          ' <a href="' + escHtml(href) + '" target="_blank" rel="noopener">' + escHtml(fileName) + '</a>' +
          '</div>' +
          '<button type="button" class="btn btn-sm btn-outline-danger" data-remove-doc="' + escHtml(docId) + '">' +
          ' <i class="bi bi-x"></i>' +
          '</button>';

        attachedListEl.appendChild(row);
      });
    }

    // ✅ törlés edit UI-ban: csak kliens oldali (mentéskor majd a hidden input megy)
    if (attachedListEl) {
      attachedListEl.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-remove-doc]');
        if (!btn) return;

        var docId = btn.getAttribute('data-remove-doc');
        var item = btn.closest('div');
        if (item) item.remove();

        // újragyűjtés
        var ids = Array.from(attachedListEl.querySelectorAll('[data-remove-doc]'))
          .map(function (b) { return parseInt(b.getAttribute('data-remove-doc'), 10); })
          .filter(function (n) { return isFinite(n) && n > 0; });

        if (attachedIdsEl) attachedIdsEl.value = ids.join(',');
        if (attachedCountEl) attachedCountEl.textContent = String(ids.length);

        if (ids.length === 0) {
          attachedListEl.innerHTML = '<div class="text-muted small">Nincs csatolt dokumentum.</div>';
        }
      });
    }

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

        if (statusEl && statusIdVal != null) statusEl.value = String(statusIdVal);
        if (priorityEl && priorityIdVal != null) priorityEl.value = String(priorityIdVal);

        // ✅ Felelős lista betöltés + kiválasztás
        await loadAssigneesSelect(assignedEl, assignedToIdVal);

        var scheduledVal = pick(task, ['scheduledDate', 'ScheduledDate']);

        var partnerIdVal = pick(task, ['partnerId', 'PartnerId']);
        var siteIdVal = pick(task, ['siteId', 'SiteId']);
        var siteNameVal = pick(task, ['siteName', 'SiteName']);
        var partnerNameVal = pick(task, ['partnerName', 'PartnerName']);

        var taskTypeIdVal = pick(task, ['taskTypePMId', 'TaskTypePMId']);

        // ✅ kommunikáció a GET-ből
        var commMethodIdVal = pick(task, ['taskPMcomMethodID', 'TaskPMcomMethodID']);
        var commDescVal = pick(task, ['communicationDescription', 'CommunicationDescription']) || '';

        // Id
        if (idEl) idEl.value = String(taskIdVal);

        // alap mezők
        if (titleEl) titleEl.value = titleVal;
        if (descEl) descEl.value = descVal;

        // scheduledDate (input type="date")
        if (scheduledDateEl) scheduledDateEl.value = fmtForInputDate(scheduledVal);

        // Partner hidden
        if (partnerHiddenEl) partnerHiddenEl.value = partnerIdVal != null ? String(partnerIdVal) : '';

        // ✅ Kommunikációs mód: SIMA SELECT-ből töltjük endpointtal, majd set value
        await loadCommMethodsSelect(commMethodEl, commMethodIdVal);

        // Kommunikáció leírás
        if (commDescEl) commDescEl.value = String(commDescVal || '');

        // ✅ SITE TOMSELECT: megvárjuk, és programozottan felvesszük + setValue
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

            // partner hidden sync
            if (partnerHiddenEl && partnerIdVal != null) partnerHiddenEl.value = String(partnerIdVal);

            ts.setValue(siteIdStr, true);
            ts.refreshOptions(false);
          } else {
            // fallback: sima select value
            siteEl.value = siteIdStr;
            try { siteEl.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }
          }
        }

        // ✅ TaskType: async opciók megvárása + beállítás
        await waitAndSetSelectValue(taskTypeEl, taskTypeIdVal);

        // ✅ Attachments (TaskDocumentLinks)
        renderEditAttachments(task);

      } catch (err) {
        console.error('[taskBejelentesEdit] open failed', err);
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

      // Site / Partner / TaskType biztos érték (FormData + fallback)
      var partnerId = toInt(fd.get('PartnerId'));
      if (!partnerId && partnerHiddenEl) partnerId = toInt(partnerHiddenEl.value);

      var taskTypeId = toInt(fd.get('TaskTypePMId'));
      if (!taskTypeId && taskTypeEl) taskTypeId = toInt(taskTypeEl.value);

      var siteId = toInt(fd.get('SiteId'));
      if (!siteId && siteEl && siteEl.tomselect) siteId = toInt(siteEl.tomselect.getValue());

      // ScheduledDate: date -> datetime midnight
      var sd = fd.get('ScheduledDate') ? String(fd.get('ScheduledDate')) : null;
      if (sd) sd = sd + 'T00:00:00';

      var payload = {
        Id: id,

        Title: String(fd.get('Title') || '').trim(),
        Description: String(fd.get('Description') || '').trim() || null,

        PartnerId: partnerId,
        SiteId: siteId,

        // ha nálad TaskType edit nem engedélyezett, maradhat kommentben
        // TaskTypePMId: taskTypeId,

        TaskPriorityPMId: toInt(fd.get('TaskPriorityPMId')),
        TaskStatusPMId: toInt(fd.get('TaskStatusPMId')),
        AssignedToId: (fd.get('AssignedToId') || '').toString().trim() || null,

        // ✅ kommunikáció a DTO szerint
        TaskPMcomMethodID: toInt(fd.get('TaskPMcomMethodID')),
        CommunicationDescription: (fd.get('CommunicationDescription') || '').toString().trim() || null,

        ScheduledDate: sd,

        // ✅ hidden inputból
        AttachedDocumentIds: (function () {
          var raw = '';
          if (attachedIdsEl) raw = String(attachedIdsEl.value || '').trim();
          if (!raw) return [];
          return raw.split(',')
            .map(function (x) { return parseInt(x.trim(), 10); })
            .filter(function (n) { return isFinite(n); });
        })()
      };

      console.log('[taskBejelentesEdit] PUT url=', '/api/tasks/' + encodeURIComponent(id));
      console.log('[taskBejelentesEdit] PUT payload=', payload);

      // Guardok
      if (!payload.Title) { toast('A tárgy (cím) megadása kötelező!', 'danger'); return; }
      if (!payload.SiteId) { toast('A telephely kiválasztása kötelező!', 'danger'); return; }
      if (!payload.PartnerId) { toast('Telephely kiválasztásakor Partner kötelező (Site választásból jön).', 'danger'); return; }

      isSubmitting = true;
      setSubmitting(submitBtn, true);

      try {
        var token = getCsrfToken(formEl);
        var headers = { 'Content-Type': 'application/json' };
        if (token) headers['RequestVerificationToken'] = token;

        // ⚠️ FIGYELEM: ez a SQL endpoint csak a felelőst frissíti.
        // Ha a dokumentumokat is menteni akarod, kell egy külön endpoint /documents/sync.
        var res = await fetch('/api/tasks/' + encodeURIComponent(id), {
          method: 'PUT',
          headers: headers,
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          var errText = await res.text().catch(function () { return ''; });
          console.error('[taskBejelentesEdit] update failed', res.status, errText);
          toast('Hiba a mentéskor (HTTP ' + res.status + ').', 'danger');
          throw new Error('HTTP ' + res.status);
        }

        var updated = await res.json().catch(function () { return null; });

        toast('Bejelentés frissítve!', 'success');
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();

        // ✅ biztos sorfrissítés (nincs oldal reload)
        if (updated && (updated.id || updated.Id)) {
          try {
            updateRowFromTask(updated);
          } catch (e) {
            await refreshRow(id);
          }
        } else {
          await refreshRow(id);
        }

      } catch (err) {
        console.error('[taskBejelentesEdit] update exception', err);
      } finally {
        isSubmitting = false;
        setSubmitting(submitBtn, false);
      }
    });
  });
})();
