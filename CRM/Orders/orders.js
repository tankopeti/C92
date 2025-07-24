document.addEventListener('DOMContentLoaded', function () {
    // Calculate Row Totals
    function calculateRowTotals(row, id) {
        console.log(`Calculating row totals for orderId: ${id}`);
        const quantityInput = row.querySelector('.quantity');
        const unitPriceInput = row.querySelector('.unit-price');
        const discountValueInput = row.querySelector('.discount-value');
        const discountTypeSelect = row.querySelector('.discount-type');
        const currencySelect = row.querySelector('.tom-select-currency');
        const vatSelect = row.querySelector('.tom-select-vat');
        const netPriceSpan = row.querySelector('.item-net-price');
        const grossPriceSpan = row.querySelector('.item-gross-price');

        if (!quantityInput || !unitPriceInput || !netPriceSpan || !grossPriceSpan) {
            console.error('Missing required inputs for row calculations:', { row, id });
            return;
        }

        const quantity = parseFloat(quantityInput.value) || 1;
        const unitPrice = parseFloat(unitPriceInput.value) || 0;
        const discountValue = parseFloat(discountValueInput.value) || 0;
        const discountType = discountTypeSelect ? discountTypeSelect.value : 'Percentage';
        const currencyId = currencySelect ? currencySelect.value || '1' : '1';
        const exchangeRate = currencyId === '1' ? 1 : 1; // Assume 1 for HUF
        const vatId = vatSelect ? vatSelect.value || '5' : '5';
        const vatRateMap = { '1': 0, '2': 5, '3': 10, '4': 20, '5': 27 };
        const vatRate = vatRateMap[vatId] || 27;

        let netPrice = quantity * unitPrice * exchangeRate;
        if (discountType === 'Percentage') {
            netPrice -= netPrice * (discountValue / 100);
        } else {
            netPrice -= discountValue;
        }
        netPrice = Math.max(0, netPrice);
        netPriceSpan.textContent = netPrice.toFixed(2);

        const vatAmount = netPrice * (vatRate / 100);
        const grossPrice = netPrice + vatAmount;
        grossPriceSpan.textContent = grossPrice.toFixed(2);

        console.log(`Row totals: netPrice=${netPrice.toFixed(2)}, vatAmount=${vatAmount.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}`);
        calculateTotals(id);
    }

    // Calculate Totals
    function calculateTotals(id) {
        console.log(`Calculating totals for orderId: ${id}`);
        const tbody = document.querySelector(`#items-tbody_${id}`);
        if (!tbody) {
            console.error('Items tbody not found for id:', id);
            return;
        }
        let totalNet = 0;
        let totalGross = 0;
        tbody.querySelectorAll('.order-item-row').forEach(row => {
            const netPrice = parseFloat(row.querySelector('.item-net-price').textContent) || 0;
            const grossPrice = parseFloat(row.querySelector('.item-gross-price').textContent) || 0;
            totalNet += netPrice;
            totalGross += grossPrice;
        });
        const totalNetSpan = tbody.querySelector('.order-total-amount');
        const totalGrossSpan = tbody.querySelector('.order-final-total');
        if (totalNetSpan) totalNetSpan.textContent = totalNet.toFixed(2);
        if (totalGrossSpan) totalGrossSpan.textContent = totalGross.toFixed(2);
        console.log(`Order totals: totalNet=${totalNet.toFixed(2)}, totalGross=${totalGross.toFixed(2)}`);
    }

    // Initialize TomSelect for dropdowns
    function initializeTomSelect(select, endpoint, defaultId, defaultText) {
        console.log(`Initializing TomSelect for ${select.name}, endpoint: http://localhost:8080${endpoint}`);
        const selectedId = select.dataset.selectedId || defaultId || '';
        const selectedText = select.dataset.selectedText || defaultText || '';

        fetch(`http://localhost:8080${endpoint}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        })
            .then(response => {
                if (!response.ok) throw new Error(`HTTP error for ${endpoint}: ${response.status}`);
                return response.json();
            })
            .then(data => {
                console.log(`Data received for ${endpoint}:`, data);
                const options = data.map(item => ({
                    id: item.id || item.Id || item.ID,
                    text: item.name || item.Name || item.address || item.Address || item.currencyName || item.CurrencyName || item.quoteNumber || item.QuoteNumber || 'Unknown'
                }));
                if (!options.length) {
                    options.push({ id: '0', text: `No options available for ${select.name}` });
                }

                if (select.tomselect) select.tomselect.destroy();
                new TomSelect(select, {
                    create: false,
                    searchField: ['text'],
                    maxItems: 1,
                    valueField: 'id',
                    labelField: 'text',
                    options: options,
                    render: {
                        option: function (item, escape) {
                            return `<div>${escape(item.text)}</div>`;
                        },
                        item: function (item, escape) {
                            return `<div>${escape(item.text)}</div>`;
                        }
                    },
                    onInitialize: function () {
                        console.log(`TomSelect initialized for ${select.name}, setting value: ${selectedId}, text: ${selectedText}`);
                        if (selectedId && selectedText) {
                            this.addOption({ id: selectedId, text: selectedText });
                            this.setValue(selectedId);
                        }
                    },
                    onChange: function (value) {
                        console.log(`${select.name} changed to:`, value);
                        if (select.name === 'partnerId') {
                            const modal = select.closest('.modal');
                            if (!modal) {
                                console.error('Modal not found for partner select:', select);
                                return;
                            }
                            const orderId = modal.querySelector('form[id^="orderItemsForm_"]')?.dataset.orderId;
                            if (!orderId) {
                                console.error('Order ID not found for partner change:', modal);
                                return;
                            }
                            const itemsTbody = modal.querySelector(`#items-tbody_${orderId}`);
                            if (!itemsTbody) {
                                console.error('Items tbody not found for partner change:', modal);
                                return;
                            }
                            itemsTbody.querySelectorAll('.tom-select-product').forEach(prodSelect => {
                                if (prodSelect.tomselect) prodSelect.tomselect.destroy();
                                initializeProductTomSelect(prodSelect, orderId);
                            });
                        }
                    }
                });
            })
            .catch(error => {
                console.error(`Error fetching ${endpoint}:`, error);
                const options = [{ id: '0', text: `Failed to load options for ${select.name}` }];
                if (select.tomselect) select.tomselect.destroy();
                new TomSelect(select, {
                    create: false,
                    searchField: ['text'],
                    maxItems: 1,
                    valueField: 'id',
                    labelField: 'text',
                    options: options,
                    onInitialize: function () {
                        this.setValue('0');
                    }
                });
            });
    }

    // Initialize Currency TomSelect
    function initializeCurrencyTomSelect(select, id) {
        initializeTomSelect(select, '/api/currencies', '1', 'HUF');
    }

    // Initialize VAT TomSelect
    function initializeVatTomSelect(select, id) {
        console.log(`Initializing Vat TomSelect for ${select.name}`);
        const selectedId = select.dataset.selectedId || '5'; // Default to 27%
        const selectedText = select.dataset.selectedText || '27%';
        const options = [
            { id: '1', text: '0%' },
            { id: '2', text: '5%' },
            { id: '3', text: '10%' },
            { id: '4', text: '20%' },
            { id: '5', text: '27%' }
        ];
        console.log(`Vat options:`, options);
        if (select.tomselect) select.tomselect.destroy();
        new TomSelect(select, {
            create: false,
            searchField: ['text'],
            maxItems: 1,
            valueField: 'id',
            labelField: 'text',
            options: options,
            onInitialize: function () {
                console.log(`TomSelect initialized for ${select.name}, setting value: ${selectedId}, text: ${selectedText}`);
                this.addOption({ id: selectedId, text: selectedText });
                this.setValue(selectedId);
            },
            onChange: function (value) {
                console.log('VAT changed for id:', id, 'New vatId:', value);
                calculateRowTotals(select.closest('tr'), id);
            }
        });
    }

    // Initialize Product TomSelect
    function initializeProductTomSelect(select, id) {
        const modal = select.closest('.modal');
        if (!modal) {
            console.error(`Modal not found for product select (order id: ${id})`);
            new TomSelect(select, {
                create: false,
                searchField: ['text'],
                maxItems: 1,
                valueField: 'id',
                labelField: 'text',
                options: [{ id: '0', text: 'No products available' }],
                onInitialize: function () {
                    this.setValue('0');
                }
            });
            return;
        }
        const partnerSelect = modal.querySelector('select[name="partnerId"].tom-select.partner-select');
        if (!partnerSelect) {
            console.warn(`Partner select not found for order id ${id}, initializing with no products`);
            if (select.tomselect) select.tomselect.destroy();
            new TomSelect(select, {
                create: false,
                searchField: ['text'],
                maxItems: 1,
                valueField: 'id',
                labelField: 'text',
                options: [{ id: '0', text: 'No products available (select a partner first)' }],
                onInitialize: function () {
                    const selectedId = select.dataset.selectedId || '';
                    const selectedText = select.dataset.selectedText || '';
                    if (selectedId && selectedText) {
                        this.addOption({ id: selectedId, text: selectedText });
                        this.setValue(selectedId);
                    }
                },
                onChange: function (value) {
                    console.log('Product changed for id:', id, 'New productId:', value);
                    calculateRowTotals(select.closest('tr'), id);
                }
            });
            return;
        }
        const partnerId = partnerSelect.value || '';
        const quantityInput = select.closest('tr').querySelector('.quantity');
        const quantity = parseInt(quantityInput?.value) || 1;
        const date = new Date().toISOString().split('T')[0];
        const productId = select.dataset.selectedId || '';

        const endpoint = `/api/products?partnerId=${partnerId}&productId=${productId}&quantity=${quantity}&date=${date}`;
        console.log(`Fetching products: http://localhost:8080${endpoint}`);
        fetch(`http://localhost:8080${endpoint}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        })
            .then(response => {
                if (!response.ok) throw new Error(`HTTP error for ${endpoint}: ${response.status}`);
                return response.json();
            })
            .then(data => {
                console.log(`Data received for ${endpoint}:`, data);
                const options = data.map(item => ({
                    id: item.id || item.Id || item.ID,
                    text: item.name || item.Name || 'Unknown'
                }));
                if (!options.length) {
                    options.push({ id: '0', text: 'No products available' });
                }
                if (select.tomselect) select.tomselect.destroy();
                new TomSelect(select, {
                    create: false,
                    searchField: ['text'],
                    maxItems: 1,
                    valueField: 'id',
                    labelField: 'text',
                    options: options,
                    onInitialize: function () {
                        const selectedId = select.dataset.selectedId || '';
                        const selectedText = select.dataset.selectedText || '';
                        if (selectedId && selectedText) {
                            this.addOption({ id: selectedId, text: selectedText });
                            this.setValue(selectedId);
                        }
                    },
                    onChange: function (value) {
                        console.log('Product changed for id:', id, 'New productId:', value);
                        calculateRowTotals(select.closest('tr'), id);
                    }
                });
            })
            .catch(error => {
                console.error(`Error fetching ${endpoint}:`, error);
                if (select.tomselect) select.tomselect.destroy();
                new TomSelect(select, {
                    create: false,
                    searchField: ['text'],
                    maxItems: 1,
                    valueField: 'id',
                    labelField: 'text',
                    options: [{ id: '0', text: 'Failed to load products' }],
                    onInitialize: function () {
                        this.setValue('0');
                    }
                });
            });
    }

    // Add Item Row
    function addItemRow(id) {
        console.log(`Adding item row for orderId: ${id}`);
        const tbody = document.querySelector(`#items-tbody_${id}`);
        if (!tbody) {
            console.error(`Items tbody not found for id: ${id}`);
            return;
        }
        const newItemId = 'new_' + Date.now();
        const itemRow = document.createElement('tr');
        itemRow.className = 'order-item-row';
        itemRow.dataset.itemId = newItemId;
        itemRow.innerHTML = `
            <td>
                <select name="orderItems[${newItemId}].productId" class="form-select tom-select-product" data-selected-id="" data-selected-text="" autocomplete="off" required>
                    <option value="" disabled selected>-- Válasszon terméket --</option>
                </select>
            </td>
            <td>
                <input type="number" name="orderItems[${newItemId}].quantity" class="form-control form-control-sm quantity" value="1" min="0" step="1" required>
            </td>
            <td>
                <input type="number" name="orderItems[${newItemId}].unitPrice" class="form-control form-control-sm unit-price" value="0" min="0" step="0.01" required>
            </td>
            <td>
                <div class="input-group input-group-sm">
                    <input type="text" name="orderItems[${newItemId}].discountPercentage" class="form-control discount-value" value="" placeholder="pl. 10">
                    <select class="form-select discount-type" data-discount-name-prefix="orderItems[${newItemId}]">
                        <option value="Percentage" selected>%</option>
                        <option value="Amount">Összeg</option>
                    </select>
                </div>
            </td>
            <td>
                <select name="orderItems[${newItemId}].currencyId" class="form-select tom-select-currency" data-selected-id="1" data-selected-text="HUF" autocomplete="off">
                    <option value="" disabled selected>-- Valuta --</option>
                </select>
            </td>
            <td>
                <span class="item-net-price">0.00</span>
            </td>
            <td>
                <select name="orderItems[${newItemId}].vatRate" class="form-select tom-select-vat" data-selected-id="5" data-selected-text="27%" autocomplete="off">
                    <option value="0" disabled selected>---</option>
                    <option value="1">0%</option>
                    <option value="2">5%</option>
                    <option value="3">10%</option>
                    <option value="4">20%</option>
                    <option value="5">27%</option>
                </select>
            </td>
            <td>
                <span class="item-gross-price">0.00</span>
                <input type="hidden" name="orderItems[${newItemId}].description" class="item-description" value="">
            </td>
            <td>
                <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${newItemId}"><i class="bi bi-pencil"></i></button>
                <button type="button" class="btn btn-danger btn-sm remove-item-row"><i class="bi bi-trash"></i></button>
            </td>
        `;
        tbody.insertBefore(itemRow, tbody.querySelector('.order-total-row'));
        const modal = tbody.closest('.modal');
        if (!modal) {
            console.error(`Modal not found for new item row (order id: ${id})`);
            return;
        }
        // Initialize PartnerId first
        const partnerSelect = modal.querySelector('select[name="partnerId"].tom-select.partner-select');
        if (partnerSelect && !partnerSelect.tomselect) {
            initializeTomSelect(partnerSelect, '/api/partners', '', '');
        }
        // Then initialize other dropdowns
        const productSelect = itemRow.querySelector('.tom-select-product');
        const currencySelect = itemRow.querySelector('.tom-select-currency');
        const vatSelect = itemRow.querySelector('.tom-select-vat');
        requestAnimationFrame(() => {
            initializeCurrencyTomSelect(currencySelect, id);
            initializeVatTomSelect(vatSelect, id);
            initializeProductTomSelect(productSelect, id);
            initializeRowCalculations(itemRow, id);
        });
    }

    // Initialize Row Calculations
    function initializeRowCalculations(row, id) {
        console.log(`Initializing row calculations for orderId: ${id}`);
        const quantityInput = row.querySelector('.quantity');
        const unitPriceInput = row.querySelector('.unit-price');
        const discountValueInput = row.querySelector('.discount-value');
        const discountTypeSelect = row.querySelector('.discount-type');
        const currencySelect = row.querySelector('.tom-select-currency');
        const vatSelect = row.querySelector('.tom-select-vat');

        [quantityInput, unitPriceInput, discountValueInput, discountTypeSelect, currencySelect, vatSelect].forEach(input => {
            if (input) {
                input.addEventListener('input', () => calculateRowTotals(row, id));
                if (input.tagName === 'SELECT') input.addEventListener('change', () => calculateRowTotals(row, id));
            }
        });

        calculateRowTotals(row, id);
    }

    // Initialize Modal Dropdowns
    function initializeModalDropdowns(modal, orderId) {
        console.log(`Initializing dropdowns for modal, orderId: ${orderId}`);
        const partnerSelect = modal.querySelector('select[name="partnerId"].tom-select.partner-select');
        const siteSelect = modal.querySelector('select[name="siteId"].tom-select');
        const currencySelects = modal.querySelectorAll('select[name$="currencyId"].tom-select-currency');
        const quoteSelect = modal.querySelector('select[name="quoteId"].tom-select');
        const productSelects = modal.querySelectorAll('.tom-select-product');

        console.log('Found partnerSelect:', partnerSelect);
        console.log('Found siteSelect:', siteSelect);
        console.log('Found quoteSelect:', quoteSelect);
        console.log('Found currencySelects:', currencySelects.length);
        console.log('Found productSelects:', productSelects.length);

        // Initialize PartnerId first
        if (partnerSelect && !partnerSelect.tomselect) {
            initializeTomSelect(partnerSelect, '/api/partners', '', '');
        }
        if (siteSelect && !siteSelect.tomselect) {
            initializeTomSelect(siteSelect, '/api/sites', '', '');
        }
        if (quoteSelect && !quoteSelect.tomselect) {
            initializeTomSelect(quoteSelect, '/api/quotes', '', '');
        }
        currencySelects.forEach(select => {
            if (!select.tomselect) {
                initializeCurrencyTomSelect(select, orderId);
            }
        });
        productSelects.forEach(select => {
            if (!select.tomselect) {
                initializeProductTomSelect(select, orderId);
            }
        });

        modal.querySelectorAll('.order-item-row').forEach(row => {
            const vatSelect = row.querySelector('.tom-select-vat');
            if (vatSelect && !vatSelect.tomselect) {
                initializeVatTomSelect(vatSelect, orderId);
            }
            initializeRowCalculations(row, orderId);
        });
    }

    // Modal Event Listeners
    document.querySelectorAll('.modal').forEach(modal => {
        console.log('Registering modal:', modal.id);
        modal.addEventListener('shown.bs.modal', function () {
            const orderId = this.querySelector('form[id^="orderItemsForm_"]')?.dataset.orderId || 'new';
            console.log(`Modal shown for orderId: ${orderId}`);
            initializeModalDropdowns(this, orderId);
        });
    });

    // Add Item Row Event
    document.querySelectorAll('.add-item-row').forEach(button => {
        console.log('Binding add-item-row event for button:', button);
        button.addEventListener('click', () => {
            console.log('Add item row clicked, orderId:', button.dataset.orderId);
            addItemRow(button.dataset.orderId);
        });
    });

    // Remove Item Row
    document.addEventListener('click', function (e) {
        if (e.target.closest('.remove-item-row')) {
            console.log('Remove item row clicked');
            const row = e.target.closest('tr');
            const id = row.closest('tbody').id.replace('items-tbody_', '');
            row.remove();
            calculateTotals(id);
        }
    });

    // Edit Description
    document.addEventListener('click', function (e) {
        if (e.target.closest('.edit-description')) {
            console.log('Edit description clicked');
            const button = e.target.closest('.edit-description');
            const itemId = button.dataset.itemId;
            const row = document.querySelector(`tr[data-item-id="${itemId}"]`);
            const descriptionInput = row.querySelector('.item-description');
            const descriptionModal = document.querySelector('#editDescriptionModal');
            const modalInput = descriptionModal.querySelector('#item-description');
            const charCount = descriptionModal.querySelector('.char-count');
            modalInput.value = descriptionInput.value;
            charCount.textContent = descriptionInput.value.length;
            descriptionModal.querySelector('#edit-item-id').value = itemId;
            new bootstrap.Modal(descriptionModal).show();
        }
    });

    document.querySelector('#save-description')?.addEventListener('click', function () {
        console.log('Save description clicked');
        const modal = document.querySelector('#editDescriptionModal');
        const itemId = modal.querySelector('#edit-item-id').value;
        const description = modal.querySelector('#item-description').value;
        const row = document.querySelector(`tr[data-item-id="${itemId}"]`);
        if (row) {
            const descriptionInput = row.querySelector('.item-description');
            descriptionInput.value = description;
            bootstrap.Modal.getInstance(modal).hide();
        }
    });

    document.querySelector('#item-description')?.addEventListener('input', function () {
        console.log('Item description input changed');
        const charCount = document.querySelector('#editDescriptionModal .char-count');
        charCount.textContent = this.value.length;
    });

    // Save Order
    document.querySelectorAll('.save-order').forEach(button => {
        console.log('Binding save-order event for button:', button);
        button.addEventListener('click', function () {
            const orderId = button.dataset.orderId;
            const baseForm = document.querySelector(`#orderBaseInfoForm_${orderId}`);
            const itemsForm = document.querySelector(`#orderItemsForm_${orderId}`);
            const baseData = Object.fromEntries(new FormData(baseForm));
            const itemsData = Object.fromEntries(new FormData(itemsForm));
            console.log('Saving order:', { orderId, baseData, itemsData });
            // Implement AJAX save logic here
            // Note: currencyId and vatRate need to be handled server-side
        });
    });

    // Copy Order
    window.copyOrder = function (orderId) {
        console.log('Copying order:', orderId);
        // Implement copy logic here
    };
});