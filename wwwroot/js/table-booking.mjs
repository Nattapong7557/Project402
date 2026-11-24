export function checkedSlot(slots, selected, unavailable) {
    const blocked = new Set(unavailable);
    return slots.includes(selected) && !blocked.has(selected) ? selected : null;
}

function bindTableBooking(page) {
    const form = page.querySelector('[data-table-form]');
    const zone = form.elements.ItemName;
    const date = form.elements.BookingDate;
    const slots = [...form.querySelectorAll('[name="TimeSlot"]')];
    const slotValues = slots.map(slot => slot.value);
    const status = form.querySelector('[data-availability-status]');
    const submit = form.querySelector('[data-table-submit]');
    const cards = [...page.querySelectorAll('[data-seat-zone]')];
    const summary = page.querySelector('[data-seat-summary]');
    let pending;

    function updateZone() {
        for (const card of cards) {
            const current = card.dataset.seatZone === zone.value;
            card.setAttribute('aria-pressed', String(current));
            if (current) summary.textContent = card.dataset.zoneTitle;
        }
    }

    function renderSlots(unavailable, unknown = false) {
        const selected = checkedSlot(slotValues, slots.find(slot => slot.checked)?.value, unavailable);
        const blocked = new Set(unavailable);
        for (const slot of slots) {
            slot.disabled = blocked.has(slot.value);
            slot.checked = slot.value === selected;
            slot.closest('label').querySelector('[data-slot-status]').textContent =
                unknown ? 'รอตรวจสอบ' : slot.disabled ? 'ไม่ว่าง' : 'ว่าง';
        }
        submit.disabled = blocked.size === slots.length;
    }

    async function refresh() {
        pending?.abort();
        updateZone();
        if (!date.value || !date.validity.valid) {
            renderSlots(slotValues, true);
            status.textContent = 'เลือกวันที่เข้าใช้บริการเพื่อดูรอบว่าง';
            return;
        }
        const request = new AbortController();
        pending = request;
        submit.disabled = true;
        status.textContent = 'กำลังตรวจรอบว่าง…';
        const url = new URL(page.dataset.availabilityUrl, location.href);
        url.searchParams.set('zone', zone.selectedOptions[0].dataset.zoneKey);
        url.searchParams.set('date', date.value);
        try {
            const response = await fetch(url, { signal: request.signal, cache: 'no-store' });
            if (!response.ok) throw new Error('Availability request failed.');
            const { unavailable } = await response.json();
            if (request.signal.aborted) return;
            renderSlots(unavailable);
            status.textContent = unavailable.length === slots.length
                ? 'วันที่เลือกไม่มีรอบว่างในโซนนี้ ลองเปลี่ยนวันหรือโซน'
                : 'เลือกจากรอบที่ว่างด้านล่าง';
        } catch {
            if (request.signal.aborted) return;
            renderSlots([], true);
            status.textContent = 'ตรวจรอบว่างไม่ได้ในขณะนี้ ระบบจะตรวจอีกครั้งตอนยืนยัน';
        }
    }

    for (const card of cards) card.addEventListener('click', () => {
        if (zone.value === card.dataset.seatZone) return;
        zone.value = card.dataset.seatZone;
        void refresh();
    });
    zone.addEventListener('change', refresh);
    date.addEventListener('change', refresh);

    submit.disabled = slots.every(slot => slot.disabled);
}

if (typeof document !== 'undefined') {
    const page = document.querySelector('[data-table-booking]');
    if (page) bindTableBooking(page);
}
