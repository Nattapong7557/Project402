const currency = new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB', maximumFractionDigits: 2 });

export function cartTotals(lines) {
    return lines.reduce((totals, { quantity, price }) => ({
        count: totals.count + quantity,
        subtotal: totals.subtotal + quantity * price
    }), { count: 0, subtotal: 0 });
}

export function boundedQuantity(value) {
    return Number.isFinite(value) ? Math.max(0, Math.min(20, Math.trunc(value))) : 0;
}

export function changeQuantity(value, change) {
    return change === 'remove' ? 0 : boundedQuantity(boundedQuantity(value) + Number(change));
}

function bindCart(cart, trigger) {
    const dialog = cart.querySelector('.food-cart-dialog');
    const preview = dialog.querySelector('[data-cart-preview]');
    const empty = dialog.querySelector('[data-cart-empty]');
    const submit = dialog.querySelector('[type="submit"]');
    const subtotal = dialog.querySelector('[data-food-total]');
    const count = dialog.querySelector('[data-food-count]');
    const triggerCount = trigger.querySelector('[data-cart-count]');
    const lines = new Map([...cart.querySelectorAll('[data-food-item]')].map(input => [input.dataset.foodItem, {
        input,
        price: Number(input.dataset.foodPrice),
        row: dialog.querySelector(`[data-cart-item="${input.dataset.foodItem}"]`)
    }]));

    function refresh() {
        const selected = [];
        for (const { input, price, row } of lines.values()) {
            const quantity = boundedQuantity(input.valueAsNumber);
            row.hidden = quantity === 0;
            if (!row.hidden) {
                row.querySelector('[data-cart-line-quantity]').textContent = quantity;
                row.querySelector('[data-cart-line-total]').textContent = currency.format(quantity * price);
                selected.push({ quantity, price });
            }
        }
        const totals = cartTotals(selected);
        subtotal.textContent = currency.format(totals.subtotal);
        count.textContent = `${totals.count} ชิ้น`;
        triggerCount.textContent = totals.count;
        empty.hidden = totals.count > 0;
        submit.disabled = totals.count === 0;
    }

    cart.addEventListener('click', event => {
        const button = event.target.closest('[data-quantity-step], [data-cart-change]');
        if (!button) return;

        const row = button.closest('[data-cart-item]');
        const input = row
            ? lines.get(row.dataset.cartItem).input
            : button.closest('[data-quantity-controls]').querySelector('input');
        input.value = changeQuantity(input.valueAsNumber, button.dataset.cartChange ?? button.dataset.quantityStep);
        refresh();
        if (row?.hidden) {
            (dialog.querySelector('[data-cart-item]:not([hidden]) [data-cart-change="remove"]') ??
                dialog.querySelector('.food-cart-add')).focus();
        }
    });
    for (const { input } of lines.values()) input.addEventListener('input', refresh);

    preview.hidden = false;
    if (typeof dialog.showModal === 'function') {
        dialog.removeAttribute('open');
        cart.classList.add('food-cart-enhanced');
        trigger.setAttribute('aria-haspopup', 'dialog');
        trigger.setAttribute('aria-controls', dialog.id);
        trigger.addEventListener('click', event => {
            event.preventDefault();
            dialog.showModal();
        });
        dialog.querySelectorAll('[data-cart-close]').forEach(button => {
            button.hidden = false;
            button.addEventListener('click', () => dialog.close());
        });
        dialog.addEventListener('close', () => trigger.focus({ preventScroll: true }));
    }

    refresh();
}

if (typeof document !== 'undefined') {
    const filters = [...document.querySelectorAll('[data-food-filter]')];
    const tiles = [...document.querySelectorAll('[data-food-category]')];
    filters.forEach(button => {
        button.addEventListener('click', () => {
            const category = button.dataset.foodFilter;
            filters.forEach(filter => filter.setAttribute('aria-pressed', String(filter === button)));
            tiles.forEach(tile => { tile.hidden = category !== 'all' && tile.dataset.foodCategory !== category; });
        });
    });

    const cart = document.querySelector('[data-food-cart]');
    const trigger = document.querySelector('[data-cart-open]');
    if (cart && trigger) bindCart(cart, trigger);
}
