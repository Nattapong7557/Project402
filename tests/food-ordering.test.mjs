import assert from 'node:assert/strict';
import test from 'node:test';
import { boundedQuantity, cartTotals, changeQuantity } from '../wwwroot/js/food-ordering.mjs';

test('cart sums the selected pieces and their line totals', () => {
    const lines = [{ quantity: 2, price: 85 }, { quantity: 0, price: 95 }, { quantity: 3, price: 49 }];

    const totals = cartTotals(lines);

    assert.deepEqual(totals, { count: 5, subtotal: 317 });
    assert.deepEqual(cartTotals([]), { count: 0, subtotal: 0 });
});

test('quantity controls stay within whole units and the supported limits', () => {
    const values = [-1, 0, 2.9, 20, 21, NaN, Infinity];

    const bounded = values.map(boundedQuantity);

    assert.deepEqual(bounded, [0, 0, 2, 20, 20, 0, 0]);
});


test('cart controls add, subtract and remove using the same bounds as the menu', () => {
    assert.equal(changeQuantity(0, '1'), 1);
    assert.equal(changeQuantity(3, '-1'), 2);
    assert.equal(changeQuantity(20, '1'), 20);
    assert.equal(changeQuantity(0, '-1'), 0);
    assert.equal(changeQuantity(12, 'remove'), 0);
});
