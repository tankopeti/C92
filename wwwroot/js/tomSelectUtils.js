window.c92 = window.c92 || {};

// VAT TomSelect initialization (used by quotes and orders)
window.c92.vatTypesCache = null;

window.c92.vatTypesCache = null;

async function fetchVatTypes() {
    if (window.c92.vatTypesCache) {
        console.log('Using cached VAT types');
        return window.c92.vatTypesCache;
    }
    try {
        const response = await fetch('/api/vat/types', {
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });
        if (!response.ok) {
            throw new Error(`Failed to fetch VAT types: ${response.status} ${response.statusText}`);
        }
        const data = await response.json();
        if (!Array.isArray(data) || data.length === 0) {
            console.warn('No VAT types returned from API');
            window.c92.showToast('warning', 'Nincsenek elérhető ÁFA típusok.');
            return [];
        }
        window.c92.vatTypesCache = data;
        return data;
    } catch (error) {
        console.error('Error fetching VAT types:', error);
        window.c92.showToast('error', 'Hiba az ÁFA típusok betöltése közben: ' + error.message);
        return [];
    }
}

window.c92.initializeVatTomSelect = async function (input, id, options = { context: 'order' }) {
    if (typeof TomSelect === 'undefined') {
        console.error('TomSelect library not loaded');
        window.c92.showToast('error', 'TomSelect könyvtár hiányzik.');
        return Promise.reject('TomSelect missing');
    }

    if (!input) {
        console.error('VAT select element is null or undefined');
        window.c92.showToast('error', 'ÁFA választó elem nem található.');
        return Promise.reject('VAT select element missing');
    }

    if (input.tomselect && input.tomselect.getValue()) {
        console.log('VAT select already initialized with value, skipping:', input, 'value:', input.tomselect.getValue());
        return input.tomselect;
    }

    let hiddenInput = input.nextElementSibling;
    if (!hiddenInput || hiddenInput.type !== 'hidden') {
        hiddenInput = document.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = input.name;
        input.parentNode.insertBefore(hiddenInput, input.nextSibling);
    }

    if (input.tomselect) {
        console.log('VAT select already initialized, destroying:', input);
        input.tomselect.destroy();
    }
    input.dataset.tomSelectInitialized = 'true';

    try {
        const data = await fetchVatTypes();
        if (data.length === 0) {
            input.dataset.tomSelectInitialized = '';
            return Promise.reject('No VAT types available');
        }

        input.innerHTML = '<option value="" disabled selected>-- Válasszon ÁFA típust --</option>';
        data.forEach(vat => {
            const option = new Option(vat.typeName, vat.vatTypeId, false, vat.vatTypeId === input.dataset.selectedId);
            option.dataset.rate = vat.rate;
            input.appendChild(option);
        });

        const selectedId = input.dataset.selectedId || input.value || '';
        const selectedText = (input.dataset.selectedText || '').replace(/ \?\? .*$/, '') || input.querySelector('option[selected]')?.text || '';
        const context = options.context || 'order';
        const calculateTotals = context === 'quote' ? window.calculateQuoteTotals : window.calculateOrderTotals;

        const tomSelect = new TomSelect(input, {
            create: false,
            maxItems: 1,
            valueField: 'vatTypeId',
            labelField: 'typeName',
            searchField: ['typeName'],
            options: data.map(v => ({
                vatTypeId: v.vatTypeId,
                typeName: v.typeName,
                rate: v.rate
            })),
            onInitialize() {
                console.log('VAT TomSelect initialized for input:', input, 'context:', context, 'options:', data, 'selectedId:', selectedId, 'selectedText:', selectedText);
                const exists = data.some(v => String(v.vatTypeId) === selectedId);
                if (selectedId && !exists && selectedText) {
                    console.warn(`Selected VAT ID ${selectedId} not in options, adding fallback`);
                    this.addOption({ vatTypeId: selectedId, typeName: selectedText, rate: parseFloat(input.dataset.selectedRate || 0) });
                }
                if (selectedId && exists) {
                    this.setValue(selectedId, true);
                    hiddenInput.value = selectedId;
                } else if (data[0]) {
                    this.setValue(data[0].vatTypeId, true);
                    hiddenInput.value = data[0].vatTypeId;
                }
                console.log('Post-initialize value:', this.getValue(), 'display:', this.getOption(this.getValue())?.typeName);
            },
            onChange(value) {
                console.log('VAT changed, value:', value, 'display:', this.getOption(value)?.typeName, 'context:', context);
                hiddenInput.value = value || '';
                let row = input.closest('tr');
                if (!row || (!row.classList.contains('order-item-row') && !row.classList.contains('quote-item-row'))) {
                    const itemId = input.name.match(/items\[([^\]]+)\]|\.orderItems\[([^\]]+)\]/)?.[1] || input.closest('[data-item-id]')?.dataset.itemId;
                    if (itemId) {
                        row = document.querySelector(`tr.order-item-row[data-item-id="${itemId}"], tr.quote-item-row[data-item-id="${itemId}"]`);
                    }
                    if (!row) {
                        console.warn('Parent row not found for VAT select:', input, 'itemId:', itemId, 'context:', context);
                        window.c92.showToast('warning', 'Szülő sor nem található az ÁFA választóhoz.');
                        return;
                    }
                }
                const rowClass = context === 'quote' ? 'quote-item-row' : 'order-item-row';
                if (!row.classList.contains(rowClass)) {
                    console.warn(`Parent row found but does not match expected class: expected ${rowClass}, found`, row.className);
                    window.c92.showToast('warning', `Szülő sor osztálya nem megfelelő: ${rowClass} helyett ${row.className}.`);
                    return;
                }
                const productSelect = row.querySelector('.tom-select-product');
                if (productSelect?.tomselect) {
                    const productId = productSelect.value || productSelect.dataset.selectedId;
                    const quantityInput = row.querySelector(context === 'quote' ? '.item-quantity' : '.quantity');
                    const unitPriceInput = row.querySelector(context === 'quote' ? '.item-unit-price' : '.unit-price');
                    const discountTypeSelect = row.querySelector('.discount-type-id');
                    const discountAmountInput = row.querySelector('.discount-value');
                    calculateAllPrices( // Changed from OrderUtils.calculateAllPrices
                        row,
                        unitPriceInput?.value || 0,
                        parseInt(discountTypeSelect?.value) || 1,
                        discountAmountInput?.value || 0,
                        parseInt(quantityInput?.value) || 1,
                        input,
                        { productId },
                        context
                    );
                    if (typeof calculateTotals === 'function') {
                        calculateTotals(id);
                    } else {
                        console.warn(`Calculate totals function not found for context: ${context}`);
                    }
                } else {
                    console.warn('Product select not found in row:', row);
                }
            },
            onDropdownOpen() {
                console.log('Dropdown opened, value:', this.getValue(), 'display:', this.getOption(this.getValue())?.typeName);
            },
            onOptionAdd(value, data) {
                console.log('Option added:', value, data);
            }
        });

        if (!tomSelect.getValue()) {
            tomSelect.refreshOptions();
        }
        console.log('VAT select initialized successfully, tomselect:', tomSelect, 'value:', tomSelect.getValue(), 'display:', tomSelect.getOption(tomSelect.getValue())?.typeName);
        return tomSelect;
    } catch (error) {
        console.error('Failed to initialize VAT select:', error);
        window.c92.showToast('error', 'Hiba az ÁFA típusok betöltése közben: ' + error.message);
        input.dataset.tomSelectInitialized = '';
        return Promise.reject(error);
    }
};


window.c92.initializePartnerTomSelect = async function (partnerSelectElement, contextId, context = 'order') {
    if (!partnerSelectElement || partnerSelectElement.dataset.tomSelectInitialized === 'true') {
        console.log(`Partner TomSelect already initialized for context: ${contextId} (${context})`);
        return partnerSelectElement?.tomselect || null;
    }

    const selectedId = partnerSelectElement.dataset.selectedId || '';
    const selectedText = partnerSelectElement.dataset.selectedText || '';

    // Fetch initial partners
    let initialOptions = [];
    try {
        const url = `/api/Partners?search=&skip=0&take=50`;
        console.log(`Fetching partners from: ${url} for context: ${context}`);
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        };
        const token = localStorage.getItem('token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(url, { headers, credentials: 'include' });
        if (!response.ok) {
            const errorText = await response.text().catch(() => 'Unknown error');
            console.error('Fetch partners failed:', { status: response.status, error: errorText, url });
            initialOptions = [{ id: null, text: 'Nincs elérhető partner' }];
            throw new Error(`Failed to fetch partners: ${response.status} - ${errorText}`);
        }

        const rawData = await response.json();
        console.log('Raw partners response:', rawData);

        initialOptions = rawData.map(item => ({
            id: item.id,
            text: item.text || `Partner ${item.id}`
        })).filter(item => item.text && item.text.trim() !== '');
        console.log('Mapped partners data:', initialOptions);
    } catch (error) {
        console.error('Error fetching initial partners:', error);
        initialOptions = [{ id: null, text: 'Partner API nem elérhető vagy nincs adat' }];
        window.c92.showToast('error', `Hiba a partnerek betöltése közben: ${error.message}`);
    }

    const control = new TomSelect(partnerSelectElement, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        maxOptions: null,
        placeholder: '-- Válasszon partnert --',
        allowEmptyOption: true,
        create: false,
        options: initialOptions,
        load: async function (query, callback) {
            try {
                const searchUrl = `/api/Partners?search=${encodeURIComponent(query)}&skip=0&take=50`;
                console.log(`Searching partners from: ${searchUrl}`);
                const headers = {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                };
                const token = localStorage.getItem('token');
                if (token) {
                    headers['Authorization'] = `Bearer ${token}`;
                }

                const response = await fetch(searchUrl, { headers, credentials: 'include' });
                if (!response.ok) {
                    const errorText = await response.text().catch(() => 'Unknown error');
                    console.error('Fetch partners failed:', { status: response.status, error: errorText, searchUrl });
                    callback([{ id: null, text: 'Nincs találat' }]);
                    return;
                }

                const searchData = await response.json();
                console.log('Search partners response:', searchData);
                const mappedData = searchData.map(item => ({
                    id: item.id,
                    text: item.text || `Partner ${item.id}`
                })).filter(item => item.text && item.text.trim() !== '');
                callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
            } catch (error) {
                console.error('Error searching partners:', error);
                window.c92.showToast('error', `Hiba a partnerek betöltése közben: ${error.message}`);
                callback([{ id: null, text: 'Hiba a betöltés során' }]);
            }
        },
        render: {
            option: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            },
            item: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            }
        },
        onInitialize: function () {
            partnerSelectElement.dataset.tomSelectInitialized = 'true';
            console.log(`Partner TomSelect initialized for context: ${contextId} (${context}), selectedId: ${selectedId}, selectedText: ${selectedText}`);
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, text: selectedText });
                this.setValue(selectedId);
            }
        },
        onChange: function (value) {
            console.log(`Partner selected for context: ${contextId} (${context}), value: ${value}`);
            // Trigger dependent dropdowns (e.g., SiteId, ProductId) to reinitialize
            const siteSelect = document.querySelector(`#site-select_${contextId}`);
            if (siteSelect && typeof window.c92.initializeSiteTomSelect === 'function') {
                window.c92.initializeSiteTomSelect(siteSelect, contextId, context);
            }
            const productSelects = document.querySelectorAll(`#items-tbody_${contextId} .tom-select-product`);
            productSelects.forEach(select => {
                if (select.tomselect) {
                    select.tomselect.destroy();
                    select.dataset.tomSelectInitialized = 'false';
                    window.c92.initializeProductTomSelect(select, contextId, context);
                }
            });
        }
    });

    console.log('Partner TomSelect options after init:', control.options);
    return control;
};

window.c92.initializeSiteTomSelect = async function (siteSelectElement, contextId, context = 'order') {
    if (!siteSelectElement || siteSelectElement.dataset.tomSelectInitialized === 'true') {
        console.log(`Site TomSelect already initialized for context: ${contextId} (${context})`);
        return siteSelectElement?.tomselect || null;
    }

    const selectedId = siteSelectElement.dataset.selectedId || '';
    const selectedText = siteSelectElement.dataset.selectedText || '';
    const partnerSelect = document.querySelector(`#partner-select_${contextId}`);
    const partnerId = partnerSelect?.value ? parseInt(partnerSelect.value) : null;

    // Fetch initial sites if partnerId is available
    let initialOptions = [];
    if (partnerId) {
        try {
            const url = `/api/orders/sites?partnerId=${partnerId}`;
            console.log(`Fetching sites from: ${url} for context: ${context}`);
            const headers = {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            };
            const token = localStorage.getItem('token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            const response = await fetch(url, { headers, credentials: 'include' });
            if (!response.ok) {
                const errorText = await response.text().catch(() => 'Unknown error');
                console.error('Fetch sites failed:', { status: response.status, error: errorText, url });
                initialOptions = [{ id: null, text: response.status === 404 ? 'Telephely API nem elérhető' : 'Nincs elérhető telephely' }];
                throw new Error(`Failed to fetch sites: ${response.status} - ${errorText}`);
            }

            const rawData = await response.json();
            console.log('Raw sites response:', rawData);

            initialOptions = rawData.map(item => ({
                id: item.id,
                text: (item.text && item.text.trim() !== '') ? item.text.trim() : `Site ${item.id}`
            })).filter(item => item.text && item.text.trim() !== '');
            console.log('Mapped sites data:', initialOptions);
        } catch (error) {
            console.error('Error fetching initial sites:', error);
            initialOptions = [{ id: null, text: 'Nincs elérhető telephely vagy endpoint hiba' }];
        }
    } else {
        initialOptions = [{ id: null, text: 'Válasszon partnert előbb' }];
    }

    const control = new TomSelect(siteSelectElement, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        maxOptions: null,
        placeholder: '-- Válasszon telephelyet --',
        allowEmptyOption: true,
        create: false,
        options: initialOptions,
        load: async function (query, callback) {
            try {
                const partnerId = partnerSelect?.value ? parseInt(partnerSelect.value) : null;
                if (!partnerId) {
                    console.log('No partner selected, skipping site fetch');
                    callback([{ id: null, text: 'Válasszon partnert előbb' }]);
                    return;
                }

                const searchUrl = `/api/orders/sites?partnerId=${partnerId}&term=${encodeURIComponent(query)}`;
                console.log(`Searching sites from: ${searchUrl}`);
                const headers = {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                };
                const token = localStorage.getItem('token');
                if (token) {
                    headers['Authorization'] = `Bearer ${token}`;
                }

                const response = await fetch(searchUrl, { headers, credentials: 'include' });
                if (!response.ok) {
                    const errorText = await response.text().catch(() => 'Unknown error');
                    console.error('Fetch sites failed:', { status: response.status, error: errorText, searchUrl });
                    callback([{ id: null, text: response.status === 404 ? 'Telephely API nem elérhető' : 'Nincs találat' }]);
                    return;
                }

                const searchData = await response.json();
                console.log('Search sites response:', searchData);
                const mappedData = searchData.map(item => ({
                    id: item.id,
                    text: (item.text && item.text.trim() !== '') ? item.text.trim() : `Site ${item.id}`
                })).filter(item => item.text && item.text.trim() !== '');
                callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
            } catch (error) {
                console.error('Error searching sites:', error);
                window.c92.showToast('error', `Hiba a telephelyek betöltése közben: ${error.message}`);
                callback([{ id: null, text: 'Hiba a betöltés során' }]);
            }
        },
        render: {
            option: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            },
            item: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            }
        },
        onInitialize: function () {
            siteSelectElement.dataset.tomSelectInitialized = 'true';
            console.log(`Site TomSelect initialized for context: ${contextId} (${context}), selectedId: ${selectedId}, selectedText: ${selectedText}`);
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, text: selectedText });
                this.setValue(selectedId);
            }
        },
        onChange: function (value) {
            console.log(`Site selected for context: ${contextId} (${context}), value: ${value}`);
        }
    });

    console.log('Site TomSelect options after init:', control.options);
    return control;
};

window.c92.initializeCurrencyTomSelect = async function (currencySelectElement, context = 'order') {
    if (!currencySelectElement || currencySelectElement.dataset.tomSelectInitialized === 'true') {
        console.log(`Currency TomSelect already initialized for context: ${context}`);
        return currencySelectElement?.tomselect || null;
    }

    const selectedId = currencySelectElement.dataset.selectedId || '';
    const selectedText = currencySelectElement.dataset.selectedText || '';
    const contextId = currencySelectElement.id.split('_')[1] || 'new'; // Extract contextId (e.g., 'new')

    // Fetch currencies
    let initialOptions = [];
    try {
        const url = `/api/currencies?term=`;
        console.log(`Fetching currencies from: ${url}`);
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        };
        const token = localStorage.getItem('token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(url, { headers, credentials: 'include' });
        if (!response.ok) {
            const errorText = await response.text().catch(() => 'Unknown error');
            console.error('Fetch currencies failed:', { status: response.status, error: errorText, url });
            initialOptions = [{ id: 1, text: 'HUF' }]; // Fallback
            throw new Error(`Failed to fetch currencies: ${response.status} - ${errorText}`);
        }

        const rawData = await response.json();
        console.log('Raw currencies response:', rawData);

        initialOptions = rawData.map(item => ({
            id: item.id,
            text: (item.text && item.text.trim() !== '') ? item.text.trim() : `Currency ${item.id}`
        })).filter(item => item.text && item.text.trim() !== '');
        console.log('Mapped currencies data:', initialOptions);
    } catch (error) {
        console.error('Error fetching currencies:', error);
        initialOptions = [{ id: 1, text: 'HUF' }]; // Fallback
        window.c92.showToast('error', `Hiba a pénznemek betöltése közben: ${error.message}`);
    }

    const control = new TomSelect(currencySelectElement, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        maxOptions: null,
        placeholder: '-- Válasszon pénznemet --',
        allowEmptyOption: false,
        create: false,
        options: initialOptions,
        load: async function (query, callback) {
            try {
                const searchUrl = `/api/currencies?term=${encodeURIComponent(query)}`;
                console.log(`Searching currencies from: ${searchUrl}`);
                const headers = {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                };
                const token = localStorage.getItem('token');
                if (token) {
                    headers['Authorization'] = `Bearer ${token}`;
                }

                const response = await fetch(searchUrl, { headers, credentials: 'include' });
                if (!response.ok) {
                    const errorText = await response.text().catch(() => 'Unknown error');
                    console.error('Fetch currencies failed:', { status: response.status, error: errorText, searchUrl });
                    callback([{ id: null, text: 'Nincs találat' }]);
                    return;
                }

                const searchData = await response.json();
                console.log('Search currencies response:', searchData);
                const mappedData = searchData.map(item => ({
                    id: item.id,
                    text: (item.text && item.text.trim() !== '') ? item.text.trim() : `Currency ${item.id}`
                })).filter(item => item.text && item.text.trim() !== '');
                callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
            } catch (error) {
                console.error('Error searching currencies:', error);
                window.c92.showToast('error', `Hiba a pénznemek betöltése közben: ${error.message}`);
                callback([{ id: null, text: 'Hiba a betöltés során' }]);
            }
        },
        render: {
            option: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            },
            item: function (data, escape) {
                return `<div>${escape(data.text)}</div>`;
            }
        },
        onInitialize: function () {
            currencySelectElement.dataset.tomSelectInitialized = 'true';
            console.log(`Currency TomSelect initialized for select:`, currencySelectElement, `context: ${context}`, `options:`, initialOptions, `selectedId: ${selectedId}, selectedText: ${selectedText}`);
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, text: selectedText });
                this.setValue(selectedId);
            } else if (initialOptions.length) {
                this.addOption(initialOptions);
                this.setValue(initialOptions[0].id); // Default to first currency
            }
        },
        onChange: function (value) {
            console.log(`Currency selected for context: ${context}, value: ${value}`);
            const display = this.options[value]?.text || 'undefined';
            console.log(`Post-initialize value: ${value} display: ${display}`);
            window.calculateOrderTotals(contextId); // Use contextId instead of context
        }
    });

    console.log('Currency select initialized successfully, tomselect:', control, 'value:', control.getValue(), 'display:', control.options[control.getValue()]?.text || 'undefined');
    return control;
};

console.log('Defining window.c92.initializeProductTomSelect');

window.c92.initializeProductTomSelect = async function (selectElement, options) {
    if (!selectElement) {
        console.error('Product select element not provided');
        window.c92.showToast('error', 'Product select element missing.');
        return;
    }

    const context = typeof options === 'string' ? 'quote' : 'order';
    const orderOptions = typeof options === 'object' ? options : {};
    const orderId = orderOptions.orderId || 'new';
    const quoteId = context === 'quote' ? options : null;

    try {
        const url = '/api/Product?search=&skip=0&take=50';
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        };
        const token = localStorage.getItem('token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        console.log(`Fetching products from: ${url}`);
        const response = await fetch(url, { headers, credentials: 'include' });
        if (!response.ok) {
            const errorText = await response.text().catch(() => 'Unknown error');
            throw new Error(`HTTP error! Status: ${response.status} - ${errorText}`);
        }
        const products = await response.json();
        const mappedProducts = products.map(product => ({
            value: product.productId,
            text: product.name || product.text || 'Ismeretlen termék',
            listPrice: product.listPrice || (product.productId === '62044' ? 980 : product.productId === '62056' ? 1000 : 0),
            volumePrice: product.volumePrice || 0,
            partnerPrice: product.partnerPrice || null,
            volumePricing: product.volumePricing || {}
        })).filter(product => product.text && product.text.trim() !== '');

        console.log('Mapped products:', mappedProducts);

        const tomSelectOptions = {
            options: mappedProducts,
            valueField: 'value',
            labelField: 'text',
            searchField: ['text'],
            maxOptions: 50,
            placeholder: context === 'order' ? '-- Válasszon terméket --' : 'Select a product',
            onInitialize: function () {
                selectElement.dataset.tomSelectInitialized = 'true';
                console.log(`Product TomSelect initialized for ${context}, orderId: ${orderId}`);
                if (selectElement.dataset.selectedId && selectElement.dataset.selectedText) {
                    this.addOption({
                        value: selectElement.dataset.selectedId,
                        text: selectElement.dataset.selectedText,
                        listPrice: selectElement.dataset.selectedId === '62044' ? 980 : selectElement.dataset.selectedId === '62056' ? 1000 : 0
                    });
                    this.setValue(selectElement.dataset.selectedId);
                    console.log('Pre-selected product:', selectElement.dataset.selectedId);
                }
            },
            onChange: async function (value) {
                console.log(`Product selected for ${context}, productId: ${value}, in row:`, selectElement.closest('tr')?.dataset.itemId);
                if (context === 'order') {
                    const row = selectElement.closest('tr.order-item-row');
                    if (!row) {
                        console.error('Row not found for product select:', value);
                        window.c92.showToast('error', 'Sor nem található a termékhez.');
                        return;
                    }
                    const products = Object.values(this.options).map(opt => ({
                        productId: opt.value,
                        listPrice: opt.listPrice || (opt.value === '62044' ? 980 : opt.value === '62056' ? 1000 : 0),
                        volumePrice: opt.volumePrice || 0,
                        partnerPrice: opt.partnerPrice || null,
                        volumePricing: opt.volumePricing || {},
                        text: opt.text
                    }));
                    console.log('Products passed to updatePriceFields:', products);
                    if (typeof window.updatePriceFields !== 'function') {
                        console.error('window.updatePriceFields is not defined. Check orders.js load order.');
                        window.c92.showToast('error', 'Árfrissítési funkció nem található.');
                        return;
                    }
                    try {
                        await window.updatePriceFields(selectElement, value, products);
                        if (typeof window.calculateOrderTotals !== 'function') {
                            console.error('window.calculateOrderTotals is not defined. Check orders.js load order.');
                            window.c92.showToast('error', 'Összesítő funkció nem található.');
                            return;
                        }
                        window.calculateOrderTotals(orderId);
                        console.log('Price fields and order totals updated successfully for product:', value);
                    } catch (error) {
                        console.error('Error in updatePriceFields for product:', value, error);
                        window.c92.showToast('error', `Hiba az árak frissítése közben: ${error.message}`);
                    }
                } else {
                    if (typeof window.calculateQuoteTotals !== 'function') {
                        console.error('window.calculateQuoteTotals is not defined.');
                        window.c92.showToast('error', 'Árajánlat összesítő funkció nem található.');
                        return;
                    }
                    window.calculateQuoteTotals(quoteId);
                }
            }
        };

        const tomSelect = new TomSelect(selectElement, tomSelectOptions);
        selectElement.tomselect = tomSelect;

        console.log(`Product TomSelect initialized for ${context}, orderId: ${orderId}, value:`, tomSelect.getValue());
        return tomSelect;
    } catch (err) {
        console.error('Failed to initialize product select:', err);
        window.c92.showToast('error', `Hiba a termékek betöltése közben: ${err.message}`);
        throw err;
    }
};


console.log('Defining window.c92.initializeQuoteTomSelect');

window.c92.initializeQuoteTomSelect = async function (select, contextId, context = 'order') {
    console.log('TomSelect Quote Init for context:', context, 'id:', contextId, 
                'selectedId:', select.dataset.selectedId, 'selectedText:', select.dataset.selectedText);

    if (!select || select.dataset.tomSelectInitialized === 'true') {
        console.log(`Quote TomSelect already initialized for context: ${contextId} (${context})`);
        return select?.tomselect || null;
    }

    const partnerSelect = document.querySelector(`#partner-select_${contextId}`);
    const partnerId = partnerSelect?.value ? parseInt(partnerSelect.value) : null;
    const selectedId = select.dataset.selectedId || '';
    const selectedText = select.dataset.selectedText || '';

    // Fetch quotes from /api/Partners
    let initialOptions = [];
    try {
        const url = partnerId 
            ? `/api/Partners?search=&partnerId=${partnerId}&skip=0&take=50`
            : `/api/Partners?search=&skip=0&take=50`;
        console.log(`Fetching quotes from: ${url} for context: ${context}`);
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        };
        const token = localStorage.getItem('token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(url, { headers, credentials: 'include' });
        if (!response.ok) {
            const errorText = await response.text().catch(() => 'Unknown error');
            console.error('Fetch quotes failed:', { status: response.status, error: errorText, url });
            initialOptions = [{ id: null, text: 'Nincs elérhető árajánlat' }];
            throw new Error(`Failed to fetch quotes: ${response.status} - ${errorText}`);
        }

        const rawData = await response.json();
        console.log('Raw partners response:', rawData);

        // Extract quotes from partner data
        initialOptions = partnerId 
            ? rawData.find(p => p.id === partnerId)?.quotes || []
            : rawData.flatMap(p => p.quotes || []);
        initialOptions = initialOptions.map(item => ({
            id: item.id,
            text: item.text || `Quote ${item.id}`
        })).filter(item => item.text && item.text.trim() !== '');
        console.log('Mapped quotes data:', initialOptions);
    } catch (error) {
        console.error('Error fetching initial quotes:', error);
        initialOptions = [{ id: null, text: 'Árajánlat API nem elérhető vagy nincs adat' }];
        window.c92.showToast('error', `Hiba az árajánlatok betöltése közben: ${error.message}`);
    }

    const tomSelect = new TomSelect(select, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        maxItems: 1,
        placeholder: '-- Válasszon árajánlatot --',
        allowEmptyOption: true,
        create: false,
        options: initialOptions,
        load: async function (query, callback) {
            try {
                const safeQuery = query.trim() || '';
                const searchUrl = partnerId 
                    ? `/api/Partners?search=${encodeURIComponent(safeQuery)}&partnerId=${partnerId}&skip=0&take=50`
                    : `/api/Partners?search=${encodeURIComponent(safeQuery)}&skip=0&take=50`;
                console.log('Quote Search Query:', searchUrl);
                const response = await fetch(searchUrl, {
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value,
                        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                    },
                    credentials: 'include'
                });
                if (!response.ok) {
                    const errorText = await response.text().catch(() => 'Unknown error');
                    throw new Error(`Failed to search quotes: ${response.status} - ${errorText}`);
                }
                const data = await response.json();
                console.log('Search partners response:', data);
                const mappedData = (partnerId 
                    ? data.find(p => p.id === partnerId)?.quotes || []
                    : data.flatMap(p => p.quotes || [])).map(item => ({
                        id: item.id,
                        text: item.text || `Quote ${item.id}`
                    })).filter(item => item.text && item.text.trim() !== '');
                callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
            } catch (error) {
                console.error('Error fetching quotes:', error);
                window.c92.showToast('error', `Hiba az árajánlatok keresése közben: ${error.message}`);
                callback([{ id: null, text: 'Hiba a betöltés során' }]);
            }
        },
        render: {
            option: (data, escape) => `<div>${escape(data.text)}</div>`,
            item: (data, escape) => `<div>${escape(data.text)}</div>`
        },
        onInitialize: function () {
            select.dataset.tomSelectInitialized = 'true';
            console.log('Quote TomSelect initialized for context:', contextId);
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, text: selectedText });
                this.setValue(selectedId);
                console.log('Initialized quote with pre-selected:', selectedId, selectedText);
            }
        },
        onChange: function (value) {
            console.log(`Quote selected for context: ${contextId} (${context}), value: ${value}`);
        }
    });

    console.log('Quote TomSelect options after init:', tomSelect.options);
    return tomSelect;
};

async function fetchWithRetry(url, options, retries = 3, delay = 1000) {
    for (let i = 0; i < retries; i++) {
        try {
            const response = await fetch(url, options);
            if (response.ok) return response;
            const errorText = await response.text().catch(() => 'Unknown error');
            console.error(`Fetch attempt ${i + 1} failed: ${response.status} - ${errorText}`);
            if (i < retries - 1) await new Promise(resolve => setTimeout(resolve, delay));
        } catch (error) {
            console.error(`Fetch attempt ${i + 1} error:`, error);
            if (i < retries - 1) await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
    throw new Error(`Failed to fetch after ${retries} attempts`);
}

console.log('Defining window.c92.initializePartnerTomSelect');

window.c92.initializePartnerTomSelect = async function (select, contextId, context = 'order', preselectedId = null) {
    console.log('TomSelect Partner Init for context:', context, 'id:', contextId);

    if (!select || select.dataset.tomSelectInitialized === 'true') {
        console.log(`Partner TomSelect already initialized for context: ${contextId} (${context})`);
        return select?.tomselect || null;
    }

    let initialOptions = [];
    try {
        const url = `/api/partners/select?search=`;
        console.log(`Fetching partners from: ${url}`);
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        };
        const token = localStorage.getItem('token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetchWithRetry(url, { headers, credentials: 'include' });
        const rawData = await response.json();
        console.log('Raw partners response:', rawData);

        initialOptions = rawData.map(item => ({
            id: item.id,
            text: item.text
        })).filter(item => item.text && item.text.trim() !== '');
        console.log('Mapped partners data:', initialOptions);
    } catch (error) {
        console.error('Error fetching initial partners:', error);
        initialOptions = [{ id: null, text: 'Nincs elérhető partner' }];
        window.c92.showToast('error', `Hiba a partnerek betöltése közben: ${error.message}`);
    }

    const tomSelect = new TomSelect(select, {
        valueField: 'id',
        labelField: 'text',
        searchField: ['text'],
        maxOptions: null,
        placeholder: '-- Válasszon partnert --',
        allowEmptyOption: false,
        create: false,
        options: initialOptions,
        load: async function (query, callback) {
            try {
                const searchUrl = `/api/partners/select?search=${encodeURIComponent(query)}`;
                console.log('Partner Search Query:', searchUrl);
                const response = await fetchWithRetry(searchUrl, {
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value,
                        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                    },
                    credentials: 'include'
                });
                const data = await response.json();
                console.log('Search partners response:', data);
                const mappedData = data.map(item => ({
                    id: item.id,
                    text: item.text
                })).filter(item => item.text && item.text.trim() !== '');
                callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
            } catch (error) {
                console.error('Error fetching partners:', error);
                window.c92.showToast('error', `Hiba a partnerek keresése közben: ${error.message}`);
                callback([{ id: null, text: 'Hiba a betöltés során' }]);
            }
        },
        render: {
            option: (data, escape) => `<div>${escape(data.text)}</div>`,
            item: (data, escape) => `<div>${escape(data.text)}</div>`
        },
        onInitialize: function () {
            select.dataset.tomSelectInitialized = 'true';
            console.log('Partner TomSelect initialized for context:', contextId);

            // ✅ Preselect partner if provided
            if (preselectedId) {
                this.setValue(preselectedId, true);
                console.log(`Preselected partner set: ${preselectedId}`);
            }
        },
        onChange: function (value) {
            console.log(`Partner selected for context: ${contextId} (${context}), value: ${value}`);
        }
    });

    console.log('Partner TomSelect options after init:', tomSelect.options);
    return tomSelect;
};
