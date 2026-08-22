const instances = new Map();

const minimumColumnWidth = 48;
const keyboardStep = 16;

export function synchronize(input, checked, indeterminate) {
    if (!input) {
        return;
    }

    input.checked = checked;
    input.indeterminate = indeterminate;
}

/**
 * Applies declared column widths through the CSSOM and wires the resize handles.
 * Widths live on the `col` elements so no inline style attribute is emitted by the
 * server-rendered markup, keeping the default path compatible with `style-src 'self'`.
 */
export function applyColumnLayout(table, resizable, reference) {
    if (!table) {
        return;
    }

    let instance = instances.get(table);
    if (!instance) {
        instance = new ColumnLayoutController(table);
        instances.set(table, instance);
    }

    instance.update(resizable, reference);
}

export function disposeColumnLayout(table) {
    const instance = instances.get(table);
    if (!instance) {
        return;
    }

    instance.dispose();
    instances.delete(table);
}

class ColumnLayoutController {
    #table;
    #reference = null;
    #resizable = false;
    #activeKey = null;
    #startX = 0;
    #startWidth = 0;
    #handlePointerDown;
    #handlePointerMove;
    #handlePointerUp;
    #handleKeyDown;

    constructor(table) {
        this.#table = table;
        this.#handlePointerDown = (event) => this.#onPointerDown(event);
        this.#handlePointerMove = (event) => this.#onPointerMove(event);
        this.#handlePointerUp = (event) => this.#onPointerUp(event);
        this.#handleKeyDown = (event) => this.#onKeyDown(event);
        table.addEventListener("pointerdown", this.#handlePointerDown);
        table.addEventListener("keydown", this.#handleKeyDown);
    }

    update(resizable, reference) {
        this.#resizable = resizable === true;
        if (reference) {
            this.#reference = reference;
        }

        this.#applyDeclaredWidths();
    }

    dispose() {
        this.#table.removeEventListener("pointerdown", this.#handlePointerDown);
        this.#table.removeEventListener("keydown", this.#handleKeyDown);
        this.#detachDragListeners();
        this.#reference = null;
    }

    #applyDeclaredWidths() {
        const headers = this.#table.querySelectorAll("thead th[data-bzs-data-grid-column]");
        for (const header of headers) {
            const key = header.dataset.bzsDataGridColumn;
            const column = this.#findColumn(key);
            if (!column) {
                continue;
            }

            const width = header.dataset.bzsWidth;
            const minWidth = header.dataset.bzsMinWidth;
            if (width) {
                column.style.setProperty("width", width);
            } else if (!column.dataset.bzsResized) {
                column.style.removeProperty("width");
            }

            if (minWidth) {
                column.style.setProperty("min-width", minWidth);
            } else {
                column.style.removeProperty("min-width");
            }
        }
    }

    #findColumn(key) {
        if (!key) {
            return null;
        }

        return this.#table.querySelector(`colgroup col[data-bzs-data-grid-column="${CSS.escape(key)}"]`);
    }

    #findHandleKey(target) {
        const handle = target instanceof Element
            ? target.closest("[data-bzs-data-grid-resize]")
            : null;
        return handle?.dataset.bzsDataGridResize ?? null;
    }

    #measure(key) {
        const header = this.#table.querySelector(
            `thead th[data-bzs-data-grid-column="${CSS.escape(key)}"]`);
        return header ? header.getBoundingClientRect().width : 0;
    }

    #isRightToLeft() {
        return getComputedStyle(this.#table).direction === "rtl";
    }

    #onPointerDown(event) {
        if (!this.#resizable || event.button !== 0) {
            return;
        }

        const key = this.#findHandleKey(event.target);
        if (!key) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.#activeKey = key;
        this.#startX = event.clientX;
        this.#startWidth = this.#measure(key);
        this.#table.ownerDocument.addEventListener("pointermove", this.#handlePointerMove);
        this.#table.ownerDocument.addEventListener("pointerup", this.#handlePointerUp);
        this.#table.ownerDocument.addEventListener("pointercancel", this.#handlePointerUp);
    }

    #onPointerMove(event) {
        if (!this.#activeKey) {
            return;
        }

        const delta = this.#isRightToLeft()
            ? this.#startX - event.clientX
            : event.clientX - this.#startX;
        this.#resizeTo(this.#activeKey, this.#startWidth + delta);
    }

    #onPointerUp() {
        if (!this.#activeKey) {
            return;
        }

        const key = this.#activeKey;
        this.#activeKey = null;
        this.#detachDragListeners();
        this.#report(key);
    }

    #onKeyDown(event) {
        if (!this.#resizable) {
            return;
        }

        const key = this.#findHandleKey(event.target);
        if (!key) {
            return;
        }

        const rightToLeft = this.#isRightToLeft();
        let step = 0;
        if (event.key === "ArrowLeft") {
            step = rightToLeft ? keyboardStep : -keyboardStep;
        } else if (event.key === "ArrowRight") {
            step = rightToLeft ? -keyboardStep : keyboardStep;
        } else {
            return;
        }

        event.preventDefault();
        this.#resizeTo(key, this.#measure(key) + step);
        this.#report(key);
    }

    #resizeTo(key, width) {
        const column = this.#findColumn(key);
        if (!column) {
            return;
        }

        const clamped = Math.max(minimumColumnWidth, Math.round(width));
        column.dataset.bzsResized = "true";
        column.style.setProperty("width", `${clamped}px`);
    }

    #report(key) {
        const column = this.#findColumn(key);
        if (!column || !this.#reference) {
            return;
        }

        const width = parseFloat(column.style.getPropertyValue("width"));
        if (!Number.isFinite(width) || width <= 0) {
            return;
        }

        this.#reference.invokeMethodAsync("ReportColumnResizedAsync", key, width).catch(() => {
            // A disconnected circuit must not break browser-side resizing.
        });
    }

    #detachDragListeners() {
        const document = this.#table.ownerDocument;
        document.removeEventListener("pointermove", this.#handlePointerMove);
        document.removeEventListener("pointerup", this.#handlePointerUp);
        document.removeEventListener("pointercancel", this.#handlePointerUp);
    }
}
