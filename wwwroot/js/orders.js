document.addEventListener('DOMContentLoaded', function () {
    console.log('orderManagement.js loaded');

    // Initialize TomSelect for dropdowns
    function initializeTomSelect(selectElement, endpoint, valueField = 'id', labelField = 'text', getDynamicParams = null, openOnFocus = true, preload = true, minChars = 0) {
        try {
            return new TomSelect(selectElement, {
                valueField: valueField,
                labelField: labelField,
                searchField: [labelField],
                placeholder: 'Válasszon...',
                allowEmptyOption: selectElement.id.includes('partnerIdSelect') || selectElement.id.includes('currencyIdSelect') ? false : true,
                openOnFocus: openOnFocus,
                preload: preload,
                shouldLoad: (query) => query.length >= minChars,
                load: function (query, callback) {
                    let url = endpoint;
                    if (getDynamicParams) {
                        url = getDynamicParams(query);
                        if (!url) {
                            console.warn(`No URL provided for ${selectElement.id}, skipping fetch`);
                            callback([]);
                            return;
                        }
                    } else if (query) {
                        url += `${url.includes('?') ? '&' : '?'}search=${encodeURIComponent(query)}`;
                    }
                    if (selectElement.id.includes('productIdSelect')) {
                        const partnerId = document.querySelector('#partnerIdSelect')?.value;
                        if (partnerId) {
                            url += `${url.includes('?') ? '&' : '?'}partnerId=${encodeURIComponent(partnerId)}`;
                        }
                    }
                    console.log(`Fetching data from ${url}`);
                    fetch(url, {
                        headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                    })
                        .then(response => {
                            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                            return response.json();
                        })
                        .then(data => {
                            const mappedData = Array.isArray(data) ? data.map(item => {
                                const id = item.productId || item.ProductId || item.id || item.OrderStatusId;
                                const text = item.name || item.Name || item.text;
                                if (!id || !text) {
                                    console.warn(`Invalid item in response for ${selectElement.id}:`, item);
                                    return null;
                                }
                                return { id, text };
                            }).filter(item => item !== null) : [];
                            console.log(`Data fetched for ${selectElement.id}:`, mappedData);
                            callback(mappedData);
                        })
                        .catch(error => {
                            console.error(`Error fetching data from ${url}:`, error);
                            document.getElementById('errorMessage').textContent = `Hiba az adatok betöltése során (${selectElement.id}): ${error.message}`;
                            document.getElementById('errorContainer').classList.remove('d-none');
                            callback([]);
                        });
                },
                onChange: function (value) {
                    if (selectElement.id.includes('productIdSelect') && value) {
                        const itemIndex = selectElement.id.match(/\d+$/)[0];
                        const currencyId = document.querySelector('#currencyIdSelect')?.value;
                        const partnerId = document.querySelector('#partnerIdSelect')?.value;
                        if (!currencyId || !partnerId) {
                            console.warn('CurrencyId or PartnerId not selected, cannot fetch prices');
                            return;
                        }
                        // Fetch PartnerProductPrice
                        const partnerPriceUrl = `/api/PartnerProductPrice/partner/${encodeURIComponent(partnerId)}/product/${encodeURIComponent(value)}`;
                        console.log(`Fetching PartnerUnitPrice for PartnerId ${partnerId} and ProductId ${value} from ${partnerPriceUrl}`);
                        fetch(partnerPriceUrl, {
                            headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                        })
                            .then(response => {
                                if (!response.ok) {
                                    if (response.status === 404) {
                                        console.log(`No PartnerProductPrice found for PartnerId ${partnerId} and ProductId ${value}`);
                                        return null;
                                    }
                                    return response.text().then(text => {
                                        throw new Error(`HTTP error! status: ${response.status}, response: ${text}`);
                                    });
                                }
                                return response.json();
                            })
                            .then(partnerPrice => {
                                const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                                if (partnerPrice && partnerPrice.partnerUnitPrice !== undefined) {
                                    partnerPriceInput.value = parseFloat(partnerPrice.partnerUnitPrice).toFixed(2);
                                    console.log(`Set PartnerPrice to ${partnerPrice.partnerUnitPrice} for PartnerId ${partnerId} and ProductId ${value}`);
                                } else {
                                    partnerPriceInput.value = '';
                                    console.log(`No PartnerProductPrice found for PartnerId ${partnerId} and ProductId ${value}`);
                                }
                            })
                            .catch(error => {
                                console.error(`Error fetching PartnerUnitPrice: ${error.message}`);
                                document.getElementById('errorMessage').textContent = `Hiba a partner ár betöltése során: ${error.message}`;
                                document.getElementById('errorContainer').classList.remove('d-none');
                            });
                        // Fetch ProductPrice for ListPrice, UnitPrice, and VolumePrice
                        const productPriceUrl = `/api/ProductPrice?productId=${encodeURIComponent(value)}&currencyId=${encodeURIComponent(currencyId)}&isActive=true`;
                        console.log(`Fetching ProductPrice for ProductId ${value} and CurrencyId ${currencyId} from ${productPriceUrl}`);
                        fetch(productPriceUrl, {
                            headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                        })
                            .then(response => {
                                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                                return response.json();
                            })
                            .then(data => {
                                const productPrice = Array.isArray(data) && data.length > 0 ? data[0] : null;
                                const listPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                                const unitPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
                                const volumePriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
                                const quantityInput = document.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
                                const discountTypeSelect = document.querySelector(`select[name="OrderItems[${itemIndex}].DiscountType"]`);
                                const discountAmountInput = document.querySelector(`input[name="OrderItems[${itemIndex}].DiscountAmount"]`);
                                const discountPercentageInput = document.querySelector(`input[name="OrderItems[${itemIndex}].DiscountPercentage"]`);
                                const basePriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].BasePrice"]`);
                                if (productPrice && productPrice.salesPrice !== undefined) {
                                    listPriceInput.value = parseFloat(productPrice.salesPrice).toFixed(2);
                                    console.log(`Set ListPrice to ${productPrice.salesPrice} for ProductId ${value}`);
                                    // Fetch volume price based on current quantity
                                    const quantity = parseFloat(quantityInput.value) || 0;
                                    const discountAmount = parseFloat(discountAmountInput.value) || 0;
                                    const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                                    const basePrice = parseFloat(basePriceInput.value) || 0;
                                    if (productPrice.productPriceId && quantity > 0) {
                                        const volumeUrl = `/api/ProductPrice/${productPrice.productPriceId}/volume-price?quantity=${quantity}`;
                                        console.log(`Fetching VolumePrice for ProductPriceId ${productPrice.productPriceId} and Quantity ${quantity} from ${volumeUrl}`);
                                        fetch(volumeUrl, {
                                            headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                                        })
                                            .then(response => {
                                                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                                                return response.json();
                                            })
                                            .then(volumePrice => {
                                                volumePriceInput.value = parseFloat(volumePrice).toFixed(2);
                                                console.log(`Set VolumePrice to ${volumePrice} for ProductId ${productId} and Quantity ${quantity}`);
                                                // Update UnitPrice based on BasePrice or DiscountType
                                                const discountType = discountTypeSelect.value;
                                                const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                                                if (basePrice > 0) {
                                                    unitPriceInput.value = (basePrice * quantity).toFixed(2);
                                                    console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                                                    // Disable DiscountAmount and DiscountPercentage
                                                    discountAmountInput.disabled = true;
                                                    discountPercentageInput.disabled = true;
                                                } else {
                                                    discountAmountInput.disabled = discountPercentage > 0;
                                                    discountPercentageInput.disabled = discountAmount > 0;
                                                    if (discountType === '1' || discountType === '6') {
                                                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                                    } else if (discountType === '2') {
                                                        unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                                                        console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                                    } else if (discountType === '3') {
                                                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                                    } else if (discountType === '5') {
                                                        unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                                                        console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                                    } else if (discountType === '4') {
                                                        unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                        console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                                    } else {
                                                        unitPriceInput.value = '';
                                                        console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                                                    }
                                                }
                                                updateTotalAmount();
                                            })
                                            .catch(error => {
                                                console.error(`Error fetching VolumePrice: ${error}`);
                                                document.getElementById('errorMessage').textContent = `Hiba a mennyiségi ár betöltése során: ${error.message}`;
                                                document.getElementById('errorContainer').classList.remove('d-none');
                                                unitPriceInput.value = '';
                                                volumePriceInput.value = '';
                                                updateTotalAmount();
                                            });
                                    } else {
                                        volumePriceInput.value = parseFloat(productPrice.salesPrice).toFixed(2);
                                        // Update UnitPrice based on BasePrice or DiscountType
                                        const discountType = discountTypeSelect.value;
                                        const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                                        if (basePrice > 0) {
                                            unitPriceInput.value = (basePrice * quantity).toFixed(2);
                                            console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                                            // Disable DiscountAmount and DiscountPercentage
                                            discountAmountInput.disabled = true;
                                            discountPercentageInput.disabled = true;
                                        } else {
                                            discountAmountInput.disabled = discountPercentage > 0;
                                            discountPercentageInput.disabled = discountAmount > 0;
                                            if (discountType === '1' || discountType === '6') {
                                                unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                            } else if (discountType === '2') {
                                                unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                                                console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                            } else if (discountType === '3') {
                                                unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                            } else if (discountType === '5') {
                                                unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                                                console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                            } else if (discountType === '4') {
                                                unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                                console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                            } else {
                                                unitPriceInput.value = '';
                                                console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                                            }
                                        }
                                        console.log(`Set UnitPrice and VolumePrice to ${productPrice.salesPrice} (no quantity or ProductPriceId)`);
                                        updateTotalAmount();
                                    }
                                } else {
                                    listPriceInput.value = '';
                                    unitPriceInput.value = '';
                                    volumePriceInput.value = '';
                                    console.warn(`No valid ProductPrice found for ProductId ${value} and CurrencyId ${currencyId}`);
                                    updateTotalAmount();
                                }
                            })
                            .catch(error => {
                                console.error(`Error fetching ProductPrice: ${error}`);
                                document.getElementById('errorMessage').textContent = `Hiba a listaár betöltése során: ${error.message}`;
                                document.getElementById('errorContainer').classList.remove('d-none');
                            });
                    }
                },
                render: {
                    option: function (item, escape) {
                        return `<div>${escape(item.text)}</div>`;
                    },
                    item: function (item, escape) {
                        return `<div>${escape(item.text)}</div>`;
                    }
                }
            });
        } catch (e) {
            console.error(`Error initializing TomSelect for ${selectElement.id}:`, e);
            document.getElementById('errorMessage').textContent = `Hiba a dropdown inicializálása során (${selectElement.id}): ${e.message}`;
            document.getElementById('errorContainer').classList.remove('d-none');
            return null;
        }
    }

    // Initialize dropdowns
    const partnerSelect = document.querySelector('#partnerIdSelect');
    let partnerTomSelect;
    if (partnerSelect) {
        console.log('Initializing PartnerId dropdown');
        partnerTomSelect = initializeTomSelect(partnerSelect, '/api/partners/select', 'id', 'text');
    }

    const currencySelect = document.querySelector('#currencyIdSelect');
    let currencyTomSelect;
    if (currencySelect) {
        console.log('Initializing CurrencyId dropdown');
        currencyTomSelect = initializeTomSelect(currencySelect, '/api/currencies', 'id', 'text');
    }

    const contactSelect = document.querySelector('#contactIdSelect');
    if (contactSelect) {
        console.log('Initializing ContactId dropdown');
        const getContactEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/partners/${partnerId}/contacts/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(contactSelect, '', 'id', 'text', getContactEndpoint);
    }

    const siteSelect = document.querySelector('#siteIdSelect');
    if (siteSelect) {
        console.log('Initializing SiteId dropdown');
        const getSiteEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/partners/${partnerId}/sites/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(siteSelect, '', 'id', 'text', getSiteEndpoint);
    }

    const shippingMethodSelect = document.querySelector('#shippingMethodIdSelect');
    if (shippingMethodSelect) {
        console.log('Initializing ShippingMethodId dropdown');
        initializeTomSelect(shippingMethodSelect, '/api/ordershippingmethods/select', 'id', 'text');
    }

    const paymentTermSelect = document.querySelector('#paymentTermIdSelect');
    if (paymentTermSelect) {
        console.log('Initializing PaymentTermId dropdown');
        initializeTomSelect(paymentTermSelect, '/api/paymentterms/select', 'id', 'text');
    }

    const quoteSelect = document.querySelector('#quoteIdSelect');
    if (quoteSelect) {
        console.log('Initializing QuoteId dropdown');
        const getQuoteEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/quotes/select?partnerId=${partnerId}${query ? `?search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
    }

    const orderStatusTypesSelect = document.querySelector('#orderStatusTypesSelect');
    if (orderStatusTypesSelect) {
        console.log('Initializing OrderStatusTypes dropdown');
        initializeTomSelect(orderStatusTypesSelect, '/api/OrderStatusTypes/select', 'id', 'text');
    }

    // Update dependent dropdowns when PartnerId changes
    if (partnerSelect && (contactSelect || siteSelect || quoteSelect)) {
        console.log('Attaching change event to PartnerId');
        partnerSelect.addEventListener('change', function () {
            console.log('PartnerId changed, updating dependent dropdowns');
            [contactSelect, siteSelect, quoteSelect].forEach(select => {
                if (select && select.tomselect) {
                    select.tomselect.clear();
                    select.tomselect.clearOptions();
                    select.tomselect.load('');
                }
            });
            document.querySelectorAll('.order-item select[name*="ProductId"]').forEach(select => {
                if (select.tomselect) {
                    select.tomselect.clear();
                    select.tomselect.clearOptions();
                    select.tomselect.load('');
                }
            });
            // Clear PartnerPrice when PartnerId changes
            document.querySelectorAll('.order-item input[name*="PartnerPrice"]').forEach(input => {
                input.value = '';
            });
        });
    }

    // Update TotalAmount based on OrderItems
    function updateTotalAmount() {
        let total = 0;
        document.querySelectorAll('.order-item').forEach(item => {
            const unitPriceInput = item.querySelector('.order-item-unit-price');
            const unitPrice = unitPriceInput ? parseFloat(unitPriceInput.value) || 0 : 0;
            total += unitPrice;
        });
        const totalAmountInput = document.getElementById('totalAmount');
        if (totalAmountInput) {
            totalAmountInput.value = total.toFixed(2);
        }
    }

    function addOrderItem(itemData = null) {
        const container = document.getElementById('orderItemsContainer');
        if (!container) {
            console.error('orderItemsContainer not found');
            return;
        }
        const itemIndex = container.children.length;
        const itemHtml = `
            <div class="order-item mb-3 p-3 border rounded" data-index="${itemIndex}">
                <h6>Tétel ${itemIndex + 1}</h6>
                <div class="row">
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label">Mennyiség</label>
                            <input name="OrderItems[${itemIndex}].Quantity" type="number" step="0.0001" min="0" class="form-control order-item-quantity" value="${itemData?.Quantity || ''}" required />
                            <div class="invalid-feedback">Mennyiség megadása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Egyedi listaár</label>
                            <input name="OrderItems[${itemIndex}].BasePrice" type="number" step="0.01" min="0" class="form-control order-item-base-price" value="${itemData?.BasePrice || ''}" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Egyedi kedvezmény összeg a listaárból</label>
                            <input name="OrderItems[${itemIndex}].DiscountAmount" type="number" step="0.01" min="0" class="form-control order-item-discount" value="${itemData?.DiscountAmount || ''}" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Egyedi kedvezmény százalék a listaárból</label>
                            <input name="OrderItems[${itemIndex}].DiscountPercentage" type="number" step="0.01" min="0" max="100" class="form-control order-item-discount-percentage" value="${itemData?.DiscountPercentage || ''}" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Összesített ár (kedvezménnyel csökkentett ár, vagy a listaár * mennyiség)</label>
                            <input name="OrderItems[${itemIndex}].UnitPrice" type="number" step="0.01" class="form-control order-item-unit-price" value="${itemData?.UnitPrice || ''}" required disabled />
                            <div class="invalid-feedback">Összesített ár megadása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">ÁFA értéke</label>
                            <input name="OrderItems[${itemIndex}].VATvalue" type="number" step="0.01" min="0" class="form-control order-item-vat-value" value="${itemData?.VATvalue || ''}" disabled />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Bruttó ár</label>
                            <input name="OrderItems[${itemIndex}].Gross" type="number" step="0.01" min="0" class="form-control order-item-gross" value="${itemData?.Gross || ''}" disabled />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label">Kedvezmény típusa</label>
                            <select name="OrderItems[${itemIndex}].DiscountType" class="form-control tomselect-item" id="discountTypeSelect_${itemIndex}">
                                <option value="">Válasszon...</option>
                                <option value="1" ${itemData?.DiscountType === 1 ? 'selected' : ''}>Nincs</option>
                                <option value="2" ${itemData?.DiscountType === 2 ? 'selected' : ''}>Egyedi kedvezmény (%)</option>
                                <option value="3" ${itemData?.DiscountType === 3 ? 'selected' : ''}>Egyedi kedvezmény összeg</option>
                                <option value="4" ${itemData?.DiscountType === 4 ? 'selected' : ''}>Partner ár</option>
                                <option value="5" ${itemData?.DiscountType === 5 ? 'selected' : ''}>Mennyiségi kedvezmény</option>
                                <option value="6" ${itemData?.DiscountType === 6 ? 'selected' : ''}>Listaár</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Termék</label>
                            <select name="OrderItems[${itemIndex}].ProductId" class="form-control tomselect-item" id="productIdSelect_${itemIndex}" required>
                                <option value="" disabled ${!itemData?.ProductId ? 'selected' : ''}>Válasszon...</option>
                            </select>
                            <div class="invalid-feedback">Termék kiválasztása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Egyedi terméknév (ha kivan töltve, ez lesz a termék neve az ajánlaton)</label>
                            <input name="OrderItems[${itemIndex}].Description" class="form-control" value="${itemData?.Description || ''}" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">ÁFA típus</label>
                            <select name="OrderItems[${itemIndex}].VatTypeId" class="form-control tomselect-item" id="vatTypeIdSelect_${itemIndex}">
                                <option value="" ${!itemData?.VatTypeId ? 'selected' : ''}>Válasszon...</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Partner ár</label>
                            <input name="OrderItems[${itemIndex}].PartnerPrice" type="number" step="0.01" min="0" class="form-control order-item-partner-price" value="${itemData?.PartnerPrice || ''}" disabled />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Mennyiségi ár</label>
                            <input name="OrderItems[${itemIndex}].VolumePrice" type="number" step="0.01" min="0" class="form-control order-item-volume-price" value="${itemData?.VolumePrice || ''}" disabled />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Listaár</label>
                            <input name="OrderItems[${itemIndex}].ListPrice" type="number" step="0.01" min="0" class="form-control order-item-list-price" value="${itemData?.ListPrice || ''}" disabled />
                        </div>
                        <button type="button" class="btn btn-danger btn-sm remove-order-item">Tétel törlése</button>
                    </div>
                </div>
            </div>`;
        container.insertAdjacentHTML('beforeend', itemHtml);

        // Initialize dropdowns for the new item
        const productSelect = container.querySelector(`#productIdSelect_${itemIndex}`);
        const vatTypeSelect = document.querySelector(`#vatTypeIdSelect_${itemIndex}`);
        if (productSelect) {
            const productTomSelect = initializeTomSelect(productSelect, '/api/Product', 'id', 'text');
            if (itemData?.ProductId) productTomSelect.setValue(itemData.ProductId);
        }
        if (vatTypeSelect) {
            const vatTomSelect = initializeTomSelect(vatTypeSelect, '/api/vat/GetVatTypesForSelect', 'id', 'text');
            if (itemData?.VatTypeId) vatTomSelect.setValue(itemData.VatTypeId);
        }

        // Attach remove event
        const removeButton = container.querySelector(`.order-item[data-index="${itemIndex}"] .remove-order-item`);
        if (removeButton) {
            removeButton.addEventListener('click', function () {
                const item = container.querySelector(`.order-item[data-index="${itemIndex}"]`);
                item.querySelectorAll('.tomselect-item').forEach(select => {
                    if (select.tomselect) select.tomselect.destroy();
                });
                item.remove();
                updateItemIndices();
                updateTotalAmount();
            });
        }

        // Update UnitPrice and VolumePrice on quantity change
        const quantityInput = container.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
        const unitPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
        const volumePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
        const discountTypeSelect = container.querySelector(`select[name="OrderItems[${itemIndex}].DiscountType"]`);
        const discountAmountInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountAmount"]`);
        const discountPercentageInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountPercentage"]`);
        const basePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].BasePrice"]`);
        if (quantityInput && productSelect) {
            quantityInput.addEventListener('input', function () {
                const productId = productSelect.value;
                const currencyId = document.querySelector('#currencyIdSelect')?.value;
                const quantity = parseFloat(quantityInput.value) || 0;
                if (!productId || !currencyId) {
                    console.warn('ProductId or CurrencyId not selected, cannot fetch VolumePrice');
                    unitPriceInput.value = '';
                    volumePriceInput.value = '';
                    updateTotalAmount();
                    return;
                }
                const url = `/api/ProductPrice?productId=${encodeURIComponent(productId)}&currencyId=${encodeURIComponent(currencyId)}&isActive=true`;
                console.log(`Fetching ProductPrice for ProductId ${productId} and CurrencyId ${currencyId} from ${url}`);
                fetch(url, {
                    headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                })
                    .then(response => {
                        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                        return response.json();
                    })
                    .then(data => {
                        const productPrice = Array.isArray(data) && data.length > 0 ? data[0] : null;
                        if (productPrice && productPrice.productPriceId && quantity > 0) {
                            const volumeUrl = `/api/ProductPrice/${productPrice.productPriceId}/volume-price?quantity=${quantity}`;
                            console.log(`Fetching VolumePrice for ProductPriceId ${productPrice.productPriceId} and Quantity ${quantity} from ${volumeUrl}`);
                            fetch(volumeUrl, {
                                headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
                            })
                                .then(response => {
                                    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                                    return response.json();
                                })
                                .then(volumePrice => {
                                    volumePriceInput.value = parseFloat(volumePrice).toFixed(2);
                                    console.log(`Set VolumePrice to ${volumePrice} for ProductId ${productId} and Quantity ${quantity}`);
                                    // Update UnitPrice based on BasePrice or DiscountType
                                    const discountType = discountTypeSelect.value;
                                    const listPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                                    const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                                    const discountAmount = parseFloat(discountAmountInput.value) || 0;
                                    const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                                    const basePrice = parseFloat(basePriceInput.value) || 0;
                                    if (basePrice > 0) {
                                        unitPriceInput.value = (basePrice * quantity).toFixed(2);
                                        console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                                        // Disable DiscountAmount and DiscountPercentage
                                        discountAmountInput.disabled = true;
                                        discountPercentageInput.disabled = true;
                                    } else {
                                        discountAmountInput.disabled = discountPercentage > 0;
                                        discountPercentageInput.disabled = discountAmount > 0;
                                        if (discountType === '1' || discountType === '6') {
                                            unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                            console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                        } else if (discountType === '2') {
                                            unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                                            console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                        } else if (discountType === '3') {
                                            unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                            console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                        } else if (discountType === '5') {
                                            unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                                            console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                        } else if (discountType === '4') {
                                            unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                            console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                        } else {
                                            unitPriceInput.value = '';
                                            console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                                        }
                                    }
                                    updateTotalAmount();
                                })
                                .catch(error => {
                                    console.error(`Error fetching VolumePrice: ${error}`);
                                    document.getElementById('errorMessage').textContent = `Hiba a mennyiségi ár betöltése során: ${error.message}`;
                                    document.getElementById('errorContainer').classList.remove('d-none');
                                    unitPriceInput.value = '';
                                    volumePriceInput.value = '';
                                    updateTotalAmount();
                                });
                        } else {
                            volumePriceInput.value = productPrice && productPrice.salesPrice ? parseFloat(productPrice.salesPrice).toFixed(2) : '';
                            // Update UnitPrice based on BasePrice or DiscountType
                            const discountType = discountTypeSelect.value;
                            const listPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                            const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                            const discountAmount = parseFloat(discountAmountInput.value) || 0;
                            const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                            const basePrice = parseFloat(basePriceInput.value) || 0;
                            if (basePrice > 0) {
                                unitPriceInput.value = (basePrice * quantity).toFixed(2);
                                console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                                // Disable DiscountAmount and DiscountPercentage
                                discountAmountInput.disabled = true;
                                discountPercentageInput.disabled = true;
                            } else {
                                discountAmountInput.disabled = discountPercentage > 0;
                                discountPercentageInput.disabled = discountAmount > 0;
                                if (discountType === '1' || discountType === '6') {
                                    unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                    console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                } else if (discountType === '2') {
                                    unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                                    console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                } else if (discountType === '3') {
                                    unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                    console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                } else if (discountType === '5') {
                                    unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                                    console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                } else if (discountType === '4') {
                                    unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                                    console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                                } else {
                                    unitPriceInput.value = '';
                                    console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                                }
                            }
                            console.log(`Set UnitPrice and VolumePrice to ${productPrice?.salesPrice || 'none'} (no quantity or ProductPriceId)`);
                            updateTotalAmount();
                        }
                    })
                    .catch(error => {
                        console.error(`Error fetching ProductPrice: ${error}`);
                        document.getElementById('errorMessage').textContent = `Hiba a listaár betöltése során: ${error.message}`;
                        document.getElementById('errorContainer').classList.remove('d-none');
                    });
            });
        }

        // Update UnitPrice on DiscountType change
        if (discountTypeSelect) {
            discountTypeSelect.addEventListener('change', function () {
                const discountType = discountTypeSelect.value;
                const listPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                const volumePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
                const partnerPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                const unitPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
                const quantityInput = container.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
                const discountAmountInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountAmount"]`);
                const discountPercentageInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountPercentage"]`);
                const basePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].BasePrice"]`);
                const quantity = parseFloat(quantityInput.value) || 0;
                const discountAmount = parseFloat(discountAmountInput.value) || 0;
                const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                const basePrice = parseFloat(basePriceInput.value) || 0;
                if (basePrice > 0) {
                    unitPriceInput.value = (basePrice * quantity).toFixed(2);
                    console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                    // Disable DiscountAmount and DiscountPercentage
                    discountAmountInput.disabled = true;
                    discountPercentageInput.disabled = true;
                } else {
                    discountAmountInput.disabled = discountPercentage > 0;
                    discountPercentageInput.disabled = discountAmount > 0;
                    if (discountType === '1' || discountType === '6') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '2') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '3') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '5') {
                        unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '4') {
                        unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else {
                        unitPriceInput.value = '';
                        console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                    }
                }
                updateTotalAmount();
            });
        }

        // Update UnitPrice on DiscountAmount change
        if (discountAmountInput) {
            discountAmountInput.addEventListener('input', function () {
                const discountType = discountTypeSelect.value;
                const listPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                const volumePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
                const partnerPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                const unitPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
                const quantityInput = container.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
                const discountPercentageInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountPercentage"]`);
                const basePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].BasePrice"]`);
                const quantity = parseFloat(quantityInput.value) || 0;
                const discountAmount = parseFloat(discountAmountInput.value) || 0;
                const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                const basePrice = parseFloat(basePriceInput.value) || 0;
                // Update disabled state
                if (discountAmount > 0) {
                    basePriceInput.disabled = true;
                    discountPercentageInput.disabled = true;
                } else {
                    basePriceInput.disabled = false;
                    discountPercentageInput.disabled = basePrice > 0;
                }
                if (basePrice > 0) {
                    unitPriceInput.value = (basePrice * quantity).toFixed(2);
                    console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                } else {
                    if (discountType === '1' || discountType === '6') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '2') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '3') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '5') {
                        unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '4') {
                        unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else {
                        unitPriceInput.value = '';
                        console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                    }
                }
                updateTotalAmount();
            });
        }

        // Update UnitPrice on DiscountPercentage change
        if (discountPercentageInput) {
            discountPercentageInput.addEventListener('input', function () {
                const discountType = discountTypeSelect.value;
                const listPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                const volumePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
                const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                const unitPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
                const quantityInput = container.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
                const discountAmountInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountAmount"]`);
                const basePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].BasePrice"]`);
                const quantity = parseFloat(quantityInput.value) || 0;
                const discountAmount = parseFloat(discountAmountInput.value) || 0;
                const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                const basePrice = parseFloat(basePriceInput.value) || 0;
                // Update disabled state
                if (discountPercentage > 0) {
                    basePriceInput.disabled = true;
                    discountAmountInput.disabled = true;
                } else {
                    basePriceInput.disabled = false;
                    discountAmountInput.disabled = basePrice > 0;
                }
                if (basePrice > 0) {
                    unitPriceInput.value = (basePrice * quantity).toFixed(2);
                    console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                } else {
                    if (discountType === '1' || discountType === '6') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '2') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '3') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '5') {
                        unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '4') {
                        unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else {
                        unitPriceInput.value = '';
                        console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                    }
                }
                updateTotalAmount();
            });
        }

        // Update UnitPrice on BasePrice change
        if (basePriceInput) {
            basePriceInput.addEventListener('input', function () {
                const discountType = discountTypeSelect.value;
                const listPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].ListPrice"]`);
                const volumePriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].VolumePrice"]`);
                const partnerPriceInput = document.querySelector(`input[name="OrderItems[${itemIndex}].PartnerPrice"]`);
                const unitPriceInput = container.querySelector(`input[name="OrderItems[${itemIndex}].UnitPrice"]`);
                const quantityInput = container.querySelector(`input[name="OrderItems[${itemIndex}].Quantity"]`);
                const discountAmountInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountAmount"]`);
                const discountPercentageInput = container.querySelector(`input[name="OrderItems[${itemIndex}].DiscountPercentage"]`);
                const quantity = parseFloat(quantityInput.value) || 0;
                const discountAmount = parseFloat(discountAmountInput.value) || 0;
                const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
                const basePrice = parseFloat(basePriceInput.value) || 0;
                // Update disabled state
                if (basePrice > 0) {
                    discountAmountInput.disabled = true;
                    discountPercentageInput.disabled = true;
                } else {
                    discountAmountInput.disabled = discountPercentage > 0;
                    discountPercentageInput.disabled = discountAmount > 0;
                }
                if (basePrice > 0) {
                    unitPriceInput.value = (basePrice * quantity).toFixed(2);
                    console.log(`Set UnitPrice to (BasePrice * Quantity) (${basePrice} * ${quantity} = ${unitPriceInput.value})`);
                } else {
                    if (discountType === '1' || discountType === '6') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '2') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * (1 - discountPercentage / 100) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * (1 - DiscountPercentage/100) * Quantity - DiscountAmount) (${listPriceInput.value} * (1 - ${discountPercentage}/100) * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '3') {
                        unitPriceInput.value = (parseFloat(listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (ListPrice * Quantity - DiscountAmount) (${listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '5') {
                        unitPriceInput.value = (parseFloat(volumePriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (VolumePrice * Quantity - DiscountAmount) (${volumePriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else if (discountType === '4') {
                        unitPriceInput.value = (parseFloat(partnerPriceInput.value || listPriceInput.value) * quantity - discountAmount).toFixed(2);
                        console.log(`Set UnitPrice to (PartnerPrice * Quantity - DiscountAmount) (${partnerPriceInput.value || listPriceInput.value} * ${quantity} - ${discountAmount} = ${unitPriceInput.value}) for DiscountType ${discountType}`);
                    } else {
                        unitPriceInput.value = '';
                        console.log(`Cleared UnitPrice for DiscountType ${discountType}`);
                    }
                }
                updateTotalAmount();
            });
        }

        // Update TotalAmount on input change
        const inputs = container.querySelectorAll(`
            .order-item[data-index="${itemIndex}"] .order-item-quantity,
            .order-item[data-index="${itemIndex}"] .order-item-unit-price,
            .order-item[data-index="${itemIndex}"] .order-item-discount,
            .order-item[data-index="${itemIndex}"] .order-item-discount-percentage,
            .order-item[data-index="${itemIndex}"] .order-item-base-price,
            .order-item[data-index="${itemIndex}"] .order-item-vat-value,
            .order-item[data-index="${itemIndex}"] .order-item-gross
        `);
        inputs.forEach(input => {
            input.addEventListener('input', updateTotalAmount);
        });

        updateItemIndices();
        updateTotalAmount();
    }

    // Update item indices
    function updateItemIndices() {
        const items = document.querySelectorAll('#orderItemsContainer .order-item');
        items.forEach((item, index) => {
            item.dataset.index = index;
            item.querySelector('h6').textContent = `Tétel ${index + 1}`;
            const inputs = item.querySelectorAll('input, select');
            inputs.forEach(input => {
                const name = input.name.replace(/OrderItems\[\d+\]/, `OrderItems[${index}]`);
                input.name = name;
                if (input.id) {
                    const id = input.id.replace(/_\d+$/, `_${index}`);
                    input.id = id;
                }
            });
        });
    }

    // Load order data for editing
    async function loadOrderForEdit(orderId) {
        try {
            const response = await fetch(`/api/Orders/${orderId}`, {
                headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
            });
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            const order = await response.json();
            console.log('Order data fetched for edit:', order);

            const form = document.getElementById('createOrderForm');
            form.dataset.orderId = orderId;
            form.dataset.mode = 'edit';

            // Populate main form fields
            form.querySelector('[name="OrderCreateDTO.OrderNumber"]').value = order.OrderNumber || '';
            form.querySelector('[name="OrderCreateDTO.OrderDate"]').value = order.OrderDate ? order.OrderDate.split('T')[0] : '';
            form.querySelector('[name="OrderCreateDTO.Deadline"]').value = order.Deadline || '';
            form.querySelector('[name="OrderCreateDTO.DeliveryDate"]').value = order.DeliveryDate || '';
            form.querySelector('[name="OrderCreateDTO.PlannedDelivery"]').value = order.PlannedDelivery || '';
            form.querySelector('[name="OrderCreateDTO.TotalAmount"]').value = order.TotalAmount ? order.TotalAmount.toFixed(2) : '';
            form.querySelector('[name="OrderCreateDTO.DiscountPercentage"]').value = order.DiscountPercentage || '';
            form.querySelector('[name="OrderCreateDTO.DiscountAmount"]').value = order.DiscountAmount || '';
            form.querySelector('[name="OrderCreateDTO.CompanyName"]').value = order.CompanyName || '';
            form.querySelector('[name="OrderCreateDTO.SalesPerson"]').value = order.SalesPerson || '';
            form.querySelector('[name="OrderCreateDTO.Status"]').value = order.Status || 'Pending';
            form.querySelector('[name="OrderCreateDTO.Subject"]').value = order.Subject || '';
            form.querySelector('[name="OrderCreateDTO.DetailedDescription"]').value = order.DetailedDescription || '';
            form.querySelector('[name="OrderCreateDTO.OrderType"]').value = order.OrderType || '';
            form.querySelector('[name="OrderCreateDTO.ReferenceNumber"]').value = order.ReferenceNumber || '';
            form.querySelector('[name="OrderCreateDTO.IsDeleted"]').checked = order.IsDeleted || false;

            // Populate dropdowns
            if (partnerSelect && order.PartnerId) partnerTomSelect.setValue(order.PartnerId);
            if (currencySelect && order.CurrencyId) currencyTomSelect.setValue(order.CurrencyId);
            if (contactSelect && order.ContactId) contactSelect.tomselect.setValue(order.ContactId);
            if (siteSelect && order.SiteId) siteSelect.tomselect.setValue(order.SiteId);
            if (shippingMethodSelect && order.ShippingMethodId) shippingMethodSelect.tomselect.setValue(order.ShippingMethodId);
            if (paymentTermSelect && order.PaymentTermId) paymentTermSelect.tomselect.setValue(order.PaymentTermId);
            if (quoteSelect && order.QuoteId) quoteSelect.tomselect.setValue(order.QuoteId);
            if (orderStatusTypesSelect && order.OrderStatusTypes) orderStatusTypesSelect.tomselect.setValue(order.OrderStatusTypes);

            // Populate order items
            const container = document.getElementById('orderItemsContainer');
            container.innerHTML = '';
            (order.OrderItems || []).forEach(item => addOrderItem(item));

            updateTotalAmount();
        } catch (error) {
            console.error('Error fetching order:', error);
            document.getElementById('errorMessage').textContent = `Hiba a rendelés betöltése során: ${error.message}`;
            document.getElementById('errorContainer').classList.remove('d-none');
        }
    }

    // Initialize order items and modal
    const newOrderModal = document.getElementById('newOrderModal');
    if (newOrderModal) {
        console.log('newOrderModal found, attaching event listeners');

        newOrderModal.addEventListener('show.bs.modal', function (event) {
            console.log('Modal shown, initializing');
            const form = document.getElementById('createOrderForm');
            const container = document.getElementById('orderItemsContainer');
            const button = event.relatedTarget;
            const mode = button ? button.dataset.mode : 'create';
            form.dataset.mode = mode;

            // Reset form and items
            if (form) form.reset();
            if (container) {
                container.querySelectorAll('.tomselect-item').forEach(select => {
                    if (select.tomselect) select.tomselect.destroy();
                });
                container.innerHTML = '';
            }
            [partnerSelect, currencySelect, contactSelect, siteSelect, shippingMethodSelect, paymentTermSelect, quoteSelect, orderStatusTypesSelect].forEach(select => {
                if (select && select.tomselect) select.tomselect.destroy();
            });

            // Reinitialize dropdowns
            if (partnerSelect) partnerTomSelect = initializeTomSelect(partnerSelect, '/api/partners/select', 'id', 'text');
            if (currencySelect) currencyTomSelect = initializeTomSelect(currencySelect, '/api/currencies', 'id', 'text');
            if (contactSelect) {
                const getContactEndpoint = (query) => {
                    const partnerId = partnerSelect ? partnerSelect.value : '';
                    if (!partnerId) return null;
                    return `/api/partners/${partnerId}/contacts/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
                };
                initializeTomSelect(contactSelect, '', 'id', 'text', getContactEndpoint);
            }
            if (siteSelect) {
                const getSiteEndpoint = (query) => {
                    const partnerId = partnerSelect ? partnerSelect.value : '';
                    if (!partnerId) return null;
                    return `/api/partners/${partnerId}/sites/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
                };
                initializeTomSelect(siteSelect, '', 'id', 'text', getSiteEndpoint);
            }
            if (shippingMethodSelect) initializeTomSelect(shippingMethodSelect, '/api/ordershippingmethods/select', 'id', 'text');
            if (paymentTermSelect) initializeTomSelect(paymentTermSelect, '/api/paymentterms/select', 'id', 'text');
            if (quoteSelect) {
                const getQuoteEndpoint = (query) => {
                    const partnerId = partnerSelect ? partnerSelect.value : '';
                    if (!partnerId) return null;
                    return `/api/quotes/select?partnerId=${partnerId}${query ? `?search=${encodeURIComponent(query)}` : ''}`;
                };
                initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
            }
            if (orderStatusTypesSelect) initializeTomSelect(orderStatusTypesSelect, '/api/orderstatustypes/select', 'id', 'text');

            if (mode === 'edit' && button) {
                console.log('Edit mode: Loading order data');
                const orderId = button.dataset.orderId;
                loadOrderForEdit(orderId);
            } else {
                console.log('Create mode: Initializing for new order');
                // Set default order date
                document.getElementById('orderDate').value = new Date().toISOString().split('T')[0];
                // Add initial order item
                if (container && container.children.length === 0) {
                    console.log('Create mode: Adding initial order item');
                    addOrderItem();
                }
            }

            // Initialize addOrderItemButton
            const addButton = document.getElementById('addOrderItemButton');
            if (addButton) {
                addButton.removeEventListener('click', addOrderItem);
                addButton.addEventListener('click', () => addOrderItem());
            }
        });

        newOrderModal.addEventListener('hidden.bs.modal', function () {
            console.log('Modal hidden, cleaning up');
            const form = document.getElementById('createOrderForm');
            if (form) {
                form.reset();
                delete form.dataset.mode;
                delete form.dataset.orderId;
                if (typeof $.fn.validate === 'function') {
                    $(form).validate().resetForm();
                    $(form).find('.is-invalid').removeClass('is-invalid');
                    $(form).find('.invalid-feedback').remove();
                }
                const container = document.getElementById('orderItemsContainer');
                if (container) {
                    container.querySelectorAll('.tomselect-item').forEach(select => {
                        if (select.tomselect) select.tomselect.destroy();
                    });
                    container.innerHTML = '';
                }
            }
            [partnerSelect, currencySelect, contactSelect, siteSelect, shippingMethodSelect, paymentTermSelect, quoteSelect, orderStatusTypesSelect].forEach(select => {
                if (select && select.tomselect) select.tomselect.destroy();
            });
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';
            const backdrops = document.querySelectorAll('.modal-backdrop');
            backdrops.forEach(backdrop => backdrop.remove());
        });
    }

    // Form submission
    const form = document.getElementById('createOrderForm');
    if (form) {
        // Initialize jQuery Validation
        if (typeof $.fn.validate === 'function') {
            $(form).validate({
                rules: {
                    'OrderCreateDTO.OrderNumber': { required: true },
                    'OrderCreateDTO.TotalAmount': { required: true, number: true },
                    'OrderCreateDTO.PartnerId': { required: true, number: true },
                    'OrderCreateDTO.CurrencyId': { required: true, number: true },
                    'OrderItems[0].Quantity': { required: true, number: true, min: 0.0001 },
                    'OrderItems[0].UnitPrice': { required: true, number: true, min: 0 },
                    'OrderItems[0].ProductId': { required: true, number: true }
                },
                messages: {
                    'OrderCreateDTO.OrderNumber': 'Rendelésszám megadása kötelező.',
                    'OrderCreateDTO.TotalAmount': 'Összeg megadása kötelező.',
                    'OrderCreateDTO.PartnerId': 'Partner kiválasztása kötelező.',
                    'OrderCreateDTO.CurrencyId': 'Pénznem kiválasztása kötelező.',
                    'OrderItems[0].Quantity': 'Mennyiség megadása kötelező.',
                    'OrderItems[0].UnitPrice': 'Összesített ár megadása kötelező.',
                    'OrderItems[0].ProductId': 'Termék kiválasztása kötelező.'
                },
                errorPlacement: function (error, element) {
                    const errorDiv = element.siblings('.invalid-feedback').length ? element.siblings('.invalid-feedback') : $('<div class="invalid-feedback"></div>').insertAfter(element);
                    errorDiv.html(error);
                    element.addClass('is-invalid');
                },
                success: function (label, element) {
                    $(element).removeClass('is-invalid');
                    $(element).siblings('.invalid-feedback').empty();
                }
            });
        }

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const errorContainer = document.getElementById('errorContainer');
            const errorMessage = document.getElementById('errorMessage');
            errorContainer.classList.add('d-none');
            form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
            form.querySelectorAll('.invalid-feedback').forEach(el => el.innerHTML = '');

            // Client-side validation
            let hasErrors = false;
            if (!partnerSelect.value || isNaN(parseInt(partnerSelect.value))) {
                hasErrors = true;
                partnerSelect.classList.add('is-invalid');
                const errorDiv = partnerSelect.parentElement.querySelector('.invalid-feedback') || document.createElement('div');
                errorDiv.className = 'invalid-feedback';
                errorDiv.textContent = 'Partner kiválasztása kötelező.';
                partnerSelect.parentElement.appendChild(errorDiv);
            }
            if (!currencySelect.value || isNaN(parseInt(currencySelect.value))) {
                hasErrors = true;
                currencySelect.classList.add('is-invalid');
                const errorDiv = currencySelect.parentElement.querySelector('.invalid-feedback') || document.createElement('div');
                errorDiv.className = 'invalid-feedback';
                errorDiv.textContent = 'Pénznem kiválasztása kötelező.';
                currencySelect.parentElement.appendChild(errorDiv);
            }
            const items = document.querySelectorAll('#orderItemsContainer .order-item');
            if (items.length === 0) {
                hasErrors = true;
                errorMessage.textContent = 'Legalább egy rendelési tétel megadása kötelező.';
                errorContainer.classList.remove('d-none');
            }
            items.forEach((item, index) => {
                const productSelect = item.querySelector(`select[name="OrderItems[${index}].ProductId"]`);
                if (!productSelect || !productSelect.value || isNaN(parseInt(productSelect.value))) {
                    hasErrors = true;
                    productSelect.classList.add('is-invalid');
                    let errorDiv = item.querySelector('.product-error');
                    if (!errorDiv) {
                        errorDiv = document.createElement('div');
                        errorDiv.className = 'invalid-feedback product-error';
                        errorDiv.textContent = 'Termék kiválasztása kötelező.';
                        productSelect.parentElement.appendChild(errorDiv);
                    }
                } else {
                    productSelect.classList.remove('is-invalid');
                    const errorDiv = item.querySelector('.product-error');
                    if (errorDiv) errorDiv.remove();
                }
            });

            if (typeof $.fn.validate === 'function' && !$(form).valid()) {
                hasErrors = true;
            }

            if (hasErrors) {
                errorMessage.textContent = 'Kérjük, töltse ki az összes kötelező mezőt, és válasszon terméket minden tételhez.';
                errorContainer.classList.remove('d-none');
                return;
            }

            // Serialize form data
            const formData = new FormData(form);
            const orderDto = {
                OrderNumber: formData.get('OrderCreateDTO.OrderNumber') || null,
                OrderDate: formData.get('OrderCreateDTO.OrderDate') || null,
                Deadline: formData.get('OrderCreateDTO.Deadline') || null,
                DeliveryDate: formData.get('OrderCreateDTO.DeliveryDate') || null,
                PlannedDelivery: formData.get('OrderCreateDTO.PlannedDelivery') || null,
                TotalAmount: parseFloat(formData.get('OrderCreateDTO.TotalAmount')) || 0,
                DiscountPercentage: parseFloat(formData.get('OrderCreateDTO.DiscountPercentage')) || null,
                DiscountAmount: parseFloat(formData.get('OrderCreateDTO.DiscountAmount')) || null,
                CompanyName: formData.get('OrderCreateDTO.CompanyName') || null,
                SalesPerson: formData.get('OrderCreateDTO.SalesPerson') || null,
                Status: formData.get('OrderCreateDTO.Status') || 'Pending',
                PartnerId: parseInt(formData.get('OrderCreateDTO.PartnerId')),
                ContactId: parseInt(formData.get('OrderCreateDTO.ContactId')) || null,
                SiteId: parseInt(formData.get('OrderCreateDTO.SiteId')) || null,
                CurrencyId: parseInt(formData.get('OrderCreateDTO.CurrencyId')),
                ShippingMethodId: parseInt(formData.get('OrderCreateDTO.ShippingMethodId')) || null,
                PaymentTermId: parseInt(formData.get('OrderCreateDTO.PaymentTermId')) || null,
                Subject: formData.get('OrderCreateDTO.Subject') || null,
                DetailedDescription: formData.get('OrderCreateDTO.DetailedDescription') || null,
                OrderType: formData.get('OrderCreateDTO.OrderType') || null,
                ReferenceNumber: formData.get('OrderCreateDTO.ReferenceNumber') || null,
                QuoteId: parseInt(formData.get('OrderCreateDTO.QuoteId')) || null,
                OrderStatusTypes: parseInt(formData.get('OrderCreateDTO.OrderStatusTypes')) || null,
                IsDeleted: formData.get('OrderCreateDTO.IsDeleted') === 'on',
                OrderItems: []
            };

            items.forEach((item, index) => {
                const productId = parseInt(item.querySelector(`select[name="OrderItems[${index}].ProductId"]`)?.value);
                if (!productId) return; // Skip invalid items
                const quantity = parseFloat(item.querySelector(`input[name="OrderItems[${index}].Quantity"]`)?.value) || 0;
                const unitPrice = parseFloat(item.querySelector(`input[name="OrderItems[${index}].UnitPrice"]`)?.value) || 0;
                if (quantity <= 0 || unitPrice <= 0) return; // Skip invalid items
                orderDto.OrderItems.push({
                    Description: item.querySelector(`input[name="OrderItems[${index}].Description"]`)?.value || null,
                    Quantity: quantity,
                    UnitPrice: unitPrice,
                    DiscountAmount: parseFloat(item.querySelector(`input[name="OrderItems[${index}].DiscountAmount"]`)?.value) || null,
                    DiscountPercentage: parseFloat(item.querySelector(`input[name="OrderItems[${index}].DiscountPercentage"]`)?.value) || null,
                    BasePrice: parseFloat(item.querySelector(`input[name="OrderItems[${index}].BasePrice"]`)?.value) || null,
                    PartnerPrice: parseFloat(item.querySelector(`input[name="OrderItems[${index}].PartnerPrice"]`)?.value) || null,
                    VolumeThreshold: parseInt(item.querySelector(`input[name="OrderItems[${index}].VolumeThreshold"]`)?.value) || null,
                    VolumePrice: parseFloat(item.querySelector(`input[name="OrderItems[${index}].VolumePrice"]`)?.value) || null,
                    ListPrice: parseFloat(item.querySelector(`input[name="OrderItems[${index}].ListPrice"]`)?.value) || null,
                    DiscountType: parseInt(item.querySelector(`select[name="OrderItems[${index}].DiscountType"]`)?.value) || null,
                    ProductId: productId,
                    VatTypeId: parseInt(item.querySelector(`select[name="OrderItems[${index}].VatTypeId"]`)?.value) || null
                });
            });

            // Log the payload for debugging
            console.log('Sending orderDto:', JSON.stringify(orderDto, null, 2));

            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                errorMessage.textContent = 'Hiba: Biztonsági token nem található.';
                errorContainer.classList.remove('d-none');
                return;
            }

            // Determine API endpoint and method
            const mode = form.dataset.mode || 'create';
            const url = mode === 'edit' ? `/api/Orders/${form.dataset.orderId}` : '/api/Orders';
            const method = mode === 'edit' ? 'PUT' : 'POST';

            try {
                const response = await fetch(url, {
                    method: method,
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer ' + localStorage.getItem('token'),
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(orderDto)
                });

                let responseData;
                try {
                    responseData = await response.json(); // Try to parse as JSON
                } catch (jsonError) {
                    // Handle non-JSON response
                    responseData = { message: await response.text() || `Hiba történt a rendelés ${mode === 'edit' ? 'módosítása' : 'létrehozása'} során.` };
                }

                if (response.ok) {
                    console.log(`Order ${mode === 'edit' ? 'updated' : 'created'}:`, responseData);
                    errorContainer.classList.add('d-none');
                    alert(`Rendelés sikeresen ${mode === 'edit' ? 'módosítva' : 'létrehozva'}! ID: ${responseData.orderId || form.dataset.orderId}`);
                    $('#newOrderModal').modal('hide');
                    location.reload();
                } else {
                    let errorText = responseData.message || `Hiba történt a rendelés ${mode === 'edit' ? 'módosítása' : 'létrehozása'} során.`;
                    if (responseData.errors) {
                        Object.keys(responseData.errors).forEach(key => {
                            const field = key.startsWith('$.') ? key.replace('$.', '') : key.replace('OrderCreateDTO.', '');
                            const errorDiv = document.querySelector(`[data-valmsg-for="OrderCreateDTO.${field}"]`) || document.createElement('div');
                            if (!errorDiv.parentElement) {
                                errorDiv.className = 'invalid-feedback';
                                const input = document.querySelector(`[name="OrderCreateDTO.${field}"]`) || document.querySelector(`[name="OrderItems[${key.match(/\d+/)}].${field.split('.').pop()}]`);
                                if (input) input.parentElement.appendChild(errorDiv);
                            }
                            errorDiv.textContent = responseData.errors[key].join(', ');
                            const input = document.querySelector(`[name="OrderCreateDTO.${field}"]`) || document.querySelector(`[name="OrderItems[${key.match(/\d+/)}].${field.split('.').pop()}]`);
                            if (input) input.classList.add('is-invalid');
                        });
                        errorText = 'Kérjük, javítsa a hibás mezőket.';
                    }
                    errorMessage.textContent = errorText;
                    errorContainer.classList.remove('d-none');
                }
            } catch (error) {
                console.error(`Error during ${mode === 'edit' ? 'update' : 'create'}:`, error);
                errorMessage.textContent = `Hiba történt a rendelés ${mode === 'edit' ? 'módosítása' : 'létrehozása'} során: ${error.message}`;
                errorContainer.classList.remove('d-none');
            }
        });
    }

    // Attach edit button listeners
    document.querySelectorAll('.edit-order-btn').forEach(button => {
        button.addEventListener('click', function () {
            const orderId = this.dataset.orderId;
            console.log(`Triggering edit mode for order ID: ${orderId}`);
            const modalButton = this.cloneNode();
            modalButton.dataset.mode = 'edit';
            modalButton.dataset.orderId = orderId;
            newOrderModal.dispatchEvent(new CustomEvent('show.bs.modal', { detail: { relatedTarget: modalButton } }));
        });
    });
});