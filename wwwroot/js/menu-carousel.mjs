const scrollTolerance = 1;

export function initializeMenuCarousel(root, { ResizeObserver, reducedMotion }) {
    const menu = root.querySelector('[data-menu-carousel]');
    if (!menu) return;

    const rows = [...menu.querySelectorAll('[data-menu-row]')].map(element => ({
        element,
        viewport: element.querySelector('[data-menu-viewport]'),
        grid: element.querySelector('[data-menu-list]'),
        previous: element.querySelector('[data-menu-previous]'),
        next: element.querySelector('[data-menu-next]')
    }));
    const filters = [...menu.querySelectorAll('[data-filter]')];
    const cards = [...menu.querySelectorAll('[data-menu-index]')].sort((a, b) => a.dataset.menuIndex - b.dataset.menuIndex);

    function updateArrows({ viewport, previous, next }) {
        const end = viewport.scrollWidth - viewport.clientWidth;
        previous.setAttribute('aria-disabled', String(viewport.scrollLeft <= scrollTolerance));
        next.setAttribute('aria-disabled', String(viewport.scrollLeft >= end - scrollTolerance));
    }

    function updateRows(category) {
        const matchingCards = category === 'all' ? cards : cards.filter(card => card.dataset.category === category);
        rows.forEach((row, rowIndex) => {
            const rowCards = matchingCards.filter((_, index) => index % rows.length === rowIndex);
            row.element.hidden = rowCards.length === 0;
            row.grid.replaceChildren(...rowCards);
            row.viewport.scrollTo({ left: 0, behavior: 'instant' });
            updateArrows(row);
        });
    }

    function scrollToCard(row, card) {
        row.viewport.scrollTo({
            left: card.offsetLeft - row.grid.firstElementChild.offsetLeft,
            behavior: reducedMotion.matches ? 'instant' : 'smooth'
        });
        updateArrows(row);
    }

    function move(row, direction, button) {
        if (button.getAttribute('aria-disabled') === 'true') return;

        const rowCards = [...row.grid.children];
        const currentOffset = rowCards[0].offsetLeft + row.viewport.scrollLeft;
        const target = direction < 0
            ? rowCards.findLast(card => card.offsetLeft < currentOffset - scrollTolerance)
            : rowCards.find(card => card.offsetLeft > currentOffset + scrollTolerance);
        if (target) scrollToCard(row, target);
    }

    filters.forEach(button => {
        button.addEventListener('click', () => {
            filters.forEach(filter => filter.setAttribute('aria-pressed', String(filter === button)));
            updateRows(button.dataset.filter);
        });
    });

    const resizeObserver = new ResizeObserver(() => rows.forEach(updateArrows));
    rows.forEach(row => {
        const { previous, next, viewport } = row;
        previous.addEventListener('click', () => move(row, -1, previous));
        next.addEventListener('click', () => move(row, 1, next));
        viewport.addEventListener('scroll', () => updateArrows(row), { passive: true });
        viewport.addEventListener('keydown', event => {
            if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
                event.preventDefault();
                if (event.key === 'ArrowLeft') move(row, -1, previous);
                else move(row, 1, next);
            }
        });
        previous.hidden = false;
        next.hidden = false;
        updateArrows(row);
        resizeObserver.observe(viewport);
    });

    root.querySelector('#showCoffeeMenu')?.addEventListener('click', () => {
        filters.find(button => button.dataset.filter === 'coffee')?.click();
    });
}

if (typeof document !== 'undefined') {
    initializeMenuCarousel(document, {
        ResizeObserver: window.ResizeObserver,
        reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)')
    });
}
