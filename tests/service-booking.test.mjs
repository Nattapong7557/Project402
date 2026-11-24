import assert from 'node:assert/strict';
import test from 'node:test';
import { initializeServiceBooking, populateServiceBooking, useImageFallback } from '../wwwroot/js/service-booking.mjs';

function createImage({ complete, naturalWidth }) {
    return Object.assign(new EventTarget(), { complete, naturalWidth, hidden: false });
}

function createDialog() {
    const fields = {
        '#modalServiceType': { value: '' },
        '#modalItemName': { value: '' },
        '#modalItemDisplay': { textContent: '' },
        '#modalServicePrice': { textContent: '' }
    };
    return { fields, querySelector: selector => fields[selector] };
}

test('keeps an already loaded image visible', () => {
    const image = createImage({ complete: true, naturalWidth: 600 });

    useImageFallback(image);

    assert.equal(image.hidden, false);
});

test('reveals the fallback when the image failed before binding', () => {
    const image = createImage({ complete: true, naturalWidth: 0 });

    useImageFallback(image);

    assert.equal(image.hidden, true);
});

test('keeps a loading image and reveals the fallback after a later failure', () => {
    const image = createImage({ complete: false, naturalWidth: 0 });

    useImageFallback(image);

    assert.equal(image.hidden, false);
    image.dispatchEvent(new Event('error'));
    assert.equal(image.hidden, true);
});

test('keeps a loading image visible when it loads successfully', () => {
    const image = createImage({ complete: false, naturalWidth: 0 });
    useImageFallback(image);

    image.dispatchEvent(new Event('load'));

    assert.equal(image.hidden, false);
});

test('copies the selected service and preserves punctuation as plain text', () => {
    const dialog = createDialog();
    const itemName = 'Portrait “Studio” & Friends\' Day <3';

    populateServiceBooking(dialog, { serviceType: 'Photographer', itemName, price: '2500.50' });

    assert.equal(dialog.fields['#modalServiceType'].value, 'Photographer');
    assert.equal(dialog.fields['#modalItemName'].value, itemName);
    assert.equal(dialog.fields['#modalItemDisplay'].textContent, itemName);
    assert.equal(dialog.fields['#modalServicePrice'].textContent, '฿2,500.5');
});

test('replaces the previous selection and displays a free service as zero', () => {
    const dialog = createDialog();
    populateServiceBooking(dialog, { serviceType: 'Costume', itemName: 'ฮันบก', price: '800' });

    populateServiceBooking(dialog, { serviceType: 'Promotion', itemName: 'แพ็กเกจตัวอย่าง', price: '0' });

    assert.equal(dialog.fields['#modalServiceType'].value, 'Promotion');
    assert.equal(dialog.fields['#modalItemName'].value, 'แพ็กเกจตัวอย่าง');
    assert.equal(dialog.fields['#modalItemDisplay'].textContent, 'แพ็กเกจตัวอย่าง');
    assert.equal(dialog.fields['#modalServicePrice'].textContent, '฿0');
});

test('passes the selected promotion identifier to the booking form', () => {
    const dialog = createDialog();
    dialog.fields['#modalServiceItemId'] = { value: '' };

    populateServiceBooking(dialog, { serviceType: 'Promotion', serviceItemId: '12', itemName: 'Combo', price: '2190' });

    assert.equal(dialog.fields['#modalServiceItemId'].value, '12');
    assert.equal(dialog.fields['#modalServicePrice'].textContent, '฿2,190');
});

function createBookingPage() {
    const dialog = Object.assign(new EventTarget(), createDialog());
    const closeButton = new EventTarget();
    const fields = dialog.querySelector;
    dialog.querySelector = selector => selector === '[data-close-service-booking]' ? closeButton : fields(selector);
    dialog.showModal = () => { dialog.open = true; };
    dialog.close = () => { dialog.open = false; };
    const button = Object.assign(new EventTarget(), {
        dataset: { serviceType: 'Makeup', itemName: 'Natural', price: '1500' }
    });
    const form = Object.assign(new EventTarget(), { dataset: { confirmServiceDeletion: 'Natural' } });
    const elements = {
        '[data-service-image]': [],
        '[data-book-service]': [button],
        '[data-confirm-service-deletion]': [form]
    };
    const root = { querySelectorAll: selector => elements[selector], getElementById: () => dialog };
    return { root, dialog, closeButton, button, form };
}

test('opens the booking dialog for the chosen card and closes from its controls', () => {
    const { root, dialog, closeButton, button } = createBookingPage();
    initializeServiceBooking(root, () => true);

    button.dispatchEvent(new Event('click'));

    assert.ok(dialog.open);
    assert.equal(dialog.fields['#modalItemName'].value, 'Natural');
    closeButton.dispatchEvent(new Event('click'));
    assert.equal(dialog.open, false);

    button.dispatchEvent(new Event('click'));
    dialog.dispatchEvent(new Event('click'));
    assert.equal(dialog.open, false);
});

test('blocks deletion when the administrator declines the confirmation', () => {
    const { root, form } = createBookingPage();
    const messages = [];
    initializeServiceBooking(root, message => {
        messages.push(message);
        return false;
    });
    const event = new Event('submit', { cancelable: true });

    form.dispatchEvent(event);

    assert.equal(event.defaultPrevented, true);
    assert.deepEqual(messages, ['ยืนยันลบ Natural?']);
});

test('allows the existing deletion form to submit when confirmed', () => {
    const { root, form } = createBookingPage();
    initializeServiceBooking(root, () => true);
    const event = new Event('submit', { cancelable: true });

    form.dispatchEvent(event);

    assert.equal(event.defaultPrevented, false);
});
