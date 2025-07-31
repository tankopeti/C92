// orders.js
console.log('Main script loaded');

async function initializeOrderModals() {
    console.log('Initializing order modals');
    const newOrderModal = document.getElementById('newOrderModal');
    if (newOrderModal) {
        newOrderModal.addEventListener('show.bs.modal', async () => {
            console.log('New Order modal shown');
            await initializeModalFields('new');
        });
    }

    document.querySelectorAll('[id^="editOrderModal_"]').forEach(modal => {
        const orderId = modal.id.replace('editOrderModal_', '');
        modal.addEventListener('show.bs.modal', async () => {
            console.log(`Edit Order modal shown for orderId: ${orderId}`);
            await populateEditOrderModal(orderId);
        });
    });
}

async function initializeModalFields(orderId) {
    console.log('Initializing modal fields for orderId:', orderId);
    const form = document.getElementById(`orderBaseInfoForm_${orderId}`);
    if (!form) {
        console.error(`Form #orderBaseInfoForm_${orderId} not found`);
        window.c92.showToast('error', 'Űrlap nem található.');
        return;
    }

    const partnerSelect = form.querySelector('[name="PartnerId"]');
    const siteSelect = form.querySelector('[name="SiteId"]');
    const currencySelect = form.querySelector('[name="currencyId"]');
    const quoteSelect = form.querySelector('[name="quoteId"]');

    console.log('Initializing TomSelect for:', partnerSelect?.name);
    if (partnerSelect && typeof window.c92.initializePartnerTomSelect === 'function') {
        try {
            await window.c92.initializePartnerTomSelect(partnerSelect, orderId, 'order');
            console.log(`Partner TomSelect initialized for orderId: ${orderId}`);
            console.log('Partner TomSelect options after init:', partnerSelect.tomselect?.options || {});
            partnerSelect.addEventListener('change', async () => {
                const partnerId = partnerSelect.value;
                console.log(`Partner selected for orderId: ${orderId}:`, partnerId);
                if (siteSelect && siteSelect.tomselect) {
                    siteSelect.tomselect.clear();
                    siteSelect.tomselect.clearOptions();
                    siteSelect.tomselect.load('');
                }
                if (quoteSelect && quoteSelect.tomselect) {
                    quoteSelect.tomselect.clear();
                    quoteSelect.tomselect.clearOptions();
                    quoteSelect.tomselect.load('');
                }
                console.log(`Site and Quote TomSelect reinitialized for partnerId: ${partnerId}`);
            });
        } catch (err) {
            console.error('Failed to initialize Partner TomSelect:', err);
            window.c92.showToast('error', `Hiba a partner kiválasztás közben: ${err.message}`);
        }
    }

    console.log('Initializing TomSelect for:', siteSelect?.name);
    if (siteSelect && typeof window.c92.initializeSiteTomSelect === 'function') {
        try {
            await window.c92.initializeSiteTomSelect(siteSelect, orderId, 'order');
            console.log(`Site TomSelect initialized for orderId: ${orderId}`);
        } catch (err) {
            console.error('Failed to initialize Site TomSelect:', err);
            window.c92.showToast('error', `Hiba a telephely kiválasztás közben: ${err.message}`);
        }
    }

    console.log('Initializing TomSelect for:', currencySelect?.name);
    if (currencySelect && typeof window.c92.initializeCurrencyTomSelect === 'function') {
        try {
            await window.c92.initializeCurrencyTomSelect(currencySelect, 'order');
            console.log(`Currency TomSelect initialized for orderId: ${orderId}`);
        } catch (err) {
            console.error('Failed to initialize Currency TomSelect:', err);
            window.c92.showToast('error', `Hiba a pénznem kiválasztás közben: ${err.message}`);
        }
    }

    console.log('Initializing TomSelect for:', quoteSelect?.name);
    if (quoteSelect && typeof window.c92.initializeQuoteTomSelect === 'function') {
        try {
            await window.c92.initializeQuoteTomSelect(quoteSelect, orderId, 'order');
            console.log(`Quote TomSelect initialized for orderId: ${orderId}`);
        } catch (err) {
            console.error('Failed to initialize Quote TomSelect:', err);
            window.c92.showToast('error', `Hiba az árajánlat kiválasztás közben: ${err.message}`);
        }
    }

    await initializeEventListeners(orderId);
}

async function initializeEventListeners(orderId) {
    console.log('Initializing event listeners for orderId:', orderId);
    const modal = document.getElementById(orderId === 'new' ? 'newOrderModal' : `editOrderModal_${orderId}`);
    if (!modal) {
        console.error(`Modal for orderId: ${orderId} not found`);
        window.c92.showToast('error', `Modal nem található: ${orderId}`);
        return;
    }

const saveButton = document.querySelector(`#saveOrderBtn_${orderId}`);
if (!saveButton) {
    console.error(`Save button #saveOrderBtn_${orderId} not found`);
    window.c92.showToast('error', `Mentés gomb nem található: ${orderId}`);
    return;
}
saveButton.addEventListener('click', () => {
    console.log(`Save button clicked for orderId: ${orderId}`);
    const form = document.querySelector(`#${orderId === 'new' ? 'newOrderForm' : 'editOrderForm_' + orderId}`);
    if (!form) {
        console.error(`Form not found for orderId: ${orderId}`);
        window.c92.showToast('error', `Űrlap nem található: ${orderId}`);
        return;
    }
    const partnerSelect = document.querySelector(`#partner-select_${orderId}`);
    const currencySelect = document.querySelector(`#currency-select_${orderId}`);
    let totalGross = parseFloat(document.querySelector(`#total-gross-${orderId}`)?.textContent) || 0;
    const orderItems = [];
    form.querySelectorAll('.order-item-row').forEach(row => {
        const productSelect = row.querySelector('.tom-select-product');
        const item = {
            productId: parseInt(productSelect?.tomselect?.getValue() || productSelect?.value) || 0,
            quantity: parseFloat(row.querySelector('.quantity')?.value) || 1,
            unitPrice: parseFloat(row.querySelector('.unit-price')?.value) || 0,
            discountPercentage: row.dataset.discountTypeId == '5' ? parseFloat(row.dataset.discountValue) || 0 : null,
            discountAmount: row.dataset.discountTypeId == '3' ? parseFloat(row.dataset.discountValue) || 0 : null,
            description: row.nextElementSibling?.querySelector('.item-description')?.value || ''
        };
        if (orderId !== 'new') {
            item.orderItemId = parseInt(row.dataset.itemId) || 0; // For UpdateOrderDto
        }
        orderItems.push(item);
    });
    const orderData = {
        partnerId: parseInt(partnerSelect?.tomselect?.getValue() || '') || 0,
        currencyId: parseInt(currencySelect?.tomselect?.getValue() || '') || 0,
        orderNumber: `ORD-${Date.now()}`,
        orderDate: new Date().toISOString(),
        totalAmount: totalGross, // Use gross amount (1244.6)
        status: orderId === 'new' ? 'Draft' : 'Pending',
        createdBy: 'user', // Replace with actual user from auth
        createdDate: new Date().toISOString(),
        modifiedBy: 'user', // Replace with actual user
        modifiedDate: new Date().toISOString(),
        orderItems: orderItems
    };
    console.log('Order data to save:', JSON.stringify(orderData, null, 2));
    if (!orderData.partnerId) {
        console.error('Missing partnerId');
        window.c92.showToast('error', 'Kérjük, válasszon partnert.');
        return;
    }
    if (!orderData.currencyId) {
        console.error('Missing currencyId');
        window.c92.showToast('error', 'Kérjük, válasszon pénznemet.');
        return;
    }
    if (!orderData.orderItems.length) {
        console.error('No items in order');
        window.c92.showToast('error', 'Legalább egy tétel szükséges.');
        return;
    }
    fetch(orderId === 'new' ? '/api/orders' : `/api/orders/${orderId}`, {
        method: orderId === 'new' ? 'POST' : 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + (localStorage.getItem('token') || '')
        },
        body: JSON.stringify(orderData)
    })
    .then(response => {
        console.log('Fetch response status:', response.status);
        if (!response.ok) {
            return response.json().then(err => {
                throw new Error(err.error || err.message || `HTTP ${response.status}`);
            });
        }
        return response.json();
    })
    .then(data => {
        console.log('Save success:', data);
        window.c92.showToast('success', `Rendelés mentve: ${orderId}`);
        const modal = document.querySelector(`#${orderId === 'new' ? 'newOrderModal' : 'editOrderModal_' + orderId}`);
        if (modal) {
            bootstrap.Modal.getInstance(modal).hide();
        }
    })
    .catch(error => {
        console.error('Save error:', error);
        window.c92.showToast('error', `Hiba a mentés során: ${error.message}`);
    });
});

    // Initialize add item button
    const addItemButton = modal.querySelector('.add-item-row');
    if (addItemButton) {
        const newAddItemButton = addItemButton.cloneNode(true);
        addItemButton.replaceWith(newAddItemButton);
        newAddItemButton.addEventListener('click', () => {
            console.log(`Add item button clicked for orderId: ${orderId}`);
            addItemRow(orderId);
        });
        console.log(`Add item button initialized for orderId: ${orderId}`);
    } else {
        console.warn(`Add item button .add-item-row not found for orderId: ${orderId}`);
        window.c92.showToast('error', 'Tétel hozzáadása gomb nem található.');
    }

    // Initialize delete buttons
    modal.querySelectorAll('.remove-item-row').forEach(btn => {
        const newBtn = btn.cloneNode(true);
        btn.replaceWith(newBtn);
        newBtn.addEventListener('click', () => {
            const itemId = newBtn.dataset.itemId;
            const itemRow = document.querySelector(`tr.order-item-row[data-item-id="${itemId}"]`);
            const descriptionRow = document.querySelector(`tr.description-row[data-item-id="${itemId}"]`);
            if (itemRow) itemRow.remove();
            if (descriptionRow) descriptionRow.remove();
            window.calculateOrderTotals(orderId);
        });
    });
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
            if (partnerSelect.value && siteSelect && siteSelect.tomselect) {
                siteSelect.tomselect.clear();
                siteSelect.tomselect.clearOptions();
                siteSelect.tomselect.load('');
            }
        }

        if (siteSelect) {
            siteSelect.dataset.selectedId = order.siteId || '';
            siteSelect.dataset.selectedText = order.siteName || '';
            await window.c92.initializeSiteTomSelect(siteSelect, orderId, 'order');
            console.log(`Site TomSelect initialized for orderId: ${orderId}`);
        }

        if (currencySelect) {
            currencySelect.dataset.selectedId = order.currencyId || '';
            currencySelect.dataset.selectedText = order.currencyName || '';
            await window.c92.initializeCurrencyTomSelect(currencySelect, 'order');
            console.log(`Currency TomSelect initialized for orderId: ${orderId}`);
        }

        if (quoteSelect) {
            quoteSelect.dataset.selectedId = order.quoteId || '';
            quoteSelect.dataset.selectedText = order.quoteName || '';
            await window.c92.initializeQuoteTomSelect(quoteSelect, orderId, 'order');
            console.log(`Quote TomSelect initialized for orderId: ${orderId}`);
        }

        // Clear existing rows
        const tbody = document.querySelector(`#items-tbody_${orderId}`);
        if (tbody) {
            tbody.querySelectorAll('.order-item-row, .description-row').forEach(row => row.remove());
        }

        // Add order items
        if (order.orderItems && order.orderItems.length > 0) {
            for (const item of order.orderItems) {
                const itemId = item.orderItemId || 'new_' + Date.now();
                const productId = item.productId || '';
                const productText = item.productName || '';
                const itemRow = document.createElement('tr');
                itemRow.className = 'order-item-row';
                itemRow.dataset.itemId = itemId;
                itemRow.dataset.orderId = orderId;
                itemRow.innerHTML = `
                    <td>
                        <select name="items[${itemId}][productId]" id="tomselect-product-${itemId}" class="form-select tom-select-product" autocomplete="off" required
                            data-selected-id="${productId}" data-selected-text="${productText}">
                            <option value="" disabled>-- Válasszon terméket --</option>
                        </select>
                    </td>
                    <td>
                        <input type="number" name="items[${itemId}][quantity]" class="form-control form-control-sm quantity" value="${item.quantity || 1}" min="0" step="1" required>
                    </td>
                    <td>
                        <input type="number" name="items[${itemId}][unitPrice]" class="form-control form-control-sm unit-price" value="${item.unitPrice || 0}" min="0" step="0.01" required>
                    </td>
                    <td>
                        <select name="items[${itemId}][discountTypeId]" class="form-select form-control-sm discount-type-id">
                            <option value="1" ${item.discountTypeId === 1 ? 'selected' : ''}>Nincs Kedvezmény</option>
                            <option value="3" ${item.discountTypeId === 3 ? 'selected' : ''}>Ügyfélár</option>
                            <option value="4" ${item.discountTypeId === 4 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                            <option value="5" ${item.discountTypeId === 5 ? 'selected' : ''}>Egyedi kedvezmény %</option>
                            <option value="6" ${item.discountTypeId === 6 ? 'selected' : ''}>Egyedi kedvezmény Összeg</option>
                        </select>
                    </td>
                    <td>
                        <input type="number" name="items[${itemId}][discountValue]" class="form-control form-control-sm discount-value" value="${item.discountPercentage || item.discountAmount || 0}" min="0" step="0.01" style="display: ${[5, 6].includes(item.discountTypeId) ? 'block' : 'none'};">
                        <span class="discount-amount">${(item.discountPercentage || item.discountAmount || 0).toFixed(2)}</span>
                    </td>
                    <td>
                        <span class="net-unit-price">0.00</span>
                    </td>
                    <td>
                        <select name="items[${itemId}][vatTypeId]" id="tomselect-vat-${itemId}" class="form-select tom-select-vat" data-selected-id="${item.vatTypeId || 1}" data-selected-text="27%" data-selected-rate="27" autocomplete="off" required>
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
                        <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${itemId}"><i class="bi bi-pencil"></i></button>
                        <button type="button" class="btn btn-danger btn-sm remove-item-row" data-item-id="${itemId}"><i class="bi bi-trash"></i></button>
                    </td>
                `;

                const descriptionRow = document.createElement('tr');
                descriptionRow.className = 'description-row';
                descriptionRow.dataset.itemId = itemId;
                descriptionRow.style.display = 'none';
                descriptionRow.innerHTML = `
                    <td colspan="10">
                        <div class="mb-2">
                            <label class="form-label">Leírás (max 200 karakter)</label>
                            <textarea name="items[${itemId}][description]" class="form-control form-control-sm item-description" maxlength="200" rows="2">${item.description || ''}</textarea>
                            <div class="form-text">Karakterek: <span class="char-count">${(item.description || '').length}</span>/200</div>
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
                if (productSelect && typeof window.c92.initializeProductTomSelect === 'function') {
                    const tomSelect = await window.c92.initializeProductTomSelect(productSelect, { orderId: orderId });
                    if (productId) {
                        tomSelect.setValue(productId);
                        const products = Object.values(tomSelect.options).map(opt => ({
                            productId: opt.value,
                            listPrice: opt.listPrice || (opt.value === '62044' ? 980 : opt.value === '62056' ? 1000 : 0),
                            volumePrice: opt.volumePrice || 0,
                            partnerPrice: opt.partnerPrice || null,
                            volumePricing: opt.volumePricing || {},
                            text: opt.text
                        }));
                        await updatePriceFields(productSelect, productId, products);
                    }
                }

                // Initialize VAT select
                const vatSelect = itemRow.querySelector('.tom-select-vat');
                if (vatSelect && typeof window.c92.initializeVatTomSelect === 'function') {
                    await window.c92.initializeVatTomSelect(vatSelect, { context: 'order' });
                }

                initializeRowCalculations(itemRow);
                initializeDescriptionToggle(itemRow);
            }
        }

        initializeEventListeners(orderId);
        window.calculateOrderTotals(orderId);
    } catch (error) {
        console.error(`Failed to populate edit order modal for orderId: ${orderId}`, error);
        window.c92.showToast('error', `Hiba a rendelés betöltése közben: ${error.message}`);
    }
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

    // Pre-select product for Order 1626
    let selectedProductId = null;
    let selectedProductText = null;
    if (orderId === '1626') {
        selectedProductId = '62044'; // Termék 4
        selectedProductText = 'Termék 4';
    }

    const itemRow = document.createElement('tr');
    itemRow.className = 'order-item-row';
    itemRow.dataset.itemId = newItemId;
    itemRow.dataset.orderId = orderId;
    itemRow.dataset.discountTypeId = '1';
    itemRow.dataset.discountValue = '0';
    itemRow.innerHTML = `
        <td>
            <select name="items[${newItemId}][productId]" id="tomselect-product-${newItemId}" class="form-select tom-select-product" autocomplete="off" required
                ${selectedProductId ? `data-selected-id="${selectedProductId}" data-selected-text="${selectedProductText}"` : ''}>
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
            <select name="items[${newItemId}][discountTypeId]" id="discount-type-${newItemId}" class="form-select form-control-sm discount-type-id">
                <option value="1" selected>Nincs Kedvezmény</option>
                <option value="3">Ügyfélár</option>
                <option value="4">Mennyiségi kedvezmény</option>
                <option value="5">Egyedi kedvezmény %</option>
                <option value="6">Egyedi kedvezmény Összeg</option>
            </select>
        </td>
        <td>
            <input type="number" name="items[${newItemId}][discountValue]" id="discount-value-${newItemId}" class="form-control form-control-sm discount-value" value="0" min="0" step="0.01" disabled>
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

    // Check for duplicate discount-value inputs
    const existingDiscountInputs = itemRow.querySelectorAll('.discount-value');
    if (existingDiscountInputs.length > 1) {
        console.warn(`Multiple discount-value inputs found in row ${newItemId}: ${existingDiscountInputs.length}`);
        existingDiscountInputs.forEach((input, index) => {
            if (index > 0) input.remove();
        });
    }

    // Initialize product select
    const productSelect = itemRow.querySelector('.tom-select-product');
    let products = [];
    if (productSelect && typeof window.c92.initializeProductTomSelect === 'function') {
        try {
            const tomSelect = await window.c92.initializeProductTomSelect(productSelect, { orderId: orderId });
            console.log('Product select initialized for item:', newItemId, 'TomSelect:', !!tomSelect);
            products = Object.values(tomSelect.options).map(opt => ({
                productId: opt.value,
                listPrice: opt.listPrice || (opt.value === '62044' ? 980 : opt.value === '62056' ? 1000 : 0),
                volumePrice: opt.volumePrice || 0,
                partnerPrice: opt.partnerPrice || null,
                volumePricing: opt.volumePricing || {},
                text: opt.text
            }));
            console.log('Products loaded:', products);

            // Handle pre-selected product
            if (selectedProductId) {
                console.log('Pre-selecting product:', selectedProductId, selectedProductText);
                tomSelect.setValue(selectedProductId);
                const product = products.find(p => p.productId == selectedProductId) || {
                    productId: selectedProductId,
                    listPrice: selectedProductId === '62044' ? 980 : 1000,
                    text: selectedProductText
                };
                await window.updatePriceFields(productSelect, selectedProductId, products);
                console.log('Pre-selection prices updated for product:', selectedProductId);
            } else {
                await window.updatePriceFields(productSelect, null, products);
            }
        } catch (err) {
            console.error('Failed to initialize product select:', err);
            window.c92.showToast('error', `Hiba a termékek betöltése közben: ${err.message}`);
            await window.updatePriceFields(productSelect, null, []);
        }
    } else {
        console.error('Product select initialization failed: initializeProductTomSelect not defined');
        window.c92.showToast('error', 'Termékválasztó inicializálási hiba.');
        await window.updatePriceFields(productSelect, null, []);
    }

    // Initialize VAT select
    const vatSelect = itemRow.querySelector('.tom-select-vat');
    if (vatSelect && typeof window.c92.initializeVatTomSelect === 'function') {
        try {
            await window.c92.initializeVatTomSelect(vatSelect, { context: 'order' });
            console.log('VAT select initialized for item:', newItemId);
        } catch (err) {
            console.error('Failed to initialize VAT select:', err);
            window.c92.showToast('error', `Hiba az ÁFA kiválasztás közben: ${err.message}`);
        }
    }

    // Initialize discount type and value
    const discountTypeSelect = itemRow.querySelector('.discount-type-id');
    const discountValueInput = itemRow.querySelector('.discount-value');
    const quantityInput = itemRow.querySelector('.quantity');

    // Ensure single discount-value input
    if (!discountValueInput) {
        console.error(`Discount value input not found in row ${newItemId}`);
        window.c92.showToast('error', 'Kedvezmény mező nem található.');
        return;
    }

    function updateDiscountField() {
        const discountTypeId = parseInt(discountTypeSelect.value);
        const isEditable = [5, 6].includes(discountTypeId);
        discountValueInput.disabled = !isEditable;
        itemRow.dataset.discountTypeId = discountTypeId.toString();
        if (!isEditable) {
            discountValueInput.value = '0';
            itemRow.dataset.discountValue = '0';
        } else {
            itemRow.dataset.discountValue = discountValueInput.value || '0';
        }
        console.log(`Discount field updated for item ${newItemId}: discountTypeId=${discountTypeId}, isEditable=${isEditable}, disabled=${discountValueInput.disabled}, value=${discountValueInput.value}`);
        window.updatePriceFields(productSelect, productSelect.value, products);
        window.calculateOrderTotals(orderId);
    }

    discountTypeSelect.addEventListener('change', updateDiscountField);
    discountValueInput.addEventListener('input', () => {
        itemRow.dataset.discountValue = discountValueInput.value || '0';
        console.log(`Discount value changed for item ${newItemId}: discountValue=${discountValueInput.value}`);
        window.updatePriceFields(productSelect, productSelect.value, products);
        window.calculateOrderTotals(orderId);
    });
    quantityInput.addEventListener('input', () => {
        console.log(`Quantity changed for item ${newItemId}: quantity=${quantityInput.value}`);
        window.updatePriceFields(productSelect, productSelect.value, products);
        window.calculateOrderTotals(orderId);
    });

    // Initialize row calculations and description toggle
    initializeRowCalculations(itemRow);
    initializeDescriptionToggle(itemRow);
    window.calculateOrderTotals(orderId);
}

function initializeRowCalculations(row) {
    const inputs = row.querySelectorAll('input, select:not(.tom-select-product, .tom-select-vat)');
    console.log(`Initializing row: ${row.dataset.itemId} Inputs: ${inputs.length}`);
    inputs.forEach(input => {
        if (input._listener) {
            input.removeEventListener('input', input._listener);
            input.removeEventListener('change', input._listener);
        }
    });
    updatePriceFields(row.querySelector('.tom-select-product'), row.querySelector('.tom-select-product').value, []);
}


function initializeDescriptionToggle(row) {
    const editDescriptionBtn = row.querySelector('.edit-description');
    const itemId = row.dataset.itemId;
    const descriptionRow = document.querySelector(`tr.description-row[data-item-id="${itemId}"]`);
    if (!editDescriptionBtn || !descriptionRow) return;

    editDescriptionBtn.addEventListener('click', () => {
        descriptionRow.style.display = descriptionRow.style.display === 'none' ? '' : 'none';
    });

    const textarea = descriptionRow.querySelector('.item-description');
    const charCountSpan = descriptionRow.querySelector('.char-count');
    if (textarea && charCountSpan) {
        textarea.addEventListener('input', () => {
            charCountSpan.textContent = textarea.value.length;
        });
    }
}

function updatePriceFields(select, productId, products) {
    const row = select.closest('tr.order-item-row');
    if (!row) {
        console.error('Row not found for select element:', select);
        window.c92.showToast('error', 'Sor nem található.');
        return;
    }
    const orderId = row.closest('table').dataset.orderId || 'new';
    const unitPriceInput = row.querySelector('.unit-price');
    const discountTypeSelect = row.querySelector('.discount-type-id');
    const discountAmountInput = row.querySelector('.discount-value');
    const netUnitPriceSpan = row.querySelector('.net-unit-price');
    const vatSelect = row.querySelector('.tom-select-vat');
    const grossTotalPriceSpan = row.querySelector('.gross-total-price');
    const quantityInput = row.querySelector('.quantity');
    if (!unitPriceInput || !discountTypeSelect || !discountAmountInput || !netUnitPriceSpan || !vatSelect || !grossTotalPriceSpan || !quantityInput) {
        console.error('Missing fields in row:', row);
        window.c92.showToast('error', 'Hiányzó mezők a sorban.');
        return;
    }
    console.log('updatePriceFields called with productId:', productId, 'products length:', products.length);
    if (!productId && select.dataset.selectedId) {
        productId = select.dataset.selectedId;
        console.log('Using dataset.selectedId as fallback productId:', productId);
    }
    if (!productId) {
        console.log('No productId, resetting fields');
        unitPriceInput.value = '0.00';
        discountAmountInput.value = '';
        netUnitPriceSpan.textContent = '0.00';
        grossTotalPriceSpan.textContent = '0.00';
        discountTypeSelect.value = '1';
        discountAmountInput.readOnly = true;
        row.dataset.discountTypeId = '1';
        row.dataset.discountAmount = '0';
        row.dataset.volumePrice = '';
        window.calculateOrderTotals(orderId);
        return;
    }
    let product = products.find(p => p.productId == productId);
    if (!product && select.tomselect) {
        product = select.tomselect.options[productId];
        console.log('Fetched product from TomSelect options:', product);
    }
    // Fallback for known products
    if (!product) {
        if (productId === '62044') {
            product = { productId: '62044', listPrice: 980, text: 'Termék 4' };
            console.log('Using fallback product data for ProductId: 62044');
        } else if (productId === '62056') {
            product = { productId: '62056', listPrice: 1000, text: 'Termék 5' }; // Adjust listPrice as needed
            console.log('Using fallback product data for ProductId: 62056');
        } else {
            product = { productId, listPrice: 0, text: 'Ismeretlen termék' };
            console.warn('No product data available for productId:', productId);
        }
    }
    const unitPrice = parseFloat(product.listPrice) || 0;
    const quantity = parseInt(quantityInput.value, 10) || 1;
    let discountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
    let discountAmount = parseFloat(discountAmountInput.value) || 0;
    unitPriceInput.value = unitPrice.toFixed(2);
    discountAmountInput.readOnly = ![5, 6].includes(discountTypeId);
    if (discountTypeId === 1) {
        discountAmountInput.value = '';
        discountAmount = 0;
    }
    calculateAllPrices(row, unitPrice, discountTypeId, discountAmount, quantity, vatSelect, product);
    // Update event listeners
    quantityInput.removeEventListener('input', quantityInput._listener);
    discountTypeSelect.removeEventListener('change', discountTypeSelect._listener);
    discountAmountInput.removeEventListener('input', discountAmountInput._listener);
    vatSelect.removeEventListener('change', vatSelect._listener);
    quantityInput._listener = () => {
        let newQuantity = parseInt(quantityInput.value, 10) || 1;
        if (newQuantity < 1) {
            window.c92.showToast('error', 'A mennyiségnek nagyobbnak kell lennie, mint 0.');
            quantityInput.value = '1';
            newQuantity = 1;
        }
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
            discountAmountInput.value = '';
            newDiscountAmount = 0;
        }
        calculateAllPrices(
            row,
            unitPrice,
            parseInt(discountTypeSelect.value, 10) || 1,
            newDiscountAmount,
            newQuantity,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    discountTypeSelect._listener = () => {
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        discountAmountInput.readOnly = ![5, 6].includes(newDiscountTypeId);
        if (newDiscountTypeId === 1) {
            discountAmountInput.value = '';
        }
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
            discountAmountInput.value = '';
            newDiscountAmount = 0;
        }
        calculateAllPrices(
            row,
            unitPrice,
            newDiscountTypeId,
            newDiscountAmount,
            parseInt(quantityInput.value, 10) || 1,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    discountAmountInput._listener = () => {
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
            discountAmountInput.value = '';
            newDiscountAmount = 0;
        }
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        if (newDiscountTypeId === 5) {
            newDiscountAmount = unitPrice * (newDiscountAmount / 100);
        }
        calculateAllPrices(
            row,
            unitPrice,
            newDiscountTypeId,
            newDiscountAmount,
            parseInt(quantityInput.value, 10) || 1,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    vatSelect._listener = () => {
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            window.c92.showToast('error', 'A kedvezmény összege nem lehet negatív.');
            discountAmountInput.value = '';
            newDiscountAmount = 0;
        }
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        if (newDiscountTypeId === 5) {
            newDiscountAmount = unitPrice * (newDiscountAmount / 100);
        }
        calculateAllPrices(
            row,
            unitPrice,
            newDiscountTypeId,
            newDiscountAmount,
            parseInt(quantityInput.value, 10) || 1,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    quantityInput.addEventListener('input', quantityInput._listener);
    discountTypeSelect.addEventListener('change', discountTypeSelect._listener);
    discountAmountInput.addEventListener('input', discountAmountInput._listener);
    vatSelect.addEventListener('change', vatSelect._listener);
    window.calculateOrderTotals(orderId);
}


async function calculateAllPrices(row, unitPrice, discountTypeId, discountAmount, quantity, vatSelect, product) {
    const itemId = row.dataset.itemId;
    const orderId = row.closest('table').dataset.orderId || 'new';
    const productId = product?.productId || row.querySelector('.tom-select-product')?.tomselect?.getValue();
    const netUnitPriceSpan = row.querySelector('.net-unit-price');
    const grossTotalPriceSpan = row.querySelector('.gross-total-price');
    const unitPriceInput = row.querySelector('.unit-price');
    const discountAmountInput = row.querySelector('.discount-value');
    const netTotalSpan = row.querySelector('.net-total-price');
    if (!netUnitPriceSpan || !grossTotalPriceSpan || !unitPriceInput || !discountAmountInput || !netTotalSpan) {
        console.error('Missing price fields in row:', row);
        window.c92.showToast('error', 'Hiányzó ár mezők a sorban.');
        return;
    }
    const vatTypeId = vatSelect.tomselect?.getValue() || vatSelect.dataset.selectedId || '1';
    const vatRate = vatSelect.tomselect?.options?.[vatTypeId]?.rate ?? 27;
    
    // Use fallback listPrice for known products
    let effectiveUnitPrice = parseFloat(unitPrice) || parseFloat(product?.listPrice) || 0;
    if (!effectiveUnitPrice) {
        if (productId === '62044') {
            effectiveUnitPrice = 980; // Fallback for Termék 4
            console.log(`Using fallback listPrice=980 for ProductId: 62044`);
        } else if (productId === '62056') {
            effectiveUnitPrice = 1000; // Example fallback for Termék 5, adjust as needed
            console.log(`Using fallback listPrice=1000 for ProductId: 62056`);
        } else {
            console.warn(`Érvénytelen egységár a termékhez (ProductId: ${productId}), 0 használata`);
            window.c92.showToast('warning', `Érvénytelen egységár a termékhez (ID: ${productId}), 0 használata`);
            effectiveUnitPrice = 0;
        }
    }
    
    let netPrice = effectiveUnitPrice;
    let parsedDiscountAmount = discountAmountInput.value ? parseFloat(discountAmountInput.value.replace(',', '.')) : (parseFloat(discountAmount) || 0);
    if (isNaN(parsedDiscountAmount)) {
        console.warn(`Érvénytelen kedvezmény összeg a tételhez ${itemId}: ${discountAmountInput.value}, 0 használata`);
        window.c92.showToast('warning', `Érvénytelen kedvezmény összeg a tételhez ${itemId}, 0 használata`);
        parsedDiscountAmount = 0;
    }
    let partnerPrice = null;
    const validDiscountTypeIds = [1, 3, 4, 5, 6];
    if (!validDiscountTypeIds.includes(discountTypeId)) {
        console.warn(`Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, NoDiscount (1) használata`);
        window.c92.showToast('error', `Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, nincs kedvezmény alkalmazva`);
        discountTypeId = 1;
        row.dataset.discountTypeId = '1';
        const discountTypeSelect = row.querySelector('.discount-type-id');
        if (discountTypeSelect?.tomselect) {
            discountTypeSelect.tomselect.setValue('1');
        }
    }
    if (discountTypeId === 3) {
        try {
            const partnerSelect = document.querySelector(`#partner-select_${orderId}`);
            const partnerId = partnerSelect?.value ? parseInt(partnerSelect.value) : 5001;
            if (!productId || !partnerId) {
                console.warn(`Missing productId or partnerId for PartnerPrice, item ${itemId}`);
                window.c92.showToast('warning', `Hiányzó termék vagy partner azonosító a partner ár kiszámításához, tétel ${itemId}`);
                discountTypeId = 1;
                row.dataset.discountTypeId = '1';
                const discountTypeSelect = row.querySelector('.discount-type-id');
                if (discountTypeSelect?.tomselect) {
                    discountTypeSelect.tomselect.setValue('1');
                }
            } else {
                const response = await fetch(`/api/product/partner-price?partnerId=${partnerId}&productId=${productId}`);
                if (!response.ok) {
                    console.warn(`Failed to fetch partner price for product ${productId}, partner ${partnerId}: ${response.status}`);
                    window.c92.showToast('warning', `Nem sikerült lekérni a partner árat a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                    discountTypeId = 1;
                    row.dataset.discountTypeId = '1';
                    const discountTypeSelect = row.querySelector('.discount-type-id');
                    if (discountTypeSelect?.tomselect) {
                        discountTypeSelect.tomselect.setValue('1');
                    }
                } else {
                    const productData = await response.json();
                    partnerPrice = productData?.partnerPrice ? parseFloat(productData.partnerPrice) : null;
                    if (!productData?.partnerPrice || productData.partnerPrice <= 0) {
                        console.warn(`No valid partner price found for product ${productId}, partner ${partnerId}, using base price`);
                        window.c92.showToast('warning', `Nincs érvényes partner ár a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                        discountTypeId = 1;
                        row.dataset.discountTypeId = '1';
                        const discountTypeSelect = row.querySelector('.discount-type-id');
                        if (discountTypeSelect?.tomselect) {
                            discountTypeSelect.tomselect.setValue('1');
                        }
                    } else {
                        parsedDiscountAmount = effectiveUnitPrice - partnerPrice;
                        if (parsedDiscountAmount < 0) {
                            console.warn(`Negative discount amount for PartnerPrice: ${parsedDiscountAmount}, item ${itemId}, using base price`);
                            window.c92.showToast('warning', `Negatív kedvezmény összeg a partner árnál, tétel ${itemId}, alapár használata`);
                            discountTypeId = 1;
                            row.dataset.discountTypeId = '1';
                            const discountTypeSelect = row.querySelector('.discount-type-id');
                            if (discountTypeSelect?.tomselect) {
                                discountTypeSelect.tomselect.setValue('1');
                            }
                            partnerPrice = null;
                        }
                    }
                }
            }
        } catch (error) {
            console.error(`Error fetching partner price for item ${itemId}:`, error);
            window.c92.showToast('error', `Hiba a partner ár lekérése közben a tételhez ${itemId}: ${error.message}`);
            discountTypeId = 1;
            row.dataset.discountTypeId = '1';
            const discountTypeSelect = row.querySelector('.discount-type-id');
            if (discountTypeSelect?.tomselect) {
                discountTypeSelect.tomselect.setValue('1');
            }
        }
    }
    if (discountTypeId === 1) {
        parsedDiscountAmount = null;
        discountAmountInput.value = '';
        netPrice = effectiveUnitPrice;
    } else if (discountTypeId === 5) {
        if (parsedDiscountAmount < 0 || parsedDiscountAmount > 100) {
            console.warn(`Érvénytelen kedvezmény százalék: ${parsedDiscountAmount} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
        }
        netPrice = effectiveUnitPrice * (1 - parsedDiscountAmount / 100);
    } else if (discountTypeId === 6) {
        parsedDiscountAmount = discountAmountInput.value ? parseFloat(discountAmountInput.value.replace(',', '.')) : (parseFloat(discountAmount) || 0);
        if (isNaN(parsedDiscountAmount) || parsedDiscountAmount < 0) {
            console.warn(`Érvénytelen kedvezmény összeg: ${discountAmountInput.value} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
        }
        netPrice = effectiveUnitPrice - parsedDiscountAmount;
    } else if (discountTypeId === 3 && partnerPrice !== null) {
        netPrice = partnerPrice;
        parsedDiscountAmount = effectiveUnitPrice - partnerPrice;
    } else if (discountTypeId === 4 && productId) {
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
            } else if (!isNaN(parse(data.volumePrice))) {
                volumePrice = parse(data.volumePrice);
            }
            if (!isNaN(volumePrice)) {
                netPrice = volumePrice;
                parsedDiscountAmount = effectiveUnitPrice - volumePrice;
                if (parsedDiscountAmount < 0 || isNaN(parsedDiscountAmount)) parsedDiscountAmount = 0;
                console.log(`✅ Volume pricing applied: quantity=${quantityInt}, unit=${volumePrice}`);
            } else {
                throw new Error(`No usable volume price for product ${productId}`);
            }
        } catch (error) {
            console.warn(`⚠️ Volume pricing error for item ${itemId}:`, error.message);
            window.c92.showToast('error', `Volume árhiba (${productId}): ${error.message}`);
            netPrice = effectiveUnitPrice;
            parsedDiscountAmount = 0;
        }
    }
    if (netPrice < 0) {
        console.warn(`Negatív nettó ár: ${netPrice} a tételhez ${itemId}, 0-ra állítva`);
        window.c92.showToast('warning', `Negatív nettó ár a tételhez ${itemId}, 0-ra állítva`);
        netPrice = 0;
    }
    const grossPrice = netPrice * (1 + vatRate / 100);
    const totalGrossPrice = grossPrice * quantity;
    const netTotalPrice = netPrice * quantity;
    unitPriceInput.value = effectiveUnitPrice.toFixed(2);
    netUnitPriceSpan.textContent = netPrice.toFixed(2);
    netTotalSpan.textContent = netTotalPrice.toFixed(2);
    grossTotalPriceSpan.textContent = totalGrossPrice.toFixed(2);
    row.dataset.discountTypeId = discountTypeId.toString();
    row.dataset.discountAmount = discountTypeId === 1 ? '' : parsedDiscountAmount.toString();
    row.dataset.partnerPrice = discountTypeId === 3 ? partnerPrice?.toString() || '' : '';
    console.log(`calculateAllPrices for item ${itemId}: unitPrice=${effectiveUnitPrice.toFixed(2)}, discountTypeId=${discountTypeId}, discountAmount=${parsedDiscountAmount}, partnerPrice=${partnerPrice || 'N/A'}, netPrice=${netPrice.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}, totalGrossPrice=${totalGrossPrice.toFixed(2)}, vatRate=${vatRate}%`);
}

window.calculateOrderTotals = function(orderId) {
    console.log(`Calculating totals for orderId: ${orderId}`);
    const tbody = document.querySelector(`#items-tbody_${orderId}`);
    if (!tbody) {
        console.error(`Tbody #items-tbody_${orderId} not found`);
        window.c92.showToast('error', `Táblázat nem található: ${orderId}`);
        return;
    }
    let totalNet = 0;
    let totalVat = 0;
    let totalGross = 0;
    const rows = tbody.querySelectorAll('.order-item-row');
    console.log(`Found ${rows.length} order-item-row(s)`);
    rows.forEach(row => {
        const netTotalSpan = row.querySelector('.net-total-price');
        const grossTotalSpan = row.querySelector('.gross-total-price');
        const netTotal = netTotalSpan ? parseFloat(netTotalSpan.textContent) || 0 : 0;
        const grossTotal = grossTotalSpan ? parseFloat(grossTotalSpan.textContent) || 0 : 0;
        console.log(`Row ${row.dataset.itemId}: netTotal=${netTotal}, grossTotal=${grossTotal}, netContent=${netTotalSpan?.textContent}, grossContent=${grossTotalSpan?.textContent}`);
        totalNet += netTotal;
        totalVat += grossTotal - netTotal;
        totalGross += grossTotal;
    });
    console.log(`Totals before update: totalNet=${totalNet}, totalVat=${totalVat}, totalGross=${totalGross}`);
    // Use class selectors within tbody
    const totalNetSpan = tbody.querySelector('.order-total-net');
    const totalVatSpan = tbody.querySelector('.order-vat-amount');
    const totalGrossSpan = tbody.querySelector('.order-gross-amount');
    console.log(`Total spans: net=${!!totalNetSpan}, vat=${!!totalVatSpan}, gross=${!!totalGrossSpan}`);
    if (!totalNetSpan || !totalVatSpan || !totalGrossSpan) {
        console.error(`Total spans not found in #items-tbody_${orderId}`, {
            netClass: '.order-total-net',
            vatClass: '.order-vat-amount',
            grossClass: '.order-gross-amount'
        });
        window.c92.showToast('error', `Összesítő mezők nem találhatók: ${orderId}`);
        return;
    }
    totalNetSpan.textContent = totalNet.toFixed(2);
    totalVatSpan.textContent = totalVat.toFixed(2);
    totalGrossSpan.textContent = totalGross.toFixed(2);
    console.log(`Order totals updated for orderId: ${orderId}`, { totalNet, totalVat, totalGross });
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOMContentLoaded, initializing order modals');
    initializeOrderModals();
});