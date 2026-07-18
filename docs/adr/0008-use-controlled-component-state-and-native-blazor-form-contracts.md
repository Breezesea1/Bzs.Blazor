# Use controlled component state and native Blazor form contracts

Public state flows down through parameters and changes flow up through `EventCallback<T>` using standard bindable pairs such as `Value`/`ValueChanged` and `Open`/`OpenChanged`; components never mutate their parameters. Form controls derive from a Bzs input base built on Blazor `InputBase<TValue>` so `EditForm`, `EditContext`, validation, and static SSR form output remain native, while only transient visual interaction state stays internal.
