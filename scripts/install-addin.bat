@echo off
setlocal
REM AutoGBT SolidWorks eklentisini COM olarak kaydeder (Yonetici olarak calistirin).
set ADDIN=%~dp0..\src\AutoGBT.Addin\bin\x64\Release\net48\AutoGBT.Addin.dll

if not exist "%ADDIN%" (
  echo Once scripts\build-windows.bat ile derleyin.
  echo Eksik: %ADDIN%
  exit /b 1
)

set REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
"%REGASM%" "%ADDIN%" /codebase
if errorlevel 1 (
  echo RegAsm basarisiz. Yonetici yetkisiyle tekrar deneyin.
  exit /b 1
)

echo AutoGBT kaydedildi.
echo SolidWorks ^> Tools ^> Add-ins ^> "AutoGBT Teknik Resim" etkinlestirin.
echo.
echo Windows onizleme icin:
echo   src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe
endlocal
