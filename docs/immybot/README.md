# ImmyBot ControlR Agent Deployment

Deploy the ControlR agent to managed endpoints via ImmyBot, with automatic device group creation per tenant.

## Scripts

| Script | Type | Purpose |
|--------|------|---------|
| `Detect-ControlRAgent.ps1` | Detection | Returns installed version or `$null` |
| `Install-ControlRAgent.ps1` | Install | Downloads and installs the agent |
| `Uninstall-ControlRAgent.ps1` | Uninstall | Runs the agent's built-in uninstall |
| `Configure-ControlRAgent.ps1` | Configuration Task | Assigns device to tenant-named group |

## Auto-Update

The agent checks for updates every 6 hours (SHA256 hash comparison). No additional ImmyBot task needed.

## Prerequisites

### In ControlR (https://control.aspendora.com)

1. **Create a Persistent Installer Key** -- Deploy page > Persistent > Generate Key. Save the **Key ID** and **Key Secret**.
2. **Create a Personal Access Token** -- Settings > Personal Access Tokens. Needs TenantAdministrator role. Used by the configuration script to manage groups.
3. **Note your Tenant ID** -- visible on the Deploy page.

### In ImmyBot

#### Step 1: Configuration Variables

**Admin** > **Configuration Variables**:

| Variable | Type | Value |
|----------|------|-------|
| `ControlRServerUrl` | Text | `https://control.aspendora.com` |
| `ControlRTenantId` | Text | Your tenant GUID |
| `ControlRInstallerKeyId` | Text | Installer key ID |
| `ControlRInstallerKeySecret` | Text (Secret) | Installer key secret |
| `ControlRPersonalAccessToken` | Text (Secret) | PAT token |

#### Step 2: Create Software Entry

1. **Software** > **+ New Software**
2. **Name**: `ControlR Agent`
3. **Detection**: Upload `Detect-ControlRAgent.ps1`
4. **Install**: Upload `Install-ControlRAgent.ps1`
   - Map parameters: `ControlRServerUrl`, `ControlRTenantId`, `ControlRInstallerKeyId`, `ControlRInstallerKeySecret`
5. **Uninstall**: Upload `Uninstall-ControlRAgent.ps1`
   - Map parameter: `ControlRServerUrl`
6. **Desired State**: Latest (use `1.0.0` or match your server version)

#### Step 3: Create Configuration Task (Group Assignment)

1. **Maintenance** > **Tasks** > **+ New Task**
2. **Name**: `ControlR Device Group Assignment`
3. **Task Type**: Configuration Task
4. Upload `Configure-ControlRAgent.ps1`
5. Check **Use Script Param Block**
6. **Map Parameters**:
   - `Method` -> Auto
   - `TenantName` -> Auto
   - `ControlRServerUrl` -> Configuration Variable
   - `ControlRPersonalAccessToken` -> Configuration Variable

#### Step 4: Deploy

1. **Deployments** > **+ New Deployment**
2. **Software**: ControlR Agent
3. **Target**: Cross-tenant (all tenants) or specific tenants
4. **Desired State**: Latest

Then add the Configuration Task to a maintenance schedule targeting all computers.

## Device Group Mapping

| ImmyBot Tenant | ControlR Device Group |
|----------------|----------------------|
| Acme Corp | Acme Corp |
| Contoso Ltd | Contoso Ltd |
| (new tenant) | (auto-created on first config run) |

Groups get description "Auto-created from ImmyBot tenant: {name}".
