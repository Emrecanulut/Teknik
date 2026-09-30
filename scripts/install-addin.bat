@echo off
setlocal
REM AutoGBT SolidWorks eklentisini COM olarak kaydeder (Yonetici olarak calistirin).
set ADDIN=%~dp0..\src\AutoGBT.Addin\bin\x64\Release\net48\AutoGBT.Addin.dll

if not exist "%ADDIN%" (
  echo Once projeyi Release^|x64 olarak derleyin.
  echo Eksik: %ADDIN%
  exit /b 1
)

set REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
"%REGASM%" "%ADDIN%" /codebase
if errorlevel 1 (
  echo RegAsm basarisiz. Visual Studio Developer Command Prompt veya yonetici yetkisi deneyin.
  exit /b 1
)

echo AutoGBT kaydedildi. SolidWorks Tools ^> Add-ins icinden "AutoGBT Teknik Resim" etkinlestirin.
endlocal
