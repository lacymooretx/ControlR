# ImmyBot ControlR Agent Deployment

Deploy the ControlR agent to all managed endpoints via ImmyBot, with automatic device group creation based on ImmyBot tenant names.

## What It Does

1. **Checks** if the ControlR agent service is installed and running
2. **Creates a device group** in ControlR matching the ImmyBot tenant name (if one doesn't exist)
3. **Downloads and installs** the ControlR agent on the target endpoint
4. **Assigns the device** to the tenant's device group
5. **Auto-updates** are built in -- the agent checks for updates every 6 hours

## Prerequisites

### In ControlR (https://control.aspendora.com)

1. **Create a Persistent Installer Key**
   - Go to **Deploy** page
   - Select **Persistent** key type
   - Click **Generate Key**
   - Save the **Key ID** and **Key Secret**

2. **Create a Personal Access Token (PAT)**
   - Go to **Settings** > **Personal Access Tokens**
   - Create a new token (needs TenantAdministrator role)
   - Save the token value

3. **Note your Tenant ID**
   - Visible in the Deploy page URL or your account settings

### In ImmyBot

#### Step 1: Create Configuration Variables

Go to **Admin** > **Configuration Variables** and create:

| Variable | Type | Value |
|----------|------|-------|
| `ControlRServerUrl` | Text | `https://control.aspendora.com` |
| `ControlRTenantId` | Text | Your tenant GUID |
| `ControlRInstallerKeyId` | Text | The installer key ID |
| `ControlRInstallerKeySecret` | Text (Secret) | The installer key secret |
| `ControlRPersonalAccessToken` | Text (Secret) | Your PAT token |

#### Step 2: Create the Task

1. **Maintenance** > **Tasks** > **+ New Task**
2. **Name**: `Deploy ControlR Agent`
3. **Category**: `Remote Management`
4. **Task Type**: `Configuration Task`
5. **Script Type**: `PowerShell`
6. Upload: `Deploy-ControlR-Agent.ps1`
7. Check **Use Script Param Block**
8. **Map Parameters**:
   - `Method` -> Auto (ImmyBot provides)
   - `TenantName` -> Auto (ImmyBot provides)
   - `ControlRServerUrl` -> Configuration Variable
   - `ControlRTenantId` -> Configuration Variable
   - `ControlRInstallerKeyId` -> Configuration Variable
   - `ControlRInstallerKeySecret` -> Configuration Variable
   - `ControlRPersonalAccessToken` -> Configuration Variable

#### Step 3: Create a Schedule

1. **Schedules** > **+ New Schedule**
2. **Name**: `ControlR Agent Deployment`
3. **Tasks**: Deploy ControlR Agent
4. **Target**: All Windows Computers
5. **Run**: On new computer onboarding + Daily check

## How Auto-Update Works

The ControlR agent has built-in auto-update:
- Checks the server every **6 hours** for a new agent binary
- Compares SHA256 hashes of local vs server binary
- If different, downloads the new version and re-installs itself
- Also checks on hub reconnection

No additional ImmyBot task needed for updates.

## Device Group Mapping

| ImmyBot Tenant | ControlR Device Group |
|----------------|----------------------|
| Acme Corp | Acme Corp |
| Contoso Ltd | Contoso Ltd |
| (any new tenant) | (auto-created on first deploy) |

Groups are created with description "Auto-created from ImmyBot tenant: {name}".
