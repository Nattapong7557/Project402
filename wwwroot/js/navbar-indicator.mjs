export function indicatorGeometry(group, link, label) {
    return {
        left: label.left - group.left,
        top: link.bottom - group.top - 5,
        width: label.width
    };
}

function storedLink(group, current) {
    try {
        const key = `cafe-navbar:${group.dataset.navIndicator}`;
        const previous = sessionStorage.getItem(key);
        if (current) sessionStorage.setItem(key, current.dataset.navKey);
        else sessionStorage.removeItem(key);
        return previous;
    } catch {
        // The current link is still highlighted if the browser blocks storage.
        return null;
    }
}

function bindIndicator(group) {
    const links = [...group.querySelectorAll('[data-nav-key]')];
    const current = links.find(link => link.hasAttribute('aria-current'));
    if (!current) {
        storedLink(group, null);
        return;
    }

    let previous;
    let movePending;
    let lastPosition;

    function position(link) {
        const label = link.querySelector('[data-nav-label]');
        return indicatorGeometry(
            group.getBoundingClientRect(), link.getBoundingClientRect(), label.getBoundingClientRect());
    }

    function place({ left, top, width }) {
        group.style.setProperty('--nav-indicator-x', `${left}px`);
        group.style.setProperty('--nav-indicator-y', `${top}px`);
        group.style.setProperty('--nav-indicator-width', `${width}px`);
    }

    function update() {
        if (!current.offsetWidth) {
            lastPosition = null;
            return;
        }

        const target = position(current);
        if (!movePending && lastPosition?.left === target.left && lastPosition.top === target.top &&
            lastPosition.width === target.width) return;

        group.classList.remove('nav-indicator-moving');
        if (movePending && previous?.offsetWidth) {
            place(position(previous));
            group.classList.add('nav-indicator-ready');
            // Commit the old position before enabling the transition.
            void getComputedStyle(group, '::after').transform;
            group.classList.add('nav-indicator-moving');
        }
        movePending = false;
        lastPosition = target;
        place(target);
        group.classList.add('nav-indicator-ready');
    }

    function restore() {
        const previousKey = storedLink(group, current);
        previous = links.find(link => link.dataset.navKey === previousKey && link !== current);
        movePending = Boolean(previous) && !matchMedia('(prefers-reduced-motion: reduce)').matches;
        lastPosition = null;
        update();
    }

    restore();
    if ('ResizeObserver' in window) {
        // Measure again when a collapsed menu opens, or when a label changes width.
        const observer = new ResizeObserver(update);
        observer.observe(group);
        links.forEach(link => {
            observer.observe(link);
            observer.observe(link.querySelector('[data-nav-label]'));
        });
    } else {
        window.addEventListener('resize', update);
        group.closest('.collapse')?.addEventListener('shown.bs.collapse', update);
    }
    window.addEventListener('pageshow', event => {
        if (event.persisted) restore();
    });
}

if (typeof document !== 'undefined') {
    document.querySelectorAll('[data-nav-indicator]').forEach(bindIndicator);
}
