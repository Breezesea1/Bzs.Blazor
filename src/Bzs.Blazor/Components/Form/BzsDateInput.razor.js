const CLOSE_FROM_BROWSER = 'CloseFromBrowserAsync';
const instances = new Map();
let listenersAttached = false;

function ensureListeners() {
    if (listenersAttached) return;
    document.addEventListener('pointerdown', handlePointerDown, true);
    listenersAttached = true;
}

function handlePointerDown(event) {
    for (const instance of instances.values()) {
        if (instance.open && !instance.root.contains(event.target)) {
            hidePanel(instance);
            void safeInvoke(instance.dotNetReference, CLOSE_FROM_BROWSER);
        }
    }
}

async function safeInvoke(dotNetReference, method) {
    try {
        await dotNetReference.invokeMethodAsync(method);
    } catch {
        // A document event can outlive its Blazor circuit.
    }
}

function handleKeyDown(event, instance) {
    const isInput = event.target.matches('[data-bzs-date-picker-input="true"]');
    const isDay = event.target.matches('[data-bzs-date-picker-day="true"]');
    const isPeriod = event.target.matches('[data-bzs-date-picker-period]');
    if (!isInput && !isDay && !isPeriod) return;

    if ((isInput && event.key === 'ArrowDown')
        || (instance.open && ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', 'PageUp', 'PageDown', 'Escape'].includes(event.key))
        || (isDay && ['Enter', ' '].includes(event.key))
        || (isPeriod && ['Enter', ' '].includes(event.key))) {
        event.preventDefault();
    }
}

export function initialize(instanceId, root, dotNetReference) {
    const previousInstance = instances.get(instanceId);
    if (previousInstance) cleanupInstance(previousInstance);

    const instance = {
        root,
        dotNetReference,
        open: false,
        pointerOffsetX: null,
        pointerOffsetY: null,
        positionHandler: null
    };
    instance.keydownHandler = event => handleKeyDown(event, instance);
    instances.set(instanceId, instance);
    root.addEventListener('keydown', instance.keydownHandler);
    ensureListeners();
    return formatLocalDate(new Date());
}

function formatLocalDate(date) {
    const year = date.getFullYear().toString().padStart(4, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
}

export function setOpen(instanceId, open, pointerX, pointerY, focusCalendar, focusTarget) {
    const instance = instances.get(instanceId);
    if (!instance) return;

    instance.open = open;
    const input = instance.root.querySelector('[data-bzs-date-picker-input="true"]');
    const inputRect = input?.getBoundingClientRect() ?? null;
    const hasPointer = Number.isFinite(pointerX) && Number.isFinite(pointerY) && inputRect !== null;
    instance.pointerOffsetX = hasPointer ? pointerX - inputRect.left : null;
    instance.pointerOffsetY = hasPointer ? pointerY - inputRect.top : null;
    detachPositionHandler(instance);

    if (open) {
        positionPanel(instance);
        instance.positionHandler = () => positionPanel(instance);
        window.addEventListener('resize', instance.positionHandler, true);
        window.addEventListener('scroll', instance.positionHandler, true);
        if (focusCalendar) focusActiveDay(instanceId);
    } else {
        hidePanel(instance);
    }

    if (focusTarget) {
        requestAnimationFrame(() => focusTarget.focus({ preventScroll: true }));
    }
}

export function focusActiveDay(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance?.open) return;
    requestAnimationFrame(() => {
        instance.root
            .querySelector('[data-bzs-date-picker-day="true"][tabindex="0"]')
            ?.focus({ preventScroll: true });
    });
}

export function scrollActivePeriodOption(menu) {
    requestAnimationFrame(() => {
        menu
            ?.querySelector('.bzs-date-picker__period-option--active')
            ?.scrollIntoView({ block: 'nearest' });
    });
}

function positionPanel(instance) {
    const input = instance.root.querySelector('[data-bzs-date-picker-input="true"]');
    const panel = instance.root.querySelector('[data-bzs-date-picker-panel="true"]');
    if (!input || !panel) return;

    showPanel(panel);
    const padding = 8;
    const gap = 8;
    const inputRect = input.getBoundingClientRect();
    panel.style.position = 'fixed';
    panel.style.left = '0';
    panel.style.top = '0';
    panel.style.visibility = 'hidden';

    const containingBlock = isPopoverOpen(panel) ? null : findFixedContainingBlock(panel);
    const containingRect = containingBlock?.getBoundingClientRect() ?? null;
    const scaleX = containingBlock?.offsetWidth > 0 && containingRect.width > 0
        ? containingRect.width / containingBlock.offsetWidth
        : 1;
    const scaleY = containingBlock?.offsetHeight > 0 && containingRect.height > 0
        ? containingRect.height / containingBlock.offsetHeight
        : 1;
    const availableViewportWidth = Math.max(0, window.innerWidth - padding * 2);
    const availableViewportHeight = Math.max(0, window.innerHeight - padding * 2);
    panel.style.maxWidth = `${availableViewportWidth / scaleX}px`;
    panel.style.maxHeight = `${availableViewportHeight / scaleY}px`;

    const panelRect = panel.getBoundingClientRect();
    const hasPointer = instance.pointerOffsetX !== null && instance.pointerOffsetY !== null;
    const pointerX = hasPointer ? inputRect.left + instance.pointerOffsetX : null;
    const pointerY = hasPointer ? inputRect.top + instance.pointerOffsetY : null;
    const preferredLeft = hasPointer ? pointerX : inputRect.left;
    const preferredTop = hasPointer ? pointerY + gap : inputRect.bottom + gap;
    const alternateTop = hasPointer
        ? pointerY - gap - panelRect.height
        : inputRect.top - gap - panelRect.height;
    const fitsBelow = preferredTop + panelRect.height <= window.innerHeight - padding;
    const fitsAbove = alternateTop >= padding;
    const maximumViewportTop = Math.max(padding, window.innerHeight - padding - panelRect.height);
    const viewportTop = fitsBelow
        ? preferredTop
        : fitsAbove
            ? alternateTop
            : Math.min(Math.max(padding, preferredTop), maximumViewportTop);
    const viewportLeft = Math.min(
        Math.max(padding, preferredLeft),
        Math.max(padding, window.innerWidth - padding - panelRect.width));

    if (containingBlock) {
        panel.style.left = `${(viewportLeft - containingRect.left) / scaleX}px`;
        panel.style.top = `${(viewportTop - containingRect.top) / scaleY}px`;
    } else {
        panel.style.left = `${viewportLeft}px`;
        panel.style.top = `${viewportTop}px`;
    }

    panel.style.zIndex = '1200';
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
            || willChange.some(value => [
                'transform',
                'perspective',
                'filter',
                'backdrop-filter'
            ].includes(value))
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

function isPopoverOpen(panel) {
    if (typeof panel.showPopover !== 'function') return false;
    try {
        return panel.matches(':popover-open');
    } catch {
        return false;
    }
}

function showPanel(panel) {
    if (typeof panel.showPopover !== 'function' || isPopoverOpen(panel)) return;
    try {
        panel.showPopover();
    } catch {
        // Browsers without a usable Popover API keep the in-place fallback.
    }
}

function hidePanel(instance) {
    instance.open = false;
    detachPositionHandler(instance);
    const panel = instance.root.querySelector('[data-bzs-date-picker-panel="true"]');
    if (!panel) return;
    panel.style.visibility = 'hidden';
    if (typeof panel.hidePopover === 'function' && isPopoverOpen(panel)) {
        try {
            panel.hidePopover();
        } catch {
            // The panel can be detached while an interactive render is closing.
        }
    }
}

function detachPositionHandler(instance) {
    if (!instance.positionHandler) return;
    window.removeEventListener('resize', instance.positionHandler, true);
    window.removeEventListener('scroll', instance.positionHandler, true);
    instance.positionHandler = null;
}

function cleanupInstance(instance) {
    hidePanel(instance);
    instance.root.removeEventListener('keydown', instance.keydownHandler);
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) return;
    cleanupInstance(instance);
    instances.delete(instanceId);
    if (instances.size === 0 && listenersAttached) {
        document.removeEventListener('pointerdown', handlePointerDown, true);
        listenersAttached = false;
    }
}
