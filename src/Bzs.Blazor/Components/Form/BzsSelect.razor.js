const instances = new Map();
let invalidFocusScheduled = false;

// The hidden constraint element carries native required validation for the enhanced combobox.
// When the form reports it invalid, focus has to land on the visible trigger instead.
export function initialize(instanceId, root) {
    dispose(instanceId);

    const invalidHandler = event => {
        if (!event.target.matches('[data-bzs-select-constraint="true"]')) return;
        event.preventDefault();
        if (invalidFocusScheduled) return;
        invalidFocusScheduled = true;
        requestAnimationFrame(() => {
            invalidFocusScheduled = false;
            root.querySelector('[role="combobox"]')?.focus({ preventScroll: true });
        });
    };

    root.addEventListener('invalid', invalidHandler, true);
    instances.set(instanceId, { root, invalidHandler });
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;
    instances.delete(instanceId);
    instance.root?.removeEventListener?.('invalid', instance.invalidHandler, true);
}
