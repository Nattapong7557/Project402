const priceFormatter = new Intl.NumberFormat('th-TH', { maximumFractionDigits: 2 });

export function useImageFallback(image) {
    const showFallback = () => { image.hidden = true; };
    image.addEventListener('error', showFallback, { once: true });

    if (image.complete && image.naturalWidth === 0) {
        showFallback();
    }
}

export function populateServiceBooking(dialog, { serviceType, serviceItemId, itemName, price }) {
    dialog.querySelector('#modalServiceType').value = serviceType;
    const itemIdField = dialog.querySelector('#modalServiceItemId');
    if (itemIdField) itemIdField.value = serviceItemId ?? '';
    dialog.querySelector('#modalItemName').value = itemName;
    dialog.querySelector('#modalItemDisplay').textContent = itemName;
    dialog.querySelector('#modalServicePrice').textContent = '฿' + priceFormatter.format(Number(price));
}

export function initializeServiceBooking(root, confirmDeletion) {
    root.querySelectorAll('[data-service-image]').forEach(useImageFallback);
    const dialog = root.getElementById('serviceBookingModal');
    if (!dialog) return;

    root.querySelectorAll('[data-book-service]').forEach(button => {
        button.addEventListener('click', () => {
            populateServiceBooking(dialog, button.dataset);
            dialog.showModal();
        });
    });

    dialog.querySelector('[data-close-service-booking]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('click', event => {
        if (event.target === dialog) dialog.close();
    });

    root.querySelectorAll('[data-confirm-service-deletion]').forEach(form => {
        form.addEventListener('submit', event => {
            if (!confirmDeletion(`ยืนยันลบ ${form.dataset.confirmServiceDeletion}?`)) {
                event.preventDefault();
            }
        });
    });
}

if (typeof document !== 'undefined') {
    initializeServiceBooking(document, message => window.confirm(message));
}
