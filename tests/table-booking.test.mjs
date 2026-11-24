import assert from 'node:assert/strict';
import test from 'node:test';
import { checkedSlot } from '../wwwroot/js/table-booking.mjs';

test('changing zone keeps the booked time only if it remains available', () => {
    const slots = ['11:00 - 13:00', '13:00 - 15:00'];

    assert.equal(checkedSlot(slots, slots[1], [slots[0]]), slots[1]);
    assert.equal(checkedSlot(slots, slots[1], [slots[1]]), null);
    assert.equal(checkedSlot(slots, undefined, []), null);
    assert.equal(checkedSlot(slots, '15:00 - 17:00', []), null);
});
