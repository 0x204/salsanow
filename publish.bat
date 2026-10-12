@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "PROJECT=%ROOT%SalsaNOW\SalsaNOW.csproj"
set "OUT=%ROOT%publish"
set "EXE=%ROOT%SalsaNOW\bin\Release\net48\SalsaNOW.exe"

if not exist "%PROJECT%" (
  echo Project not found: "%PROJECT%"
  exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet was not found. Install the .NET SDK or Visual Studio with the .NET desktop workload.
  exit /b 1
)

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%"

echo Publishing SalsaNOW (Release, x64, single-file)...
dotnet build "%PROJECT%" -c Release --nologo -v minimal
if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

if not exist "%EXE%" (
  echo Build finished but SalsaNOW.exe was not found:
  echo   %EXE%
  exit /b 1
)

copy /Y "%EXE%" "%OUT%\SalsaNOW.exe" >nul

echo.
echo Done: "%OUT%\SalsaNOW.exe"
start "" explorer.exe "%OUT%"
endlocal
