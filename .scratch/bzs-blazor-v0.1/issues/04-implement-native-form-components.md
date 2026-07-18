# Implement native form components

Type: task  
Status: resolved  
Blocked by: 01, 02, 03

## Goal

Implement the v0.1 form family on Blazor's native form contracts so consumers can build validated, culture-aware forms in static and interactive modes.

## Scope

- Implement the shared input base and field shell.
- Implement text input, textarea, generic number input, generic date input, checkbox, and generic select.
- Connect labels, descriptions, required indication, and validation messages accessibly.
- Support `EditForm`, `EditContext`, DataAnnotations, field CSS state, and standard value expressions.
- Use controlled values and standard `Value`/`ValueChanged` behavior.
- Parse and format number/date values with the current culture.
- Support static SSR form names and emitted values where Blazor permits.
- Keep select options strongly typed and collection inputs read-only.
- Expose semantic sizes/density without leaking raw theme values.

## Acceptance Criteria

- Every input works inside an `EditForm` and reports field changes correctly.
- Invalid values produce stable validation behavior rather than silent coercion.
- English and Simplified Chinese validation/accessibility text can be localized.
- RTL layout and logical field spacing work.
- Inputs are usable by keyboard, at 200% zoom, and in forced-colors mode.
- Static SSR markup includes the required name/value semantics.

## Testing

- bUnit tests for binding, parsing, formatting, validation, disabled/read-only states, labels, descriptions, errors, culture changes, and additional attributes.
- Demo forms for valid, invalid, disabled, compact, comfortable, Light, Dark, English, Chinese, and RTL states.
- Browser keyboard and zoom checks.

## Out of Scope

- MultiSelect, autocomplete, file upload, and asynchronous business validation services.

## Comments

- 2026-07-18: Completed native EditContext-integrated text, textarea, checkbox, number, date, and strongly typed select controls; accessible field relationships; localized parse errors; static form name/value semantics; 14 focused unit tests; and Forms Demo/browser keyboard validation coverage. Nullable generic number/date values remain outside the initial component contract.
