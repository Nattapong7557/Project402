import assert from 'node:assert/strict';
import test from 'node:test';
import { startBookingStatusUpdates } from '../wwwroot/js/booking-status.mjs';

function fixture(ids, fetch) {
    const message = { hidden: true, textContent: '' };
    const cards = ids.map(id => {
        const badge = { textContent: 'รออนุมัติ' };
        const cancel = { hidden: false };
        const bar = { value: 20 };
        const labels = [20, 45, 65, 85, 100].map(step => {
            const classes = new Set();
            return {
                dataset: { progressStep: String(step) },
                classes,
                classList: {
                    toggle(name, enabled) {
                        if (enabled) classes.add(name);
                        else classes.delete(name);
                    }
                }
            };
        });
        const progress = { hidden: false, querySelector: () => bar, querySelectorAll: () => labels };
        return {
            badge, cancel, bar, labels, progress, dataset: { bookingId: String(id) },
            querySelector: selector => ({
                '[data-booking-status]': badge,
                '[data-booking-progress]': progress,
                '[data-booking-cancel]': cancel
            })[selector]
        };
    });
    const page = {
        dataset: { bookingStatusUrl: '/Booking/StatusUpdates' },
        querySelector: () => message,
        querySelectorAll: () => cards
    };
    const hooks = {};
    const document = {
        hidden: false,
        addEventListener: (type, handler) => { hooks[type] = handler; },
        removeEventListener: type => { delete hooks[type]; }
    };
    const timers = {
        setInterval: (callback, delay) => { hooks.tick = callback; hooks.delay = delay; return 1; },
        clearInterval: id => { assert.equal(id, 1); delete hooks.tick; }
    };
    startBookingStatusUpdates(page, { document, fetch, ...timers });
    return { cards, message, document, hooks };
}

const statusResponse = (id, status) => ({
    ok: true, status: 200, json: async () => [{ id, status }]
});

test('the next tick updates the matching card and hides cancellation when preparation starts', async () => {
    const preparing = { label: 'กำลังจัดเตรียม', badgeClass: 'amber', progress: 65, canCancel: false };
    const cancelled = { label: 'ยกเลิกแล้ว', badgeClass: 'rose', progress: 0, canCancel: false };
    const responses = [statusResponse(2, preparing), statusResponse(2, cancelled)];
    const { cards: [unrelated, card], hooks } = fixture([1, 2], async (url, options) => {
        assert.equal(url, '/Booking/StatusUpdates');
        assert.equal(options.cache, 'no-store');
        return responses.shift();
    });

    assert.equal(hooks.delay, 15_000);
    await hooks.tick();
    assert.equal(unrelated.badge.textContent, 'รออนุมัติ');
    assert.equal(card.badge.textContent, 'กำลังจัดเตรียม');
    assert.equal(card.badge.className, 'amber');
    assert.equal(card.bar.value, 65);
    assert.equal(card.cancel.hidden, true);
    assert.equal(card.labels[2].classes.has('text-dark'), true);
    assert.equal(card.labels[3].classes.has('text-dark'), false);
    await hooks.tick();
    assert.equal(card.progress.hidden, true);
    assert.equal(card.labels[2].classes.has('fw-bold'), false);
});

test('requests pause in a hidden tab and overlapping ticks do not send another request', async () => {
    let calls = 0;
    let finish;
    const { document, hooks } = fixture([1], () => {
        calls++;
        return new Promise(resolve => { finish = resolve; });
    });
    document.hidden = true;
    await hooks.tick();
    assert.equal(calls, 0);

    document.hidden = false;
    const resumed = hooks.visibilitychange();
    await hooks.tick();
    assert.equal(calls, 1);
    finish({ ok: true, json: async () => [] });
    await resumed;
});

test('transient failures keep polling and session expiration stops polling', async () => {
    const responses = [
        { status: 500, ok: false },
        { status: 200, ok: true, json: async () => [] },
        { status: 401, ok: false }
    ];
    const { hooks, message } = fixture([1], async () => responses.shift());

    await hooks.tick();
    assert.equal(message.hidden, false);
    assert.match(message.textContent, /ลองอีกครั้ง/);
    await hooks.tick();
    assert.equal(message.hidden, true);
    await hooks.tick();
    assert.match(message.textContent, /เข้าสู่ระบบอีกครั้ง/);
    assert.equal(hooks.tick, undefined);
    assert.equal(hooks.visibilitychange, undefined);
});

test('an empty bookings page does not schedule polling', () => {
    const { hooks } = fixture([], () => assert.fail('Unexpected request'));

    assert.equal(hooks.tick, undefined);
});
