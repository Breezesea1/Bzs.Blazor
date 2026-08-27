const instances = new Map();

// Positioning, outside interaction, and Escape belong to the anchored overlay module. This module
// carries only what the calendar itself needs from the browser.
export function initialize(instanceId, root) {
    if (!isUsableRoot(root)) return null;

    instances.set(instanceId, { root });
    return formatLocalDate(new Date());
}

function isUsableRoot(root) {
    return root !== null
        && typeof root === 'object'
        && typeof root.querySelector === 'function';
}

function formatLocalDate(date) {
    const year = date.getFullYear().toString().padStart(4, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
}

export function focusActiveDay(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;
    requestAnimationFrame(() => {
        instance.root
            .querySelector('[data-bzs-date-picker-day="true"][tabindex="0"]')
            ?.focus({ preventScroll: true });
    });
}

export function scrollActivePeriodOption(menu) {
    requestAnimationFrame(() => {
        menu
            ?.querySelector('.bzs-date-picker__period-option--active')
            ?.scrollIntoView({ block: 'nearest' });
    });
}

export function dispose(instanceId) {
    instances.delete(instanceId);
}
