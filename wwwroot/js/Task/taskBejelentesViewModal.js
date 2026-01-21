(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {

    const modalEl = document.getElementById('taskViewModal');
    const bodyEl = document.getElementById('taskModalBody');
    const titleEl = document.getElementById('taskModalTitle');
    const editBtn = document.getElementById('editTaskBtn');

    if (!modalEl || !bodyEl) return;

    // --------------------------------------------------------------------
    // CSRF
    // --------------------------------------------------------------------
    const csrf =
      document.querySelector('meta[name="csrf-token"]')?.content ||
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1] ||
      '';

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------
    function esc(v) {
      const d = document.createElement('div');
      d.textContent = v == null ? '' : String(v);
      return d.innerHTML;
    }

    function fmtDate(v) {
      if (!v) return '–';
      const d = new Date(v);
      if (isNaN(d.getTime())) return '–';
      return d.toLocaleString('hu-HU', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      }).replace(',', '');
    }

    function badge(text, color) {
      const bg = color && String(color).trim() ? color : '#6c757d';
      return `<span class="badge text-white" style="background:${esc(bg)}">${esc(text || '–')}</span>`;
    }

    // --------------------------------------------------------------------
    // ATTACHMENTS
    // --------------------------------------------------------------------
    function fileLinkFor(att) {
      const docId = att.documentId ?? att.DocumentId;
      const filePath = att.filePath ?? att.FilePath;

      if (filePath) return filePath;

      // ⬇️ ha nálad más a letöltő endpoint, CSAK EZT AZ 1 SORT írd át
      return `/api/documents/${encodeURIComponent(docId)}/download`;
    }

    function renderAttachments(d) {
      const list = d.attachments ?? d.Attachments ?? [];
      if (!Array.isArray(list) || list.length === 0) {
        return `<div class="text-muted small">Nincs csatolt dokumentum.</div>`;
      }

      return list.map(att => {
        const docId = att.documentId ?? att.DocumentId;
        const name = att.fileName ?? att.FileName ?? (`Dokumentum #${docId}`);
        const linkedDate = att.linkedDate ?? att.LinkedDate;
        const linkedBy = att.linkedByName ?? att.LinkedByName;
        const note = att.note ?? att.Note;
        const href = fileLinkFor(att);

        return `
          <div class="d-flex justify-content-between align-items-start p-2 mb-2 bg-white border rounded">
            <div class="me-2">
              <div>
                <i class="bi bi-paperclip me-2 text-success"></i>
                <a href="${esc(href)}" target="_blank" rel="noopener">
                  <strong>${esc(name)}</strong>
                </a>
              </div>
              <div class="text-muted small mt-1">
                ${linkedDate ? esc(fmtDate(linkedDate)) : '–'}
                ${linkedBy ? ` • ${esc(linkedBy)}` : ''}
                ${note ? ` • ${esc(note)}` : ''}
              </div>
            </div>
            <span class="badge bg-light text-dark">#${esc(docId)}</span>
          </div>
        `;
      }).join('');
    }

    // --------------------------------------------------------------------
    // FETCH
    // --------------------------------------------------------------------
    async function fetchTask(taskId) {
      const res = await fetch(`/api/tasks/${encodeURIComponent(taskId)}`, {
        headers: {
          'Accept': 'application/json',
          ...(csrf ? { 'RequestVerificationToken': csrf } : {})
        }
      });

      if (!res.ok) {
        const t = await res.text().catch(() => '');
        throw new Error(`HTTP ${res.status} ${t}`);
      }

      return res.json();
    }

    // --------------------------------------------------------------------
    // RENDER
    // --------------------------------------------------------------------
    async function renderTask(d) {
      console.log('[VIEW TASK RAW]', d);

      const id = d.id ?? d.Id;
      const title = d.title ?? d.Title;
      const desc = d.description ?? d.Description;

      const typeName = d.taskTypePMName ?? d.TaskTypePMName ?? '–';
      const statusName = d.taskStatusPMName ?? d.TaskStatusPMName ?? '–';
      const prioName = d.taskPriorityPMName ?? d.TaskPriorityPMName ?? '–';

      const statusColor = d.colorCode ?? d.StatusColorCode;
      const prioColor = d.priorityColorCode ?? d.PriorityColorCode;

      const scheduled = d.scheduledDate ?? d.ScheduledDate;
      const due = d.dueDate ?? d.DueDate;

      const assignedName = d.assignedToName ?? d.AssignedToName ?? '–';

      const partnerName = d.partnerName ?? d.PartnerName ?? '–';
      const siteName = d.siteName ?? d.SiteName ?? '–';
      const city = d.city ?? d.City ?? '';

      const created = d.createdDate ?? d.CreatedDate;
      const updated = d.updatedDate ?? d.UpdatedDate;
      const createdBy = d.createdByName ?? d.CreatedByName ?? '–';

      if (titleEl) {
        titleEl.innerHTML = `<i class="bi bi-eye me-2"></i> Intézkedés #${esc(id)} – ${esc(title)}`;
      }

      bodyEl.innerHTML = `
        <div class="row g-4">
          <div class="col-lg-6">
            <div class="p-3 border rounded bg-light">
              <h6 class="mb-3 text-success">Alapadatok</h6>

              <div><strong>Tárgy:</strong> ${esc(title)}</div>
              <div><strong>Leírás:</strong> ${desc ? esc(desc) : '<em>Nincs</em>'}</div>

              <hr/>

              <div><strong>Feladat típusa:</strong> ${esc(typeName)}</div>
              <div><strong>Státusz:</strong> ${badge(statusName, statusColor)}</div>
              <div><strong>Prioritás:</strong> ${badge(prioName, prioColor)}</div>

              <hr/>

              <div><strong>Beütemezve:</strong> ${scheduled ? esc(fmtDate(scheduled)) : '–'}</div>
              <div><strong>Határidő:</strong> ${due ? esc(fmtDate(due)) : '–'}</div>

              <hr/>

              <div><strong>Felelős:</strong> ${esc(assignedName)}</div>
            </div>
          </div>

          <div class="col-lg-6">
            <div class="p-3 border rounded bg-light">
              <h6 class="mb-3 text-success">Kapcsolódó adatok</h6>

              <div><strong>Partner:</strong> ${esc(partnerName)}</div>
              <div><strong>Telephely:</strong> ${esc(siteName)} ${city ? `(${esc(city)})` : ''}</div>

              <hr/>

              <div><strong>Létrehozva:</strong> ${esc(fmtDate(created))} – ${esc(createdBy)}</div>
              <div><strong>Módosítva:</strong> ${updated ? esc(fmtDate(updated)) : '–'}</div>
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">
                <i class="bi bi-paperclip me-2"></i> Csatolt dokumentumok
              </h6>
              ${renderAttachments(d)}
            </div>
          </div>
        </div>
      `;

      if (editBtn) {
        editBtn.style.display = 'inline-block';
        editBtn.dataset.taskId = String(id);
        editBtn.onclick = () => window.Tasks?.openEditModal?.(id);
      }
    }

    // --------------------------------------------------------------------
    // OPEN
    // --------------------------------------------------------------------
    async function openTaskView(taskId) {
      bodyEl.innerHTML = `
        <div class="text-center py-5">
          <div class="spinner-border text-success"></div>
          <p class="mt-3 text-muted">Adatok betöltése...</p>
        </div>`;

      bootstrap.Modal.getOrCreateInstance(modalEl).show();

      try {
        const data = await fetchTask(taskId);
        await renderTask(data);
      } catch (e) {
        console.error(e);
        bodyEl.innerHTML = `<div class="alert alert-danger">Nem sikerült betölteni a feladatot.</div>`;
      }
    }

    // --------------------------------------------------------------------
    // EVENTS
    // --------------------------------------------------------------------
    document.addEventListener('click', function (e) {
      const btn = e.target.closest('.btn-view-task,[data-view-task]');
      if (btn) {
        const id = btn.dataset.taskId || btn.dataset.viewTask;
        if (id) openTaskView(id);
      }
    });

    window.Tasks = window.Tasks || {};
    window.Tasks.openViewModal = openTaskView;

    modalEl.addEventListener('hidden.bs.modal', function () {
      bodyEl.innerHTML = '';
      if (editBtn) {
        editBtn.style.display = 'none';
        editBtn.onclick = null;
      }
    });

  });
})();
