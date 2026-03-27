 # HSMThalesEmu

Thales HSM emulator — local implementation of server logic and a set of hosts/clients for development and integration testing.

## Repository layout

- `ThalesCore/` — core library with command implementations, message parser, and cryptography.
- `ThalesService/` — service library implementing the `ThalesTcpService` BackgroundService (TCP server).
- `ThalesService.Hosts.Console/` — console host that runs `ThalesTcpService` inside an `IHost` for local runs.
- `ThalesService.Hosts.WindowsService/` — Windows Service host (uses `UseWindowsService`).
- `ThalesService.Hosts.UI/` — WinForms host to run the service interactively.
- `ThalesService.IntegrationTests/` — integration tests (NUnit). Tests start the host in-process and use ephemeral ports.
 - `ThalesService.IntegrationTests/` — integration tests (NUnit). Tests start the host in-process and use ephemeral ports. Tests prefer the HTTP→TCP proxy (`ThalesService.Proxy`) but will fall back to direct TCP when the proxy is unavailable or times out.
- `ThalesCore.Tests/` — unit tests for `ThalesCore` (NUnit).
- `ThalesClients/ConsoleClient/` — simple console TCP client (not included in the solution by design).
- `ThalesClients/UIClient/` — simple WinForms client with checkboxes/lists (not included in the solution by design).
 - `ThalesClients/ConsoleClient/` — simple console TCP client (included in the solution).
 - `ThalesClients/UIClient/` — simple WinForms client with dynamic actions and payload builder (included in the solution).

## Key concepts

- The service reads the listening port from the `THALES_SERVICE_PORT` environment variable. Integration tests use an ephemeral port and set this environment variable for the test host.
- Integration tests run the service in-process with `Host.CreateDefaultBuilder()` and `AddHostedService<ThalesTcpService>()` to avoid inter-process race conditions.
- The message parser has been hardened — it now returns a rejection code (for example, `80`) for insufficient or malformed input instead of throwing an exception.
 - The service reads the listening port from the `THALES_SERVICE_PORT` environment variable. Integration tests use an ephemeral port and set this environment variable for the test host.
 - Integration tests run the service in-process with `Host.CreateDefaultBuilder()` and `AddHostedService<ThalesTcpService>()` to avoid inter-process race conditions.
 - The HTTP→TCP proxy exposes a JSON endpoint at `/api/hsm/command` which accepts payloads like `{ "command": "<framed-thales-command>" }`.
 - The message parser and several host commands were hardened in this session: malformed input is handled defensively and command handlers return proper HSM error codes instead of throwing.

## Build

1. Install .NET SDK 8.x (latest stable recommended).
2. From the repository root you can build specific projects:

```bash
dotnet build ThalesCore/ThalesCore.csproj -c Debug
dotnet build ThalesService/ThalesService.csproj -c Debug
dotnet build ThalesService.IntegrationTests/ThalesService.IntegrationTests.csproj -c Debug
```

Or build the whole solution:

```bash
dotnet build ThalesEmu.sln -c Debug
```

## Running hosts

- Console host (run the service locally):

```bash
cd ThalesService.Hosts.Console/bin/Debug/net8.0
dotnet ThalesService.Hosts.Console.dll
```

- Windows Service host: build `ThalesService.Hosts.WindowsService` and install it as a Windows service (for example use `sc create ...` or PowerShell service management).

- UI host (WinForms):

```bash
cd ThalesService.Hosts.UI/bin/Debug/net8.0-windows
dotnet ThalesService.Hosts.UI.dll
```

By default the service listens on port `1500`. For tests or alternative local runs set the `THALES_SERVICE_PORT` environment variable to a different port.
 
 Proxy host (HTTP→TCP) — `ThalesService.Proxy`:

- Run via `dotnet run` and set the listen address with `--urls` or `ASPNETCORE_URLS`:

```powershell
dotnet run --project ThalesService.Proxy --urls "http://localhost:54879"
# or
$env:ASPNETCORE_URLS = "http://localhost:54879"
dotnet run --project ThalesService.Proxy
```

Or run the published executable at `ThalesService.Proxy/bin/Debug/net8.0/ThalesService.Proxy.exe`.

The proxy endpoint for commands is `POST /api/hsm/command`. Integration tests prefer the proxy but will automatically fall back to direct TCP to `HSM_HOST`/`HSM_PORT` if the proxy returns a timeout or 5xx.

## Clients (included in the solution)

- `ThalesClients/ConsoleClient` — interactive console TCP client. Run:

```bash
dotnet run --project ThalesClients/ConsoleClient/ThalesConsoleClient.csproj -- 127.0.0.1 1500
```

- `ThalesClients/UIClient` — WinForms UI client with descriptive actions and a `PayloadBuilder`. Run:

```bash
dotnet run --project ThalesClients/UIClient/ThalesUIClient.csproj
```

These client projects are added to the solution for convenience and remain lightweight examples for manual testing and development.

## Tests

- Run integration tests:

```bash
dotnet test ThalesService.IntegrationTests/ThalesService.IntegrationTests.csproj -c Debug
```

- Run all tests in the solution:

```bash
dotnet test ThalesEmu.sln -c Debug

Notes on test resilience:
- Integration tests that target the HTTP proxy will fall back to a direct TCP connection when the proxy fails or times out.
- If you see intermittent 0-byte reads in proxy logs, check `ThalesService.ThalesTcpService` logs for write activity — the host now logs attempts to write response bytes back to the client, which helps diagnose socket/flush races.
```

## Where to look in the code

- Message parser: `ThalesCore/Message/XML/MessageParser.cs` — length checks and parsing logic are implemented here.
- Service TCP listener: `ThalesService/ThalesTcpService.cs` — connection handling and command dispatching.
- Built-in commands: `ThalesCore/HostCommands/BuildIn` — examples such as `EchoTest_B2`.

## Debugging tips

- If tests are not visible in Visual Studio Test Explorer, ensure that compatible versions of `NUnit3TestAdapter` and `Microsoft.NET.Test.Sdk` are specified in `Directory.Packages.props`, and that projects target `net8.0` (or a compatible TFM).
- For local debugging of integration tests, start the service manually (see Running hosts) and use the `ThalesClients/ConsoleClient` to send test requests.
- The repository contains compiler warnings about deprecated APIs and unused variables; these can be cleaned up over time and do not block integration tests.

## Recommended next steps

- Add per-project README files (for example `ThalesService/README.md`) with project-specific run examples and settings.
- Configure CI (GitHub Actions) to run `dotnet build` and `dotnet test` on a Windows runner to catch platform-specific issues.
- Extend `ThalesClients/UIClient` with real command parameter forms and validation.

If you want, I can:
- add separate README files per project, or
- create a GitHub Actions workflow that builds and runs tests on Windows.

---
Author: automated assistant — repository edits and instructions were prepared as part of the current session.
