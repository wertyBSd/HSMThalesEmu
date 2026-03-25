# HSMThalesEmu
Thales HSM emulator — локальная реализация серверной логики и набор хостов/клиентов для разработки и интеграционного тестирования.

**Содержание репозитория**
- **ThalesCore/**: основная библиотека с реализацией команд, парсером сообщений и криптографией.
- **ThalesService/**: библиотека, реализующая BackgroundService (TCP-сервер) — сервисная логика вынесена в библиотеку.
- **ThalesService.Hosts.Console/**: консольный хост, запускающий `ThalesTcpService` в `IHost` для локального запуска.
- **ThalesService.Hosts.WindowsService/**: Windows Service хост (использует `UseWindowsService`).
- **ThalesService.Hosts.UI/**: WinForms хост для интерактивного запуска сервиса с минимальным UI.
- **ThalesService.IntegrationTests/**: интеграционные тесты (NUnit). Тесты запускают хост в процессе и используют эпемерные порты.
- **ThalesCore.Tests/**: unit-тесты для `ThalesCore` (NUnit).
- **ThalesClients/ConsoleClient/**: простой консольный TCP-клиент (не включён в .sln по требованию).
- **ThalesClients/UIClient/**: простой WinForms клиент с чекбоксами/списками (не включён в .sln).

**Ключевые концепции**
- Сервис читает порт из переменной окружения `THALES_SERVICE_PORT`. Для интеграционных тестов используется свободный (ephemeral) порт, который прописывается в окружение теста.
- Интеграционные тесты создают `Host.CreateDefaultBuilder()` и регистрируют `AddHostedService<ThalesTcpService>()`, чтобы запустить сервис в процессе теста и избежать проблем с межпроцессными гонками.
- Парсер сообщений укреплён — теперь возвращает код отказа (например, `80`) при недостающих данных вместо выброса исключения.

**Сборка**
1. Установите .NET SDK 8.x (рекомендуется последнее стабильное).
2. В корне репозитория выполните:

```bash
dotnet build ThalesCore/ThalesCore.csproj -c Debug
dotnet build ThalesService/ThalesService.csproj -c Debug
dotnet build ThalesService.IntegrationTests/ThalesService.IntegrationTests.csproj -c Debug
```

Или собрать всё решение:

```bash
dotnet build ThalesEmu.sln -c Debug
```

**Запуск хостов**
- Консольный хост (локальный запуск сервиса):

```bash
cd ThalesService.Hosts.Console/bin/Debug/net8.0
dotnet ThalesService.Hosts.Console.dll
```

- Windows Service хост: соберите `ThalesService.Hosts.WindowsService` и установите как сервис (например `sc create ...`), либо используйте `sc`/PowerShell для установки.

- UI хост (WinForms):

```bash
cd ThalesService.Hosts.UI/bin/Debug/net8.0-windows
dotnet ThalesService.Hosts.UI.dll
```

По умолчанию сервис слушает порт `1500`, но для тестов и локального запуска можно указать другой порт через переменную окружения `THALES_SERVICE_PORT`.

**Клиенты (не в решении)**
- `ThalesClients/ConsoleClient` — интерактивный консольный клиент. Запуск:

```bash
dotnet run --project ThalesClients/ConsoleClient/ThalesConsoleClient.csproj -- 127.0.0.1 1500
```

- `ThalesClients/UIClient` — простой WinForms UI-клиент. Запуск:

```bash
dotnet run --project ThalesClients/UIClient/ThalesUIClient.csproj
```

Эти проекты сознательно не добавлены в `.sln` — они служат как примеры-клиенты для разработки.

**Тесты**
- Запустить интеграционные тесты:

```bash
dotnet test ThalesService.IntegrationTests/ThalesService.IntegrationTests.csproj -c Debug
```

- Запустить все тесты решения:

```bash
dotnet test ThalesEmu.sln -c Debug
```

**Полезные ссылки по коду**
- Парсер сообщений: `ThalesCore/Message/XML/MessageParser.cs` — здесь реализованы проверки длины и логика разбора полей.
- Сервисный TCP-слушатель: `ThalesService/ThalesTcpService.cs` — обработка подключений и делегирование команд.
- Примеры команд: `ThalesCore/HostCommands/BuildIn` — несколько встроенных команд (например, `EchoTest_B2`).

**Отладка и советы**
- Если тесты не видны в Visual Studio Test Explorer, убедитесь, что:
	- в `Directory.Packages.props` указана совместимая версия `NUnit3TestAdapter` и `Microsoft.NET.Test.Sdk`;
	- проекты таргетят `net8.0` (или совместимую TFM) и совпадают с настройками Visual Studio.
- Для локальной отладки интеграционных тестов полезно запускать сервис вручную в консоли (см. выше) и отправлять запросы из `ThalesClients/ConsoleClient`.
- В репозитории остались предупреждения компиляции о устаревших API и неиспользуемых переменных — их можно постепенно устранять; они не влияют на корректность интеграционных тестов.

**Дальнейшие шаги (рекомендации)**
- Добавить README внутри каждого важного проекта (например, `ThalesService/README.md`) с локальными примерами запуска и специфичными параметрами.
- Настроить CI (GitHub Actions) для прогонки `dotnet build` и `dotnet test` на Windows runner, чтобы отловить платформо-зависимые проблемы.
- Расширить `ThalesClients/UIClient` реальными полями команд с соответствующими метками и валидацией.

Если хотите, я могу:
- добавить отдельные READMEs по проектам, или
- настроить простой GitHub Actions workflow для сборки и тестирования на Windows.

---
Автор: автоматизированный помощник — изменения и инструкции подготовлены в рамках текущего репозитория.
