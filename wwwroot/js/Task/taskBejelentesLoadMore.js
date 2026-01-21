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
      console.warn('[taskBejelentesLoadMore] missing elements', { wrap: !!wrap, tbody: !!tbody, btn: !!btn });
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

    // ✅ a backend által adott valós összes (csak IsActive=1!) pl. 71
    var totalCount = null;

    function setInfoText(text) {
      if (info) info.textContent = text || '';
    }

    function setInfoLoaded() {
      var loaded = renderedIds.size;
      if (typeof totalCount === 'number') {
        if (loaded > totalCount) loaded = totalCount;
        setInfoText('Betöltve: ' + loaded + ' / ' + totalCount);
      } else {
        setInfoText('Betöltve: ' + loaded);
      }
    }

    function esc(s) {
      var div = document.createElement('div');
      div.textContent = s == null ? '' : String(s);
      return div.innerHTML;
    }

    function formatDateHu(iso) {
      if (!iso) return '–';
      var d = new Date(iso);
      if (Number.isNaN(d.getTime())) return '–';
      return d.toLocaleString('hu-HU', {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit'
      }).replace(',', '');
    }

    function renderRow(t) {
      var id = Number(t.id);
      var tr = document.createElement('tr');
      tr.setAttribute('data-task-id', String(id));

      var priorityColor = t.priorityColorCode || '#6c757d';
      var statusColor = (t.colorCode && String(t.colorCode).trim()) ? t.colorCode : '#6c757d';

      var assignedEmail = t.assignedToEmail || '';
      var assignedName = t.assignedToName || '';

      var assignedHtml = assignedEmail
        ? `<a class="js-assigned-mail" href="mailto:${esc(assignedEmail)}">${esc(assignedName)}</a>`
        : esc(assignedName);

      tr.innerHTML = `
        <td class="text-nowrap">${esc(id)}</td>
        <td class="text-nowrap">${esc(t.siteName || '')}</td>
        <td class="text-nowrap">${esc(t.city || '')}</td>
        <td class="text-nowrap">${esc(t.partnerName || '')}</td>
        <td class="text-nowrap">${esc(t.title || '')}</td>

        <td class="text-center">
          <span class="badge text-white clickable-priority-badge"
            style="background-color:${esc(priorityColor)}; cursor:pointer;"
            data-priority-id="${esc(t.taskPriorityPMId || '')}">
            ${esc(t.taskPriorityPMName || '')}
          </span>
        </td>

        <td class="text-nowrap">${esc(t.dueDate || '')}</td>

        <td class="text-center">
          <span class="badge text-white clickable-status-badge"
            style="background-color:${esc(statusColor)}; cursor:pointer;"
            data-status-id="${esc(t.taskStatusPMId || '')}">
            ${esc(t.taskStatusPMName || '')}
          </span>
        </td>

        <td class="text-nowrap">${esc(formatDateHu(t.createdDate))}</td>
        <td class="text-nowrap">${esc(formatDateHu(t.updatedDate))}</td>

        <!-- ✅ CSAK itt mailto -->
        <td class="text-nowrap">${assignedHtml}</td>

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
                      data-bs-toggle="dropdown">
                <i class="bi bi-three-dots-vertical"></i>
              </button>

              <ul class="dropdown-menu dropdown-menu-end">
                <li><a class="dropdown-item js-edit-task" href="#" data-task-id="${esc(id)}">Szerkesztés</a></li>
                <li><a class="dropdown-item btn-show-history" href="#" data-task-id="${esc(id)}">Előzmények</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item text-danger js-delete-task" href="#" data-task-id="${esc(id)}">Törlés</a></li>
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

      // oldal logika: TaskStatusPMId > 1000 (szűrés a BACKENDEN legyen)
      qs.set('minStatusId', '1001');

      var url = '/api/tasks/paged?' + qs.toString();
      var res = await fetch(url, { headers: { 'Accept': 'application/json' } });

      if (!res.ok) {
        var txt = await res.text().catch(function () { return ''; });
        throw new Error('HTTP ' + res.status + ' @ ' + url + ' :: ' + txt);
      }

      var json = await res.json();

      // PagedResult kompatibilis olvasás (camel + PascalCase)
      var items = Array.isArray(json)
        ? json
        : (json.items || json.Items || json.results || json.Results || []);

      // ✅ csak IsActive=1-hez számolt totalRecords/totalCount jöjjön a backendből
      var tc = Array.isArray(json)
        ? null
        : (json.totalCount ?? json.totalrecords ?? json.totalRecords ?? json.TotalRecords ?? json.TotalCount ?? null);

      return { items: items, totalCount: tc };
    }

    // ✅ GOMB: ne tűnjön el, csak disable
    function showLoadMore() {
      btn.style.display = '';
      btn.disabled = false;
      btn.textContent = 'Több betöltése';
    }

    function setNoMore() {
      btn.style.display = '';
      btn.disabled = true;
      btn.textContent = 'Nincs több';
    }

    function setButtonLoading(loading) {
      btn.style.display = '';
      btn.disabled = loading;
      btn.textContent = loading ? 'Betöltés...' : (reachedEnd ? 'Nincs több' : 'Több betöltése');
    }

    function recomputeEndState() {
      if (typeof totalCount === 'number') {
        reachedEnd = renderedIds.size >= totalCount;
      }
      if (reachedEnd) setNoMore();
      else showLoadMore();
    }

    async function loadMore() {
      if (isLoading || reachedEnd) return;

      isLoading = true;
      setButtonLoading(true);
      setInfoText('Betöltés...');

      try {
        var result = await fetchPage(page);
        var items = result.items || [];

        // ✅ valós összes (IsActive=1) pl. 71
        if (typeof result.totalCount === 'number') {
          totalCount = result.totalCount;
        }

        if (items.length === 0) {
          // ha nincs több item, akkor vége
          reachedEnd = true;

          if (renderedIds.size === 0) {
            tbody.innerHTML = `
              <tr>
                <td colspan="12" class="text-center py-5">
                  <div class="alert alert-info">Nincs megjeleníthető intézkedés.</div>
                </td>
              </tr>`;
          }

          setInfoLoaded();
          setNoMore();
          return;
        }

        // ✅ NINCS kliens oldali filter → így 20-asával nő a betöltött elemszám
        items.forEach(function (t) {
          var id = Number(t && t.id);
          if (!Number.isFinite(id)) return;
          if (renderedIds.has(id)) return;

          renderedIds.add(id);
          tbody.appendChild(renderRow(t));
        });

        // oldal léptetés
        // - ha a backend pageSize-t ad, akkor 20-asával nő a loaded
        // - utolsó oldalon lehet kevesebb (pl. +11)
        if (items.length < pageSize) {
          reachedEnd = true;
        } else {
          page += 1;
        }

        setInfoLoaded();
        recomputeEndState();

      } catch (e) {
        console.error('[taskBejelentesLoadMore] load failed', e);
        setInfoText('Hiba a betöltéskor (nézd meg a konzolt).');
        showLoadMore();
      } finally {
        isLoading = false;
        setButtonLoading(false);
      }
    }

    // ✅ csak erre az oldalra, wrap-on, capture módban
if (!wrap.dataset._delegationBound) {
  wrap.dataset._delegationBound = '1';

  wrap.addEventListener('click', function (e) {
        // ✅ mailto: default menjen, de más JS handler ne rondítson bele
        var mail = e.target.closest('a[href^="mailto:"]');
        if (mail) {
          e.stopPropagation();
          return;
        }

        // ✅ eye gomb: nyisson modalt
        var viewBtn = e.target.closest('.js-view-task-btn');
        if (viewBtn) {
          e.preventDefault();
          e.stopPropagation();

          var id = parseInt(viewBtn.dataset.taskId, 10);
          if (!Number.isFinite(id)) return;

          if (window.Tasks && typeof window.Tasks.openViewModal === 'function') {
            window.Tasks.openViewModal(id);
          } else {
            window.dispatchEvent(new CustomEvent('tasks:view', { detail: { id: id } }));
          }
          return;
        }

        // ✅ dropdown menü elemek
        var del = e.target.closest('.js-delete-task');
        if (del) {
          e.preventDefault();
          e.stopPropagation();
          var id2 = parseInt(del.dataset.taskId, 10);
          if (!Number.isFinite(id2)) return;
          window.dispatchEvent(new CustomEvent('tasks:openDelete', { detail: { id: id2 } }));
          return;
        }

        var edit = e.target.closest('.js-edit-task');
        if (edit) {
          e.preventDefault();
          e.stopPropagation();
          var id3 = parseInt(edit.dataset.taskId, 10);
          if (!Number.isFinite(id3)) return;
          window.dispatchEvent(new CustomEvent('tasks:openEdit', { detail: { id: id3 } }));
          return;
        }

        var hist = e.target.closest('.btn-show-history');
        if (hist) {
          e.preventDefault();
          e.stopPropagation();
          var id4 = parseInt(hist.dataset.taskId, 10);
          if (!Number.isFinite(id4)) return;
          window.dispatchEvent(new CustomEvent('tasks:history', { detail: { id: id4 } }));
          return;
        }

        // ✅ minden más kattintás a táblán belül: némítás (ne kattintható legyen a sor)
        var interactive = e.target.closest('button, a, input, select, textarea, .dropdown, .dropdown-menu, [role="button"]');
        if (!interactive) e.stopPropagation();
      }, true);
    }

    btn.addEventListener('click', function (e) {
      e.preventDefault();
      loadMore();
    });

    window.addEventListener('tasks:reload', function () {
      page = 1;
      renderedIds.clear();
      reachedEnd = false;
      isLoading = false;
      totalCount = null;
      tbody.innerHTML = '';
      setInfoText('');
      showLoadMore();
      loadMore();
    });

    // ✅ törlés: a sor eltűnik a DOM-ból (taskDelete.js), itt a számláló + totalCount csökken
    window.addEventListener('tasks:deleted', function (e) {
      var id = parseInt(e && e.detail && e.detail.id, 10);
      if (!Number.isFinite(id)) return;

      if (renderedIds.has(id)) renderedIds.delete(id);

      // ✅ összes csökkentése: 71 -> 70 -> 69 (IsActive=0 ne számítson bele)
      if (typeof totalCount === 'number' && totalCount > 0) {
        totalCount = totalCount - 1;
      }

      setInfoLoaded();
      recomputeEndState();
    });

    // init
    showLoadMore();
    loadMore();
  });
})();
