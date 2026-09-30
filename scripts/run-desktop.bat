@echo off
setlocal
set "ROOT=%~dp0.."
set "EXE="
for %%E in (
  "%ROOT%\src\AutoGBT.Desktop\bin\Release\net48\AutoGBT.Desktop.exe"
  "%ROOT%\src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe"
  "%ROOT%\src\AutoGBT.Desktop\bin\AnyCPU\Release\net48\AutoGBT.Desktop.exe"
  "%ROOT%\src\AutoGBT.Desktop\bin\Debug\net48\AutoGBT.Desktop.exe"
  "%ROOT%\src\AutoGBT.Desktop\bin\x64\Debug\net48\AutoGBT.Desktop.exe"
) do if exist "%%~E" if not defined EXE set "EXE=%%~fE"

if not defined EXE (
  echo AutoGBT.Desktop.exe yok. Once:
  echo   scripts\build-windows.bat
  echo veya Visual Studio'da AutoGBT.Desktop -^> F5
  exit /b 1
)
echo Baslatiliyor: %EXE%
start "" "%EXE%"
endlocal
