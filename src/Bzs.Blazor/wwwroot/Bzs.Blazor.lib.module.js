import { startStaticResizeControllers } from './Components/Layout/BzsNavigationDrawer.razor.js';

let compositionEventRegistered = false;

function registerCompositionEvent(blazor) {
    if (compositionEventRegistered) {
        return;
    }

    blazor.registerCustomEventType("bzscompositionend", {
        browserEventName: "compositionend",
        createEventArgs: event => ({ value: event.target?.value ?? null }),
    });
    compositionEventRegistered = true;
}

export function afterStarted(blazor) {
    registerCompositionEvent(blazor);
    startStaticResizeControllers();
}

export function afterWebStarted(blazor) {
    registerCompositionEvent(blazor);
    startStaticResizeControllers();
}
