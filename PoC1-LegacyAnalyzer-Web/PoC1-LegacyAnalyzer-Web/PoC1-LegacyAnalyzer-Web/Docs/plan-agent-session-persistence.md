# Plan: Multi-Agent Session Persistence (Browser SQLite + Encryption)

## Goals

- **Same as multi-file persistence**: Save multi-agent analysis sessions in the same browser-hosted, encrypted SQLite store used for multi-file analysis.
- **History and load**: Users can see recent agent sessions and load a past session to restore `TeamAnalysisResult` and related context (business objective, selected agents).
- **No new crypto or storage backend**: Reuse existing IndexedDB, CryptoEnvelope, AES-GCM, and `ISecureClientInterop`. Same key lifecycle; keys never cross JS→.NET.
- **SOLID**: Add a dedicated repository and domain model for agent sessions; keep page logic to orchestration only.

---

## Scope

| In scope | Out of scope |
|----------|--------------|
| Persist `TeamAnalysisResult` after multi-agent run | Changing crypto or key strategy |
| List recent agent sessions; load by id | Server-side DB for agents |
| Same encrypted DB (new table only) | Persisting raw file contents used in agent run |
| Feature flag `ClientPersistence:Enabled` gates agent persistence too | New JS crypto or KDF |

---

## High-Level Architecture

- **Existing**: Multi-file sessions → `IBrowserStorageService` → `IBrowserAnalysisRepository` → `ISecureClientInterop` → `browserStorage.js` (AnalysisSession + AnalysisFileResult + UserPreference).
- **New**: Agent sessions → same `IBrowserStorageService` (extended) → `IBrowserAgentSessionRepository` → same `ISecureClientInterop` → same JS module (new table `AgentSession`).

No new JS crypto or interop methods: reuse `ExecuteSqlAsync`, `QueryAsync`, `LoadDatabaseAsync`, `SaveDatabaseAsync`, and the same encrypted DB blob.

---

## SQLite Schema (Client-Side)

Add one table in the **same** in-memory SQLite DB (created in `createSchema()` in `wwwroot/js/browserStorage.js`):

### AgentSession

| Column | Type | Description |
|--------|------|-------------|
| Id | TEXT PK | GUID (e.g. `Guid.NewGuid().ToString("N")`) |
| CreatedAt | TEXT NOT NULL | ISO 8601 (e.g. `DateTime.UtcNow.ToString("O")`) |
| BusinessObjective | TEXT NOT NULL | e.g. "security", "performance", or custom label |
| CustomObjective | TEXT | User-typed objective when not a preset |
| AgentSpecialtiesJson | TEXT | JSON array of selected agent names (e.g. `["security","performance"]`) |
| UserFriendlyName | TEXT | Optional label for history list |
| SchemaVersion | INTEGER NOT NULL | Schema version at save time (e.g. 1) |
| ResultJson | TEXT NOT NULL | Full serialized `TeamAnalysisResult` (JSON) |

- **Single row per session.** No child table (unlike AnalysisFileResult); one blob per session is enough for agent result size.
- **Migration**: Add table with `CREATE TABLE IF NOT EXISTS AgentSession (...)` so existing DBs get the table on next load; no data migration needed.

---

## .NET Abstractions

### 1. Model: SavedAgentSession

New class (e.g. in `Services/Persistence` or `Models`) used for history and load:

- `Id` (string)
- `CreatedAt` (DateTime)
- `BusinessObjective` (string)
- `CustomObjective` (string?)
- `SelectedAgents` (IReadOnlyList&lt;string&gt; or string[] — parsed from AgentSpecialtiesJson)
- `UserFriendlyName` (string?)
- `SchemaVersion` (int)
- `TeamResult` (TeamAnalysisResult) — deserialized from ResultJson

### 2. Interface: IBrowserAgentSessionRepository

New interface in `Services/Persistence`:

- `Task SaveAgentSessionAsync(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default)`
- `Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default)`
- `Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)`
- `Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default)`

Hides SQLite and serialization; callers work with `TeamAnalysisResult` and `SavedAgentSession`.

### 3. Implementation: BrowserAgentSessionRepository

- Depends on `ISecureClientInterop` and `ILogger<BrowserAgentSessionRepository>`.
- Serialize `TeamAnalysisResult` (and optional metadata) to JSON with the same `JsonSerializerOptions` style as `BrowserAnalysisRepository` (camelCase, etc.).
- **Save**: `InitializeSchemaAsync` → `LoadDatabaseAsync` → `ExecuteSqlAsync` (INSERT into AgentSession) → `SaveDatabaseAsync`.
- **Get recent**: `InitializeSchemaAsync` → `LoadDatabaseAsync` → `QueryAsync<AgentSessionRow>` (SELECT … ORDER BY CreatedAt DESC LIMIT ?) → map rows to `SavedAgentSession` (deserialize ResultJson to `TeamAnalysisResult`).
- **Get by id**: Same init/load, then SELECT by Id, map single row to `SavedAgentSession`.
- **Delete**: Same init/load, then `ExecuteSqlAsync` (DELETE FROM AgentSession WHERE Id = ?), then `SaveDatabaseAsync`.
- Use parameterized SQL only; same error handling and logging style as `BrowserAnalysisRepository`.

### 4. Extend IBrowserStorageService and BrowserStorageService

Add to existing `IBrowserStorageService`:

- `Task SaveAgentSessionAsync(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default)`
- `Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default)`
- `Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)`
- `Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default)`

In `BrowserStorageService`:

- Inject `IBrowserAgentSessionRepository`.
- New methods: if `!_config.Enabled` return no-op / empty / null; otherwise delegate to `_agentSessionRepository`.
- Keeps one entry point for “browser storage” for both multi-file and multi-agent.

### 5. DI Registration

- Register `IBrowserAgentSessionRepository` → `BrowserAgentSessionRepository` (same lifetime as other persistence services, e.g. scoped).
- Register only when `ClientPersistence:Enabled` is true (same as existing persistence services), or always register and let `BrowserStorageService` no-op when disabled—consistent with current multi-file behavior.

---

## JavaScript Changes

### wwwroot/js/browserStorage.js

- In `createSchema()`, add:

```js
db.run(`
    CREATE TABLE IF NOT EXISTS AgentSession (
        Id TEXT PRIMARY KEY,
        CreatedAt TEXT NOT NULL,
        BusinessObjective TEXT NOT NULL,
        CustomObjective TEXT,
        AgentSpecialtiesJson TEXT NOT NULL,
        UserFriendlyName TEXT,
        SchemaVersion INTEGER NOT NULL DEFAULT 1,
        ResultJson TEXT NOT NULL
    )
`);
```

- No new public API: existing `executeSql`, `query`, `saveDb`, `loadDb` and schema init are sufficient. Same encrypted DB blob contains both existing tables and `AgentSession`.

---

## Blazor Integration: MultiAgentOrchestration.razor

### Dependencies

- Inject `IBrowserStorageService` (already used for multi-file elsewhere; same service extended for agent sessions).

### After analysis completes

- In the handler that calls `CoordinateTeamAnalysisAsync` (e.g. `HandleAnalysisStarted`), after `teamResult = await orchestrator.CoordinateTeamAnalysisAsync(...)` and success:
  - Check persistence is available (e.g. `await BrowserStorageService.IsAvailableAsync()` or a cached `agentStorageAvailable` flag).
  - If available, call `await BrowserStorageService.SaveAgentSessionAsync(teamResult, lastBusinessObjective, lastCustomObjective, lastSelectedAgents, userFriendlyName: null)` (and optionally refresh recent list).
  - Wrap in try/catch; on failure log and continue (do not block UI or report failure to user unless desired).

### Load recent list on load

- In `OnInitializedAsync` (or equivalent), if persistence is enabled, call `GetRecentAgentSessionsAsync(20)` and store in a field (e.g. `recentAgentSessions`).
- Set a flag (e.g. `agentStorageAvailable`) from `IsAvailableAsync()` so the “Recent agent analyses” block only shows when storage is available.

### UI: Recent agent analyses

- Add a card/section “Recent agent analyses” (only when `agentStorageAvailable && recentAgentSessions.Any()`).
- List entries (e.g. “CreatedAt — BusinessObjective, N agents” or UserFriendlyName).
- Per entry: button “Load” that calls `LoadAgentSession(session.Id)`.

### Load session

- `LoadAgentSession(string sessionId)`: call `GetAgentSessionByIdAsync(sessionId)`. If non-null, set `teamResult = session.TeamResult`, and restore `lastBusinessObjective`, `lastCustomObjective`, `lastSelectedAgents` from the saved session so the UI and report download stay consistent. Optionally set `projectSummary` if you store it (or leave null). Call `StateHasChanged()`.

### Optional

- “Refresh” button to re-fetch `GetRecentAgentSessionsAsync` and update the list.
- Optional `UserFriendlyName` when saving (e.g. from a small prompt or inline input); otherwise use CreatedAt or a default label in the list.

---

## Configuration and Feature Flags

- **No new config sections.** Reuse `ClientPersistence:Enabled`. When it is true, multi-file and multi-agent persistence are both enabled. When false, both are no-op.
- Optional: later add `ClientPersistence:MaxAgentSessionsToKeep` and purge logic (e.g. delete oldest by CreatedAt when count &gt; N); not required for initial implementation.

---

## SOLID and Complexity

- **Single responsibility**: `BrowserAgentSessionRepository` only maps agent sessions to/from SQLite; `BrowserStorageService` delegates; page only orchestrates and displays.
- **Open/closed**: Existing multi-file flow unchanged; agent persistence is an extension via new interface and table.
- **Dependency inversion**: Page and service depend on `IBrowserStorageService` and `IBrowserAgentSessionRepository` (abstractions), not concrete storage.

Keep `MultiAgentOrchestration.razor` to: start analysis, save session on success, load recent list, load by id, and render; avoid putting serialization or SQL logic in the page.

---

## Testing and Validation

- **Manual**: Enable `ClientPersistence:Enabled`, run a multi-agent analysis, reload page → “Recent agent analyses” shows the run; click “Load” → same result and context restored.
- **Manual**: In DevTools → IndexedDB → same DB/store as multi-file; after save, confirm DB blob is updated (opaque encrypted); no plaintext agent result in storage.
- **Unit** (optional): Fake `ISecureClientInterop` and test `BrowserAgentSessionRepository` Save + GetRecent + GetById round-trip with a minimal `TeamAnalysisResult`.

---

## Summary Checklist

| Item | Action |
|------|--------|
| Schema | Add `AgentSession` table in `browserStorage.js` `createSchema()` |
| Model | Add `SavedAgentSession` (Id, CreatedAt, BusinessObjective, CustomObjective, SelectedAgents, UserFriendlyName, TeamResult) |
| Repository | Add `IBrowserAgentSessionRepository` and `BrowserAgentSessionRepository` |
| Service | Extend `IBrowserStorageService` and `BrowserStorageService` with agent session methods |
| DI | Register `IBrowserAgentSessionRepository` → `BrowserAgentSessionRepository` |
| Page | In `MultiAgentOrchestration.razor`: save after run, load recent on init, UI “Recent agent analyses” + Load by id |

This plan reuses the existing browser SQLite + encryption implementation and adds agent session persistence in the same way multi-file analysis sessions are saved and loaded.
