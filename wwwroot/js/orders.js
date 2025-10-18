document.addEventListener('DOMContentLoaded', function () {
    console.log('createOrder.js loaded');

    // Initialize TomSelect for dropdowns
    function initializeTomSelect(selectElement, endpoint, valueField = 'id', labelField = 'text', getDynamicParams = null) {
        try {
            return new TomSelect(selectElement, {
                valueField: valueField,
                labelField: labelField,
                searchField: [labelField],
                placeholder: 'Válasszon...',
                allowEmptyOption: selectElement.id.includes('partnerIdSelect') || selectElement.id.includes('currencyIdSelect') ? false : true,
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
            return `/api/quotes/select?partnerId=${partnerId}${query ? `&search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
    }

    const orderStatusTypesSelect = document.querySelector('#orderStatusTypesSelect');
    if (orderStatusTypesSelect) {
        console.log('Initializing OrderStatusTypes dropdown');
        initializeTomSelect(orderStatusTypesSelect, '/api/orderstatustypes/select', 'id', 'text');
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
        });
    }

    // Update TotalAmount based on OrderItems
    function updateTotalAmount() {
        let total = 0;
        document.querySelectorAll('.order-item').forEach(item => {
            const quantityInput = item.querySelector('.order-item-quantity');
            const unitPriceInput = item.querySelector('.order-item-unit-price');
            const discountInput = item.querySelector('.order-item-discount');
            const quantity = quantityInput ? parseFloat(quantityInput.value) || 0 : 0;
            const unitPrice = unitPriceInput ? parseFloat(unitPriceInput.value) || 0 : 0;
            const discount = discountInput ? parseFloat(discountInput.value) || 0 : 0;
            total += quantity * unitPrice - discount;
        });
        const totalAmountInput = document.getElementById('totalAmount');
        if (totalAmountInput) {
            totalAmountInput.value = total.toFixed(2);
        }
    }

    // Initialize order items
    const newOrderModal = document.getElementById('newOrderModal');
    if (newOrderModal) {
        console.log('newOrderModal found, attaching event listeners');

        newOrderModal.addEventListener('show.bs.modal', function () {
            console.log('Modal shown, initializing for create');
            const form = document.getElementById('createOrderForm');
            const container = document.getElementById('orderItemsContainer');

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
                    return `/api/quotes/select?partnerId=${partnerId}${query ? `&search=${encodeURIComponent(query)}` : ''}`;
                };
                initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
            }
            if (orderStatusTypesSelect) initializeTomSelect(orderStatusTypesSelect, '/api/orderstatustypes/select', 'id', 'text');

            // Set default order date
            document.getElementById('orderDate').value = new Date().toISOString().split('T')[0];

            // Add initial order item
            if (container && container.children.length === 0) {
                console.log('Create mode: Adding initial order item');
                addOrderItem();
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

    // Add order item
    function addOrderItem() {
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
                            <label class="form-label">Leírás</label>
                            <input name="OrderItems[${itemIndex}].Description" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Mennyiség</label>
                            <input name="OrderItems[${itemIndex}].Quantity" type="number" step="0.0001" min="0" class="form-control order-item-quantity" required />
                            <div class="invalid-feedback">Mennyiség megadása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Egységár</label>
                            <input name="OrderItems[${itemIndex}].UnitPrice" type="number" step="0.01" class="form-control order-item-unit-price" required />
                            <div class="invalid-feedback">Egységár megadása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Kedvezmény összege</label>
                            <input name="OrderItems[${itemIndex}].DiscountAmount" type="number" step="0.01" class="form-control order-item-discount" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label">Kedvezmény típusa</label>
                            <select name="OrderItems[${itemIndex}].DiscountType" class="form-control tomselect-item" id="discountTypeSelect_${itemIndex}">
                                <option value="">Válasszon...</option>
                                <option value="1">Nincs</option>
                                <option value="2">Egyedi kedvezmény (%)</option>
                                <option value="3">Egyedi kedvezmény összeg</option>
                                <option value="4">Partner ár</option>
                                <option value="5">Mennyiségi kedvezmény</option>
                                <option value="6">Listaár</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Termék</label>
                            <select name="OrderItems[${itemIndex}].ProductId" class="form-control tomselect-item" id="productIdSelect_${itemIndex}" required>
                                <option value="" disabled selected>Válasszon...</option>
                            </select>
                            <div class="invalid-feedback">Termék kiválasztása kötelező.</div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">ÁFA típus</label>
                            <select name="OrderItems[${itemIndex}].VatTypeId" class="form-control tomselect-item" id="vatTypeIdSelect_${itemIndex}">
                                <option value="">Válasszon...</option>
                            </select>
                        </div>
                        <button type="button" class="btn btn-danger btn-sm remove-order-item">Tétel törlése</button>
                    </div>
                </div>
            </div>`;
        container.insertAdjacentHTML('beforeend', itemHtml);

        // Initialize dropdowns for the new item
        const productSelect = container.querySelector(`#productIdSelect_${itemIndex}`);
        const vatTypeSelect = container.querySelector(`#vatTypeIdSelect_${itemIndex}`);
        if (productSelect) {
            initializeTomSelect(productSelect, '/api/Product', 'id', 'text');
        }
        if (vatTypeSelect) {
            initializeTomSelect(vatTypeSelect, '/api/vat/GetVatTypesForSelect', 'id', 'text');
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

        // Update TotalAmount on input change
        const inputs = container.querySelectorAll(`.order-item[data-index="${itemIndex}"] .order-item-quantity, .order-item[data-index="${itemIndex}"] .order-item-unit-price, .order-item[data-index="${itemIndex}"] .order-item-discount`);
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
                    'OrderItems[0].UnitPrice': 'Egységár megadása kötelező.',
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

            try {
                const response = await fetch('/api/Orders', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer ' + localStorage.getItem('token'),
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(orderDto)
                });

                let errorData;
                try {
                    errorData = await response.json(); // Try to parse as JSON
                } catch (jsonError) {
                    // Handle non-JSON response
                    errorData = { message: await response.text() || 'Hiba történt a rendelés létrehozása során.' };
                }

                if (response.ok) {
                    console.log('Order created:', errorData);
                    errorContainer.classList.add('d-none');
                    alert('Rendelés sikeresen létrehozva! ID: ' + errorData.orderId);
                    $('#newOrderModal').modal('hide');
                    location.reload();
                } else {
                    let errorText = errorData.message || 'Hiba történt a rendelés létrehozása során.';
                    if (errorData.errors) {
                        Object.keys(errorData.errors).forEach(key => {
                            const field = key.startsWith('$.') ? key.replace('$.', '') : key.replace('OrderCreateDTO.', '');
                            const errorDiv = document.querySelector(`[data-valmsg-for="OrderCreateDTO.${field}"]`) || document.createElement('div');
                            if (!errorDiv.parentElement) {
                                errorDiv.className = 'invalid-feedback';
                                const input = document.querySelector(`[name="OrderCreateDTO.${field}"]`) || document.querySelector(`[name="OrderItems[${key.match(/\d+/)}].${field.split('.').pop()}]`);
                                if (input) input.parentElement.appendChild(errorDiv);
                            }
                            errorDiv.textContent = errorData.errors[key].join(', ');
                            const input = document.querySelector(`[name="OrderCreateDTO.${field}"]`) || document.querySelector(`[name="OrderItems[${key.match(/\d+/)}].${field.split('.').pop()}]`);
                            if (input) input.classList.add('is-invalid');
                        });
                        errorText = 'Kérjük, javítsa a hibás mezőket.';
                    }
                    errorMessage.textContent = errorText;
                    errorContainer.classList.remove('d-none');
                }
            } catch (error) {
                console.error('Error during create:', error);
                errorMessage.textContent = 'Hiba történt a rendelés létrehozása során: ' + error.message;
                errorContainer.classList.remove('d-none');
            }
        });
    }
});