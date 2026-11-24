import assert from 'node:assert/strict';
import test from 'node:test';
import { indicatorGeometry } from '../wwwroot/js/navbar-indicator.mjs';

test('the underline is centered on the label rather than matching the full button', () => {
    const group = { left: 130, top: 20 };
    const link = { bottom: 68 };
    const label = { left: 232, width: 49.5 };

    assert.deepEqual(indicatorGeometry(group, link, label), { left: 102, top: 43, width: 49.5 });
});

test('the underline follows a different row and width in an expanded mobile menu', () => {
    const group = { left: 12, top: 70 };

    const first = indicatorGeometry(group, { bottom: 118 }, { left: 24, width: 51 });
    const second = indicatorGeometry(group, { bottom: 166 }, { left: 24, width: 77 });

    assert.deepEqual(first, { left: 12, top: 43, width: 51 });
    assert.deepEqual(second, { left: 12, top: 91, width: 77 });
});
