window.initializePartnerTomSelect = function (select, quoteId) {
    const modal = select.closest('.modal') || document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error(`Modal not found for quoteId: ${quoteId}`);
        window.showToast('error', 'Modal not found.');
        return Promise.reject('Modal not found');
    }
    const selectedId = select.dataset.selectedId || '5004';
    const selectedText = select.dataset.selectedText || 'Default Partner';

    modal.dataset.partnerId = '5004';
    console.log(`Fallback partnerId set for quoteId ${quoteId}:`, modal.dataset.partnerId);

    if (select.tomselect) {
        select.tomselect.destroy();
    }

    select.dataset.tomSelectInitialized = 'true';

    return new Promise((resolve, reject) => {
        fetch('/api/partners')
            .then(response => {
                if (!response.ok) throw new Error(`Failed to fetch partners: ${response.status}`);
                return response.json();
            })
            .then(data => {
                if (!Array.isArray(data) || data.length === 0) {
                    console.warn('No partners returned for quoteId:', quoteId);
                    window.showToast('warning', 'No partners available.');
                    modal.dataset.partnerId = '5004';
                    select.innerHTML = '<option value="5004">Default Partner</option>';
                    resolve();
                    return;
                }
                console.log('Partners loaded:', data);
                select.innerHTML = '<option value="">Válasszon partnert</option>';
                data.forEach(partner => {
                    const option = new Option(partner.text, partner.id);
                    select.appendChild(option);
                });

                const tomSelect = new TomSelect(select, {
                    create: false,
                    searchField: ['text'],
                    maxItems: 1,
                    valueField: 'id',
                    labelField: 'text',
                    onInitialize: function () {
                        if (selectedId && data.find(p => p.id == selectedId)) {
                            this.addOption({ id: selectedId, text: selectedText });
                            this.addItem(selectedId);
                            modal.dataset.partnerId = selectedId;
                        } else {
                            this.addOption({ id: '5004', text: 'Default Partner' });
                            this.addItem('5004');
                            modal.dataset.partnerId = '5004';
                        }
                        console.log('Partner initialized, partnerId:', modal.dataset.partnerId);
                        resolve();
                    },
                    onChange: function (value) {
                        modal.dataset.partnerId = value || '5004';
                        console.log('Partner changed, partnerId:', modal.dataset.partnerId);
                        const itemsTab = document.querySelector(`#items-tab_${quoteId}`);
                        const addItemButton = document.querySelector(`.add-item-row[data-quote-id="${quoteId}"]`);
                        if (itemsTab && addItemButton) {
                            itemsTab.disabled = !value;
                            addItemButton.disabled = !value;
                        }
                        document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-product`).forEach(productSelect => {
                            if (productSelect.tomselect) {
                                productSelect.tomselect.clear();
                                productSelect.tomselect.clearOptions();
                                window.initializeProductTomSelect(productSelect, quoteId);
                            }
                        });
                    }
                });
            })
            .catch(error => {
                console.error(`Error fetching partners for quoteId ${quoteId}:`, error);
                window.showToast('error', 'Failed to load partners: ' + error.message);
                modal.dataset.partnerId = '5004';
                select.innerHTML = '<option value="5004">Default Partner</option>';
                resolve();
            })
            .finally(() => {
                if (quoteId === 'new' && modal.dataset.partnerId && !document.querySelector('#items-tbody_new .quote-item-row')) {
                    console.log('Calling addItemRow after partner initialization for quoteId:', quoteId);
                    window.addItemRow('new');
                }
            });
    });
};


window.initializeProductTomSelect = function (select, quoteId) {
    if (typeof TomSelect === 'undefined') {
        console.error('TomSelect library is not loaded.');
        window.showToast('error', 'Required library missing.');
        return;
    }

    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error(`Modal not found for quoteId: ${quoteId}`);
        window.showToast('error', 'Modal not found.');
        return;
    }

    const row = select.closest('tr.quote-item-row');
    if (!row) {
        console.error(`Row not found for product select in quoteId: ${quoteId}`, select);
        window.showToast('error', 'Product row not found.');
        return;
    }

    const quantityInput = row.querySelector('.item-quantity');
    if (!quantityInput) {
        console.error(`Quantity input not found for quoteId: ${quoteId}`);
        window.showToast('error', 'Quantity input missing.');
        return;
    }

    select.dataset.tomSelectInitialized = 'true';

    function fetchProducts(quantity) {
        const partnerId = modal.dataset.partnerId || '5004';
        const quoteDate = modal.dataset.quoteDate || new Date().toISOString().split('T')[0];
        const parsedQuantity = parseInt(quantity, 10) || 1;
        const apiUrl = `/api/product?partnerId=${encodeURIComponent(partnerId)}&eDate=${encodeURIComponent(quoteDate)}&quantity=${encodeURIComponent(parsedQuantity)}`;

        fetch(apiUrl)
            .then(response => {
                if (!response.ok) throw new Error(`Failed to fetch products: ${response.status}`);
                return response.json();
            })
            .then(data => {
                const products = Array.isArray(data) ? data : [];
                if (products.length === 0) {
                    console.warn('No products returned for partnerId:', partnerId);
                    window.showToast('warning', 'No products available.');
                    return;
                }

                if (select.tomselect) select.tomselect.destroy();

                new TomSelect(select, {
                    create: false,
                    searchField: ['name'],
                    maxItems: 1,
                    valueField: 'productId',
                    labelField: 'name',
                    options: products,
                    render: {
                        option: function (item, escape) {
                            return `<div>${escape(item.name)}</div>`;
                        },
                        item: function (item, escape) {
                            return `<div>${escape(item.name)}</div>`;
                        }
                    },
                    onInitialize: function () {
                        const selectedId = select.dataset.selectedId;
                        if (selectedId && products.find(p => p.productId == selectedId) && row.closest('tbody')) {
                            this.addItem(selectedId);
                            window.updatePriceFields(select, selectedId, products);
                        }
                    },
                    onChange: function (value) {
                        const selectedProduct = products.find(p => p.productId == value);
                        if (selectedProduct && row.closest('tbody')) {
                            select.dataset.selectedId = value;
                            select.dataset.selectedText = selectedProduct.name;
                            window.updatePriceFields(select, value, products);
                            window.calculateQuoteTotals(quoteId);
                        }
                    }
                });
            })
            .catch(error => {
                console.error(`Failed to fetch products for quoteId ${quoteId}:`, error);
                window.showToast('error', 'Failed to load products: ' + error.message);
            });
    }

    quantityInput.addEventListener('input', () => {
        fetchProducts(quantityInput.value);
    });

    const partnerSelect = modal.querySelector('.partner-select');
    if (partnerSelect) {
        partnerSelect.addEventListener('change', () => {
            modal.dataset.partnerId = partnerSelect.value || '5004';
            fetchProducts(quantityInput.value);
        }, { once: true });
    }

    fetchProducts(quantityInput.value);
};

// Placeholder for site dropdown initialization (to be fixed later)
window.initializeSiteTomSelect = function (select, quoteId, partnerData = null) {
    console.log('Site TomSelect placeholder called for quoteId:', quoteId, 'partnerData:', partnerData);
    if (select.tomselect) {
        select.tomselect.destroy();
    }
    select.dataset.tomSelectInitialized = 'true';

    const sites = partnerData?.sites || [];
    console.log('Available sites for partner:', sites.length, JSON.stringify(sites, null, 2));

    select.innerHTML = '<option value="" disabled selected>-- Válasszon telephelyet --</option>';

    const control = new TomSelect(select, {
        dropdownParent: 'body',
        valueField: 'id',
        labelField: 'text',
        searchField: 'text',
        placeholder: '-- Válasszon telephelyet --',
        allowEmptyOption: true,
        maxOptions: 100,
        options: [],
        load: function (query, callback) {
            if (sites.length > 0) {
                const filteredSites = sites.filter(s => s.text.toLowerCase().includes(query.toLowerCase()));
                console.log('Filtered sites for query:', query, filteredSites.length, JSON.stringify(filteredSites, null, 2));
                callback(filteredSites);
                return;
            }
            const url = `/api/sites?search=${encodeURIComponent(query)}`;
            console.log('Site Search Query:', url);
            fetch(url)
                .then(response => {
                    if (!response.ok) throw new Error(`HTTP error: ${response.status}`);
                    return response.json();
                })
                .then(data => {
                    console.log('API /api/sites Response:', JSON.stringify(data, null, 2));
                    callback(Array.isArray(data) ? data : []);
                })
                .catch(error => {
                    console.error('Site Search Error:', error);
                    window.showToast('error', `Failed to load sites: ${error.message}`);
                    callback([]);
                });
        },
        shouldLoad: function (query) {
            return sites.length === 0;
        },
        render: {
            option: function (data, escape) {
                const primaryBadge = data.isPrimary ? '<span class="badge bg-primary ms-2">Elsődleges</span>' : '';
                console.log('Rendering site option:', data.text, 'id:', data.id, 'isPrimary:', data.isPrimary);
                return `<div data-value="${data.id}">${escape(data.text)}${primaryBadge}</div>`;
            },
            item: function (data, escape) {
                console.log('Rendering selected site:', data.text, 'id:', data.id);
                return `<div>${escape(data.text)}</div>`;
            },
            no_results: function (data, escape) {
                return `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`;
            }
        },
        onInitialize: function () {
            console.log('Initializing site TomSelect with sites:', sites.length);
            if (!partnerData || sites.length === 0) {
                console.log('No partner data or sites, initializing empty dropdown');
                return;
            }
            sites.forEach(site => {
                this.addOption(site);
                console.log('Added site option:', site.text, 'id:', site.id, 'isPrimary:', site.isPrimary);
            });
            const primarySite = sites.find(s => s.isPrimary);
            if (primarySite) {
                this.addItem(primarySite.id, true);
                console.log('Set primary site:', primarySite.text, 'id:', primarySite.id, 'for quoteId:', quoteId);
            } else if (sites.length > 0) {
                this.addItem(sites[0].id, true);
                console.log('Set fallback site:', sites[0].text, 'id:', sites[0].id, 'for quoteId:', quoteId);
            }
            this.refreshOptions(false);
            const dropdownContent = select.closest('.ts-wrapper')?.querySelector('.ts-dropdown-content');
            console.log('Site dropdown HTML after initialization:', dropdownContent ? dropdownContent.innerHTML : 'Site dropdown HTML not found');
        }
    });

    console.log('Tom Select Site initialized for quoteId:', quoteId, 'with sites:', sites.length);
    return control;
};