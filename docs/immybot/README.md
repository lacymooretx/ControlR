# ImmyBot ControlR Agent Deployment

Deploy the ControlR agent via ImmyBot with automatic tenant-based device groups.

## Scripts

| Script | Type | Purpose |
|--------|------|---------|
| `Detect-ControlRAgent.ps1` | Detection | Returns installed version or `$null` |
| `Install-ControlRAgent.ps1` | Install | Creates installer key via API, downloads and installs agent |
| `Uninstall-ControlRAgent.ps1` | Uninstall | Runs agent's built-in uninstall |
| `Configure-ControlRAgent.ps1` | Configuration Task | Creates/assigns device group per tenant |

## Configuration Variables (only 2 needed)

| Variable | Type | Value |
|----------|------|-------|
| `ControlRServerUrl` | Text | `https://control.aspendora.com` |
| `ControlRPersonalAccessToken` | Text (Secret) | PAT from a TenantAdministrator |

The install script discovers the tenant ID and creates its own single-use installer key via the API. No manual key management needed.

## Setup

### 1. Create Software Entry

1. **Software** > **+ New Software**
2. **Name**: `ControlR Agent`
3. **Detection**: `Detect-ControlRAgent.ps1` (no parameters)
4. **Install**: `Install-ControlRAgent.ps1`
   - `ControlRServerUrl` -> Configuration Variable
   - `ControlRPersonalAccessToken` -> Configuration Variable
5. **Uninstall**: `Uninstall-ControlRAgent.ps1`
   - `ControlRServerUrl` -> Configuration Variable
6. **Desired State**: Latest (`1.0.0`)

### 2. Create Configuration Task

1. **Maintenance** > **Tasks** > **+ New Task**
2. **Name**: `ControlR Device Group Assignment`
3. **Task Type**: Configuration Task
4. Upload `Configure-ControlRAgent.ps1`, check **Use Script Param Block**
5. **Map Parameters**:
   - `Method` -> Auto
   - `ControlRServerUrl` -> Configuration Variable
   - `ControlRPersonalAccessToken` -> Configuration Variable

### 3. Deploy

- Create a cross-tenant deployment for the software
- Schedule the configuration task on all computers

## How It Works

**Install flow:**
1. Calls `/api/me` with PAT to get tenant ID
2. Creates a single-use installer key via `/api/installer-keys`
3. Downloads agent binary from server
4. Runs `ControlR.Agent.exe install` with tenant/key args
5. Agent registers with server and auto-updates every 6 hours

**Configuration flow (per-run):**
1. Calls `/api/me` to get the tenant name
2. Looks up device in ControlR by `$env:COMPUTERNAME`
3. Finds or creates a device group matching the tenant name
4. Assigns the device to that group

## API Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/me` | GET | Discover tenant ID and name from PAT |
| `/api/installer-keys` | POST | Create single-use installer key |
| `/api/devices` | GET | Find device by name |
| `/api/device-groups` | GET | List groups |
| `/api/device-groups` | POST | Create group |
| `/api/device-groups/{deviceId}/group` | PUT | Assign device to group |
