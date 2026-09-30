@echo off
set EXE=%~dp0..\src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe
if not exist "%EXE%" set EXE=%~dp0..\src\AutoGBT.Desktop\bin\Release\net48\AutoGBT.Desktop.exe
if not exist "%EXE%" (
  echo Once scripts\build-windows.bat calistirin.
  exit /b 1
)
start "" "%EXE%"
