// wwwroot/js/Task/taskBejelentesLoadMore.js
(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[taskBejelentesLoadMore] init');

    var wrap = document.getElementById('tasksTableWrap');
    var tbody = document.getElementById('tasksTbody');
    var btn = document.getElementById('btnLoadMoreTasks');
    var info = document.getElementById('tasksLoadInfo');

    if (!wrap || !tbody || !btn) {
      console.warn('[taskBejelentesLoadMore] missing elements');
      return;
    }

    var pageSize = parseInt(wrap.dataset.pageSize || '20', 10) || 20;
    var sort = (wrap.dataset.sort || 'CreatedDate').trim() || 'CreatedDate';
    var order = (wrap.dataset.order || 'desc').trim() || 'desc';
    var search = (wrap.dataset.search || '').trim();

    var page = 1;
    var isLoading = false;
    var renderedIds = new Set();
    var reachedEnd = false;
    var totalCount = null;

    // -------------------------------
    // Advanced filters (modalból)
    // -------------------------------
    var activeFilters = {}; // pl: { statusId:"1", partnerId:"10", dueDateFrom:"2026-01-01", ... }
    window.__TASKS_FILTER_AJAX__ = true; // hogy a modal JS ne navigáljon fallback-ként

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------
    function esc(s) {
      var d = document.createElement('div');
      d.textContent = s == null ? '' : String(s);
      return d.innerHTML;
    }

    function formatDateHu(v) {
      if (!v) return '–';
      var d = new Date(v);
      if (isNaN(d.getTime())) return '–';
      return d.toLocaleString('hu-HU', {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit'
      }).replace(',', '');
    }

    function setInfo(text) {
      if (!info) return;
      info.textContent = text || '';
    }

    function setInfoLoaded() {
      if (!info) return;
      var loaded = renderedIds.size;

      if (typeof totalCount === 'number') {
        info.textContent = 'Betöltve: ' + Math.min(loaded, totalCount) + ' / ' + totalCount;
      } else {
        info.textContent = 'Betöltve: ' + loaded;
      }
    }

    function setButtonLoading(loading) {
      btn.disabled = !!loading;
      btn.textContent = loading ? 'Betöltés...' : (reachedEnd ? 'Nincs több' : 'Több betöltése');
    }

    function setNoMore() {
      reachedEnd = true;
      btn.disabled = true;
      btn.textContent = 'Nincs több';
    }

    function renderRow(t) {
      var id = Number(t && t.id);
      if (!Number.isFinite(id)) id = Number(t && t.Id);
      if (!Number.isFinite(id)) id = 0;

      var priorityColor = (t.priorityColorCode || t.PriorityColorCode || '').trim() || '#6c757d';
      var statusColor = (t.colorCode || t.ColorCode || '').trim() || '#6c757d';

      var assignedEmail = t.assignedToEmail || t.AssignedToEmail || '';
      var assignedName = t.assignedToName || t.AssignedToName || '';

      var assignedHtml = assignedEmail
        ? `<a class="js-assigned-mail" href="mailto:${esc(assignedEmail)}">${esc(assignedName)}</a>`
        : esc(assignedName);

      var tr = document.createElement('tr');
      tr.setAttribute('data-task-id', String(id));

      tr.innerHTML = `
        <td>${esc(id)}</td>
        <td>${esc(t.siteName || t.SiteName || '')}</td>
        <td>${esc(t.city || t.City || '')}</td>
        <td>${esc(t.partnerName || t.PartnerName || '')}</td>
        <td>${esc(t.title || t.Title || '')}</td>

        <td class="text-center">
          <span class="badge text-white" style="background:${esc(priorityColor)}">
            ${esc(t.taskPriorityPMName || t.TaskPriorityPMName || '')}
          </span>
        </td>

        <td>${esc(formatDateHu(t.dueDate || t.DueDate))}</td>

        <td class="text-center">
          <span class="badge text-white" style="background:${esc(statusColor)}">
            ${esc(t.taskStatusPMName || t.TaskStatusPMName || '')}
          </span>
        </td>

        <td>${esc(formatDateHu(t.createdDate || t.CreatedDate))}</td>
        <td>${esc(formatDateHu(t.updatedDate || t.UpdatedDate))}</td>
        <td>${assignedHtml}</td>

        <td>
          <div class="btn-group btn-group-sm" role="group">
            <button type="button"
                    class="btn btn-outline-info js-view-task-btn"
                    data-task-id="${esc(id)}"
                    title="Megtekintés">
              <i class="bi bi-eye"></i>
            </button>

            <div class="dropdown">
              <button class="btn btn-outline-secondary dropdown-toggle btn-sm"
                      type="button"
                      data-bs-toggle="dropdown"
                      aria-expanded="false">
                <i class="bi bi-three-dots-vertical"></i>
              </button>

<ul class="dropdown-menu dropdown-menu-end">
  <li>
    <a class="dropdown-item js-edit-task" href="#" data-task-id="${esc(id)}">
      <i class="bi bi-pencil-square me-2"></i> Szerkesztés
    </a>
  </li>

  <li>
    <a class="dropdown-item js-task-documents" href="#" data-task-id="${esc(id)}">
      <i class="bi bi-paperclip me-2"></i> Fájlok
    </a>
  </li>

  <li>
    <a class="dropdown-item btn-show-history" href="#" data-task-id="${esc(id)}">
      <i class="bi bi-clock-history me-2"></i> Előzmények
    </a>
  </li>

  <li><hr class="dropdown-divider"></li>

  <li>
    <a class="dropdown-item text-danger js-delete-task" href="#" data-task-id="${esc(id)}">
      <i class="bi bi-trash me-2"></i> Törlés
    </a>
  </li>
</ul>

            </div>
          </div>
        </td>
      `;

      return tr;
    }

    async function fetchPage(p) {
      var qs = new URLSearchParams();
      qs.set('page', String(p));
      qs.set('pageSize', String(pageSize));
      qs.set('sort', sort);
      qs.set('order', order);
      if (search) qs.set('search', search);

      // Advanced filter paramok hozzáadása (üreseket ne tegyünk be)
      if (activeFilters && typeof activeFilters === 'object') {
        Object.keys(activeFilters).forEach(function (k) {
          var v = activeFilters[k];
          if (v == null) return;
          v = String(v).trim();
          if (!v) return;
          qs.set(k, v);
        });
      }

      var url = '/api/tasks/paged?' + qs.toString();
      var res = await fetch(url, { headers: { 'Accept': 'application/json' } });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('HTTP ' + res.status + ' :: ' + txt);
      }

      var json = await res.json();

      var items = Array.isArray(json)
        ? json
        : (json.items || json.Items || json.results || json.Results || []);

      var tc = Array.isArray(json)
        ? null
        : (json.totalCount ?? json.TotalCount ?? json.totalRecords ?? json.TotalRecords ?? null);

      return { items: items, totalCount: tc };
    }

    async function loadMore() {
      if (isLoading || reachedEnd) return;

      isLoading = true;
      setButtonLoading(true);
      setInfo('Betöltés...');

      try {
        var result = await fetchPage(page);
        var items = result.items || [];

        // totalCount: parse (akkor is, ha string)
        if (result.totalCount != null) {
          var n = parseInt(String(result.totalCount), 10);
          if (Number.isFinite(n)) totalCount = n;
        }

        if (!items.length) {
          setInfoLoaded();
          setNoMore();
          return;
        }

        items.forEach(function (t) {
          var id = Number(t && (t.id ?? t.Id));
          if (!Number.isFinite(id)) return;
          if (renderedIds.has(id)) return;

          renderedIds.add(id);
          tbody.appendChild(renderRow(t));
        });

        if (items.length < pageSize) setNoMore();
        else page += 1;

        setInfoLoaded();

        if (typeof totalCount === 'number' && renderedIds.size >= totalCount) {
          setNoMore();
        }

      } catch (e) {
        console.error('[taskBejelentesLoadMore] load failed', e);
        setInfo('Hiba a betöltéskor (nézd meg a konzolt).');
      } finally {
        isLoading = false;
        setButtonLoading(false);
      }
    }

    // --------------------------------------------------
    // CLICK DELEGATION (BUBBLE, V2, AbortController)
    // --------------------------------------------------
    // ha már volt korábban handler, ezt leállítjuk
    if (wrap._delegationAbortController) {
      try { wrap._delegationAbortController.abort(); } catch (e) { }
    }
    var ac = new AbortController();
    wrap._delegationAbortController = ac;

    wrap.addEventListener('click', function (e) {
      // mailto menjen default
      var mail = e.target.closest('a[href^="mailto:"]');
      if (mail) return;

      var view = e.target.closest('.js-view-task-btn');
      if (view) {
        e.preventDefault();
        var vid = parseInt(view.dataset.taskId, 10);
        if (!Number.isFinite(vid)) return;

        console.log('[taskBejelentesLoadMore] view click', vid);

        if (window.Tasks && typeof window.Tasks.openViewModal === 'function') {
          window.Tasks.openViewModal(vid);
        } else {
          window.dispatchEvent(new CustomEvent('tasks:view', { detail: { id: vid } }));
        }
        return;
      }

      var edit = e.target.closest('.js-edit-task');
      if (edit) {
        e.preventDefault();
        var eid = parseInt(edit.dataset.taskId, 10);
        if (!Number.isFinite(eid)) return;

        console.log('[taskBejelentesLoadMore] edit click', eid);

        window.dispatchEvent(new CustomEvent('tasks:openEdit', { detail: { id: eid } }));
        return;
      }

      var files = e.target.closest('.js-task-documents');
      if (files) {
        e.preventDefault();
        var tid = parseInt(files.dataset.taskId, 10);
        if (!Number.isFinite(tid)) return;

        console.log('[taskBejelentesLoadMore] files click', tid);

        window.dispatchEvent(new CustomEvent('tasks:openDocuments', { detail: { taskId: tid } }));
        return;
      }

      var hist = e.target.closest('.btn-show-history');
      if (hist) {
        e.preventDefault();
        var hid = parseInt(hist.dataset.taskId, 10);
        if (!Number.isFinite(hid)) return;

        console.log('[taskBejelentesLoadMore] history click', hid);

        window.dispatchEvent(new CustomEvent('tasks:history', { detail: { id: hid } }));
        return;
      }

      var del = e.target.closest('.js-delete-task');
      if (del) {
        e.preventDefault();
        var did = parseInt(del.dataset.taskId, 10);
        if (!Number.isFinite(did)) return;

        console.log('[taskBejelentesLoadMore] delete click', did);

        window.dispatchEvent(new CustomEvent('tasks:openDelete', { detail: { id: did } }));
        return;
      }
    }, { signal: ac.signal });

    btn.addEventListener('click', function (e) {
      e.preventDefault();
      loadMore();
    });

// --------------------------------------------------
// ADVANCED FILTER MODAL -> AJAX submit (NO PAGE RELOAD)
// --------------------------------------------------
(function wireAdvancedFilterModal() {
  var modalEl = document.getElementById('advancedFilterModal');
  if (!modalEl) return;

  // id-s formot keressük, de fallback: első form a modalban
  var form = document.getElementById('advancedFilterForm') || modalEl.querySelector('form');
  if (!form) return;

  function closeModal() {
    try {
      var inst = bootstrap.Modal.getInstance(modalEl);
      if (!inst) inst = new bootstrap.Modal(modalEl);
      inst.hide();
    } catch (e) { /* ignore */ }
  }

  // Partner -> Site szűrés a data-partner-id alapján
  var partnerSel = modalEl.querySelector('#partnerFilterSelect');
  var siteSel = modalEl.querySelector('#siteFilterSelect');

  if (partnerSel && siteSel) {
    var allSiteOptions = Array.from(siteSel.options).map(function (o) {
      return {
        value: o.value,
        text: o.textContent,
        partnerId: o.dataset.partnerId || ""
      };
    });

    function rebuildSites(partnerId) {
      var current = siteSel.value;

      siteSel.innerHTML = "";

      var opt0 = document.createElement('option');
      opt0.value = "";
      opt0.textContent = "-- Minden --";
      siteSel.appendChild(opt0);

      allSiteOptions
        .filter(function (x) { return x.value !== ""; })
        .filter(function (x) { return !partnerId || x.partnerId === partnerId; })
        .forEach(function (x) {
          var opt = document.createElement('option');
          opt.value = x.value;
          opt.textContent = x.text;
          if (x.partnerId) opt.dataset.partnerId = x.partnerId;
          siteSel.appendChild(opt);
        });

      var still = Array.from(siteSel.options).some(function (o) { return o.value === current; });
      siteSel.value = still ? current : "";
    }

    rebuildSites(partnerSel.value);
    partnerSel.addEventListener('change', function () {
      rebuildSites(partnerSel.value);
    });
  }

  // A lényeg: submit elkapás
  form.addEventListener('submit', function (e) {
    e.preventDefault(); // <<<<< ettől lesz AJAX

    var fd = new FormData(form);
    var cleaned = {};

    fd.forEach(function (v, k) {
      var val = (v ?? "").toString().trim();
      if (!val) return;

      // ne engedjük, hogy a modal felülírja a paging/sortot
      if (k === 'page' || k === 'pageSize' || k === 'sort' || k === 'order' || k === 'search')
        return;

      cleaned[k] = val;
    });

    activeFilters = cleaned;

    console.log('[taskBejelentesLoadMore] apply filters', activeFilters);

    // reset + első oldal betöltése
    page = 1;
    renderedIds.clear();
    reachedEnd = false;
    totalCount = null;

    tbody.innerHTML = '';
    btn.disabled = false;
    btn.textContent = 'Több betöltése';
    setInfo('');

    closeModal();
    loadMore();
  });

  // Biztonság: ha a gomb onclick submitol, az ugyanúgy ide fut be
})();



    // --------------------------------------------------
    // APPLY FILTERS (Advanced filter modal -> LoadMore reset)
    // --------------------------------------------------
    window.addEventListener('tasks:applyFilters', function (e) {
      var params = e && e.detail && (e.detail.params || e.detail);
      if (!params || typeof params !== 'object') return;

      // mentsük el az aktív szűrőket (page/pageSize/sort/order/search ne innen jöjjön)
      var cleaned = {};
      Object.keys(params).forEach(function (k) {
        var v = params[k];
        if (v == null) return;
        v = String(v).trim();
        if (!v) return;

        // ezek ne írják felül a LoadMore logikát
        if (k === 'page' || k === 'pageSize' || k === 'sort' || k === 'order' || k === 'search') return;

        cleaned[k] = v;
      });

      activeFilters = cleaned;

      // reset + első oldal betöltése
      page = 1;
      renderedIds.clear();
      reachedEnd = false;
      totalCount = null;

      tbody.innerHTML = '';
      btn.disabled = false;
      btn.textContent = 'Több betöltése';
      setInfo('');

      loadMore();
    });

    // opcionális: clear event (ha a modal JS küldi)
    window.addEventListener('tasks:clearFilters', function () {
      activeFilters = {};

      page = 1;
      renderedIds.clear();
      reachedEnd = false;
      totalCount = null;

      tbody.innerHTML = '';
      btn.disabled = false;
      btn.textContent = 'Több betöltése';
      setInfo('');

      loadMore();
    });



    window.addEventListener('tasks:reload', function () {
      page = 1;
      renderedIds.clear();
      reachedEnd = false;
      totalCount = null;

      tbody.innerHTML = '';
      btn.disabled = false;
      btn.textContent = 'Több betöltése';
      setInfo('');

      loadMore();
    });

    window.addEventListener('tasks:deleted', function (e) {
      var id = parseInt(e && e.detail && e.detail.id, 10);
      if (!Number.isFinite(id)) return;

      if (renderedIds.has(id)) renderedIds.delete(id);
      if (typeof totalCount === 'number' && totalCount > 0) totalCount -= 1;

      setInfoLoaded();
      if (typeof totalCount === 'number' && renderedIds.size >= totalCount) setNoMore();
    });

    // init
    loadMore();
  });
})();
