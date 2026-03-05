# ControlR Deployment Runlog

## 2026-03-05: Interactive PTY Terminal Implementation

### Goal
Add real PTY-based terminal (xterm.js + ConPTY/forkpty) to replace command-and-response PowerShell model.

### Steps Completed
1. **Created PTY DTOs** — PtyInputDto, PtyOutputDto, PtyResizeDto with MessagePack serialization
2. **Added hub interface methods** — IViewerHub (4 methods), IAgentHubClient (4 methods), IAgentHub (+1), IViewerHubClient (+1)
3. **Implemented server hub methods** — ViewerHub (CreatePtySession, SendPtyInput, ResizePty, ClosePtySession), AgentHub (SendPtyOutputToViewer)
4. **Created PtyTerminalSession** — Platform-specific PTY: ConPTY on Windows, forkpty on Linux/macOS
5. **Created ConPtyInterop.cs** — Windows P/Invoke: CreatePseudoConsole, CreateProcessW, ResizePseudoConsole, etc.
6. **Created UnixPtyInterop.cs** — Linux/macOS P/Invoke: forkpty, read, write, ioctl(TIOCSWINSZ), kill, waitpid
7. **Updated TerminalSessionFactory** — Added CreatePtySession method
8. **Updated TerminalStore** — Added PTY session cache, WritePtyInput, ResizePty, TryRemovePty
9. **Updated AgentHubClient** — Added 5 PTY handlers
10. **Updated ViewerHubClient** — Added ReceivePtyOutput handler
11. **Downloaded xterm.js v5.5.0** — xterm.min.js, xterm.css, addon-fit.min.js, addon-web-links.min.js
12. **Created PtyTerminal Blazor component** — .razor, .razor.cs, .razor.js, .razor.css
13. **Updated Terminal.razor** — Added toggle between Interactive Terminal (PTY) and PowerShell Console
14. **Updated App.razor** — Added xterm.js CSS and script tags

### Files Created (14)
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyInputDto.cs
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyOutputDto.cs
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyResizeDto.cs
- ControlR.Agent.Common/Services/Terminal/PtyTerminalSession.cs
- ControlR.Agent.Common/Services/Terminal/Interop/ConPtyInterop.cs
- ControlR.Agent.Common/Services/Terminal/Interop/UnixPtyInterop.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.js
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.css
- ControlR.Web.Server/wwwroot/lib/xterm/xterm.min.js
- ControlR.Web.Server/wwwroot/lib/xterm/xterm.css
- ControlR.Web.Server/wwwroot/lib/xterm/addon-fit.min.js
- ControlR.Web.Server/wwwroot/lib/xterm/addon-web-links.min.js

### Files Modified (12)
- Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs
- Libraries/ControlR.Libraries.Shared/Hubs/IAgentHub.cs
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IViewerHubClient.cs
- ControlR.Web.Server/Hubs/ViewerHub.cs
- ControlR.Web.Server/Hubs/AgentHub.cs
- ControlR.Web.Server/Components/App.razor
- ControlR.Web.Client/Services/ViewerHubClient.cs
- ControlR.Agent.Common/Services/Terminal/TerminalSessionFactory.cs
- ControlR.Agent.Common/Services/Terminal/TerminalStore.cs
- ControlR.Agent.Common/Services/AgentHubClient.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/Terminal.razor
- ControlR.Web.Client/Components/Pages/DeviceAccess/Terminal.razor.cs

### Next Steps
- Docker build verification on server
- Deploy and smoke test

---

## 2026-03-04: Production Deployment to control.aspendora.com

### Summary
Deployed ControlR fork (lacymooretx/ControlR) with all 12 MSP features to production at `https://control.aspendora.com`.

### Key Steps
1. **Committed & pushed** 107 files (8,530 lines) covering all 6 phases
2. **Fixed Dockerfile** — updated project references, added Directory.Packages.props, created novnc dir
3. **Fixed OpenAPI conflict** — renamed duplicate path in ClientPortalController
4. **Generated EF Core migration** — `AddMspFeatures` for 15+ new tables
5. **Server setup** — cloned repo, built Docker image, created docker-compose.yml + .env
6. **SSL cert** — obtained via certbot dns-cloudflare for control.aspendora.com
7. **Nginx** — added server block with WebSocket support (86400s timeout for SignalR)
8. **Cloudflare DNS** — A record proxied to 149.28.251.164
9. **Network fix** — corrected network name from `aspendora-net` to `docker_aspendora-net`

### Issues Resolved
- Dockerfile referenced non-existent `ControlR.Libraries.Clients` → fixed to actual library names
- Missing `/app/novnc/` directory in container → added `RUN mkdir -p novnc`
- Duplicate OpenAPI path `assignments/{id}` → renamed to `assignments/by-user/{userId}`
- PostgreSQL 18 data dir change → mount at `/var/lib/postgresql` not `/var/lib/postgresql/data`
- No EF Core migration for new entities → generated on server via Docker SDK container
- Nginx couldn't resolve `controlr` → wrong network name (docker prefix issue)

### Server Files
| File | Action |
|------|--------|
| `/opt/docker/controlr/source/` | Git clone |
| `/opt/docker/controlr/docker-compose.yml` | Created |
| `/opt/docker/controlr/.env` | Created |
| `/opt/docker/nginx/conf.d/default.conf` | Appended |
| Cloudflare DNS | A record added |
| SSL cert | `/etc/letsencrypt/live/control.aspendora.com/` |

### Docker Config
- Network: `docker_aspendora-net` (172.27.0.0/16, gateway 172.27.0.1)
- Containers: `controlr`, `controlr-postgres`, `controlr-aspire`
- Image: `controlr-aspendora:latest` (built from fork)

## 2026-03-04: Entra ID SSO Setup

### Summary
Configured Entra ID (Azure AD) single sign-on for ControlR via Azure CLI.

### Steps
1. **Azure App Registration** — created `ControlR - Aspendora` (client ID: `f9479760-e358-4960-aa55-cd035b1232cf`)
2. **Client secret** — created with 2-year expiry
3. **ID token issuance** — enabled in app registration
4. **Env vars** — added `CONTROLR_ENTRAID_CLIENT_ID`, `CONTROLR_ENTRAID_CLIENT_SECRET`, `CONTROLR_ENTRAID_TENANT_ID` to `.env` and `docker-compose.yml`

### Issues Resolved
- **502 Bad Gateway on `/signin-oidc`** — nginx default `proxy_buffer_size` (4k/8k) too small for OIDC token response headers → added `proxy_buffer_size 128k`, `proxy_buffers 4 256k`, `proxy_busy_buffers_size 256k`
- **520 Unknown Error on `/Account/ExternalLogin`** — large auth cookies in subsequent requests exceeded nginx `large_client_header_buffers` default → added `large_client_header_buffers 4 32k`

### Nginx Config Changes (control.aspendora.com server block)
```nginx
# Added to server block:
large_client_header_buffers 4 32k;

# Added to location block:
proxy_buffer_size 128k;
proxy_buffers 4 256k;
proxy_busy_buffers_size 256k;
```

### Result
Entra ID SSO flow completes successfully. Users authenticate with Microsoft and are shown the account association/registration page in ControlR.
