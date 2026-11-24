const refreshInterval = 15_000;

export function startBookingStatusUpdates(page, { document, fetch, setInterval, clearInterval }) {
    const cards = new Map(Array.from(page.querySelectorAll('[data-booking-id]'),
        card => [Number(card.dataset.bookingId), card]));
    if (cards.size === 0) return;

    const message = page.querySelector('[data-booking-refresh-message]');
    let refreshing = false;

    function showMessage(text) {
        message.hidden = !text;
        if (message.textContent !== text) message.textContent = text;
    }

    async function refresh() {
        if (document.hidden || refreshing) return;
        refreshing = true;
        try {
            const response = await fetch(page.dataset.bookingStatusUrl, {
                cache: 'no-store',
                signal: AbortSignal.timeout(10_000)
            });
            if (response.status === 401 || response.status === 403 || response.redirected) {
                clearInterval(timer);
                document.removeEventListener('visibilitychange', refresh);
                showMessage('เข้าสู่ระบบอีกครั้งเพื่ออัปเดตสถานะการจอง');
                return;
            }
            if (!response.ok) throw new Error('Could not refresh booking statuses');

            const statuses = await response.json();
            for (const { id, status } of statuses) {
                const card = cards.get(id);
                if (card) updateCard(card, status);
            }
            showMessage('');
        } catch {
            showMessage('อัปเดตสถานะไม่ได้ชั่วคราว ระบบจะลองอีกครั้ง');
        } finally {
            refreshing = false;
        }
    }

    const timer = setInterval(refresh, refreshInterval);
    document.addEventListener('visibilitychange', refresh);
}

function updateCard(card, status) {
    const badge = card.querySelector('[data-booking-status]');
    if (badge.textContent !== status.label) badge.textContent = status.label;
    badge.className = status.badgeClass;

    const progress = card.querySelector('[data-booking-progress]');
    progress.hidden = status.progress === 0;
    progress.querySelector('progress').value = status.progress;
    for (const label of progress.querySelectorAll('[data-progress-step]')) {
        const step = Number(label.dataset.progressStep);
        const reached = status.progress >= step;
        label.classList.toggle('fw-bold', reached);
        label.classList.toggle(step === 100 ? 'text-success' : 'text-dark', reached);
    }

    card.querySelector('[data-booking-cancel]').hidden = !status.canCancel;
}

if (typeof document !== 'undefined') {
    const page = document.querySelector('[data-booking-status-url]');
    if (page) startBookingStatusUpdates(page, {
        document,
        fetch: window.fetch.bind(window),
        setInterval: window.setInterval.bind(window),
        clearInterval: window.clearInterval.bind(window)
    });
}
