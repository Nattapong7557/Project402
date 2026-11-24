import assert from 'node:assert/strict';
import test from 'node:test';
import { initializeMenuCarousel } from '../wwwroot/js/menu-carousel.mjs';

function element(dataset = {}) {
    const attributes = new Map();
    return Object.assign(new EventTarget(), {
        dataset,
        hidden: false,
        setAttribute: (name, value) => attributes.set(name, value),
        getAttribute: name => attributes.get(name),
        click() { this.dispatchEvent(new Event('click')); }
    });
}

const cardWidth = 100;

function createRow(initialCards) {
    const previous = element();
    const next = element();
    const scrolls = [];
    const grid = {
        children: [],
        get firstElementChild() { return this.children[0]; },
        append(...cards) {
            for (const card of cards) {
                if (card.parentGrid) {
                    card.parentGrid.children = card.parentGrid.children.filter(child => child !== card);
                }
                this.children.push(card);
                card.parentGrid = this;
            }
        },
        replaceChildren(...cards) {
            this.children.forEach(card => { card.parentGrid = null; });
            this.children = [];
            this.append(...cards);
        }
    };
    grid.append(...initialCards);

    const viewport = Object.assign(element(), {
        clientWidth: 200,
        scrollLeft: 0,
        scrollTo(options) {
            scrolls.push(options);
            this.scrollLeft = Math.min(Math.max(0, options.left), Math.max(0, this.scrollWidth - this.clientWidth));
            this.dispatchEvent(new Event('scroll'));
        }
    });
    Object.defineProperty(viewport, 'scrollWidth', {
        get: () => Math.max(grid.children.length * cardWidth, viewport.clientWidth)
    });
    const row = {
        querySelector(selector) {
            return {
                '[data-menu-viewport]': viewport,
                '[data-menu-previous]': previous,
                '[data-menu-next]': next,
                '[data-menu-list]': grid
            }[selector];
        }
    };
    return { row, viewport, previous, next, scrolls, visibleCards: () => [...grid.children] };
}

function createMenu({ categories = ['coffee', 'tea', 'tea', 'coffee', 'coffee', ...Array(10).fill('cake')], reducedMotion = true } = {}) {
    const cards = categories.map((category, menuIndex) => element({ category, menuIndex: String(menuIndex) }));
    for (const card of cards) {
        Object.defineProperty(card, 'offsetLeft', {
            get: () => card.parentGrid.children.indexOf(card) * cardWidth
        });
    }

    const [top, bottom] = [0, 1].map(row => createRow(cards.filter((_, index) => index % 2 === row)));
    const markupOrder = [...top.visibleCards(), ...bottom.visibleCards()];
    const filters = Object.fromEntries(['all', 'coffee', 'tea', 'cake', 'empty'].map(category => [category, element({ filter: category })]));
    const feature = element();
    let resize;
    const menu = {
        querySelectorAll(selector) {
            return {
                '[data-menu-row]': [top.row, bottom.row],
                '[data-filter]': Object.values(filters),
                '[data-menu-index]': markupOrder
            }[selector];
        }
    };
    const root = { querySelector: selector => selector === '[data-menu-carousel]' ? menu : feature };
    initializeMenuCarousel(root, {
        ResizeObserver: class {
            constructor(callback) { resize = callback; }
            observe() {}
        },
        reducedMotion: { matches: reducedMotion }
    });

    return { top, bottom, filters, feature, resize };
}

test('each row moves in both directions without scrolling the other row', () => {
    const { top, bottom } = createMenu({ reducedMotion: false });

    top.previous.click();
    top.next.click();
    assert.deepEqual(top.scrolls.at(-1), { left: 100, behavior: 'smooth' });
    assert.equal(bottom.viewport.scrollLeft, 0);
    bottom.next.click();
    bottom.next.click();
    assert.equal(top.viewport.scrollLeft, 100);
    top.previous.click();
    assert.equal(bottom.viewport.scrollLeft, 200);
    bottom.previous.click();

    assert.equal(top.viewport.scrollLeft, 0);
    assert.equal(bottom.viewport.scrollLeft, 100);
    assert.equal(top.previous.getAttribute('aria-disabled'), 'true');
    assert.equal(bottom.previous.getAttribute('aria-disabled'), 'false');
});

test('stops at the end of one row while the other row can advance', () => {
    const { top, bottom } = createMenu();

    for (let step = 0; step < 7; step++) top.next.click();

    assert.equal(top.viewport.scrollLeft, 600);
    assert.equal(top.next.getAttribute('aria-disabled'), 'true');
    assert.equal(bottom.next.getAttribute('aria-disabled'), 'false');
    assert.equal(top.scrolls.length, 6);
});

test('moves left from a partially scrolled position and honors reduced motion', () => {
    const { bottom: { viewport, previous, scrolls } } = createMenu();
    viewport.scrollTo({ left: 25 });

    previous.click();

    assert.deepEqual(scrolls.at(-1), { left: 0, behavior: 'instant' });
});

test('supports the arrow keys on the focused row without consuming other keys', () => {
    const { top, bottom: { viewport, scrolls } } = createMenu();
    const pressKey = key => {
        const event = Object.assign(new Event('keydown', { cancelable: true }), { key });
        viewport.dispatchEvent(event);
        return event.defaultPrevented;
    };

    assert.equal(pressKey('ArrowRight'), true);
    assert.equal(pressKey('ArrowLeft'), true);
    assert.equal(pressKey('Tab'), false);
    assert.deepEqual(scrolls.map(scroll => scroll.left), [100, 0]);
    assert.equal(top.viewport.scrollLeft, 0);
});

test('leaves pages with no menu untouched', () => {
    const root = { querySelector: () => null };
    assert.doesNotThrow(() => initializeMenuCarousel(root, {}));
});

test('resets and redistributes both rows when a category changes', () => {
    const { filters, top, bottom } = createMenu();
    top.next.click();
    bottom.next.click();

    filters.tea.click();

    assert.deepEqual(top.visibleCards().map(card => card.dataset.menuIndex), ['1']);
    assert.deepEqual(bottom.visibleCards().map(card => card.dataset.menuIndex), ['2']);
    assert.equal(filters.tea.getAttribute('aria-pressed'), 'true');
    assert.equal(filters.all.getAttribute('aria-pressed'), 'false');
    for (const row of [top, bottom]) {
        assert.equal(row.viewport.scrollLeft, 0);
        assert.equal(row.previous.getAttribute('aria-disabled'), 'true');
        assert.equal(row.next.getAttribute('aria-disabled'), 'true');
    }
});

test('makes every menu item reachable with the arrows and restores the full list after filtering', () => {
    const categories = ['coffee', ...Array(23).fill('cake')];
    const { top, bottom, filters } = createMenu({ categories });
    assert.deepEqual([top.visibleCards().length, bottom.visibleCards().length], [12, 12]);

    for (const row of [top, bottom]) {
        for (let step = 0; step < 10; step++) row.next.click();
        assert.equal(row.viewport.scrollLeft, 1000);
        assert.equal(row.next.getAttribute('aria-disabled'), 'true');
    }

    assert.equal(top.visibleCards().at(-1).dataset.menuIndex, '22');
    assert.equal(bottom.visibleCards().at(-1).dataset.menuIndex, '23');

    filters.coffee.click();
    assert.deepEqual([top.visibleCards().length, bottom.visibleCards().length], [1, 0]);
    filters.all.click();
    assert.deepEqual([top.visibleCards().length, bottom.visibleCards().length], [12, 12]);
    assert.deepEqual([top.viewport.scrollLeft, bottom.viewport.scrollLeft], [0, 0]);
});

test('supports the coffee shortcut, an empty filter and a viewport resize', () => {
    const { top, bottom, filters, feature, resize } = createMenu();
    top.viewport.clientWidth = 100;

    feature.click();
    assert.deepEqual([top.visibleCards().length, bottom.visibleCards().length], [2, 1]);
    assert.equal(filters.coffee.getAttribute('aria-pressed'), 'true');
    assert.equal(top.next.getAttribute('aria-disabled'), 'false');

    top.viewport.clientWidth = 200;
    resize();
    assert.equal(top.next.getAttribute('aria-disabled'), 'true');

    filters.empty.click();
    for (const row of [top, bottom]) {
        assert.equal(row.row.hidden, true);
        assert.equal(row.visibleCards().length, 0);
        assert.equal(row.previous.getAttribute('aria-disabled'), 'true');
        assert.equal(row.next.getAttribute('aria-disabled'), 'true');
    }
});
