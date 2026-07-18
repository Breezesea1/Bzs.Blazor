const observers = new WeakMap();
const onSystemPreferenceChanged = 'OnSystemPreferenceChanged';

export function setSystemMode(element, dotNetReference, enabled) {
    dispose(element);
    if (!enabled) {
        return false;
    }

    const query = window.matchMedia('(prefers-color-scheme: dark)');
    const notify = async () => {
        try {
            await dotNetReference.invokeMethodAsync(onSystemPreferenceChanged, query.matches);
        } catch {
            // A Server circuit can be temporarily unavailable and later reconnect.
            // Keep the observer so the next preference change retries the callback.
        }
    };

    query.addEventListener('change', notify);
    observers.set(element, { query, notify });
    return query.matches;
}

export function dispose(element) {
    const observer = observers.get(element);
    if (!observer) {
        return;
    }

    observer.query.removeEventListener('change', observer.notify);
    observers.delete(element);
}
