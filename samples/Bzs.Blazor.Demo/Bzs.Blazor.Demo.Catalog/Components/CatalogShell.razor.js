const storageKey = 'bzs-demo-sidebar-collapsed';
const controllers = new Map();

export function initialize(shellId, callback) {
    const shell = document.getElementById(shellId);
    if (!shell) {
        throw new Error(`Catalog shell '${shellId}' was not found.`);
    }

    dispose(shellId);

    const mobileQuery = window.matchMedia('(width < 48rem)');
    const readDesktopOpen = () => {
        try {
            return window.localStorage.getItem(storageKey) !== '1';
        } catch {
            return true;
        }
    };
    const writeDesktopOpen = open => {
        try {
            window.localStorage.setItem(storageKey, open ? '0' : '1');
        } catch {
            // The controlled component still retains the choice for this visit.
        }
    };
    const focusToggle = mode => requestAnimationFrame(() => {
        shell.querySelector(`[data-demo-navigation-toggle-mode="${mode}"]`)?.focus({ preventScroll: true });
    });
    const requestOpen = async (open, focusMode = null) => {
        if (!mobileQuery.matches) {
            writeDesktopOpen(open);
        }

        await callback.invokeMethodAsync('HandleNavigationRequested', open);
        if (focusMode !== null) {
            focusToggle(focusMode);
        }
    };
    const handleClick = event => {
        const toggle = event.target instanceof Element
            ? event.target.closest('[data-demo-navigation-toggle]')
            : null;
        if (toggle && shell.contains(toggle)) {
            const open = toggle.dataset.demoNavigationToggleMode === 'open';
            void requestOpen(open, mobileQuery.matches ? null : open ? 'close' : 'open');
            return;
        }

        const navigationLink = event.target instanceof Element
            ? event.target.closest('.demo-nav-link')
            : null;
        if (navigationLink && shell.contains(navigationLink) && mobileQuery.matches) {
            void requestOpen(false);
        }
    };
    const handleViewportChange = async event => {
        const drawer = shell.querySelector('#demo-navigation-drawer');
        const activeElement = document.activeElement;
        const activeElementWasInDrawer = drawer?.contains(activeElement) ?? false;
        const activeElementWasOpenToggle = activeElement?.matches(
            '[data-demo-navigation-toggle-mode="open"]') ?? false;
        const open = event.matches ? false : readDesktopOpen();

        await callback.invokeMethodAsync('HandleNavigationRequested', open);
        if (!open && activeElementWasInDrawer) {
            focusToggle('open');
        } else if (open && activeElementWasOpenToggle) {
            focusToggle('close');
        }
    };

    shell.addEventListener('click', handleClick);
    mobileQuery.addEventListener('change', handleViewportChange);
    controllers.set(shellId, { shell, mobileQuery, handleClick, handleViewportChange });

    return { open: mobileQuery.matches ? false : readDesktopOpen() };
}

export function dispose(shellId) {
    const controller = controllers.get(shellId);
    if (!controller) {
        return;
    }

    controller.shell.removeEventListener('click', controller.handleClick);
    controller.mobileQuery.removeEventListener('change', controller.handleViewportChange);
    controllers.delete(shellId);
}
