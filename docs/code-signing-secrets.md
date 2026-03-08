# Code Signing — Required GitHub Secrets

These secrets must be configured in the GitHub repository settings for the ControlR fork
(`lacymooretx/controlr`) under **Settings > Secrets and variables > Actions**.

The same certificates used for the RustDesk project (`~/code/rustdesk`) are reused here.

---

## Windows — DigiCert KeyLocker

| Secret | Description | Source |
|--------|-------------|--------|
| `SM_API_KEY` | DigiCert KeyLocker API key (91 chars) | DigiCert ONE dashboard |
| `SM_CLIENT_CERT_FILE_BASE64` | Client certificate .p12, base64-encoded | DigiCert ONE dashboard |
| `SM_CLIENT_CERT_PASSWORD` | Password for the .p12 certificate | Set during cert creation |

**Keypair alias:** `key_1474429650` (hardcoded in workflow, org-specific)
**Certificate identity:** Aspendora Technologies, LLC

### How to get values
These are the same secrets used in the RustDesk GitHub repo. Copy them directly.

---

## macOS — Apple Developer ID

| Secret | Description | Source |
|--------|-------------|--------|
| `APPLE_P12_BASE64` | Developer ID Application cert .p12, base64-encoded | Keychain Access export |
| `APPLE_P12_PASSWORD` | Password for the .p12 export | Set during export |
| `APPLE_DEVELOPER_ID` | Signing identity string | `Y6PY3BLQD2` |

### Notarization (optional, for DMG/bundle distribution)

| Secret | Description | Source |
|--------|-------------|--------|
| `MACOS_NOTARIZE_KEY_BASE64` | App Store Connect API .p8 key, base64-encoded | App Store Connect > Keys |
| `MACOS_NOTARIZE_KEY_ID` | API key ID | `AH34R6T788` |
| `MACOS_NOTARIZE_ISSUER_ID` | Issuer UUID | `8d2f8211-dbf7-4d58-bccb-31b2cdbbf9d7` |

### How to get values
These are the same secrets used in the RustDesk GitHub repo. Copy them directly.

---

## GitHub Environment

The Windows build job uses the `secure-signing` environment. Create this under
**Settings > Environments** if it doesn't exist. No special protection rules required
unless you want approval gates.

---

## Local Signing (optional)

For local builds on macOS using `deploy/sign-rustdesk.sh`-style scripts:
```bash
source ~/.secrets/.env  # Must contain SM_API_KEY, etc.
```

The PKCS11 config file for local signing is at `~/code/rustdesk/deploy/pkcs11-keylocker.cfg`.
