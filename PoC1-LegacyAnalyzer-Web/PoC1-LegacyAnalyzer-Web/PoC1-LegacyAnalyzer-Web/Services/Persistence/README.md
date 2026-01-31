# Client-Side Persistence (Browser SQLite + Encryption)

This folder contains the browser-hosted, encrypted persistence layer: analysis sessions and user preferences are stored in a client-side SQLite database, encrypted with AES-GCM. No server database is required by default.

## Manual Validation Checklist

Use this checklist to validate client-side persistence, encryption, and fallbacks:

1. **Enable persistence**  
   Ensure `ClientPersistence:Enabled` is `true` in `appsettings.json` (or environment). `ClientCrypto` and `ClientPersistence` sections must be present.

2. **Run a full analysis**  
   - Open the Multi-File Analysis page (`/multifile`).  
   - Upload one or more files and run an analysis.  
   - Wait for completion.

3. **Reload the page**  
   - Refresh the browser (F5) or close and reopen the tab.  
   - **Confirm**: The "Recent analyses" section appears and lists the last run (and optionally others).  
   - **Confirm**: Selecting "Load" on a recent session restores that analysis (no server DB required).

4. **Preferences**  
   - Change analysis type or other preferences; run another analysis.  
   - Reload the page.  
   - **Confirm**: Preferences (e.g. last analysis type) are restored from browser storage.

5. **Encryption in DevTools**  
   - Open browser DevTools → Application (or Storage) → IndexedDB.  
   - **Confirm**: Stored values are opaque (encrypted blobs). No plaintext analysis content or keys visible.

6. **Graceful degradation**  
   - Set `ClientPersistence:Enabled` to `false` and restart the app.  
   - **Confirm**: Analysis still works; "Recent analyses" does not appear; no errors in console.  
   - In a browser with IndexedDB/Web Crypto disabled (or private mode with strict storage), **confirm**: App continues to work; persistence is skipped without crashing.

## Architecture Summary

- **Blazor UI** calls `IBrowserStorageService` for save/load of sessions and preferences.  
- **Repositories** (`IBrowserAnalysisRepository`, `IBrowserPreferencesRepository`) map domain models to/from the encrypted store.  
- **SecureClientInterop** talks to the JS module (`wwwroot/js/browserStorage.js`) for SQLite and crypto.  
- **Keys** stay in JavaScript (Web Crypto); .NET never sees key material.  
- **CryptoEnvelope** wraps all persisted blobs (version, KDF, cipher, payload) for algorithm upgrades.
