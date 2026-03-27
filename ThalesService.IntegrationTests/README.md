Integration tests require a running Thales service (TCP) and optionally the HTTP→TCP proxy that exposes `/api/hsm/command`.

Quick start (PowerShell):

1) Start the Thales console host on a chosen port (example uses 50000):

```powershell
# from repo root
$env:THALES_SERVICE_PORT = "50000"
dotnet run --project ThalesService.Hosts.Console
```

2) Start the HTTP→TCP proxy (recommended, binds to 54879 by convention):

```powershell
# from repo root
dotnet run --project ThalesService.Proxy --urls "http://localhost:54879"
# or set ASPNETCORE_URLS then run
$env:ASPNETCORE_URLS = "http://localhost:54879"
dotnet run --project ThalesService.Proxy
```

3) Export environment variables used by the tests:

```powershell
$env:HSM_HOST = "localhost"
$env:HSM_PORT = "50000"
$env:HSM_API_URL = "http://localhost:54879"   # optional; tests fall back to TCP if proxy fails
```

4) Run the integration tests:

```powershell
dotnet test ThalesService.IntegrationTests/ThalesService.IntegrationTests.csproj -v normal
```

Notes and troubleshooting:
- Integration tests prefer to use the HTTP→TCP proxy (`HSM_API_URL`) but will fall back to a direct TCP connection using `HSM_HOST`/`HSM_PORT` when the proxy returns a timeout or error.
- If you observe intermittent proxy `504` / no-data responses in proxy logs, check the `ThalesService.ThalesTcpService` logs — the service now logs write attempts and completion which helps diagnose socket/flush races.
- If you only want to test TCP without the proxy, unset `HSM_API_URL` and set `HSM_HOST`/`HSM_PORT` instead.
