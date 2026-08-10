const instances = new Map();
let documentListenersAttached = false;

const closeFromBrowserMethod = 'CloseFromBrowserAsync';

export function initialize(instanceId, root, dotNetReference) {
    dispose(instanceId);
    instances.set(instanceId, {
        root,
        dotNetReference,
        open: false,
        placement: 'bottom-start',
        closeOnOutsideInteraction: true,
        closeOnEscape: true,
        closeRequestPending: false,
        clientX: null,
        clientY: null,
        positionHandler: null,
        resizeObserver: null
    });
    ensureDocumentListeners();
}

export function setOpen(
    instanceId,
    open,
    placement,
    closeOnOutsideInteraction,
    closeOnEscape,
    restoreFocus) {
    setOpenCore(
        instanceId,
        open,
        placement,
        closeOnOutsideInteraction,
        closeOnEscape,
        restoreFocus,
        null,
        null);
}

export function setOpenAt(
    instanceId,
    open,
    placement,
    closeOnOutsideInteraction,
    closeOnEscape,
    restoreFocus,
    clientX,
    clientY) {
    setOpenCore(
        instanceId,
        open,
        placement,
        closeOnOutsideInteraction,
        closeOnEscape,
        restoreFocus,
        clientX,
        clientY);
}

function setOpenCore(
    instanceId,
    open,
    placement,
    closeOnOutsideInteraction,
    closeOnEscape,
    restoreFocus,
    clientX,
    clientY) {
    const instance = instances.get(instanceId);
    if (!instance) return;

    instance.open = open;
    instance.placement = placement;
    instance.closeOnOutsideInteraction = closeOnOutsideInteraction;
    instance.closeOnEscape = closeOnEscape;
    instance.clientX = Number.isFinite(clientX) ? clientX : null;
    instance.clientY = Number.isFinite(clientY) ? clientY : null;
    detachPositioning(instance);

    if (open) {
        instances.delete(instanceId);
        instances.set(instanceId, instance);
        position(instance);
        instance.positionHandler = () => position(instance);
        window.addEventListener('resize', instance.positionHandler, true);
        window.addEventListener('scroll', instance.positionHandler, true);
        if (typeof ResizeObserver !== 'undefined') {
            instance.resizeObserver = new ResizeObserver(instance.positionHandler);
            instance.resizeObserver.observe(instance.root);
            const panel = getPanel(instance);
            if (panel) instance.resizeObserver.observe(panel);
        }
    } else if (restoreFocus) {
        getAnchor(instance)?.focus({ preventScroll: true });
    }
}

function ensureDocumentListeners() {
    if (documentListenersAttached) return;
    document.addEventListener('pointerdown', handlePointerDown, true);
    document.addEventListener('keydown', handleKeyDown, true);
    documentListenersAttached = true;
}

function handlePointerDown(event) {
    for (const instance of instances.values()) {
        if (instance.open
            && instance.closeOnOutsideInteraction
            && !instance.root.contains(event.target)) {
            requestClose(instance, false);
        }
    }
}

function handleKeyDown(event) {
    preventMenuNavigationDefault(event);
    if (event.key !== 'Escape') return;
    const openInstances = [...instances.values()].filter(instance => instance.open && instance.closeOnEscape);
    const instance = openInstances.at(-1);
    if (!instance) return;
    event.preventDefault();
    event.stopPropagation();
    requestClose(instance, true);
}

function preventMenuNavigationDefault(event) {
    if (!(event.target instanceof Element)) return;

    const instance = [...instances.values()]
        .reverse()
        .find(candidate => (candidate.root.matches('[data-bzs-menu="true"]')
                || candidate.root.matches('[data-bzs-context-menu="true"]'))
            && candidate.root.contains(event.target));
    if (!instance) return;

    const isMenuTrigger = instance.root.matches('[data-bzs-menu="true"]')
        && event.target.matches('[data-bzs-anchor="true"]')
        && (event.key === 'ArrowDown' || event.key === 'ArrowUp');
    const isMenuItem = instance.open
        && event.target.matches('[role="menuitem"], [role="menuitemcheckbox"]')
        && ['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key);
    if (isMenuTrigger || isMenuItem) {
        event.preventDefault();
    }
}

async function requestClose(instance, restoreFocus) {
    if (!instance.open || instance.closeRequestPending) return;
    instance.closeRequestPending = true;
    try {
        await instance.dotNetReference.invokeMethodAsync(closeFromBrowserMethod, restoreFocus);
    } catch {
        // The Blazor circuit may be gone during document-level event delivery.
    } finally {
        instance.closeRequestPending = false;
    }
}

function position(instance) {
    const anchor = getAnchor(instance);
    const panel = getPanel(instance);
    if (!anchor || !panel) return;

    const padding = 8;
    const gap = 4;
    const measuredAnchorRect = anchor.getBoundingClientRect();
    const anchorRect = instance.clientX === null || instance.clientY === null
        ? measuredAnchorRect
        : {
            left: instance.clientX,
            right: instance.clientX,
            top: instance.clientY,
            bottom: instance.clientY,
            width: 0,
            height: 0
        };
    panel.style.position = 'fixed';
    panel.style.inset = 'auto';
    panel.style.left = '0';
    panel.style.top = '0';
    panel.style.visibility = 'hidden';

    const containingBlock = findFixedContainingBlock(panel);
    const containingRect = containingBlock?.getBoundingClientRect() ?? null;
    const scaleX = containingBlock?.offsetWidth > 0 && containingRect.width > 0
        ? containingRect.width / containingBlock.offsetWidth
        : 1;
    const scaleY = containingBlock?.offsetHeight > 0 && containingRect.height > 0
        ? containingRect.height / containingBlock.offsetHeight
        : 1;
    const availableWidth = Math.max(0, window.innerWidth - padding * 2);
    const availableHeight = Math.max(0, window.innerHeight - padding * 2);
    panel.style.minWidth = `${Math.min(Math.max(measuredAnchorRect.width, 160), availableWidth) / scaleX}px`;
    panel.style.maxWidth = `${availableWidth / scaleX}px`;
    panel.style.maxHeight = `${availableHeight / scaleY}px`;

    const panelRect = panel.getBoundingClientRect();
    const rtl = getComputedStyle(instance.root).direction === 'rtl';
    const preferred = calculatePosition(instance.placement, anchorRect, panelRect, gap, rtl);
    const flipped = calculatePosition(flip(instance.placement), anchorRect, panelRect, gap, rtl);
    const useFlipped = overflow(preferred, panelRect, padding) > overflow(flipped, panelRect, padding);
    const selected = useFlipped ? flipped : preferred;
    const left = clamp(selected.left, padding, Math.max(padding, window.innerWidth - padding - panelRect.width));
    const top = clamp(selected.top, padding, Math.max(padding, window.innerHeight - padding - panelRect.height));

    panel.style.left = containingRect ? `${(left - containingRect.left) / scaleX}px` : `${left}px`;
    panel.style.top = containingRect ? `${(top - containingRect.top) / scaleY}px` : `${top}px`;
    panel.style.visibility = 'visible';
}

function findFixedContainingBlock(element) {
    let current = element.parentElement;
    while (current) {
        const style = getComputedStyle(current);
        const containment = style.contain.split(/\s+/);
        const willChange = style.willChange.split(',').map(value => value.trim());
        if (hasNonDefaultEffect(style.transform)
            || hasNonDefaultEffect(style.perspective)
            || hasNonDefaultEffect(style.filter)
            || hasNonDefaultEffect(style.backdropFilter)
            || containment.some(value => ['layout', 'paint', 'content', 'strict'].includes(value))
            || willChange.some(value => ['transform', 'perspective', 'filter', 'backdrop-filter'].includes(value))
            || style.contentVisibility === 'auto') {
            return current;
        }
        current = current.parentElement;
    }
    return null;
}

function hasNonDefaultEffect(value) {
    return value !== undefined && value !== '' && value !== 'none';
}

function calculatePosition(placement, anchor, panel, gap, rtl) {
    const start = rtl ? anchor.right - panel.width : anchor.left;
    const end = rtl ? anchor.left : anchor.right - panel.width;
    switch (placement) {
        case 'bottom': return { left: anchor.left + (anchor.width - panel.width) / 2, top: anchor.bottom + gap };
        case 'bottom-end': return { left: end, top: anchor.bottom + gap };
        case 'top-start': return { left: start, top: anchor.top - panel.height - gap };
        case 'top': return { left: anchor.left + (anchor.width - panel.width) / 2, top: anchor.top - panel.height - gap };
        case 'top-end': return { left: end, top: anchor.top - panel.height - gap };
        case 'start': return rtl
            ? { left: anchor.right + gap, top: anchor.top + (anchor.height - panel.height) / 2 }
            : { left: anchor.left - panel.width - gap, top: anchor.top + (anchor.height - panel.height) / 2 };
        case 'end': return rtl
            ? { left: anchor.left - panel.width - gap, top: anchor.top + (anchor.height - panel.height) / 2 }
            : { left: anchor.right + gap, top: anchor.top + (anchor.height - panel.height) / 2 };
        default: return { left: start, top: anchor.bottom + gap };
    }
}

function flip(placement) {
    if (placement.startsWith('bottom')) return placement.replace('bottom', 'top');
    if (placement.startsWith('top')) return placement.replace('top', 'bottom');
    return placement === 'start' ? 'end' : 'start';
}

function overflow(position, panel, padding) {
    const right = position.left + panel.width;
    const bottom = position.top + panel.height;
    return Math.max(0, padding - position.left)
        + Math.max(0, padding - position.top)
        + Math.max(0, right - (window.innerWidth - padding))
        + Math.max(0, bottom - (window.innerHeight - padding));
}

function clamp(value, minimum, maximum) {
    return Math.min(Math.max(value, minimum), maximum);
}

function getAnchor(instance) {
    return instance.root.querySelector('[data-bzs-anchor="true"]');
}

function getPanel(instance) {
    return instance.root.querySelector('[data-bzs-anchored-panel="true"]');
}

function detachPositioning(instance) {
    if (instance.positionHandler) {
        window.removeEventListener('resize', instance.positionHandler, true);
        window.removeEventListener('scroll', instance.positionHandler, true);
        instance.positionHandler = null;
    }
    instance.resizeObserver?.disconnect();
    instance.resizeObserver = null;
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;
    detachPositioning(instance);
    instances.delete(instanceId);
    if (instances.size === 0 && documentListenersAttached) {
        document.removeEventListener('pointerdown', handlePointerDown, true);
        document.removeEventListener('keydown', handleKeyDown, true);
        documentListenersAttached = false;
    }
}
