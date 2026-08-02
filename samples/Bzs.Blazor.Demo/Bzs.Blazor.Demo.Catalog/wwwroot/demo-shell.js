(() => {
    const storageKey = "bzs-demo-sidebar-collapsed";
    const mobileQuery = window.matchMedia("(width < 48rem)");
    const focusableSelector = [
        "a[href]",
        "button:not([disabled])",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "[tabindex]:not([tabindex='-1'])",
    ].join(",");
    let desktopCollapsed = null;
    let mobileOpen = false;

    const readDesktopCollapsed = () => {
        if (desktopCollapsed !== null) {
            return desktopCollapsed;
        }

        try {
            desktopCollapsed = window.localStorage.getItem(storageKey) === "1";
        } catch {
            desktopCollapsed = false;
        }

        return desktopCollapsed;
    };

    const writeDesktopCollapsed = (collapsed) => {
        desktopCollapsed = collapsed;
        try {
            window.localStorage.setItem(storageKey, collapsed ? "1" : "0");
        } catch {
            // The in-memory value preserves this visit's choice when storage is unavailable.
        }
    };

    const readOpen = () => mobileQuery.matches ? mobileOpen : !readDesktopCollapsed();

    const applyState = (focusMode = null) => {
        const open = readOpen();
        const modalOpen = mobileQuery.matches && open;

        document.querySelectorAll("#demo-navigation-drawer").forEach((drawer) => {
            drawer.dataset.bzsOpen = open ? "true" : "false";
            if (open) {
                drawer.removeAttribute("aria-hidden");
                drawer.removeAttribute("inert");
            } else {
                drawer.setAttribute("aria-hidden", "true");
                drawer.setAttribute("inert", "");
            }
        });

        document.querySelectorAll("[data-demo-navigation-toggle]").forEach((button) => {
            button.setAttribute("aria-expanded", open ? "true" : "false");
        });

        document.querySelectorAll("#demo-app-bar, #main-content").forEach((element) => {
            if (modalOpen) {
                element.setAttribute("inert", "");
            } else {
                element.removeAttribute("inert");
            }
        });

        if (focusMode !== null) {
            requestAnimationFrame(() => {
                document.querySelector(`[data-demo-navigation-toggle-mode="${focusMode}"]`)?.focus({ preventScroll: true });
            });
        }
    };

    const requestOpen = (open) => {
        if (mobileQuery.matches) {
            mobileOpen = open;
        } else {
            writeDesktopCollapsed(!open);
        }

        applyState(open ? "close" : "open");
    };

    const closeMobileNavigation = () => {
        if (!mobileQuery.matches || !mobileOpen) {
            return;
        }

        mobileOpen = false;
        applyState("open");
    };

    const getFocusableDrawerElements = () => {
        const drawer = document.querySelector("#demo-navigation-drawer");
        if (!drawer) {
            return [];
        }

        return [...drawer.querySelectorAll(focusableSelector)].filter((element) => {
            const style = window.getComputedStyle(element);
            const bounds = element.getBoundingClientRect();
            return style.display !== "none"
                && style.visibility !== "hidden"
                && element.getAttribute("tabindex") !== "-1"
                && bounds.width > 0
                && bounds.height > 0;
        });
    };

    const handleKeyDown = (event) => {
        if (!mobileQuery.matches || !mobileOpen) {
            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            closeMobileNavigation();
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusableElements = getFocusableDrawerElements();
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
    };

    const observeDrawerState = (drawer) => {
        if (drawer.dataset.demoNavigationStateObserved === "1") {
            return;
        }

        drawer.dataset.demoNavigationStateObserved = "1";
        const observer = new MutationObserver(() => {
            const expectedOpen = readOpen();
            if (drawer.dataset.bzsOpen !== (expectedOpen ? "true" : "false")) {
                applyState();
            }
        });
        observer.observe(drawer, {
            attributes: true,
            attributeFilter: ["data-bzs-open"],
        });
    };

    const wire = () => {
        document.querySelectorAll("#demo-navigation-drawer").forEach(observeDrawerState);

        document.querySelectorAll("[data-demo-navigation-toggle]").forEach((button) => {
            if (button.dataset.demoNavigationToggleWired !== "1") {
                button.dataset.demoNavigationToggleWired = "1";
                button.addEventListener("click", () => {
                    const opening = button.dataset.demoNavigationToggleMode === "open";
                    requestOpen(opening);
                });
            }
        });

        document.querySelectorAll("#demo-navigation-drawer .bzs-navigation-drawer__backdrop").forEach((backdrop) => {
            if (backdrop.dataset.demoNavigationBackdropWired !== "1") {
                backdrop.dataset.demoNavigationBackdropWired = "1";
                backdrop.addEventListener("click", () => {
                    if (mobileQuery.matches) {
                        closeMobileNavigation();
                    } else {
                        requestOpen(false);
                    }
                });
            }
        });

        document.querySelectorAll("#demo-navigation-drawer .demo-nav-link").forEach((link) => {
            if (link.dataset.demoNavigationLinkWired !== "1") {
                link.dataset.demoNavigationLinkWired = "1";
                link.addEventListener("click", () => {
                    if (mobileQuery.matches) {
                        closeMobileNavigation();
                    }
                });
            }
        });

        applyState();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", wire);
    } else {
        wire();
    }

    // Blazor enhanced navigation replaces the layout DOM; rewire afterwards.
    document.addEventListener("enhancedload", wire);
    document.addEventListener("keydown", handleKeyDown);
    mobileQuery.addEventListener("change", () => {
        const activeElement = document.activeElement;
        const drawer = document.querySelector("#demo-navigation-drawer");
        const activeElementWasInDrawer = drawer?.contains(activeElement) ?? false;
        const activeElementWasOpenToggle = activeElement?.matches(
            "[data-demo-navigation-toggle-mode='open']") ?? false;
        mobileOpen = false;
        const open = readOpen();
        const focusMode = !open && activeElementWasInDrawer
            ? "open"
            : open && activeElementWasOpenToggle
                ? "close"
                : null;
        applyState(focusMode);
    });

    const hostObserver = new MutationObserver(wire);
    hostObserver.observe(document.body, { childList: true, subtree: true });
})();
