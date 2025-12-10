// wwwroot/js/TomSelectHelper.js
window.TomSelectHelper = (function () {
    const dropdownCacheStatic = {};

    // Log helper
    const log = (msg, ...args) => console.log(`%c[TomSelect] ${msg}`, 'color: #28a745; font-weight: bold;', ...args);
    const error = (msg, ...args) => console.error(`%c[TomSelect] ${msg}`, 'color: #dc3545; font-weight: bold;', ...args);

    // ---------------------------------------------------------------
    // POPULATE SELECT – A TELJES TomSelect logika itt van!
    // ---------------------------------------------------------------
    function populateSelect(selector, urlOrFn, selectedId = null) {
        log(`populateSelect: ${selector}`, { urlOrFn, selectedId });
        const select = document.querySelector(selector);
        if (!select) return error(`Select not found: ${selector}`);

        // Destroy existing TomSelect
        if (select.tomselect) {
            select.tomselect.destroy();
            select.tomselect = null;
        }

        const isMultiple = select.hasAttribute('multiple');
        const config = {
            valueField: 'id',
            labelField: 'text',
            searchField: ['text'],
            placeholder: select.getAttribute('placeholder') || 'Válasszon...',
            allowEmptyOption: true,
            openOnFocus: true,
            sortField: { field: 'text', direction: 'asc' },
            maxOptions: 100,
            plugins: isMultiple ? ['remove_button'] : []
        };

        function setValue(ts, value) {
            if (value === null || value === undefined) return;
            const finalValue = isMultiple && !Array.isArray(value) ? [value] : value;
            setTimeout(() => ts.setValue(finalValue), 10);
        }

        function initWithData(data, valueToSet) {
            config.options = data;
            const ts = new TomSelect(selector, config);
            setValue(ts, valueToSet);
            log(`TomSelect initialized: ${selector}`, { count: data.length });
            return ts;
        }

        // Static URL (cached)
        if (typeof urlOrFn === 'string') {
            const cacheKey = urlOrFn;
            if (dropdownCacheStatic[cacheKey]) {
                initWithData(dropdownCacheStatic[cacheKey], selectedId);
            } else {
                log(`Fetching static: ${urlOrFn}`);
                fetch(urlOrFn)
                    .then(r => r.ok ? r.json() : Promise.reject())
                    .then(data => {
                        dropdownCacheStatic[cacheKey] = data;
                        initWithData(data, selectedId);
                    })
                    .catch(err => {
                        error(`Fetch failed: ${urlOrFn}`, err);
                        dropdownCacheStatic[cacheKey] = [];
                        initWithData([], selectedId);
                    });
            }
            return;
        }

        // Dynamic function (pl. keresés)
        if (typeof urlOrFn === 'function') {
            config.load = function (query, callback) {
                const url = urlOrFn(this.control) + (query.trim() ? `?q=${encodeURIComponent(query)}` : '');
                log(`Dynamic load: ${url}`);
                fetch(url)
                    .then(r => r.ok ? r.json() : Promise.reject())
                    .then(json => callback(Array.isArray(json) ? json : (json.items || json.results || [])))
                    .catch(e => {
                        error('Load error', e);
                        callback([]);
                    });
            };
        }

        const ts = new TomSelect(selector, config);
        if (selectedId !== null && selectedId !== undefined) setValue(ts, selectedId);

        ts.control.addEventListener('click', () => {
            if (!ts.isOpen) { ts.load(''); ts.open(); }
        });

        return ts;
    }

    // ---------------------------------------------------------------
    // CASCADE (Partner → Site, Contact, stb.)
    // ---------------------------------------------------------------
    function setupPartnerCascade(form, dependentConfig, initialData = {}) {
        const partnerSelect = form.querySelector('[name="PartnerId"]');
        if (!partnerSelect || !partnerSelect.tomselect) return;

        const ts = partnerSelect.tomselect;
        ts.off('change');

        ts.on('change', () => {
            const partnerId = ts.getValue();
            log(`Partner changed → ${partnerId}`);

            dependentConfig.PartnerId.forEach(dep => {
                const target = form.querySelector(`[name="${dep.target}"]`);
                if (!target) return;

                if (target.tomselect) {
                    target.tomselect.clear();
                    target.tomselect.clearOptions();
                    target.tomselect.destroy();
                }

                const selectedId = initialData[dep.target.toLowerCase()] || null;
                const urlFn = partnerId ? () => dep.url(partnerId) : () => null;
                populateSelect(`[name="${dep.target}"]`, urlFn, selectedId);
            });
        });

        if (ts.getValue()) ts.trigger('change');
    }

    return { populateSelect, setupPartnerCascade };
})();