import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const bookingHeaders = {
    Promotions: 'promotions',
    Makeup: 'makeup',
    Photographer: 'photographer',
    Costumes: 'costumes',
    Table: 'table',
    Index: 'bookings'
};

for (const [viewName, assetName] of Object.entries(bookingHeaders)) {
    test(`${viewName} references its own local WebP header`, async () => {
        const viewPath = new URL(`../Views/Booking/${viewName}.cshtml`, import.meta.url);
        const assetPath = new URL(`../wwwroot/images/header-${assetName}.webp`, import.meta.url);

        const [view, asset] = await Promise.all([readFile(viewPath, 'utf8'), readFile(assetPath)]);

        assert.deepEqual(view.match(/~\/images\/header-[a-z]+\.webp/g), [`~/images/header-${assetName}.webp`]);
        assert.equal(asset.toString('ascii', 0, 4), 'RIFF');
        assert.equal(asset.toString('ascii', 8, 12), 'WEBP');
        assert.equal(asset.readUInt32LE(4) + 8, asset.length);
    });
}

test('the six booking headers contain distinct images', async () => {
    const paths = Object.values(bookingHeaders).map(name => new URL(`../wwwroot/images/header-${name}.webp`, import.meta.url));

    const images = await Promise.all(paths.map(path => readFile(path)));
    const hashes = images.map(image => createHash('sha256').update(image).digest('hex'));

    assert.equal(new Set(hashes).size, paths.length);
});
