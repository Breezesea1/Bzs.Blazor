const overlays = new Map();
let bodyLockCount = 0;
let bodyOverflow = null;
let keydownListenerAttached = false;
let lastInteractionTarget = null;

document.addEventListener('enhancedload', () => {
    for (const [id, overlay] of overlays) {
        if (overlay.navigationDrawer && !overlay.root?.isConnected) {
            deactivate(id);
        }
    }
});

const focusableSelector = [
    'a[href]',
    'area[href]',
    'button',
    'input:not([type="hidden"])',
    'select',
    'textarea',
    '[tabindex]',
    '[contenteditable="true"]'
].join(',');

export function activate(id, panel, modal, initialFocusSelector) {
    const existing = overlays.get(id);
    if (existing) {
        existing.panel = panel;
        existing.initialFocusSelector = initialFocusSelector;
        updateModalState(existing, modal);
        return;
    }

    const activeElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousFocus = lastInteractionTarget?.isConnected ? lastInteractionTarget : activeElement;
    lastInteractionTarget = null;
    const overlay = {
        panel,
        modal,
        initialFocusSelector,
        previousFocus,
        root: null,
        navigationDrawer: false,
        observer: null,
        backgroundTargets: []
    };
    overlays.set(id, overlay);
    if (modal) {
        lockBody();
    }

    attachKeydownListener();
    queueMicrotask(() => {
        if (overlays.get(id) === overlay) {
            focusInitial(overlay);
        }
    });
}

export function activateNavigationDrawer(id, root, panel, escapeTrigger, initialFocusSelector, variant) {
    const existing = overlays.get(id);
    if (existing) {
        existing.root = root;
        existing.panel = panel;
        existing.escapeTrigger = escapeTrigger;
        existing.initialFocusSelector = initialFocusSelector;
        existing.variant = variant;
        synchronizeNavigationDrawer(existing);
        return;
    }

    const activeElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousFocus = lastInteractionTarget?.isConnected ? lastInteractionTarget : activeElement;
    lastInteractionTarget = null;
    const overlay = {
        root,
        panel,
        escapeTrigger,
        modal: false,
        variant,
        initialFocusSelector,
        previousFocus,
        navigationDrawer: true,
        observer: null,
        backgroundObserver: null,
        backgroundParent: null,
        backgroundTargets: []
    };
    overlays.set(id, overlay);
    overlay.observer = new MutationObserver(() => synchronizeNavigationDrawer(overlay));
    overlay.observer.observe(root, {
        attributes: true,
        attributeFilter: ['class', 'data-bzs-open', 'style']
    });
    const resizeObserver = new ResizeObserver(() => synchronizeNavigationDrawer(overlay));
    resizeObserver.observe(root);
    overlay.resizeObserver = resizeObserver;
    synchronizeNavigationDrawer(overlay);
    attachKeydownListener();
}

export function deactivate(id) {
    const overlay = overlays.get(id);
    if (!overlay) {
        return;
    }

    overlays.delete(id);
    overlay.observer?.disconnect();
    overlay.resizeObserver?.disconnect();
    overlay.backgroundObserver?.disconnect();
    if (overlay.modal) {
        releaseBackground(overlay);
        unlockBody();
    }

    const topOverlay = getTopModalOverlay();
    if (topOverlay) {
        if (overlay.previousFocus?.isConnected && topOverlay.panel.contains(overlay.previousFocus)) {
            overlay.previousFocus.focus({ preventScroll: true });
        } else {
            focusInitial(topOverlay);
        }
    } else if (!overlay.navigationDrawer || overlay.modal) {
        restoreFocus(overlay.previousFocus);
    }

    detachKeydownListenerWhenUnused();
}

function synchronizeNavigationDrawer(overlay) {
    if (!overlay.root?.isConnected || overlay.root.dataset.bzsOpen !== 'true') {
        return;
    }

    const modal = overlay.variant === 'temporary'
        ? true
        : overlay.variant === 'persistent'
            ? false
            : isNavigationDrawerBackdropModal(overlay.root);
    updateModalState(overlay, modal);
}

function isNavigationDrawerBackdropModal(root) {
    const backdrop = root.querySelector('.bzs-navigation-drawer__backdrop');
    const backdropStyle = backdrop instanceof HTMLElement ? getComputedStyle(backdrop) : null;
    return backdropStyle !== null
        && backdropStyle.display !== 'none'
        && backdropStyle.visibility !== 'hidden'
        && backdropStyle.pointerEvents !== 'none';
}

function updateModalState(overlay, modal) {
    if (overlay.modal === modal) {
        return;
    }

    overlay.modal = modal;
    if (modal) {
        const activeElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        if (activeElement && !overlay.panel.contains(activeElement)) {
            overlay.previousFocus = lastInteractionTarget?.isConnected ? lastInteractionTarget : activeElement;
            lastInteractionTarget = null;
        }

        lockBody();
        makeBackgroundInert(overlay);
        queueMicrotask(() => {
            if (!Array.from(overlays.values()).includes(overlay)) {
                return;
            }

            if (!overlay.panel.contains(document.activeElement)) {
                focusInitial(overlay);
            }
        });
    } else {
        releaseBackground(overlay);
        unlockBody();
    }
}

function makeBackgroundInert(overlay) {
    reconcileBackground(overlay);
}

function reconcileBackground(overlay) {
    if (!overlay.modal) {
        return;
    }

    const parent = overlay.root?.parentElement;
    if (!parent) {
        releaseBackground(overlay);
        return;
    }

    if (overlay.backgroundParent !== parent) {
        overlay.backgroundObserver?.disconnect();
        overlay.backgroundParent = parent;
        overlay.backgroundObserver = new MutationObserver(() => reconcileBackground(overlay));
        overlay.backgroundObserver.observe(parent, { childList: true });
    }

    const nextTargets = Array.from(parent.children)
        .filter(element => element !== overlay.root && element instanceof HTMLElement);
    for (const target of overlay.backgroundTargets) {
        if (!nextTargets.includes(target)) {
            releaseBackgroundTarget(target);
        }
    }

    for (const target of nextTargets) {
        if (!overlay.backgroundTargets.includes(target)) {
            makeBackgroundTargetInert(target);
        }
    }

    overlay.backgroundTargets = nextTargets;
}

function makeBackgroundTargetInert(target) {
    const count = Number.parseInt(target.dataset.bzsOverlayInertCount ?? '0', 10);
    if (count === 0) {
        target.dataset.bzsOverlayWasInert = target.hasAttribute('inert') ? 'true' : 'false';
    }

    target.dataset.bzsOverlayInertCount = String(count + 1);
    target.setAttribute('inert', '');
}

function releaseBackground(overlay) {
    for (const target of overlay.backgroundTargets) {
        releaseBackgroundTarget(target);
    }

    overlay.backgroundTargets = [];
    overlay.backgroundObserver?.disconnect();
    overlay.backgroundObserver = null;
    overlay.backgroundParent = null;
}

function releaseBackgroundTarget(target) {
    const count = Number.parseInt(target.dataset.bzsOverlayInertCount ?? '1', 10) - 1;
    if (count > 0) {
        target.dataset.bzsOverlayInertCount = String(count);
        return;
    }

    if (target.dataset.bzsOverlayWasInert !== 'true') {
        target.removeAttribute('inert');
    }

    delete target.dataset.bzsOverlayInertCount;
    delete target.dataset.bzsOverlayWasInert;
}

function lockBody() {
    if (bodyLockCount++ !== 0) {
        return;
    }

    bodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
}

function unlockBody() {
    if (bodyLockCount === 0 || --bodyLockCount !== 0) {
        return;
    }

    document.body.style.overflow = bodyOverflow ?? '';
    bodyOverflow = null;
}

function attachKeydownListener() {
    if (keydownListenerAttached) {
        return;
    }

    document.addEventListener('keydown', handleKeydown, true);
    document.addEventListener('pointerdown', handlePointerDown, true);
    keydownListenerAttached = true;
}

function detachKeydownListenerWhenUnused() {
    if (!keydownListenerAttached || overlays.size !== 0) {
        return;
    }

    document.removeEventListener('keydown', handleKeydown, true);
    document.removeEventListener('pointerdown', handlePointerDown, true);
    keydownListenerAttached = false;
    lastInteractionTarget = null;
}

function handlePointerDown(event) {
    if (event.target instanceof HTMLElement) {
        lastInteractionTarget = event.target.closest(focusableSelector) ?? event.target;
    }
}

function handleKeydown(event) {
    if (event.target instanceof HTMLElement && event.key !== 'Tab' && event.key !== 'Escape') {
        lastInteractionTarget = event.target.closest(focusableSelector) ?? event.target;
    }

    if (event.key === 'Escape') {
        const overlay = getTopEscapeOverlay();
        if (overlay?.navigationDrawer && overlay.modal) {
            event.preventDefault();
            event.stopPropagation();
            overlay.escapeTrigger?.click();
        } else if (overlay && !overlay.panel.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
            overlay.panel.dispatchEvent(new KeyboardEvent('keydown', {
                key: 'Escape',
                bubbles: true
            }));
        }
        return;
    }

    if (event.key !== 'Tab') {
        return;
    }

    const overlay = getTopModalOverlay();
    if (!overlay?.modal) {
        return;
    }

    if (!overlay.panel.contains(document.activeElement)) {
        event.preventDefault();
        focusInitial(overlay);
        return;
    }

    const focusableElements = getFocusableElements(overlay.panel);
    if (focusableElements.length === 0) {
        event.preventDefault();
        focusPanel(overlay.panel);
        return;
    }

    const first = focusableElements[0];
    const last = focusableElements[focusableElements.length - 1];
    if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus({ preventScroll: true });
    } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus({ preventScroll: true });
    }
}

function getTopEscapeOverlay() {
    let overlay = null;
    for (const value of overlays.values()) {
        if (!value.navigationDrawer || value.modal) {
            overlay = value;
        }
    }

    return overlay;
}

function getTopModalOverlay() {
    let overlay = null;
    for (const value of overlays.values()) {
        if (value.modal) {
            overlay = value;
        }
    }

    return overlay;
}

function focusInitial(overlay) {
    const target = findInitialFocus(overlay.panel, overlay.initialFocusSelector)
        ?? getFocusableElements(overlay.panel)[0]
        ?? overlay.panel;
    target.focus({ preventScroll: true });
}

function findInitialFocus(panel, selector) {
    if (!selector) {
        const target = panel.querySelector('[autofocus], [data-bzs-initial-focus]');
        return isFocusableElement(target) ? target : null;
    }

    try {
        const target = panel.querySelector(selector);
        return isFocusableElement(target) ? target : null;
    } catch {
        return null;
    }
}

function getFocusableElements(panel) {
    return Array.from(panel.querySelectorAll(focusableSelector)).filter(isTabbableElement);
}

function isFocusableElement(element) {
    return element instanceof HTMLElement
        && element.matches(focusableSelector)
        && !element.hasAttribute('disabled')
        && !element.matches(':disabled')
        && !element.closest('[inert]')
        && isVisible(element);
}

function isTabbableElement(element) {
    return isFocusableElement(element) && element.tabIndex >= 0;
}

function isVisible(element) {
    const style = getComputedStyle(element);
    return style.visibility !== 'hidden'
        && style.visibility !== 'collapse'
        && element.getClientRects().length !== 0;
}

function focusPanel(panel) {
    panel.focus({ preventScroll: true });
}

function restoreFocus(element) {
    if (element?.isConnected) {
        element.focus({ preventScroll: true });
    }
}
