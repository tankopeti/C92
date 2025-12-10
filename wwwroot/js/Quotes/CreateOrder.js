//a programibility-ben van a másoluás tárolt procedúrája az sql serveren

// SET ANSI_NULLS ON
// GO
// SET QUOTED_IDENTIFIER ON
// GO
// -- =============================================
// -- Convert Quote → Order (with all items)
// -- Usage: EXEC dbo.usp_ConvertQuoteToOrder @QuoteId = 123
// -- =============================================
// CREATE   PROCEDURE [dbo].[usp_ConvertQuoteToOrder]
//     @QuoteId INT,
//     @CreatedBy NVARCHAR(100) = NULL   -- optional, will use current user or "System"
// AS
// BEGIN
//     SET NOCOUNT, XACT_ABORT ON;

//     DECLARE @NewOrderId INT;

//     BEGIN TRANSACTION;

//     -- 1. Create the Order header
//     INSERT INTO dbo.Orders (
//         OrderNumber, OrderDate, Deadline, Description, TotalAmount,
//         SalesPerson, DeliveryDate, DiscountPercentage, DiscountAmount,
//         CompanyName, Subject, DetailedDescription,
//         CreatedBy, CreatedDate, ModifiedBy, ModifiedDate,
//         Status, PartnerId, SiteId, CurrencyId,
//         PaymentTerms, ShippingMethod,
//         ReferenceNumber, QuoteId,
//         OrderStatusTypes, PlannedDelivery
//     )
//     SELECT 
//         -- Generate OrderNumber like Q-1001 → O-1001
//         'O-' + RIGHT('00000' + CAST(RIGHT(QuoteNumber, 10) AS VARCHAR(10)), 10),
//         CAST(QuoteDate AS DATE),               -- OrderDate
//         ValidityDate,                          -- use ValidityDate as Deadline
//         Description,
//         TotalAmount - ISNULL(QuoteDiscountAmount,0) - ISNULL(TotalItemDiscounts,0), -- final net amount
//         SalesPerson,
//         NULL,                                  -- DeliveryDate (unknown yet)
//         DiscountPercentage,
//         QuoteDiscountAmount + ISNULL(TotalItemDiscounts,0),
//         CompanyName,
//         Subject,
//         DetailedDescription,
//         ISNULL(@CreatedBy, SUSER_SNAME()),     -- CreatedBy
//         GETUTCDATE(),
//         ISNULL(@CreatedBy, SUSER_SNAME()),
//         GETUTCDATE(),
//         'Pending',                             -- Status
//         PartnerId,
//         SiteId,
//         CurrencyId,
//         NULL,                                  -- PaymentTerms (you can map later)
//         NULL,                                  -- ShippingMethod
//         QuoteNumber,                           -- ReferenceNumber = original Quote number
//         QuoteId,                               -- link back
//         1,                                     -- 1 = Pending (adjust to your OrderStatusTypes table)
//         ValidityDate                           -- PlannedDelivery
//     FROM dbo.Quotes 
//     WHERE QuoteId = @QuoteId AND IsActive = 1;

//     SET @NewOrderId = SCOPE_IDENTITY();

//     -- 2. Copy all QuoteItems → OrderItems
//     INSERT INTO dbo.OrderItems (
//         OrderId, Description, Quantity, UnitPrice, DiscountAmount,
//         CreatedBy, CreatedDate, ModifiedBy, ModifiedDate,
//         ProductId, DiscountType, VatTypeId,
//         DiscountPercentage, BasePrice, PartnerPrice,
//         VolumeThreshold, VolumePrice, ListPrice
//     )
//     SELECT 
//         @NewOrderId,
//         ItemDescription,
//         Quantity,
//         NetDiscountedPrice,                    -- this is already the final unit price after all discounts
//         DiscountAmount,                        -- item-level discount (if any)
//         ISNULL(@CreatedBy, SUSER_SNAME()),
//         GETUTCDATE(),
//         ISNULL(@CreatedBy, SUSER_SNAME()),
//         GETUTCDATE(),
//         ProductId,
//         DiscountTypeId,
//         VatTypeId,
//         -- calculate percentage if you want it in OrderItems too
//         CASE WHEN ListPrice > 0 
//              THEN ROUND((DiscountAmount / (Quantity * ListPrice)) * 100, 2) 
//              ELSE NULL END,
//         ListPrice,
//         PartnerPrice,
//         NULL,                                  -- VolumeThreshold (you can copy logic if you have it)
//         VolumePrice,
//         ListPrice
//     FROM dbo.QuoteItems 
//     WHERE QuoteId = @QuoteId;

//     -- 3. Optional: mark Quote as converted (add a column QuoteStatus or IsConverted if you want)
//     -- UPDATE dbo.Quotes SET Status = 'Converted' WHERE QuoteId = @QuoteId;

//     COMMIT TRANSACTION;

//     SELECT @NewOrderId AS NewOrderId;
// END
// GO




document.addEventListener('DOMContentLoaded', function () {
    console.log('[Quote → Order] Page loaded – initializing Convert to Order buttons');

    // Select all "Convert to Order" buttons
    const convertButtons = document.querySelectorAll('.convert-to-order-btn');

    if (convertButtons.length === 0) {
        console.warn('[Quote → Order] No convert buttons found on this page.');
        return;
    }

    console.log(`[Quote → Order] Found ${convertButtons.length} convert button(s). Binding click events...`);

    convertButtons.forEach(btn => {
        btn.addEventListener('click', async function () {
            const quoteId = this.dataset.quoteId;
            const quoteNumber = this.dataset.quoteNumber || 'Unknown';

            console.log(`[Quote → Order] Convert button clicked → Quote ID: ${quoteId}, Number: ${quoteNumber}`);

            // Confirmation dialog
            const confirmed = confirm(`Are you sure you want to create an order from this quote?\n\nQuote: ${quoteNumber}`);
            if (!confirmed) {
                console.log('[Quote → Order] Conversion cancelled by user.');
                return;
            }

            // UI feedback: disable button + spinner
            this.disabled = true;
            const originalText = this.innerHTML;
            this.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Processing...';
            console.log('[Quote → Order] Button disabled, showing spinner...');

            try {
                console.log(`[Quote → Order] Sending POST request to /api/quotes/${quoteId}/convert-to-order`);

                const response = await fetch(`/api/quotes/${quoteId}/convert-to-order`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    }
                });

                const result = await response.json();

                if (response.ok) {
                    console.log('[Quote → Order] Success!', result);
                    console.log(`[Quote → Order] New Order created → Order ID: ${result.orderId}, Number: ${result.orderNumber || 'N/A'}`);

                    showToast('Order successfully created!', 'success');

                    // Redirect to the new order details page after a short delay
                    setTimeout(() => {
                        console.log(`[Quote → Order] Redirecting to /CRM/Orders/Details?id=${result.orderId}`);
                        window.location.href = `/CRM/Quotes`;
                    }, 1200);

                } else {
                    // API returned error (4xx or 5xx)
                    console.warn('[Quote → Order] API returned error:', response.status, result);
                    const errorMsg = result.message || result.error || 'Unknown error occurred';
                    showToast(`Error: ${errorMsg}`, 'danger');

                    // Restore button
                    this.disabled = false;
                    this.innerHTML = originalText || 'Create Order';
                }
            } catch (err) {
                // Network or unexpected JS error
                console.error('[Quote → Order] Request failed (network or JS error):', err);
                showToast('Network error – please check your connection', 'danger');

                // Restore button
                this.disabled = false;
                this.innerHTML = originalText || 'Create Order';
            }
        });
    });

    // Toast notification helper
    function showToast(message, type = 'info') {
        console.log(`[Toast] Showing: ${message} (${type})`);

        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.role = 'alert';
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>`;

        const container = document.getElementById('toastContainer');
        if (container) {
            container.appendChild(toast);
            const bsToast = new bootstrap.Toast(toast, { delay: 4000 });
            bsToast.show();

            // Auto-remove after hiding
            toast.addEventListener('hidden.bs.toast', () => toast.remove());
        } else {
            console.warn('[Toast] toastContainer not found – falling back to alert');
            alert(message);
        }
    }

    console.log('[Quote → Order] Initialization complete.');
});