window.c92 = window.c92 || {};
console.log('quotes.js loaded, window.c92 initialized:', window.c92);

window.c92.showToast = function (type, message) {
    const toastContainer = document.createElement('div');
    toastContainer.className = 'position-fixed bottom-0 end-0 p-3';
    toastContainer.style.zIndex = '1050';
    toastContainer.innerHTML = `
        <div class="toast align-items-center text-white bg-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'warning'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    document.body.appendChild(toastContainer);
    const toast = toastContainer.querySelector('.toast');
    const bootstrapToast = new bootstrap.Toast(toast);
    bootstrapToast.show();
    toast.addEventListener('hidden.bs.toast', () => {
        toastContainer.remove();
    });
};

// Initialize TomSelect for Partner select
window.c92.initializePartnerTomSelect = function (select, quoteId) {
    const selectedId = select.dataset.selectedId;
    const selectedText = select.dataset.selectedText;
    console.log(`Initializing partner select for quoteId ${quoteId}, selectedId: ${selectedId}, selectedText: ${selectedText}`);
    const tomSelect = new TomSelect(select, {
        valueField: 'id',
        labelField: 'name',
        searchField: ['name', 'taxId'],
        placeholder: 'Írja be a partner nevét...',
        maxItems: 1,
        load: function (query, callback) {
            fetch(`/api/partners?search=${encodeURIComponent(query)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error(`Failed to fetch partners: ${response.status}`);
                    return response.json();
                })
                .then(data => callback(data))
                .catch(error => {
                    console.error(`Error fetching partners for quoteId ${quoteId}:`, error);
                    window.c92.showToast('error', 'Hiba a partnerek lekérése közben: ' + error.message);
                    callback([]);
                });
        },
        render: {
            option: function (item, escape) {
                return `<div>${escape(item.name)}${item.taxId ? ` (${escape(item.taxId)})` : ''}</div>`;
            },
            item: function (item, escape) {
                return `<div>${escape(item.name)}</div>`;
            }
        },
        onInitialize: function () {
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, name: selectedText, taxId: '' });
                this.addItem(selectedId);
                const modal = select.closest('.modal');
                modal.dataset.partnerId = selectedId;
                console.log(`Partner select initialized for quoteId ${quoteId}, value: ${this.getValue()}, modal.dataset.partnerId: ${modal.dataset.partnerId}`);
                const addButton = modal.querySelector(`.add-item-row[data-quote-id="${quoteId}"]`);
                if (addButton) addButton.disabled = false;
            } else {
                console.log(`No initial partner selected for quoteId ${quoteId}`);
                const modal = select.closest('.modal');
                const addButton = modal.querySelector(`.add-item-row[data-quote-id="${quoteId}"]`);
                if (addButton) addButton.disabled = true;
            }
            select.dataset.tomSelectInitialized = 'true';
        },
        onChange: function (value) {
            const modal = select.closest('.modal');
            modal.dataset.partnerId = value || '';
            console.log(`Partner selected: ${value}, modal.dataset.partnerId: ${modal.dataset.partnerId}`);
            const addButton = modal.querySelector(`.add-item-row[data-quote-id="${quoteId}"]`);
            if (addButton) addButton.disabled = !value;
            if (value) {
                window.c92.refreshAllRows(quoteId);
            }
        }
    });
    return tomSelect;
};


// Initialize TomSelect for Currency select
window.c92.initializeCurrencyTomSelect = function (select, quoteId = null) {
    const selectedId = select.dataset.selectedId;
    const selectedText = select.dataset.selectedText;
    console.log(`Initializing currency select for quoteId ${quoteId}, selectedId: ${selectedId}, selectedText: ${selectedText}`);
    const tomSelect = new TomSelect(select, {
        valueField: 'id',
        labelField: 'currencyName',
        searchField: ['currencyName', 'currencyCode'],
        placeholder: 'Válasszon pénznemet...',
        maxItems: 1,
        load: function (query, callback) {
            fetch(`/api/currencies?search=${encodeURIComponent(query)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Authorization': 'Bearer ' + (localStorage.getItem('token') || '')
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error(`Failed to fetch currencies: ${response.status}`);
                    return response.json();
                })
                .then(data => {
                    if (!data || data.length === 0) {
                        console.warn(`No currencies returned for quoteId ${quoteId}`);
                        window.c92.showToast('warning', 'Nincs elérhető pénznem. Kérjük, ellenőrizze az API-t.');
                        callback([]);
                    } else {
                        callback(data);
                    }
                })
                .catch(error => {
                    console.error(`Error fetching currencies${quoteId ? ` for quoteId ${quoteId}` : ''}:`, error);
                    window.c92.showToast('error', 'Hiba a pénznemek lekérése közben: ' + error.message);
                    callback([]);
                });
        },
        render: {
            option: function (item, escape) {
                return `<div>${escape(item.currencyName)} (${escape(item.currencyCode)})</div>`;
            },
            item: function (item, escape) {
                return `<div>${escape(item.currencyName)}</div>`;
            }
        },
        onInitialize: function () {
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, currencyName: selectedText, currencyCode: '' });
                this.addItem(selectedId);
            }
            select.dataset.tomSelectInitialized = 'true';
            console.log(`Currency select initialized${quoteId ? ` for quoteId ${quoteId}` : ''}, value: ${this.getValue()}`);
        },
        onChange: function (value) {
            console.log(`Currency selected for quoteId ${quoteId}: ${value}`);
        }
    });
    return tomSelect;
};

// Calculate quote totals
window.c92.calculateQuoteTotals = function (quoteId) {
    const form = document.querySelector(`#quoteItemsForm_${quoteId}`);
    if (!form) {
        console.error('Items form not found for quoteId:', quoteId);
        return { totalNet: 0, totalVat: 0, totalGross: 0, totalItemDiscounts: 0 };
    }
    return updateQuoteTotals(form, quoteId); // Call updateQuoteTotals directly
};

// Refresh all rows
window.c92.refreshAllRows = function (quoteId) {
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    if (!tbody) {
        console.error('Tbody not found for quoteId:', quoteId);
        return;
    }
    const rows = tbody.querySelectorAll('tr.quote-item-row');
    rows.forEach(row => {
        updateRowCalculations(row, quoteId);
    });
    window.c92.calculateQuoteTotals(quoteId);
};

// Initialize TomSelect for Product select
window.c92.initializeProductTomSelect = async function (select, quoteId) {
    const selectedId = select.dataset.selectedId;
    const selectedText = select.dataset.selectedText;
    const tomSelect = new TomSelect(select, {
        valueField: 'id',
        labelField: 'name',
        searchField: ['name', 'productCode'],
        placeholder: 'Válasszon terméket...',
        load: function (query, callback) {
            fetch(`/api/product?search=${encodeURIComponent(query)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error(`Failed to fetch products: ${response.status}`);
                    return response.json();
                })
                .then(data => {
                    const mappedData = data.map(item => ({
                        id: item.productId,
                        name: item.name,
                        productCode: item.productCode || `P${item.productId}`,
                        listPrice: item.listPrice,
                        partnerPrice: item.partnerPrice,
                        volumePricing: item.volumePricing
                    }));
                    if (!mappedData || mappedData.length === 0) {
                        console.warn(`No products returned for query: ${query}`);
                        window.c92.showToast('warning', 'Nincs elérhető termék. Kérjük, ellenőrizze az API-t.');
                        callback([]);
                    } else {
                        console.log('Products loaded:', mappedData);
                        callback(mappedData);
                    }
                })
                .catch(error => {
                    console.error('Error fetching products:', error);
                    window.c92.showToast('error', 'Hiba a termékek lekérése közben: ' + error.message);
                    callback([]);
                });
        },
        render: {
            option: function (item, escape) {
                return `<div>${escape(item.name)} (${escape(item.productCode)})</div>`;
            },
            item: function (item, escape) {
                return `<div>${escape(item.name)}</div>`;
            }
        },
        onInitialize: function () {
            if (selectedId && selectedText) {
                this.addOption({ id: selectedId, name: selectedText, productCode: `P${selectedId}` });
                this.addItem(selectedId);
            }
            select.dataset.tomSelectInitialized = 'true';
            console.log(`Product select initialized, tomselect:`, this, 'value:', this.getValue());
            this.load('');
        },
        onChange: function (value) {
            if (value) {
                fetch(`/api/product/${value}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                })
                    .then(response => {
                        if (!response.ok) throw new Error(`Failed to fetch product ${value}: ${response.status}`);
                        return response.json();
                    })
                    .then(product => {
                        const row = this.input.closest('tr');
                        const listPriceInput = row.querySelector('.list-price-input');
                        listPriceInput.value = product.listPrice ? product.listPrice.toFixed(2) : '0.00';
                        row.dataset.productData = JSON.stringify({
                            listPrice: product.listPrice,
                            partnerPrice: product.partnerPrice,
                            volumePricing: product.volumePricing
                        });
                        updateRowCalculations(row, quoteId);
                    })
                    .catch(error => {
                        console.error('Error fetching product:', error);
                        window.c92.showToast('error', 'Hiba a termékadatok lekérése közben: ' + error.message);
                    });
            }
        }
    });
    return tomSelect;
};


// Initialize TomSelect for VAT Type select
window.c92.initializeVatTomSelect = async function (select, quoteId) {
    const selectedId = select.dataset.selectedId;
    const selectedText = select.dataset.selectedText;
    let vatTypes = [];

    try {
        const response = await fetch('/api/vat/types', {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });
        if (!response.ok) {
            throw new Error(`Failed to fetch VAT types: ${response.status}`);
        }
        vatTypes = await response.json();
        console.log(`Fetched VAT types for quoteId ${quoteId}:`, vatTypes);
    } catch (error) {
        console.error(`Error fetching VAT types for quoteId ${quoteId}:`, error);
        window.c92.showToast('error', 'Hiba az ÁFA típusok lekérése közben: ' + error.message);
        // Fallback options (adjust IDs to match your database)
        vatTypes = [
            { vatTypeId: 3, formattedRate: '0%' },
            { vatTypeId: 2, formattedRate: '5%' },
            { vatTypeId: 1, formattedRate: '27%' }
        ];
    }

    const tomSelect = new TomSelect(select, {
        valueField: 'vatTypeId',
        labelField: 'formattedRate',
        searchField: ['formattedRate'],
        placeholder: 'Válasszon ÁFA kulcsot...',
        maxItems: 1,
        options: vatTypes,
        onInitialize: function () {
            if (selectedId && selectedText) {
                this.addOption({ vatTypeId: selectedId, formattedRate: selectedText });
                this.addItem(selectedId);
            }
            select.dataset.tomSelectInitialized = 'true';
            console.log(`VAT select initialized for quoteId ${quoteId}, value: ${this.getValue()}`);
        },
        onChange: function (value) {
            console.log(`VAT changed for quoteId ${quoteId}: ${value}`);
            const row = select.closest('tr');
            if (row) updateRowCalculations(row, quoteId);
        }
    });
    return tomSelect;
};


// Calculate prices for a row
async function updateRowCalculations(row, quoteId) {
    const itemId = row.dataset.itemId;
    const quantity = parseFloat(row.querySelector('.quantity-input').value) || 1;
    const productSelect = row.querySelector('.product-select');
    const selectedOption = productSelect.tomselect?.options[productSelect.tomselect.getValue()];
    const productData = row.dataset.productData ? JSON.parse(row.dataset.productData) : {};
    const listPrice = selectedOption ? parseFloat(selectedOption.listPrice) || 0 : parseFloat(row.querySelector('.list-price-input').value) || 0;
    const discountTypeId = parseInt(row.querySelector('.discount-type-select').value) || 6;
    const discountAmountInput = row.querySelector('.discount-amount-input');
    let discountAmount = parseFloat(discountAmountInput.value) || 0;
    const vatSelect = row.querySelector('.vat-rate-select');
    const vatRate = parseFloat(vatSelect.selectedOptions[0]?.dataset.rate) || 0;
    const productId = productSelect?.tomselect?.getValue() || productSelect.value;
    const partnerId = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`)?.dataset.partnerId || '';
    const netUnitPriceSpan = row.querySelector('.net-unit-price-input');
    const netTotalSpan = row.querySelector('.net-total');
    const grossTotalSpan = row.querySelector('.gross-total');
    const listPriceInput = row.querySelector('.list-price-input');

    if (!netUnitPriceSpan || !netTotalSpan || !grossTotalSpan || !listPriceInput) {
        console.error('Missing price fields in row:', row);
        window.c92.showToast('error', 'Hiányzó ár mezők a sorban.');
        return;
    }

    let netPrice = listPrice;
    let partnerPrice = null;
    let volumePrice = null;

    const validDiscountTypeIds = [1, 2, 3, 4, 5, 6];
    if (!validDiscountTypeIds.includes(discountTypeId)) {
        console.warn(`Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, CustomDiscountAmount (6) használata`);
        window.c92.showToast('error', `Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}`);
        row.querySelector('.discount-type-select').value = '6';
        discountAmount = 0;
        discountAmountInput.value = '';
        discountAmountInput.readOnly = true;
        netPrice = listPrice;
    } else if (discountTypeId === 1 || discountTypeId === 2) { // NoDiscount or ListPrice
        discountAmount = 0;
        discountAmountInput.value = '';
        discountAmountInput.readOnly = true;
        netPrice = listPrice;
    } else if (discountTypeId === 3) { // PartnerPrice
        if (!productId || !partnerId) {
            console.warn(`Missing productId or partnerId for PartnerPrice, item ${itemId}`);
            window.c92.showToast('warning', `Hiányzó termék vagy partner azonosító a partner ár kiszámításához, tétel ${itemId}`);
            row.querySelector('.discount-type-select').value = '6';
            discountAmount = 0;
            discountAmountInput.value = '';
            discountAmountInput.readOnly = true;
            netPrice = listPrice;
        } else {
            try {
                const response = await fetch(`/api/product/partner-price?partnerId=${partnerId}&productId=${productId}`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' }
                });
                if (!response.ok) {
                    console.warn(`Failed to fetch partner price for product ${productId}, partner ${partnerId}: ${response.status}`);
                    window.c92.showToast('warning', `Nem sikerült lekérni a partner árat a termékhez ${productId}, alapár használata`);
                    row.querySelector('.discount-type-select').value = '6';
                    discountAmount = 0;
                    discountAmountInput.value = '';
                    discountAmountInput.readOnly = true;
                    netPrice = listPrice;
                } else {
                    const productData = await response.json();
                    partnerPrice = parseFloat(productData.partnerPrice) || null;
                    if (!partnerPrice || partnerPrice <= 0) {
                        console.warn(`No valid partner price for product ${productId}, partner ${partnerId}, using list price`);
                        window.c92.showToast('warning', `Nincs érvényes partner ár a termékhez ${productId}, alapár használata`);
                        row.querySelector('.discount-type-select').value = '6';
                        discountAmount = 0;
                        discountAmountInput.value = '';
                        discountAmountInput.readOnly = true;
                        netPrice = listPrice;
                    } else {
                        netPrice = partnerPrice;
                        discountAmount = listPrice - partnerPrice;
                        if (discountAmount < 0) {
                            console.warn(`Negative discount amount for PartnerPrice: ${discountAmount}, item ${itemId}`);
                            window.c92.showToast('warning', `Negatív kedvezmény összeg a partner árnál, tétel ${itemId}`);
                            discountAmount = 0;
                        }
                        discountAmountInput.value = discountAmount.toFixed(2);
                        discountAmountInput.readOnly = true;
                    }
                }
            } catch (error) {
                console.error(`Error fetching partner price for item ${itemId}:`, error);
                window.c92.showToast('error', `Hiba a partner ár lekérése közben a tételhez ${itemId}: ${error.message}`);
                row.querySelector('.discount-type-select').value = '6';
                discountAmount = 0;
                discountAmountInput.value = '';
                discountAmountInput.readOnly = true;
                netPrice = listPrice;
            }
        }
    } else if (discountTypeId === 4) { // VolumeDiscount
        const volumePricing = productData.volumePricing || {};
        const quantityInt = parseInt(quantity);
        const parse = val => val !== null && val !== undefined ? parseFloat(val) : NaN;
        volumePrice = NaN;
        if (volumePricing.volume3 && quantityInt >= volumePricing.volume3 && !isNaN(parse(volumePricing.volume3Price))) {
            volumePrice = parse(volumePricing.volume3Price);
        } else if (volumePricing.volume2 && quantityInt >= volumePricing.volume2 && !isNaN(parse(volumePricing.volume2Price))) {
            volumePrice = parse(volumePricing.volume2Price);
        } else if (volumePricing.volume1 && quantityInt >= volumePricing.volume1 && !isNaN(parse(volumePricing.volume1Price))) {
            volumePrice = parse(volumePricing.volume1Price);
        }
        if (!isNaN(volumePrice)) {
            netPrice = volumePrice;
            discountAmount = listPrice - volumePrice;
            if (discountAmount < 0 || isNaN(discountAmount)) discountAmount = 0;
            discountAmountInput.value = discountAmount.toFixed(2);
            discountAmountInput.readOnly = true;
        } else {
            console.warn(`No valid volume price for product ${productId}, item ${itemId}, using list price`);
            window.c92.showToast('warning', `Nincs érvényes mennyiségi ár a termékhez ${productId}, alapár használata`);
            row.querySelector('.discount-type-select').value = '6';
            discountAmount = 0;
            discountAmountInput.value = '';
            discountAmountInput.readOnly = true;
            netPrice = listPrice;
        }
    } else if (discountTypeId === 5) { // CustomDiscountPercentage
        discountAmountInput.readOnly = false;
        if (discountAmount < 0 || discountAmount > 100) {
            console.warn(`Érvénytelen kedvezmény százalék: ${discountAmount} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie a tételhez ${itemId}`);
            discountAmount = 0;
            discountAmountInput.value = '';
        }
        netPrice = listPrice * (1 - discountAmount / 100);
        discountAmount = listPrice - netPrice;
    } else if (discountTypeId === 6) { // CustomDiscountAmount
        discountAmountInput.readOnly = false;
        if (discountAmount < 0 || discountAmount >= listPrice) {
            console.warn(`Érvénytelen kedvezmény összeg: ${discountAmount} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív vagy nagyobb/egyenlő az listaárral a tételhez ${itemId}`);
            discountAmount = 0;
            discountAmountInput.value = '';
        }
        netPrice = listPrice - discountAmount;
    }

    if (netPrice < 0) {
        console.warn(`Negatív nettó ár: ${netPrice} a tételhez ${itemId}, 0-ra állítva`);
        window.c92.showToast('warning', `Negatív nettó ár a tételhez ${itemId}, 0-ra állítva`);
        netPrice = 0;
        discountAmount = 0;
    }

    const grossPrice = netPrice * (1 + vatRate / 100);
    const netTotalPrice = netPrice * quantity;
    const totalGrossPrice = grossPrice * quantity;
    netUnitPriceSpan.textContent = netPrice.toFixed(2);
    netTotalSpan.textContent = netTotalPrice.toFixed(2);
    grossTotalSpan.textContent = totalGrossPrice.toFixed(2);
    listPriceInput.value = listPrice.toFixed(2);
    row.dataset.discountTypeId = discountTypeId.toString();
    row.dataset.discountAmount = discountAmount.toString();
    console.log(`updateRowCalculations for item ${itemId}: listPrice=${listPrice.toFixed(2)}, discountTypeId=${discountTypeId}, discountAmount=${discountAmount}, netPrice=${netPrice.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}, totalGrossPrice=${totalGrossPrice.toFixed(2)}, vatRate=${vatRate}%`);
    updateQuoteTotals(row.closest('form'), quoteId);
}


// Update quote totals
function updateQuoteTotals(form, quoteId) {
    let totalNet = 0;
    let totalVat = 0;
    let totalGross = 0;
    let totalItemDiscounts = 0;
    const totalDiscountInput = form.querySelector('.total-discount-input');
    const totalDiscount = totalDiscountInput ? parseFloat(totalDiscountInput.value) || 0 : 0;
    form.querySelectorAll('.quote-item-row').forEach(row => {
        const quantity = parseFloat(row.querySelector('.quantity-input').value) || 1;
        const netPerUnit = parseFloat(row.querySelector('.net-unit-price-input').textContent) || 0;
        const grossTotal = parseFloat(row.querySelector('.gross-total').textContent) || 0;
        const discountAmount = parseFloat(row.dataset.discountAmount) || 0;
        const netTotal = netPerUnit * quantity;
        totalNet += netTotal;
        totalVat += grossTotal - netTotal;
        totalGross += grossTotal;
        totalItemDiscounts += discountAmount * quantity;
    });
    const discountedNet = totalNet * (1 - totalDiscount / 100);
    const discountedGross = totalNet > 0 ? discountedNet * (1 + totalVat / totalNet) : 0;
    const totalNetElement = form.querySelector('.quote-total-net');
    const totalVatElement = form.querySelector('.quote-vat-amount');
    const totalGrossElement = form.querySelector('.quote-gross-amount');
    const totalNetInput = form.querySelector('.quote-total-net-input');
    const totalVatInput = form.querySelector('.quote-vat-amount-input');
    const totalGrossInput = form.querySelector('.quote-gross-amount-input');
    if (totalNetElement) totalNetElement.textContent = discountedNet.toFixed(2);
    if (totalVatElement) totalVatElement.textContent = (discountedGross - discountedNet).toFixed(2);
    if (totalGrossElement) totalGrossElement.textContent = discountedGross.toFixed(2);
    if (totalNetInput) totalNetInput.value = discountedNet.toFixed(2);
    if (totalVatInput) totalVatInput.value = (discountedGross - discountedNet).toFixed(2);
    if (totalGrossInput) totalGrossInput.value = discountedGross.toFixed(2);
    const baseInfoForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
    if (baseInfoForm) {
        const totalItemDiscountsInput = baseInfoForm.querySelector('[name="TotalItemDiscounts"]');
        if (totalItemDiscountsInput) {
            totalItemDiscountsInput.value = totalItemDiscounts.toFixed(2);
        }
    }
    return { totalNet: discountedNet, totalVat: discountedGross - discountedNet, totalGross: discountedGross, totalItemDiscounts };
}

// Debounce utility
function debounce(fn, delay = 200) {
    let timeout;
    return (...args) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => fn(...args), delay);
    };
}
const debouncedUpdateQuoteTotals = debounce(updateQuoteTotals);

// Bind row events
function bindRowEvents(row, quoteId) {
    const quantityInput = row.querySelector('.quantity-input');
    const discountTypeSelect = row.querySelector('.discount-type-select');
    const discountInput = row.querySelector('.discount-amount-input');
    const vatSelect = row.querySelector('.vat-rate-select');
    const productSelect = row.querySelector('.product-select');
    if (quantityInput) {
        quantityInput.addEventListener('input', () => {
            if (parseInt(quantityInput.value) < 1) {
                window.c92.showToast('error', 'A mennyiségnek nagyobbnak kell lennie, mint 0.');
                quantityInput.value = '1';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (discountTypeSelect) {
        discountTypeSelect.addEventListener('change', () => {
            const newDiscountTypeId = parseInt(discountTypeSelect.value) || 1;
            discountInput.readOnly = ![5, 6].includes(newDiscountTypeId);
            if (newDiscountTypeId === 1 || newDiscountTypeId === 2) {
                discountInput.value = '';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (discountInput) {
        discountInput.addEventListener('input', () => {
            if (parseFloat(discountInput.value) < 0) {
                window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
                discountInput.value = '';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (vatSelect) {
        vatSelect.addEventListener('change', () => {
            updateRowCalculations(row, quoteId);
        });
    }
    if (productSelect) {
        productSelect.addEventListener('change', () => {
            updateRowCalculations(row, quoteId);
        });
    }
}

// Add new item row
window.c92.addItemRow = async function (quoteId) {
    console.log(`Starting addItemRow for quoteId: ${quoteId}`);
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    if (!tbody) {
        console.error('Tbody not found for quoteId:', quoteId);
        window.c92.showToast('error', 'Táblázat nem található.');
        return;
    }
    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error('Modal not found for quoteId:', quoteId);
        window.c92.showToast('error', 'Modal nem található.');
        return;
    }
    const partnerId = modal.dataset.partnerId;
    if (!partnerId) {
        console.error('No partnerId found for quoteId:', quoteId);
        window.c92.showToast('warning', 'Kérjük, válasszon partnert.');
        const partnerSelect = modal.querySelector('[name="PartnerId"]');
        if (partnerSelect && partnerSelect.tomselect) {
            partnerSelect.tomselect.open();
            partnerSelect.focus();
        }
        return;
    }

    // Fetch VAT options
    let vatOptionsHtml = '';
    try {
        const response = await fetch('/api/vat/types');
        if (response.ok) {
            const vatTypes = await response.json();
            vatOptionsHtml = vatTypes.map(vt => `<option value="${vt.vatTypeId}" data-rate="${vt.rate}">${vt.formattedRate}</option>`).join('');
        } else {
            vatOptionsHtml = `
                <option value="3">0%</option>
                <option value="2">5%</option>
                <option value="1" selected>27%</option>
            `;
        }
    } catch (error) {
        console.error('Error fetching VAT options:', error);
        window.c92.showToast('warning', 'Hiba az ÁFA típusok lekérése közben, alapértelmezett opciók használata.');
        vatOptionsHtml = `
            <option value="3">0%</option>
            <option value="2">5%</option>
            <option value="1" selected>27%</option>
        `;
    }

    console.log(`Creating new item row for quoteId: ${quoteId}`);
    const existingRows = tbody.querySelectorAll('.quote-item-row').length;
    const newItemId = `new_${Date.now()}_${existingRows}`;
    const itemRow = document.createElement('tr');
    itemRow.className = 'quote-item-row';
    itemRow.dataset.itemId = newItemId;
    itemRow.dataset.discountTypeId = '6';
    itemRow.dataset.discountAmount = '0';
    itemRow.innerHTML = `
        <td><select name="items[${existingRows}].ProductId" class="form-select product-select" required><option value="" disabled selected>-- Válasszon terméket --</option></select></td>
        <td><input type="number" name="items[${existingRows}].Quantity" class="form-control form-control-sm quantity-input" value="1" min="1" step="1" required></td>
        <td><input type="number" name="items[${existingRows}].ListPrice" class="form-control form-control-sm list-price-input" value="0.00" min="0" step="0.01" readonly style="background-color: #f8f9fa; cursor: not-allowed;"></td>
        <td>
            <select name="items[${existingRows}].DiscountType" class="form-select form-select-sm discount-type-select">
                <option value="1">Nincs kedvezmény</option>
                <option value="2">Listaár</option>
                <option value="3">Partner ár</option>
                <option value="4">Mennyiségi kedvezmény</option>
                <option value="5">Egyedi kedvezmény %</option>
                <option value="6" selected>Egyedi kedvezmény összeg</option>
            </select>
        </td>
        <td><input type="number" name="items[${existingRows}].DiscountAmount" class="form-control form-control-sm discount-amount-input" value="0" min="0" step="0.01"></td>
        <td><span class="net-unit-price-input">0.00</span></td>
        <td><select name="items[${existingRows}].VatTypeId" class="form-select vat-rate-select" required>${vatOptionsHtml}</select></td>
        <td><span class="net-total">0.00</span></td>
        <td><span class="gross-total">0.00</span></td>
        <td><button type="button" class="btn btn-danger btn-sm delete-item-row" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button></td>
    `;
    tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
    try {
        await window.c92.initializeProductTomSelect(itemRow.querySelector('.product-select'), quoteId);
        await window.c92.initializeVatTomSelect(itemRow.querySelector('.vat-rate-select'), quoteId);
    } catch (err) {
        console.error('Failed to initialize selects for quoteId:', quoteId, err);
        window.c92.showToast('warning', 'Hiba a választók inicializálása közben, alapértelmezett sor hozzáadva.');
    }
    bindRowEvents(itemRow, quoteId);
    itemRow.querySelector('.delete-item-row').addEventListener('click', () => {
        itemRow.remove();
        window.c92.calculateQuoteTotals(quoteId);
    });
    window.c92.calculateQuoteTotals(quoteId);
};

// Bind row events
function bindRowEvents(row, quoteId) {
    const quantityInput = row.querySelector('.quantity-input');
    const discountTypeSelect = row.querySelector('.discount-type-select');
    const discountInput = row.querySelector('.discount-amount-input');
    const vatSelect = row.querySelector('.vat-rate-select');
    const productSelect = row.querySelector('.product-select');
    if (quantityInput) {
        quantityInput.addEventListener('input', () => {
            if (parseInt(quantityInput.value) < 1) {
                window.c92.showToast('error', 'A mennyiségnek nagyobbnak kell lennie, mint 0.');
                quantityInput.value = '1';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (discountTypeSelect) {
        discountTypeSelect.addEventListener('change', () => {
            const newDiscountTypeId = parseInt(discountTypeSelect.value) || 6;
            discountInput.readOnly = ![5, 6].includes(newDiscountTypeId);
            if (![5, 6].includes(newDiscountTypeId)) {
                discountInput.value = '';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (discountInput) {
        discountInput.addEventListener('input', () => {
            if (parseFloat(discountInput.value) < 0) {
                window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
                discountInput.value = '';
            }
            updateRowCalculations(row, quoteId);
        });
    }
    if (vatSelect) {
        vatSelect.addEventListener('change', () => {
            updateRowCalculations(row, quoteId);
        });
    }
    if (productSelect) {
        productSelect.addEventListener('change', () => {
            updateRowCalculations(row, quoteId);
        });
    }
}

// Validate form
function validateForm(form, quoteId) {
    const errors = [];
    const requiredFields = form.querySelectorAll('[required]');
    requiredFields.forEach(field => {
        const value = field.value.trim();
        if (!value) {
            const label = field.closest('tr')?.querySelector('th')?.textContent || field.name;
            errors.push(`${label} mező kitöltése kötelező`);
        }
        if (['subject'].includes(field.name)) {
            if (!value || value === '1' || value === 'x') {
                const label = field.closest('tr')?.querySelector('th')?.textContent || field.name;
                errors.push(`${label} mező nem lehet üres, "1" vagy "x"`);
            }
        }
        if (field.name.includes('.Quantity') || field.name.includes('.ListPrice')) {
            const numValue = parseFloat(value);
            if (isNaN(numValue) || numValue <= 0) {
                const label = field.closest('tr')?.querySelector('th')?.textContent || field.name;
                errors.push(`${label} mező pozitív szám kell legyen`);
            }
        }
    });
    return errors;
}

// Validate row
function validateRow(row) {
    const itemId = row.dataset.itemId;
    const discountTypeId = parseInt(row.dataset.discountTypeId);
    const discountAmountInput = row.querySelector('.discount-amount-input');
    const listPriceInput = row.querySelector('.list-price-input');
    const discountAmount = parseFloat(discountAmountInput?.value) || 0;
    const listPrice = parseFloat(listPriceInput?.value) || 0;
    if (!listPriceInput || !discountAmountInput || isNaN(discountTypeId)) {
        console.warn(`❌ Missing required fields or invalid discount type in row ${itemId}`);
        window.c92.showToast('error', `Hiányzó vagy hibás mezők (${itemId})`);
        return false;
    }
    if (discountTypeId === 5 && (discountAmount < 0 || discountAmount > 100)) {
        window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie (tétel: ${itemId})`);
        return false;
    }
    if (discountTypeId === 6 && (discountAmount < 0 || discountAmount >= listPrice)) {
        window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív vagy nagyobb/egyenlő a listaárral (tétel: ${itemId})`);
        return false;
    }
    return true;
}

// Initialize filter dropdown
function initializeFilterDropdown() {
    const filterItems = document.querySelectorAll('.dropdown-menu [data-filter]');
    filterItems.forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            const filter = this.getAttribute('data-filter');
            const sort = this.getAttribute('data-sort');
            const form = document.querySelector('form[asp-page="./Index"]') ||
                document.querySelector('form[action="/CRM/Quotes"]') ||
                document.querySelector('form[action="/CRM/Quotes/Index"]') ||
                document.querySelector('form[action="./Index"]');
            if (form) {
                form.querySelectorAll('input[name="StatusFilter"], input[name="SortBy"]').forEach(input => input.remove());
                const statusInput = document.createElement('input');
                statusInput.type = 'hidden';
                statusInput.name = 'StatusFilter';
                statusInput.value = filter === 'all' ? '' : filter;
                form.appendChild(statusInput);
                const sortInput = document.createElement('input');
                sortInput.type = 'hidden';
                sortInput.name = 'SortBy';
                sortInput.value = sort;
                form.appendChild(sortInput);
                form.submit();
            } else {
                console.error('Form not found.');
            }
        });
    });
}

// Populate edit quote modal
async function populateEditQuoteModal(quoteId) {
    try {
        const response = await fetch(`/api/Quotes/${quoteId}`, {
            headers: {
                'Authorization': 'Bearer ' + localStorage.getItem('token'),
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });
        if (!response.ok) {
            throw new Error(`Failed to fetch quote: ${response.status}`);
        }
        const quote = await response.json();
        const modal = document.getElementById(`editQuoteModal_${quoteId}`);
        if (!modal) {
            console.error(`Modal #editQuoteModal_${quoteId} not found`);
            window.c92.showToast('error', 'Modal nem található.');
            return;
        }
        modal.dataset.partnerId = quote.PartnerId || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} .form-control-plaintext`).textContent = quote.QuoteNumber || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="quoteDate"]`).value = quote.QuoteDate ? quote.QuoteDate.split('T')[0] : '';
        const partnerSelect = modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="PartnerId"]`);
        const currencySelect = modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="CurrencyId"]`);
        partnerSelect.dataset.selectedId = quote.PartnerId || '';
        partnerSelect.dataset.selectedText = quote.CompanyName || '';
        currencySelect.dataset.selectedId = quote.CurrencyId || '';
        currencySelect.dataset.selectedText = quote.CurrencyName || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="salesPerson"]`).value = quote.SalesPerson || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="validityDate"]`).value = quote.ValidityDate ? quote.ValidityDate.split('T')[0] : '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="subject"]`).value = quote.Subject || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="description"]`).value = quote.Description || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="detailedDescription"]`).value = quote.DetailedDescription || '';
        modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="status"]`).value = quote.Status || 'Draft';
        if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
            window.c92.initializePartnerTomSelect(partnerSelect, quoteId);
        }
        if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
            window.c92.initializeCurrencyTomSelect(currencySelect, quoteId);
        }
        const tbody = modal.querySelector(`#items-tbody_${quoteId}`);
        tbody.querySelectorAll('tr:not(.quote-total-row, .quote-vat-row, .quote-gross-row)').forEach(row => row.remove());
quote.Items.forEach(item => {
    const newItemId = item.QuoteItemId || 'new_' + Date.now();
    const itemRow = document.createElement('tr');
    itemRow.className = 'quote-item-row';
    itemRow.dataset.itemId = newItemId;
    itemRow.dataset.discountTypeId = item.DiscountType || '6';
    itemRow.dataset.discountAmount = item.DiscountAmount || '0';
    itemRow.dataset.productData = JSON.stringify({
        listPrice: item.ListPrice,
        partnerPrice: item.PartnerPrice,
        volumePricing: item.VolumePricing
    });
    itemRow.innerHTML = `
        <td><select name="items[${newItemId}].ProductId" class="form-select product-select" data-selected-id="${item.ProductId || ''}" data-selected-text="${item.ProductName || ''}" autocomplete="off" required></select></td>
        <td><input type="number" name="items[${newItemId}].Quantity" class="form-control form-control-sm quantity-input" value="${item.Quantity || 1}" min="1" step="1" required></td>
        <td><input type="number" name="items[${newItemId}].ListPrice" class="form-control form-control-sm list-price-input" value="${item.ListPrice || '0.00'}" min="0" step="0.01" readonly style="background-color: #f8f9fa; cursor: not-allowed;"></td>
        <td>
            <select name="items[${newItemId}].DiscountType" class="form-select form-select-sm discount-type-select">
                <option value="1" ${item.DiscountType === 1 ? 'selected' : ''}>Nincs kedvezmény</option>
                <option value="2" ${item.DiscountType === 2 ? 'selected' : ''}>Listaár</option>
                <option value="3" ${item.DiscountType === 3 ? 'selected' : ''}>Partner ár</option>
                <option value="4" ${item.DiscountType === 4 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                <option value="5" ${item.DiscountType === 5 ? 'selected' : ''}>Egyedi kedvezmény %</option>
                <option value="6" ${item.DiscountType === 6 ? 'selected' : ''}>Egyedi kedvezmény összeg</option>
            </select>
        </td>
        <td><input type="number" name="items[${newItemId}].DiscountAmount" class="form-control form-control-sm discount-amount-input" value="${item.DiscountAmount || '0'}" min="0" step="0.01" ${[5, 6].includes(item.DiscountType) ? '' : 'readonly'}></td>
        <td><span class="net-unit-price-input">${item.NetDiscountedPrice || '0.00'}</span></td>
        <td><select name="items[${newItemId}].VatRate" class="form-select vat-rate-select" data-selected-id="${item.VatTypeId || ''}" data-selected-text="${item.VatTypeName || ''}" data-selected-rate="${item.VatRate || 0}" required></select></td>
        <td><span class="net-total">${(item.NetDiscountedPrice * item.Quantity) || '0.00'}</span></td>
        <td><span class="gross-total">${item.TotalPrice || '0.00'}</span></td>
        <td>
            <button type="button" class="btn btn-danger btn-sm delete-item-row" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button>
        </td>
    `;
    tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
    window.c92.initializeProductTomSelect(itemRow.querySelector('.product-select'), quoteId);
    window.c92.initializeVatTomSelect(itemRow.querySelector('.vat-rate-select'), quoteId);
    bindRowEvents(itemRow, quoteId);
    itemRow.querySelector('.delete-item-row').addEventListener('click', () => {
        itemRow.remove();
        updateQuoteTotals(tbody.closest('form'), quoteId);
    });
});
        window.c92.calculateQuoteTotals(quoteId);
    } catch (error) {
        console.error('Error fetching quote:', error.message);
        window.c92.showToast('error', 'Failed to load quote data: ' + error.message);
    }
}

async function saveQuote(quoteId) {
    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error('Modal not found for quoteId:', quoteId);
        window.c92.showToast('error', 'Modal nem található.');
        return;
    }

    const baseForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
    const itemsForm = document.querySelector(`#quoteItemsForm_${quoteId}`);
    if (!baseForm || !itemsForm) {
        console.error('Forms not found for quoteId:', quoteId);
        window.c92.showToast('error', 'Űrlapok nem találhatók.');
        return;
    }

    // Validate forms
    const errors = validateForm(baseForm, quoteId);
    itemsForm.querySelectorAll('.quote-item-row').forEach(row => {
        if (!validateRow(row)) {
            errors.push(`Érvénytelen adatok a tételben: ${row.dataset.itemId}`);
        }
    });
    if (errors.length > 0) {
        errors.forEach(error => window.c92.showToast('error', error));
        return;
    }

    // Collect base form data
    const formData = new FormData(baseForm);
    const quoteDto = {
        QuoteNumber: '',
        QuoteDate: formData.get('quoteDate') || new Date().toISOString().split('T')[0],
        PartnerId: parseInt(formData.get('PartnerId')) || 0,
        CurrencyId: parseInt(formData.get('CurrencyId')) || 0,
        SalesPerson: formData.get('salesPerson') || '',
        ValidityDate: formData.get('validityDate') || new Date(new Date().setDate(new Date().getDate() + 30)).toISOString().split('T')[0],
        Subject: formData.get('subject') || '',
        Description: formData.get('description') || '',
        DetailedDescription: formData.get('detailedDescription') || '',
        Status: formData.get('status') || 'Folyamatban',
        DiscountPercentage: parseFloat(itemsForm.querySelector('.total-discount-input')?.value) || 0,
        QuoteDiscountAmount: 0,
        TotalItemDiscounts: parseFloat(formData.get('TotalItemDiscounts')) || 0,
        TotalAmount: parseFloat(itemsForm.querySelector('.quote-gross-amount-input')?.value) || 0,
        ReferenceNumber: formData.get('ReferenceNumber') || '',
        QuoteItems: []
    };

    if (!quoteDto.PartnerId) {
        window.c92.showToast('error', 'Kérjük, válasszon partnert.');
        const partnerSelect = baseForm.querySelector('[name="PartnerId"]');
        if (partnerSelect && partnerSelect.tomselect) {
            partnerSelect.tomselect.open();
            partnerSelect.focus();
        }
        return;
    }

    if (!quoteDto.CurrencyId) {
        window.c92.showToast('error', 'Kérjük, válasszon pénznemet.');
        return;
    }

    if (!quoteDto.Subject) {
        window.c92.showToast('error', 'A tárgy mező kitöltése kötelező.');
        return;
    }

    // Collect quote items
    const rows = itemsForm.querySelectorAll('.quote-item-row');
    for (const row of rows) {
        const itemId = row.dataset.itemId;
        const productSelect = row.querySelector('.product-select');
        const vatSelect = row.querySelector('.vat-rate-select');
        const discountTypeSelect = row.querySelector('.discount-type-select');
        const discountAmountInput = row.querySelector('.discount-amount-input');
        const netUnitPrice = parseFloat(row.querySelector('.net-unit-price-input')?.textContent) || 0;
        const quantity = parseInt(row.querySelector('.quantity-input')?.value) || 1;

        const item = {
            QuoteItemId: 0,
            ProductId: parseInt(productSelect?.value) || 0,
            Quantity: quantity,
            ListPrice: parseFloat(row.querySelector('.list-price-input')?.value) || 0,
            NetDiscountedPrice: netUnitPrice,
            TotalPrice: netUnitPrice * quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100),
            VatTypeId: parseInt(vatSelect?.value) || 0,
            DiscountTypeId: parseInt(discountTypeSelect?.value) || null,
            DiscountAmount: parseFloat(row.dataset.discountAmount) || 0,
            PartnerPrice: null,
            VolumePrice: null
        };

        if (!item.ProductId) {
            window.c92.showToast('error', `Kérjük, válasszon terméket a tételhez: ${itemId}`);
            return;
        }
        if (!item.VatTypeId) {
            window.c92.showToast('error', `Kérjük, válasszon ÁFA kulcsot a tételhez: ${itemId}`);
            return;
        }

        if (item.DiscountTypeId === 3 && quoteDto.PartnerId && item.ProductId) {
            try {
                const response = await fetch(`/api/product/partner-price?partnerId=${quoteDto.PartnerId}&productId=${item.ProductId}`, {
                    headers: { 'Accept': 'application/json' }
                });
                if (response.ok) {
                    const productData = await response.json();
                    item.PartnerPrice = parseFloat(productData.partnerPrice) || null;
                    item.DiscountAmount = item.ListPrice - (item.PartnerPrice || item.ListPrice);
                    item.NetDiscountedPrice = item.PartnerPrice || item.ListPrice;
                    item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
                } else {
                    window.c92.showToast('warning', `Nem sikerült lekérni a partner árat a tételhez ${itemId}`);
                    item.DiscountTypeId = 6;
                    item.DiscountAmount = 0;
                    item.NetDiscountedPrice = item.ListPrice;
                    item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
                }
            } catch (error) {
                console.error(`Error fetching partner price for product ${item.ProductId}:`, error);
                window.c92.showToast('warning', `Nem sikerült lekérni a partner árat a tételhez ${itemId}`);
                item.DiscountTypeId = 6;
                item.DiscountAmount = 0;
                item.NetDiscountedPrice = item.ListPrice;
                item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
            }
        } else if (item.DiscountTypeId === 4 && item.ProductId) {
            try {
                const response = await fetch(`/api/product/${item.ProductId}`, {
                    headers: { 'Accept': 'application/json' }
                });
                if (response.ok) {
                    const productData = await response.json();
                    const volumePricing = productData.volumePricing || {};
                    const quantity = item.Quantity;
                    if (volumePricing.volume3 && quantity >= volumePricing.volume3 && volumePricing.volume3Price) {
                        item.VolumePrice = parseFloat(volumePricing.volume3Price);
                    } else if (volumePricing.volume2 && quantity >= volumePricing.volume2 && volumePricing.volume2Price) {
                        item.VolumePrice = parseFloat(volumePricing.volume2Price);
                    } else if (volumePricing.volume1 && quantity >= volumePricing.volume1 && volumePricing.volume1Price) {
                        item.VolumePrice = parseFloat(volumePricing.volume1Price);
                    }
                    if (item.VolumePrice) {
                        item.DiscountAmount = item.ListPrice - item.VolumePrice;
                        item.NetDiscountedPrice = item.VolumePrice;
                        item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
                    } else {
                        window.c92.showToast('warning', `Nem sikerült lekérni a mennyiségi árat a tételhez ${itemId}`);
                        item.DiscountTypeId = 6;
                        item.DiscountAmount = 0;
                        item.NetDiscountedPrice = item.ListPrice;
                        item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
                    }
                } else {
                    window.c92.showToast('warning', `Nem sikerült lekérni a mennyiségi árat a tételhez ${itemId}`);
                    item.DiscountTypeId = 6;
                    item.DiscountAmount = 0;
                    item.NetDiscountedPrice = item.ListPrice;
                    item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
                }
            } catch (error) {
                console.error(`Error fetching volume pricing for product ${item.ProductId}:`, error);
                window.c92.showToast('warning', `Nem sikerült lekérni a mennyiségi árat a tételhez ${itemId}`);
                item.DiscountTypeId = 6;
                item.DiscountAmount = 0;
                item.NetDiscountedPrice = item.ListPrice;
                item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
            }
        } else if (item.DiscountTypeId === 5) {
            const discountPercentage = parseFloat(discountAmountInput?.value) || 0;
            item.DiscountAmount = item.ListPrice * (discountPercentage / 100);
            item.NetDiscountedPrice = item.ListPrice * (1 - discountPercentage / 100);
            item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
        } else if (item.DiscountTypeId === 6) {
            item.DiscountAmount = parseFloat(discountAmountInput?.value) || 0;
            item.NetDiscountedPrice = item.ListPrice - item.DiscountAmount;
            item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
        } else {
            item.DiscountAmount = 0;
            item.NetDiscountedPrice = item.ListPrice;
            item.TotalPrice = item.NetDiscountedPrice * item.Quantity * (1 + (parseFloat(vatSelect?.selectedOptions[0]?.dataset.rate) || 0) / 100);
        }

        quoteDto.QuoteItems.push(item);
    }

    if (quoteDto.QuoteItems.length === 0) {
        window.c92.showToast('error', 'Legalább egy tétel hozzáadása kötelező.');
        return;
    }

    console.log('Sending quoteDto:', JSON.stringify(quoteDto, null, 2));

    try {
        const response = await fetch('/CRM/Quotes?handler=CreateQuote', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(quoteDto)
        });

        const result = await response.json();
        if (response.ok) {
            window.c92.showToast('success', result.message || 'Árajánlat sikeresen létrehozva.');
            bootstrap.Modal.getInstance(modal)?.hide();
            location.reload();
        } else {
            window.c92.showToast('error', result.message || 'Hiba történt az árajánlat létrehozása közben.');
        }
    } catch (error) {
        console.error('Error saving quote:', error);
        window.c92.showToast('error', 'Hiba történt az árajánlat létrehozása közben: ' + error.message);
    }
}

// Helper for auth token (if needed)
function getAuthToken() {
    // Replace with your token retrieval logic
    return localStorage.getItem('authToken') || '';
}

// Initialize event listeners
function initializeEventListeners(quoteId) {
    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error('Modal not found for quoteId:', quoteId);
        window.c92.showToast('error', 'Modal nem található.');
        return;
    }
    const addButtons = modal.querySelectorAll(`.add-item-row[data-quote-id="${quoteId}"]`);
    console.log(`Found ${addButtons.length} add-item-row buttons for quoteId: ${quoteId}`);
    if (addButtons.length === 0) {
        console.error(`No add-item-row buttons found for quoteId: ${quoteId}. Check DOM for button with class 'add-item-row' and data-quote-id="${quoteId}"`);
    }
    addButtons.forEach(button => {
        if (button.dataset.listenerAdded !== 'true') {
            console.log(`Attaching listener to add-item-row button for quoteId: ${quoteId}`);
            button.addEventListener('click', () => {
                console.log(`Add item row clicked for quoteId: ${quoteId}, partnerId: ${modal.dataset.partnerId}`);
                if (!modal.dataset.partnerId) {
                    window.c92.showToast('warning', 'Kérjük, válasszon partnert.');
                    const partnerSelect = modal.querySelector('[name="PartnerId"]');
                    if (partnerSelect && partnerSelect.tomselect) {
                        partnerSelect.tomselect.open();
                        partnerSelect.focus();
                    }
                    return;
                }
                window.c92.addItemRow(quoteId);
            });
            button.dataset.listenerAdded = 'true';
        } else {
            console.log(`Listener already added to add-item-row button for quoteId: ${quoteId}`);
        }
    });
    const saveButtons = document.querySelectorAll(`.save-quote[data-quote-id="${quoteId}"]`);
    saveButtons.forEach(button => {
        if (button.dataset.listenerAdded !== 'true') {
            button.addEventListener('click', () => {
                saveQuote(quoteId);
            });
            button.dataset.listenerAdded = 'true';
        }
    });
    const partnerSelect = modal.querySelector('select[name="PartnerId"]');
    const currencySelect = modal.querySelector('select[name="CurrencyId"]');
    if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
        window.c92.initializePartnerTomSelect(partnerSelect, quoteId);
    }
    if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
        window.c92.initializeCurrencyTomSelect(currencySelect, quoteId);
    }
    modal.addEventListener('show.bs.modal', () => {
        document.querySelectorAll(`#items-tbody_${quoteId} .product-select`).forEach(productSelect => {
            if (!productSelect.dataset.tomSelectInitialized) {
                window.c92.initializeProductTomSelect(productSelect, quoteId);
            }
        });
        document.querySelectorAll(`#items-tbody_${quoteId} .vat-rate-select`).forEach(vatSelect => {
            if (!vatSelect.dataset.tomSelectInitialized) {
                window.c92.initializeVatTomSelect(vatSelect, quoteId);
            }
        });
        if (partnerSelect && partnerSelect.tomselect) {
            modal.dataset.partnerId = partnerSelect.tomselect.getValue() || partnerSelect.dataset.selectedId || '';
        }
        window.c92.calculateQuoteTotals(quoteId);
    });
    modal.addEventListener('hidden.bs.modal', () => {
        document.querySelectorAll(`#items-tbody_${quoteId} .product-select`).forEach(productSelect => {
            if (productSelect.tomselect) {
                productSelect.tomselect.destroy();
                productSelect.dataset.tomSelectInitialized = '';
            }
        });
        document.querySelectorAll(`#items-tbody_${quoteId} .vat-rate-select`).forEach(vatSelect => {
            if (vatSelect.tomselect) {
                vatSelect.tomselect.destroy();
                vatSelect.dataset.tomSelectInitialized = '';
            }
        });
        if (partnerSelect?.tomselect) {
            partnerSelect.tomselect.destroy();
            partnerSelect.dataset.tomSelectInitialized = '';
        }
        if (currencySelect?.tomselect) {
            currencySelect.tomselect.destroy();
            currencySelect.dataset.tomSelectInitialized = '';
        }
        modal.dataset.partnerId = '';
        const tbody = document.querySelector(`#items-tbody_${quoteId}`);
        if (tbody) {
            tbody.querySelectorAll('tr:not(.quote-total-row, .quote-vat-row, .quote-gross-row)').forEach(row => row.remove());
        }
    });
    document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`).forEach(row => {
        bindRowEvents(row, quoteId);
    });
    document.querySelectorAll(`#items-tbody_${quoteId} .delete-item-row`).forEach(button => {
        button.addEventListener('click', () => {
            const itemId = button.dataset.itemId;
            const row = button.closest('tr');
            row.remove();
            updateQuoteTotals(row.closest('form'), quoteId);
        });
    });
}

// Initialize modals
function initializeModals() {
    const newQuoteModal = document.getElementById('newQuoteModal');
    if (newQuoteModal) {
        newQuoteModal.addEventListener('shown.bs.modal', async function () {
            console.log('newQuoteModal shown, initializing for quoteId: new');
            initializeEventListeners('new');
            window.c92.calculateQuoteTotals('new');
            const baseInfoForm = document.getElementById('quoteBaseInfoForm_new');
            console.log('Base info form found:', !!baseInfoForm);
            if (baseInfoForm) {
                const partnerSelect = baseInfoForm.querySelector('[name="PartnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="CurrencyId"]');
                console.log('Partner select found:', !!partnerSelect, 'Currency select found:', !!currencySelect);
                if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
                    console.log('Initializing partner select for new quote');
                    window.c92.initializePartnerTomSelect(partnerSelect, 'new');
                }
                if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
                    console.log('Initializing currency select for new quote');
                    window.c92.initializeCurrencyTomSelect(currencySelect, 'new');
                }
                console.log('Initial modal.dataset.partnerId:', newQuoteModal.dataset.partnerId);
            } else {
                console.error('Base info form not found for quoteId: new');
                window.c92.showToast('error', 'Alapinformációs űrlap nem található.');
            }
            const tbody = document.getElementById('items-tbody_new');
            if (tbody && !tbody.querySelector('.quote-item-row')) {
                window.c92.addItemRow('new');
            }
        });
        newQuoteModal.addEventListener('hidden.bs.modal', function () {
            const baseInfoForm = document.getElementById('quoteBaseInfoForm_new');
            if (baseInfoForm) {
                baseInfoForm.reset();
                baseInfoForm.querySelector('[name="quoteDate"]').value = new Date().toISOString().split('T')[0];
                baseInfoForm.querySelector('[name="validityDate"]').value = new Date(new Date().setDate(new Date().getDate() + 30)).toISOString().split('T')[0];
                baseInfoForm.querySelector('[name="status"]').value = 'Draft';
                const partnerSelect = baseInfoForm.querySelector('[name="PartnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="CurrencyId"]');
                if (partnerSelect?.tomselect) {
                    partnerSelect.tomselect.clear();
                    partnerSelect.tomselect.destroy();
                    partnerSelect.dataset.tomSelectInitialized = '';
                }
                if (currencySelect?.tomselect) {
                    currencySelect.tomselect.clear();
                    currencySelect.tomselect.destroy();
                    currencySelect.dataset.tomSelectInitialized = '';
                }
            }
            const itemsForm = document.getElementById('quoteItemsForm_new');
            if (itemsForm) {
                const tbody = document.getElementById('items-tbody_new');
                if (tbody) {
                    tbody.querySelectorAll('.quote-item-row').forEach(row => {
                        const productSelect = row.querySelector('.product-select');
                        const vatSelect = row.querySelector('.vat-rate-select');
                        if (productSelect?.tomselect) productSelect.tomselect.destroy();
                        if (vatSelect?.tomselect) vatSelect.tomselect.destroy();
                        row.remove();
                    });
                    const totalNet = tbody.querySelector('.quote-total-net');
                    const totalVat = tbody.querySelector('.quote-vat-amount');
                    const totalGross = tbody.querySelector('.quote-gross-amount');
                    if (totalNet) totalNet.textContent = '0.00';
                    if (totalVat) totalVat.textContent = '0.00';
                    if (totalGross) totalGross.textContent = '0.00';
                    const totalItemDiscounts = baseInfoForm.querySelector('[name="TotalItemDiscounts"]');
                    if (totalItemDiscounts) totalItemDiscounts.value = '0';
                }
            }
        });
    }
    document.querySelectorAll('[id^="editQuoteModal_"]').forEach(modal => {
        modal.addEventListener('shown.bs.modal', function () {
            const quoteId = this.id.split('_')[1];
            populateEditQuoteModal(quoteId);
        });
    });
    document.querySelectorAll('.confirm-delete-quote').forEach(button => {
        button.addEventListener('click', function (event) {
            const quoteId = event.target.dataset.quoteId;
            if (!quoteId) {
                console.error('Missing quoteId on delete button');
                return;
            }
            const confirmed = confirm(`Biztosan törölni szeretné a következő árajánlatot: QUOTE-${quoteId}?`);
            if (!confirmed) {
                return;
            }
            fetch(`/api/quotes/${quoteId}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
                .then(response => {
                    if (response.ok) {
                        window.c92.showToast('success', 'Árajánlat sikeresen törölve.');
                        location.reload();
                    } else if (response.status === 404) {
                        window.c92.showToast('error', 'Az árajánlat nem található.');
                    } else {
                        window.c92.showToast('error', 'Hiba történt az árajánlat törlése során.');
                    }
                })
                .catch(error => {
                    console.error('Error during deletion:', error);
                    window.c92.showToast('error', 'Hálózati hiba történt.');
                });
        });
    });
    document.querySelectorAll('.convert-quote').forEach(button => {
        button.addEventListener('click', async function () {
            const quoteId = this.dataset.quoteId;
            const modal = document.getElementById(`convertQuoteModal_${quoteId}`);
            const form = document.getElementById(`convertQuoteForm_${quoteId}`);
            const convertDto = {
                currencyId: parseInt(form.querySelector('[name="currency"]').value) || null,
                siteId: null,
                paymentTerms: '',
                shippingMethod: '',
                orderType: ''
            };
            try {
                const response = await fetch(`/api/quotes/${quoteId}/convert-to-order`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(convertDto)
                });
                if (response.ok) {
                    const result = await response.json();
                    window.c92.showToast('success', `Rendelés létrehozva: #${result.orderNumber}`);
                    bootstrap.Modal.getInstance(modal)?.hide();
                } else {
                    const error = await response.json();
                    window.c92.showToast('error', error.error || 'A konverzió nem sikerült.');
                }
            } catch (err) {
                console.error('Conversion error:', err);
                window.c92.showToast('error', 'Váratlan hiba történt.');
            }
        });
    });
}

// Initialize DOM
document.addEventListener('DOMContentLoaded', function () {
    initializeModals();
    initializeFilterDropdown();

    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
    console.log('Main script loaded');
});