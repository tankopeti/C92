document.addEventListener('DOMContentLoaded', function () {
    console.log('orders.js loaded');

    // Initialize TomSelect for dropdowns
    function initializeTomSelect(selectElement, endpoint, valueField = 'id', labelField = 'text', getDynamicParams = null) {
        try {
            new TomSelect(selectElement, {
                valueField: valueField,
                labelField: labelField,
                searchField: [labelField],
                placeholder: 'Válasszon...',
                allowEmptyOption: true,
                load: function (query, callback) {
                    let url = endpoint;
                    if (getDynamicParams) {
                        url = getDynamicParams(query);
                        if (!url) {
                            callback(); // No PartnerId selected
                            return;
                        }
                    } else if (query) {
                        url += `${endpoint.includes('?') ? '&' : '?'}search=${encodeURIComponent(query)}`;
                    }
                    fetch(url, {
                        headers: {
                            'Authorization': 'Bearer ' + (document.querySelector('meta[name="jwt-token"]')?.content || '')
                        }
                    })
                        .then(response => {
                            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                            return response.json();
                        })
                        .then(data => callback(data))
                        .catch(error => {
                            console.error(`Error fetching data from ${url}:`, error);
                            callback();
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
            console.error('Error initializing TomSelect:', e);
        }
    }

    // Initialize TomSelect for PartnerId dropdown
    const partnerSelect = document.querySelector('#partnerIdSelect');
    if (partnerSelect) {
        initializeTomSelect(partnerSelect, '/api/partners/select', 'id', 'text');
    }

    // Initialize TomSelect for CurrencyId dropdown
    const currencySelect = document.querySelector('#currencyIdSelect');
    if (currencySelect) {
        initializeTomSelect(currencySelect, '/api/currencies', 'id', 'text');
    }

    // Initialize TomSelect for ContactId dropdown (depends on PartnerId)
    const contactSelect = document.querySelector('#contactIdSelect');
    if (contactSelect) {
        const getContactEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/partners/${partnerId}/contacts/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(contactSelect, '', 'id', 'text', getContactEndpoint);
    }

    // Initialize TomSelect for SiteId dropdown (depends on PartnerId)
    const siteSelect = document.querySelector('#siteIdSelect');
    if (siteSelect) {
        const getSiteEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/partners/${partnerId}/sites/select${query ? `?search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(siteSelect, '', 'id', 'text', getSiteEndpoint);
    }

    // Initialize TomSelect for ShippingMethodId dropdown
    const shippingMethodSelect = document.querySelector('#shippingMethodIdSelect');
    if (shippingMethodSelect) {
        initializeTomSelect(shippingMethodSelect, '/api/ordershippingmethods/select', 'id', 'text');
    }

    // Initialize TomSelect for PaymentTermId dropdown
    const paymentTermSelect = document.querySelector('#paymentTermIdSelect');
    if (paymentTermSelect) {
        initializeTomSelect(paymentTermSelect, '/api/paymentterms/select', 'id', 'text');
    }

    // Initialize TomSelect for QuoteId dropdown (depends on PartnerId)
    const quoteSelect = document.querySelector('#quoteIdSelect');
    if (quoteSelect) {
        const getQuoteEndpoint = (query) => {
            const partnerId = partnerSelect ? partnerSelect.value : '';
            if (!partnerId) return null;
            return `/api/quotes/select?partnerId=${partnerId}${query ? `&search=${encodeURIComponent(query)}` : ''}`;
        };
        initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
    }

    // Update ContactId, SiteId, and QuoteId dropdowns when PartnerId changes
    if (partnerSelect && (contactSelect || siteSelect || quoteSelect)) {
        partnerSelect.addEventListener('change', function () {
            if (contactSelect && contactSelect.tomselect) {
                contactSelect.tomselect.clear();
                contactSelect.tomselect.clearOptions();
                contactSelect.tomselect.load('');
            }
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
        });
    }

    // Initialize TomSelect for other static dropdowns (none remain)
    const staticDropdowns = document.querySelectorAll(''); // Empty since all dropdowns are dynamic
    staticDropdowns.forEach(select => {
        try {
            new TomSelect(select, {
                placeholder: 'Válasszon...',
                allowEmptyOption: true
            });
        } catch (e) {
            console.error('Error initializing TomSelect for static dropdown:', e);
        }
    });

    // Dynamic order items
    const addButton = document.getElementById('addOrderItemButton');
    if (addButton) {
        addButton.addEventListener('click', function () {
            const container = document.getElementById('orderItemsContainer');
            if (!container) {
                console.error('orderItemsContainer not found');
                return;
            }
            const index = container.children.length;
            const itemHtml = `
                <div class="order-item mb-3 p-3 border rounded" data-index="${index}">
                    <h6>Tétel ${index + 1}</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="mb-3">
                                <label class="form-label">Leírás</label>
                                <input name="OrderCreateDTO.OrderItems[${index}].Description" class="form-control" required />
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Mennyiség</label>
                                <input name="OrderCreateDTO.OrderItems[${index}].Quantity" type="number" step="1" class="form-control" required />
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Egységár</label>
                                <input name="OrderCreateDTO.OrderItems[${index}].UnitPrice" type="number" step="0.01" class="form-control" required />
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Kedvezmény összege</label>
                                <input name="OrderCreateDTO.OrderItems[${index}].DiscountAmount" type="number" step="0.01" class="form-control" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="mb-3">
                                <label class="form-label">Kedvezmény típusa</label>
                                <input name="OrderCreateDTO.OrderItems[${index}].DiscountType" class="form-control" />
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Termék</label>
                                <select name="OrderCreateDTO.OrderItems[${index}].ProductId" class="form-control tomselect-item">
                                    <option value="">Válasszon...</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">ÁFA típus</label>
                                <select name="OrderCreateDTO.OrderItems[${index}].VatTypeId" class="form-control tomselect-item">
                                    <option value="">Válasszon...</option>
                                </select>
                            </div>
                            <button type="button" class="btn btn-danger btn-sm remove-order-item">Tétel törlése</button>
                        </div>
                    </div>
                </div>`;
            container.insertAdjacentHTML('beforeend', itemHtml);

            // Initialize TomSelect for new dropdowns
            const newProductSelect = container.querySelector(`.order-item[data-index="${index}"] select[name*="ProductId"]`);
            const newVatTypeSelect = container.querySelector(`.order-item[data-index="${index}"] select[name*="VatTypeId"]`);
            if (newProductSelect) {
                initializeTomSelect(newProductSelect, '/api/products/select', 'id', 'text');
            }
            if (newVatTypeSelect) {
                initializeTomSelect(newVatTypeSelect, '/api/vattypes/select', 'id', 'text');
            }

            const removeButton = container.querySelector(`.order-item[data-index="${index}"] .remove-order-item`);
            if (removeButton) {
                removeButton.addEventListener('click', function () {
                    const item = container.querySelector(`.order-item[data-index="${index}"]`);
                    item.querySelectorAll('.tomselect-item').forEach(select => {
                        if (select.tomselect) {
                            select.tomselect.destroy();
                        }
                    });
                    item.remove();
                    updateItemIndices();
                });
            }
        });
    } else {
        console.error('addOrderItemButton not found');
    }

    function updateItemIndices() {
        const items = document.querySelectorAll('#orderItemsContainer .order-item');
        items.forEach((item, index) => {
            item.dataset.index = index;
            item.querySelector('h6').textContent = `Tétel ${index + 1}`;
            const inputs = item.querySelectorAll('input, select');
            inputs.forEach(input => {
                const name = input.name.replace(/OrderCreateDTO\.OrderItems\[\d+\]/, `OrderCreateDTO.OrderItems[${index}]`);
                input.name = name;
            });
        });
    }

    // Form validation
    try {
        if (typeof $.fn.validate === 'function') {
            $(document).ready(function () {
                $("#createOrderForm").validate({
                    errorElement: 'span',
                    errorClass: 'text-danger',
                    highlight: function (element) {
                        $(element).addClass('is-invalid');
                    },
                    unhighlight: function (element) {
                        $(element).removeClass('is-invalid');
                    },
                    ignore: [],
                    rules: {
                        'OrderCreateDTO.PartnerId': { required: true },
                        'OrderCreateDTO.CurrencyId': { required: true },
                        'OrderCreateDTO.ContactId': { required: true },
                        'OrderCreateDTO.SiteId': { required: true },
                        'OrderCreateDTO.ShippingMethodId': { required: true },
                        'OrderCreateDTO.PaymentTermId': { required: true },
                        'OrderCreateDTO.QuoteId': { required: false } // Optional, adjust as needed
                    },
                    messages: {
                        'OrderCreateDTO.PartnerId': { required: 'Kérem, válasszon egy partnert.' },
                        'OrderCreateDTO.CurrencyId': { required: 'Kérem, válasszon egy pénznemet.' },
                        'OrderCreateDTO.ContactId': { required: 'Kérem, válasszon egy kapcsolattartót.' },
                        'OrderCreateDTO.SiteId': { required: 'Kérem, válasszon egy telephelyet.' },
                        'OrderCreateDTO.ShippingMethodId': { required: 'Kérem, válasszon egy szállítási módot.' },
                        'OrderCreateDTO.PaymentTermId': { required: 'Kérem, válasszon egy fizetési feltételt.' },
                        'OrderCreateDTO.QuoteId': { required: 'Kérem, válasszon egy árajánlatot.' }
                    }
                });
            });
        } else {
            console.warn('jQuery Validate is not loaded, skipping form validation');
        }
    } catch (e) {
        console.error('Error initializing form validation:', e);
    }

    // Fix modal backdrop issue
    const newOrderModal = document.getElementById('newOrderModal');
    if (newOrderModal) {
        newOrderModal.addEventListener('hidden.bs.modal', function () {
            console.log('Modal hidden, cleaning up');
            const form = document.getElementById('createOrderForm');
            if (form) {
                form.reset();
                if (typeof $.fn.validate === 'function') {
                    $(form).validate().resetForm();
                    $(form).find('.is-invalid').removeClass('is-invalid');
                }
                const container = document.getElementById('orderItemsContainer');
                if (container) {
                    container.querySelectorAll('.tomselect-item').forEach(select => {
                        if (select.tomselect) {
                            select.tomselect.destroy();
                        }
                    });
                    container.innerHTML = '';
                }
            }
            // Destroy TomSelect instances
            [partnerSelect, currencySelect, contactSelect, siteSelect, shippingMethodSelect, paymentTermSelect, quoteSelect].forEach(select => {
                if (select && select.tomselect) {
                    select.tomselect.destroy();
                }
            });
            // Remove modal backdrop
            const backdrops = document.querySelectorAll('.modal-backdrop');
            backdrops.forEach(backdrop => backdrop.remove());
            // Reset body styles
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';
            // Reinitialize TomSelect
            if (partnerSelect) {
                initializeTomSelect(partnerSelect, '/api/partners/select', 'id', 'text');
            }
            if (currencySelect) {
                initializeTomSelect(currencySelect, '/api/currencies', 'id', 'text');
            }
            if (contactSelect) {
                initializeTomSelect(contactSelect, '', 'id', 'text', getContactEndpoint);
            }
            if (siteSelect) {
                initializeTomSelect(siteSelect, '', 'id', 'text', getSiteEndpoint);
            }
            if (shippingMethodSelect) {
                initializeTomSelect(shippingMethodSelect, '/api/ordershippingmethods/select', 'id', 'text');
            }
            if (paymentTermSelect) {
                initializeTomSelect(paymentTermSelect, '/api/paymentterms/select', 'id', 'text');
            }
            if (quoteSelect) {
                initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
            }
        });

        newOrderModal.addEventListener('show.bs.modal', function () {
            if (partnerSelect && !partnerSelect.tomselect) {
                initializeTomSelect(partnerSelect, '/api/partners/select', 'id', 'text');
            }
            if (currencySelect && !currencySelect.tomselect) {
                initializeTomSelect(currencySelect, '/api/currencies', 'id', 'text');
            }
            if (contactSelect && !contactSelect.tomselect) {
                initializeTomSelect(contactSelect, '', 'id', 'text', getContactEndpoint);
            }
            if (siteSelect && !siteSelect.tomselect) {
                initializeTomSelect(siteSelect, '', 'id', 'text', getSiteEndpoint);
            }
            if (shippingMethodSelect && !shippingMethodSelect.tomselect) {
                initializeTomSelect(shippingMethodSelect, '/api/ordershippingmethods/select', 'id', 'text');
            }
            if (paymentTermSelect && !paymentTermSelect.tomselect) {
                initializeTomSelect(paymentTermSelect, '/api/paymentterms/select', 'id', 'text');
            }
            if (quoteSelect && !quoteSelect.tomselect) {
                initializeTomSelect(quoteSelect, '', 'id', 'text', getQuoteEndpoint);
            }
        });
    }
});