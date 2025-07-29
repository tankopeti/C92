document.addEventListener('DOMContentLoaded', function () {
    console.log('Main script loaded');

    const newOrderModal = document.getElementById('newOrderModal');
    if (newOrderModal) {
        newOrderModal.addEventListener('shown.bs.modal', async function () {
            console.log('New Order modal shown');

            const baseInfoForm = document.getElementById('orderBaseInfoForm_new');
            if (baseInfoForm) {
                const tomSelects = baseInfoForm.querySelectorAll('.tom-select');
                for (const select of tomSelects) {
                    const name = select.getAttribute('name');
                    console.log('Initializing TomSelect for:', name);
                    if (name === 'PartnerId' && !select.dataset.tomSelectInitialized) {
                        try {
                            // Fetch partner data
                            const url = '/api/Partners?search=&skip=0&take=50';
                            const headers = {
                                'Content-Type': 'application/json',
                                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                            };
                            const token = localStorage.getItem('token');
                            if (token) {
                                headers['Authorization'] = `Bearer ${token}`;
                            }

                            console.log(`Fetching partners from: ${url}`);
                            const response = await fetch(url, { headers, credentials: 'include' });
                            if (!response.ok) {
                                const errorText = await response.text().catch(() => 'Unknown error');
                                throw new Error(`Failed to fetch partners: ${response.status} - ${errorText}`);
                            }

                            const rawData = await response.json();
                            console.log('Raw partners response:', rawData);

                            // Map data using API-provided text
                            const data = rawData.map(item => ({
                                id: item.id,
                                text: item.text
                            })).filter(item => item.text && item.text.trim() !== '');

                            console.log('Mapped partners data:', data);

                            // Initialize TomSelect
                            const control = new TomSelect(select, {
                                valueField: 'id',
                                labelField: 'text',
                                searchField: ['text'],
                                maxOptions: null,
                                placeholder: '-- Válasszon partnert --',
                                allowEmptyOption: false,
                                create: false,
                                options: data.length ? data : [{ id: null, text: 'Nincs elérhető partner' }],
                                load: async function (query, callback) {
                                    try {
                                        const searchUrl = `/api/Partners?search=${encodeURIComponent(query)}&skip=0&take=50`;
                                        console.log(`Searching partners from: ${searchUrl}`);
                                        const searchResponse = await fetch(searchUrl, { headers, credentials: 'include' });
                                        if (!searchResponse.ok) {
                                            const errorText = await searchResponse.text().catch(() => 'Unknown error');
                                            throw new Error(`Failed to search partners: ${searchResponse.status} - ${errorText}`);
                                        }

                                        const searchData = await searchResponse.json();
                                        console.log('Search partners response:', searchData);
                                        const mappedData = searchData.map(item => ({
                                            id: item.id,
                                            text: item.text
                                        })).filter(item => item.text && item.text.trim() !== '');
                                        callback(mappedData.length ? mappedData : [{ id: null, text: 'Nincs találat' }]);
                                    } catch (error) {
                                        console.error('Error searching partners:', error);
                                        window.c92.showToast('error', `Hiba a partnerek keresése közben: ${error.message}`);
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
                                    select.dataset.tomSelectInitialized = 'true';
                                    console.log('Partner TomSelect initialized for newOrderModal');
                                },
                                onChange: function (value) {
                                    console.log(`Partner selected for newOrderModal: ${value}`);
                                    const siteSelect = document.querySelector('#site-select_new');
                                    if (siteSelect?.tomselect) {
                                        siteSelect.tomselect.clear();
                                        siteSelect.tomselect.clearOptions();
                                        siteSelect.tomselect.destroy();
                                        siteSelect.dataset.tomSelectInitialized = '';
                                        window.c92.initializeSiteTomSelect(siteSelect, 'new', 'order').then(control => {
                                            console.log('Site TomSelect reinitialized for partnerId:', value);
                                        });
                                    }
                                    const quoteSelect = document.querySelector('#quote-select_new');
                                    if (quoteSelect?.tomselect) {
                                        quoteSelect.tomselect.clear();
                                        quoteSelect.tomselect.clearOptions();
                                        quoteSelect.tomselect.destroy();
                                        quoteSelect.dataset.tomSelectInitialized = '';
                                        window.c92.initializeQuoteTomSelect(quoteSelect, 'new', 'order').then(control => {
                                            console.log('Quote TomSelect reinitialized for partnerId:', value);
                                        });
                                    }
                                }
                            });

                            console.log('Partner TomSelect options after init:', control.options);
                        } catch (error) {
                            console.error('Failed to initialize Partner TomSelect:', error);
                            window.c92.showToast('error', 'Hiba a partner választó inicializálása közben.');
                        }
                    } else if (name === 'currencyId' && !select.dataset.tomSelectInitialized) {
                        try {
                            await window.c92.initializeCurrencyTomSelect(select, 'order');
                            console.log('Currency TomSelect initialized');
                        } catch (error) {
                            console.error('Failed to initialize Currency TomSelect:', error);
                            window.c92.showToast('error', 'Hiba a pénznem választó inicializálása közben.');
                        }
                    } else if (name === 'SiteId' && !select.dataset.tomSelectInitialized) {
                        try {
                            await window.c92.initializeSiteTomSelect(select, 'new', 'order');
                            console.log('Site TomSelect initialized');
                        } catch (error) {
                            console.error('Failed to initialize Site TomSelect:', error);
                            window.c92.showToast('error', 'Hiba a telephely választó inicializálása közben.');
                        }
                    } else if (name === 'quoteId' && !select.dataset.tomSelectInitialized) {
                        try {
                            await window.c92.initializeQuoteTomSelect(select, 'new', 'order');
                            console.log('Quote TomSelect initialized');
                        } catch (error) {
                            console.error('Failed to initialize Quote TomSelect:', error);
                            window.c92.showToast('error', 'Hiba az árajánlat választó inicializálása közben.');
                        }
                    }
                }
            }

            const orderNumberInput = baseInfoForm.querySelector('input[name="orderNumber"]');
            if (orderNumberInput) {
                orderNumberInput.value = '';
                orderNumberInput.placeholder = 'Automatikusan generálódik';
                orderNumberInput.readOnly = true;
            }

            initializeEventListeners('new');
        });

        newOrderModal.addEventListener('hidden.bs.modal', function () {
            const baseInfoForm = document.getElementById('orderBaseInfoForm_new');
            if (baseInfoForm) {
                baseInfoForm.reset();
                const tomSelects = baseInfoForm.querySelectorAll('.tom-select');
                tomSelects.forEach(select => {
                    if (select.tomselect) {
                        select.tomselect.clear();
                        select.tomselect.clearOptions();
                        select.tomselect.destroy();
                        select.dataset.tomSelectInitialized = '';
                        console.log('Destroyed TomSelect for:', select.getAttribute('name'));
                    }
                });
            }
            const tbody = document.getElementById('items-tbody_new');
            if (tbody) {
                tbody.querySelectorAll('.order-item-row, .description-row').forEach(row => row.remove());
                window.calculateOrderTotals('new');
            }
        });
    }

    function initializeEventListeners(orderId) {
        console.log(`Initializing event listeners for orderId: ${orderId}`);
        const modal = orderId === 'new' ? document.getElementById('newOrderModal') : document.getElementById(`editOrderModal_${orderId}`);
        if (!modal) {
            console.error(`Modal not found for orderId: ${orderId}`);
            window.c92.showToast('error', `Modal nem található: ${orderId}`);
            return;
        }

        // Initialize save button
        const saveButton = modal.querySelector('.save-order');
        if (saveButton) {
            const newSaveButton = saveButton.cloneNode(true);
            saveButton.replaceWith(newSaveButton);
            newSaveButton.addEventListener('click', () => saveOrder(orderId));
            console.log('Save button initialized for orderId:', orderId);
        } else {
            console.warn('Save button not found for orderId:', orderId);
        }

        // Initialize item row events
        const tbody = document.querySelector(`#items-tbody_${orderId}`);
        if (tbody) {
            tbody.querySelectorAll('.order-item-row').forEach(row => {
                initializeRowCalculations(row);
                initializeDescriptionToggle(row);
            });
            // Add item button
            const addItemButton = modal.querySelector('.add-item-row');
            if (addItemButton) {
                const newAddItemButton = addItemButton.cloneNode(true);
                addItemButton.replaceWith(newAddItemButton);
                newAddItemButton.addEventListener('click', () => addItemRow(orderId));
                console.log('Add item button initialized for orderId:', orderId);
            } else {
                console.warn('Add item button not found for orderId:', orderId);
            }
        }

        initializeDeleteButtons(orderId);
    }

    async function populateEditOrderModal(orderId) {
        try {
            const modal = document.getElementById(`editOrderModal_${orderId}`);
            if (!modal) {
                console.error(`Modal #editOrderModal_${orderId} not found`);
                window.c92.showToast('error', 'Modal nem található.');
                return;
            }

            const form = modal.querySelector(`#orderBaseInfoForm_${orderId}`);
            if (!form) {
                console.error(`Form #orderBaseInfoForm_${orderId} not found`);
                window.c92.showToast('error', 'Űrlap nem található.');
                return;
            }

            // Fetch order data
            const response = await fetch(`/api/orders/${orderId}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`,
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            });
            if (!response.ok) {
                throw new Error(`Failed to fetch order: ${response.status}`);
            }
            const order = await response.json();

            // Initialize form fields
            const setFieldValue = (selector, value) => {
                const element = form.querySelector(selector);
                if (element) {
                    element.value = value || '';
                } else {
                    console.warn(`Element ${selector} not found in #orderBaseInfoForm_${orderId}`);
                }
            };

            setFieldValue('[name="orderNumber"]', order.orderNumber);
            setFieldValue('[name="orderDate"]', order.orderDate ? order.orderDate.split('T')[0] : '');
            setFieldValue('[name="deadline"]', order.deadline ? order.deadline.split('T')[0] : '');
            setFieldValue('[name="status"]', order.status || 'Draft');
            setFieldValue('[name="salesPerson"]', order.salesPerson);
            setFieldValue('[name="deliveryDate"]', order.deliveryDate ? order.deliveryDate.split('T')[0] : '');
            setFieldValue('[name="companyName"]', order.companyName);
            setFieldValue('[name="subject"]', order.subject);
            setFieldValue('[name="paymentTerms"]', order.paymentTerms);
            setFieldValue('[name="shippingMethod"]', order.shippingMethod);
            setFieldValue('[name="orderType"]', order.orderType);
            setFieldValue('[name="referenceNumber"]', order.referenceNumber);
            setFieldValue('[name="discountPercentage"]', order.discountPercentage ? order.discountPercentage.toFixed(2) : '');
            setFieldValue('[name="discountAmount"]', order.discountAmount ? order.discountAmount.toFixed(2) : '');
            setFieldValue('[name="totalAmount"]', order.totalAmount ? order.totalAmount.toFixed(2) : 0);
            setFieldValue('[name="description"]', order.description);
            setFieldValue('[name="detailedDescription"]', order.detailedDescription);

            // Initialize TomSelect dropdowns
            const partnerSelect = form.querySelector('[name="PartnerId"]');
            const currencySelect = form.querySelector('[name="currencyId"]');
            const siteSelect = form.querySelector('[name="SiteId"]');
            const quoteSelect = form.querySelector('[name="quoteId"]');

            if (partnerSelect) {
                partnerSelect.dataset.selectedId = order.partnerId || '';
                partnerSelect.dataset.selectedText = order.partnerName || '';
                await window.c92.initializePartnerTomSelect(partnerSelect, orderId, 'order');
                console.log(`Partner TomSelect initialized for orderId: ${orderId}`);
                // Trigger site dropdown load if partner is selected
                if (partnerSelect.value && siteSelect && siteSelect.tomselect) {
                    siteSelect.tomselect.clear();
                    siteSelect.tomselect.clearOptions();
                    siteSelect.tomselect.load('');
                }
            } else {
                console.warn(`Partner select not found in #orderBaseInfoForm_${orderId}`);
                window.c92.showToast('error', 'Partner választó elem nem található.');
            }

            if (siteSelect) {
                siteSelect.dataset.selectedId = order.siteId || '';
                siteSelect.dataset.selectedText = order.siteName || '';
                await window.c92.initializeSiteTomSelect(siteSelect, orderId, 'order');
                console.log(`Site TomSelect initialized for orderId: ${orderId}`);
            } else {
                console.warn(`Site select not found in #orderBaseInfoForm_${orderId}`);
                window.c92.showToast('error', 'Telephely választó elem nem található.');
            }

            if (currencySelect) {
                currencySelect.dataset.selectedId = order.currencyId || '';
                currencySelect.dataset.selectedText = order.currencyName || '';
                await window.c92.initializeCurrencyTomSelect(currencySelect, 'order');
                console.log(`Currency TomSelect initialized for orderId: ${orderId}`);
            } else {
                console.warn(`Currency select not found in #orderBaseInfoForm_${orderId}`);
            }

            if (quoteSelect) {
                quoteSelect.dataset.selectedId = order.quoteId || '';
                quoteSelect.dataset.selectedText = order.quoteName || '';
                await window.c92.initializeQuoteTomSelect(quoteSelect, orderId, 'order');
                console.log(`Quote TomSelect initialized for orderId: ${orderId}`);
            } else {
                console.warn(`Quote select not found in #orderBaseInfoForm_${orderId}`);
            }

            // Initialize order items
            const productSelects = modal.querySelectorAll(`#items-tbody_${orderId} .tom-select-product`);
            for (const select of productSelects) {
                const row = select.closest('.order-item-row');
                if (row) {
                    const vatSelect = row.querySelector('.tom-select-vat');
                    if (vatSelect) {
                        await window.c92.initializeVatTomSelect(vatSelect, orderId, 'order');
                    }
                    initializeRowCalculations(row);
                    initializeDescriptionToggle(row);
                }
            }

            initializeEventListeners(orderId);
            window.calculateOrderTotals(orderId);
        } catch (error) {
            console.error(`Failed to populate edit order modal for orderId: ${orderId}`, error);
            window.c92.showToast('error', `Hiba a rendelés betöltése közben: ${error.message}`);
        }
    }

    // Attach edit modal handler
    document.querySelectorAll('[id^="editOrderModal_"]').forEach(modal => {
        const orderId = modal.id.replace('editOrderModal_', '');
        modal.addEventListener('shown.bs.modal', async function () {
            console.log(`Edit Order modal shown for orderId: ${orderId}`);
            await populateEditOrderModal(orderId);
        });

        modal.addEventListener('hidden.bs.modal', function () {
            const form = document.querySelector(`#orderBaseInfoForm_${orderId}`);
            if (form) form.reset();
            const tbody = document.querySelector(`#items-tbody_${orderId}`);
            if (tbody) tbody.querySelectorAll('.order-item-row, .description-row').forEach(row => row.remove());
            window.calculateOrderTotals(orderId);
            const tomSelects = document.querySelectorAll(`#orderBaseInfoForm_${orderId} .tom-select`);
            tomSelects.forEach(select => {
                if (select.tomselect) {
                    select.tomselect.destroy();
                    console.log('Destroyed TomSelect for:', select.getAttribute('name'));
                }
            });
        });
    });

    window.calculateTotalPrice = function (row, forceRecalculate = false) {
        try {
            const itemId = row.dataset.itemId;
            console.log('Calculating total price for row:', itemId);
            const quantityInput = row.querySelector('.quantity');
            const unitPriceInput = row.querySelector('.unit-price');
            const discountTypeSelect = row.querySelector('.discount-type-id');
            const netUnitPriceSpan = row.querySelector('.net-unit-price');
            const netTotalPriceSpan = row.querySelector('.net-total-price');
            const grossTotalPriceSpan = row.querySelector('.gross-total-price');
            const vatRateSelect = row.querySelector('.tom-select-vat');

            if (!quantityInput || !unitPriceInput || !discountTypeSelect || !netUnitPriceSpan || !netTotalPriceSpan || !grossTotalPriceSpan || !vatRateSelect) {
                console.error('Missing elements in row:', itemId);
                return;
            }

            if (forceRecalculate || row.dataset.initialized !== 'true') {
                const quantity = parseFloat(quantityInput.value) || 0;
                const unitPrice = parseFloat(unitPriceInput.value) || 0;
                const discountTypeId = parseInt(discountTypeSelect.value) || 1;
                const vatRate = parseFloat(vatRateSelect.dataset.selectedRate) || 0;

                let netUnitPrice = unitPrice;
                let discount = 0;

                if (discountTypeId === 5) { // Egyedi kedvezmény %
                    const discountInput = row.querySelector('.discount-value');
                    const discountPercentage = parseFloat(discountInput?.value) || 0;
                    discount = unitPrice * (discountPercentage / 100);
                    row.querySelector('.discount-amount').textContent = discountPercentage.toFixed(2);
                } else if (discountTypeId === 6) { // Egyedi kedvezmény Összeg
                    const discountInput = row.querySelector('.discount-value');
                    discount = parseFloat(discountInput?.value) || 0;
                    row.querySelector('.discount-amount').textContent = discount.toFixed(2);
                } else {
                    row.querySelector('.discount-amount').textContent = '0.00';
                }

                netUnitPrice = Math.max(unitPrice - discount, 0);
                const netTotal = quantity * netUnitPrice;
                const grossTotal = netTotal * (1 + vatRate / 100);

                netUnitPriceSpan.textContent = netUnitPrice.toFixed(2);
                netTotalPriceSpan.textContent = netTotal.toFixed(2);
                grossTotalPriceSpan.textContent = grossTotal.toFixed(2);
                row.dataset.initialized = 'true';
                console.log('Net unit price:', netUnitPrice.toFixed(2), 'Net total:', netTotal.toFixed(2), 'Gross total:', grossTotal.toFixed(2), 'for item:', itemId);
            }
        } catch (error) {
            console.error('Error calculating total price:', error);
        }
    };

    window.calculateOrderTotals = function (orderId) {
        try {
            const tbody = document.querySelector(`#items-tbody_${orderId}`);
            if (!tbody) {
                console.error('Tbody not found for orderId:', orderId);
                return;
            }

            const totalNetElement = tbody.querySelector('.order-total-net');
            const totalVatElement = tbody.querySelector('.order-vat-amount');
            const totalGrossElement = tbody.querySelector('.order-gross-amount');

            if (!totalNetElement || !totalVatElement || !totalGrossElement) {
                console.error('Missing order total elements for orderId:', orderId);
                return;
            }

            let totalNet = 0;
            let totalVat = 0;
            let totalGross = 0;

            const rows = tbody.querySelectorAll('.order-item-row');
            rows.forEach(row => {
                const netTotalPrice = parseFloat(row.querySelector('.net-total-price')?.textContent) || 0;
                const grossTotalPrice = parseFloat(row.querySelector('.gross-total-price')?.textContent) || 0;
                totalNet += netTotalPrice;
                totalGross += grossTotalPrice;
                totalVat += grossTotalPrice - netTotalPrice;
            });

            totalNetElement.textContent = totalNet.toFixed(2);
            totalVatElement.textContent = totalVat.toFixed(2);
            totalGrossElement.textContent = totalGross.toFixed(2);

            const form = tbody.closest('form');
            if (form) {
                form.querySelector('.order-total-net-input').value = totalNet.toFixed(2);
                form.querySelector('.order-vat-amount-input').value = totalVat.toFixed(2);
                form.querySelector('.order-gross-amount-input').value = totalGross.toFixed(2);
            }

            console.log('Order totals for orderId:', orderId, { totalNet, totalVat, totalGross });
        } catch (error) {
            console.error('Error calculating order totals:', error);
        }
    };

    window.updatePriceFields = function (selectElement, productId, products) {
        const row = selectElement.closest('.order-item-row');
        const product = products.find(p => p.productId == productId);
        if (product && row) {
            const unitPriceInput = row.querySelector('.unit-price');
            unitPriceInput.value = product.unitPrice || 0;
            window.calculateTotalPrice(row, true);
            console.log('Price fields updated for productId:', productId, 'unitPrice:', product.unitPrice);
        }
    };

    function initializeRowCalculations(row) {
        const inputs = row.querySelectorAll('.quantity, .unit-price, .discount-type-id, .tom-select-vat, .discount-value');
        console.log('Initializing row:', row.dataset.itemId, 'Inputs:', inputs.length);
        inputs.forEach(input => {
            input.addEventListener('input', () => {
                window.calculateTotalPrice(row, true);
                window.calculateOrderTotals(row.closest('tbody').id.replace('items-tbody_', ''));
            });
        });
        window.calculateTotalPrice(row);
    }

    function initializeDescriptionToggle(row) {
        const itemId = row.dataset.itemId;
        const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
        const editButton = row.querySelector('.edit-description');
        if (!descriptionRow || !editButton) {
            console.warn('Description row or edit button missing for item:', itemId);
            return;
        }
        const textarea = descriptionRow.querySelector('.item-description');
        const charCount = descriptionRow.querySelector('.char-count');
        textarea.addEventListener('input', () => {
            charCount.textContent = `${textarea.value.length}/200`;
        });
        editButton.addEventListener('click', () => {
            descriptionRow.style.display = descriptionRow.style.display === 'none' ? 'table-row' : 'none';
        });
    }

    function initializeDeleteButtons(orderId) {
        const tbody = document.querySelector(`#items-tbody_${orderId}`);
        if (!tbody) return;
        tbody.addEventListener('click', function (event) {
            if (event.target.closest('.remove-item-row')) {
                const row = event.target.closest('.order-item-row');
                const itemId = row.dataset.itemId;
                const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
                row.remove();
                if (descriptionRow) descriptionRow.remove();
                window.calculateOrderTotals(orderId);
            }
        });
    }

    async function addItemRow(orderId) {
        const tbody = document.querySelector(`#items-tbody_${orderId}`);
        if (!tbody) {
            console.error('Items tbody not found for orderId:', orderId);
            window.c92.showToast('error', 'Táblázat nem található.');
            return;
        }

        const newItemId = 'new_' + Date.now();
        console.log('Adding new item row for orderId:', orderId, 'NewItemId:', newItemId);

        const itemRow = document.createElement('tr');
        itemRow.className = 'order-item-row';
        itemRow.dataset.itemId = newItemId;
        itemRow.innerHTML = `
            <td>
                <select name="items[${newItemId}][productId]" id="tomselect-product-${newItemId}" class="form-select tom-select-product" autocomplete="off" required>
                    <option value="" disabled selected>-- Válasszon terméket --</option>
                </select>
            </td>
            <td>
                <input type="number" name="items[${newItemId}][quantity]" class="form-control form-control-sm quantity" value="1" min="0" step="1" required>
            </td>
            <td>
                <input type="number" name="items[${newItemId}][unitPrice]" class="form-control form-control-sm unit-price" value="0" min="0" step="0.01" required>
            </td>
            <td>
                <select name="items[${newItemId}][discountTypeId]" class="form-select form-control-sm discount-type-id">
                    <option value="1" selected>Nincs Kedvezmény</option>
                    <option value="3">Ügyfélár</option>
                    <option value="4">Mennyiségi kedvezmény</option>
                    <option value="5">Egyedi kedvezmény %</option>
                    <option value="6">Egyedi kedvezmény Összeg</option>
                </select>
            </td>
            <td>
                <input type="number" name="items[${newItemId}][discountValue]" class="form-control form-control-sm discount-value" value="0" min="0" step="0.01" style="display: none;">
                <span class="discount-amount">0.00</span>
            </td>
            <td>
                <span class="net-unit-price">0.00</span>
            </td>
            <td>
                <select name="items[${newItemId}][vatTypeId]" id="tomselect-vat-${newItemId}" class="form-select tom-select-vat" data-selected-id="1" data-selected-text="27%" data-selected-rate="27" autocomplete="off" required>
                    <option value="1" selected>27%</option>
                    <option value="" disabled>-- Válasszon ÁFA típust --</option>
                </select>
            </td>
            <td>
                <span class="net-total-price">0.00</span>
            </td>
            <td>
                <span class="gross-total-price">0.00</span>
            </td>
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
                    <textarea name="items[${newItemId}][description]" class="form-control form-control-sm item-description" maxlength="200" rows="2"></textarea>
                    <div class="form-text">Karakterek: <span class="char-count">0</span>/200</div>
                </div>
            </td>
        `;

        const orderTotalRow = tbody.querySelector('.order-total-row');
        if (orderTotalRow) {
            tbody.insertBefore(itemRow, orderTotalRow);
            tbody.insertBefore(descriptionRow, orderTotalRow);
        } else {
            tbody.appendChild(itemRow);
            tbody.appendChild(descriptionRow);
        }

        // Initialize product select
        const productSelect = itemRow.querySelector('.tom-select-product');
        if (productSelect) {
            if (typeof window.initializeProductTomSelect !== 'function') {
                console.error('initializeProductTomSelect is not defined');
                window.c92.showToast('error', 'Termékválasztó inicializálási függvény hiányzik.');
            } else {
                try {
                    const partnerSelect = document.querySelector(`#partner-select_${orderId}`);
                    const partnerId = partnerSelect?.value ? parseInt(partnerSelect.value) : null;
                    await window.initializeProductTomSelect(productSelect, {
                        partnerId: partnerId,
                        quoteDate: new Date().toISOString(),
                        quantity: 1
                    });
                    console.log('Product select initialized for item:', newItemId);
                } catch (err) {
                    console.error('Failed to initialize product select:', err);
                    window.c92.showToast('error', 'Hiba a termékek betöltése közben: ' + err.message);
                }
            }
        }

        // Initialize VAT select
        const vatSelect = itemRow.querySelector('.tom-select-vat');
        if (vatSelect) {
            if (typeof window.c92.initializeVatTomSelect !== 'function') {
                console.error('initializeVatTomSelect is not defined');
                window.c92.showToast('error', 'ÁFA választó inicializálási függvény hiányzik.');
            } else {
                try {
                    await window.c92.initializeVatTomSelect(vatSelect, orderId, 'order');
                    console.log('VAT select initialized for item:', newItemId);
                } catch (err) {
                    console.error('Failed to initialize VAT select:', err);
                    window.c92.showToast('error', 'Hiba az ÁFA típusok betöltése közben: ' + err.message);
                }
            }
        }

        // Show/hide discount input based on discount type
        const discountTypeSelect = itemRow.querySelector('.discount-type-id');
        const discountInput = itemRow.querySelector('.discount-value');
        const discountSpan = itemRow.querySelector('.discount-amount');
        discountTypeSelect.addEventListener('change', () => {
            const discountTypeId = parseInt(discountTypeSelect.value) || 1;
            discountInput.style.display = (discountTypeId === 5 || discountTypeId === 6) ? 'block' : 'none';
            discountInput.value = '0';
            discountSpan.textContent = '0.00';
            window.calculateTotalPrice(itemRow, true);
            window.calculateOrderTotals(orderId);
        });

        // Initialize row calculations and description toggle
        initializeRowCalculations(itemRow);
        initializeDescriptionToggle(itemRow);
        window.calculateOrderTotals(orderId);
    }

    async function saveOrder(orderId) {
        const baseInfoForm = document.querySelector(`#orderBaseInfoForm_${orderId}`);
        const itemsForm = document.querySelector(`#orderItemsForm_${orderId}`);
        if (!baseInfoForm || !itemsForm) {
            console.error('Forms not found for orderId:', orderId);
            window.c92.showToast('error', 'Hiba: Űrlapok nem találhatók.');
            return;
        }

        const baseFormData = new FormData(baseInfoForm);
        const orderData = {
            partnerId: parseInt(baseFormData.get('PartnerId')) || 0,
            currencyId: parseInt(baseFormData.get('currencyId')) || 0,
            siteId: parseInt(baseFormData.get('SiteId')) || null,
            quoteId: parseInt(baseFormData.get('quoteId')) || null,
            orderNumber: baseFormData.get('orderNumber') || null,
            orderDate: baseFormData.get('orderDate') || new Date().toISOString().split('T')[0],
            deadline: baseFormData.get('deadline') || null,
            deliveryDate: baseFormData.get('deliveryDate') || null,
            referenceNumber: baseFormData.get('referenceNumber') || null,
            orderType: baseFormData.get('orderType') || null,
            companyName: baseFormData.get('companyName') || null,
            totalAmount: parseFloat(baseFormData.get('totalAmount')) || 0,
            discountPercentage: parseFloat(baseFormData.get('discountPercentage')) || null,
            discountAmount: parseFloat(baseFormData.get('discountAmount')) || null,
            paymentTerms: baseFormData.get('paymentTerms') || null,
            shippingMethod: baseFormData.get('shippingMethod') || null,
            salesPerson: baseFormData.get('salesPerson') || null,
            subject: baseFormData.get('subject') || null,
            description: baseFormData.get('description') || null,
            detailedDescription: baseFormData.get('detailedDescription') || null,
            status: baseFormData.get('status') || 'Draft',
            createdBy: baseFormData.get('createdBy') || 'System',
            createdDate: baseFormData.get('createdDate') || new Date().toISOString(),
            modifiedBy: baseFormData.get('modifiedBy') || 'System',
            modifiedDate: baseFormData.get('modifiedDate') || new Date().toISOString(),
            orderItems: []
        };

        const itemRows = document.querySelectorAll(`#items-tbody_${orderId} .order-item-row`);
        itemRows.forEach(row => {
            const itemId = row.dataset.itemId;
            const productIdInput = row.querySelector(`select[name="items[${itemId}][productId]"]`);
            const quantityInput = row.querySelector(`input[name="items[${itemId}][quantity]"]`);
            const unitPriceInput = row.querySelector(`input[name="items[${itemId}][unitPrice]"]`);
            const discountTypeSelect = row.querySelector(`select[name="items[${itemId}][discountTypeId]"]`);
            const discountInput = row.querySelector(`input[name="items[${itemId}][discountValue]"]`);
            const descriptionInput = row.querySelector(`textarea[name="items[${itemId}][description]"]`);
            const vatTypeIdInput = row.querySelector(`select[name="items[${itemId}][vatTypeId]"]`);

            const discountTypeId = parseInt(discountTypeSelect?.value) || 1;
            let discountPercentage = null;
            let discountAmount = null;
            if (discountTypeId === 5) { // Egyedi kedvezmény %
                discountPercentage = parseFloat(discountInput?.value) || 0;
            } else if (discountTypeId === 6) { // Egyedi kedvezmény Összeg
                discountAmount = parseFloat(discountInput?.value) || 0;
            }

            orderData.orderItems.push({
                orderItemId: itemId.startsWith('new_') ? null : parseInt(itemId),
                productId: parseInt(productIdInput?.value) || 0,
                quantity: parseFloat(quantityInput?.value) || 0,
                unitPrice: parseFloat(unitPriceInput?.value) || 0,
                discountPercentage: discountPercentage,
                discountAmount: discountAmount,
                description: descriptionInput?.value || null,
                vatTypeId: parseInt(vatTypeIdInput?.value) || null
            });
        });

        let errors = [];
        if (!orderData.partnerId) errors.push('Partner kiválasztása kötelező.');
        if (!orderData.currencyId) errors.push('Pénznem kiválasztása kötelező.');
        if (!orderData.siteId) errors.push('Telephely kiválasztása kötelező.');
        if (orderData.orderItems.length === 0) errors.push('Legalább egy tétel hozzáadása kötelező.');
        else {
            orderData.orderItems.forEach((item, index) => {
                if (!item.productId) errors.push(`Tétel ${index + 1}: Termék kiválasztása kötelező.`);
                if (item.quantity <= 0) errors.push(`Tétel ${index + 1}: Mennyiség nagyobb kell legyen, mint 0.`);
                if (item.unitPrice <= 0) errors.push(`Tétel ${index + 1}: Egységár nagyobb kell legyen, mint 0.`);
            });
        }

        if (errors.length > 0) {
            window.c92.showToast('error', errors.join(' '));
            return;
        }

        const isNewOrder = orderId === 'new';
        const url = isNewOrder ? '/api/orders' : `/api/orders/${orderId}`;
        const method = isNewOrder ? 'POST' : 'PUT';

        try {
            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`,
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(orderData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Failed to save order: ${response.status}`);
            }

            const data = await response.json();
            window.c92.showToast('success', `Rendelés #${data.orderNumber || orderId} sikeresen mentve!`);
            bootstrap.Modal.getInstance(document.getElementById(`editOrderModal_${orderId}`) || document.getElementById('newOrderModal')).hide();
            if (!isNewOrder) {
                window.location.reload();
            }
        } catch (error) {
            console.error('Save order error:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    }

    // Filter/Sort Dropdown Logic
    const filterItems = document.querySelectorAll('.dropdown-menu [data-filter]');
    console.log('Found filter items:', filterItems.length);

    filterItems.forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            const filter = this.getAttribute('data-filter');
            const sort = this.getAttribute('data-sort');
            console.log('Clicked filter/sort item:', { filter, sort });

            let form = document.querySelector('form[asp-page="./Index"]') ||
                document.querySelector('form[action="/CRM/Orders"]') ||
                document.querySelector('form[action="/CRM/Orders/Index"]');

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
});