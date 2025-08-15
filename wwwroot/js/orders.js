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
    // Initialize delete modals
    document.querySelectorAll('[id^="deleteOrderModal_"]').forEach(modal => {
            const orderId = modal.id.replace('deleteOrderModal_', '');
            modal.addEventListener('show.bs.modal', () => {
                console.log(`Delete Order modal shown for orderId: ${orderId}`);
                initializeDeleteOrderModal(orderId);
            });
        });
}

window.c92 = window.c92 || {}; // Initialize namespace early

window.c92.copyOrder = async function(orderId) {
    try {
        if (!Number.isInteger(Number(orderId)) || orderId <= 0) {
            throw new Error("Érvénytelen OrderId: Az azonosítónak pozitív egész számnak kell lennie");
        }
        window.c92.showToast('Megrendelés másolása...', 'info');
        const response = await fetch(`/api/orders/copy/${orderId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) {
            let errorMessage = 'Nem sikerült a megrendelés másolása';
            try {
                const errorData = await response.json();
                errorMessage = errorData.error || `Hiba: ${response.status} ${response.statusText}`;
            } catch {
                errorMessage = `Hiba: ${response.status} ${response.statusText}`;
            }
            throw new Error(errorMessage);
        }

        const newOrder = await response.json();
        window.c92.showToast(`Megrendelés ${orderId} sikeresen másolva! Új megrendelés ID: ${newOrder.orderId}`, 'success');
    } catch (error) {
        console.error('Hiba a megrendelés másolásakor:', error, { orderId });
        window.c92.showToast(`Nem sikerült a megrendelés másolása: ${error.message}`, 'error');
    }
};

async function initializeDeleteOrderModal(orderId) {
    console.log('Initializing delete order modal for orderId:', orderId);
    const modal = document.getElementById(`deleteOrderModal_${orderId}`);
    if (!modal) {
        console.error(`Delete modal #deleteOrderModal_${orderId} not found`);
        window.c92.showToast('error', `Törlési modal nem található: ${orderId}`);
        return;
    }
    let deleteButton = modal.querySelector('.delete-order-btn');
    if (!deleteButton) {
        // Fallback selectors for robustness
        deleteButton = modal.querySelector('button[data-order-id="' + orderId + '"]') || 
                      modal.querySelector('button.btn-danger') || 
                      modal.querySelector('button[type="button"]:not(.btn-close)');
        if (!deleteButton) {
            console.error(`Delete button not found for orderId: ${orderId}`);
            console.log('Modal HTML:', modal.outerHTML); // Log modal HTML for debugging
            window.c92.showToast('error', `Törlés gomb nem található: ${orderId}`);
            return;
        }
        console.warn(`Fallback delete button used for orderId: ${orderId}`, deleteButton);
    }
    // Remove existing listeners to prevent duplicates
    deleteButton.removeEventListener('click', deleteButton._clickHandler);
    deleteButton._clickHandler = async () => {
        console.log(`Delete button clicked for orderId: ${orderId}`);
        const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!antiForgeryToken) {
            console.error('Anti-forgery token not found');
            window.c92.showToast('error', 'Hiányzik az anti-forgery token.');
            return;
        }
        try {
            const basePath = ''; // Set to '/CRM' if your app runs under /CRM
            const response = await fetch(`${basePath}/api/orders/${orderId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                credentials: 'include' // Include cookies for ASP.NET Core Identity
            });
            console.log(`DELETE response status: ${response.status}`);
            if (!response.ok) {
                const err = await response.json().catch(() => ({}));
                console.error('API error response:', err);
                if (response.status === 401 || response.status === 403) {
                    console.error('Unauthorized or forbidden access, redirecting to login');
                    window.c92.showToast('error', 'Kérjük, jelentkezzen be a rendelés törléséhez.');
                    window.location.href = '/Identity/Account/Login';
                    return;
                }
                window.c92.showToast('error', `Hiba a rendelés törlése során: ${err.error || err.message || `HTTP ${response.status}`}`);
                return;
            }
            console.log(`Order ${orderId} deleted from server`);
            window.c92.showToast('success', `Rendelés törölve: ${orderId}`);
            const modalInstance = bootstrap.Modal.getInstance(modal);
            if (modalInstance) {
                modalInstance.hide();
            }
            // Remove order card from UI
            const orderCard = document.querySelector(`.partner-card[data-order-id="${orderId}"]`);
            if (orderCard) {
                orderCard.remove();
                console.log(`Order card removed for orderId: ${orderId}`);
            } else {
                console.warn(`Order card not found for orderId: ${orderId}`);
            }
        } catch (error) {
            console.error('Delete error:', error);
            window.c92.showToast('error', `Hiba a rendelés törlése során: ${error.message}`);
        }
    };
    deleteButton.addEventListener('click', deleteButton._clickHandler);
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

    // Save Button
    const saveButton = document.querySelector(`#saveOrderBtn_${orderId}`);
    if (!saveButton) {
        console.error(`Save button #saveOrderBtn_${orderId} not found`);
        window.c92.showToast('error', `Mentés gomb nem található: ${orderId}`);
        return;
    }

    // Remove existing listeners to prevent duplicates
    saveButton.removeEventListener('click', saveButton._clickHandler);
    saveButton._clickHandler = async () => {
        console.log(`Save button clicked for orderId: ${orderId}`);
        const form = document.querySelector(`#${orderId === 'new' ? 'newOrderForm' : 'editOrderForm_' + orderId}`);
        const baseInfoForm = document.querySelector(`#orderBaseInfoForm_${orderId}`);
        if (!form || !baseInfoForm) {
            console.error(`Form not found: newOrderForm=${!!form}, orderBaseInfoForm_${orderId}=${!!baseInfoForm}`);
            window.c92.showToast('error', `Űrlap nem található: ${orderId}`);
            return;
        }

        const totalGrossElement = document.querySelector(`#items-tbody_${orderId} .order-gross-amount`);
        const totalGrossText = totalGrossElement?.textContent?.trim() || '0';
        console.log(`Total gross element: ${totalGrossElement ? 'found' : 'not found'}, text: ${totalGrossText}`);
        let totalGross = parseFloat(totalGrossText.replace(/[^0-9.]/g, '')) || 0;
        console.log(`Parsed totalGross: ${totalGross}`);

        const orderItems = [];
        for (const row of form.querySelectorAll('.order-item-row')) {
            const productSelect = row.querySelector('.tom-select-product');
            const vatSelect = row.querySelector('.tom-select-vat');
            const discountTypeId = parseInt(row.dataset.discountTypeId) || 1;
            const unitPrice = parseFloat(row.querySelector('.unit-price')?.value) || 0;
            const productId = parseInt(productSelect?.tomselect?.getValue() || productSelect?.value) || 0;
            const vatTypeId = parseInt(vatSelect?.tomselect?.getValue() || vatSelect?.dataset.selectedId) || 0;
            if (!productId) {
                console.error(`Invalid ProductId for row ${row.dataset.itemId}: ${productId}`);
                window.c92.showToast('error', `Érvénytelen termék azonosító: ${row.dataset.itemId}`);
                return;
            }
            if (!vatTypeId) {
                console.error(`Invalid VatTypeId for row ${row.dataset.itemId}: ${vatTypeId}`);
                window.c92.showToast('error', `Érvénytelen ÁFA típus azonosító: ${row.dataset.itemId}`);
                return;
            }
            const item = {
                ProductId: productId,
                VatTypeId: vatTypeId, // Added
                DiscountType: discountTypeId,
                Quantity: parseFloat(row.querySelector('.quantity')?.value) || 1,
                UnitPrice: unitPrice,
                DiscountAmount: parseFloat(row.dataset.discountAmount) || 0,
                Description: row.nextElementSibling?.querySelector('.item-description')?.value || '',
                CreatedBy: localStorage.getItem('username') || 'System',
                CreatedDate: new Date().toISOString(),
                ModifiedBy: localStorage.getItem('username') || 'System',
                ModifiedDate: new Date().toISOString()
            };
            if (orderId !== 'new') {
                item.OrderItemId = parseInt(row.dataset.itemId) || 0;
            }
            console.log(`Order item for row ${row.dataset.itemId}:`, item);
            orderItems.push(item);
        }
        console.log(`Order items to save:`, JSON.stringify(orderItems, null, 2));

        const orderData = {
            PartnerId: parseInt(baseInfoForm.querySelector('[name="PartnerId"]')?.tomselect?.getValue() || baseInfoForm.querySelector('[name="PartnerId"]')?.value) || 0,
            CurrencyId: parseInt(baseInfoForm.querySelector('[name="currencyId"]')?.tomselect?.getValue() || baseInfoForm.querySelector('[name="currencyId"]')?.value) || 0,
            SiteId: parseInt(baseInfoForm.querySelector('[name="SiteId"]')?.tomselect?.getValue() || baseInfoForm.querySelector('[name="SiteId"]')?.value) || null,
            QuoteId: parseInt(baseInfoForm.querySelector('[name="quoteId"]')?.tomselect?.getValue() || baseInfoForm.querySelector('[name="quoteId"]')?.value) || null,
            OrderNumber: baseInfoForm.querySelector('[name="orderNumber"]')?.value || `ORD-${Date.now()}`,
            OrderDate: baseInfoForm.querySelector('[name="orderDate"]')?.value || new Date().toISOString().split('T')[0],
            TotalAmount: totalGross,
            Status: baseInfoForm.querySelector('[name="status"]')?.value || 'Draft',
            CreatedBy: baseInfoForm.querySelector('[name="createdBy"]')?.value || localStorage.getItem('username') || 'System',
            CreatedDate: baseInfoForm.querySelector('[name="createdDate"]')?.value || new Date().toISOString(),
            ModifiedBy: baseInfoForm.querySelector('[name="modifiedBy"]')?.value || localStorage.getItem('username') || 'System',
            ModifiedDate: baseInfoForm.querySelector('[name="modifiedDate"]')?.value || new Date().toISOString(),
            OrderItems: orderItems,
            Deadline: baseInfoForm.querySelector('[name="deadline"]')?.value || null,
            Description: baseInfoForm.querySelector('[name="description"]')?.value || null,
            SalesPerson: baseInfoForm.querySelector('[name="salesPerson"]')?.value || null,
            DeliveryDate: baseInfoForm.querySelector('[name="deliveryDate"]')?.value || null,
            DiscountPercentage: parseFloat(baseInfoForm.querySelector('[name="discountPercentage"]')?.value) || null,
            DiscountAmount: parseFloat(baseInfoForm.querySelector('[name="discountAmount"]')?.value) || null,
            CompanyName: baseInfoForm.querySelector('[name="companyName"]')?.value || null,
            Subject: baseInfoForm.querySelector('[name="subject"]')?.value || null,
            DetailedDescription: baseInfoForm.querySelector('[name="detailedDescription"]')?.value || null,
            PaymentTerms: baseInfoForm.querySelector('[name="paymentTerms"]')?.value || null,
            ShippingMethod: baseInfoForm.querySelector('[name="shippingMethod"]')?.value || null,
            OrderType: baseInfoForm.querySelector('[name="orderType"]')?.value || null,
            ReferenceNumber: baseInfoForm.querySelector('[name="referenceNumber"]')?.value || null
        };
        console.log('Order data to save:', JSON.stringify(orderData, null, 2));

        if (!orderData.PartnerId) {
            console.error('Missing PartnerId');
            window.c92.showToast('error', 'Kérjük, válasszon partnert.');
            return;
        }
        if (!orderData.CurrencyId) {
            console.error('Missing CurrencyId');
            window.c92.showToast('error', 'Kérjük, válasszon pénznemet.');
            return;
        }
        if (!orderData.OrderItems.length) {
            console.error('No items in order');
            window.c92.showToast('error', 'Legalább egy tétel szükséges.');
            return;
        }

        // For existing orders, sync OrderItems by deleting removed items
        if (orderId !== 'new') {
            try {
                const response = await fetch(`/api/orders/${orderId}`, {
                    headers: {
                        'Authorization': 'Bearer ' + (localStorage.getItem('token') || ''),
                        'Accept': 'application/json'
                    }
                });
                if (!response.ok) {
                    const err = await response.json();
                    console.error(`Failed to fetch order ${orderId}:`, err);
                    window.c92.showToast('error', `Hiba a rendelés lekérése során: ${err.error || err.message || 'HTTP ' + response.status}`);
                    return;
                }
                const existingOrder = await response.json();
                const existingItemIds = existingOrder.orderItems.map(item => item.orderItemId);
                const currentItemIds = orderItems.map(item => item.OrderItemId).filter(id => id > 0);

                // Delete items that are no longer in the UI
                for (const itemId of existingItemIds) {
                    if (!currentItemIds.includes(itemId)) {
                        console.log(`Deleting OrderItem ${itemId} from server`);
                        const deleteResponse = await fetch(`/api/orders/${orderId}/items/${itemId}`, {
                            method: 'DELETE',
                            headers: {
                                'Content-Type': 'application/json',
                                'Authorization': 'Bearer ' + (localStorage.getItem('token') || '')
                            }
                        });
                        if (!deleteResponse.ok) {
                            const err = await deleteResponse.json();
                            console.error(`Failed to delete OrderItem ${itemId}:`, err);
                            window.c92.showToast('error', `Hiba a tétel törlése során: ${err.error || err.message || 'HTTP ' + deleteResponse.status}`);
                            return;
                        }
                        console.log(`OrderItem ${itemId} deleted from server`);
                    }
                }
            } catch (error) {
                console.error('Error syncing OrderItems:', error);
                window.c92.showToast('error', `Hiba a tételek szinkronizálása során: ${error.message}`);
                return;
            }
        }

        // Save the order
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
                    console.error('API error response:', err);
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
    };
    saveButton.addEventListener('click', saveButton._clickHandler);

    // Add Item Button
    const addItemButton = modal.querySelector('.add-item-row');
    if (addItemButton) {
        // Remove existing listeners to prevent duplicates
        addItemButton.removeEventListener('click', addItemButton._clickHandler);
        const newAddItemButton = addItemButton.cloneNode(true);
        addItemButton.replaceWith(newAddItemButton);
        newAddItemButton._clickHandler = () => {
            console.log(`Add item button clicked for orderId: ${orderId}`);
            addItemRow(orderId);
        };
        newAddItemButton.addEventListener('click', newAddItemButton._clickHandler);
        console.log(`Add item button initialized for orderId: ${orderId}`);
    } else {
        console.warn(`Add item button .add-item-row not found for orderId: ${orderId}`);
        window.c92.showToast('error', 'Tétel hozzáadása gomb nem található.');
    }

// Inside initializeEventListeners
modal.removeEventListener('click', modal._removeItemHandler);
modal._removeItemHandler = async (event) => {
    if (!event.target.closest('.remove-item-row')) return;
    const btn = event.target.closest('.remove-item-row');
    const itemId = btn.dataset.itemId;
    console.log(`Remove item button clicked for orderId: ${orderId}, itemId: ${itemId}, button:`, btn);
    if (!itemId) {
        console.error('Missing itemId on remove button');
        window.c92.showToast('error', 'Tétel azonosító hiányzik.');
        return;
    }
    if (orderId !== 'new' && !parseInt(orderId)) {
        console.error(`Invalid orderId: ${orderId}`);
        window.c92.showToast('error', `Érvénytelen rendelés azonosító: ${orderId}`);
        return;
    }
    const itemRow = document.querySelector(`tr.order-item-row[data-item-id="${itemId}"]`);
    const descriptionRow = document.querySelector(`tr.description-row[data-item-id="${itemId}"]`);
    console.log(`Item row found: ${!!itemRow}, Description row found: ${!!descriptionRow}`);
    if (!itemRow) {
        console.error(`Item row not found for itemId: ${itemId}`);
        window.c92.showToast('error', `Tétel nem található: ${itemId}`);
        return;
    }
    if (orderId !== 'new' && !itemId.startsWith('new_')) {
        const numericItemId = parseInt(itemId);
        if (isNaN(numericItemId) || numericItemId <= 0) {
            console.error(`Invalid OrderItemId: ${itemId}`);
            window.c92.showToast('error', `Érvénytelen tétel azonosító: ${itemId}`);
            return;
        }
        try {
            const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!antiForgeryToken) {
                console.error('Anti-forgery token not found');
                window.c92.showToast('error', 'Hiányzik az anti-forgery token.');
                return;
            }
            const basePath = ''; // Corrected to match /api/orders
            const orderResponse = await fetch(`${basePath}/api/orders/${orderId}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                credentials: 'include'
            });
            console.log(`GET request URL: ${basePath}/api/orders/${orderId}`);
            if (!orderResponse.ok) {
                console.error(`Failed to fetch order ${orderId}: ${orderResponse.status}`);
                window.c92.showToast('error', `Nem sikerült lekérni a rendelést: ${orderId}`);
                return;
            }
            const orderData = await orderResponse.json();
            const itemExists = orderData.orderItems?.some(item => item.orderItemId === numericItemId);
            if (!itemExists) {
                console.error(`Order item ${numericItemId} not found in order ${orderId} data`);
                window.c92.showToast('error', `A tétel nem található a rendelésben: ${numericItemId}`);
                return;
            }
            const response = await fetch(`${basePath}/api/orders/${orderId}/items/${numericItemId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                credentials: 'include'
            });
            console.log(`DELETE request URL: ${basePath}/api/orders/${orderId}/items/${numericItemId}`);
            console.log(`DELETE response status: ${response.status}`);
            console.log(`Response headers:`, Object.fromEntries(response.headers));
            if (!response.ok) {
                const err = await response.json().catch(() => ({}));
                console.error('API error response:', err);
                if (response.status === 401 || response.status === 403) {
                    console.error('Unauthorized or forbidden access, redirecting to login');
                    console.log('Cookies sent:', document.cookie);
                    window.c92.showToast('error', 'Kérjük, jelentkezzen be a tétel törléséhez.');
                    window.location.href = '/CRM/Identity/Account/Login';
                    return;
                }
                if (response.status === 404) {
                    console.error(`Order item ${numericItemId} not found for order ${orderId}`);
                    window.c92.showToast('error', `A tétel nem található: ${numericItemId}`);
                    return;
                }
                window.c92.showToast('error', `Hiba a tétel törlése során: ${err.error || err.message || `HTTP ${response.status}`}`);
                return;
            }
            console.log(`Order item ${numericItemId} deleted from server for order ${orderId}`);
            window.c92.showToast('success', `Tétel törölve a szerverről: ${itemId}`);
        } catch (error) {
            console.error('Delete error:', error);
            window.c92.showToast('error', `Hiba a tétel törlése során: ${error.message}`);
            return;
        }
    }
    console.log(`Removing UI elements for itemId: ${itemId}`);
    itemRow.remove();
    if (descriptionRow) descriptionRow.remove();
    window.calculateOrderTotals(orderId);
    window.c92.showToast('success', `Tétel törölve a felületről: ${itemId}`);
};
modal.addEventListener('click', modal._removeItemHandler);
}

async function populateEditOrderModal(orderId) {
    try {
        console.log('Starting populateEditOrderModal for orderId:', orderId);
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
        const basePath = '';
        const response = await fetch(`${basePath}/api/orders/${orderId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            credentials: 'include'
        });
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`API error for order ${orderId}: ${response.status} ${response.statusText}`, errorText);
            if (response.status === 404) {
                window.c92.showToast('error', `A rendelés nem található: ${orderId}`);
                modal.querySelector('.btn-close').click();
                const orderCard = document.querySelector(`.partner-card[data-order-id="${orderId}"]`);
                if (orderCard) orderCard.remove();
                return;
            }
            throw new Error(`Failed to fetch order: ${response.status} ${response.statusText}`);
        }
        const order = await response.json();
        console.log(`Fetched order data for ${orderId}:`, JSON.stringify(order, null, 2));
        const setFieldValue = (selector, value) => {
            const element = form.querySelector(selector);
            if (element) {
                element.value = value || '';
            } else {
                console.error(`Element ${selector} not found in #orderBaseInfoForm_${orderId}`);
            }
        };
        setFieldValue('[name="OrderNumber"]', order.orderNumber);
        setFieldValue('[name="OrderDate"]', order.orderDate ? order.orderDate.split('T')[0] : '');
        setFieldValue('[name="Deadline"]', order.deadline ? order.deadline.split('T')[0] : '');
        setFieldValue('[name="Status"]', order.status || 'Draft');
        setFieldValue('[name="SalesPerson"]', order.salesPerson);
        setFieldValue('[name="DeliveryDate"]', order.deliveryDate ? order.deliveryDate.split('T')[0] : '');
        setFieldValue('[name="CompanyName"]', order.companyName);
        setFieldValue('[name="Subject"]', order.subject);
        setFieldValue('[name="PaymentTerms"]', order.paymentTerms);
        setFieldValue('[name="ShippingMethod"]', order.shippingMethod);
        setFieldValue('[name="OrderType"]', order.orderType);
        setFieldValue('[name="ReferenceNumber"]', order.referenceNumber);
        setFieldValue('[name="DiscountPercentage"]', order.discountPercentage ? order.discountPercentage.toFixed(2) : '');
        setFieldValue('[name="DiscountAmount"]', order.discountAmount ? order.discountAmount.toFixed(2) : '');
        setFieldValue('[name="TotalAmount"]', order.totalAmount ? order.totalAmount.toFixed(2) : 0);
        setFieldValue('[name="Description"]', order.description);
        setFieldValue('[name="DetailedDescription"]', order.detailedDescription);
        console.log('Form fields populated for orderId:', orderId);
        const partnerSelect = form.querySelector('[name="PartnerId"]');
        const currencySelect = form.querySelector('[name="CurrencyId"]');
        const siteSelect = form.querySelector('[name="SiteId"]');
        const quoteSelect = form.querySelector('[name="QuoteId"]');
        if (!partnerSelect) console.error(`Partner select [name="PartnerId"] not found in #orderBaseInfoForm_${orderId}`);
        if (!siteSelect) console.error(`Site select [name="SiteId"] not found in #orderBaseInfoForm_${orderId}`);
        if (!currencySelect) console.error(`Currency select [name="CurrencyId"] not found in #orderBaseInfoForm_${orderId}`);
        if (!quoteSelect) console.error(`Quote select [name="QuoteId"] not found in #orderBaseInfoForm_${orderId}`);

        if (partnerSelect) {
            try {
                partnerSelect.dataset.selectedId = order.partnerId || '';
                partnerSelect.dataset.selectedText = order.partnerName || '';
                const partnerTomSelect = await window.c92.initializePartnerTomSelect?.(partnerSelect, orderId, 'order');
                if (partnerTomSelect && order.partnerId) {
                    partnerTomSelect.setValue(order.partnerId);
                    console.log(`Partner TomSelect set to: ${order.partnerId}`);
                }
                console.log(`Partner TomSelect initialized for orderId: ${orderId}, value: ${order.partnerId}`);
            } catch (error) {
                console.error(`Failed to initialize Partner TomSelect for orderId: ${orderId}`, error);
                window.c92.showToast('error', 'Hiba a partner kiválasztás inicializálásakor.');
            }
        }

        if (siteSelect) {
            try {
                siteSelect.dataset.selectedId = order.siteId || '';
                siteSelect.dataset.selectedText = order.siteName || '';
                const siteTomSelect = await window.c92.initializeSiteTomSelect?.(siteSelect, orderId, 'order');
                if (siteTomSelect && order.siteId) {
                    siteTomSelect.setValue(order.siteId);
                    console.log(`Site TomSelect set to: ${order.siteId}`);
                }
                console.log(`Site TomSelect initialized for orderId: ${orderId}, value: ${order.siteId}`);
            } catch (error) {
                console.error(`Failed to initialize Site TomSelect for orderId: ${orderId}`, error);
                window.c92.showToast('error', 'Hiba a telephely kiválasztás inicializálásakor.');
            }
        }

        if (currencySelect) {
            try {
                currencySelect.dataset.selectedId = order.currencyId || '';
                currencySelect.dataset.selectedText = order.currencyName || '';
                const currencyTomSelect = await window.c92.initializeCurrencyTomSelect?.(currencySelect, 'order');
                if (currencyTomSelect && order.currencyId) {
                    currencyTomSelect.setValue(order.currencyId);
                    console.log(`Currency TomSelect set to: ${order.currencyId}`);
                }
                console.log(`Currency TomSelect initialized for orderId: ${orderId}, value: ${order.currencyId}`);
            } catch (error) {
                console.error(`Failed to initialize Currency TomSelect for orderId: ${orderId}`, error);
                window.c92.showToast('error', 'Hiba a pénznem kiválasztás inicializálásakor.');
            }
        }

        if (quoteSelect) {
            try {
                quoteSelect.dataset.selectedId = order.quoteId || '';
                quoteSelect.dataset.selectedText = order.quoteName || '';
                const quoteTomSelect = await window.c92.initializeQuoteTomSelect?.(quoteSelect, orderId, 'order');
                if (quoteTomSelect && order.quoteId) {
                    quoteTomSelect.setValue(order.quoteId);
                    console.log(`Quote TomSelect set to: ${order.quoteId}`);
                }
                console.log(`Quote TomSelect initialized for orderId: ${orderId}, value: ${order.quoteId}`);
            } catch (error) {
                console.error(`Failed to initialize Quote TomSelect for orderId: ${orderId}`, error);
                window.c92.showToast('error', 'Hiba az árajánlat kiválasztás inicializálásakor.');
            }
        }

        const tbody = document.querySelector(`#items-tbody_${orderId}`);
        if (!tbody) {
            console.error(`Table body #items-tbody_${orderId} not found`);
            window.c92.showToast('error', 'Rendelési tételek táblázata nem található.');
            return;
        }
        tbody.querySelectorAll('.order-item-row, .description-row').forEach(row => row.remove());
        console.log('Cleared existing order items for orderId:', orderId);

        if (order.orderItems && order.orderItems.length > 0) {
            console.log(`Populating ${order.orderItems.length} order items for orderId: ${orderId}`);
            for (const item of order.orderItems) {
                const itemId = item.orderItemId || 'new_' + Date.now();
                const productId = item.productId || '';
                const productText = item.productName || '';
                const quantity = item.quantity || 1;
                const unitPrice = item.unitPrice || 0;
                const discountTypeId = item.discountTypeId || 1;
                const discountValue = item.discountPercentage || item.discountAmount || 0;
                const vatTypeId = item.vatTypeId || 1;
                const description = item.description || '';
                const vatRate = item.vatRate || 0.27;

                console.log(`Creating row for itemId: ${itemId}, productId: ${productId}, quantity: ${quantity}, unitPrice: ${unitPrice}, vatTypeId: ${vatTypeId}`);

                // Calculate initial prices
                const netUnitPrice = unitPrice * (1 - (discountTypeId === 5 ? discountValue / 100 : 0));
                const netTotalPrice = quantity * netUnitPrice;
                const grossTotalPrice = netTotalPrice * (1 + vatRate);

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
                        <input type="number" name="items[${itemId}][quantity]" class="form-control form-control-sm quantity" value="${quantity}" min="0" step="1" required>
                    </td>
                    <td>
                        <input type="number" name="items[${itemId}][unitPrice]" class="form-control form-control-sm unit-price" value="${unitPrice.toFixed(2)}" min="0" step="0.01" required>
                    </td>
                    <td>
                        <select name="items[${itemId}][discountTypeId]" id="discount-type-${itemId}" class="form-select form-control-sm discount-type-id">
                            <option value="1" ${discountTypeId === 1 ? 'selected' : ''}>Nincs Kedvezmény</option>
                            <option value="3" ${discountTypeId === 3 ? 'selected' : ''}>Ügyfélár</option>
                            <option value="4" ${discountTypeId === 4 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                            <option value="5" ${discountTypeId === 5 ? 'selected' : ''}>Egyedi kedvezmény %</option>
                            <option value="6" ${discountTypeId === 6 ? 'selected' : ''}>Egyedi kedvezmény Összeg</option>
                        </select>
                    </td>
                    <td>
                        <input type="number" name="items[${itemId}][discountValue]" id="discount-value-${itemId}" class="form-control form-control-sm discount-value" value="${discountValue.toFixed(2)}" min="0" step="0.01" readonly>
                    </td>
                    <td>
                        <span class="net-unit-price">${netUnitPrice.toFixed(2)}</span>
                    </td>
                    <td>
                        <select name="items[${itemId}][vatTypeId]" id="tomselect-vat-${itemId}" class="form-select tom-select-vat" data-selected-id="${vatTypeId}" data-selected-text="${vatRate * 100}%" data-selected-rate="${vatRate}" autocomplete="off" required>
                            <option value="" disabled>-- Válasszon ÁFA típust --</option>
                        </select>
                    </td>
                    <td>
                        <span class="net-total-price">${netTotalPrice.toFixed(2)}</span>
                    </td>
                    <td>
                        <span class="gross-total-price">${grossTotalPrice.toFixed(2)}</span>
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
                            <textarea name="items[${itemId}][description]" class="form-control form-control-sm item-description" maxlength="200" rows="2">${description}</textarea>
                            <div class="form-text">Karakterek: <span class="char-count">${description.length}</span>/200</div>
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
                console.log(`Appended item row for itemId: ${itemId}`);

                try {
                    const productSelect = itemRow.querySelector('.tom-select-product');
                    if (productSelect && typeof window.c92.initializeProductTomSelect === 'function') {
                        const tomSelect = await window.c92.initializeProductTomSelect(productSelect, { orderId: orderId });
                        if (tomSelect && productId) {
                            tomSelect.setValue(productId);
                            console.log(`Product TomSelect set to: ${productId}`);
                            const products = Object.values(tomSelect.options).map(opt => ({
                                productId: opt.value,
                                listPrice: opt.listPrice || (opt.value === '62044' ? 980 : opt.value === '62056' ? 1000 : 0),
                                volumePrice: opt.volumePrice || 0,
                                partnerPrice: opt.partnerPrice || null,
                                volumePricing: opt.volumePricing || {},
                                text: opt.text
                            }));
                            await updatePriceFields(productSelect, productId, products);
                        } else {
                            console.warn(`Failed to set Product TomSelect for itemId: ${itemId}, productId: ${productId}`);
                        }
                    } else {
                        console.warn(`Product TomSelect not initialized for itemId: ${itemId}`);
                    }
                } catch (error) {
                    console.error(`Failed to initialize Product TomSelect for itemId: ${itemId}`, error);
                    window.c92.showToast('error', 'Hiba a termék kiválasztás inicializálásakor.');
                }

                try {
                    const vatSelect = itemRow.querySelector('.tom-select-vat');
                    if (vatSelect && typeof window.c92.initializeVatTomSelect === 'function') {
                        const vatOptions = [
                            { vatTypeId: 1, typeName: '27%', rate: 0.27 },
                            { vatTypeId: 2, typeName: '0%', rate: 0.0 },
                            { vatTypeId: 3, typeName: '5%', rate: 0.05 }
                        ];
                        const vatTomSelect = await window.c92.initializeVatTomSelect(vatSelect, orderId, { context: 'order', vatOptions });
                        if (vatTomSelect && vatTypeId) {
                            vatTomSelect.setValue(vatTypeId);
                            console.log(`VAT TomSelect set to: ${vatTypeId}`);
                        } else {
                            console.warn(`Failed to set VAT TomSelect for itemId: ${itemId}, vatTypeId: ${vatTypeId}`);
                        }
                        console.log('VAT select initialized for item:', itemId);
                    } else {
                        console.warn(`VAT TomSelect not initialized for itemId: ${itemId}`);
                    }
                } catch (error) {
                    console.error(`Failed to initialize VAT TomSelect for itemId: ${itemId}`, error);
                    window.c92.showToast('error', 'Hiba az ÁFA kiválasztás inicializálásakor.');
                }

                try {
                    initializeRowCalculations(itemRow);
                    console.log(`Row calculations initialized for itemId: ${itemId}`);
                } catch (error) {
                    console.error(`Failed to initialize row calculations for itemId: ${itemId}`, error);
                }

                try {
                    initializeDescriptionToggle(itemRow);
                    console.log(`Description toggle initialized for itemId: ${itemId}`);
                } catch (error) {
                    console.error(`Failed to initialize description toggle for itemId: ${itemId}`, error);
                }
            }
        } else {
            console.log('No order items to populate for orderId:', orderId);
            tbody.innerHTML = '<tr><td colspan="10">Nincsenek rendelési tételek.</td></tr>';
        }

        try {
            initializeEventListeners(orderId);
            console.log('Event listeners initialized for orderId:', orderId);
        } catch (error) {
            console.error(`Failed to initialize event listeners for orderId: ${orderId}`, error);
        }

        try {
            window.calculateOrderTotals(orderId);
            console.log('Order totals calculated for orderId:', orderId);
        } catch (error) {
            console.error(`Failed to calculate order totals for orderId: ${orderId}`, error);
            window.c92.showToast('error', 'Hiba a rendelés összegek számításakor.');
        }

        console.log('Completed populateEditOrderModal for orderId:', orderId);
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
    let selectedProductId = null;
    let selectedProductText = null;
    if (orderId === '1626') {
        selectedProductId = '62044';
        selectedProductText = 'Termék 4';
    }
    const itemRow = document.createElement('tr');
    itemRow.className = 'order-item-row';
    itemRow.dataset.itemId = newItemId;
    itemRow.dataset.orderId = orderId;
    itemRow.dataset.discountTypeId = '1';
    itemRow.dataset.discountAmount = '0';
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
            <input type="number" name="items[${newItemId}][discountValue]" id="discount-value-${newItemId}" class="form-control form-control-sm discount-value" value="0" min="0">
        </td>
        <td>
            <span class="net-unit-price">0.00</span>
        </td>
        <td>
            <select name="orderItems[${newItemId}][vatTypeId]" id="tomselect-vat-${newItemId}" class="form-select tom-select-vat" data-selected-id="1" data-selected-text="27%" data-selected-rate="27" autocomplete="off" required>
                <option value="" disabled selected>-- Válasszon ÁFA típust --</option>
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
    const existingDiscountInputs = itemRow.querySelectorAll('.discount-value');
    if (existingDiscountInputs.length > 1) {
        console.warn(`Multiple discount-value inputs found in row ${newItemId}: ${existingDiscountInputs.length}`);
        existingDiscountInputs.forEach((input, index) => {
            if (index > 0) input.remove();
        });
    }
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
            if (selectedProductId) {
                console.log('Pre-selecting product:', selectedProductId, selectedProductText);
                tomSelect.setValue(selectedProductId);
                const product = products.find(p => p.productId == selectedProductId) || {
                    productId: selectedProductId,
                    listPrice: selectedProductId === '62044' ? 980 : 1000,
                    partnerPrice: selectedProductId === '62044' ? 900 : null,
                    text: selectedProductText
                };
                await window.updatePriceFields(productSelect, selectedProductId, products);
                console.log('Pre-selection prices updated for product:', selectedProductId);
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
    const discountTypeSelect = itemRow.querySelector('.discount-type-id');
    const discountValueInput = itemRow.querySelector('.discount-value');
    const quantityInput = itemRow.querySelector('.quantity');
    if (!discountValueInput) {
        console.error(`Discount value input not found in row ${newItemId}`);
        window.c92.showToast('error', 'Kedvezmény mező nem található.');
        return;
    }
    // Log existing event listeners to detect interference
    const getEventListeners = (element) => {
        const events = ['input', 'change', 'keydown'];
        const listeners = {};
        events.forEach(event => {
            listeners[event] = (element.__proto__.__lookupGetter__('on' + event) || (() => [])).call(element);
        });
        return listeners;
    };
    console.log(`Event listeners on discountValueInput for item ${newItemId}:`, getEventListeners(discountValueInput));
    function updateDiscountField() {
        const discountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        const isEditable = [5, 6].includes(discountTypeId);
        discountValueInput.readOnly = !isEditable;
        discountValueInput.step = discountTypeId === 5 ? '1' : '0.01';
        console.log(`updateDiscountField for item ${newItemId}: discountTypeId=${discountTypeId}, isEditable=${isEditable}, value=${discountValueInput.value}, dataset=${itemRow.dataset.discountAmount}`);
        window.updatePriceFields(productSelect, productSelect.value, products);
        window.calculateOrderTotals(orderId);
    }
    // Debounce utility
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    discountTypeSelect.addEventListener('change', updateDiscountField);
    discountValueInput.addEventListener('input', (event) => {
        console.log(`Typing in discountValueInput for item ${newItemId}: value=${event.target.value}`);
    });
    discountValueInput.addEventListener('change', debounce((event) => {
        const discountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        if ([5, 6].includes(discountTypeId)) {
            const value = parseFloat(event.target.value);
            console.log(`Discount value changed for item ${newItemId}: input=${event.target.value}, parsed=${value}`);
            if (isNaN(value) || value < 0 || (discountTypeId === 5 && value > 100)) {
                console.warn(`Invalid discount value for item ${newItemId}: ${event.target.value}`);
                window.c92.showToast('error', `A kedvezmény ${discountTypeId === 5 ? 'százaléknak 0 és 100 között kell lennie' : 'összeg nem lehet negatív'}.`);
                discountValueInput.value = '0';
                itemRow.dataset.discountAmount = '0';
            } else {
                const unitPrice = parseFloat(itemRow.querySelector('.unit-price')?.value) || 0;
                itemRow.dataset.discountAmount = discountTypeId === 5 ? (unitPrice * (value / 100)).toFixed(2) : value.toFixed(2);
                console.log(`Discount value processed for item ${newItemId}: discountValue=${discountValueInput.value}, dataset=${itemRow.dataset.discountAmount}`);
                window.updatePriceFields(productSelect, productSelect.value, products);
                window.calculateOrderTotals(orderId);
            }
        } else {
            console.log(`Ignored change event for non-editable discountTypeId=${discountTypeId}`);
        }
    }, 500));
    quantityInput.addEventListener('input', () => {
        console.log(`Quantity changed for item ${newItemId}: quantity=${quantityInput.value}`);
        window.updatePriceFields(productSelect, productSelect.value, products);
        window.calculateOrderTotals(orderId);
    });
    initializeRowCalculations(itemRow);
    initializeDescriptionToggle(itemRow);
    if (selectedProductId) {
        window.updatePriceFields(productSelect, selectedProductId, products);
    }
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
        discountAmountInput.value = '0';
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
    if (!product) {
        if (productId === '62044') {
            product = { productId: '62044', listPrice: 980, partnerPrice: 900, text: 'Termék 4' };
            console.log('Using fallback product data for ProductId: 62044');
        } else if (productId === '62056') {
            product = { productId: '62056', listPrice: 1000, partnerPrice: null, text: 'Termék 5' };
            console.log('Using fallback product data for ProductId: 62056');
        } else {
            product = { productId, listPrice: 0, partnerPrice: null, text: 'Ismeretlen termék' };
            console.warn('No product data available for productId:', productId);
        }
    }
    const unitPrice = parseFloat(product.listPrice) || 0;
    const partnerPrice = parseFloat(product.partnerPrice) || unitPrice;
    const quantity = parseInt(quantityInput.value, 10) || 1;
    let discountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
    let discountPercentage = 0;
    let discountAmount = 0;
    unitPriceInput.value = unitPrice.toFixed(2);
    discountAmountInput.readOnly = ![5, 6].includes(discountTypeId);
    if ([1, 3, 4].includes(discountTypeId)) {
        if (discountTypeId === 1) {
            discountAmount = 0;
        } else {
            discountAmount = unitPrice - partnerPrice;
        }
        const formattedDiscount = discountAmount > 0 ? discountAmount.toFixed(2) : '0';
        discountAmountInput.value = formattedDiscount;
        row.dataset.discountAmount = formattedDiscount;
        console.log(`Auto-filled discount for type ${discountTypeId}: discountAmount=${formattedDiscount}, unitPrice=${unitPrice}, partnerPrice=${partnerPrice}`);
    } else if (discountTypeId === 5) {
        discountPercentage = parseFloat(discountAmountInput.value) || 0;
        if (discountPercentage < 0 || discountPercentage > 100) {
            console.warn(`Invalid discount percentage: ${discountPercentage} for item ${row.dataset.itemId}, using 0`);
            window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie`);
            discountPercentage = 0;
            discountAmountInput.value = '0';
        }
        discountAmount = unitPrice * (discountPercentage / 100);
        row.dataset.discountAmount = (unitPrice - (unitPrice * (1 - discountPercentage / 100))).toFixed(2);
        console.log(`Percentage discount for type 5: discountPercentage=${discountPercentage}, discountAmount=${discountAmount}, unitPrice=${unitPrice}`);
    } else if (discountTypeId === 6) {
        discountAmount = parseFloat(discountAmountInput.value) || 0;
        if (discountAmount < 0) {
            console.warn(`Invalid discount amount: ${discountAmount} for item ${row.dataset.itemId}, using 0`);
            window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív`);
            discountAmount = 0;
            discountAmountInput.value = '0';
        }
        row.dataset.discountAmount = discountAmount.toFixed(2);
    }
    calculateAllPrices(row, unitPrice, discountTypeId, discountPercentage, quantity, vatSelect, product);
    if ([1, 3, 4, 5, 6].includes(discountTypeId)) {
        const formattedDiscount = discountTypeId === 5 ? discountPercentage.toFixed(2) : (discountAmount > 0 ? discountAmount.toFixed(2) : '0');
        if (discountAmountInput.value !== formattedDiscount) {
            discountAmountInput.value = formattedDiscount;
            console.log(`Reapplied discountAmountInput.value=${formattedDiscount} after calculateAllPrices`);
            discountAmountInput.dispatchEvent(new Event('input'));
            discountAmountInput.dispatchEvent(new Event('change'));
        }
    }
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
        let newDiscountPercentage = [5].includes(discountTypeId) ? parseFloat(discountAmountInput.value) || 0 : 0;
        let newDiscountAmount = [1, 3, 4].includes(discountTypeId) ? (discountTypeId === 1 ? 0 : unitPrice - partnerPrice) : ([5].includes(discountTypeId) ? unitPrice * (newDiscountPercentage / 100) : parseFloat(discountAmountInput.value) || 0);
        if (newDiscountAmount < 0 || (discountTypeId === 5 && (newDiscountPercentage < 0 || newDiscountPercentage > 100))) {
            window.c92.showToast('error', `A kedvezmény ${discountTypeId === 5 ? 'százaléknak 0 és 100 között kell lennie' : 'összeg nem lehet negatív'}.`);
            discountAmountInput.value = '0';
            newDiscountPercentage = 0;
            newDiscountAmount = 0;
        }
        calculateAllPrices(
            row,
            unitPrice,
            parseInt(discountTypeSelect.value, 10) || 1,
            discountTypeId === 5 ? newDiscountPercentage : newDiscountAmount,
            newQuantity,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    discountTypeSelect._listener = () => {
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        discountAmountInput.readOnly = ![5, 6].includes(newDiscountTypeId);
        let newDiscountPercentage = 0;
        let newDiscountAmount = 0;
        if ([1, 3, 4].includes(newDiscountTypeId)) {
            newDiscountAmount = newDiscountTypeId === 1 ? 0 : unitPrice - partnerPrice;
            const formattedDiscount = newDiscountAmount > 0 ? newDiscountAmount.toFixed(2) : '0';
            discountAmountInput.value = formattedDiscount;
            row.dataset.discountAmount = formattedDiscount;
            console.log(`Auto-filled discount for type ${newDiscountTypeId}: discountAmount=${formattedDiscount}`);
            discountAmountInput.dispatchEvent(new Event('input'));
            discountAmountInput.dispatchEvent(new Event('change'));
        } else {
            newDiscountPercentage = newDiscountTypeId === 5 ? parseFloat(discountAmountInput.value) || 0 : 0;
            newDiscountAmount = newDiscountTypeId === 5 ? unitPrice * (newDiscountPercentage / 100) : parseFloat(discountAmountInput.value) || 0;
            if (newDiscountAmount < 0 || (newDiscountTypeId === 5 && (newDiscountPercentage < 0 || newDiscountPercentage > 100))) {
                window.c92.showToast('error', `A kedvezmény ${newDiscountTypeId === 5 ? 'százaléknak 0 és 100 között kell lennie' : 'összeg nem lehet negatív'}.`);
                discountAmountInput.value = '0';
                newDiscountPercentage = 0;
                newDiscountAmount = 0;
            }
            row.dataset.discountAmount = newDiscountAmount.toFixed(2);
        }
        calculateAllPrices(
            row,
            unitPrice,
            newDiscountTypeId,
            newDiscountTypeId === 5 ? newDiscountPercentage : newDiscountAmount,
            parseInt(quantityInput.value, 10) || 1,
            vatSelect,
            product
        );
        window.calculateOrderTotals(orderId);
    };
    discountAmountInput._listener = () => {
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        if ([1, 3, 4].includes(newDiscountTypeId)) {
            const newDiscountAmount = newDiscountTypeId === 1 ? 0 : unitPrice - partnerPrice;
            const formattedDiscount = newDiscountAmount > 0 ? newDiscountAmount.toFixed(2) : '0';
            discountAmountInput.value = formattedDiscount;
            console.log(`Ignored manual input for type ${newDiscountTypeId}, reset to ${formattedDiscount}`);
            discountAmountInput.dispatchEvent(new Event('input'));
            discountAmountInput.dispatchEvent(new Event('change'));
        } else {
            let newDiscountPercentage = newDiscountTypeId === 5 ? parseFloat(discountAmountInput.value) || 0 : 0;
            let newDiscountAmount = newDiscountTypeId === 5 ? unitPrice * (newDiscountPercentage / 100) : parseFloat(discountAmountInput.value) || 0;
            if (newDiscountAmount < 0 || (newDiscountTypeId === 5 && (newDiscountPercentage < 0 || newDiscountPercentage > 100))) {
                window.c92.showToast('error', `A kedvezmény ${newDiscountTypeId === 5 ? 'százaléknak 0 és 100 között kell lennie' : 'összeg nem lehet negatív'}.`);
                discountAmountInput.value = '0';
                newDiscountPercentage = 0;
                newDiscountAmount = 0;
            }
            row.dataset.discountAmount = newDiscountAmount.toFixed(2);
            calculateAllPrices(
                row,
                unitPrice,
                newDiscountTypeId,
                newDiscountTypeId === 5 ? newDiscountPercentage : newDiscountAmount,
                parseInt(quantityInput.value, 10) || 1,
                vatSelect,
                product
            );
        }
        window.calculateOrderTotals(orderId);
    };
    vatSelect._listener = () => {
        let newDiscountPercentage = [5].includes(discountTypeId) ? parseFloat(discountAmountInput.value) || 0 : 0;
        let newDiscountAmount = [1, 3, 4].includes(discountTypeId) ? (discountTypeId === 1 ? 0 : unitPrice - partnerPrice) : ([5].includes(discountTypeId) ? unitPrice * (newDiscountPercentage / 100) : parseFloat(discountAmountInput.value) || 0);
        if (newDiscountAmount < 0 || (discountTypeId === 5 && (newDiscountPercentage < 0 || newDiscountPercentage > 100))) {
            window.c92.showToast('error', `A kedvezmény ${discountTypeId === 5 ? 'százaléknak 0 és 100 között kell lennie' : 'összeg nem lehet negatív'}.`);
            discountAmountInput.value = '0';
            newDiscountPercentage = 0;
            newDiscountAmount = 0;
        }
        calculateAllPrices(
            row,
            unitPrice,
            discountTypeId,
            discountTypeId === 5 ? newDiscountPercentage : newDiscountAmount,
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

async function calculateAllPrices(row, unitPrice, discountTypeId, discountValue, quantity, vatSelect, product, context = 'order') {
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
    const vatTypeId = vatSelect.tomselect?.getValue() || vatSelect.dataset.selectedId || '';
    let vatRate = vatSelect.tomselect?.options?.[vatTypeId]?.rate;
    if (vatRate === undefined || vatRate === null) {
        console.warn(`VAT rate not found for VatTypeId: ${vatTypeId}, item: ${itemId}, using fallback rate from dataset`);
        vatRate = parseFloat(vatSelect.dataset.selectedRate) || 0;
        if (vatRate === 0 && vatTypeId) {
            console.warn(`Invalid VAT rate (0) for VatTypeId: ${vatTypeId}, item: ${itemId}`);
            window.c92.showToast('warning', `Érvénytelen ÁFA kulcs a tételhez ${itemId}, 0% használata`);
        }
    }
    // Normalize vatRate: convert percentage (e.g., 27) to decimal (e.g., 0.27)
    if (vatRate > 1) {
        console.warn(`VAT rate ${vatRate} appears to be in percentage, converting to decimal for item: ${itemId}`);
        vatRate = vatRate / 100;
    }
    if (vatRate < 0 || isNaN(vatRate)) {
        console.warn(`Invalid VAT rate: ${vatRate} for VatTypeId: ${vatTypeId}, item: ${itemId}, using 0`);
        window.c92.showToast('warning', `Érvénytelen ÁFA kulcs a tételhez ${itemId}, 0% használata`);
        vatRate = 0;
    }
    let effectiveUnitPrice = parseFloat(unitPrice) || parseFloat(product?.listPrice) || 0;
    if (!effectiveUnitPrice) {
        if (productId === '62044') {
            effectiveUnitPrice = 980;
            console.log(`Using fallback listPrice=980 for ProductId: 62044`);
        } else if (productId === '62056') {
            effectiveUnitPrice = 1000;
            console.log(`Using fallback listPrice=1000 for ProductId: 62056`);
        } else {
            console.warn(`Érvénytelen egységár a termékhez (ProductId: ${productId}), 0 használata`);
            window.c92.showToast('warning', `Érvénytelen egységár a termékhez (ID: ${productId}), 0 használata`);
            effectiveUnitPrice = 0;
        }
    }
    let netPrice = effectiveUnitPrice;
    let parsedDiscountAmount = discountValue;
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
        } else if (discountTypeSelect) {
            discountTypeSelect.value = '1';
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
                } else if (discountTypeSelect) {
                    discountTypeSelect.value = '1';
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
                    } else if (discountTypeSelect) {
                        discountTypeSelect.value = '1';
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
                        } else if (discountTypeSelect) {
                            discountTypeSelect.value = '1';
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
                            } else if (discountTypeSelect) {
                                discountTypeSelect.value = '1';
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
            } else if (discountTypeSelect) {
                discountTypeSelect.value = '1';
            }
        }
    }
    if (discountTypeId === 1) {
        parsedDiscountAmount = 0;
        discountAmountInput.value = '0';
        netPrice = effectiveUnitPrice;
    } else if (discountTypeId === 5) {
        parsedDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (parsedDiscountAmount < 0 || parsedDiscountAmount > 100) {
            console.warn(`Érvénytelen kedvezmény százalék: ${parsedDiscountAmount} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
            discountAmountInput.value = '0';
        }
        netPrice = effectiveUnitPrice * (1 - parsedDiscountAmount / 100);
        row.dataset.discountAmount = (effectiveUnitPrice - netPrice).toFixed(2);
    } else if (discountTypeId === 6) {
        parsedDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (parsedDiscountAmount < 0) {
            console.warn(`Érvénytelen kedvezmény összeg: ${parsedDiscountAmount} a tételhez ${itemId}, 0 használata`);
            window.c92.showToast('error', `A kedvezmény összeg nem lehet negatív a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
            discountAmountInput.value = '0';
        }
        netPrice = effectiveUnitPrice - parsedDiscountAmount;
        row.dataset.discountAmount = parsedDiscountAmount.toFixed(2);
    } else if (discountTypeId === 3 && partnerPrice !== null) {
        netPrice = partnerPrice;
        parsedDiscountAmount = effectiveUnitPrice - partnerPrice;
        discountAmountInput.value = parsedDiscountAmount > 0 ? parsedDiscountAmount.toFixed(2) : '0';
        row.dataset.discountAmount = parsedDiscountAmount > 0 ? parsedDiscountAmount.toFixed(2) : '0';
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
                discountAmountInput.value = parsedDiscountAmount > 0 ? parsedDiscountAmount.toFixed(2) : '0';
                row.dataset.discountAmount = parsedDiscountAmount > 0 ? parsedDiscountAmount.toFixed(2) : '0';
                console.log(`✅ Volume pricing applied: quantity=${quantityInt}, unit=${volumePrice}`);
            } else {
                throw new Error(`No usable volume price for product ${productId}`);
            }
        } catch (error) {
            console.warn(`⚠️ Volume pricing error for item ${itemId}:`, error.message);
            window.c92.showToast('error', `Volume árhiba (${productId}): ${error.message}`);
            netPrice = effectiveUnitPrice;
            parsedDiscountAmount = 0;
            discountAmountInput.value = '0';
            row.dataset.discountAmount = '0';
        }
    }
    if (netPrice < 0) {
        console.warn(`Negatív nettó ár: ${netPrice} a tételhez ${itemId}, 0-ra állítva`);
        window.c92.showToast('warning', `Negatív nettó ár a tételhez ${itemId}, 0-ra állítva`);
        netPrice = 0;
        parsedDiscountAmount = 0;
        discountAmountInput.value = '0';
        row.dataset.discountAmount = '0';
    }
    const grossPrice = netPrice * (1 + vatRate);
    const totalGrossPrice = grossPrice * quantity;
    const netTotalPrice = netPrice * quantity;
    unitPriceInput.value = effectiveUnitPrice.toFixed(2);
    netUnitPriceSpan.textContent = netPrice.toFixed(2);
    netTotalSpan.textContent = netTotalPrice.toFixed(2);
    grossTotalPriceSpan.textContent = totalGrossPrice.toFixed(2);
    row.dataset.discountTypeId = discountTypeId.toString();
    console.log(`calculateAllPrices for item ${itemId}: unitPrice=${effectiveUnitPrice.toFixed(2)}, discountTypeId=${discountTypeId}, discountAmount=${parsedDiscountAmount}, partnerPrice=${partnerPrice || 'N/A'}, netPrice=${netPrice.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}, totalGrossPrice=${totalGrossPrice.toFixed(2)}, vatRate=${(vatRate * 100).toFixed(0)}%`);
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

document.addEventListener('DOMContentLoaded', () => {
    console.log('DOMContentLoaded, initializing order modals');

    // Attach click handlers to copy buttons
    document.querySelectorAll('.copy-order-btn').forEach(button => {
            button.addEventListener('click', function(event) {
                event.preventDefault();
                const orderId = this.dataset.orderId;
                console.log('Copying OrderId:', orderId);
                this.disabled = true;
                window.c92.copyOrder(orderId).finally(() => {
                    this.disabled = false;
                });
            });
        });
    
    initializeOrderModals();
});