# ApiX.Security

Cryptographic utilities and DI-friendly abstractions for ApiX.

Currently supports:

- **Password Hashing** (PBKDF2-SHA256, Argon2id, versioned APX strings)
- **Encryption** (AES-GCM authenticated encryption with versioned envelopes, optional legacy CBC fallback)

---

## Password Hashing

Password hashing utilities and abstractions for user authentication.  
Supports **PBKDF2-HMAC-SHA256** and **Argon2id** with a **versioned, self-describing storage format**, configurable defaults, and **rehash-on-verify** signaling.

### Features
- ✅ PBKDF2 (SHA-256) and Argon2id implementations
- ✅ Versioned, self-describing hash strings (`APX$…`) for forward-compatible migrations
- ✅ `IPasswordHasher` interface + `IPasswordHasherFactory` for A/B switching via config
- ✅ `needsRehash` signal on verify to transparently upgrade stored hashes
- ✅ Constant-time comparisons via `CryptographicOperations.FixedTimeEquals`
- ✅ Sensible defaults + configurable via `IOptions<PasswordHashingOptions>`

---

### Install
Add to **ApiX.Security** project:

```xml
<ItemGroup>
  <!-- Only if you use Argon2id -->
  <PackageReference Include="Konscious.Security.Cryptography.Argon2" Version="1.3.0" />
</ItemGroup>
```

> PBKDF2 uses the BCL (`Rfc2898DeriveBytes`) – no extra package needed.

---

### Storage format (APX)
All hashes are stored as self-describing strings. This allows you to change parameters (or algorithms) later and still verify old hashes.

#### PBKDF2 (version 1)
```
APX$1$PBKDF2-SHA256$iter=<n>$s=<base64>$h=<base64>
```
- `iter` – iteration count
- `s` / `h` – salt / hash, Base64-encoded

#### Argon2id (version 2)
```
APX$2$ARGON2ID$m=<KB>,t=<iters>,p=<lanes>$sl=<saltLen>,hl=<hashLen>$s=<base64>$h=<base64>
```
- `m` – memory in KiB
- `t` – time cost (iterations)
- `p` – degree of parallelism (lanes)
- `sl` / `hl` – salt length / hash length (bytes)

---

### Defaults (tune per hardware)
- **PBKDF2**: `iterations = 200,000`, `salt = 16`, `hash = 32` (target ~100 ms on server)
- **Argon2id**: `m = 64 MiB`, `t = 3`, `p = 2`, `salt = 16`, `hash = 32` (target ~100–250 ms)

> **Recommendation:** Benchmark on prod-like hardware and adjust to your latency budget.

---

### Configuration
`PasswordHashingOptions` (bound from config) controls the default algorithm and parameters.

**appsettings.json**
```json
{
  "Security": {
    "PasswordHashing": {
      "Algorithm": "Argon2id", // or "Pbkdf2Sha256"
      "Pbkdf2": {
        "Iterations": 200000,
        "SaltSize": 16,
        "HashSize": 32
      },
      "Argon2id": {
        "MemoryKb": 65536,
        "Iterations": 3,
        "Parallelism": 2,
        "SaltSize": 16,
        "HashSize": 32
      }
    }
  }
}
```

**Registration**
```csharp
// Program.cs / composition root
builder.Services.AddPasswordHashing(builder.Configuration);
```

This registers:
- `IPasswordHasher` – the **current default** implementation per config
- `IPasswordHasherFactory` – to detect algorithms and manually obtain specific hashers
- Concrete implementations: `Pbkdf2PasswordHasher`, `Argon2idPasswordHasher`

---

### API Overview
```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string stored, out bool needsRehash);
}

public interface IPasswordHasherFactory
{
    IPasswordHasher Create(); // current default per config
    bool TryDetect(string stored, out PasswordAlgorithm alg);
}
```

---

### Quick start

#### Create a hash
```csharp
public class AuthService(IPasswordHasher hasher)
{
    public string CreateHash(string password) => hasher.Hash(password);
}
```

#### Verify (with auto-upgrade)
```csharp
public class AuthService(IPasswordHasher hasher)
{
    public bool VerifyAndMaybeUpgrade(string password, ref string storedHash)
    {
        var ok = hasher.Verify(password, storedHash, out var needsRehash);
        if (ok && needsRehash)
        {
            // Rehash under current policy and persist
            storedHash = hasher.Hash(password);
            // save storedHash to your user record here
        }
        return ok;
    }
}
```

#### Detect algorithm (optional)
```csharp
public class DiagnosticsService(IPasswordHasherFactory factory)
{
    public string Describe(string stored)
        => factory.TryDetect(stored, out var alg) ? alg.ToString() : "Unknown";
}
```

---

### Using the static helpers directly (optional)
If you prefer low-level calls, you can use the static helpers that produce/verify the APX strings:

- **PBKDF2**: `PasswordHasher.HashPassword(string, int? iterations)` / `VerifyPassword(string, string)`
- **Argon2id**: `PasswordHasherArgon2id.HashPassword(...)` / `VerifyPassword(string, string)`

> Prefer the DI abstractions for application code; use helpers for scripting, tooling, or tests.

---

### Rehash policy
`Verify` sets `needsRehash = true` when any of the following are true:
- The stored algorithm is **different** from the configured default
- The stored parameters are **weaker** than configured (e.g., fewer iterations, less memory)
- Salt/hash lengths differ from current policy

Your app can then transparently upgrade the hash on the next successful login.

---

### Security notes
- Comparisons use `CryptographicOperations.FixedTimeEquals` (constant-time).
- Passwords are accepted as `string`. If you require extra hardening, consider overloads that accept `ReadOnlySpan<char>` or secure buffers to reduce lifetime in memory.
- Add rate-limiting / lockout controls on authentication endpoints to mitigate online guessing.
- Keep hashing parameters **high enough** to be costly for attackers but acceptable for your UX.

---

### Unit testing tips
- ✅ Happy path: hash then verify → true, `needsRehash` based on your policy
- ✅ Tamper tests: mutate any field in the APX string (iterations, memory, salt, hash) → verify false
- ✅ Malformed strings: missing sections, bad Base64 → verify false
- ✅ Migration: verify PBKDF2 under Argon2id default → true + `needsRehash = true` (if password matches)

---

### Troubleshooting
- **Verify always false:** ensure you pass the *stored APX string* (not the raw hash) from your DB, and that it wasn’t truncated.
- **High CPU or latency:** lower parameters (or scale out auth service). Start with 64 MiB / t=3 / p=2 for Argon2id and benchmark.
- **Out-of-memory with Argon2id in containers:** reduce `MemoryKb` or request more memory for the pod/container.

---

### Extending
Adding a new algorithm (e.g., scrypt) is straightforward:
1. Implement a `*PasswordHasher` that emits `APX$<newVersion>$SCRYPT$...` strings.
2. Extend `PasswordHasherInspector` to parse the new header/params.
3. Add the implementation and a switch arm in `PasswordHasherFactory`.
4. Add config options under `Security:PasswordHashing`.

---

## Encryption

Authenticated symmetric encryption for protecting PII and other sensitive data.  
Uses **AES-GCM** by default (confidentiality + integrity), with **per-message random nonces**, and a **versioned envelope format** for forward-compatible migrations.  

Optional legacy AES-CBC decryptor is provided to read existing ciphertexts from older systems.

### Features
- ✅ AES-GCM with 128-bit tags (configurable)  
- ✅ Per-message random nonce (12-byte default)  
- ✅ Versioned envelopes (`ver|alg|flags|nonce|tag|cipher`)  
- ✅ DI registration with `IAuthenticatedEncryptor` abstraction  
- ✅ Optional fallback to legacy AES-CBC decryptor for migration  
- ✅ Frontend compatibility (Web Crypto API supports AES-GCM)  

---

### Configuration

**appsettings.json**

```json
{
  "Security": {
    "Encryption": {
      "RawKeyHex": "3e5d9c...32byteshex...",   // or use KdfSecret instead
      "KdfSecret": null,                       // derive key from secret if RawKeyHex omitted
      "KdfIterations": 200000,
      "NonceSize": 12,
      "TagSize": 16,
      "Version": 1,
      "Legacy": {
        "KeyHex": "001122... (optional)",
        "IvHex":  "ffeedd... (optional)"
      }
    }
  }
}
```

- **RawKeyHex** – 16/24/32-byte AES key in hex (prefer 32-byte AES-256).  
- **KdfSecret** – alternative: derive a process key from a secret using PBKDF2.  
- **NonceSize** – nonce length in bytes (12 recommended).  
- **TagSize** – auth tag length in bytes (16 = 128-bit).  
- **Version** – envelope version for migrations.  
- **Legacy** – optional key/IV for decrypting old CBC ciphertexts.  

---

### Registration

```csharp
// Program.cs / composition root
// Without legacy support:
builder.Services.AddAeadEncryption(builder.Configuration);

// With legacy CBC fallback:
builder.Services.AddAeadEncryptionWithLegacyFallback(builder.Configuration);
```

This registers:

- `IAuthenticatedEncryptor` – default AES-GCM encryptor (or facade with legacy fallback)
- `AesGcmEncryptor` – concrete GCM implementation
- `LegacyAesCbcDecryptor` (optional) – legacy decryptor

---

### API Overview

```csharp
public interface IAuthenticatedEncryptor
{
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad = default);
    byte[] Decrypt(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> aad = default);

    string EncryptToBase64Url(string plaintext, ReadOnlySpan<byte> aad = default);
    string DecryptFromBase64Url(string b64url, ReadOnlySpan<byte> aad = default);
}
```

- **AAD (Additional Authenticated Data)** is optional; you can bind ciphertexts to context (e.g., route name, request ID).

---

### Quick start

#### Protect PII

```csharp
public class CustomerService(IAuthenticatedEncryptor enc)
{
    public string ProtectEmail(string email) =>
        enc.EncryptToBase64Url(email);

    public string UnprotectEmail(string token) =>
        enc.DecryptFromBase64Url(token);
}
```

#### With AAD

```csharp
var aad = Encoding.UTF8.GetBytes("route:/register POST");
var token = enc.EncryptToBase64Url("wesley@example.com", aad);
var email = enc.DecryptFromBase64Url(token, aad);
```

If ciphertext or AAD is tampered, decryption throws `CryptographicException`.

---

### Envelope format

Internal binary layout (version 1, AES-GCM):

```
[ ver(1) | alg(1=GCM) | flags(1) | nonceLen(1) | nonce | tag | ciphertext ]
```

Final result is Base64Url-encoded for transport.

---

### Migration from legacy AES-CBC

If you already have ciphertexts produced by the old static `AesEncryptionHelper`, configure `Security:Encryption:Legacy` with the legacy KeyHex/IVHex.

`EncryptionFacade` will:
1. Try AES-GCM (new data)  
2. If that fails, attempt AES-CBC (legacy data)  
3. Return plaintext  

This allows gradual migration: once you decrypt legacy values, re-encrypt them with AES-GCM.

---

### TypeScript Implementation Example

```
/**
 * AES-GCM (Web Crypto) — ApiX envelope
 * Envelope layout (bytes): [ver(1)|alg(1=GCM)|flags(1)|nonceLen(1)|nonce|tag|cipher]
 * Base64URL-encoded for transport. Tag length = 16 bytes (128-bit) by default.
 */

const ALG_GCM = 1;
const VERSION = 1;
const NONCE_LEN = 12;      // must match server config
const TAG_LEN = 16;        // bytes (128-bit) must match server config

// -------------------- Small helpers --------------------

const enc = new TextEncoder();
const dec = new TextDecoder();

export function toHex(buf: ArrayBuffer | Uint8Array): string {
  const u8 = buf instanceof Uint8Array ? buf : new Uint8Array(buf);
  return [...u8].map(b => b.toString(16).padStart(2, "0")).join("");
}

export function fromHex(hex: string): Uint8Array {
  if (hex.length % 2 !== 0) throw new Error("Invalid hex length");
  const out = new Uint8Array(hex.length / 2);
  for (let i = 0; i < out.length; i++) {
    out[i] = parseInt(hex.slice(i * 2, i * 2 + 2), 16);
  }
  return out;
}

export function b64urlEncode(buf: ArrayBuffer | Uint8Array): string {
  const u8 = buf instanceof Uint8Array ? buf : new Uint8Array(buf);
  let bin = "";
  for (const b of u8) bin += String.fromCharCode(b);
  const b64 = btoa(bin);
  return b64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

export function b64urlDecode(s: string): Uint8Array {
  const b64 = s.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(s.length / 4) * 4, "=");
  const bin = atob(b64);
  const out = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
  return out;
}

// -------------------- Key import --------------------

/** Import a raw AES key (16/24/32 bytes) from hex. Prefer 32 bytes (AES-256). */
export async function importAesGcmKeyFromHex(hexKey: string): Promise<CryptoKey> {
  const raw = fromHex(hexKey);
  if (![16, 24, 32].includes(raw.byteLength)) {
    throw new Error("AES key must be 16, 24, or 32 bytes");
  }
  return crypto.subtle.importKey(
    "raw",
    raw,
    { name: "AES-GCM" },
    false,
    ["encrypt", "decrypt"]
  );
}

// -------------------- Encrypt / Decrypt --------------------

/**
 * Encrypt a UTF-8 string with AES-GCM and return Base64URL ApiX envelope.
 * @param key AES-GCM CryptoKey (imported with importAesGcmKeyFromHex or derived)
 * @param plaintext string to encrypt
 * @param aad optional Additional Authenticated Data (bytes)
 */
export async function encryptToB64Url(
  key: CryptoKey,
  plaintext: string,
  aad?: Uint8Array
): Promise<string> {
  const nonce = crypto.getRandomValues(new Uint8Array(NONCE_LEN));
  const data = enc.encode(plaintext);

  // WebCrypto returns ciphertext||tag (tag appended)
  const cipherAndTag = new Uint8Array(
    await crypto.subtle.encrypt(
      { name: "AES-GCM", iv: nonce, additionalData: aad, tagLength: TAG_LEN * 8 },
      key,
      data
    )
  );

  const tag = cipherAndTag.slice(cipherAndTag.length - TAG_LEN);
  const cipher = cipherAndTag.slice(0, cipherAndTag.length - TAG_LEN);

  // Build envelope: [ver|alg|flags|nonceLen|nonce|tag|cipher]
  const header = new Uint8Array([VERSION, ALG_GCM, 0 /*flags*/, NONCE_LEN]);
  const envelope = new Uint8Array(header.length + nonce.length + tag.length + cipher.length);
  let o = 0;
  envelope.set(header, o); o += header.length;
  envelope.set(nonce, o);  o += nonce.length;
  envelope.set(tag, o);    o += tag.length;
  envelope.set(cipher, o);

  return b64urlEncode(envelope);
}

/**
 * Decrypt a Base64URL ApiX envelope to UTF-8 string.
 * @param key AES-GCM CryptoKey
 * @param b64url envelope produced by encryptToB64Url (or the C# service)
 * @param aad optional AAD (must match what was used during encrypt)
 */
export async function decryptFromB64Url(
  key: CryptoKey,
  b64url: string,
  aad?: Uint8Array
): Promise<string> {
  const env = b64urlDecode(b64url);
  if (env.length < 4) throw new Error("Envelope too short");
  let o = 0;
  const ver = env[o++];                 // version
  const alg = env[o++];                 // algorithm
  const _flags = env[o++];              // reserved
  const nonceLen = env[o++];

  if (ver !== VERSION) throw new Error(`Unsupported version: ${ver}`);
  if (alg !== ALG_GCM) throw new Error(`Unsupported algorithm: ${alg}`);
  if (env.length < 4 + nonceLen + TAG_LEN) throw new Error("Envelope malformed");

  const nonce = env.slice(o, o + nonceLen); o += nonceLen;
  const tag   = env.slice(o, o + TAG_LEN);  o += TAG_LEN;
  const cipher = env.slice(o);

  // WebCrypto expects ciphertext||tag concatenated
  const cipherAndTag = new Uint8Array(cipher.length + tag.length);
  cipherAndTag.set(cipher, 0);
  cipherAndTag.set(tag, cipher.length);

  const plainBuf = await crypto.subtle.decrypt(
    { name: "AES-GCM", iv: nonce, additionalData: aad, tagLength: TAG_LEN * 8 },
    key,
    cipherAndTag
  );
  return dec.decode(plainBuf);
}

// -------------------- Example usage --------------------

// (1) Import a 32-byte AES key (same hex as server's RawKeyHex)
async function example() {
  const hexKey = "f3c9f2f5bb0c2db5f4b5d59a3b7a9a63a0c77d6b0e2de1f2f5fe6faa11223344";
  const key = await importAesGcmKeyFromHex(hexKey);

  // (2) Optional AAD to bind context
  const aad = enc.encode("route:/register POST");

  // (3) Encrypt
  const token = await encryptToB64Url(key, "wesley@example.com", aad);
  console.log("token:", token);

  // (4) Decrypt
  const email = await decryptFromB64Url(key, token, aad);
  console.log("email:", email);
}

// Call example() somewhere appropriate in your app
// example().catch(console.error);
```

---

### Unit testing tips

- ✅ Roundtrip: encrypt → decrypt → matches original  
- ✅ Tamper: flip bits → `CryptographicException`  
- ✅ AAD mismatch → `CryptographicException`  
- ✅ Nonce randomness: same plaintext encrypts to different ciphertexts  
- ✅ Legacy fallback: old Base64 CBC values still decrypt  

---

## License & attribution
- Password hashing: PBKDF2 via BCL, Argon2id via Konscious library.  
- Encryption: AES-GCM via .NET `System.Security.Cryptography`.  
