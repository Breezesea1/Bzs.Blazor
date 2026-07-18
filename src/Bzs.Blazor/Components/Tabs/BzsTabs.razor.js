const listeners = new WeakMap();

export function attach(tabList, orientation, activation) {
    detach(tabList);

    const listener = event => {
        const navigationKeys = orientation === 'vertical'
            ? ['ArrowUp', 'ArrowDown', 'Home', 'End']
            : ['ArrowLeft', 'ArrowRight', 'Home', 'End'];
        const activationKeys = activation === 'manual' ? ['Enter', ' '] : [];
        if (navigationKeys.includes(event.key) || activationKeys.includes(event.key)) {
            event.preventDefault();
        }
    };

    tabList.addEventListener('keydown', listener);
    listeners.set(tabList, listener);
}

export function getDirection(element) {
    return getComputedStyle(element).direction;
}

export function detach(tabList) {
    const listener = listeners.get(tabList);
    if (!listener) {
        return;
    }

    tabList.removeEventListener('keydown', listener);
    listeners.delete(tabList);
}
