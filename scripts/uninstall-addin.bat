@echo off
setlocal
set ADDIN=%~dp0..\src\AutoGBT.Addin\bin\x64\Release\net48\AutoGBT.Addin.dll
set REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
if exist "%ADDIN%" "%REGASM%" "%ADDIN%" /unregister
echo AutoGBT kaydi silindi.
endlocal
