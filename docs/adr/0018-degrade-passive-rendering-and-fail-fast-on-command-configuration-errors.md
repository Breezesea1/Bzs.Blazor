# Degrade passive rendering and fail fast on command configuration errors

Components without a theme provider use deterministic default light tokens, and static SSR emits meaningful inert HTML without browser calls. Missing or duplicate overlay hosts, invalid parameters, and other command-path configuration errors fail with actionable exceptions rather than silently dropping work, while temporary JavaScript or circuit unavailability may skip focus and scroll enhancements but must not lose completion results, callbacks, or cleanup.
