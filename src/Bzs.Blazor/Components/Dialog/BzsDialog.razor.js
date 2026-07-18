const overlays = new Map();
let bodyLockCount = 0;
let bodyOverflow = null;
let keydownListenerAttached = false;
let lastInteractionTarget = null;

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
        if (existing.modal !== modal) {
            if (existing.modal) {
                unlockBody();
            } else if (modal) {
                lockBody();
            }
        }

        existing.panel = panel;
        existing.modal = modal;
        existing.initialFocusSelector = initialFocusSelector;
        return;
    }

    const activeElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousFocus = lastInteractionTarget?.isConnected ? lastInteractionTarget : activeElement;
    lastInteractionTarget = null;
    const overlay = {
        panel,
        modal,
        initialFocusSelector,
        previousFocus
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

export function deactivate(id) {
    const overlay = overlays.get(id);
    if (!overlay) {
        return;
    }

    overlays.delete(id);
    if (overlay.modal) {
        unlockBody();
    }

    const topOverlay = getTopOverlay();
    if (topOverlay) {
        if (overlay.previousFocus?.isConnected && topOverlay.panel.contains(overlay.previousFocus)) {
            overlay.previousFocus.focus({ preventScroll: true });
        } else {
            focusInitial(topOverlay);
        }
    } else {
        restoreFocus(overlay.previousFocus);
    }

    detachKeydownListenerWhenUnused();
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

    const overlay = getTopOverlay();
    if (event.key === 'Escape') {
        if (overlay && !overlay.panel.contains(event.target)) {
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

function getTopOverlay() {
    let overlay = null;
    for (const value of overlays.values()) {
        overlay = value;
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
