// window.initializeProductTomSelect = function(selectElement, {
//     partnerId = null,
//     quoteDate = null,
//     quantity = 1,
//     selectedProductId = null,
//     selectedProductName = ''
// } = {}) {
//     if (typeof TomSelect === 'undefined') {
//         console.error('TomSelect library is not loaded.');
//         window.showToast('Error', 'TomSelect library missing.');
//         return;
//     }

//     if (!selectElement) {
//         console.error('No select element provided for product TomSelect');
//         window.showToast('Error', 'Product select element not found.');
//         return;
//     }

//     const modal = selectElement.closest('.modal') || document.querySelector('#newOrderModal');
//     if (!modal) {
//         console.error('Modal not found for product select');
//         window.showToast('Error', 'Modal not found.');
//         return;
//     }

//     const row = selectElement.closest('.order-item-row');
//     if (!row) {
//         console.error('Row not found for product select', selectElement);
//         window.showToast('Error', 'Product row not found.');
//         return;
//     }

//     const orderId = modal.id.replace('editOrderModal_', '').replace('newOrderModal', 'new');
//     const quantityInput = row.querySelector('.quantity');
//     if (!quantityInput) {
//         console.error('Quantity input not found for orderId:', orderId);
//         window.showToast('Error', 'Quantity input missing.');
//         return;
//     }

//     console.log('Initializing product TomSelect for orderId:', orderId, 'partnerId:', partnerId);

//     function fetchProducts(quantity) {
//         const effectivePartnerId = partnerId ?? modal.dataset.partnerId ?? null;
//         const effectiveQuoteDate = quoteDate ? new Date(quoteDate).toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
//         const parsedQuantity = parseInt(quantity, 10) || 1;

//         if (!effectivePartnerId) {
//             console.error('No partnerId provided for product fetch');
//             window.showToast('Error', 'Please select a partner first.');
//             selectElement.innerHTML = '<option value="" disabled selected>Select a partner first</option>';
//             return;
//         }

//         const apiUrl = `/api/product?partnerId=${encodeURIComponent(effectivePartnerId)}&eDate=${encodeURIComponent(effectiveQuoteDate)}&quantity=${encodeURIComponent(parsedQuantity)}`;
//         console.log('Fetching products from:', apiUrl);

//         selectElement.innerHTML = '<option value="" disabled selected>Loading products...</option>';

//         fetch(apiUrl, {
//             headers: {
//                 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
//             }
//         })
//             .then(response => {
//                 console.log('Product API response status:', response.status);
//                 if (!response.ok) {
//                     throw new Error(`HTTP ${response.status}: ${response.statusText}`);
//                 }
//                 return response.json();
//             })
//             .then(data => {
//                 console.log('API response data:', data);
//                 const products = Array.isArray(data) ? data : [];
//                 if (products.length === 0) {
//                     console.warn('No products returned for partnerId:', effectivePartnerId);
//                     window.showToast('Warning', 'No products available for the selected partner.');
//                     selectElement.innerHTML = '<option value="" disabled selected>No products available</option>';
//                     return;
//                 }

//                 if (selectElement.tomselect) {
//                     selectElement.tomselect.destroy();
//                 }

//                 new TomSelect(selectElement, {
//                     create: false,
//                     searchField: ['name'],
//                     maxItems: 1,
//                     valueField: 'productId',
//                     labelField: 'name',
//                     placeholder: '-- Válasszon terméket --',
//                     allowEmptyOption: true,
//                     maxOptions: 100,
//                     options: products,
//                     render: {
//                         option: item => `<div>${item.name}</div>`,
//                         item: item => `<div>${item.name}</div>`,
//                         no_results: data => `<div class="no-results">Nincs találat "${data.input}"</div>`
//                     },
//                     onInitialize: function() {
//                         if (selectedProductId && products.find(p => p.productId == selectedProductId)) {
//                             this.addItem(selectedProductId);
//                             window.updatePriceFields(selectElement, selectedProductId, products);
//                         }
//                     },
//                     onChange: function(value) {
//                         const selectedProduct = products.find(p => p.productId == value);
//                         if (selectedProduct && row.closest('tbody')) {
//                             selectElement.dataset.selectedId = value;
//                             selectElement.dataset.selectedText = selectedProduct.name;
//                             window.updatePriceFields(selectElement, value, products);
//                             window.calculateOrderTotals(orderId);
//                         }
//                     }
//                 });

//                 console.log('TomSelect initialized with products:', products.length);
//             })
//             .catch(error => {
//                 console.error('Failed to fetch products for orderId:', orderId, 'Error:', error);
//                 window.showToast('Error', `Failed to load products: ${error.message}`);
//                 selectElement.innerHTML = '<option value="" disabled selected>Error loading products</option>';
//             });
//     }

//     fetchProducts(quantity);
// };

// window.initializePartnerTomSelect = function(select, orderId) {
//     const selectedId = select.dataset.selectedId || '';
//     const selectedText = select.dataset.selectedText || '';
//     console.log('Tom Select Partner Init for orderId:', orderId, 'selectedId:', selectedId, 'selectedText:', selectedText);

//     if (select.tomselect) {
//         select.tomselect.destroy();
//         console.log('Destroyed existing TomSelect for partnerId:', orderId);
//     }

//     const control = new TomSelect(select, {
//         valueField: 'id',
//         labelField: 'text',
//         searchField: 'text',
//         placeholder: '-- Válasszon partnert --',
//         allowEmptyOption: true,
//         maxOptions: 100,
//         load: function(query, callback) {
//             const url = `/api/partners?term=${encodeURIComponent(query)}`;
//             console.log('Partner Search Query:', url);
//             fetch(url)
//                 .then(response => {
//                     if (!response.ok) {
//                         return response.text().then(text => {
//                             throw new Error(`HTTP error: ${response.status}, ${text}`);
//                         });
//                     }
//                     return response.json();
//                 })
//                 .then(data => {
//                     console.log('API /api/partners Response:', data);
//                     callback(Array.isArray(data) ? data : []);
//                 })
//                 .catch(error => {
//                     console.error('Partner Search Error:', error);
//                     window.showToast('Error', `Failed to load partners: ${error.message}`);
//                     callback([]);
//                 });
//         },
//         render: {
//             option: function(data, escape) {
//                 return `<div>${escape(data.text)}</div>`;
//             },
//             item: function(data, escape) {
//                 return `<div>${escape(data.text)}</div>`;
//             },
//             no_results: function(data, escape) {
//                 return `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`;
//             }
//         },
//         onInitialize: function() {
//             if (selectedId && selectedText) {
//                 this.addOption({ id: selectedId, text: selectedText });
//                 this.setValue(selectedId, true);
//                 console.log('Initialized partner with pre-selected:', selectedId, selectedText);
//             }
//             this.load('');
//         },
//         onChange: function(value) {
//             console.log('Partner changed for orderId:', orderId, 'New partnerId:', value);
//             const partner = this.options[value] || {};
//             console.log('Partner data passed to site and quote:', JSON.stringify(partner, null, 2));
//             const siteSelect = document.querySelector('#site-select_new');
//             const quoteSelect = document.querySelector('#quote-select_new');
//             // Remove existing change handlers to prevent duplicates
//             if (siteSelect && siteSelect.tomselect) {
//                 siteSelect.tomselect.destroy();
//                 console.log('Destroyed existing site TomSelect to prevent duplicate initialization');
//             }
//             if (quoteSelect && quoteSelect.tomselect) {
//                 quoteSelect.tomselect.destroy();
//                 console.log('Destroyed existing quote TomSelect to prevent duplicate initialization');
//             }
//             if (siteSelect) window.initializeSiteTomSelect(siteSelect, orderId, partner);
//             if (quoteSelect) window.initializeQuoteTomSelect(quoteSelect, orderId, partner);
//         }
//     });

//     return control;
// };

// window.initializeSiteTomSelect = function(select, orderId, partnerData = null) {
//     const selectedId = select.dataset.selectedId || '';
//     const selectedText = select.dataset.selectedText || '';
//     console.log('Tom Select Site Init for orderId:', orderId, 'selectedId:', selectedId, 'selectedText:', selectedText, 'partnerData:', JSON.stringify(partnerData, null, 2));

//     if (select.tomselect) {
//         select.tomselect.destroy();
//         console.log('Destroyed existing TomSelect for site-select_new');
//     }

//     const sites = partnerData?.sites || [];
//     console.log('Available sites for partner:', sites.length, JSON.stringify(sites, null, 2));

//     // Clear existing options in the select element
//     select.innerHTML = '<option value="" disabled selected>-- Válasszon telephelyet --</option>';

//     const control = new TomSelect(select, {
//         dropdownParent: 'body',
//         valueField: 'id',
//         labelField: 'text',
//         searchField: 'text',
//         placeholder: '-- Válasszon telephelyet --',
//         allowEmptyOption: true,
//         maxOptions: 100,
//         options: [], // Initialize with empty options to avoid duplication
//         load: function(query, callback) {
//             if (sites.length > 0) {
//                 const filteredSites = sites.filter(s => s.text.toLowerCase().includes(query.toLowerCase()));
//                 console.log('Filtered sites for query:', query, filteredSites.length, JSON.stringify(filteredSites, null, 2));
//                 callback(filteredSites);
//                 return;
//             }
//             const url = `/api/sites?search=${encodeURIComponent(query)}`;
//             console.log('Site Search Query:', url);
//             fetch(url)
//                 .then(response => {
//                     if (!response.ok) {
//                         throw new Error(`HTTP error: ${response.status}`);
//                     }
//                     return response.json();
//                 })
//                 .then(data => {
//                     console.log('API /api/sites Response:', JSON.stringify(data, null, 2));
//                     callback(Array.isArray(data) ? data : []);
//                 })
//                 .catch(error => {
//                     console.error('Site Search Error:', error);
//                     window.showToast('Error', `Failed to load sites: ${error.message}`);
//                     callback([]);
//                 });
//         },
//         shouldLoad: function(query) {
//             return sites.length === 0;
//         },
//         render: {
//             option: function(data, escape) {
//                 const primaryBadge = data.isPrimary ? '<span class="badge bg-primary ms-2">Elsődleges</span>' : '';
//                 console.log('Rendering site option:', data.text, 'id:', data.id, 'isPrimary:', data.isPrimary);
//                 return `<div data-value="${data.id}">${escape(data.text)}${primaryBadge}</div>`;
//             },
//             item: function(data, escape) {
//                 console.log('Rendering selected site:', data.text, 'id:', data.id);
//                 return `<div>${escape(data.text)}</div>`;
//             },
//             no_results: function(data, escape) {
//                 return `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`;
//             }
//         },
//         onInitialize: function() {
//             console.log('Initializing site TomSelect with sites:', sites.length);
//             // Explicitly add all sites
//             sites.forEach(site => {
//                 this.addOption(site);
//                 console.log('Added site option:', site.text, 'id:', site.id, 'isPrimary:', site.isPrimary);
//             });
//             // Set primary site after all options are added
//             if (sites.length > 0) {
//                 const primarySite = sites.find(s => s.isPrimary);
//                 if (primarySite) {
//                     this.addItem(primarySite.id, true); // Use addItem to ensure rendering
//                     console.log('Set primary site:', primarySite.text, 'id:', primarySite.id, 'for orderId:', orderId);
//                 } else {
//                     this.addItem(sites[0].id, true);
//                     console.log('Set fallback site:', sites[0].text, 'id:', sites[0].id, 'for orderId:', orderId);
//                 }
//             } else if (selectedId && selectedText) {
//                 this.addOption({ id: selectedId, text: selectedText, isPrimary: false });
//                 this.addItem(selectedId, true);
//                 console.log('Set pre-selected site:', selectedText, 'id:', selectedId);
//             }
//             // Log the dropdown HTML for debugging
//             const dropdownContent = document.querySelector('.ts-dropdown-content');
//             console.log('Dropdown HTML after initialization:', dropdownContent ? dropdownContent.innerHTML : 'Dropdown not found');
//             console.log('Site TomSelect initialized with options:', JSON.stringify(this.options, null, 2));
//         }
//     });

//     console.log('Tom Select Site initialized for orderId:', orderId, 'with sites:', sites.length);
//     return control;
// };


// window.initializeQuoteTomSelect = function(selectElement, orderId, partner) {
//     if (!selectElement) {
//         console.error('Quote select element not found for orderId:', orderId);
//         return;
//     }

//     console.log('Tom Select Quote Init for orderId:', orderId, 'partner:', partner);

//     const quotes = partner?.quotes ?? [];
//     if (selectElement.tomselect) {
//         selectElement.tomselect.destroy();
//     }

//     selectElement.innerHTML = '<option value="">Válasszon árajánlatot</option>';
//     quotes.forEach(q => {
//         const option = document.createElement('option');
//         option.value = q.id;
//         option.text = q.text;
//         selectElement.appendChild(option);
//     });

//     new TomSelect(selectElement, {
//         create: false,
//         sortField: {
//             field: 'text',
//             direction: 'asc'
//         },
//         dropdownParent: 'body',
//         placeholder: 'Válasszon árajánlatot...',
//         render: {
//             option: function(data, escape) {
//                 return `<div>${escape(data.text)}</div>`;
//             },
//             item: function(data, escape) {
//                 return `<div>${escape(data.text)}</div>`;
//             },
//             no_results: function(data, escape) {
//                 return `<div class="no-results">Nincs találat "${escape(data.input)}"</div>`;
//             }
//         },
//         onInitialize: function() {
//             const selectedId = selectElement.dataset.selectedId;
//             const selectedText = selectElement.dataset.selectedText;
//             if (selectedId && selectedText) {
//                 this.addOption({ id: selectedId, text: selectedText });
//                 this.setValue(selectedId);
//             }
//         }
//     });
// };