// Line 1: Start of DOMContentLoaded event listener
document.addEventListener('DOMContentLoaded', function () {
    // Line 2-28: Initialize TomSelect for partner and currency dropdowns
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
                if (selectedId && selectedText) {
                    this.addOption({ value: selectedId, text: selectedText });
                    this.setValue(selectedId);
                }
            }
        });
    });

    console.log('TomSelect available:', typeof TomSelect); // Debug TomSelect loading

    // Line 30-42: Initialize TomSelect for rows when modal is shown
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('shown.bs.modal', function () {
            const quoteId = this.id.split('_')[1] || 'new';
            console.log('Modal shown for quoteId:', quoteId); // Debug modal open
            const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
            rows.forEach(row => {
                const itemId = row.getAttribute('data-item-id');
                initializeTomSelectForRow(quoteId, itemId);
            });
            updateDiscountFieldState(quoteId);
            updateQuoteTotals(quoteId);
        });
    });

    // Line 44-79: Handle partner dropdown change
    document.addEventListener('change', function (e) {
        if (e.target.matches('.tom-select')) {
            const partnerSelect = e.target.closest('#partnerSelect_new') || e.target.closest(`#partnerSelect_${e.target.id.split('_')[1]}`);
            if (partnerSelect) {
                const quoteId = partnerSelect.id.split('_')[1] || 'new';
                const partnerId = partnerSelect.tomselect ? partnerSelect.tomselect.getValue() : partnerSelect.getAttribute('data-selected-id') || '';
                console.log('Partner changed to partnerId:', partnerId); // Debug
                document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-product`).forEach(productSelect => {
                    productSelect.setAttribute('data-partner-id', partnerId);
                    const itemId = productSelect.getAttribute('data-item-id');
                    const tomSelect = productSelect.tomselect;
                    if (tomSelect && tomSelect.getValue()) {
                        const selectedId = tomSelect.getValue();
                        const selectedText = tomSelect.options[selectedId].text;
                        const quoteDate = productSelect.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0];
                        const quantity = parseInt(document.querySelector(`#items-tbody_${quoteId} input[name="quoteItems[${itemId}].Quantity"]`)?.value) || 1;
                        fetch(`/api/Product?search=${encodeURIComponent(selectedText)}&partnerId=${encodeURIComponent(partnerId)}&quoteDate=${encodeURIComponent(quoteDate)}&quantity=${quantity}`)
                            .then(response => response.json())
                            .then(data => {
                                console.log('Product API response after partner change:', JSON.stringify(data, null, 2)); // Debug
                                const product = data.find(p => p.productId == selectedId);
                                if (product) {
                                    tomSelect.addOption({
                                        id: product.productId,
                                        text: product.name,
                                        listPrice: product.listPrice,
                                        partnerPrice: product.partnerPrice,
                                        volumePrice: product.volumePrice,
                                        unitPrice: product.unitPrice
                                    });
                                    tomSelect.setValue(selectedId);
                                    const row = productSelect.closest('tr');
                                    const listPriceInput = row.querySelector('.item-list-price');
                                    const listPrice = product.listPrice != null ? product.listPrice : 0;
                                    listPriceInput.value = listPrice.toFixed(2);
                                    updateQuoteTotals(quoteId);
                                }
                            });
                    }
                });
            }
        }
    });

    // Line 81-88: Handle total discount change
    document.addEventListener('input', function (e) {
        if (e.target.matches('.total-discount-input')) {
            const quoteId = e.target.closest('form').id.split('_')[1] || 'new';
            updateDiscountFieldState(quoteId);
            updateQuoteTotals(quoteId);
        }
    });

    // Line 90-218: Handle click events (add item, remove item, edit description, delete quote)
    document.addEventListener('click', async function (e) {
        if (e.target.closest('.add-item-row')) {
            console.log('Add item clicked for quoteId:', e.target.closest('.add-item-row').getAttribute('data-quote-id')); // Debug
            const button = e.target.closest('.add-item-row');
            const quoteId = button.getAttribute('data-quote-id') || 'new';
            const tbody = document.querySelector(`#items-tbody_${quoteId}`);
            if (!tbody) {
                console.error(`Table body #items-tbody_${quoteId} not found`);
                return;
            }
            const partnerSelect = document.querySelector(`#partnerSelect_${quoteId}`);
            const partnerId = partnerSelect ? partnerSelect.tomselect ? partnerSelect.tomselect.getValue() : partnerSelect.getAttribute('data-selected-id') || '' : '';
            const modal = document.querySelector(`#editQuoteModal_${quoteId}`) || document.querySelector('#newQuoteModal') || document.querySelector('.modal');
            const quoteDate = modal ? modal.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
            console.log('Adding row with partnerId:', partnerId, 'quoteDate:', quoteDate); // Debug

            const tempItemId = 'new_' + Date.now();
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

            const descriptionRow = document.createElement('tr');
            descriptionRow.classList.add('description-row');
            descriptionRow.setAttribute('data-item-id', tempItemId);
            descriptionRow.style.display = 'none';
            descriptionRow.innerHTML = `
                <td colspan="10">
                    <div class="mb-2">
                        <label class="form-label">Leírás (max 200 karakter)</label>
                        <textarea name="quoteItems[${tempItemId}].ItemDescriptionInput"
                                  class="form-control form-control-sm item-description-input"
                                  maxlength="200" rows="2"></textarea>
                        <div class="form-text">Karakterek: <span class="char-count">0</span>/200</div>
                    </div>
                </td>
            `;

            const totalRow = tbody.querySelector('.quote-total-row');
            if (totalRow) {
                tbody.insertBefore(newRow, totalRow);
                tbody.insertBefore(descriptionRow, totalRow);
            } else {
                console.error('Total row not found in tbody');
                tbody.appendChild(newRow);
                tbody.appendChild(descriptionRow);
            }

            initializeTomSelectForRow(quoteId, tempItemId);
            updateDiscountFieldState(quoteId);
            updateQuoteTotals(quoteId);
        }

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

        if (e.target.closest('.edit-description')) {
            const button = e.target.closest('.edit-description');
            const itemId = button.getAttribute('data-item-id');
            const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
            if (descriptionRow) {
                descriptionRow.style.display = descriptionRow.style.display === 'none' ? 'table-row' : 'none';
            }
        }

        if (e.target.closest('.confirm-delete-quote')) {
            const button = e.target.closest('.confirm-delete-quote');
            const quoteId = button.getAttribute('data-quote-id');
            const modal = document.querySelector(`#deleteQuoteModal_${quoteId}`);

            if (!modal) {
                console.error(`Delete modal not found for quoteId: ${quoteId}`);
                showToast('Hiba történt a törlés során. Kérjük, próbálja újra.', true);
                return;
            }

            console.log('Deleting quote with ID:', quoteId); // Debug

            try {
                const response = await fetch(`/api/quotes/${quoteId}`, {
                    method: 'DELETE',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                let responseData;
                try {
                    responseData = await response.json();
                } catch (jsonError) {
                    if (response.status === 204) {
                        responseData = { message: 'Success' };
                    } else {
                        const responseText = await response.text();
                        console.error('Non-JSON response received:', responseText);
                        throw new Error(`A szerver érvénytelen JSON-t adott vissza: ${responseText.substring(0, 50)}...`);
                    }
                }

                if (!response.ok) {
                    console.error('Error response:', responseData);
                    const errorMessage = typeof responseData === 'string' ? responseData : responseData.message || `HTTP hiba! Státusz: ${response.status}`;
                    throw new Error(errorMessage);
                }

                console.log('Quote deleted successfully:', quoteId);
                const bsModal = bootstrap.Modal.getInstance(modal);
                bsModal.hide();
                showToast('Árajánlat sikeresen törölve!');
                window.location.reload();
            } catch (error) {
                console.error('Error deleting quote:', error);
                showToast(`Hiba történt az árajánlat törlése során: ${error.message}`, true);
            }
        }
    });

    // Line 220-234: Handle input events
    document.addEventListener('input', function (e) {
        if (e.target.matches('.item-quantity, .discount-value, .discount-type-id, .tom-select-product, .tom-select-vat')) {
            const row = e.target.closest('.quote-item-row');
            const quoteId = row.closest('table').id.split('_')[1] || 'new';
            updateQuoteTotals(quoteId);
        }

        if (e.target.matches('.item-description-input')) {
            const textarea = e.target;
            const charCountSpan = textarea.closest('td').querySelector('.char-count');
            charCountSpan.textContent = textarea.value.length;
        }
    });

    // Line 236-245: Handle dropdown clicks
    document.addEventListener('click', function (e) {
        if (e.target.closest('.tom-select-product, .tom-select-vat')) {
            const select = e.target.closest('select');
            if (select && select.tomselect) {
                console.log('Dropdown clicked:', select.className); // Debug
                select.tomselect.open();
            }
        }
    });

    // Line 247-375: Handle save button for new/edit quote modals
    document.querySelectorAll('.save-quote').forEach(button => {
        button.addEventListener('click', async function () {
            const quoteId = this.getAttribute('data-quote-id');
            const isNewQuote = quoteId === 'new';
            const modalId = isNewQuote ? 'newQuoteModal' : `editQuoteModal_${quoteId}`;
            const baseInfoForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
            const itemsForm = document.querySelector(`#quoteItemsForm_${quoteId}`);
            const modal = document.querySelector(`#${modalId}`);

            if (!baseInfoForm || !itemsForm || !modal) {
                console.error(`Form or modal not found for quoteId: ${quoteId}`);
                showToast('Hiba történt az űrlap betöltése során. Kérjük, próbálja újra.', true);
                return;
            }

            if (!baseInfoForm.checkValidity() || !itemsForm.checkValidity()) {
                baseInfoForm.reportValidity();
                itemsForm.reportValidity();
                showToast('Kérjük, töltse ki az összes kötelező mezőt.', true);
                return;
            }

            const originalButtonText = button.innerHTML;
            button.disabled = true;
            button.innerHTML = 'Mentés...';

            const baseInfoData = new FormData(baseInfoForm);
            const quoteData = {
                QuoteId: isNewQuote ? 0 : parseInt(quoteId),
                QuoteNumber: baseInfoData.get('quoteNumber') || '',
                QuoteDate: baseInfoData.get('quoteDate'),
                PartnerId: parseInt(baseInfoData.get('PartnerId')) || 0,
                CurrencyId: parseInt(baseInfoData.get('CurrencyId')) || 0,
                SalesPerson: baseInfoData.get('salesPerson') || '',
                ValidityDate: baseInfoData.get('validityDate'),
                Subject: baseInfoData.get('subject') || '',
                Description: baseInfoData.get('description') || '',
                DetailedDescription: baseInfoData.get('detailedDescription') || '',
                Status: baseInfoData.get('status') || 'Draft',
                DiscountPercentage: parseFloat(baseInfoData.get('TotalDiscount')) || 0,
                TotalAmount: 0,
                QuoteItems: []
            };

            let totalNet = 0;
            let totalVat = 0;
            let totalGross = 0;

            const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
            rows.forEach(row => {
                const itemId = row.getAttribute('data-item-id');
                const productSelect = row.querySelector(`select[name="quoteItems[${itemId}].ProductId"]`);
                const vatSelect = row.querySelector(`select[name="quoteItems[${itemId}].VatTypeId"]`);
                const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
                const quantity = parseFloat(row.querySelector(`input[name="quoteItems[${itemId}].Quantity"]`).value) || 0;
                const listPrice = parseFloat(row.querySelector(`input[name="quoteItems[${itemId}].ListPrice"]`).value) || 0;
                const discountType = parseInt(row.querySelector(`select[name="quoteItems[${itemId}].DiscountTypeId"]`).value) || 1;
                const discountAmountInput = row.querySelector(`input[name="quoteItems[${itemId}].DiscountAmount"]`);
                let discountAmount = parseFloat(discountAmountInput.value) || 0;
                const vatRate = vatSelect && vatSelect.tomselect && vatSelect.tomselect.options[vatSelect.value]
                    ? parseFloat(vatSelect.tomselect.options[vatSelect.value].rate) || 0
                    : 0;

                // Calculate item-level prices
                let netDiscountedPrice = listPrice;
                if (quoteData.DiscountPercentage === 0) {
                    if (discountType == 5) {
                        netDiscountedPrice = listPrice * (1 - discountAmount / 100);
                    } else if (discountType == 6) {
                        netDiscountedPrice = listPrice - discountAmount;
                    } else if (discountType == 3 && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
                        netDiscountedPrice = productSelect.tomselect.options[productSelect.value].partnerPrice || listPrice;
                        discountAmount = listPrice - netDiscountedPrice;
                        if (discountAmount < 0) discountAmount = 0;
                        discountAmountInput.value = discountAmount.toFixed(2);
                    } else if (discountType == 4 && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
                        netDiscountedPrice = productSelect.tomselect.options[productSelect.value].volumePrice || listPrice;
                        discountAmount = listPrice - netDiscountedPrice;
                        if (discountAmount < 0) discountAmount = 0;
                        discountAmountInput.value = discountAmount.toFixed(2);
                    }
                }
                const totalPrice = quantity * netDiscountedPrice * (1 + vatRate / 100);

                const itemData = {
                    QuoteItemId: itemId.startsWith('new_') ? 0 : parseInt(itemId),
                    ProductId: parseInt(productSelect.value) || 0,
                    Quantity: quantity,
                    ListPrice: listPrice,
                    NetDiscountedPrice: netDiscountedPrice,
                    TotalPrice: totalPrice,
                    VatTypeId: parseInt(vatSelect.value) || 0,
                    DiscountTypeId: discountType,
                    DiscountAmount: discountAmount,
                    PartnerPrice: productSelect.tomselect && productSelect.tomselect.options[productSelect.value]
                        ? productSelect.tomselect.options[productSelect.value].partnerPrice || null
                        : null,
                    VolumePrice: productSelect.tomselect && productSelect.tomselect.options[productSelect.value]
                        ? productSelect.tomselect.options[productSelect.value].volumePrice || null
                        : null,
                    ItemDescription: descriptionRow ? descriptionRow.querySelector(`textarea[name="quoteItems[${itemId}].ItemDescriptionInput"]`).value || '' : ''
                };
                quoteData.QuoteItems.push(itemData);

                totalNet += quantity * netDiscountedPrice;
                totalVat += quantity * netDiscountedPrice * (vatRate / 100);
                totalGross += totalPrice;
            });

            if (quoteData.DiscountPercentage > 0) {
                totalNet *= (1 - quoteData.DiscountPercentage / 100);
                totalVat = totalNet * (totalVat / (totalNet || 1));
                totalGross = totalNet + totalVat;
            }

            quoteData.TotalAmount = totalGross;

            // Update totals in the UI
            updateQuoteTotals(quoteId);

            quoteData.TotalNet = totalNet;
            quoteData.TotalVat = totalVat;
            quoteData.TotalGross = totalGross;

            console.log('Saving quote data:', JSON.stringify(quoteData, null, 2)); // Debug

            try {
                const url = isNewQuote ? '/api/quotes' : `/api/quotes/${quoteId}`;
                const method = isNewQuote ? 'POST' : 'PUT';
                const response = await fetch(url, {
                    method: method,
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(quoteData)
                });

                let responseData;
                try {
                    responseData = await response.json();
                } catch (jsonError) {
                    const responseText = await response.text();
                    console.error('Non-JSON response received:', responseText);
                    throw new Error(`A szerver érvénytelen JSON-t adott vissza: ${responseText.substring(0, 50)}...`);
                }

                if (!response.ok) {
                    console.error('Error response:', responseData);
                    const errorMessage = typeof responseData === 'string' ? responseData : responseData.message || `HTTP hiba! Státusz: ${response.status}`;
                    if (errorMessage.includes('QuoteItem.QuoteItemId')) {
                        throw new Error('Hiba az árajánlat tételek frissítése során. Kérjük, ellenőrizze a tételeket és próbálja újra.');
                    }
                    throw new Error(errorMessage);
                }

                console.log('Success response:', responseData);
                const bsModal = bootstrap.Modal.getInstance(modal);
                bsModal.hide();
                showToast(isNewQuote ? 'Árajánlat sikeresen létrehozva!' : 'Árajánlat sikeresen frissítve!');
                window.location.reload();
            } catch (error) {
                console.error('Error saving quote:', error);
                showToast(`Hiba történt az árajánlat mentése során: ${error.message}`, true);
            } finally {
                button.disabled = false;
                button.innerHTML = originalButtonText;
            }
        });
    });

    // Line 377-396: Update discount field state
    function updateDiscountFieldState(quoteId) {
        const totalDiscountInput = document.querySelector(`#quoteItemsForm_${quoteId} .total-discount-input`);
        const totalDiscount = parseFloat(totalDiscountInput.value) || 0;
        const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
        rows.forEach(row => {
            const discountTypeSelect = row.querySelector('.discount-type-id');
            const discountAmountInput = row.querySelector('.discount-value');
            if (totalDiscount > 0) {
                discountTypeSelect.disabled = true;
                discountAmountInput.disabled = true;
                discountAmountInput.value = 0;
            } else {
                discountTypeSelect.disabled = false;
                discountAmountInput.disabled = false;
            }
        });
        console.log('Discount field state updated for quoteId:', quoteId, 'totalDiscount:', totalDiscount); // Debug
    }

    // Line 398-651: Initialize TomSelect for product and VAT dropdowns
    function initializeTomSelectForRow(quoteId, itemId) {
        console.log('initializeTomSelectForRow called for quoteId:', quoteId, 'itemId:', itemId); // Debug
        const productSelect = document.querySelector(`#items-tbody_${quoteId} select[name="quoteItems[${itemId}].ProductId"]`);
        const vatSelect = document.querySelector(`#items-tbody_${quoteId} select[name="quoteItems[${itemId}].VatTypeId"]`);
        const partnerSelect = document.querySelector(`#partnerSelect_${quoteId}`);
        const partnerId = partnerSelect ? partnerSelect.tomselect ? partnerSelect.tomselect.getValue() : partnerSelect.getAttribute('data-selected-id') || '' : '';
        const quoteDate = productSelect ? productSelect.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
        const quantity = parseInt(document.querySelector(`#items-tbody_${quoteId} input[name="quoteItems[${itemId}].Quantity"]`)?.value) || 1;
        const row = document.querySelector(`#items-tbody_${quoteId} .quote-item-row[data-item-id="${itemId}"]`);
        const discountTypeSelect = row.querySelector('.discount-type-id');
        const discountType = parseInt(discountTypeSelect.value) || 1; // Read from dropdown
        const discountAmountInput = row.querySelector('.discount-value');
        let discountAmount = parseFloat(discountAmountInput.value) || parseFloat(row.getAttribute('data-discount-amount')) || 0;
        console.log('DiscountTypeId for itemId:', itemId, 'is:', discountType, 'initial DiscountAmount:', discountAmount); // Debug

        if (productSelect && typeof TomSelect !== 'undefined') {
            console.log('Initializing product TomSelect for quoteId:', quoteId, 'itemId:', itemId, 'quoteDate:', quoteDate, 'partnerId:', partnerId); // Debug
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
                    console.log('Fetching products from:', url); // Debug
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
                            console.log('Product API response:', JSON.stringify(data, null, 2)); // Debug
                            const formattedData = data.map(product => ({
                                id: product.productId,
                                text: product.name,
                                listPrice: product.listPrice,
                                partnerPrice: product.partnerPrice,
                                volumePrice: product.volumePrice,
                                unitPrice: product.unitPrice
                            }));
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
                        console.log('Selected product:', { 
                            id: selectedOption.id, 
                            text: selectedOption.text, 
                            listPrice: selectedOption.listPrice, 
                            unitPrice: selectedOption.unitPrice, 
                            partnerPrice: selectedOption.partnerPrice, 
                            volumePrice: selectedOption.volumePrice 
                        }); // Debug
                        const quoteDate = productSelect.getAttribute('data-quote-date') || new Date().toISOString().split('T')[0];
                        const quantity = parseInt(row.querySelector(`input[name="quoteItems[${itemId}].Quantity"]`)?.value) || 1;
                        fetch(`/api/Product?search=${encodeURIComponent(selectedOption.text)}&partnerId=${encodeURIComponent(partnerId)}&quoteDate=${encodeURIComponent(quoteDate)}&quantity=${quantity}`)
                            .then(response => response.json())
                            .then(data => {
                                console.log('Product API response on change:', JSON.stringify(data, null, 2)); // Debug
                                const product = data.find(p => p.productId == selectedOption.id);
                                if (product) {
                                    const listPriceInput = row.querySelector('.item-list-price');
                                    const netDiscountedPriceSpan = row.querySelector('.item-net-discounted-price');
                                    const discountAmountInput = row.querySelector('.discount-value');
                                    const listPrice = product.listPrice != null ? product.listPrice : 0;
                                    const discountType = parseInt(row.querySelector('.discount-type-id').value) || 1;
                                    let netDiscountedPrice = listPrice;
                                    let discountAmount = parseFloat(discountAmountInput.value) || 0;

                                    if (discountType == 5) {
                                        netDiscountedPrice = listPrice * (1 - discountAmount / 100);
                                    } else if (discountType == 6) {
                                        netDiscountedPrice = listPrice - discountAmount;
                                    } else if (discountType == 3) {
                                        netDiscountedPrice = product.partnerPrice || listPrice;
                                        discountAmount = listPrice - netDiscountedPrice;
                                        if (discountAmount < 0) discountAmount = 0;
                                        discountAmountInput.value = discountAmount.toFixed(2);
                                    } else if (discountType == 4) {
                                        netDiscountedPrice = product.volumePrice || listPrice;
                                        discountAmount = listPrice - netDiscountedPrice;
                                        if (discountAmount < 0) discountAmount = 0;
                                        discountAmountInput.value = discountAmount.toFixed(2);
                                    }

                                    listPriceInput.value = listPrice.toFixed(2);
                                    netDiscountedPriceSpan.textContent = netDiscountedPrice.toFixed(2);
                                    updateQuoteTotals(quoteId);
                                }
                            });
                    }
                },
                onDropdownOpen: function() {
                    console.log('Product dropdown opened for quoteId:', quoteId, 'itemId:', itemId); // Debug
                }
            });

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
                                const netDiscountedPriceSpan = row.querySelector('.item-net-discounted-price');
                                const discountAmountInput = row.querySelector('.discount-value');
                                const listPrice = product.listPrice != null ? product.listPrice : 0;
                                let netDiscountedPrice = listPrice;
                                let discountAmount = parseFloat(discountAmountInput.value) || 0;

                                if (discountType == 5) {
                                    netDiscountedPrice = listPrice * (1 - discountAmount / 100);
                                } else if (discountType == 6) {
                                    netDiscountedPrice = listPrice - discountAmount;
                                } else if (discountType == 3) {
                                    netDiscountedPrice = product.partnerPrice || listPrice;
                                    discountAmount = listPrice - netDiscountedPrice;
                                    if (discountAmount < 0) discountAmount = 0;
                                    discountAmountInput.value = discountAmount.toFixed(2);
                                } else if (discountType == 4) {
                                    netDiscountedPrice = product.volumePrice || listPrice;
                                    discountAmount = listPrice - netDiscountedPrice;
                                    if (discountAmount < 0) discountAmount = 0;
                                    discountAmountInput.value = discountAmount.toFixed(2);
                                }

                                console.log('Setting initial prices for product:', { 
                                    productId: product.productId, 
                                    name: product.name, 
                                    listPrice: product.listPrice, 
                                    unitPrice: product.unitPrice, 
                                    partnerPrice: product.partnerPrice, 
                                    volumePrice: product.volumePrice,
                                    discountType,
                                    discountAmount,
                                    netDiscountedPrice
                                }); // Debug
                                if (listPrice === 0 || product.listPrice == null) {
                                    console.warn('ListPrice is zero or null for existing product:', product.productId, 'Falling back to 0');
                                    showToast('Warning: ListPrice is missing for existing product ' + product.name + '. Check ProductPrices table.', true);
                                }
                                listPriceInput.value = listPrice.toFixed(2);
                                netDiscountedPriceSpan.textContent = netDiscountedPrice.toFixed(2);
                                productTomSelect.addOption({
                                    id: product.productId,
                                    text: product.name,
                                    listPrice: product.listPrice,
                                    partnerPrice: product.partnerPrice,
                                    volumePrice: product.volumePrice,
                                    unitPrice: product.unitPrice
                                });
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
                    console.log('Fetching VAT types from: /api/vat/types'); // Debug
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
                            console.log('VAT API response:', JSON.stringify(data, null, 2)); // Debug
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

    // Line 653-744: Update quote totals
    function updateQuoteTotals(quoteId) {
        console.log('updateQuoteTotals called for quoteId:', quoteId); // Debug
        const tbody = document.querySelector(`#items-tbody_${quoteId}`);
        const partnerSelect = document.querySelector(`#partnerSelect_${quoteId}`);
        const partnerId = partnerSelect ? partnerSelect.tomselect ? partnerSelect.tomselect.getValue() : partnerSelect.getAttribute('data-selected-id') || '' : '';
        const totalDiscountInput = document.querySelector(`#quoteItemsForm_${quoteId} .total-discount-input`);
        const totalDiscount = parseFloat(totalDiscountInput.value) || 0;
        console.log('Updating totals for quoteId:', quoteId, 'partnerId:', partnerId, 'totalDiscount:', totalDiscount); // Debug
        let totalNet = 0;
        let totalVat = 0;
        let totalGross = 0;

        tbody.querySelectorAll('.quote-item-row').forEach(row => {
            const productSelect = row.querySelector('.tom-select-product');
            const quantity = parseFloat(row.querySelector('.item-quantity').value) || 0;
            const listPrice = parseFloat(row.querySelector('.item-list-price').value) || 0;
            const discountTypeSelect = row.querySelector('.discount-type-id');
            const discountType = parseInt(discountTypeSelect.value) || 1;
            const discountAmountInput = row.querySelector('.discount-value');
            let discountAmount = parseFloat(discountAmountInput.value) || parseFloat(row.getAttribute('data-discount-amount')) || 0;
            const vatSelect = row.querySelector('.tom-select-vat');
            const vatRate = vatSelect && vatSelect.tomselect && vatSelect.tomselect.options[vatSelect.value]
                ? parseFloat(vatSelect.tomselect.options[vatSelect.value].rate) || 0
                : 0;
            console.log('Processing row for itemId:', row.getAttribute('data-item-id'), 'discountType:', discountType, 'discountAmount:', discountAmount); // Debug

            let netDiscountedPrice = listPrice;
            let calculatedDiscountAmount = discountAmount;

            if (totalDiscount === 0) {
                if (discountType == 5) {
                    netDiscountedPrice = listPrice * (1 - discountAmount / 100);
                } else if (discountType == 6) {
                    netDiscountedPrice = listPrice - discountAmount;
                } else if (discountType == 3 && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
                    const partnerPrice = productSelect.tomselect.options[productSelect.value].partnerPrice;
                    if (partnerPrice != null) {
                        netDiscountedPrice = partnerPrice;
                        calculatedDiscountAmount = listPrice - partnerPrice;
                        if (calculatedDiscountAmount < 0) calculatedDiscountAmount = 0;
                        console.log('Ügyfélár applied:', { productId: productSelect.tomselect.options[productSelect.value].id, listPrice, partnerPrice, calculatedDiscountAmount }); // Debug
                        discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
                    } else {
                        console.warn('PartnerPrice is null for product:', productSelect.tomselect.options[productSelect.value].id, 'PartnerId:', partnerId);
                        if (!partnerId) {
                            console.warn('No partner selected (partnerId is empty). Please select a partner.');
                            showToast('Kérem, válasszon egy partnert az Ügyfélár alkalmazásához.', true);
                        } else {
                            console.warn('No PartnerPrice available for product:', productSelect.tomselect.options[productSelect.value].id, 'PartnerId:', partnerId);
                            showToast('Nincs ügyfélár meghatározva a kiválasztott termékhez (' + productSelect.tomselect.options[productSelect.value].text + ') és partnerhez (ID: ' + partnerId + ').', true);
                        }
                        calculatedDiscountAmount = 0;
                        discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
                    }
                } else if (discountType == 4 && productSelect.tomselect && productSelect.tomselect.options[productSelect.value]) {
                    const volumePrice = productSelect.tomselect.options[productSelect.value].volumePrice;
                    if (volumePrice != null) {
                        netDiscountedPrice = volumePrice;
                        calculatedDiscountAmount = listPrice - volumePrice;
                        if (calculatedDiscountAmount < 0) calculatedDiscountAmount = 0;
                        console.log('Mennyiségi kedvezmény applied:', { productId: productSelect.tomselect.options[productSelect.value].id, listPrice, volumePrice, calculatedDiscountAmount }); // Debug
                        discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
                    } else {
                        console.warn('VolumePrice is null for product:', productSelect.tomselect.options[productSelect.value].id, 'Falling back to ListPrice');
                        calculatedDiscountAmount = 0;
                        discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
                    }
                } else {
                    calculatedDiscountAmount = 0;
                    discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
                }
            } else {
                calculatedDiscountAmount = 0;
                discountAmountInput.value = calculatedDiscountAmount.toFixed(2);
            }

            const rowNetTotal = quantity * netDiscountedPrice;
            const rowGrossTotal = rowNetTotal * (1 + vatRate / 100);
            const rowVatTotal = rowGrossTotal - rowNetTotal;

            totalNet += rowNetTotal;
            totalVat += rowVatTotal;
            totalGross += rowGrossTotal;

            row.querySelector('.item-net-discounted-price').textContent = netDiscountedPrice.toFixed(2);
            row.querySelector('.item-list-price-total').textContent = rowNetTotal.toFixed(2);
            row.querySelector('.item-gross-price').textContent = rowGrossTotal.toFixed(2);
        });

        if (totalDiscount > 0) {
            totalNet *= (1 - totalDiscount / 100);
            totalVat = totalNet * (totalVat / (totalNet || 1));
            totalGross = totalNet + totalVat;
        }

        document.querySelector(`#items-tbody_${quoteId} .quote-total-net`).textContent = totalNet.toFixed(2);
        document.querySelector(`#items-tbody_${quoteId} .quote-vat-amount`).textContent = totalVat.toFixed(2);
        document.querySelector(`#items-tbody_${quoteId} .quote-gross-amount`).textContent = totalGross.toFixed(2);

        document.querySelector(`#quoteItemsForm_${quoteId} .quote-total-net-input`).value = totalNet.toFixed(2);
        document.querySelector(`#quoteItemsForm_${quoteId} .quote-vat-amount-input`).value = totalVat.toFixed(2);
        document.querySelector(`#quoteItemsForm_${quoteId} .quote-gross-amount-input`).value = totalGross.toFixed(2);
    }

    // Line 746-764: Toast notification function
    function showToast(message, isError = false) {
        const toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.innerHTML = `
            <div class="toast ${isError ? 'bg-danger' : 'bg-success'} text-white" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <strong class="me-auto">${isError ? 'Hiba' : 'Siker'}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">${message}</div>
            </div>
        `;
        document.body.appendChild(toastContainer);
        const toast = new bootstrap.Toast(toastContainer.querySelector('.toast'));
        toast.show();
    }
}); // Line 764: End of DOMContentLoaded