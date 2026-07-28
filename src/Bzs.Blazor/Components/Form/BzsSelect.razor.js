const instances = new Map();
let listenersAttached = false;
let invalidFocusScheduled = false;

function ensureListeners() {
    if (listenersAttached) return;
    document.addEventListener('pointerdown', handlePointerDown, true);
    listenersAttached = true;
}

function handlePointerDown(event) {
    for (const instance of instances.values()) {
        if (instance.open && !instance.root.contains(event.target)) {
            instance.open = false;
            detachPositionHandler(instance);
            void safeInvoke(instance.dotNetReference, 'CloseFromBrowserAsync');
        }
    }
}

async function safeInvoke(dotNetReference, method) {
    try {
        await dotNetReference.invokeMethodAsync(method);
    } catch {
        // The Blazor circuit may be gone during document-level event delivery.
    }
}

function handleKeyDown(event, instance) {
    const isTrigger = event.target.matches('[role="combobox"]');
    const isSearch = event.target.matches('input[type="search"]');
    if (!isTrigger && !isSearch) return;

    if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)
        || (event.key === 'Enter' && instance.open)
        || (event.key === ' ' && isTrigger)) {
        event.preventDefault();
    }
}

export function initialize(instanceId, root, dotNetReference) {
    const invalidHandler = event => {
        if (!event.target.matches('[data-bzs-select-constraint="true"]')) return;
        event.preventDefault();
        if (invalidFocusScheduled) return;
        invalidFocusScheduled = true;
        requestAnimationFrame(() => {
            invalidFocusScheduled = false;
            root.querySelector('[role="combobox"]')?.focus({ preventScroll: true });
        });
    };
    const instance = {
        root,
        dotNetReference,
        invalidHandler,
        open: false,
        positionHandler: null
    };
    instance.keydownHandler = event => handleKeyDown(event, instance);
    instances.set(instanceId, instance);
    root.addEventListener('keydown', instance.keydownHandler);
    root.addEventListener('invalid', invalidHandler, true);
    ensureListeners();
}

export function setOpen(instanceId, open, focusTarget) {
    const instance = instances.get(instanceId);
    if (!instance) return;

    instance.open = open;
    if (open) {
        detachPositionHandler(instance);
        positionPanel(instance);
        instance.positionHandler = () => positionPanel(instance);
        window.addEventListener('resize', instance.positionHandler, true);
        window.addEventListener('scroll', instance.positionHandler, true);
    } else {
        detachPositionHandler(instance);
    }

    if (focusTarget) {
        requestAnimationFrame(() => focusTarget.focus({ preventScroll: true }));
    }
}

function positionPanel(instance) {
    const trigger = instance.root.querySelector('[role="combobox"]');
    const panel = instance.root.querySelector('[data-bzs-select-panel="true"]');
    if (!trigger || !panel) return;

    const triggerRect = trigger.getBoundingClientRect();
    const padding = 8;
    const gap = 4;
    const containingBlock = findFixedContainingBlock(trigger);
    if (containingBlock) {
        Object.assign(panel.style, {
            position: 'absolute',
            left: '0',
            top: `${instance.root.offsetHeight + gap}px`,
            width: `${instance.root.clientWidth}px`,
            zIndex: '1100',
            visibility: 'visible'
        });
        return;
    }
    const containingRect = containingBlock?.getBoundingClientRect() ?? { left: 0, top: 0 };
    const width = Math.min(triggerRect.width, window.innerWidth - padding * 2);
    const panelHeight = panel.getBoundingClientRect().height;
    const below = triggerRect.bottom + gap;
    const above = triggerRect.top - gap - panelHeight;
    const top = below + panelHeight <= window.innerHeight - padding || above < padding ? below : above;
    const viewportLeft = Math.min(
        Math.max(padding, triggerRect.left),
        Math.max(padding, window.innerWidth - padding - width));

    Object.assign(panel.style, {
        position: 'fixed',
        left: `${viewportLeft - containingRect.left}px`,
        top: `${Math.max(padding, top) - containingRect.top}px`,
        width: `${width}px`,
        zIndex: '1100',
        visibility: 'visible'
    });
}

function findFixedContainingBlock(element) {
    let current = element.parentElement;
    while (current) {
        const style = getComputedStyle(current);
        if (style.transform !== 'none'
            || style.perspective !== 'none'
            || style.filter !== 'none'
            || style.backdropFilter !== 'none'
            || style.contain.includes('paint')) {
            return current;
        }
        current = current.parentElement;
    }
    return null;
}

function detachPositionHandler(instance) {
    if (!instance.positionHandler) return;
    window.removeEventListener('resize', instance.positionHandler, true);
    window.removeEventListener('scroll', instance.positionHandler, true);
    instance.positionHandler = null;
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;
    detachPositionHandler(instance);
    instance.root.removeEventListener('keydown', instance.keydownHandler);
    instance.root.removeEventListener('invalid', instance.invalidHandler, true);
    instances.delete(instanceId);
    if (instances.size === 0 && listenersAttached) {
        document.removeEventListener('pointerdown', handlePointerDown, true);
        listenersAttached = false;
    }
}
