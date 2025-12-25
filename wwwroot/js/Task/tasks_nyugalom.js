// wwwroot/js/tasks_nyugalom.js
document.addEventListener('DOMContentLoaded', () => {
    console.log('%c[NYUGALOM] JS aktív – TomSelect inicializálva', 'color:#9c27b0;font-weight:bold;font-size:14px');

    // ====================== ÚJ FELADAT MODAL – TELEPHELY KERESÉS ======================
    const modalEl = document.getElementById('newTaskModal');
    if (!modalEl) {
        console.warn('[NYUGALOM] Modal nem található: #newTaskModal');
    } else {
        modalEl.addEventListener('shown.bs.modal', initModalOnce);

        let tomSelectInstance = null;

        function initModalOnce() {
            const form = modalEl.querySelector('form');
            if (!form) return;

            // Fix TaskTypePMId (ha nincs a formban)
            if (!form.querySelector('input[name="TaskTypePMId"]')) {
                const hiddenTaskType = document.createElement('input');
                hiddenTaskType.type = 'hidden';
                hiddenTaskType.name = 'TaskTypePMId';
                hiddenTaskType.value = '1';
                form.appendChild(hiddenTaskType);
            }

            // Auto PartnerId hidden input
            let partnerInput = document.getElementById('autoPartnerId');
            if (!partnerInput) {
                partnerInput = document.createElement('input');
                partnerInput.type = 'hidden';
                partnerInput.name = 'PartnerId';
                partnerInput.id = 'autoPartnerId';
                form.appendChild(partnerInput);
            }

            const siteSelect = form.querySelector('[name="SiteId"]');
            if (!siteSelect || siteSelect.tomselect) {
                tomSelectInstance = siteSelect?.tomselect || null;
                return;
            }

            tomSelectInstance = new TomSelect(siteSelect, {
                maxOptions: 300,
                valueField: 'id',
                labelField: 'text',
                searchField: ['text', 'partnerName', 'partnerDetails'],
                placeholder: 'Keresés telephely névre, városra vagy partnerre...',
                load: function(query, callback) {
                    if (!query || query.length < 2) return callback();
                    fetch(`/api/nyugalom/sites/all/select?search=${encodeURIComponent(query)}`)
                        .then(r => r.json())
                        .then(callback)
                        .catch(() => callback());
                },
                render: {
                    option: function(item, escape) {
                        return `
                            <div class="py-2 px-3">
                                <div class="font-medium text-gray-900">${escape(item.text)}</div>
                                <div class="text-sm text-gray-600 mt-1">
                                    Partner: <strong>${escape(item.partnerDetails || '–')}</strong>
                                </div>
                            </div>
                        `;
                    },
                    item: function(item, escape) {
                        return `
                            <div class="inline-flex items-center">
                                <span class="font-medium">${escape(item.text)}</span>
                                <span class="ml-2 text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
                                    ${escape(item.partnerName || 'nincs partner')}
                                </span>
                            </div>
                        `;
                    },
                    no_results: function() {
                        return '<div class="py-2 px-3 text-center text-gray-500">Nincs találat</div>';
                    }
                },
                onItemAdd: function(value) {
                    const item = this.options[value];
                    if (item) {
                        partnerInput.value = item.partnerId || '';
                        const infoEl = modalEl.querySelector('#selected-partner-info');
                        if (infoEl) {
                            infoEl.innerHTML = `Partner: <strong>${escapeHtml(item.partnerDetails || '–')}</strong>`;
                            infoEl.classList.remove('d-none');
                        }
                    }
                },
                onItemRemove: function() {
                    partnerInput.value = '';
                    const infoEl = modalEl.querySelector('#selected-partner-info');
                    if (infoEl) infoEl.classList.add('d-none');
                }
            });
        }

        modalEl.addEventListener('hidden.bs.modal', () => {
            if (tomSelectInstance) {
                tomSelectInstance.clear();
                tomSelectInstance.clearOptions();
            }
        });
    }

    // ====================== STÁTUSZ GYORS MÓDOSÍTÁS ======================
    const changeStatusModalEl = document.getElementById('changeStatusModal');
    const currentStatusBadge = document.getElementById('currentStatusBadge');
    const newStatusSelect = document.getElementById('newStatusSelect');
    const saveStatusBtn = document.getElementById('saveStatusBtn');

    let currentTaskId = null;

    const tableEl = document.querySelector('table');

    if (tableEl) {
        tableEl.addEventListener('click', function(e) {
            const badge = e.target.closest('.clickable-status-badge');
            if (!badge || !changeStatusModalEl) return;

            e.stopPropagation();
            e.preventDefault();

            const row = badge.closest('tr');
            currentTaskId = row.dataset.taskId;

            // Előbb tisztítjuk
            if (currentStatusBadge) {
                currentStatusBadge.textContent = '';
                currentStatusBadge.style.backgroundColor = '';
            }
            if (newStatusSelect) {
                newStatusSelect.value = '';
            }

            // Aztán beállítjuk az aktuálisat
            if (currentStatusBadge) {
                currentStatusBadge.textContent = badge.textContent.trim();
                currentStatusBadge.style.backgroundColor = badge.style.backgroundColor;
            }

            const currentStatusId = badge.dataset.statusId || '';
            if (currentStatusId && newStatusSelect) {
                newStatusSelect.value = currentStatusId;

                const selectedOption = newStatusSelect.options[newStatusSelect.selectedIndex];
                if (selectedOption && currentStatusBadge) {
                    currentStatusBadge.style.backgroundColor = selectedOption.dataset.color || '#6c757d';
                }
            }

            const modal = new bootstrap.Modal(changeStatusModalEl);
            modal.show();
        });
    }

    if (newStatusSelect) {
        newStatusSelect.addEventListener('change', function() {
            const selected = this.options[this.selectedIndex];
            const color = selected?.dataset.color || '#6c757d';
            if (currentStatusBadge) {
                currentStatusBadge.style.backgroundColor = color;
            }
        });
    }

    if (saveStatusBtn) {
        saveStatusBtn.addEventListener('click', async () => {
            if (!currentTaskId) return;

            const newStatusId = parseInt(newStatusSelect.value);
            if (isNaN(newStatusId)) {
                alert('Érvénytelen státusz');
                return;
            }

            try {
                const response = await fetch('/api/nyugalom/taskstatuses/change', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({
                        taskId: currentTaskId,
                        newStatusId: newStatusId
                    })
                });

                const result = await response.json();

                if (result.success) {
                    const badge = document.querySelector(`tr[data-task-id="${currentTaskId}"] .clickable-status-badge`);
                    if (badge) {
                        badge.textContent = result.statusName;
                        badge.style.backgroundColor = result.colorCode;
                        badge.dataset.statusId = newStatusId;
                    }

                    bootstrap.Modal.getInstance(changeStatusModalEl).hide();

                    if (typeof showToast === 'function') {
                        showToast(`Státusz módosítva: ${result.statusName}`, 'success');
                    }
                } else {
                    alert('Hiba: ' + (result.message || 'Ismeretlen hiba'));
                }
            } catch (err) {
                console.error('Státusz módosítás hiba:', err);
                alert('Nem sikerült a módosítás');
            }
        });
    }

    // Bezáráskor teljes tisztítás (státusz)
    if (changeStatusModalEl) {
        changeStatusModalEl.addEventListener('hidden.bs.modal', () => {
            if (currentStatusBadge) {
                currentStatusBadge.textContent = '';
                currentStatusBadge.style.backgroundColor = '';
            }
            if (newStatusSelect) {
                newStatusSelect.value = '';
            }
            currentTaskId = null;
        });
    }

    // ====================== PRIORITÁS GYORS MÓDOSÍTÁS ======================
    const changePriorityModalEl = document.getElementById('changePriorityModal');
    const currentPriorityBadge = document.getElementById('currentPriorityBadge');
    const newPrioritySelect = document.getElementById('newPrioritySelect');
    const savePriorityBtn = document.getElementById('savePriorityBtn');

    let currentTaskIdForPriority = null;

    if (tableEl) {
        tableEl.addEventListener('click', function(e) {
            const badge = e.target.closest('.clickable-priority-badge');
            if (!badge || !changePriorityModalEl) return;

            e.stopPropagation();
            e.preventDefault();

            const row = badge.closest('tr');
            currentTaskIdForPriority = row.dataset.taskId;

            // Előbb tisztítjuk
            if (currentPriorityBadge) {
                currentPriorityBadge.textContent = '';
                currentPriorityBadge.style.backgroundColor = '';
            }
            if (newPrioritySelect) {
                newPrioritySelect.value = '';
            }

            // Aztán beállítjuk az aktuálisat
            if (currentPriorityBadge) {
                currentPriorityBadge.textContent = badge.textContent.trim();
                currentPriorityBadge.style.backgroundColor = badge.style.backgroundColor;
            }

            const currentPriorityId = badge.dataset.priorityId || '';
            if (currentPriorityId && newPrioritySelect) {
                newPrioritySelect.value = currentPriorityId;

                const selectedOption = newPrioritySelect.options[newPrioritySelect.selectedIndex];
                if (selectedOption && currentPriorityBadge) {
                    currentPriorityBadge.style.backgroundColor = selectedOption.dataset.color || '#82D4BB';
                }
            }

            const modal = new bootstrap.Modal(changePriorityModalEl);
            modal.show();
        });
    }

    if (newPrioritySelect) {
        newPrioritySelect.addEventListener('change', function() {
            const selected = this.options[this.selectedIndex];
            const color = selected?.dataset.color || '#82D4BB';
            if (currentPriorityBadge) {
                currentPriorityBadge.style.backgroundColor = color;
            }
        });
    }

    if (savePriorityBtn) {
        savePriorityBtn.addEventListener('click', async () => {
            if (!currentTaskIdForPriority) return;

            const newPriorityId = parseInt(newPrioritySelect.value);
            if (isNaN(newPriorityId)) {
                alert('Érvénytelen prioritás');
                return;
            }

            try {
                const response = await fetch('/api/nyugalom/taskpriorities/change', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({
                        taskId: currentTaskIdForPriority,
                        newPriorityId: newPriorityId
                    })
                });

                const result = await response.json();

                if (result.success) {
                    const badge = document.querySelector(`tr[data-task-id="${currentTaskIdForPriority}"] .clickable-priority-badge`);
                    if (badge) {
                        badge.textContent = result.priorityName;
                        badge.style.backgroundColor = result.colorCode || "#6c757d";
                        badge.dataset.priorityId = newPriorityId;
                    }

                    bootstrap.Modal.getInstance(changePriorityModalEl).hide();

                    if (typeof showToast === 'function') {
                        showToast(`Prioritás módosítva: ${result.priorityName}`, 'success');
                    }
                } else {
                    alert('Hiba: ' + (result.message || 'Ismeretlen hiba'));
                }
            } catch (err) {
                console.error('Prioritás módosítás hiba:', err);
                alert('Nem sikerült a módosítás');
            }
        });
    }

    // Bezáráskor teljes tisztítás (prioritás)
    if (changePriorityModalEl) {
        changePriorityModalEl.addEventListener('hidden.bs.modal', () => {
            if (currentPriorityBadge) {
                currentPriorityBadge.textContent = '';
                currentPriorityBadge.style.backgroundColor = '';
            }
            if (newPrioritySelect) {
                newPrioritySelect.value = '';
            }
            currentTaskIdForPriority = null;
        });
    }

    // ====================== ÖSSZETETT SZŰRŐ MODAL – DEBUG MÓD TOMSELECT SITE ======================
    const advancedFilterModalEl = document.getElementById('advancedFilterModal');
    if (advancedFilterModalEl) {
        console.log('%c[DEBUG] Összetett szűrő modal megtalálva', 'color: orange; font-weight: bold');

        let siteTomSelectInFilter = null;

        advancedFilterModalEl.addEventListener('shown.bs.modal', function () {
            console.log('%c[DEBUG] Összetett szűrő modal megnyílt (shown.bs.modal)', 'color: cyan');

            const siteSelect = advancedFilterModalEl.querySelector('select[name="siteId"]');

            if (!siteSelect) {
                console.error('[DEBUG] HIBA: Nem található select[name="siteId"] az advancedFilterModal-ban!');
                return;
            }

            console.log('[DEBUG] Site select elem megtalálva:', siteSelect);

            if (siteSelect.tomselect) {
                console.log('[DEBUG] TomSelect már inicializálva van ezen a select-en, skip.');
                siteTomSelectInFilter = siteSelect.tomselect;
                return;
            }

            console.log('%c[DEBUG] TomSelect inicializálása indul a szűrő site select-re...', 'color: green');

            try {
                siteTomSelectInFilter = new TomSelect(siteSelect, {
                    maxOptions: 300,
                    valueField: 'id',
                    labelField: 'text',
                    searchField: ['text', 'partnerName', 'partnerDetails'],
                    placeholder: 'Keresés telephely névre, városra vagy partnerre...',

                    load: function(query, callback) {
                        console.log(`%c[DEBUG] TomSelect load hívás – query: "${query}"`, 'color: blue');

                        if (!query || query.length < 2) {
                            console.log('[DEBUG] Query túl rövid (<2 karakter), callback üres listával');
                            return callback();
                        }

                        const url = `/api/nyugalom/sites/all/select?search=${encodeURIComponent(query)}`;
                        console.log('[DEBUG] Fetch URL:', url);

                        fetch(url)
                            .then(response => {
                                console.log('[DEBUG] Fetch válasz státusz:', response.status, response.statusText);

                                if (!response.ok) {
                                    throw new Error(`HTTP ${response.status}`);
                                }
                                return response.json();
                            })
                            .then(data => {
                                console.log('[DEBUG] API válasz megérkezett, elemek száma:', data.length);
                                console.log('[DEBUG] Első néhány elem:', data.slice(0, 3));
                                callback(data);
                            })
                            .catch(error => {
                                console.error('[DEBUG] HIBA a fetch során:', error);
                                console.error('[DEBUG] Hiba részletek:', error.message);
                                callback();
                            });
                    },

                    render: {
                        option: function(item, escape) {
                            return `
                                <div class="py-2 px-3">
                                    <div class="font-medium text-gray-900">${escape(item.text)}</div>
                                    <div class="text-sm text-gray-600 mt-1">
                                        Partner: <strong>${escape(item.partnerDetails || '–')}</strong>
                                    </div>
                                </div>
                            `;
                        },
                        item: function(item, escape) {
                            return `
                                <div class="inline-flex items-center">
                                    <span class="font-medium">${escape(item.text)}</span>
                                    <span class="ml-2 text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
                                        ${escape(item.partnerName || 'nincs partner')}
                                    </span>
                                </div>
                            `;
                        },
                        no_results: function() {
                            return '<div class="py-2 px-3 text-center text-gray-500">Nincs találat</div>';
                        }
                    },

                    onInitialize: function() {
                        console.log('%c[DEBUG] TomSelect sikeresen inicializálva a szűrő site select-en!', 'color: lime; font-weight: bold');
                    }
                });
            } catch (err) {
                console.error('[DEBUG] Kivétel a TomSelect létrehozása közben:', err);
            }
        });

        advancedFilterModalEl.addEventListener('hidden.bs.modal', function () {
            console.log('[DEBUG] Összetett szűrő modal bezárva');
            if (siteTomSelectInFilter) {
                console.log('[DEBUG] TomSelect cleanup: clear + clearOptions');
                siteTomSelectInFilter.clear();
                siteTomSelectInFilter.clearOptions();
            }
        });
    } else {
        console.warn('[DEBUG] advancedFilterModal elem NEM található a DOM-ban!');
    }

    // ====================== SEGÉDFÜGGVÉNY ======================
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ====================== FELADAT ELŐZMÉNYEK MEGJELENÍTÉSE ======================
    const taskHistoryModalEl = document.getElementById('taskHistoryModal');
    const historyTaskTitle = document.getElementById('historyTaskTitle');
    const historyLoading = document.getElementById('historyLoading');
    const historyContent = document.getElementById('historyContent');
    const historyEmpty = document.getElementById('historyEmpty');
    const historyList = document.getElementById('historyList');

    if (tableEl) {
        tableEl.addEventListener('click', function(e) {
            const btn = e.target.closest('.btn-show-history');
            if (!btn || !taskHistoryModalEl) return;

            e.preventDefault();
            e.stopPropagation();

            const taskId = btn.dataset.taskId;
            const row = btn.closest('tr');
            const taskTitle = row.querySelector('td:nth-child(2)')?.textContent.trim() || 'Ismeretlen feladat';

            // Modal cím beállítása
            historyTaskTitle.textContent = taskTitle;

            // Állapotok visszaállítása
            historyLoading.classList.remove('d-none');
            historyContent.classList.add('d-none');
            historyEmpty.classList.add('d-none');
            historyList.innerHTML = '';

            // Modal megnyitása
            const modal = new bootstrap.Modal(taskHistoryModalEl);
            modal.show();

            // Előzmények lekérdezése
            // Előzmények lekérdezése
fetch(`/api/tasks/${taskId}/history`)
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP hiba: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log('Előzmények API válasz:', data);

        historyLoading.classList.add('d-none');
        historyContent.classList.remove('d-none');

        // FONTOS: a controller sima tömböt ad vissza, nem objektumot!
        let histories = Array.isArray(data) ? data : [];

        if (histories.length === 0) {
            historyEmpty.classList.remove('d-none');
            return;
        }

        // Legújabb elöl
        histories.sort((a, b) => 
            new Date(b.modifiedDate || b.ModifiedDate) - new Date(a.modifiedDate || a.ModifiedDate)
        );

        histories.forEach(h => {
            const item = document.createElement('div');
            item.className = 'timeline-item mb-4';

            const date = new Date(h.modifiedDate || h.ModifiedDate || new Date());
            const formattedDate = date.toLocaleString('hu-HU', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });

            const userName = h.modifiedByName || h.ModifiedByName || h.modifiedById || 'Rendszer';
            const description = h.changeDescription || h.ChangeDescription || 'Nincs leírás';

            item.innerHTML = `
                <div class="d-flex">
                    <div class="timeline-dot bg-info me-3 mt-1"></div>
                    <div class="flex-grow-1">
                        <div class="fw-medium text-primary">${userName}</div>
                        <div class="text-muted small">${formattedDate}</div>
                        <div class="mt-2 text-dark">${description}</div>
                    </div>
                </div>
            `;

            historyList.appendChild(item);
        });
    })
    .catch(err => {
        console.error('Előzmények betöltési hiba:', err);
        historyLoading.classList.add('d-none');
        historyContent.classList.remove('d-none');
        historyEmpty.classList.remove('d-none');
        historyEmpty.querySelector('p').textContent = 'Hiba történt az előzmények betöltésekor.';
    });
    
        });
    }
});