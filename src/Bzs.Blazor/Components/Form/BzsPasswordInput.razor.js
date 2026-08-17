const selections = new WeakMap();

export function captureSelection(input) {
    if (!input) {
        return;
    }

    selections.set(input, {
        end: input.selectionEnd,
        focused: document.activeElement === input,
        start: input.selectionStart,
    });
}

export function restoreFocusAndSelection(input) {
    if (!input) {
        return;
    }

    const selection = selections.get(input);
    selections.delete(input);
    if (!selection?.focused) {
        return;
    }

    requestAnimationFrame(() => {
        input.focus({ preventScroll: true });
        if (selection.start !== null && selection.end !== null) {
            input.setSelectionRange(selection.start, selection.end);
        }
    });
}
