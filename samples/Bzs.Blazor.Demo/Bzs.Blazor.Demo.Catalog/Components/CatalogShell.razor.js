const storageKey = 'bzs-demo-sidebar-collapsed';
const breakpoint = '(width < 48rem)';
const focusableSelector = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    "[tabindex]:not([tabindex='-1'])",
].join(',');
const registryKey = Symbol.for('bzs.demo.catalog-navigation');
const registry = globalThis[registryKey] ??= {
    controllers: new Map(),
    nextConnectionId: 0,
};

class CatalogNavigationController {
    constructor(shellId) {
        this.shellId = shellId;
        this.mobileQuery = window.matchMedia(breakpoint);
        this.desktopCollapsed = null;
        this.mobileOpen = false;
        this.callback = null;
        this.observedDrawers = new WeakSet();
        this.handleClick = this.handleClick.bind(this);
        this.handleKeyDown = this.handleKeyDown.bind(this);
        this.handleViewportChange = this.handleViewportChange.bind(this);
        this.wire = this.wire.bind(this);

        document.addEventListener('click', this.handleClick);
        document.addEventListener('keydown', this.handleKeyDown);
        document.addEventListener('enhancedload', this.wire);
        this.mobileQuery.addEventListener('change', this.handleViewportChange);
        this.hostObserver = new MutationObserver(this.wire);
        this.hostObserver.observe(document.body, { childList: true, subtree: true });
        this.wire();
    }

    connect(callback) {
        const connectionId = `${this.shellId}-${++registry.nextConnectionId}`;
        this.callback = callback;
        this.connectionId = connectionId;
        return { open: this.readOpen(), connectionId };
    }

    disconnect(connectionId) {
        if (this.connectionId !== connectionId) {
            return;
        }

        this.callback = null;
        this.connectionId = null;
        this.applyStaticState();
    }

    get shell() {
        return document.getElementById(this.shellId);
    }

    readDesktopCollapsed() {
        if (this.desktopCollapsed !== null) {
            return this.desktopCollapsed;
        }

        try {
            this.desktopCollapsed = window.localStorage.getItem(storageKey) === '1';
        } catch {
            this.desktopCollapsed = false;
        }

        return this.desktopCollapsed;
    }

    writeDesktopCollapsed(collapsed) {
        this.desktopCollapsed = collapsed;
        try {
            window.localStorage.setItem(storageKey, collapsed ? '1' : '0');
        } catch {
            // Retain the choice in memory when storage is unavailable.
        }
    }

    readOpen() {
        return this.mobileQuery.matches ? this.mobileOpen : !this.readDesktopCollapsed();
    }

    focusToggle(mode) {
        requestAnimationFrame(() => {
            this.shell?.querySelector(`[data-demo-navigation-toggle-mode="${mode}"]`)
                ?.focus({ preventScroll: true });
        });
    }

    async publishOpen(open) {
        if (this.callback !== null) {
            await this.callback.invokeMethodAsync('HandleNavigationRequested', open);
        } else {
            this.applyStaticState();
        }
    }

    async requestOpen(open, focusMode = null) {
        if (this.mobileQuery.matches) {
            this.mobileOpen = open;
        } else {
            this.writeDesktopCollapsed(!open);
        }

        await this.publishOpen(open);
        if (focusMode !== null) {
            this.focusToggle(focusMode);
        }
    }

    async closeMobileNavigation() {
        if (!this.mobileQuery.matches || !this.mobileOpen) {
            return;
        }

        this.mobileOpen = false;
        await this.publishOpen(false);
        this.focusToggle('open');
    }

    applyStaticState(focusMode = null) {
        if (this.callback !== null) {
            return;
        }

        const shell = this.shell;
        if (!shell) {
            return;
        }

        const open = this.readOpen();
        const modalOpen = this.mobileQuery.matches && open;
        shell.querySelectorAll('#demo-navigation-drawer').forEach((drawer) => {
            drawer.dataset.bzsOpen = open ? 'true' : 'false';
            if (open) {
                drawer.removeAttribute('aria-hidden');
                drawer.removeAttribute('inert');
            } else {
                drawer.setAttribute('aria-hidden', 'true');
                drawer.setAttribute('inert', '');
            }
        });
        shell.querySelectorAll('[data-demo-navigation-toggle]').forEach((button) => {
            button.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
        shell.querySelectorAll('#demo-app-bar, #main-content').forEach((element) => {
            if (modalOpen) {
                element.setAttribute('inert', '');
            } else {
                element.removeAttribute('inert');
            }
        });

        if (focusMode !== null) {
            this.focusToggle(focusMode);
        }
    }

    getFocusableDrawerElements() {
        const drawer = this.shell?.querySelector('#demo-navigation-drawer');
        if (!drawer) {
            return [];
        }

        return [...drawer.querySelectorAll(focusableSelector)].filter((element) => {
            const style = window.getComputedStyle(element);
            const bounds = element.getBoundingClientRect();
            return style.display !== 'none'
                && style.visibility !== 'hidden'
                && element.getAttribute('tabindex') !== '-1'
                && bounds.width > 0
                && bounds.height > 0;
        });
    }

    handleClick(event) {
        const shell = this.shell;
        if (!shell || !(event.target instanceof Element) || !shell.contains(event.target)) {
            return;
        }

        const toggle = event.target.closest('[data-demo-navigation-toggle]');
        if (toggle) {
            const open = toggle.dataset.demoNavigationToggleMode === 'open';
            const focusMode = this.mobileQuery.matches && this.callback !== null
                ? null
                : open ? 'close' : 'open';
            void this.requestOpen(open, focusMode);
            return;
        }

        const navigationLink = event.target.closest('.demo-nav-link');
        if (navigationLink && this.mobileQuery.matches) {
            void this.requestOpen(false);
            return;
        }

        const backdrop = event.target.closest('.bzs-navigation-drawer__backdrop');
        if (backdrop && this.callback === null) {
            if (this.mobileQuery.matches) {
                void this.closeMobileNavigation();
            } else {
                void this.requestOpen(false);
            }
        }
    }

    handleKeyDown(event) {
        if (this.callback !== null || !this.mobileQuery.matches || !this.mobileOpen) {
            return;
        }

        if (event.key === 'Escape') {
            event.preventDefault();
            void this.closeMobileNavigation();
            return;
        }

        if (event.key !== 'Tab') {
            return;
        }

        const focusableElements = this.getFocusableDrawerElements();
        if (focusableElements.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusableElements[0];
        const last = focusableElements[focusableElements.length - 1];
        const activeElement = document.activeElement;
        if (event.shiftKey && (activeElement === first || !focusableElements.includes(activeElement))) {
            event.preventDefault();
            last.focus({ preventScroll: true });
        } else if (!event.shiftKey && (activeElement === last || !focusableElements.includes(activeElement))) {
            event.preventDefault();
            first.focus({ preventScroll: true });
        }
    }

    async handleViewportChange() {
        const activeElement = document.activeElement;
        const drawer = this.shell?.querySelector('#demo-navigation-drawer');
        const activeElementWasInDrawer = drawer?.contains(activeElement) ?? false;
        const activeElementWasOpenToggle = activeElement?.matches(
            '[data-demo-navigation-toggle-mode="open"]') ?? false;
        this.mobileOpen = false;
        const open = this.readOpen();

        await this.publishOpen(open);
        if (!open && activeElementWasInDrawer) {
            this.focusToggle('open');
        } else if (open && activeElementWasOpenToggle) {
            this.focusToggle('close');
        }
    }

    observeDrawer(drawer) {
        if (this.observedDrawers.has(drawer)) {
            return;
        }

        this.observedDrawers.add(drawer);
        const observer = new MutationObserver(() => {
            if (this.callback !== null) {
                return;
            }

            const expectedOpen = this.readOpen();
            if (drawer.dataset.bzsOpen !== (expectedOpen ? 'true' : 'false')) {
                this.applyStaticState();
            }
        });
        observer.observe(drawer, {
            attributes: true,
            attributeFilter: ['data-bzs-open'],
        });
    }

    wire() {
        this.shell?.querySelectorAll('#demo-navigation-drawer')
            .forEach((drawer) => this.observeDrawer(drawer));
        this.applyStaticState();
    }
}

function getController(shellId) {
    let controller = registry.controllers.get(shellId);
    if (!controller) {
        controller = new CatalogNavigationController(shellId);
        registry.controllers.set(shellId, controller);
    }

    return controller;
}

export function startStaticController(shellId) {
    getController(shellId).wire();
}

export function initialize(shellId, callback) {
    const shell = document.getElementById(shellId);
    if (!shell) {
        throw new Error(`Catalog shell '${shellId}' was not found.`);
    }

    return getController(shellId).connect(callback);
}

export function dispose(shellId, connectionId) {
    registry.controllers.get(shellId)?.disconnect(connectionId);
}
