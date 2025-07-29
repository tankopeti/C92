// Utility Functions and Initial Setup



// Placeholder for currentUsername (replace with actual user data)
const currentUsername = '@username';

// Utility function for displaying toast notifications
function showToast(type, message) {
    const toast = $(`<div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    </div>`);
    $('#toastContainer').append(toast);
    const bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();
    setTimeout(() => toast.remove(), 5500);
}

// Calculate prices for a row
async function calculateAllPrices(row, unitPrice, discountTypeId, discountAmount, quantity, vatSelect, product) {
    const itemId = row.dataset.itemId;
    const productId = product?.productId || row.querySelector('.tom-select-product')?.tomselect?.getValue();
    const netDiscountedPriceSpan = row.querySelector('.item-net-discounted-price');
    const totalSpan = row.querySelector('.item-gross-price');
    const unitPriceInput = row.querySelector('.item-unit-price');
    const discountAmountInput = row.querySelector('.discount-amount');
    if (!netDiscountedPriceSpan || !totalSpan || !unitPriceInput || !discountAmountInput) {
        console.error('Missing price fields in row:', row);
        showToast('error', 'Hiányzó ár mezők a sorban.');
        return;
    }
    const vatTypeId = vatSelect.tomselect?.getValue() || vatSelect.dataset.selectedId || '2';
    const vatRate = vatSelect.tomselect?.options?.[vatTypeId]?.rate ?? 0;
    let effectiveUnitPrice = parseFloat(unitPrice) || parseFloat(product?.listPrice) || 0;
    if (!effectiveUnitPrice) {
        console.warn(`Érvénytelen egységár a termékhez (ProductId: ${product?.productId}), 0 használata`);
        showToast('warning', `Érvénytelen egységár a termékhez (ID: ${product?.productId}), 0 használata`);
        effectiveUnitPrice = 0;
    }
    let netPrice = effectiveUnitPrice;
    let parsedDiscountAmount = discountAmountInput.value ? parseFloat(discountAmountInput.value.replace(',', '.')) : (parseFloat(discountAmount) || 0);
    if (isNaN(parsedDiscountAmount)) {
        console.warn(`Érvénytelen kedvezmény összeg a tételhez ${itemId}: ${discountAmountInput.value}, 0 használata`);
        showToast('warning', `Érvénytelen kedvezmény összeg a tételhez ${itemId}, 0 használata`);
        parsedDiscountAmount = 0;
    }
    let partnerPrice = null;
    const validDiscountTypeIds = [1, 2, 3, 4, 5, 6];
    if (!validDiscountTypeIds.includes(discountTypeId)) {
        console.warn(`Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, NoDiscount (1) használata`);
        showToast('error', `Érvénytelen kedvezmény típus: ${discountTypeId} a tételhez ${itemId}, nincs kedvezmény alkalmazva`);
        discountTypeId = 1;
        row.dataset.discountTypeId = '1';
        const discountTypeSelect = row.querySelector('.discount-type-select');
        if (discountTypeSelect?.tomselect) {
            discountTypeSelect.tomselect.setValue('1');
        }
    }
    if (discountTypeId === 3) {
        try {
            const partnerId = document.querySelector(`#quoteBaseInfoForm_${row.dataset.quoteId}`)?.querySelector('[name="partnerId"]')?.value || '5004';
            const productId = product?.productId || row.querySelector('.tom-select-product')?.tomselect?.getValue();
            if (!productId || !partnerId) {
                console.warn(`Missing productId or partnerId for PartnerPrice, item ${itemId}`);
                showToast('warning', `Hiányzó termék vagy partner azonosító a partner ár kiszámításához, tétel ${itemId}`);
                discountTypeId = 1;
                row.dataset.discountTypeId = '1';
                const discountTypeSelect = row.querySelector('.discount-type-select');
                if (discountTypeSelect?.tomselect) {
                    discountTypeSelect.tomselect.setValue('1');
                }
            } else {
                const response = await fetch(`/api/product/partner-price?partnerId=${partnerId}&productId=${productId}`);
                if (!response.ok) {
                    console.warn(`Failed to fetch partner price for product ${productId}, partner ${partnerId}: ${response.status}`);
                    showToast('warning', `Nem sikerült lekérni a partner árat a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                    discountTypeId = 1;
                    row.dataset.discountTypeId = '1';
                    const discountTypeSelect = row.querySelector('.discount-type-select');
                    if (discountTypeSelect?.tomselect) {
                        discountTypeSelect.tomselect.setValue('1');
                    }
                } else {
                    const productData = await response.json();
                    partnerPrice = productData?.partnerPrice ? parseFloat(productData.partnerPrice) : null;
                    if (!productData?.partnerPrice || productData.partnerPrice === 0) {
                        console.warn(`No valid partner price found for product ${productId}, partner ${partnerId}, using base price`);
                        showToast('warning', `Nincs érvényes partner ár a termékhez ${productId} (tétel ${itemId}), alapár használata`);
                        discountTypeId = 1;
                        row.dataset.discountTypeId = '1';
                        const discountTypeSelect = row.querySelector('.discount-type-select');
                        if (discountTypeSelect?.tomselect) {
                            discountTypeSelect.tomselect.setValue('1');
                        }
                    }
                }
            }
        } catch (error) {
            console.error(`Error fetching partner price for item ${itemId}:`, error);
            showToast('error', `Hiba a partner ár lekérése közben a tételhez ${itemId}: ${error.message}`);
            discountTypeId = 1;
            row.dataset.discountTypeId = '1';
            const discountTypeSelect = row.querySelector('.discount-type-select');
            if (discountTypeSelect?.tomselect) {
                discountTypeSelect.tomselect.setValue('1');
            }
        }
    }
    if (discountTypeId === 1 || discountTypeId === 2) {
        parsedDiscountAmount = null;
        discountAmountInput.value = '';
        netPrice = effectiveUnitPrice;
    } else if (discountTypeId === 5) {
        if (parsedDiscountAmount < 0 || parsedDiscountAmount > 100) {
            console.warn(`Érvénytelen kedvezmény százalék: ${parsedDiscountAmount} a tételhez ${itemId}, 0 használata`);
            showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
        }
        netPrice = effectiveUnitPrice * (1 - parsedDiscountAmount / 100);
    } else if (discountTypeId === 6) {
        parsedDiscountAmount = discountAmountInput.value ? parseFloat(discountAmountInput.value.replace(',', '.')) : (parseFloat(discountAmount) || 0);
        if (isNaN(parsedDiscountAmount) || parsedDiscountAmount < 0) {
            console.warn(`Érvénytelen kedvezmény összeg: ${discountAmountInput.value} a tételhez ${itemId}, 0 használata`);
            showToast('error', `A kedvezmény összeg nem lehet negatív a tételhez ${itemId}`);
            parsedDiscountAmount = 0;
        }
        netPrice = effectiveUnitPrice - parsedDiscountAmount;
    } else if (discountTypeId === 3) {
        if (partnerPrice !== null && partnerPrice !== 0) {
            netPrice = partnerPrice;
            parsedDiscountAmount = effectiveUnitPrice - partnerPrice;
            if (parsedDiscountAmount < 0) {
                console.warn(`Negative discount amount for PartnerPrice: ${parsedDiscountAmount}, item ${itemId}`);
                showToast('warning', `Negatív kedvezmény összeg a partner árnál, tétel ${itemId}`);
                parsedDiscountAmount = 0;
            }
        } else {
            netPrice = effectiveUnitPrice;
            parsedDiscountAmount = 0;
        }
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
            } else if (!isNaN(parse(data.salesPrice))) {
                volumePrice = parse(data.salesPrice);
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
            showToast('error', `Volume árhiba (${productId}): ${error.message}`);
            netPrice = effectiveUnitPrice;
            parsedDiscountAmount = 0;
        }
    }
    if (netPrice < 0) {
        console.warn(`Negatív nettó ár: ${netPrice} a tételhez ${itemId}, 0-ra állítva`);
        showToast('warning', `Negatív nettó ár a tételhez ${itemId}, 0-ra állítva`);
        netPrice = 0;
    }
    const grossPrice = netPrice * (1 + vatRate / 100);
    const totalGrossPrice = grossPrice * quantity;
    unitPriceInput.value = effectiveUnitPrice.toFixed(2);
    netDiscountedPriceSpan.textContent = netPrice.toFixed(2);
    const netTotalSpan = row.querySelector('.item-net-total');
    const netTotalPrice = netPrice * quantity;
    if (netTotalSpan) {
        netTotalSpan.textContent = netTotalPrice.toFixed(2);
    }
    totalSpan.textContent = totalGrossPrice.toFixed(2);
    row.dataset.discountTypeId = discountTypeId.toString();
    row.dataset.discountAmount = discountTypeId === 1 || discountTypeId === 2 ? '' : parsedDiscountAmount.toString();
    row.dataset.partnerPrice = discountTypeId === 3 ? partnerPrice?.toString() || '' : '';
    console.log(`calculateAllPrices for item ${itemId}: unitPrice=${effectiveUnitPrice.toFixed(2)}, discountTypeId=${discountTypeId}, discountAmount=${parsedDiscountAmount}, partnerPrice=${partnerPrice || 'N/A'}, netPrice=${netPrice.toFixed(2)}, grossPrice=${grossPrice.toFixed(2)}, totalGrossPrice=${totalGrossPrice.toFixed(2)}, vatRate=${vatRate}%`);
}

// Update price fields
function updatePriceFields(select, productId, products) {
    const row = select.closest('tr.quote-item-row');
    if (!row) {
        console.error('Row not found for select element:', select);
        showToast('error', 'Row not found.');
        return;
    }
    const unitPriceInput = row.querySelector('.item-unit-price');
    const discountTypeSelect = row.querySelector('.discount-type-id');
    const discountAmountInput = row.querySelector('.discount-amount');
    const netDiscountedPriceSpan = row.querySelector('.item-net-discounted-price');
    const vatSelect = row.querySelector('.tom-select-vat');
    const totalSpan = row.querySelector('.item-gross-price');
    const quantityInput = row.querySelector('.item-quantity');
    if (!unitPriceInput || !discountTypeSelect || !discountAmountInput || !netDiscountedPriceSpan || !vatSelect || !totalSpan || !quantityInput) {
        console.error('Missing fields in row:', row);
        showToast('error', 'Missing fields in row.');
        return;
    }
    console.log('updatePriceFields called with productId:', productId, 'products:', products);
    if (!productId && select.dataset.selectedId) {
        productId = select.dataset.selectedId;
        console.log('Using dataset.selectedId as fallback productId:', productId);
    }
    if (!productId) {
        console.log('No productId, resetting fields');
        unitPriceInput.value = '0.00';
        discountAmountInput.value = '';
        netDiscountedPriceSpan.textContent = '0.00';
        totalSpan.textContent = '0.00';
        discountTypeSelect.value = '1';
        discountAmountInput.readOnly = true;
        row.dataset.discountTypeId = '1';
        row.dataset.discountAmount = '0';
        row.dataset.volumeThreshold = '';
        row.dataset.volumePrice = '';
        calculateQuoteTotals(row.closest('table').dataset.quoteId);
        return;
    }
    let product = products.find(p => p.productId == productId);
    if (!product && select.tomselect) {
        product = select.tomselect.options[productId];
        console.log('Fetched product from TomSelect options:', product);
    }
    if (!product && select.dataset.tomSelectInitialized && !select.tomselect) {
        console.log('Product select not fully initialized, skipping field reset');
        return;
    }
    const unitPrice = product ? parseFloat(product.listPrice) || 0 : parseFloat(unitPriceInput.value) || 0;
    const quantity = parseInt(quantityInput.value, 10) || 1;
    let discountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
    let discountAmount = parseFloat(discountAmountInput.value) || 0;
    if (product) {
        unitPriceInput.value = unitPrice.toFixed(2);
        discountAmountInput.readOnly = ![5, 6].includes(discountTypeId);
        if (discountTypeId === 1) {
            discountAmountInput.value = '';
            discountAmount = 0;
        }
    } else {
        console.warn('No product data available, preserving existing values');
    }
    calculateAllPrices(row, unitPrice, discountTypeId, discountAmount, quantity, vatSelect, product || {});
    quantityInput.removeEventListener('input', quantityInput._listener);
    discountTypeSelect.removeEventListener('change', discountTypeSelect._listener);
    discountAmountInput.removeEventListener('input', discountAmountInput._listener);
    quantityInput._listener = () => {
        let newQuantity = parseInt(quantityInput.value, 10) || 1;
        if (newQuantity < 1) {
            showToast('error', 'A mennyiségnek nagyobbnak kell lennie, mint 0.');
            quantityInput.value = '1';
            newQuantity = 1;
        }
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            showToast('error', 'A kedvezmény összege nem lehet negatív.');
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
            product || {}
        );
        calculateQuoteTotals(row.closest('table').dataset.quoteId);
    };
    discountTypeSelect._listener = () => {
        const newDiscountTypeId = parseInt(discountTypeSelect.value, 10) || 1;
        discountAmountInput.readOnly = ![5, 6].includes(newDiscountTypeId);
        if (newDiscountTypeId === 1) {
            discountAmountInput.value = '';
        }
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            showToast('error', 'A kedvezmény összege nem lehet negatív.');
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
            product || {}
        );
        calculateQuoteTotals(row.closest('table').dataset.quoteId);
    };
    discountAmountInput._listener = () => {
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            showToast('error', 'A kedvezmény összege nem lehet negatív.');
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
            product || {}
        );
        calculateQuoteTotals(row.closest('table').dataset.quoteId);
    };
    quantityInput.addEventListener('input', quantityInput._listener);
    discountTypeSelect.addEventListener('change', discountTypeSelect._listener);
    discountAmountInput.addEventListener('input', discountAmountInput._listener);
    vatSelect.removeEventListener('change', vatSelect._listener);
    vatSelect._listener = () => {
        let newDiscountAmount = parseFloat(discountAmountInput.value) || 0;
        if (newDiscountAmount < 0) {
            showToast('error', 'A kedvezmény összege nem lehet negatív.');
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
            product || {}
        );
        calculateQuoteTotals(row.closest('table').dataset.quoteId);
    };
    vatSelect.addEventListener('change', vatSelect._listener);
    calculateQuoteTotals(row.closest('table').dataset.quoteId);
}

// Calculate quote totals
function calculateQuoteTotals(quoteId) {
    let totalNet = 0;
    let totalVat = 0;
    let totalGross = 0;
    let totalItemDiscounts = 0;
    const rows = document.querySelectorAll(`#items-tbody_${quoteId} .quote-item-row`);
    rows.forEach(row => {
        const quantity = parseInt(row.querySelector('.item-quantity')?.value || '1', 10);
        const netPerUnit = parseFloat(row.querySelector('.item-net-discounted-price')?.textContent || '0');
        const grossTotal = parseFloat(row.querySelector('.item-gross-price')?.textContent || '0');
        const discountAmount = parseFloat(row.dataset.discountAmount?.replace(',', '.') || '0');
        const netTotal = netPerUnit * quantity;
        totalNet += netTotal;
        totalVat += grossTotal - netTotal;
        totalGross += grossTotal;
        totalItemDiscounts += discountAmount * quantity;
    });
    const totalNetElement = document.querySelector(`#items-tbody_${quoteId} .quote-total-net`);
    const totalVatElement = document.querySelector(`#items-tbody_${quoteId} .quote-vat-amount`);
    const totalGrossElement = document.querySelector(`#items-tbody_${quoteId} .quote-gross-amount`);
    if (totalNetElement) totalNetElement.textContent = totalNet.toFixed(2);
    if (totalVatElement) totalVatElement.textContent = totalVat.toFixed(2);
    if (totalGrossElement) totalGrossElement.textContent = totalGross.toFixed(2);
    const baseInfoForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
    if (baseInfoForm) {
        const totalItemDiscountsInput = baseInfoForm.querySelector('[name="TotalItemDiscounts"]');
        if (totalItemDiscountsInput) {
            totalItemDiscountsInput.value = totalItemDiscounts.toFixed(2);
        }
    }
    return { totalNet, totalVat, totalGross, totalItemDiscounts };
}

// Debounce utility to avoid excessive recalculations
function debounce(fn, delay = 200) {
    let timeout;
    return (...args) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => fn(...args), delay);
    };
}

const debouncedCalculateTotals = debounce(calculateQuoteTotals);

// Attach live event listeners to a row
function bindRowEvents(row, quoteId) {
    const quantityInput = row.querySelector('.item-quantity');
    const discountInput = row.querySelector('.discount-amount');
    const vatSelect = row.querySelector('.tom-select-vat');
    if (quantityInput) {
        quantityInput.addEventListener('input', () => {
            calculateAllPrices(row, quantityInput.value, parseInt(row.dataset.discountTypeId), discountInput?.value, quantityInput.value, vatSelect, null);
            debouncedCalculateTotals(quoteId);
        });
    }
    if (discountInput) {
        discountInput.addEventListener('input', () => {
            calculateAllPrices(row, row.querySelector('.item-unit-price')?.value, parseInt(row.dataset.discountTypeId), discountInput.value, quantityInput?.value, vatSelect, null);
            debouncedCalculateTotals(quoteId);
        });
    }
    if (vatSelect?.tomselect) {
        vatSelect.tomselect.on('change', () => {
            calculateAllPrices(row, row.querySelector('.item-unit-price')?.value, parseInt(row.dataset.discountTypeId), discountInput?.value, quantityInput?.value, vatSelect, null);
            debouncedCalculateTotals(quoteId);
        });
    }
}


//Row and Modal Management


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
    const textarea = descriptionRow.querySelector('.item-description');
    const charCount = descriptionRow.querySelector('.char-count');
    if (textarea && charCount) {
        textarea.addEventListener('input', () => {
            charCount.textContent = textarea.value.length;
        });
    }
}

// Initialize description editing
function initializeDescriptionEditing(quoteId) {
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    tbody.addEventListener('click', function (e) {
        const button = e.target.closest('.edit-description');
        if (button) {
            const itemId = button.getAttribute('data-item-id');
            const row = document.querySelector(`tr[data-item-id="${itemId}"]`);
            if (!row) {
                console.error('Row not found for itemId:', itemId);
                return;
            }
            const descriptionInput = row.parentElement.querySelector(`textarea[name="items[${itemId}][itemDescription]"]`);
            if (!descriptionInput) {
                console.error('Description input not found for itemId:', itemId);
                return;
            }
            const currentDescription = descriptionInput.value;
            const modal = document.querySelector('#editDescriptionModal');
            if (!modal) {
                console.error('Edit description modal not found');
                return;
            }
            modal.querySelector('#editDescriptionItemId').value = itemId;
            modal.querySelector('#editDescription').value = currentDescription;
            modal.querySelector('#charCount').textContent = currentDescription.length;
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        }
    });
    const descriptionTextarea = document.querySelector('#editDescription');
    if (descriptionTextarea) {
        descriptionTextarea.addEventListener('input', function () {
            const charCount = this.value.length;
            const charCountElement = document.querySelector('#charCount');
            if (charCountElement) {
                charCountElement.textContent = charCount;
            }
        });
    }
    const saveDescriptionBtn = document.querySelector('#saveDescriptionBtn');
    if (saveDescriptionBtn) {
        saveDescriptionBtn.addEventListener('click', function () {
            const itemId = document.querySelector('#editDescriptionItemId').value;
            const newDescription = document.querySelector('#editDescription').value;
            const row = document.querySelector(`tr[data-item-id="${itemId}"]`);
            if (!row) {
                console.error('Row not found for itemId:', itemId);
                return;
            }
            const descriptionInput = row.parentElement.querySelector(`textarea[name="items[${itemId}][itemDescription]"]`);
            if (!descriptionInput) {
                console.error('Description input not found for itemId:', itemId);
                return;
            }
            descriptionInput.value = newDescription;
            const modal = document.querySelector('#editDescriptionModal');
            const bsModal = bootstrap.Modal.getInstance(modal);
            if (bsModal) {
                bsModal.hide();
            }
            calculateQuoteTotals(quoteId);
        });
    }
}

// Initialize delete buttons
function initializeDeleteButtons(quoteId) {
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    if (!tbody) {
        console.error('Tbody not found for quoteId:', quoteId);
        return;
    }
    tbody.addEventListener('click', function (event) {
        const button = event.target.closest('.remove-item');
        if (button) {
            const row = button.closest('.quote-item-row');
            if (!row) {
                console.error('Quote item row not found for button:', button);
                return;
            }
            const itemId = row.dataset.itemId;
            const descriptionRow = document.querySelector(`.description-row[data-item-id="${itemId}"]`);
            row.remove();
            if (descriptionRow) {
                descriptionRow.remove();
            }
            calculateQuoteTotals(quoteId);
        }
    });
}

// Add a new item row
async function addItemRow(quoteId) {
    initializeDeleteButtons(quoteId);
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    if (!tbody) {
        console.error('Items tbody not found for quoteId:', quoteId);
        showToast('error', 'Table not found.');
        return;
    }
    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error('Modal not found for quoteId:', quoteId);
        showToast('error', 'Modal not found.');
        return;
    }
    const partnerId = modal.dataset.partnerId;
    if (!partnerId) {
        console.error('No partnerId found for quoteId:', quoteId);
        showToast('warning', 'Please select a partner.');
        return;
    }
    const newItemId = 'new_' + Date.now();
    const itemRow = document.createElement('tr');
    itemRow.className = 'quote-item-row';
    itemRow.dataset.itemId = newItemId;
    itemRow.dataset.discountTypeId = '1';
    itemRow.dataset.discountAmount = '0';
    itemRow.dataset.volumeThreshold = '';
    itemRow.dataset.volumePrice = '';
    itemRow.innerHTML = `
        <td><select name="items[${newItemId}][productId]" class="form-select tom-select-product" data-selected-id="62044" data-selected-text="Termék 4" autocomplete="off" required></select></td>
        <td><input type="number" name="items[${newItemId}][quantity]" class="form-control form-control-sm item-quantity" value="1" min="1" step="1" required></td>
        <td><input type="number" name="items[${newItemId}][unitPrice]" class="form-control form-control-sm item-unit-price" value="0.00" min="0" step="0.01" readonly></td>
        <td>
            <select name="items[${newItemId}][discountTypeId]" class="form-select form-select-sm discount-type-id">
                <option value="1" selected>Nincs Kedvezmény</option>
                <option value="3">Ügyfélár</option>
                <option value="4">Mennyiségi kedvezmény</option>
                <option value="5">Egyedi kedvezmény %</option>
                <option value="6">Egyedi kedvezmény Összeg</option>
            </select>
        </td>
        <td><input type="number" name="items[${newItemId}][discountAmount]" class="form-control form-control-sm discount-amount" value="" min="0" step="0.01" readonly></td>
        <td><span class="item-net-discounted-price">0.00</span></td>
        <td>
            <select name="items[${newItemId}][vatTypeId]"
                    class="form-select tom-select-vat"
                    data-selected-id="1"
                    data-selected-text="27%"
                    data-selected-rate="27"
                    autocomplete="off"
                    required>
                <option value="1" selected>27%</option>
                <option value="" disabled>-- Válasszon ÁFA típust --</option>
            </select>
        </td>
        <td><span class="item-net-total">0.00</span></td>
        <td><span class="item-gross-price">0.00</span></td>
        <td>
            <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${newItemId}"><i class="bi bi-pencil"></i></button>
            <button type="button" class="btn btn-danger btn-sm remove-item" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button>
        </td>
    `;
    const descriptionRow = document.createElement('tr');
    descriptionRow.className = 'description-row';
    descriptionRow.dataset.itemId = newItemId;
    descriptionRow.style.display = 'none';
    descriptionRow.innerHTML = `
        <td colspan="9">
            <textarea name="items[${newItemId}][itemDescription]" class="form-control item-description" rows="3" maxlength="500" placeholder="Tétel leírása..."></textarea>
            <small class="char-count">0</small>/500
        </td>
    `;
    tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
    tbody.insertBefore(descriptionRow, tbody.querySelector('.quote-total-row'));
    const productSelect = itemRow.querySelector('.tom-select-product');
    const vatSelect = itemRow.querySelector('.tom-select-vat');
    if (productSelect) {
        if (typeof window.initializeProductTomSelect !== 'function') {
            console.error('initializeProductTomSelect is not defined');
            showToast('error', 'Product select initialization function missing.');
        } else {
            try {
                await window.initializeProductTomSelect(productSelect, quoteId);
                console.log('Product select initialized, tomselect:', productSelect.tomselect, 'value:', productSelect.value);
            } catch (err) {
                console.error('Failed to initialize product select:', err);
                showToast('error', 'Failed to load products: ' + err.message);
            }
        }
    }
    if (vatSelect) {
        if (typeof window.c92.initializeVatTomSelect !== 'function') {
            console.error('initializeVatTomSelect is not defined');
            showToast('error', 'VAT select initialization function missing.');
        } else {
            try {
                await window.c92.initializeVatTomSelect(vatSelect, quoteId);
                console.log('VAT select initialized, tomselect:', vatSelect.tomselect, 'value:', vatSelect.value);
            } catch (err) {
                console.error('Failed to initialize VAT select:', err);
                showToast('error', 'Failed to load VAT types: ' + err.message);
            }
        }
    }
    initializeDescriptionToggle(itemRow);
    calculateQuoteTotals(quoteId);
    bindRowEvents(itemRow, quoteId);
    initializeDeleteButtons(quoteId);
}

// Initialize event listeners
function initializeEventListeners(quoteId) {
    const modal = document.querySelector(`#${quoteId === 'new' ? 'newQuoteModal' : 'editQuoteModal_' + quoteId}`);
    if (!modal) {
        console.error('Modal not found for quoteId:', quoteId);
        showToast('error', 'Modal not found.');
        return;
    }
    const addButtons = document.querySelectorAll(`.add-item-row[data-quote-id="${quoteId}"]`);
    addButtons.forEach(button => {
        if (button.dataset.listenerAdded !== 'true') {
            button.addEventListener('click', () => {
                if (!modal.dataset.partnerId) {
                    showToast('warning', 'Please select a partner.');
                    return;
                }
                addItemRow(quoteId);
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
    const partnerSelect = modal.querySelector('select[name="partnerId"]');
    if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
        window.initializePartnerTomSelect(partnerSelect, quoteId);
    }
    const currencySelect = modal.querySelector('select[name="currencyId"]');
    if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
        window.c92.initializeCurrencyTomSelect(currencySelect);
    }
    modal.addEventListener('show.bs.modal', () => {
        document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-product`).forEach(productSelect => {
            if (!productSelect.dataset.tomSelectInitialized) {
                window.initializeProductTomSelect(productSelect, quoteId);
            }
        });
        document.querySelectorAll(`#items-tbody_${quoteId} .tom-select-vat`).forEach(vatSelect => {
            if (!vatSelect.dataset.tomSelectInitialized) {
                window.c92.initializeVatTomSelect(vatSelect, quoteId);
            }
        });
        if (partnerSelect && partnerSelect.tomselect) {
            modal.dataset.partnerId = partnerSelect.tomselect.getValue() || partnerSelect.dataset.selectedId || '5004';
        }
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
        modal.dataset.partnerId = '';
        const tbody = document.querySelector(`#items-tbody_${quoteId}`);
        if (tbody) {
            tbody.querySelectorAll('tr:not(.quote-total-row)').forEach(row => row.remove());
        }
    });
}

// Refresh all rows
function refreshAllRows(quoteId) {
    const tbody = document.querySelector(`#items-tbody_${quoteId}`);
    if (!tbody) {
        console.error('Tbody not found for quoteId:', quoteId);
        return;
    }
    const rows = tbody.querySelectorAll('tr.quote-item-row');
    rows.forEach(row => {
        const productSelect = row.querySelector('.tom-select-product');
        const vatSelect = row.querySelector('.tom-select-vat');
        if (productSelect) {
            window.initializeProductTomSelect(productSelect, quoteId);
        }
        if (vatSelect) {
            window.c92.initializeVatTomSelect(vatSelect, quoteId);
        }
    });
    calculateQuoteTotals(quoteId);
}

// Get current VAT percentage
function getCurrentVatPercentage(vatSelect) {
    const selectedVatId = vatSelect.tomselect ? vatSelect.tomselect.getValue() : null;
    const selectedOption = vatSelect.tomselect ? vatSelect.tomselect.options[selectedVatId] : null;
    return selectedOption ? parseFloat(selectedOption.rate) || 0 : 0;
}

//Form Validation, Save, and Modal Initialization

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
        if (field.name.includes('.Quantity') || field.name.includes('.UnitPrice')) {
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
    const discountAmountInput = row.querySelector('.discount-amount');
    const unitPriceInput = row.querySelector('.item-unit-price');
    const discountAmount = parseFloat(discountAmountInput?.value.replace(',', '.') || '0');
    const listPrice = parseFloat(unitPriceInput?.value.replace(',', '.') || '0');
    if (!unitPriceInput || !discountAmountInput || isNaN(discountTypeId)) {
        console.warn(`❌ Missing required fields or invalid discount type in row ${itemId}`);
        showToast('error', `Hiányzó vagy hibás mezők (${itemId})`);
        return false;
    }
    if (discountTypeId === 5 && (discountAmount < 0 || discountAmount > 100)) {
        showToast('error', `A kedvezmény százaléknak 0 és 100 között kell lennie (tétel: ${itemId})`);
        return false;
    }
    if (discountTypeId === 6) {
        if (discountAmount <= 0) {
            showToast('error', `A kedvezmény összegnek nagyobbnak kell lennie 0-nál (tétel: ${itemId})`);
            return false;
        }
        if (discountAmount >= listPrice) {
            showToast('error', `A kedvezmény összeg nem lehet nagyobb vagy egyenlő a listaárral (tétel: ${itemId})`);
            return false;
        }
    }
    console.log(`✅ validateRow passed: item ${itemId}, type=${discountTypeId}, amount=${discountAmount}, listPrice=${listPrice}`);
    return true;
}

// Save quote
async function saveQuote(quoteId) {
    const baseForm = document.querySelector(`#quoteBaseInfoForm_${quoteId}`);
    const itemsForm = document.querySelector(`#quoteItemsForm_${quoteId}`);
    const totals = calculateQuoteTotals(quoteId);
    const baseInfoTab = document.querySelector(`#base-info-tab_${quoteId}`);
    if (baseInfoTab) {
        baseInfoTab.click();
    }
    const baseErrors = validateForm(baseForm, quoteId);
    const itemErrors = validateForm(itemsForm, quoteId);
    if (baseErrors.length > 0 || itemErrors.length > 0) {
        const allErrors = [...baseErrors, ...itemErrors];
        showToast('error', 'Kérjük, töltse ki az összes kötelező mezőt megfelelően:\n' + allErrors.join('\n'));
        return;
    }
    const baseData = new FormData(baseForm);
    const itemsData = new FormData(itemsForm);
    const currencyId = baseData.get('currencyId');
    if (!currencyId || isNaN(parseInt(currencyId))) {
        showToast('error', 'Kérjük, válasszon pénznemet.');
        return;
    }
    const partnerId = baseData.get('partnerId');
    if (!partnerId || isNaN(parseInt(partnerId))) {
        showToast('error', 'Kérjük, válasszon partnert.');
        return;
    }
    const subject = baseData.get('subject');
    if (!subject || subject.trim() === '') {
        showToast('error', 'Kérjük, adja meg az árajánlat tárgyát.');
        return;
    }
    const status = baseData.get('status');
    if (!status || status.trim() === '') {
        showToast('error', 'Kérjük, válasszon státuszt.');
        return;
    }
    const quoteNumber = `QUOTE-${Date.now()}`;
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
        PartnerId: parseInt(baseData.get('partnerId')),
        CurrencyId: parseInt(currencyId),
        QuoteDate: baseData.get('quoteDate') || null,
        Status: statusMapping[baseData.get('status')] || 'Draft',
        TotalAmount: totals.totalNet,
        SalesPerson: baseData.get('salesPerson') || null,
        ValidityDate: baseData.get('validityDate') || null,
        Subject: subject,
        Description: baseData.get('description') || null,
        DetailedDescription: baseData.get('detailedDescription') || null,
        DiscountPercentage: null,
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
        const productId = itemsData.get(`items[${itemId}][productId]`);
        if (!productId) {
            console.warn(`Skipping item ${itemId}: No productId`);
            showToast('warning', `Tétel ${itemId} kihagyva: nincs termék azonosító.`);
            continue;
        }
        let vatTypeId = parseInt(itemsData.get(`items[${itemId}][vatTypeId]`));
        if (!vatTypeId || isNaN(vatTypeId) || !validVatTypeIds.includes(vatTypeId)) {
            console.warn(`Invalid vatTypeId for item ${itemId}: ${vatTypeId}, using default: 2`);
            showToast('warning', `Érvénytelen ÁFA típus tételnél ${itemId}, alapértelmezett 0% használata.`);
            vatTypeId = 2;
        }
        const quantity = parseFloat(itemsData.get(`items[${itemId}][quantity]`)) || 0;
        const unitPrice = parseFloat(itemsData.get(`items[${itemId}][unitPrice]`)) || 0;
        if (quantity <= 0) {
            showToast('error', 'A mennyiségnek pozitívnak kell lennie.');
            return;
        }
        if (unitPrice < 0) {
            showToast('error', 'Az egységár nem lehet negatív.');
            return;
        }
        const discountTypeId = parseInt(row.dataset.discountTypeId, 10) || 1;
        if (discountTypeId < 1 || discountTypeId > 6) {
            showToast('error', 'A kedvezmény típusa 1 és 6 között kell legyen.');
            return;
        }
        const netPrice = parseFloat(row.querySelector('.item-net-discounted-price').textContent) || 0;
        const totalPrice = parseFloat(row.querySelector('.item-gross-price').textContent) || 0;
        let discountAmount = null;
        if (discountTypeId !== 1) {
            const discountAmountValue = row.dataset.discountAmount || itemsData.get(`items[${itemId}][discountAmount]`);
            discountAmount = discountAmountValue && discountAmountValue !== '' ? parseFloat(discountAmountValue) : 0;
        }
        const volumeThreshold = parseInt(row.dataset.volumeThreshold) || null;
        const volumePrice = parseFloat(row.dataset.volumePrice) || null;
        const item = {
            QuoteId: 0,
            QuoteItemId: itemId.startsWith('new_') ? 0 : parseInt(itemId),
            ProductId: parseInt(productId),
            VatTypeId: vatTypeId,
            ItemDescription: itemsData.get(`items[${itemId}][itemDescription]`) || null,
            Quantity: quantity,
            NetDiscountedPrice: netPrice,
            TotalPrice: totalPrice,
            DiscountTypeId: discountTypeId,
            DiscountAmount: discountAmount,
            PartnerPrice: discountTypeId === 3 ? netPrice : null,
            BasePrice: discountTypeId === 1 ? unitPrice : null,
            ListPrice: unitPrice,
            DiscountPercentage: discountTypeId === 5 ? parseFloat(itemsData.get(`items[${itemId}][discountAmount]`)) || 0 : null,
            VolumeThreshold: discountTypeId === 4 ? volumeThreshold : null,
            VolumePrice: discountTypeId === 4 ? volumePrice : null
        };
        quoteItems.push({ item, isNew: itemId.startsWith('new_') });
        quoteDto.Items.push(item);
    }
    if (quoteItems.length === 0) {
        showToast('error', 'Legalább egy tétel szükséges az árajánlathoz.');
        return;
    }
    quoteDto.TotalItemDiscounts = totals.totalItemDiscounts;
    quoteDto.TotalAmount = totals.totalNet;
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
            const newQuoteId = data.quoteId || data.QuoteId;
            showToast('success', 'Árajánlat sikeresen létrehozva!');
            const modal = document.getElementById('newQuoteModal');
            bootstrap.Modal.getInstance(modal)?.hide();
            window.location.reload();
        } catch (error) {
            console.error('Save error for quoteId:', quoteId, error);
            showToast('error', 'Hiba történt az árajánlat létrehozása közben: ' + error.message);
        }
    } else {
        const itemPromises = quoteItems.map(async ({ item, isNew }, index) => {
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
                method: method, // Fixed: Use dynamic method instead of 'method'
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
            const data = await response.json();
            showToast('success', 'Árajánlat sikeresen mentve!');
            const modal = document.getElementById(`editQuoteModal_${quoteId}`);
            bootstrap.Modal.getInstance(modal).hide();
            window.location.reload();
        } catch (error) {
            console.error('Save error for quoteId:', quoteId, error);
            showToast('error', 'Hiba történt az árajánlat mentése közben: ' + error.message);
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
                showToast('error', 'Érvénytelen árajánlat azonosító.');
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
                    showToast('success', `Árajánlat sikeresen másolva! Új szám: ${quoteNumber}`);
                    setTimeout(() => location.reload(), 3000);
                })
                .catch(error => {
                    console.error('Copy quote error:', error);
                    showToast('error', 'Hiba történt a másolás során: ' + error.message);
                });
        });
    });
}

// Populate edit quote modal
async function populateEditQuoteModal(quoteId) {
    fetch(`/api/Quotes/${quoteId}`, {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('token'),
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`Failed to fetch quote: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            const quote = data;
            const modal = document.getElementById(`editQuoteModal_${quoteId}`);
            if (!modal) {
                console.error(`Modal #editQuoteModal_${quoteId} not found`);
                showToast('error', 'Modal not found.');
                return;
            }
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="quoteNumber"]`).value = quote.QuoteNumber || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="partnerId"]`).value = quote.PartnerId || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="currencyId"]`).value = quote.CurrencyId || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="quoteDate"]`).value = quote.QuoteDate ? quote.QuoteDate.split('T')[0] : '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="status"]`).value = quote.Status || 'Draft';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="salesPerson"]`).value = quote.SalesPerson || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="validityDate"]`).value = quote.ValidityDate ? quote.QuoteDate.split('T')[0] : '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="subject"]`).value = quote.Subject || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="description"]`).value = quote.Description || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="detailedDescription"]`).value = quote.DetailedDescription || '';
            modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="TotalItemDiscounts"]`).value = quote.TotalItemDiscounts || 0;
            const partnerSelect = modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="partnerId"]`);
            const currencySelect = modal.querySelector(`#quoteBaseInfoForm_${quoteId} [name="currencyId"]`);
            if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
                window.initializePartnerTomSelect(partnerSelect, quoteId);
            }
            if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
                window.c92.initializeCurrencyTomSelect(currencySelect);
            }
            const tbody = modal.querySelector(`#items-tbody_${quoteId}`);
            tbody.innerHTML = '';
            quote.Items.forEach(item => {
                const newItemId = item.QuoteItemId || 'new_' + Date.now();
                const itemRow = document.createElement('tr');
                itemRow.className = 'quote-item-row';
                itemRow.dataset.itemId = newItemId;
                itemRow.dataset.discountTypeId = item.DiscountTypeId || '1';
                itemRow.dataset.discountAmount = item.DiscountAmount || '0';
                itemRow.innerHTML = `
                    <td><select name="items[${newItemId}][productId]" class="form-select tom-select-product" data-selected-id="${item.ProductId}" data-selected-text="${item.ProductName || ''}" autocomplete="off" required></select></td>
                    <td><input type="number" name="items[${newItemId}][quantity]" class="form-control form-control-sm item-quantity" value="${item.Quantity}" min="1" step="1" required></td>
                    <td><input type="number" name="items[${newItemId}][unitPrice]" class="form-control form-control-sm item-unit-price" value="${item.ListPrice || '0.00'}" min="0" step="0.01" readonly></td>
                    <td>
                        <select name="items[${newItemId}][discountTypeId]" class="form-select form-select-sm discount-type-id">
                            <option value="1" ${item.DiscountTypeId === 1 ? 'selected' : ''}>Nincs Kedvezmény</option>
                            <option value="3" ${item.DiscountTypeId === 3 ? 'selected' : ''}>Ügyfélár</option>
                            <option value="4" ${item.DiscountTypeId === 4 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                            <option value="5" ${item.DiscountTypeId === 5 ? 'selected' : ''}>Egyedi kedvezmény %</option>
                            <option value="6" ${item.DiscountTypeId === 6 ? 'selected' : ''}>Egyedi kedvezmény Összeg</option>
                        </select>
                    </td>
                    <td><input type="number" name="items[${newItemId}][discountAmount]" class="form-control form-control-sm discount-amount" value="${item.DiscountAmount || ''}" min="0" step="0.01" ${[5, 6].includes(item.DiscountTypeId) ? '' : 'readonly'}></td>
                    <td><span class="item-net-discounted-price">${item.NetDiscountedPrice || '0.00'}</span></td>
                    <td><input type="text" name="items[${newItemId}][vatTypeId]" class="form-input tom-select-vat" data-selected-id="${item.VatTypeId}" data-selected-text="${item.VatTypeName || ''}" autocomplete="off"></td>
                    <td><span class="item-gross-price">${item.TotalPrice || '0.00'}</span></td>
                    <td>
                        <button type="button" class="btn btn-outline-secondary btn-sm edit-description" data-item-id="${newItemId}"><i class="bi bi-pencil"></i></button>
                        <button type="button" class="btn btn-danger btn-sm remove-item" data-item-id="${newItemId}"><i class="bi bi-trash"></i></button>
                    </td>
                `;
                const descriptionRow = document.createElement('tr');
                descriptionRow.className = 'description-row';
                descriptionRow.dataset.itemId = newItemId;
                descriptionRow.style.display = 'none';
                descriptionRow.innerHTML = `
                    <td colspan="9">
                        <textarea name="items[${newItemId}][itemDescription]" class="form-control item-description" rows="3" maxlength="500" placeholder="Tétel leírása...">${item.ItemDescription || ''}</textarea>
                        <small class="char-count">${(item.ItemDescription || '').length}</small>/500
                    </td>
                `;
                tbody.insertBefore(itemRow, tbody.querySelector('.quote-total-row'));
                tbody.insertBefore(descriptionRow, tbody.querySelector('.quote-total-row'));
                const productSelect = itemRow.querySelector('.tom-select-product');
                const vatSelect = itemRow.querySelector('.tom-select-vat');
                if (productSelect) window.initializeProductTomSelect(productSelect, quoteId);
                if (vatSelect) window.c92.initializeVatTomSelect(vatSelect, quoteId);
            });
            initializeEventListeners(quoteId);
            calculateQuoteTotals(quoteId);
        })
        .catch(error => {
            console.error('Error fetching quote:', error.message);
            showToast('error', 'Failed to load quote data: ' + error.message);
        });
}

// Initialize modals
function initializeModals() {
    // Placeholder for additional modal initialization if needed
}

// DOMContentLoaded event listener
document.addEventListener('DOMContentLoaded', function () {
    const newQuoteModal = document.getElementById('newQuoteModal');
    if (newQuoteModal) {
        newQuoteModal.addEventListener('shown.bs.modal', async function () {
            console.log('newQuoteModal shown, initializing for quoteId: new');
            initializeEventListeners('new');
            calculateQuoteTotals('new');
            const baseInfoForm = document.getElementById('quoteBaseInfoForm_new');
            if (baseInfoForm) {
                const partnerSelect = baseInfoForm.querySelector('[name="partnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="currencyId"]');
                if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
                    await window.c92.initializeCurrencyTomSelect(currencySelect).catch(err => {
                        console.error('Failed to initialize currency select:', err);
                        showToast('error', 'Failed to initialize currency dropdown: ' + err.message);
                    });
                }
                if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
                    await window.initializePartnerTomSelect(partnerSelect, 'new').catch(err => {
                        console.error('Failed to initialize partner select:', err);
                        showToast('error', 'Failed to initialize partner dropdown: ' + err.message);
                    });
                }
            }
            const tbody = document.getElementById('items-tbody_new');
            if (tbody && !tbody.querySelector('.quote-item-row')) {
                addItemRow('new');
            }
        });
        newQuoteModal.addEventListener('hidden.bs.modal', function () {
            const baseInfoForm = document.getElementById('quoteBaseInfoForm_new');
            if (baseInfoForm) {
                baseInfoForm.reset();
                baseInfoForm.querySelector('[name="quoteDate"]').value = new Date().toISOString().split('T')[0];
                baseInfoForm.querySelector('[name="validityDate"]').value = new Date(new Date().setDate(new Date().getDate() + 30)).toISOString().split('T')[0];
                baseInfoForm.querySelector('[name="status"]').value = 'Draft';
                const partnerSelect = baseInfoForm.querySelector('[name="partnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="currencyId"]');
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
                    addItemRow('new');
                }
            }
        });
    }
    document.querySelectorAll(".confirm-delete-quote").forEach(button => {
        button.addEventListener("click", function (event) {
            const quoteId = event.target.dataset.quoteId;
            if (!quoteId) {
                console.error("Missing quoteId on delete button");
                return;
            }
            const confirmed = confirm(`Biztosan törölni szeretné a következő árajánlatot: QUOTE-${quoteId}?`);
            if (!confirmed) {
                return;
            }
            fetch(`/api/quotes/${quoteId}`, {
                method: 'DELETE'
            })
                .then(response => {
                    if (response.ok) {
                        alert("Árajánlat sikeresen törölve.");
                        location.reload();
                    } else if (response.status === 404) {
                        alert("Az árajánlat nem található.");
                    } else {
                        alert("Hiba történt az árajánlat törlése során.");
                    }
                })
                .catch(error => {
                    console.error("Error during deletion:", error);
                    alert("Hálózati hiba történt.");
                });
        });
    });
    document.querySelectorAll('[id^="editQuoteModal_"]').forEach(modal => {
        modal.addEventListener('shown.bs.modal', function () {
            const quoteId = this.id.split('_')[1];
            populateEditQuoteModal(quoteId);
            const baseInfoForm = document.getElementById(`quoteBaseInfoForm_${quoteId}`);
            if (baseInfoForm) {
                const partnerSelect = baseInfoForm.querySelector('[name="partnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="currencyId"]');
                if (partnerSelect && !partnerSelect.dataset.tomSelectInitialized) {
                    window.initializePartnerTomSelect(partnerSelect, quoteId);
                }
                if (currencySelect && !currencySelect.dataset.tomSelectInitialized) {
                    window.c92.initializeCurrencyTomSelect(currencySelect);
                }
            }
            calculateQuoteTotals(quoteId);
        });
        modal.addEventListener('hidden.bs.modal', function () {
            const quoteId = this.id.split('_')[1];
            const baseInfoForm = document.getElementById(`quoteBaseInfoForm_${quoteId}`);
            if (baseInfoForm) {
                const partnerSelect = baseInfoForm.querySelector('[name="partnerId"]');
                const currencySelect = baseInfoForm.querySelector('[name="currencyId"]');
                if (partnerSelect?.tomselect) {
                    partnerSelect.tomselect.destroy();
                    partnerSelect.dataset.tomSelectInitialized = '';
                }
                if (currencySelect?.tomselect) {
                    currencySelect.tomselect.destroy();
                    currencySelect.dataset.tomSelectInitialized = '';
                }
            }
            const itemsForm = document.getElementById(`quoteItemsForm_${quoteId}`);
            if (itemsForm) {
                const tbody = document.getElementById(`items-tbody_${quoteId}`);
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
                }
            }
        });
    });
    document.querySelectorAll('.convert-quote').forEach(button => {
        button.addEventListener('click', async function () {
            const quoteId = this.dataset.quoteId;
            const modal = document.getElementById(`convertQuoteModal_${quoteId}`);
            const form = document.getElementById(`convertQuoteForm_${quoteId}`);
            const convertDto = {
                currencyId: parseInt(form.querySelector('[name="currency"]').value),
                siteId: null,
                paymentTerms: "",
                shippingMethod: "",
                orderType: ""
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
                    showToast('success', `Order created: #${result.orderNumber}`);
                    bootstrap.Modal.getInstance(modal)?.hide();
                } else {
                    const error = await response.json();
                    showToast('error', error.error || 'Conversion failed');
                }
            } catch (err) {
                console.error('Conversion error:', err);
                showToast('error', 'Unexpected error occurred');
            }
        });
    });
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
    initializeFilterDropdown();
    document.querySelectorAll('select[name="currencyId"], .currency-select').forEach(window.c92.initializeCurrencyTomSelect);
    document.querySelectorAll('input[name$="VatTypeId"], .tom-select-vat').forEach(vatSelect => window.c92.initializeVatTomSelect(vatSelect, null));
    initializeModals();
    initializeCopyQuote();
});