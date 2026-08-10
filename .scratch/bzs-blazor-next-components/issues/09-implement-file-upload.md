# Implement FileUpload

Type: task
Status: resolved
Blocked by: 01

## Goal

Deliver an EditForm-integrated `BzsFileUpload` that owns file selection and
validation while leaving transport and storage to the consumer.

## Scope

- Wrap native Blazor file selection with controlled selected-file metadata.
- Support single/multiple selection, accept filters, maximum count, per-file
  size, total size, disabled state, clear/remove commands, and templates.
- Expose progress presentation driven by consumer-owned values.
- Provide localized validation and accessible drag/drop enhancement only if it
  does not weaken the native selection path.

## Acceptance Criteria

- The library never buffers full file contents merely to display metadata.
- Re-selection and removal produce deterministic callbacks and EditContext
  notifications.
- Browser restrictions and validation failures are surfaced clearly.
- Static SSR renders a meaningful native file input.

## Verification

- bUnit tests for validation, callbacks, field state, attributes, and templates.
- Browser tests with small fixture files across supported interactive modes.
- Package-consumer test proves any collocated upload asset is shipped.

## Out Of Scope

- HTTP upload, authentication, cloud storage, chunking, resume, retry, virus
  scanning, and server endpoint implementation.

## Comments

- 2026-08-08: Implemented EditForm-integrated `BzsFileUpload` with native
  selection, controlled metadata, deterministic selection/removal/clear
  callbacks, count and size validation, templates, consumer-owned progress,
  localized English and Simplified Chinese defaults, and no file buffering.
  Partial removal rebuilds the native picker so stale browser files cannot
  survive. Ten focused tests pass; real picker and package-consumer evidence
  remains centralized in ticket 12.
