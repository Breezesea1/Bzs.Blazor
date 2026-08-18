const resizeCompletedMethod = 'NotifyResizeCompletedAsync';
const drawerWidthProperty = '--bzs-navigation-drawer-width';
const controllers = new Map();
let staticResizeObserver = null;

document.addEventListener('enhancedload', () => {
    cleanupDisconnectedControllers();
    wireStaticResizeControllers();
});

class NavigationDrawerResizeController {
    #root;
    #panel;
    #handle;
    #dotNetReference;
    #shell;
    #abortController = new AbortController();
    #resizeObserver;
    #minimumWidth;
    #maximumWidth;
    #resizeStep;
    #position;
    #preferredWidth;
    #currentWidth;
    #activePointerId = null;
    #dragStartX = 0;
    #dragStartWidth = 0;
    #initialRootWidth;
    #initialRootWidthPriority;
    #initialShellWidth;
    #initialShellWidthPriority;

    constructor(
        root,
        panel,
        handle,
        dotNetReference,
        minimumWidth,
        maximumWidth,
        resizeStep,
        position) {
        this.#root = root;
        this.#panel = panel;
        this.#handle = handle;
        this.#dotNetReference = dotNetReference;
        this.#shell = root.closest('[data-bzs-app-shell]');
        this.#initialRootWidth = root.style.getPropertyValue(drawerWidthProperty);
        this.#initialRootWidthPriority = root.style.getPropertyPriority(drawerWidthProperty);
        this.#initialShellWidth = this.#shell?.style.getPropertyValue(drawerWidthProperty) ?? '';
        this.#initialShellWidthPriority = this.#shell?.style.getPropertyPriority(drawerWidthProperty) ?? '';
        this.update(minimumWidth, maximumWidth, resizeStep, position, dotNetReference);

        const signal = this.#abortController.signal;
        handle.addEventListener('pointerdown', this.#handlePointerDown, { signal });
        handle.addEventListener('pointermove', this.#handlePointerMove, { signal });
        handle.addEventListener('pointerup', this.#handlePointerUp, { signal });
        handle.addEventListener('pointercancel', this.#handlePointerCancel, { signal });
        handle.addEventListener('keydown', this.#handleKeyDown, { signal });
        this.#resizeObserver = new ResizeObserver(this.#handleAvailableSizeChanged);
        this.#resizeObserver.observe(root);
    }

    get currentWidth() {
        return this.#currentWidth;
    }

    matches(panel, handle) {
        return this.#panel === panel && this.#handle === handle;
    }

    update(minimumWidth, maximumWidth, resizeStep, position, dotNetReference = null) {
        this.#minimumWidth = minimumWidth;
        this.#maximumWidth = maximumWidth;
        this.#resizeStep = resizeStep;
        this.#position = position;
        this.#dotNetReference = dotNetReference ?? this.#dotNetReference;
        this.#preferredWidth ??= this.#panel.getBoundingClientRect().width;
        this.#applyPreferredWidth();
    }

    dispose() {
        this.#finishPointerCapture();
        this.#abortController.abort();
        this.#resizeObserver.disconnect();
        this.#restoreWidth(this.#root, this.#initialRootWidth, this.#initialRootWidthPriority);
        this.#restoreWidth(this.#shell, this.#initialShellWidth, this.#initialShellWidthPriority);
        this.#root.removeAttribute('data-bzs-resizing');
    }

    #handlePointerDown = (event) => {
        if (event.button !== 0 || this.#activePointerId !== null) {
            return;
        }

        event.preventDefault();
        this.#activePointerId = event.pointerId;
        this.#dragStartX = event.clientX;
        this.#dragStartWidth = this.#currentWidth;
        this.#root.setAttribute('data-bzs-resizing', 'true');
        this.#handle.setPointerCapture(event.pointerId);
    };

    #handlePointerMove = (event) => {
        if (event.pointerId !== this.#activePointerId) {
            return;
        }

        event.preventDefault();
        const physicalDelta = event.clientX - this.#dragStartX;
        const widthDelta = this.#isAnchoredLeft() ? physicalDelta : -physicalDelta;
        this.#setPreferredWidth(this.#dragStartWidth + widthDelta);
    };

    #handlePointerUp = async (event) => {
        if (event.pointerId !== this.#activePointerId) {
            return;
        }

        event.preventDefault();
        this.#finishPointerCapture();
        await this.#notifyResizeCompleted();
    };

    #handlePointerCancel = (event) => {
        if (event.pointerId !== this.#activePointerId) {
            return;
        }

        this.#setPreferredWidth(this.#dragStartWidth);
        this.#finishPointerCapture();
    };

    #handleKeyDown = async (event) => {
        let width = this.#currentWidth;
        if (event.key === 'Home') {
            width = this.#effectiveMinimumWidth();
        } else if (event.key === 'End') {
            width = this.#effectiveMaximumWidth();
        } else if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            const physicalDirection = event.key === 'ArrowRight' ? 1 : -1;
            const widthDirection = this.#isAnchoredLeft() ? physicalDirection : -physicalDirection;
            width += widthDirection * this.#resizeStep;
        } else {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.#setPreferredWidth(width);
        await this.#notifyResizeCompleted();
    };

    #handleAvailableSizeChanged = () => {
        this.#applyPreferredWidth();
    };

    #setPreferredWidth(width) {
        this.#preferredWidth = this.#constrain(width);
        this.#applyPreferredWidth();
    }

    #applyPreferredWidth() {
        this.#currentWidth = Math.round(this.#constrain(this.#preferredWidth) * 100) / 100;
        const cssWidth = `${this.#currentWidth}px`;
        this.#root.style.setProperty(drawerWidthProperty, cssWidth);
        this.#shell?.style.setProperty(drawerWidthProperty, cssWidth);
        this.#handle.setAttribute('aria-valuemin', `${this.#effectiveMinimumWidth()}`);
        this.#handle.setAttribute('aria-valuemax', `${this.#effectiveMaximumWidth()}`);
        this.#handle.setAttribute('aria-valuenow', `${this.#currentWidth}`);
        this.#handle.setAttribute('aria-valuetext', `${this.#currentWidth} pixels`);
    }

    #constrain(width) {
        return Math.min(
            Math.max(width, this.#effectiveMinimumWidth()),
            this.#effectiveMaximumWidth());
    }

    #effectiveMinimumWidth() {
        return Math.min(this.#minimumWidth, this.#effectiveMaximumWidth());
    }

    #effectiveMaximumWidth() {
        const availableWidth = this.#root.getBoundingClientRect().width;
        return availableWidth > 0
            ? Math.min(this.#maximumWidth, availableWidth)
            : this.#maximumWidth;
    }

    #isAnchoredLeft() {
        const rightToLeft = getComputedStyle(this.#root).direction === 'rtl';
        return this.#position === 'start' ? !rightToLeft : rightToLeft;
    }

    #finishPointerCapture() {
        if (this.#activePointerId === null) {
            return;
        }

        if (this.#handle.hasPointerCapture(this.#activePointerId)) {
            this.#handle.releasePointerCapture(this.#activePointerId);
        }

        this.#activePointerId = null;
        this.#root.removeAttribute('data-bzs-resizing');
    }

    async #notifyResizeCompleted() {
        if (!this.#dotNetReference) {
            return;
        }

        try {
            await this.#dotNetReference.invokeMethodAsync(
                resizeCompletedMethod,
                this.#currentWidth);
            this.#applyPreferredWidth();
        } catch {
            // The interactive circuit may be disconnecting.
        }
    }

    #restoreWidth(element, value, priority) {
        if (!element) {
            return;
        }

        if (value) {
            element.style.setProperty(drawerWidthProperty, value, priority);
        } else {
            element.style.removeProperty(drawerWidthProperty);
        }
    }
}

export function configure(
    root,
    panel,
    handle,
    dotNetReference,
    minimumWidth,
    maximumWidth,
    resizeStep,
    position) {
    let controller = controllers.get(root);
    if (controller && !controller.matches(panel, handle)) {
        controller.dispose();
        controllers.delete(root);
        controller = null;
    }

    if (!controller) {
        controller = new NavigationDrawerResizeController(
            root,
            panel,
            handle,
            dotNetReference,
            minimumWidth,
            maximumWidth,
            resizeStep,
            position);
        controllers.set(root, controller);
    } else {
        controller.update(minimumWidth, maximumWidth, resizeStep, position, dotNetReference);
    }

    return controller.currentWidth;
}

export function disable(root) {
    const controller = controllers.get(root);
    controller?.dispose();
    controllers.delete(root);
}

export function startStaticResizeControllers() {
    wireStaticResizeControllers();
    if (staticResizeObserver || !document.body) {
        return;
    }

    staticResizeObserver = new MutationObserver(() => {
        cleanupDisconnectedControllers();
        wireStaticResizeControllers();
    });
    staticResizeObserver.observe(document.body, {
        attributes: true,
        attributeFilter: [
            'data-bzs-navigation-drawer-resizable',
            'data-bzs-navigation-drawer-position',
            'data-bzs-navigation-drawer-minimum-width',
            'data-bzs-navigation-drawer-maximum-width',
            'data-bzs-navigation-drawer-resize-step'
        ],
        childList: true,
        subtree: true
    });
}

function wireStaticResizeControllers() {
    document.querySelectorAll('[data-bzs-navigation-drawer-resizable="true"]')
        .forEach((root) => {
            const panel = root.querySelector('.bzs-navigation-drawer__panel');
            const handle = root.querySelector('.bzs-navigation-drawer__resize-handle');
            if (!(panel instanceof HTMLElement) || !(handle instanceof HTMLElement)) {
                return;
            }

            configure(
                root,
                panel,
                handle,
                null,
                readPositiveNumber(root.dataset.bzsNavigationDrawerMinimumWidth, 192),
                readPositiveNumber(root.dataset.bzsNavigationDrawerMaximumWidth, 480),
                readPositiveNumber(root.dataset.bzsNavigationDrawerResizeStep, 16),
                root.dataset.bzsNavigationDrawerPosition === 'end' ? 'end' : 'start');
        });
}

function cleanupDisconnectedControllers() {
    for (const [root, controller] of controllers) {
        if (!root.isConnected || root.dataset.bzsNavigationDrawerResizable !== 'true') {
            controller.dispose();
            controllers.delete(root);
        }
    }
}

function readPositiveNumber(value, fallback) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}
