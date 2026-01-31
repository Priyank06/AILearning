/**
 * Browser-based encrypted SQLite persistence for LegacyAnalyzer.
 * Uses AES-GCM only, CryptoEnvelope format, IndexedDB (key envelope + DB in same store).
 * Keys never leave this module; .NET sees only opaque handles/results.
 */
(function (global) {
    'use strict';

    const DB_NAME = 'LegacyAnalyzerSecure';
    const DB_VERSION = 1;
    const STORE_KEY_SEED = 'keySeed';
    const STORE_ENVELOPE = 'dbEnvelope';
    const KDF_ITERATIONS = 300000;
    const AES_KEY_LENGTH = 256;
    const SALT_LENGTH = 16;
    const IV_LENGTH = 12;
    const SQLJS_CDN = 'https://cdn.jsdelivr.net/npm/sql.js@1.10.2/dist/sql-wasm.js';

    let db = null;           // in-memory SQLite
    let SQL = null;          // sql.js namespace
    let keyState = 'locked'; // locked | unlocked | expired
    let keyMaterial = null;  // CryptoKey for AES-GCM (held in JS only)

    function idb() {
        return new Promise((resolve, reject) => {
            const req = indexedDB.open(DB_NAME, DB_VERSION);
            req.onerror = () => reject(req.error);
            req.onsuccess = () => resolve(req.result);
            req.onupgradeneeded = (e) => {
                const store = e.target.result;
                if (!store.objectStoreNames.contains(STORE_KEY_SEED))
                    store.createObjectStore(STORE_KEY_SEED);
                if (!store.objectStoreNames.contains(STORE_ENVELOPE))
                    store.createObjectStore(STORE_ENVELOPE);
            };
        });
    }

    function getStore(db, name, mode = 'readwrite') {
        return db.transaction(name, mode).objectStore(name);
    }

    function idbGet(store, key) {
        return new Promise((resolve, reject) => {
            const req = store.get(key);
            req.onerror = () => reject(req.error);
            req.onsuccess = () => resolve(req.result);
        });
    }

    function idbPut(store, key, value) {
        return new Promise((resolve, reject) => {
            const req = store.put(value, key);
            req.onerror = () => reject(req.error);
            req.onsuccess = () => resolve();
        });
    }

    async function loadSqlJs() {
        if (typeof global.initSqlJs !== 'undefined') {
            return global.initSqlJs({
                locateFile: file => 'https://cdn.jsdelivr.net/npm/sql.js@1.10.2/dist/' + file
            });
        }
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = SQLJS_CDN;
            script.onload = () => {
                global.initSqlJs({
                    locateFile: file => 'https://cdn.jsdelivr.net/npm/sql.js@1.10.2/dist/' + file
                }).then(resolve).catch(reject);
            };
            script.onerror = () => reject(new Error('Failed to load sql.js'));
            document.head.appendChild(script);
        });
    }

    function ensureDb() {
        if (!db && SQL) {
            db = new SQL.Database();
            createSchema();
        }
        return db;
    }

    function createSchema() {
        if (!db) return;
        db.run(`
            CREATE TABLE IF NOT EXISTS AnalysisSession (
                Id TEXT PRIMARY KEY,
                CreatedAt TEXT NOT NULL,
                AnalysisType TEXT NOT NULL,
                FileCount INTEGER NOT NULL,
                OverallComplexityScore INTEGER NOT NULL,
                OverallRiskLevel TEXT NOT NULL,
                SchemaVersion INTEGER NOT NULL DEFAULT 1,
                UserFriendlyName TEXT,
                SeverityLevel TEXT,
                ResultJson TEXT NOT NULL,
                BusinessMetricsJson TEXT
            )
        `);
        db.run(`
            CREATE TABLE IF NOT EXISTS AnalysisFileResult (
                SessionId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                ComplexityScore INTEGER NOT NULL,
                Status TEXT NOT NULL,
                Hash TEXT,
                Ignored INTEGER NOT NULL DEFAULT 0,
                Suppressed INTEGER NOT NULL DEFAULT 0,
                ResultJson TEXT NOT NULL,
                PRIMARY KEY (SessionId, FileName),
                FOREIGN KEY (SessionId) REFERENCES AnalysisSession(Id)
            )
        `);
        db.run(`
            CREATE TABLE IF NOT EXISTS UserPreference (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )
        `);
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
    }

    function zeroizeKey() {
        keyMaterial = null;
        keyState = 'expired';
    }

    async function getOrCreateKeySeed(idbDb) {
        const store = getStore(idbDb, STORE_KEY_SEED);
        let seed = await idbGet(store, 'seed');
        if (!seed) {
            seed = crypto.getRandomValues(new Uint8Array(32));
            await idbPut(store, 'seed', seed);
        }
        return seed;
    }

    async function deriveKey(keySeed, salt) {
        const baseKey = await crypto.subtle.importKey(
            'raw',
            keySeed,
            { name: 'PBKDF2' },
            false,
            ['deriveBits', 'deriveKey']
        );
        return await crypto.subtle.deriveKey(
            {
                name: 'PBKDF2',
                salt: salt,
                iterations: KDF_ITERATIONS,
                hash: 'SHA-256'
            },
            baseKey,
            { name: 'AES-GCM', length: AES_KEY_LENGTH },
            false,
            ['encrypt', 'decrypt']
        );
    }

    async function encrypt(plaintext, key) {
        const iv = crypto.getRandomValues(new Uint8Array(IV_LENGTH));
        const enc = await crypto.subtle.encrypt(
            { name: 'AES-GCM', iv: iv, tagLength: 128 },
            key,
            plaintext
        );
        return { iv, ciphertext: new Uint8Array(enc) };
    }

    async function decrypt(iv, ciphertext, key) {
        return await crypto.subtle.decrypt(
            { name: 'AES-GCM', iv: iv, tagLength: 128 },
            key,
            ciphertext
        );
    }

    function b64Encode(u8) {
        let binary = '';
        for (let i = 0; i < u8.length; i++) binary += String.fromCharCode(u8[i]);
        return btoa(binary);
    }

    function b64Decode(str) {
        const binary = atob(str);
        const u8 = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) u8[i] = binary.charCodeAt(i);
        return u8;
    }

    async function unlock() {
        if (keyState === 'unlocked' && keyMaterial) return;
        const idbDb = await idb();
        const keySeed = await getOrCreateKeySeed(idbDb);
        const envelopeStore = getStore(idbDb, STORE_ENVELOPE, 'readonly');
        const envelopeJson = await idbGet(envelopeStore, 'main');
        let salt;
        if (envelopeJson) {
            const env = JSON.parse(envelopeJson);
            salt = b64Decode(env.kdf.salt);
        } else {
            salt = crypto.getRandomValues(new Uint8Array(SALT_LENGTH));
        }
        keyMaterial = await deriveKey(keySeed, salt);
        keyState = 'unlocked';
    }

    async function init() {
        if (SQL && db) return true;
        try {
            SQL = await loadSqlJs();
            ensureDb();
            return true;
        } catch (e) {
            console.error('browserStorage init failed', e);
            return false;
        }
    }

    async function isAvailable() {
        try {
            if (typeof indexedDB === 'undefined' || !crypto.subtle) return false;
            await idb();
            return await init();
        } catch {
            return false;
        }
    }

    async function initializeSchema() {
        const ok = await init();
        if (!ok) throw new Error('Browser storage not available');
        ensureDb();
    }

    function toPositionalParams(params) {
        if (params == null) return [];
        if (Array.isArray(params)) return params;
        if (typeof params === 'object') return Object.values(params);
        return [params];
    }

    async function executeSql(sql, params) {
        await init();
        const database = ensureDb();
        if (!database) throw new Error('DB not initialized');
        const arr = toPositionalParams(params);
        if (arr.length) {
            database.run(sql, arr);
        } else {
            database.run(sql);
        }
    }

    async function query(sql, params) {
        await init();
        const database = ensureDb();
        if (!database) return [];
        const arr = toPositionalParams(params);
        let result;
        if (arr.length) {
            const stmt = database.prepare(sql);
            const columns = stmt.getColumnNames();
            const rows = [];
            stmt.bind(arr);
            while (stmt.step()) {
                const row = stmt.getAsObject();
                rows.push(row);
            }
            stmt.free();
            return rows;
        }
        result = database.exec(sql);
        if (!result.length) return [];
        const columns = result[0].columns;
        return result[0].values.map(row => {
            const obj = {};
            columns.forEach((col, i) => obj[col] = row[i]);
            return obj;
        });
    }

    async function saveDb() {
        await init();
        const database = ensureDb();
        if (!database) throw new Error('DB not initialized');
        await unlock();
        const binary = database.export();
        const plaintext = new Uint8Array(binary);
        const salt = crypto.getRandomValues(new Uint8Array(SALT_LENGTH));
        const idbDb = await idb();
        const keySeed = await getOrCreateKeySeed(idbDb);
        const key = await deriveKey(keySeed, salt);
        const { iv, ciphertext } = await encrypt(plaintext, key);
        const envelope = {
            version: 1,
            kdf: { name: 'PBKDF2', salt: b64Encode(salt), iterations: KDF_ITERATIONS },
            cipher: { name: 'AES-GCM', iv: b64Encode(iv) },
            payload: b64Encode(ciphertext)
        };
        await idbPut(getStore(idbDb, STORE_ENVELOPE), 'main', JSON.stringify(envelope));
    }

    async function loadDb() {
        await init();
        const idbDb = await idb();
        const store = getStore(idbDb, STORE_ENVELOPE, 'readonly');
        const envelopeJson = await idbGet(store, 'main');
        if (!envelopeJson) return;
        await unlock();
        const env = JSON.parse(envelopeJson);
        const salt = b64Decode(env.kdf.salt);
        const iv = b64Decode(env.cipher.iv);
        const ciphertext = b64Decode(env.payload);
        const key = await deriveKey(await getOrCreateKeySeed(idbDb), salt);
        const plaintext = await decrypt(iv, ciphertext, key);
        if (db) db.close();
        db = new SQL.Database(new Uint8Array(plaintext));
        keyState = 'unlocked';
    }

    async function exportBundle() {
        await init();
        const database = ensureDb();
        if (!database) throw new Error('DB not initialized');
        await unlock();
        const idbDb = await idb();
        const envelopeJson = await idbGet(getStore(idbDb, STORE_ENVELOPE, 'readonly'), 'main');
        if (!envelopeJson) throw new Error('No database to export');
        const env = JSON.parse(envelopeJson);
        const payloadBytes = b64Decode(env.payload);
        const metadata = {
            schemaVersion: 1,
            exportTime: new Date().toISOString(),
            app: 'LegacyAnalyzer'
        };
        return {
            encryptedDbPayload: Array.from(payloadBytes),
            keyEnvelopeJson: JSON.stringify({
                version: env.version,
                kdf: env.kdf,
                cipher: env.cipher
            }),
            metadataJson: JSON.stringify(metadata)
        };
    }

    async function importBundle(bundle) {
        await init();
        const env = JSON.parse(bundle.keyEnvelopeJson);
        const salt = b64Decode(env.kdf.salt);
        const idbDb = await idb();
        const keySeed = await getOrCreateKeySeed(idbDb);
        const key = await deriveKey(keySeed, salt);
        const iv = b64Decode(env.cipher.iv);
        const payload = bundle.encryptedDbPayload;
        const ciphertext = Array.isArray(payload) ? new Uint8Array(payload) : new Uint8Array(payload);
        const plaintext = await decrypt(iv, ciphertext, key);
        if (db) db.close();
        db = new SQL.Database(new Uint8Array(plaintext));
        const envelope = {
            version: env.version,
            kdf: env.kdf,
            cipher: env.cipher,
            payload: Array.isArray(payload) ? btoa(String.fromCharCode.apply(null, payload)) : b64Encode(new Uint8Array(payload))
        };
        await idbPut(getStore(idbDb, STORE_ENVELOPE), 'main', JSON.stringify(envelope));
    }

    global.legacyAnalyzerStorage = {
        init,
        isAvailable,
        initializeSchema,
        executeSql,
        query,
        saveDb,
        loadDb,
        exportBundle,
        importBundle,
        zeroizeKey
    };
})(typeof window !== 'undefined' ? window : self);
