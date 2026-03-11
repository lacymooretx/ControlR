# ControlR REST API Usage

## Authentication

All API requests require a Personal Access Token (PAT) passed via the `x-personal-token` header.

### Creating a PAT

From the production server:

```bash
ssh root@149.28.251.164
docker exec controlr curl -s -X POST http://localhost:8080/management/create-pat \
  -H 'Content-Type: application/json' \
  -d '{"email":"lacy@aspendora.com","name":"My API Token"}'
```

The `/management/create-pat` endpoint is restricted to localhost connections (within the container).

### Storing the PAT

Add to `~/.secrets/.env`:
```bash
CONTROLR_PAT="<token from create-pat response>"
CONTROLR_API_URL="https://control.aspendora.com"
```

### Using the PAT

```bash
source ~/.secrets/.env
curl -s https://control.aspendora.com/api/devices \
  -H "x-personal-token: $CONTROLR_PAT"
```

## Endpoints

### Devices

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/devices` | List all authorized devices |
| GET | `/api/devices/{id}` | Get a specific device |
| POST | `/api/devices/search` | Search/filter devices |

### Scripts

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/scripts` | List all saved scripts |
| GET | `/api/scripts/{id}` | Get a saved script |
| POST | `/api/scripts` | Create a saved script (TenantAdmin) |
| PUT | `/api/scripts` | Update a saved script (TenantAdmin) |
| DELETE | `/api/scripts/{id}` | Delete a saved script (TenantAdmin) |

### Script Execution

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/script-executions` | Execute a script on target devices |
| GET | `/api/script-executions/{id}` | Get execution results |
| GET | `/api/script-executions?count=N` | List recent executions |

#### Execute Script Request

```json
{
  "ScriptId": null,
  "AdHocScriptContent": "Write-Output 'hello'",
  "ScriptType": "powershell",
  "TargetDeviceIds": ["device-guid-1", "device-guid-2"]
}
```

- `ScriptId`: GUID of a saved script (use this OR AdHocScriptContent)
- `AdHocScriptContent`: Raw script content for ad-hoc execution
- `ScriptType`: `"powershell"`, `"bash"`, or `"cmd"`
- `TargetDeviceIds`: Array of device GUIDs to execute on

#### Execute Script Response

Returns a `ScriptExecutionDto` with an `id` that can be polled for results.

#### Polling for Results

```bash
curl -s "https://control.aspendora.com/api/script-executions/$EXECUTION_ID" \
  -H "x-personal-token: $CONTROLR_PAT"
```

Result statuses: `Pending`, `Running`, `Completed`, `Failed`, `TimedOut`

Overall statuses: `Running`, `Completed`, `CompletedWithErrors`

## Example: Deploy Software to All Devices

```bash
source ~/.secrets/.env

# Get all online device IDs
DEVICE_IDS=$(curl -s "$CONTROLR_API_URL/api/devices" \
  -H "x-personal-token: $CONTROLR_PAT" | \
  python3 -c "import json,sys; print(json.dumps([d['id'] for d in json.load(sys.stdin) if d['isOnline']]))")

# Execute script
EXEC_ID=$(curl -s -X POST "$CONTROLR_API_URL/api/script-executions" \
  -H "x-personal-token: $CONTROLR_PAT" \
  -H "Content-Type: application/json" \
  -d "{
    \"ScriptId\": null,
    \"AdHocScriptContent\": \"Write-Output 'hello from API'\",
    \"ScriptType\": \"powershell\",
    \"TargetDeviceIds\": $DEVICE_IDS
  }" | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")

echo "Execution ID: $EXEC_ID"

# Poll for results
sleep 30
curl -s "$CONTROLR_API_URL/api/script-executions/$EXEC_ID" \
  -H "x-personal-token: $CONTROLR_PAT" | python3 -m json.tool
```

## Notes

- Scripts execute with a 5-minute timeout per device
- Agents run as SYSTEM on Windows — `winget` doesn't work (requires user context)
- Use Chocolatey or direct download for package installation
- For large fleets, batch executions to avoid rate limits on external package repos
- PAT-authenticated requests bypass the action verification (TOTP) requirement
