# ADR-004: Browser-Side Encrypted SQLite for Session Persistence

## Status
Retrospective

## Context
The application is a PoC with no dedicated back-end database. Analysis sessions, agent interaction history, and user preferences need to survive page refreshes without requiring a server-side data store. Storing raw analysis results in `localStorage` raises privacy concerns as results may include sensitive code snippets.

## Decision
Use **sql.js** (SQLite compiled to WebAssembly) running in the browser, accessed via Blazor JS interop (`SecureClientInterop`), with all data encrypted using **AES-GCM** and a per-browser key generated and stored in the browser's own key material. Repository classes (`BrowserAnalysisRepository`, `BrowserAgentSessionRepository`, `BrowserPreferencesRepository`) wrap the interop layer.

## Consequences
### Positive
- No server-side database required — simplifies deployment and removes a dependency.
- AES-GCM encryption protects sensitive code content stored in the browser.
- SQLite query model provides structured access without the limitations of raw `localStorage`.
- Data is scoped to the browser — no cross-device sync concerns for a PoC.
### Negative
- sql.js WASM adds several hundred KB to the initial page load.
- Encrypted data is lost if the user clears browser storage or switches browsers/devices.
- JS interop round-trips for every database query add latency compared to a native store.
- Not suitable for production systems where analysis history needs to be shared or audited centrally.
