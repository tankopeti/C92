    document.addEventListener('DOMContentLoaded', function () {
        // Initialize TomSelect on elements with class 'tom-select'
        document.querySelectorAll('.tom-select').forEach(function (select) {
            const selectedId = select.getAttribute('data-selected-id');
            const selectedText = select.getAttribute('data-selected-text');
            const isPartnerSelect = select.id.startsWith('partnerSelect_');
            const endpoint = isPartnerSelect ? '/api/partners/select' : '/api/currencies/select';

            new TomSelect(select, {
                create: false,
                maxItems: 1,
                valueField: 'value',
                labelField: 'text',
                searchField: ['text'],
                placeholder: select.querySelector('option[value=""]').text,
                allowEmptyOption: true,
                preload: 'focus',
                load: function (query, callback) {
                    // Fetch options dynamically from the appropriate endpoint
                    fetch(`${endpoint}?search=${encodeURIComponent(query)}`)
                        .then(response => response.json())
                        .then(data => {
                            callback(data.map(item => ({
                                value: item.id,
                                text: item.text
                            })));
                        })
                        .catch(() => callback());
                },
                onInitialize: function () {
                    // Set pre-selected option if available
                    if (selectedId && selectedText) {
                        this.addOption({ value: selectedId, text: selectedText });
                        this.setValue(selectedId);
                    }
                }
            });
        });





    console.log('TomSelect available:', typeof TomSelect); // Debug TomSelect loading

    // Initialize TomSelect for existing rows when modal is shown
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('shown.bs.modal', function () {
            const quoteId = this.id.split('_')[1] || 'new';
            console.log('Modal shown for quoteId:', quoteId); // Debug modal open
            const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
            rows.forEach(row => {
                const itemId = row.getAttribute('data-item-id');
                initializeTomSelectForRow(quoteId, itemId);
            });
        });
    });

    // Handle "Tétel hozzáadása" button click
    document.addEventListener('click', function (e) {
        if (e.target.closest('.add-item-row')) {
            console.log('Add item clicked for quoteId:', e.target.closest('.add-item-row').getAttribute('data-quote-id')); // Debug button click
            const button = e.target.closest('.add-item-row');
            const quoteId = button.getAttribute('data-quote-id') || 'new';
            const tbody = document.querySelector(`#items-tbody_${quoteId}`);
            if (!tbody) {
                console.error(`Table body #items-tbody_${quoteId} not found`);
                return;
            }
            // Get partnerId and quoteDate from base info tab
            const partnerSelect = document.querySelector(`#partnerSelect_${quoteId}`);
            const partnerId = partnerSelect ? partnerSelect.getAttribute('data-selected-id') || '' : '';
            const modal = document.querySelector(`#editQuoteModal_${quoteId}`) || document.querySelector('#newQuoteModal') || document.querySelector('.modal');
            const quoteDate = modal ? modal.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
            console.log('Adding row with partnerId:', partnerId, 'quoteDate:', quoteDate); // Debug parameters

            // Generate a unique temporary ID for the new row
            const tempItemId = 'new_' + Date.now();

            // Create a new table row
            const newRow = document.createElement('tr');
            newRow.classList.add('quote-item-row');
            newRow.setAttribute('data-item-id', tempItemId);

            newRow.innerHTML = `
                <td>
                    <select name="quoteItems[${tempItemId}].ProductId" 
                            class="form-select tom-select-product" 
                            data-quote-id="${quoteId}" 
                            data-item-id="${tempItemId}" 
                            data-partner-id="${partnerId}" 
                            data-quote-date="${quoteDate}" 
                            autocomplete="off" required>
                        <option value="">Válasszon terméket...</option>
                    </select>
                </td>
                <td>
                    <input type="number" name="quoteItems[${tempItemId}].Quantity" 
                           class="form-control form-control-sm item-quantity" 
                           value="1" min="0" step="1" required>
                </td>
                <td>
                    <input type="number" name="quoteItems[${tempItemId}].ListPrice" 
                           class="form-control form-control-sm item-list-price" 
                           value="0" min="0" step="0.01" readonly 
                           style="background-color: #f8f9fa; cursor: not-allowed;">
                </td>
                <td>
                    <select name="quoteItems[${tempItemId}].DiscountTypeId" 
                            class="form-select form-select-sm discount-type-id" 
                            data-discount-name-prefix="quoteItems[${tempItemId}]">
                        <option value="1" selected>Nincs Kedvezmény</option>
                        <option value="2">Listaár</option>
                        <option value="3">Ügyfélár</option>
                        <option value="4">Mennyiségi kedvezmény</option>
                        <option value="5">Egyedi kedvezmény %</option>
                        <option value="6">Egyedi kedvezmény Összeg</option>
                    </select>
                </td>
                <td>
                    <input type="number" name="quoteItems[${tempItemId}].DiscountAmount" 
                           class="form-control form-control-sm discount-value" 
                           value="0" min="0" step="0.01">
                </td>
                <td>
                    <span class="item-net-discounted-price">0.00</span>
                </td>
                <td>
                    <select name="quoteItems[${tempItemId}].VatTypeId" 
                            class="form-select tom-select-vat" 
                            autocomplete="off" required>
                        <option value="">Válasszon ÁFA típust...</option>
                    </select>
                </td>
                <td>
                    <span class="item-list-price-total">0.00</span>
                </td>
                <td>
                    <span class="item-gross-price">0.00</span>
                </td>
                <td>
                    <button type="button" class="btn btn-outline-secondary btn-sm edit-description" 
                            data-item-id="${tempItemId}"><i class="bi bi-pencil"></i></button>
                    <button type="button" class="btn btn-danger btn-sm remove-item-row" 
                            data-item-id="${tempItemId}"><i class="bi bi-trash"></i></button>
                </td>
            `;

            // Append the new row before the total rows
            const totalRow = tbody.querySelector('.quote-total-row');
            if (totalRow) {
                tbody.insertBefore(newRow, totalRow);
            } else {
                console.error('Total row not found in tbody');
                tbody.appendChild(newRow);
            }

            // Initialize TomSelect for both dropdowns
            initializeTomSelectForRow(quoteId, tempItemId);

            // Update totals
            updateQuoteTotals(quoteId);
        }

        // Handle "Remove Item" button
        if (e.target.closest('.remove-item-row')) {
            const button = e.target.closest('.remove-item-row');
            const itemId = button.getAttribute('data-item-id');
            const row = document.querySelector(`.quote-item-row[data-item-id="${itemId}"]`);
            const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
            const quoteId = button.closest('table').id.split('_')[1] || 'new';

            if (row) row.remove();
            if (descriptionRow) descriptionRow.remove();
            updateQuoteTotals(quoteId);
        }

        // Handle "Edit Description" button
        if (e.target.closest('.edit-description')) {
            const button = e.target.closest('.edit-description');
            const itemId = button.getAttribute('data-item-id');
            const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
            if (descriptionRow) {
                descriptionRow.style.display = descriptionRow.style.display === 'none' ? 'table-row' : 'none';
            }
        }
    });

    // Handle quantity, discount, and dropdown changes
    document.addEventListener('input', function (e) {
        if (e.target.matches('.item-quantity, .discount-value, .discount-type-id, .tom-select-product, .tom-select-vat')) {
            const row = e.target.closest('.quote-item-row');
            const quoteId = row.closest('table').id.split('_')[1] || 'new';
            updateQuoteTotals(quoteId);
        }
    });

    // Ensure dropdowns open on click
    document.addEventListener('click', function (e) {
        if (e.target.closest('.tom-select-product, .tom-select-vat')) {
            const select = e.target.closest('select');
            if (select && select.tomselect) {
                console.log('Dropdown clicked:', select.className); // Debug
                select.tomselect.open();
            }
        }
    });
});

// Initialize TomSelect for product and VAT dropdowns
function initializeTomSelectForRow(quoteId, itemId) {
    const productSelect = document.querySelector(`#items-tbody_${quoteId} select[name="quoteItems[${itemId}].ProductId"]`);
    const vatSelect = document.querySelector(`#items-tbody_${quoteId} select[name="quoteItems[${itemId}].VatTypeId"]`);
    const partnerId = productSelect ? productSelect.getAttribute('data-partner-id') || '' : '';
    const quoteDate = productSelect ? productSelect.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
    const quantity = parseInt(document.querySelector(`#items-tbody_${quoteId} input[name="quoteItems[${itemId}].Quantity"]`)?.value) || 1;

    // Initialize TomSelect for product dropdown
    if (productSelect && typeof TomSelect !== 'undefined') {
        console.log('Initializing product TomSelect for quoteId:', quoteId, 'itemId:', itemId, 'quoteDate:', quoteDate); // Debug
        const productTomSelect = new TomSelect(productSelect, {
            create: true,
            sortField: { field: 'text', direction: 'asc' },
            valueField: 'id',
            labelField: 'text',
            searchField: ['text'],
            maxOptions: 50,
            allowEmptyOption: true,
            preload: 'focus',
            load: function(query, callback) {
                const url = `/api/Product?search=${encodeURIComponent(query)}&partnerId=${encodeURIComponent(partnerId)}&quoteDate=${encodeURIComponent(quoteDate)}&quantity=${quantity}`;
                console.log('Fetching products from:', url); // Debug API call
                fetch(url, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(`HTTP error! Status: ${response.status}`);
                        }
                        return response.json();
                    })
                    .then(data => {
                        console.log('Product API response:', JSON.stringify(data, null, 2)); // Debug full response
                        const formattedData = data.map(product => ({
                            id: product.productId,
                            text: product.name,
                            listPrice: product.listPrice,
                            partnerPrice: product.partnerPrice,
                            volumePrice: product.volumePrice,
                            unitPrice: product.unitPrice // Include for debugging
                        }));
                        formattedData.forEach(product => {
                            if (product.listPrice === product.unitPrice) {
                                console.warn('ListPrice equals UnitPrice for product:', product.id, 'ListPrice:', product.listPrice, 'UnitPrice:', product.unitPrice, 'Check ProductPrices table or quoteDate:', quoteDate);
                            }
                            if (product.id === 64151 && product.listPrice !== 980.00) {
                                console.error('Unexpected ListPrice for ProductId 64151:', product.listPrice, 'Expected: 980.00');
                            }
                        });
                        callback(formattedData);
                    })
                    .catch(error => {
                        console.error('Error fetching products:', error);
                        callback([]);
                    });
            },
            placeholder: 'Válasszon terméket...',
            render: {
                option: function(data, escape) {
                    return `<div>${escape(data.text)}</div>`;
                },
                item: function(data, escape) {
                    return `<div>${escape(data.text)}</div>`;
                }
            },
            onChange: function(value) {
                const row = productSelect.closest('tr');
                const selectedOption = this.options[value];
                if (selectedOption) {
                    const listPriceInput = row.querySelector('.item-list-price');
                    const listPrice = selectedOption.listPrice != null ? selectedOption.listPrice : 0;
                    console.log('Selected product:', { 
                        id: selectedOption.id, 
                        text: selectedOption.text, 
                        listPrice: selectedOption.listPrice, 
                        unitPrice: selectedOption.unitPrice, 
                        partnerPrice: selectedOption.partnerPrice, 
                        volumePrice: selectedOption.volumePrice 
                    }); // Debug all prices
                    if (listPrice === 0 || selectedOption.listPrice == null) {
                        console.warn('ListPrice is zero or null for product:', selectedOption.id, 'Falling back to 0');
                        alert('Warning: ListPrice is missing for product ' + selectedOption.text + '. Check ProductPrices table.');
                    }
                    if (selectedOption.listPrice === selectedOption.unitPrice) {
                        console.warn('ListPrice equals UnitPrice for selected product:', selectedOption.id, 'ListPrice:', selectedOption.listPrice, 'UnitPrice:', selectedOption.unitPrice, 'Check ProductPrices table or quoteDate:', quoteDate);
                    }
                    if (selectedOption.id === 64151 && selectedOption.listPrice !== 980.00) {
                        console.error('Unexpected ListPrice for ProductId 64151:', selectedOption.listPrice, 'Expected: 980.00');
                        alert('Error: ListPrice for Termék 111 is ' + selectedOption.listPrice + ' instead of 980.00. Check ProductPrices table or quoteDate.');
                    }
                    listPriceInput.value = listPrice.toFixed(2); // Use listPrice only
                    updateQuoteTotals(quoteId);
                }
            },
            onDropdownOpen: function() {
                console.log('Product dropdown opened for quoteId:', quoteId, 'itemId:', itemId); // Debug
            }
        });

        // For existing rows, set the initial product
        if (productSelect.hasAttribute('data-selected-id') && productSelect.getAttribute('data-selected-id')) {
            const selectedId = productSelect.getAttribute('data-selected-id');
            const selectedText = productSelect.getAttribute('data-selected-text');
            if (selectedId && selectedText) {
                productTomSelect.addOption({ id: selectedId, text: selectedText });
                productTomSelect.setValue(selectedId);
                fetch(`/api/Product?search=${encodeURIComponent(selectedText)}&partnerId=${encodeURIComponent(partnerId)}&quoteDate=${encodeURIComponent(quoteDate)}&quantity=${quantity}`)
                    .then(response => response.json())
                    .then(data => {
                        console.log('Product API response for existing row:', JSON.stringify(data, null, 2)); // Debug
                        const product = data.find(p => p.productId == selectedId);
                        if (product) {
                            const row = productSelect.closest('tr');
                            const listPriceInput = row.querySelector('.item-list-price');
                            const listPrice = product.listPrice != null ? product.listPrice : 0;
                            console.log('Setting initial prices for product:', { 
                                productId: product.productId, 
                                name: product.name, 
                                listPrice: product.listPrice, 
                                unitPrice: product.unitPrice, 
                                partnerPrice: product.partnerPrice, 
                                volumePrice: product.volumePrice 
                            }); // Debug all prices
                            if (listPrice === 0 || product.listPrice == null) {
                                console.warn('ListPrice is zero or null for existing product:', product.productId, 'Falling back to 0');
                                alert('Warning: ListPrice is missing for existing product ' + product.name + '. Check ProductPrices table.');
                            }
                            if (product.listPrice === product.unitPrice) {
                                console.warn('ListPrice equals UnitPrice for existing product:', product.productId, 'ListPrice:', product.listPrice, 'UnitPrice:', product.unitPrice, 'Check ProductPrices table or quoteDate:', quoteDate);
                            }
                            if (product.productId === 64151 && product.listPrice !== 980.00) {
                                console.error('Unexpected ListPrice for ProductId 64151:', product.listPrice, 'Expected: 980.00');
                                alert('Error: ListPrice for Termék 111 is ' + product.listPrice + ' instead of 980.00. Check ProductPrices table or quoteDate.');
                            }
                            listPriceInput.value = listPrice.toFixed(2); // Use listPrice only
                            updateQuoteTotals(quoteId);
                        }
                    });
            }
        }
    } else if (!productSelect) {
        console.error(`Product select not found for quoteId: ${quoteId}, itemId: ${itemId}`);
    } else {
        console.error('TomSelect is not defined for product select. Ensure the TomSelect library is loaded.');
    }

    // Initialize TomSelect for VAT dropdown
    if (vatSelect && typeof TomSelect !== 'undefined') {
        console.log('Initializing VAT TomSelect for quoteId:', quoteId, 'itemId:', itemId); // Debug
        const vatTomSelect = new TomSelect(vatSelect, {
            create: true,
            sortField: { field: 'text', direction: 'asc' },
            valueField: 'id',
            labelField: 'text',
            searchField: ['text'],
            maxOptions: 50,
            allowEmptyOption: true,
            preload: 'focus',
            load: function(query, callback) {
                console.log('Fetching VAT types from: /api/vat/types'); // Debug API call
                fetch('/api/vat/types', {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(`HTTP error! Status: ${response.status}`);
                        }
                        return response.json();
                    })
                    .then(data => {
                        console.log('VAT API response:', JSON.stringify(data, null, 2)); // Debug response
                        const formattedData = data.map(vat => ({
                            id: vat.vatTypeId,
                            text: vat.typeName,
                            rate: vat.rate
                        }));
                        callback(formattedData);
                    })
                    .catch(error => {
                        console.error('Error fetching VAT types:', error);
                        callback([]);
                    });
            },
            placeholder: 'Válasszon ÁFA típust...',
            render: {
                option: function(data, escape) {
                    return `<div>${escape(data.text)}</div>`;
                },
                item: function(data, escape) {
                    return `<div>${escape(data.text)}</div>`;
                }
            },
            onChange: function() {
                updateQuoteTotals(quoteId);
            },
            onDropdownOpen: function() {
                console.log('VAT dropdown opened for quoteId:', quoteId, 'itemId:', itemId); // Debug
            }
        });

        // For existing rows, set the initial VAT type
        if (vatSelect.hasAttribute('data-selected-id') && vatSelect.getAttribute('data-selected-id')) {
            const selectedId = vatSelect.getAttribute('data-selected-id');
            const selectedText = vatSelect.getAttribute('data-selected-text');
            if (selectedId && selectedText) {
                vatTomSelect.addOption({ id: selectedId, text: selectedText, rate: parseFloat(vatSelect.getAttribute('data-selected-text').match(/\d+/)?.[0]) || 0 });
                vatTomSelect.setValue(selectedId);
                updateQuoteTotals(quoteId);
            }
        }
    } else if (!vatSelect) {
        console.error(`VAT select not found for quoteId: ${quoteId}, itemId: ${itemId}`);
    } else {
        console.error('TomSelect is not defined for VAT select. Ensure the TomSelect library is loaded.');
    }
}

// Update quote totals
function updateQuoteTotals(quoteId) {
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    let totalNet = 0;
    let totalVat = 0;
    let totalGross = 0;

    tbody.querySelectorAll('.quote-item-row').forEach(row => {
        const productSelect = row.querySelector('.tom-select-product');
        const quantity = parseFloat(row.querySelector('.item-quantity').value) || 0;
        const listPrice = parseFloat(row.querySelector('.item-list-price').value) || 0;
        const discountType = row.querySelector('.discount-type-id').value;
        const discountAmount = parseFloat(row.querySelector('.discount-value').value) || 0;
        const vatSelect = row.querySelector('.tom-select-vat');
        const vatRate = vatSelect && vatSelect.tomselect && vatSelect.tomselect.options[vatSelect.value]
            ? parseFloat(vatSelect.tomselect.options[vatSelect.value].rate) || 0
            : 0;

        // Calculate net discounted price
        let netDiscountedPrice = listPrice;
        if (discountType == '5') { // Percentage discount
            netDiscountedPrice = listPrice * (1 - discountAmount / 100);
        } else if (discountType == '6') { // Fixed amount discount
            netDiscountedPrice = listPrice - discountAmount;
        } else if (discountType == '3' && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
            netDiscountedPrice = productSelect.tomselect.options[productSelect.value].partnerPrice || listPrice;
        } else if (discountType == '4' && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
            netDiscountedPrice = productSelect.tomselect.options[productSelect.value].volumePrice || listPrice;
        }

        const rowNetTotal = quantity * netDiscountedPrice;
        const rowGrossTotal = rowNetTotal * (1 + vatRate / 100);
        const rowVatTotal = rowGrossTotal - rowNetTotal;

        totalNet += rowNetTotal;
        totalVat += rowVatTotal;
        totalGross += rowNetTotal;

        row.querySelector('.item-net-discounted-price').textContent = netDiscountedPrice.toFixed(2);
        row.querySelector('.item-list-price-total').textContent = rowNetTotal.toFixed(2);
        row.querySelector('.item-gross-price').textContent = rowGrossTotal.toFixed(2);
    });

    // Apply total discount
    const totalDiscountInput = document.querySelector(`#quoteItemsForm_${quoteId} .total-discount-input`);
    const totalDiscount = parseFloat(totalDiscountInput.value) || 0;
    totalGross = totalGross * (1 - totalDiscount / 100);

    // Update total displays
    document.querySelector(`#items-tbody_${quoteId} .quote-total-net`).textContent = totalNet.toFixed(2);
    document.querySelector(`#items-tbody_${quoteId} .quote-vat-amount`).textContent = totalVat.toFixed(2);
    document.querySelector(`#items-tbody_${quoteId} .quote-gross-amount`).textContent = totalGross.toFixed(2);

    // Update hidden inputs
    document.querySelector(`#quoteItemsForm_${quoteId} .quote-total-net-input`).value = totalNet.toFixed(2);
    document.querySelector(`#quoteItemsForm_${quoteId} .quote-vat-amount-input`).value = totalVat.toFixed(2);
    document.querySelector(`#quoteItemsForm_${quoteId} .quote-gross-amount-input`).value = totalGross.toFixed(2);
}

// Fix modal focus trapping
document.addEventListener('shown.bs.modal', function (e) {
    const modal = e.target;
    const dropdowns = modal.querySelectorAll('.tom-select-product, .tom-select-vat');
    dropdowns.forEach(dropdown => {
        if (dropdown.tomselect) {
            dropdown.tomselect.control_input.addEventListener('focus', function () {
                console.log('Dropdown input focused:', dropdown.className); // Debug
                dropdown.tomselect.open();
            });
        }
    });
});