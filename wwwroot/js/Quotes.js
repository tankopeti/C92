window.c92 = window.c92 || {};
console.log('quotes.js loaded, window.c92 initialized:', window.c92);

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
                    'Accept': 'application/json',
                    'Authorization': 'Bearer ' + getAuthToken()
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
                    callback();
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
            } else {
                console.log(`No initial partner selected for quoteId ${quoteId}`);
            }
            select.dataset.tomSelectInitialized = 'true';
        },
        onChange: function (value) {
            const modal = select.closest('.modal');
            modal.dataset.partnerId = value || '';
            console.log(`Partner selected: ${value}, modal.dataset.partnerId: ${modal.dataset.partnerId}`);
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
                    'Authorization': 'Bearer ' + getAuthToken()
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
    return debouncedUpdateQuoteTotals(form, quoteId);
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

document.addEventListener('DOMContentLoaded', function () {
    // Placeholder for currentUsername (replace with actual user data)
    const currentUsername = '@username';
    // Ensure window.c92 namespace
    window.c92 = window.c92 || {};

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
                fetch(`/api/products?search=${encodeURIComponent(query)}`)
                    .then(response => {
                        if (!response.ok) throw new Error(`Failed to fetch products: ${response.status}`);
                        return response.json();
                    })
                    .then(data => callback(data))
                    .catch(error => {
                        console.error('Error fetching products:', error);
                        window.c92.showToast('error', 'Hiba a termékek lekérése közben: ' + error.message);
                        callback();
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
                    this.addOption({ id: selectedId, name: selectedText });
                    this.addItem(selectedId);
                }
                select.dataset.tomSelectInitialized = 'true';
                console.log(`Product select initialized, tomselect:`, this, 'value:', this.getValue());
            },
            onChange: function (value) {
                if (value) {
                    fetch(`/api/products/${value}`)
                        .then(response => {
                            if (!response.ok) throw new Error(`Failed to fetch product ${value}: ${response.status}`);
                            return response.json();
                        })
                        .then(product => {
                            const row = this.input.closest('tr');
                            const listPriceInput = row.querySelector('.item-list-price');
                            listPriceInput.value = product.listPrice ? product.listPrice.toFixed(2) : '0.00';
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
    window.c92.initializeVatTomSelect = function (select, quoteId) {
        const tomSelect = new TomSelect(select, {
            valueField: 'id',
            labelField: 'rate',
            searchField: ['rate'],
            placeholder: 'Válasszon ÁFA kulcsot...',
            maxItems: 1,
            load: function (query, callback) {
                fetch(`/api/vatRates?search=${encodeURIComponent(query)}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Authorization': 'Bearer ' + getAuthToken()
                    }
                })
                    .then(response => {
                        if (!response.ok) throw new Error(`Failed to fetch VAT rates: ${response.status}`);
                        return response.json();
                    })
                    .then(data => callback(data))
                    .catch(error => {
                        console.error(`Error fetching VAT rates for quoteId ${quoteId}:`, error);
                        window.c92.showToast('error', 'Hiba az ÁFA kulcsok lekérése közben: ' + error.message);
                        callback();
                    });
            },
            render: {
                option: function (item, escape) {
                    return `<div>${escape(item.rate)}%</div>`;
                },
                item: function (item, escape) {
                    return `<div>${escape(item.rate)}%</div>`;
                }
            },
            onInitialize: function () {
                const selectedId = select.dataset.selectedId;
                const selectedText = select.dataset.selectedText;
                if (selectedId && selectedText) {
                    this.addOption({ id: selectedId, rate: selectedText });
                    this.addItem(selectedId);
                }
                select.dataset.tomSelectInitialized = 'true';
                console.log(`VAT select initialized for quoteId ${quoteId}, value: ${this.getValue()}`);
            }
        });
        return tomSelect;
    };

    // Calculate prices for a row
    async function updateRowCalculations(row, quoteId) {
        const itemId = row.dataset.itemId;
        const quantity = parseFloat(row.querySelector('.item-quantity').value) || 1;
        const listPrice = parseFloat(row.querySelector('.item-list-price').value) || 0;
        const discountTypeId = parseInt(row.querySelector('.discount-type-id').value) || 1;
        const discountAmountInput = row.querySelector('.discount-value');
        let discountAmount = parseFloat(discountAmountInput.value) || 0;
        const vatSelect = row.querySelector('.tom-select-vat');
        const vatRate = vatSelect.tomselect?.options[vatSelect.tomselect.getValue()]?.rate || parseFloat(vatSelect.dataset.selectedRate) || 0;
        const productId = row.querySelector('.tom-select-product')?.tomselect?.getValue();
        const netDiscountedPriceSpan = row.querySelector('.item-net-discounted-price');
        const netTotalSpan = row.querySelector('.item-list-price-total');
        const totalSpan = row.querySelector('.item-gross-price');
        if (!netDiscountedPriceSpan || !netTotalSpan || !totalSpan) {
            console.error('Missing price fields in row:', row);
            window.c92.showToast('error', 'Hiányzó ár mezők a sorban.');
            return;
        }
        let netPrice = listPrice;
        let partnerPrice = null;
        const validDiscountTypeIds = [1, 2, 3, 4, 5, 6];
        if (!validDiscountTypeIds.includes(discountTypeId)) {
            console.warn(`Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, NoDiscount (1) használata`);
            window.c92.showToast('error', `Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, nincs kedvezmény alkalmazva`);
            row.querySelector('.discount-type-id').value = '1';
            discountAmount = 0;
            discountAmountInput.value = '';
        }
        if (discountTypeId === 3) { // PartnerPrice
            try {
                const partnerId = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`)?.dataset.partnerId || '';
                if (!productId || !partnerId) {
                    console.warn(`Missing productId or partnerId for PartnerPrice, item ${itemId}`);
                    window.c92.showToast('warning', `Hiányzó termék vagy partner azonosító a partner ár kiszámításához, tétel ${itemId}`);
                    row.querySelector('.discount-type-id').value = '1';
                    discountAmount = 0;
                    discountAmountInput.value = '';
                } else {
                    const response = await fetch(`/api/product/partner-price?partnerId=${partnerId}&productId=${productId}`);
                    if (!response.ok) {
                        console.warn(`Failed to fetch partner price for product ${productId}, partner ${partnerId}: ${response.status}`);
                        window.c92.showToast('warning', `Nem sikerült lekérni a partner árat a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                        row.querySelector('.discount-type-id').value = '1';
                        discountAmount = 0;
                        discountAmountInput.value = '';
                    } else {
                        const productData = await response.json();
                        partnerPrice = productData?.partnerPrice ? parseFloat(productData.partnerPrice) : null;
                        if (!partnerPrice || partnerPrice === 0) {
                            console.warn(`No valid partner price found for product ${productId}, partner ${partnerId}, using base price`);
                            window.c92.showToast('warning', `Nincs érvényes partner ár a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                            row.querySelector('.discount-type-id').value = '1';
                            discountAmount = 0;
                            discountAmountInput.value = '';
                        } else {
                            netPrice = partnerPrice;
                            discountAmount = listPrice - partnerPrice;
                            if (discountAmount < 0) {
                                console.warn(`Negative discount amount for PartnerPrice: ${discountAmount}, item ${itemId}`);
                                window.c92.showToast('warning', `Negatív kedvezmény összeg a partner árnál, tétel ${itemId}`);
                                discountAmount = 0;
                            }
                            discountAmountInput.value = discountAmount.toFixed(2);
                        }
                    }
                }
            } catch (error) {
                console.error(`Error fetching partner price for item ${itemId}:`, error);
                window.c92.showToast('error', `Hiba a partner ár lekérése közben a tételhez ${itemId}: ${error.message}`);
                row.querySelector('.discount-type-id').value = '1';
                discountAmount = 0;
                discountAmountInput.value = '';
            }
        } else if (discountTypeId === 1 || discountTypeId === 2) { // NoDiscount or ListPrice
            discountAmount = 0;
            discountAmountInput.value = '';
            netPrice = listPrice;
        } else if (discountTypeId === 5) { // CustomDiscountPercentage
            if (discountAmount < 0 || discountAmount > 100) {
                console.warn(`Érvénytelen kedvezmény százalék: ${discountAmount} a tételhez ${itemId}, 0 használata`);
                window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie a tételhez ${itemId}`);
                discountAmount = 0;
                discountAmountInput.value = '';
            }
            netPrice = listPrice * (1 - discountAmount / 100);
        } else if (discountTypeId === 6) { // CustomDiscountAmount
            if (discountAmount < 0 || discountAmount >= listPrice) {
                console.warn(`Érvénytelen kedvezmény összeg: ${discountAmount} a tételhez ${itemId}, 0 használata`);
                window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív vagy nagyobb/egyenlő az listaárral a tételhez ${itemId}`);
                discountAmount = 0;
                discountAmountInput.value = '';
            }
            netPrice = listPrice - discountAmount;
        } else if (discountTypeId === 4 && productId) { // VolumeDiscount
            try {
                const response = await fetch(`/api/product/pricing/${productId}`);
                if (!response.ok) throw new Error(`Fetch failed: ${response.status}`);
                const data = await response.json();
                let volumePrice = NaN;
                const quantityInt = parseInt(quantity);
                const parse = val => val !== null && val !== undefined ? parseFloat(val) : NaN;
                if (data.volume3 && quantityInt >= data.volume3 && !isNaN(parse(data.volume3Price))) {
                    volumePrice = parse(data.volume3Price);
                } else if (data.volume2 && quantityInt >= data.volume2 && !isNaN(parse(data.volume2Price))) {
                    volumePrice = parse(data.volume2Price);
                } else if (data.volume1 && quantityInt >= data.volume1 && !isNaN(parse(data.volume1Price))) {
                    volumePrice = parse(data.volume1Price);
                } else if (!isNaN(parse(data.salesPrice))) {
                    volumePrice = parse(data.salesPrice);
                }
                if (!isNaN(volumePrice)) {
                    netPrice = volumePrice;
                    discountAmount = listPrice - volumePrice;
                    if (discountAmount < 0 || isNaN(discountAmount)) discountAmount = 0;
                    discountAmountInput.value = discountAmount.toFixed(2);
                    row.dataset.volumeThreshold = data.volume3 || data.volume2 || data.volume1 || '';
                    row.dataset.volumePrice = volumePrice.toFixed(2);
                    console.log(`✅ Volume pricing applied: quantity=${quantityInt}, unit=${volumePrice}`);
                } else {
                    throw new Error(`No usable volume price for product ${productId}`);
                }
            } catch (error) {
                console.warn(`⚠️ Volume pricing error for item ${itemId}:`, error.message);
                window.c92.showToast('error', `Volume árhiba (${productId}): ${error.message}`);
                netPrice = listPrice;
                discountAmount = 0;
                discountAmountInput.value = '';
                row.dataset.volumeThreshold = '';
                row.dataset.volumePrice = '';
            }
        }
        if (netPrice < 0) {
            console.warn(`Negatív nettó ár: ${netPrice} a tételhez ${itemId}, 0-ra állítva`);
            window.c92.showToast('warning', `Negatív nettó ár a tételhez ${itemId}, 0-ra állítva`);
            netPrice = 0;
        }
        const grossPrice = netPrice * (1 + vatRate / 100);
        const netTotalPrice = netPrice * quantity;
        const totalGrossPrice = grossPrice * quantity;
        netDiscountedPriceSpan.textContent = netPrice.toFixed(2);
        netTotalSpan.textContent = netTotalPrice.toFixed(2);
        totalSpan.textContent = totalGrossPrice.toFixed(2);
        row.dataset.discountTypeId = discountTypeId.toString();
        row.dataset.discountAmount = discountTypeId === 1 || discountTypeId === 2 ? '' : discountAmount.toString();
        row.dataset.partnerPrice = discountTypeId === 3 ? partnerPrice?.toString() || '' : '';
        console.log(`updateRowCalculations for item ${itemId}: listPrice=${listPrice.toFixed(2)}, discountTypeId=${discountTypeId}, discountAmount=${discountAmount}, partnerPrice=${partnerPrice || 'N/A'}, netPrice=${netPrice.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}, totalGrossPrice=${totalGrossPrice.toFixed(2)}, vatRate=${vatRate}%`);
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
            const quantity = parseFloat(row.querySelector('.item-quantity').value) || 1;
            const netPerUnit = parseFloat(row.querySelector('.item-net-discounted-price').textContent) || 0;
            const grossTotal = parseFloat(row.querySelector('.item-gross-price').textContent) || 0;
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
        const quantityInput = row.querySelector('.item-quantity');
        const discountTypeSelect = row.querySelector('.discount-type-id');
        const discountInput = row.querySelector('.discount-value');
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
    }

    // Initialize description toggle
    function initializeDescriptionToggle(row) {
        const editButton = row.querySelector('.edit-description');
        const itemId = row.dataset.itemId;
        const descriptionRow = row.parentElement.querySelector(`tr.description-row[data-item-id="${itemId}"]`);
        if (!editButton || !descriptionRow) {
            console.error('Edit button or description row not found for itemId:', itemId);
            return;
        }
        editButton.addEventListener('click', () => {
            descriptionRow.style.display = descriptionRow.style.display === 'none' ? '' : 'none';
        });
        const textarea = descriptionRow.querySelector('.item-description-input');
        const charCount = descriptionRow.querySelector('.char-count');
        if (textarea && charCount) {
            textarea.addEventListener('input', () => {
                charCount.textContent = textarea.value.length;
            });
        }
    }

    // Add new item row
    window.c92.addItemRow = async function (quoteId) {
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
        const newItemId = 'new_' + Date.now();
        const itemRow = document.createElement('tr');
        itemRow.className = 'quote-item-row';
        itemRow.dataset.itemId = newItemId;
        itemRow.dataset.discountTypeId = '1';
        itemRow.dataset.discountAmount = '0';
        itemRow.innerHTML = `
            <td><select name="quoteItems[${newItemId}].ProductId" class="form-select tom-select-product" autocomplete="off" required><option value="" disabled selected>-- Válasszon terméket --</option></select></td>
            <td><input type="number" name="quoteItems[${newItemId}].Quantity" class="form-control form-control-sm item-quantity" value="1" min="1" step="1" required></td>
            <td><input type="number" name="quoteItems[${newItemId}].ListPrice" class="form-control form-control-sm item-list-price" value="0" min="0" step="0.01" readonly style="background-color: #f8f9fa; cursor: not-allowed;"></td>
            <td>
                <select name="quoteItems[${newItemId}].DiscountTypeId" class="form-select form-select-sm discount-type-id" data-discount-name-prefix="quoteItems[${newItemId}]">
                    <option value="1" selected>Nincs Kedvezmény</option>
                    <option value="2">Listaár</option>
                    <option value="3">Ügyfélár</option>
                    <option value="4">Mennyiségi kedvezmény</option>
                    <option value="5">Egyedi kedvezmény %</option>
                    <option value="6">Egyedi kedvezmény Összeg</option>
                </select>
            </td>
            <td><input type="number" name="quoteItems[${newItemId}].DiscountAmount" class="form-control form-control-sm discount-value" value="" min="0" step="0.01" readonly></td>
            <td><span class="item-net-discounted-price">0.00</span></td>
            <td><select name="quoteItems[${newItemId}].VatTypeId" class="form-select tom-select-vat" data-selected-id="1" data-selected-text="27%" data-selected-rate="27" autocomplete="off" required><option value="1" selected>27%</option><option value="" disabled>-- Válasszon ÁFA típust --</option></select></td>
            <td><span class="item-list-price-total">0.00</span></td>
            <td><span class="item-gross-price">0.00</span></td>
            <td>
                <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${newItemId}"><i class="bi bi-pencil"></i></button>
                <button type="button" class="btn btn-danger btn-sm remove-item-row" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button>
            </td>
        `;
        const descriptionRow = document.createElement('tr');
        descriptionRow.className = 'description-row';
        descriptionRow.dataset.itemId = newItemId;
        descriptionRow.style.display = 'none';
        descriptionRow.innerHTML = `
            <td colspan="10">
                <div class="mb-2">
                    <label class="form-label">Leírás (max 200 karakter)</label>
                    <textarea name="quoteItems[${newItemId}].ItemDescriptionInput" class="form-control form-control-sm item-description-input" maxlength="200" rows="2"></textarea>
                    <div class="form-text">Karakterek: <span class="char-count">0</span>/200</div>
                </div>
            </td>
        `;
        tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
        tbody.insertBefore(descriptionRow, tbody.querySelector('.quote-total-row'));
        const productSelect = itemRow.querySelector('.tom-select-product');
        const vatSelect = itemRow.querySelector('.tom-select-vat');
        try {
            await window.c92.initializeProductTomSelect(productSelect, quoteId);
            window.c92.initializeVatTomSelect(vatSelect, quoteId);
        } catch (err) {
            console.error('Failed to initialize selects:', err);
            window.c92.showToast('error', 'Hiba a választók inicializálása közben: ' + err.message);
        }
        initializeDescriptionToggle(itemRow);
        bindRowEvents(itemRow, quoteId);
        itemRow.querySelector('.remove-item-row').addEventListener('click', () => {
            itemRow.remove();
            descriptionRow.remove();
            updateQuoteTotals(tbody.closest('form'), quoteId);
        });
        console.log(`Calling addItemRow for quoteId: ${quoteId}`);
        window.c92.calculateQuoteTotals(quoteId);
    };

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
        const discountAmountInput = row.querySelector('.discount-value');
        const listPriceInput = row.querySelector('.item-list-price');
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
        if (discountTypeId === 6) {
            if (discountAmount <= 0) {
                window.c92.showToast('error', `A kedvezmény összegnek nagyobbnak kell lennie 0-nál (tétel: ${itemId})`);
                return false;
            }
            if (discountAmount >= listPrice) {
                window.c92.showToast('error', `A kedvezmény összeg nem lehet nagyobb vagy egyenlő a listaárral (tétel: ${itemId})`);
                return false;
            }
        }
        return true;
    }

    // Save quote
    async function saveQuote(quoteId) {
        const baseForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
        const itemsForm = document.querySelector(`#quoteItemsForm_${quoteId}`);
        const totals = window.c92.calculateQuoteTotals(quoteId);
        const baseInfoTab = document.querySelector(`#base-info-tab_${quoteId}`);
        if (baseInfoTab) {
            baseInfoTab.click();
        }
        const baseErrors = validateForm(baseForm, quoteId);
        const itemErrors = validateForm(itemsForm, quoteId);
        if (baseErrors.length > 0 || itemErrors.length > 0) {
            const allErrors = [...baseErrors, ...itemErrors];
            window.c92.showToast('error', 'Kérjük, töltse ki az összes kötelező mezőt megfelelően:\n' + allErrors.join('\n'));
            return;
        }
        const baseData = new FormData(baseForm);
        const itemsData = new FormData(itemsForm);
        const currencyId = baseData.get('CurrencyId');
        if (!currencyId || isNaN(parseInt(currencyId))) {
            window.c92.showToast('error', 'Kérjük, válasszon pénznemet.');
            const currencySelect = baseForm.querySelector('[name="CurrencyId"]');
            if (currencySelect && currencySelect.tomselect) {
                currencySelect.tomselect.open();
                currencySelect.focus();
            }
            return;
        }
        const partnerId = baseData.get('PartnerId');
        if (!partnerId || isNaN(parseInt(partnerId))) {
            window.c92.showToast('error', 'Kérjük, válasszon partnert.');
            const partnerSelect = baseForm.querySelector('[name="PartnerId"]');
            if (partnerSelect && partnerSelect.tomselect) {
                partnerSelect.tomselect.open();
                partnerSelect.focus();
            }
            return;
        }
        const subject = baseData.get('subject');
        if (!subject || subject.trim() === '') {
            window.c92.showToast('error', 'Kérjük, adja meg az árajánlat tárgyát.');
            return;
        }
        const status = baseData.get('status');
        if (!status || status.trim() === '') {
            window.c92.showToast('error', 'Kérjük, válasszon státuszt.');
            return;
        }
        const quoteNumber = quoteId === 'new' ? `QUOTE-${Date.now()}` : baseForm.querySelector('.form-control-plaintext')?.textContent || `QUOTE-${quoteId}`;
        const statusMapping = {
            'Folyamatban': 'InProgress',
            'Felfüggesztve': 'Draft',
            'Jóváhagyásra_vár': 'PendingApproval',
            'Jóváhagyva': 'Approved',
            'Kiküldve': 'Sent',
            'Elfogadva': 'Accepted',
            'Megrendelve': 'Ordered',
            'Teljesítve': 'Fulfilled',
            'Lezárva': 'Closed',
            'InProgress': 'InProgress',
            'Accepted': 'Accepted',
            'Rejected': 'Rejected',
            'Draft': 'Draft'
        };
        const validUsername = currentUsername && !currentUsername.includes('using System.Security.Claims') ? currentUsername : 'System';
        const quoteDto = {
            QuoteNumber: quoteNumber,
            PartnerId: parseInt(partnerId),
            CurrencyId: parseInt(currencyId),
            QuoteDate: baseData.get('quoteDate') || null,
            Status: statusMapping[baseData.get('status')] || 'Draft',
            TotalAmount: totals.totalNet,
            SalesPerson: baseData.get('salesPerson') || null,
            ValidityDate: baseData.get('validityDate') || null,
            Subject: subject,
            Description: baseData.get('description') || null,
            DetailedDescription: baseData.get('detailedDescription') || null,
            DiscountPercentage: parseFloat(baseData.get('TotalDiscount')) || null,
            DiscountAmount: null,
            TotalItemDiscounts: totals.totalItemDiscounts,
            CompanyName: null,
            CreatedBy: validUsername,
            CreatedDate: new Date().toISOString(),
            ModifiedBy: validUsername,
            ModifiedDate: new Date().toISOString(),
            ReferenceNumber: null,
            Items: []
        };
        const validVatTypeIds = [1, 2, 3];
        const quoteItems = [];
        const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
        for (const row of rows) {
            if (!validateRow(row)) {
                return;
            }
            const itemId = row.dataset.itemId;
            const productId = itemsData.get(`quoteItems[${itemId}].ProductId`);
            if (!productId) {
                console.warn(`Skipping item ${itemId}: No productId`);
                window.c92.showToast('warning', `Tétel ${itemId} kihagyva: nincs termék azonosító.`);
                continue;
            }
            let vatTypeId = parseInt(itemsData.get(`quoteItems[${itemId}].VatTypeId`));
            if (!vatTypeId || isNaN(vatTypeId) || !validVatTypeIds.includes(vatTypeId)) {
                console.warn(`Invalid vatTypeId for item ${itemId}: ${vatTypeId}, using default: 2`);
                window.c92.showToast('warning', `Érvénytelen ÁFA típus tételnél ${itemId}, alapértelmezett 0% használata.`);
                vatTypeId = 2;
            }
            const quantity = parseFloat(itemsData.get(`quoteItems[${itemId}].Quantity`)) || 1;
            const listPrice = parseFloat(itemsData.get(`quoteItems[${itemId}].ListPrice`)) || 0;
            if (quantity <= 0) {
                window.c92.showToast('error', 'A mennyiségnek pozitívnak kell lennie.');
                return;
            }
            if (listPrice < 0) {
                window.c92.showToast('error', 'Az egységár nem lehet negatív.');
                return;
            }
            const discountTypeId = parseInt(row.dataset.discountTypeId) || 1;
            const netPrice = parseFloat(row.querySelector('.item-net-discounted-price').textContent) || 0;
            const totalPrice = parseFloat(row.querySelector('.item-gross-price').textContent) || 0;
            let discountAmount = null;
            if (discountTypeId !== 1 && discountTypeId !== 2) {
                discountAmount = parseFloat(row.dataset.discountAmount) || 0;
            }
            const item = {
                QuoteId: parseInt(quoteId) || 0,
                QuoteItemId: itemId.startsWith('new_') ? 0 : parseInt(itemId),
                ProductId: parseInt(productId),
                VatTypeId: vatTypeId,
                ItemDescription: itemsData.get(`quoteItems[${itemId}].ItemDescriptionInput`) || null,
                Quantity: quantity,
                NetDiscountedPrice: netPrice,
                TotalPrice: totalPrice,
                DiscountTypeId: discountTypeId,
                DiscountAmount: discountAmount,
                PartnerPrice: discountTypeId === 3 ? netPrice : null,
                BasePrice: discountTypeId === 1 || discountTypeId === 2 ? listPrice : null,
                ListPrice: listPrice,
                DiscountPercentage: discountTypeId === 5 ? parseFloat(itemsData.get(`quoteItems[${itemId}].DiscountAmount`)) || 0 : null,
                VolumeThreshold: discountTypeId === 4 ? parseInt(row.dataset.volumeThreshold) || null : null,
                VolumePrice: discountTypeId === 4 ? parseFloat(row.dataset.volumePrice) || null : null
            };
            quoteItems.push({ item, isNew: itemId.startsWith('new_') });
            quoteDto.Items.push(item);
        }
        if (quoteItems.length === 0) {
            window.c92.showToast('error', 'Legalább egy tétel szükséges az árajánlathoz.');
            return;
        }
        console.log('Saving Quote DTO:', JSON.stringify(quoteDto, null, 2));
        if (quoteId === 'new') {
            try {
                const response = await fetch('/api/Quotes', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(quoteDto)
                });
                if (!response.ok) {
                    const text = await response.text();
                    let errorDetails;
                    try {
                        errorDetails = JSON.parse(text);
                        if (errorDetails.error) {
                            throw new Error(`Validation errors: ${errorDetails.error}\n${JSON.stringify(errorDetails.details || {})}`);
                        }
                    } catch {
                        errorDetails = text || 'No details provided';
                    }
                    throw new Error(`Failed to create quote: ${response.status} "${errorDetails}"`);
                }
                const data = await response.json();
                window.c92.showToast('success', 'Árajánlat sikeresen létrehozva!');
                const modal = document.getElementById('newQuoteModal');
                bootstrap.Modal.getInstance(modal)?.hide();
                window.location.reload();
            } catch (error) {
                console.error('Save error for quoteId:', quoteId, error);
                window.c92.showToast('error', 'Hiba történt az árajánlat létrehozása közben: ' + error.message);
            }
        } else {
            const itemPromises = quoteItems.map(async ({ item, isNew }) => {
                const url = isNew ? `/api/Quotes/${quoteId}/Items` : `/api/Quotes/${quoteId}/Items/${item.QuoteItemId}`;
                const method = isNew ? 'POST' : 'PUT';
                const payload = {
                    QuoteId: parseInt(quoteId),
                    QuoteItemId: item.QuoteItemId,
                    ProductId: item.ProductId,
                    VatTypeId: item.VatTypeId,
                    ItemDescription: item.ItemDescription,
                    Quantity: item.Quantity,
                    NetDiscountedPrice: item.NetDiscountedPrice,
                    TotalPrice: item.TotalPrice,
                    DiscountTypeId: item.DiscountTypeId,
                    DiscountAmount: item.DiscountAmount,
                    PartnerPrice: item.PartnerPrice,
                    BasePrice: item.BasePrice,
                    ListPrice: item.ListPrice,
                    DiscountPercentage: item.DiscountPercentage,
                    VolumeThreshold: item.VolumeThreshold,
                    VolumePrice: item.VolumePrice
                };
                console.log(`Sending item ${item.QuoteItemId || 'new'} payload:`, JSON.stringify(payload, null, 2));
                const response = await fetch(url, {
                    method: method,
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(payload)
                });
                if (!response.ok) {
                    const err = await response.json();
                    throw new Error(`Failed to ${isNew ? 'create' : 'update'} item: ${response.status} "${err.error || JSON.stringify(err.details) || 'Request failed'}"`);
                }
                return response.json();
            });
            try {
                const savedItems = await Promise.all(itemPromises);
                const response = await fetch(`/api/Quotes/${quoteId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(quoteDto)
                });
                if (!response.ok) {
                    const err = await response.json();
                    throw new Error(`Failed to update quote: ${response.status} "${err.error || JSON.stringify(err.details) || 'Request failed'}"`);
                }
                window.c92.showToast('success', 'Árajánlat sikeresen mentve!');
                const modal = document.getElementById(`editQuoteModal_${quoteId}`);
                bootstrap.Modal.getInstance(modal).hide();
                window.location.reload();
            } catch (error) {
                console.error('Save error for quoteId:', quoteId, error);
                window.c92.showToast('error', 'Hiba történt az árajánlat mentése közben: ' + error.message);
            }
        }
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

    // Initialize copy quote functionality
    function initializeCopyQuote() {
        document.querySelectorAll('.copy-quote-btn').forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                const quoteId = this.getAttribute('data-quote-id');
                if (!quoteId) {
                    window.c92.showToast('error', 'Érvénytelen árajánlat azonosító.');
                    return;
                }
                fetch(`/api/Quotes/${quoteId}/copy`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    },
                    body: JSON.stringify({})
                })
                    .then(response => {
                        if (!response.ok) {
                            return response.json().then(err => {
                                throw new Error(`HTTP error! Status: ${response.status}, Message: ${err.error || 'Unknown error'}`);
                            });
                        }
                        return response.json();
                    })
                    .then(result => {
                        if (!result.quoteNumber && !result.QuoteNumber) {
                            throw new Error('quoteNumber missing in response');
                        }
                        const quoteNumber = result.quoteNumber || result.QuoteNumber;
                        window.c92.showToast('success', `Árajánlat sikeresen másolva! Új szám: ${quoteNumber}`);
                        setTimeout(() => location.reload(), 3000);
                    })
                    .catch(error => {
                        console.error('Copy quote error:', error);
                        window.c92.showToast('error', 'Hiba történt a másolás során: ' + error.message);
                    });
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
                itemRow.dataset.discountTypeId = item.DiscountTypeId || '1';
                itemRow.dataset.discountAmount = item.DiscountAmount || '0';
                itemRow.innerHTML = `
                    <td><select name="quoteItems[${newItemId}].ProductId" class="form-select tom-select-product" data-selected-id="${item.ProductId || ''}" data-selected-text="${item.ProductName || ''}" autocomplete="off" required></select></td>
                    <td><input type="number" name="quoteItems[${newItemId}].Quantity" class="form-control form-control-sm item-quantity" value="${item.Quantity || 1}" min="1" step="1" required></td>
                    <td><input type="number" name="quoteItems[${newItemId}].ListPrice" class="form-control form-control-sm item-list-price" value="${item.ListPrice || '0.00'}" min="0" step="0.01" readonly style="background-color: #f8f9fa; cursor: not-allowed;"></td>
                    <td>
                        <select name="quoteItems[${newItemId}].DiscountTypeId" class="form-select form-select-sm discount-type-id" data-discount-name-prefix="quoteItems[${newItemId}]">
                            <option value="1" ${item.DiscountTypeId === 1 ? 'selected' : ''}>Nincs Kedvezmény</option>
                            <option value="2" ${item.DiscountTypeId === 2 ? 'selected' : ''}>Listaár</option>
                            <option value="3" ${item.DiscountTypeId === 3 ? 'selected' : ''}>Ügyfélár</option>
                            <option value="4" ${item.DiscountTypeId === 4 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                            <option value="5" ${item.DiscountTypeId === 5 ? 'selected' : ''}>Egyedi kedvezmény %</option>
                            <option value="6" ${item.DiscountTypeId === 6 ? 'selected' : ''}>Egyedi kedvezmény Összeg</option>
                        </select>
                    </td>
                    <td><input type="number" name="quoteItems[${newItemId}].DiscountAmount" class="form-control form-control-sm discount-value" value="${item.DiscountAmount || ''}" min="0" step="0.01" ${[5, 6].includes(item.DiscountTypeId) ? '' : 'readonly'}></td>
                    <td><span class="item-net-discounted-price">${item.NetDiscountedPrice || '0.00'}</span></td>
                    <td><select name="quoteItems[${newItemId}].VatTypeId" class="form-select tom-select-vat" data-selected-id="${item.VatTypeId || ''}" data-selected-text="${item.VatTypeName || ''}" data-selected-rate="${item.VatRate || 0}" autocomplete="off" required></select></td>
                    <td><span class="item-list-price-total">${(item.NetDiscountedPrice * item.Quantity) || '0.00'}</span></td>
                    <td><span class="item-gross-price">${item.TotalPrice || '0.00'}</span></td>
                    <td>
                        <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${newItemId}"><i class="bi bi-pencil"></i></button>
                        <button type="button" class="btn btn-danger btn-sm remove-item-row" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button>
                    </td>
                `;
                const descriptionRow = document.createElement('tr');
                descriptionRow.className = 'description-row';
                descriptionRow.dataset.itemId = newItemId;
                descriptionRow.style.display = 'none';
                descriptionRow.innerHTML = `
                    <td colspan="10">
                        <div class="mb-2">
                            <label class="form-label">Leírás (max 200 karakter)</label>
                            <textarea name="quoteItems[${newItemId}].ItemDescriptionInput" class="form-control form-control-sm item-description-input" maxlength="200" rows="2">${item.ItemDescription || ''}</textarea>
                            <div class="form-text">Karakterek: <span class="char-count">${(item.ItemDescription || '').length}</span>/200</div>
                        </div>
                    </td>
                `;
                tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
                tbody.insertBefore(descriptionRow, tbody.querySelector('.quote-total-row'));
                window.c92.initializeProductTomSelect(itemRow.querySelector('.tom-select-product'), quoteId);
                window.c92.initializeVatTomSelect(itemRow.querySelector('.tom-select-vat'), quoteId);
                initializeDescriptionToggle(itemRow);
                bindRowEvents(itemRow, quoteId);
                itemRow.querySelector('.remove-item-row').addEventListener('click', () => {
                    itemRow.remove();
                    descriptionRow.remove();
                    updateQuoteTotals(tbody.closest('form'), quoteId);
                });
            });
            window.c92.calculateQuoteTotals(quoteId);
        } catch (error) {
            console.error('Error fetching quote:', error.message);
            window.c92.showToast('error', 'Failed to load quote data: ' + error.message);
        }
    }

    // Initialize event listeners
    function initializeEventListeners(quoteId) {
        const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
        if (!modal) {
            console.error('Modal not found for quoteId:', quoteId);
            window.c92.showToast('error', 'Modal nem található.');
            return;
        }
        const addButtons = document.querySelectorAll(`.add-item-row[data-quote-id="${quoteId}"]`);
        addButtons.forEach(button => {
            if (button.dataset.listenerAdded !== 'true') {
                button.addEventListener('click', () => {
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
            document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-product`).forEach(productSelect => {
                if (!productSelect.dataset.tomSelectInitialized) {
                    window.c92.initializeProductTomSelect(productSelect, quoteId);
                }
            });
            document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-vat`).forEach(vatSelect => {
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
            document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-product`).forEach(productSelect => {
                if (productSelect.tomselect) {
                    productSelect.tomselect.destroy();
                    productSelect.dataset.tomSelectInitialized = '';
                }
            });
            document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-vat`).forEach(vatSelect => {
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
            initializeDescriptionToggle(row);
        });
        document.querySelectorAll(`#items-tbody_${quoteId} .remove-item-row`).forEach(button => {
            button.addEventListener('click', () => {
                const itemId = button.dataset.itemId;
                const row = button.closest('tr');
                row.remove();
                document.querySelector(`tr.description-row[data-item-id="${itemId}"]`).remove();
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
                        tbody.querySelectorAll('.quote-item-row, .description-row').forEach(row => {
                            const productSelect = row.querySelector('.tom-select-product');
                            const vatSelect = row.querySelector('.tom-select-vat');
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
        initializeCopyQuote();
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
        console.log('Main script loaded');
    });
});