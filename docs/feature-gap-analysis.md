# ControlR Feature Gap Analysis vs Commercial Remote Control Tools

**Date:** 2026-03-06
**Competitors Analyzed:** ConnectWise ScreenConnect, TeamViewer, LogMeIn Resolve, Splashtop, AnyDesk

---

## Current State: Implemented Features (18 Exist, 3 Partial)

| Feature | Status | Detail |
|---------|--------|--------|
| Multi-monitor support | Exists | Full display enumeration, switching, per-display capture with DPI/position |
| In-session chat | Exists | Bidirectional via SignalR with audit logging |
| File transfer | Exists | Bidirectional upload/download, stream-based, configurable size limits |
| Clipboard sync | Exists | Bidirectional text, platform-specific (Win/Mac/Linux) |
| Wake-on-LAN | Exists | Multi-MAC, group-based, tag-based targeting |
| Unattended access | Exists | Persistent agent as Windows service / Linux daemon |
| Multi-session | Exists | Multiple viewers to same device concurrently |
| Device groups/tags | Exists | Many-to-many tagging, tag-based access control |
| Reporting/audit | Exists | Comprehensive audit logging, device metrics, script reports |
| 2FA/MFA | Exists | TOTP, recovery codes, Passkey/WebAuthn, per-tenant enforcement |
| RBAC | Exists | 4 roles + tag-based + device-level policies |
| Scripting engine | Exists | Ad-hoc and saved scripts with output capture |
| Scheduled tasks | Exists | Cron-based with timezone support |
| Alerting/monitoring | Exists | Threshold-based on CPU, memory, disk |
| Inventory management | Exists | Software, hardware, update tracking |
| Webhooks | Exists | Event-driven with delivery tracking |
| Entra ID SSO | Exists | OpenID Connect for Azure AD |
| Interactive terminal | Exists | PTY-based (ConPTY/forkpty) with xterm.js |
| Backstage mode | Partial | Agent runs as service, `NotifyUserOnSessionStart` configurable, but no screen blanking |
| Custom branding | Partial | `BrandingConstants.cs` infrastructure but no tenant-specific themes |
| Mobile PWA | Partial | Blazor WASM works in mobile browsers, service worker added |

---

## Gap Analysis: Missing Features Ranked by Impact

### Tier 1 — High Impact (Present in 4-5 Competitors)

| # | Feature | Who Has It | Description | Effort |
|---|---------|-----------|-------------|--------|
| 1 | **On-Demand / Guest Sessions** | SC, TV, Splashtop, AnyDesk | Generate one-time link/code. End-user clicks → lightweight temp agent downloads → instant session. Auto-expires. No pre-install needed. | Large |
| 2 | **Session Recording (Video)** | SC, TV, LogMeIn, Splashtop, AnyDesk | Record full remote sessions as video for compliance/training. Configurable retention, storage path, auto-record option. | Medium |
| 3 | **Toolbox (Program Store & Deploy)** | SC (signature feature) | Upload portable tools/scripts to personal or shared library. One-click deploy and execute on remote endpoints without manual transfer. ScreenConnect uses .scapp packages. | Medium |
| 4 | **Remote Resolution Change** | TV, SC, AnyDesk | Change display resolution on remote device from the viewer. Essential for headless servers and bandwidth optimization. | Small |
| 5 | **Full Backstage / Privacy Mode** | SC, AnyDesk | Black out remote screen so end-user can't see technician activity. Switch between user session and SYSTEM session invisibly. | Medium |

### Tier 2 — Medium Impact (Present in 2-3 Competitors)

| # | Feature | Who Has It | Description | Effort |
|---|---------|-----------|-------------|--------|
| 6 | **Credential Vault** | SC, LogMeIn | Encrypted per-device/group credential storage for quick auth during sessions. AES-256 at rest. | Medium |
| 7 | **JIT Admin Account Creation** | LogMeIn (zero trust) | Create temporary local admin on remote device, do work, auto-delete. Eliminates shared admin passwords. | Medium |
| 8 | **Annotations / Whiteboard** | AnyDesk, Splashtop, TV | Draw, highlight, annotate on remote screen. For user training and guided troubleshooting. | Medium |
| 9 | **Remote Printing** | TV, AnyDesk | Print from remote machine to local printer via virtual printer driver. | Large |
| 10 | **White-Label Branding** | SC, TV, AnyDesk | Custom logo, colors, product name, login page, agent branding per tenant. | Medium |
| 11 | **Reboot to Safe Mode + Reconnect** | SC, TV, LogMeIn | Reboot into Safe Mode with Networking, agent auto-reconnects. Critical for malware remediation. | Small |
| 12 | **Patch Management** | LogMeIn, Splashtop | OS and third-party patch scanning, approval workflows, automated deployment. | Large |

### Tier 3 — Differentiators / Nice-to-Have

| # | Feature | Who Has It | Description | Effort |
|---|---------|-----------|-------------|--------|
| 13 | **Extension / Plugin API** | SC | Open API for community/custom extensions. ScreenConnect has 100+. | Large |
| 14 | **Audio Forwarding** | TV, Splashtop | Stream remote audio to local machine. Currently only volume keys forwarded. | Medium |
| 15 | **Voice/Video Chat** | TV | Real-time voice/video during remote sessions (beyond text chat). | Large |
| 16 | **Multi-to-Multi Monitor** | Splashtop, LogMeIn | Pop out individual remote monitors into separate local windows. | Medium |
| 17 | **Integrated Helpdesk/Ticketing** | LogMeIn, Splashtop | Built-in ticket system with session-to-ticket linking, SLA tracking. | Large |
| 18 | **Zero Trust Per-Action Auth** | LogMeIn | Per-action MFA for sensitive ops (deploy agents, run scripts), not just session start. | Medium |
| 19 | **AR Support** | Splashtop | End-user shares mobile camera, technician annotates live feed for physical troubleshooting. | Large |
| 20 | **AI Virtual Technician** | LogMeIn | Natural-language automation — "check disk space on all servers" without writing scripts. | Large |

---

## Recommended Implementation Phases

### Phase 7: Session Enhancements
- On-demand / guest sessions (one-time link + lightweight temp agent)
- Session recording (video capture with configurable retention)
- Full backstage mode (screen blanking on remote device)

### Phase 8: Toolbox & Deployment
- Toolbox (program store, upload, one-click deploy & execute)
- Reboot to Safe Mode + reconnect
- Remote resolution change

### Phase 9: Security & Credentials
- Credential vault (encrypted per-device/group storage)
- JIT admin account creation + auto-cleanup
- Zero trust per-action auth for sensitive operations

### Phase 10: Polish & Differentiation
- Annotations / whiteboard overlay
- Full white-label branding per tenant
- Multi-to-multi monitor (pop-out windows)
- Remote printing
- Patch management

### Phase 11: Platform Expansion
- Extension / plugin API
- Audio forwarding
- Integrated helpdesk / ticketing
- AI-assisted automation

---

## Top 3 Highest-Value Features

1. **Toolbox** — ScreenConnect's killer feature. Store and one-click deploy tools (SysInternals, malware scanners, custom scripts) to any endpoint. Massive time-saver for technicians.

2. **On-Demand Sessions** — Every competitor has this. Without it, you can only support devices with the agent pre-installed. "Generate link → user clicks → instant session" is table stakes for ad-hoc support.

3. **JIT Admin Accounts** — Leapfrog opportunity. Most tools still use shared admin credentials. Auto-creating a temp admin, doing work, and auto-deleting is a security game-changer.

---

## Competitor Quick Reference

### ConnectWise ScreenConnect — MSP Power Tool
- Backstage mode, Toolbox (.scapp), 100+ extensions marketplace
- Self-hosted with perpetual license, session-level RBAC
- Deep ConnectWise Manage PSA integration

### TeamViewer Tensor — Enterprise Governance
- Conditional access policies, Malwarebytes endpoint protection
- Broadest platform coverage (Chrome OS, BlackBerry, UWP)
- Multitenancy, deep MDM integration (Intune, MobileIron)

### LogMeIn Resolve — All-in-One IT Platform
- Zero trust per-action auth, AI Virtual Technician
- Built-in helpdesk/ticketing, natural language reporting
- Combined RMM + remote support + helpdesk

### Splashtop — Performance & Innovation
- 8K/60fps streaming (best-in-class), AR support
- Multi-to-multi monitor, unlimited concurrent sessions (MSP plans)
- AI-enhanced CVE detection, Smart Actions automated remediation

### AnyDesk — Lightweight & Affordable
- Custom namespace with namespace-level whitelisting
- DeskRT codec (best low-bandwidth), Privacy Mode
- Smallest client, cheapest entry point
