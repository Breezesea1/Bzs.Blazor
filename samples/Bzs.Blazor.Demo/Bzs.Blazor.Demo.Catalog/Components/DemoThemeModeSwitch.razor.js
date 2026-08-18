const globalProviderSelector = '[data-testid="demo-global-theme-provider"]';
const modeSelector = '[data-demo-theme-mode]';
const storageKey = 'bzs-demo-theme-mode';
const systemMode = 'system';
const validModes = new Set(['light', 'dark', systemMode]);

let activeInstance;
let currentMode;
let instanceSequence = 0;

function readStoredMode() {
    try {
        const storedMode = window.localStorage.getItem(storageKey);
        return validModes.has(storedMode) ? storedMode : undefined;
    } catch {
        return undefined;
    }
}

function resolveTheme(mode) {
    return mode === systemMode ? (activeInstance?.mediaQuery.matches ? 'dark' : 'light') : mode;
}

function updateSelection(mode) {
    for (const option of document.querySelectorAll(modeSelector)) {
        option.setAttribute('aria-pressed', String(option.dataset.demoThemeMode === mode));
    }
}

function applyMode(mode) {
    const provider = document.querySelector(globalProviderSelector);
    if (!provider) {
        return;
    }

    provider.setAttribute('data-bzs-demo-theme-mode', mode);
    provider.setAttribute('data-bzs-theme', resolveTheme(mode));
    updateSelection(mode);
}

function persistMode(mode) {
    try {
        window.localStorage.setItem(storageKey, mode);
    } catch {
    }
}

export function setMode(mode) {
    if (!activeInstance || !validModes.has(mode)) {
        return;
    }

    currentMode = mode;
    persistMode(mode);
    applyMode(mode);
}

export function initialize() {
    dispose();

    const instance = {
        token: String(++instanceSequence),
        mediaQuery: window.matchMedia('(prefers-color-scheme: dark)'),
    };
    currentMode ??= readStoredMode() ?? 'light';
    activeInstance = instance;

    instance.mediaQueryListener = () => {
        if (activeInstance === instance && currentMode === systemMode) {
            applyMode(systemMode);
        }
    };
    instance.mediaQuery.addEventListener('change', instance.mediaQueryListener);

    instance.providerObserver = new MutationObserver(() => {
        if (activeInstance === instance) {
            applyMode(currentMode);
        }
    });
    instance.providerObserver.observe(document.body, { childList: true, subtree: true });

    instance.clickListener = event => {
        const option = event.target.closest(modeSelector);
        const mode = option?.dataset.demoThemeMode;
        if (!validModes.has(mode)) {
            return;
        }

        setMode(mode);
    };
    document.addEventListener('click', instance.clickListener, true);

    instance.enhancedNavigationListener = () => {
        if (activeInstance === instance) {
            applyMode(currentMode);
        }
    };
    document.addEventListener('enhancedload', instance.enhancedNavigationListener);

    instance.storageListener = event => {
        if (activeInstance !== instance || event.key !== storageKey || !validModes.has(event.newValue)) {
            return;
        }

        currentMode = event.newValue;
        applyMode(currentMode);
    };
    window.addEventListener('storage', instance.storageListener);

    // The static shell cannot cascade a parameter across this interactive leaf, so it adapts the global provider root DOM.
    applyMode(currentMode);
    return instance.token;
}

export function dispose(token) {
    const instance = activeInstance;
    if (!instance || (token && token !== instance.token)) {
        return;
    }

    instance.mediaQuery.removeEventListener('change', instance.mediaQueryListener);
    instance.providerObserver.disconnect();
    document.removeEventListener('click', instance.clickListener, true);
    document.removeEventListener('enhancedload', instance.enhancedNavigationListener);
    window.removeEventListener('storage', instance.storageListener);
    activeInstance = undefined;
}
