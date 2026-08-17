export function synchronize(input, checked, indeterminate) {
    if (!input) {
        return;
    }

    input.checked = checked;
    input.indeterminate = indeterminate;
}
