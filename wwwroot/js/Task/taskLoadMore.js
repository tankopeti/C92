// // /wwwroot/js/Task/taskLoadMore.js
// // Feladat lista "Több betöltése" – Paged API (page/pageSize) + querystring forward + duplikáció védelem
// // FONTOS: ha a backend /api/tasks/paged PagedResult-ot ad vissza (items + totalRecords + totalPages),
// // akkor ebből dolgozunk. Ha nálad más a property-név, jelezd és átírom.

// document.addEventListener('DOMContentLoaded', function () {
//   console.log('[taskLoadMore] init (paged)');

//   const tableBody = document.getElementById('tasksTableBody');
//   const loadMoreBtn = document.getElementById('loadMoreTasksBtn');
//   const loadMoreContainer = document.getElementById('loadMoreTasksContainer');

//   if (!tableBody || !loadMoreBtn) {
//     console.warn('[taskLoadMore] tasksTableBody vagy loadMoreTasksBtn hiányzik – kilépés');
//     return;
//   }

//   // 12 adat oszlop + Actions
//   const TOTAL_COLS = 13;

//   // pageSize: NaN-safe
//   const pageSizeRaw = document.querySelector('.table-dynamic-height')?.dataset?.pageSize;
//   const pageSize = Number.isFinite(parseInt(pageSizeRaw, 10)) ? parseInt(pageSizeRaw, 10) : 20;
//   console.log('[taskLoadMore] pageSizeRaw=', pageSizeRaw, '=> pageSize=', pageSize);

//   let isLoading = false;

//   // ✅ Paged API-hoz page-alapú state kell (NE skip!)
//   let currentPage = 1;
//   let totalRecords = null; // a backend adja vissza
//   const renderedIds = new Set(); // duplikáció védelem

//   function setLoadingState(loading) {
//     isLoading = loading;
//     loadMoreBtn.disabled = loading;
//     loadMoreBtn.textContent = loading ? 'Betöltés...' : 'Több betöltése';
//   }

//   function showLoadMore() {
//     if (loadMoreContainer) loadMoreContainer.style.display = 'block';
//     loadMoreBtn.disabled = false;
//     loadMoreBtn.textContent = 'Több betöltése';
//   }

//   // NEM rejtjük el, csak letiltjuk
//   function setNoMore() {
//     if (loadMoreContainer) loadMoreContainer.style.display = 'block';
//     loadMoreBtn.disabled = true;
//     loadMoreBtn.textContent = 'Nincs több';
//   }

//   function getParamAny(qs, ...keys) {
//     for (const k of keys) {
//       const v = qs.get(k);
//       if (v !== null && v !== undefined && String(v).trim() !== '') return String(v).trim();
//     }
//     return '';
//   }

//   function buildApiUrl() {
//     const qs = new URLSearchParams(window.location.search);

//     const searchTerm = getParamAny(qs, 'SearchTerm', 'searchTerm', 'search');

//     const sort = getParamAny(qs, 'sort', 'Sort', 'SortBy', 'sortBy') || 'CreatedDate';
//     const order = (getParamAny(qs, 'order', 'Order', 'sortDir', 'SortDir') || 'desc').toLowerCase() === 'asc'
//       ? 'asc'
//       : 'desc';

//     const params = new URLSearchParams();
//     params.set('page', String(currentPage));
//     params.set('pageSize', String(pageSize));
//     params.set('sort', sort);
//     params.set('order', order);

//     if (searchTerm) params.set('search', searchTerm);

//     // ide később jöhetnek filterek is, ha bevezeted:
//     // params.set('statusId', ...)
//     // params.set('priorityId', ...)
//     // params.set('partnerId', ...)
//     // params.set('siteId', ...)
//     // params.set('assignedToId', ...)
//     // params.set('createdDateFrom', ...)
//     // params.set('createdDateTo', ...)
//     // params.set('dueDateFrom', ...)
//     // params.set('dueDateTo', ...)

//     return `/api/tasks/paged?${params.toString()}`;
//   }

//   function formatDate(dateString) {
//     if (!dateString) return '';
//     try {
//       const d = new Date(dateString);
//       if (isNaN(d.getTime())) return '';
//       return d.toLocaleDateString('hu-HU');
//     } catch {
//       return '';
//     }
//   }

//   function esc(s) {
//     return String(s ?? '')
//       .replaceAll('&', '&amp;')
//       .replaceAll('<', '&lt;')
//       .replaceAll('>', '&gt;')
//       .replaceAll('"', '&quot;')
//       .replaceAll("'", '&#39;');
//   }

//   function badge(text) {
//     const t = esc(text || '');
//     if (!t) return '';
//     return `<span class="badge bg-secondary">${t}</span>`;
//   }

// function renderRow(t) {
//   const idNum = Number(t.id);

//   const title = t.title ?? '';
//   const siteName = t.siteName ?? '';
//   const city = t.city ?? '';
//   const createdByName = t.createdByName ?? '';
//   const assignedToName = t.assignedToName ?? '';
//   const typeName = t.taskTypePMName ?? '';
//   const prioName = t.taskPriorityPMName ?? '';
//   const statusName = t.taskStatusPMName ?? '';

//   const createdDate = formatDate(t.createdDate);
//   const dueDate = formatDate(t.dueDate);
//   const completedDate = formatDate(t.completedDate);

//   return `
// <tr data-task-id="${esc(idNum)}">
//   <td class="text-nowrap" style="width:80px">
//     <a href="#" class="js-view-task" data-task-id="${esc(idNum)}">${esc(idNum)}</a>
//   </td>

//   <td class="text-nowrap" style="width:240px">
//     <a href="#" class="js-view-task" data-task-id="${esc(idNum)}">${esc(siteName)}</a>
//   </td>

//   <td class="text-nowrap" style="width:120px">
//     <a href="#" class="js-view-task" data-task-id="${esc(idNum)}">${esc(city)}</a>
//   </td>

//   <td class="text-nowrap" style="width:160px">
//     <a href="#" class="js-view-task" data-task-id="${esc(idNum)}">${esc(createdByName)}</a>
//   </td>

//   <td class="text-nowrap">
//     <a href="#" class="js-view-task" data-task-id="${esc(idNum)}">${esc(title)}</a>
//   </td>

//   <td class="text-nowrap" style="width:120px">${badge(typeName)}</td>
//   <td class="text-nowrap" style="width:110px">${badge(prioName)}</td>

//   <td class="text-nowrap" style="width:140px">${esc(createdDate)}</td>
//   <td class="text-nowrap" style="width:140px">${esc(dueDate)}</td>

//   <td class="text-nowrap" style="width:160px">${esc(assignedToName)}</td>
//   <td class="text-nowrap" style="width:140px">${esc(completedDate)}</td>

//   <td class="text-nowrap" style="width:110px">${badge(statusName)}</td>

//   <td style="width:110px">
//     <div class="btn-group btn-group-sm" role="group">
//       <button type="button" class="btn btn-outline-info js-view-task" data-task-id="${esc(idNum)}">
//         <i class="bi bi-eye"></i>
//       </button>

//       <div class="dropdown">
//         <button class="btn btn-outline-secondary dropdown-toggle btn-sm" type="button" data-bs-toggle="dropdown">
//           <i class="bi bi-three-dots-vertical"></i>
//         </button>

//         <ul class="dropdown-menu dropdown-menu-end">
//           <li>
//             <a class="dropdown-item js-edit-task" href="#" data-task-id="${esc(idNum)}">
//               <i class="bi bi-pencil-square me-2"></i>Szerkesztés
//             </a>
//           </li>
//           <li>
//             <a class="dropdown-item btn-show-history" href="#" data-task-id="${esc(idNum)}">
//               <i class="bi bi-clock-history me-2"></i>Előzmények
//             </a>
//           </li>
//           <li><hr class="dropdown-divider"></li>
//           <li>
//             <a class="dropdown-item text-danger js-delete-task" href="#" data-task-id="${esc(idNum)}">
//               Törlés
//             </a>
//           </li>
//         </ul>
//       </div>
//     </div>
//   </td>
// </tr>`;
// }

//   function renderEmpty() {
//     tableBody.innerHTML = `
// <tr>
//   <td colspan="${TOTAL_COLS}" class="text-center py-5 text-muted">
//     Nincs megjeleníthető feladat
//   </td>
// </tr>`;
//   }

//   function applyNoMoreIfKnown() {
//     if (typeof totalRecords === 'number' && totalRecords >= 0) {
//       const loadedCount = renderedIds.size;
//       if (loadedCount >= totalRecords) {
//         setNoMore();
//       }
//     }
//   }

//   async function loadTasks() {
//     if (isLoading) return;

//     setLoadingState(true);

//     try {
//       const url = buildApiUrl();
//       console.log('[taskLoadMore] fetch:', url);

//       const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
//       if (!res.ok) throw new Error(`HTTP ${res.status}`);

//       const data = await res.json();

//       // Várható PagedResult:
//       // { items: [...], totalRecords: 123, totalPages: 7, currentPage: 1, pageSize: 20 }
//       const items = data?.items ?? data?.Items ?? null;
//       const tr = data?.totalRecords ?? data?.TotalRecords ?? null;

//       // ha első hívás és a placeholder sor bent van, ürítsük
//       if (currentPage === 1) tableBody.innerHTML = '';

//       if (typeof tr === 'number') totalRecords = tr;

//       if (!Array.isArray(items) || items.length === 0) {
//         if (currentPage === 1) renderEmpty();
//         setNoMore();
//         return;
//       }

//       // ✅ duplikáció védelem + render
//       let renderedNow = 0;
//       for (const t of items) {
//         const id = Number(t?.id);
//         if (!Number.isFinite(id)) continue;

//         if (renderedIds.has(id)) continue; // ne duplikáljon
//         renderedIds.add(id);

//         tableBody.insertAdjacentHTML('beforeend', renderRow(t));
//         renderedNow++;
//       }

//       console.log('[taskLoadMore] page=', currentPage, 'items=', items.length, 'renderedNow=', renderedNow, 'totalRendered=', renderedIds.size, 'totalRecords=', totalRecords);

//       // ✅ előrelépünk oldalban (ez volt a hiányzó)
//       currentPage++;

//       // ✅ van-e még?
//       // 1) ha a backend totalRecords-t ad → abból pontos
//       // 2) ha nem ad → fallback: ha kevesebb jött, mint pageSize, akkor nincs több
//       if (typeof totalRecords === 'number') {
//         applyNoMoreIfKnown();
//         if (loadMoreBtn.disabled !== true) showLoadMore();
//       } else {
//         (items.length < pageSize) ? setNoMore() : showLoadMore();
//       }

//     } catch (err) {
//       console.error('[taskLoadMore] load failed:', err);
//       // hiba esetén ne rejtsük el végleg a gombot
//       showLoadMore();
//     } finally {
//       setLoadingState(false);
//     }
//   }

//   // ---- Events ----
//   loadMoreBtn.addEventListener('click', loadTasks);

//   // ha később JS-es filter (pushState) lesz, ezt használd:
//   window.addEventListener('tasks:reload', () => {
//     if (isLoading) return;

//     currentPage = 1;
//     totalRecords = null;
//     renderedIds.clear();

//     tableBody.innerHTML = '';
//     showLoadMore();

//     loadTasks();
//   });

//   // init
//   showLoadMore();
//   loadTasks();
//   document.addEventListener('click', function (e) {
//   const view = e.target.closest('.js-view-task');
//   if (view) {
//     e.preventDefault();
//     const id = parseInt(view.dataset.taskId, 10);
//     if (window.Tasks?.openViewModal) window.Tasks.openViewModal(id);
//     else window.dispatchEvent(new CustomEvent('tasks:view', { detail: { id } }));
//     return;
//   }

//   const del = e.target.closest('.js-delete-task');
//   if (del) {
//     e.preventDefault();
//     const id = parseInt(del.dataset.taskId, 10);
//     window.dispatchEvent(new CustomEvent('tasks:openDelete', { detail: { id } }));
//     return;
//   }

//   const edit = e.target.closest('.js-edit-task');
//   if (edit) {
//     e.preventDefault();
//     const id = parseInt(edit.dataset.taskId, 10);
//     window.dispatchEvent(new CustomEvent('tasks:openEdit', { detail: { id } }));
//     return;
//   }
// });

// });
