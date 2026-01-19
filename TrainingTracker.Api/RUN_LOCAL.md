# Run API Locally (Pre-Git Snapshot)

## Prerequisites
- .NET SDK 9 (confirm with `dotnet --version`)
- Local SQL Server instance (e.g., Developer Edition or container) with login: `sa` / `YourStrong!Passw0rd` (adjust connection string otherwise)
- DACPAC published already or build the SSDT project:
  - Using VS: Build the SQL project in Debug or Release.
  - CLI (Release preferred):
    ```powershell
    msbuild ..\..\..\POC_SpecKitProj\POC_SpecKitProj.sqlproj /t:Build /p:Configuration=Release
    ```

## Connection String
Configured in `appsettings.Development.json` under `ConnectionStrings:TrainingTracker`.

## Persistence Mode
Set to `Ef` in `appsettings.Development.json`. Change to `InMemory` for quick API smoke tests without SQL.

## Run the API
```powershell
cd backend/TrainingTracker.Api
dotnet run --launch-profile https
```
API base URLs (from launchSettings):
- HTTP: http://localhost:5115
- HTTPS: https://localhost:7026

Health check:
```powershell
curl https://localhost:7026/health -k
```

## Seeding (Optional)
After DACPAC publish, run seed script:
```powershell
sqlcmd -S localhost -d POC_TrainingDB_Local -U sa -P YourStrong!Passw0rd -i ..\..\scripts\Seed_Training_Data.sql
```

## Switch to InMemory Quickly
Edit `appsettings.Development.json`:
```json
"Persistence": { "Mode": "InMemory" }
```
Then re-run `dotnet run`.

## Common Issues
| Symptom | Cause | Fix |
|---------|-------|-----|
| 500 on courses endpoint | Missing tables / DACPAC not published | Publish DACPAC or switch to InMemory |
| CORS error in browser | Missing origin | API already allows http://localhost:5173; ensure Vite runs there |
| Login failures | No auth implemented yet | Ignore; remove stored token if present |

---
Once stable, initialize git and promote Release DACPAC as the default.
