function readIds(storageKey) {
    try {
        const storedValue = window.localStorage.getItem(storageKey);
        if (!storedValue) {
            return [];
        }

        const parsedValue = JSON.parse(storedValue);
        if (!Array.isArray(parsedValue)) {
            return [];
        }

        return parsedValue.filter(value => typeof value === "string");
    } catch {
        return [];
    }
}

export function readAcknowledgedIds(storageKey) {
    return readIds(storageKey);
}

export function acknowledge(storageKey, announcementIds) {
    try {
        const acknowledgedIds = new Set(readIds(storageKey));
        for (const announcementId of announcementIds) {
            acknowledgedIds.add(announcementId);
        }

        window.localStorage.setItem(storageKey, JSON.stringify([...acknowledgedIds]));
    } catch {
    }
}
