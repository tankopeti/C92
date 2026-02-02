// wwwroot/js/Task/taskIntezkedesAdvancedFilter.js
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var modalEl = document.getElementById('advancedFilterModal');
        if (!modalEl) return;

        var formEl = document.getElementById('advancedFilterForm');
        if (!formEl) return;

        var assignedEl = document.getElementById('assignedToFilterSelect');
        var relatedPartnerEl = document.getElementById('relatedPartnerFilterSelect');

        function getQueryValue(name) {
            try {
                var u = new URL(window.location.href);
                return u.searchParams.get(name);
            } catch (e) {
                return null;
            }
        }

        async function loadAssigneesSelectOptions() {
            if (!assignedEl) return;

            assignedEl.disabled = true;
            assignedEl.innerHTML = '<option value="">Betöltés...</option>';

            try {
                var res = await fetch('/api/tasks/assignees/select', {
                    headers: { 'Accept': 'application/json' },
                    credentials: 'same-origin'
                });
                if (!res.ok) throw new Error('HTTP ' + res.status);

                var items = await res.json();
                if (!Array.isArray(items)) items = [];

                assignedEl.innerHTML =
                    '<option value="">-- Minden --</option>' +
                    items.map(function (x) {
                        return '<option value="' + String(x.id) + '">' + String(x.text) + '</option>';
                    }).join('');
            } catch (e) {
                console.error('[advancedFilter] assignees load failed', e);
                assignedEl.innerHTML = '<option value="">-- Nem sikerült betölteni --</option>';
            } finally {
                assignedEl.disabled = false;
            }
        }

        function initTomSelect(selectEl) {
            if (!selectEl || selectEl.tomselect) return selectEl.tomselect;

            // ✅ Bootstrap modal fix: dropdownParent legyen a modal,
            // így nem “a háttérben” nyílik és nem takarja el a backdrop.
            return new TomSelect(selectEl, {
                allowEmptyOption: true,
                create: false,
                dropdownParent: modalEl,
                plugins: ['clear_button']
            });
        }

        // ✅ z-index fix (biztosra megyünk)
        // Bootstrap modal z-index ~1055, a dropdown legyen fölötte
        function ensureDropdownZIndex() {
            if (document.getElementById('tsZIndexFix')) return;
            var style = document.createElement('style');
            style.id = 'tsZIndexFix';
            style.textContent = `
        .ts-dropdown, .tomselect .ts-dropdown { z-index: 2000 !important; }
      `;
            document.head.appendChild(style);
        }

        modalEl.addEventListener('shown.bs.modal', async function () {
            // Felelős: betöltés API-ból (ha ezt akarod)
            await loadAssigneesSelectOptions();

            // Querystring value visszatöltés
            var selectedAssigned = getQueryValue('assignedToId') || '';
            if (assignedEl) assignedEl.value = selectedAssigned;

            // Kapcsolt partner: ezt a view már feltölti @foreach-fel,
            // itt csak visszatöltjük querystringből
            var selectedRelated = getQueryValue('relatedPartnerId') || '';
            if (relatedPartnerEl) relatedPartnerEl.value = selectedRelated;

            var clearBtn = document.getElementById('clearFiltersBtn');
            if (clearBtn) {
                clearBtn.addEventListener('click', function () {
                    // querystring eldobása, marad az aktuális path
                    window.location.href = window.location.pathname;
                });
            }

        });


        // (opcionális) ha bezárásnál nem akarod eldobni a TS instance-t, hagyd így.
        // Ha mégis újra akarod initelni minden nyitásnál, akkor itt destroy kellene.
    });
})();
