![SalsaNOW_Banner](https://salsanowfiles.work/RepoImages/SalsaNOW_Banner.png)

**Updater exe: https://salsa.salsanowfiles.work/**

**Documentation Website: https://documentation.salsanowfiles.work**

**Ko-fi Link: https://ko-fi.com/dpadguy**

**Discord Server: https://discord.gg/ZZDAyUC9a4**

Project can contain and rely on closed source, proprietary software, SalsaNOW is fully open source.

Pull requests are disabled at the moment.

## Building

Requires the .NET 8 SDK (or Visual Studio 2022 with the .NET desktop workload). SalsaNOW itself targets .NET Framework 4.8, which every GeForce NOW machine has.

```
dotnet build SalsaNOW.sln -c Release
dotnet test SalsaNOW.Tests
```

The build produces a single `SalsaNOW\bin\Release\net48\SalsaNOW.exe` (dependencies are embedded with Costura). `publish.bat` builds it and copies it to `publish\`.

## Settings

Besides the options the documentation describes, `SalsaNOWSettings.json` accepts:

| Key | Default | Effect |
| --- | --- | --- |
| `persistUserFolders` | `true` | Keeps Downloads, Pictures, Videos and Music on the SalsaNOW drive (`I:\Downloads` and so on) and sends temporary files to `I:\Temp`, so they survive the session and don't fill up C:. `false` leaves them on C:. |
| `disabledSilentApps` | none | Silent apps to skip, by name or file name, for example `["CtrlTab", "WindowsMenuToggle"]`. |
| `ctrlWinApp` | none | Program, shortcut or script that Ctrl+Win opens instead of the Start menu. |

`C:\Users\kiosk\Saved Games` is always kept with the other game saves.

## Project layout

| Project | Contents |
| --- | --- |
| `SalsaNOW` | The app: session setup (`SalsaSession`), installers (`Apps`, `Desktop`), Windows integration (`Shell`, `Shortcuts`, `Persistence`, `Steam`) and shared infrastructure (`Net`, `Infrastructure`). |
| `SalsaNOW.Core` | Platform-independent logic: the JSON formats, version checks, Steam config edits, path rules. |
| `SalsaNOW.Tests` | Unit tests for `SalsaNOW.Core`. |
