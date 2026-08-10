const instances = new Map();

export function initialize(instanceId, root) {
    dispose(instanceId);
    const keydownHandler = event => {
        if (event.target.getAttribute('role') !== 'combobox') return;

        const expanded = event.target.getAttribute('aria-expanded') === 'true';
        if (event.key === 'ArrowDown'
            || event.key === 'ArrowUp'
            || (expanded && ['Home', 'End', 'Enter'].includes(event.key))) {
            event.preventDefault();
        }
    };

    instances.set(instanceId, { root, keydownHandler });
    root.addEventListener('keydown', keydownHandler);
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;

    instance.root.removeEventListener('keydown', instance.keydownHandler);
    instances.delete(instanceId);
}
