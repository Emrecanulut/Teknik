@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

echo ============================================
echo  AutoGBT - Tek adim kurulum
echo ============================================
echo.

echo [1/4] Derleniyor...
call "%~dp0build-windows.bat"
if errorlevel 1 (
  echo.
  echo [HATA] Derleme basarisiz.
  echo Yukaridaki indirme linklerinden Visual Studio / Build Tools kurun,
  echo sonra bu scripti tekrar calistirin.
  echo.
  pause
  exit /b 1
)

echo.
echo [2/4] Masaustu uygulama baslatiliyor...
call "%~dp0run-desktop.bat"
if errorlevel 1 (
  echo Desktop exe bulunamadi; once derleme basarili olmali.
)

echo.
echo [3/4] SolidWorks eklentisi
set "ADDIN_DLL=%cd%\src\AutoGBT.Addin\bin\x64\Release\net48\AutoGBT.Addin.dll"
if not exist "%ADDIN_DLL%" set "ADDIN_DLL=%cd%\src\AutoGBT.Addin\bin\Release\net48\AutoGBT.Addin.dll"
if not exist "%ADDIN_DLL%" (
  echo Eklenti derlenmedi — bu adim atlandi.
  goto :web
)

set /p SW="SolidWorks eklentisini kur? [Y/N]: "
if /I "%SW%"=="Y" (
  echo SolidWorks'u KAPATIN, sonra bu pencerede Enter'a basin...
  pause >nul
  powershell -Command "Start-Process -FilePath '%~dp0install-addin.bat' -Verb RunAs -Wait"
  echo Sonra: SolidWorks -^> Tools -^> Add-ins -^> AutoGBT Teknik Resim
)

:web
echo.
echo [4/4] Web stüdyo (opsiyonel)
where npm >nul 2>&1
if errorlevel 1 (
  echo npm yok — web atlandi. Node.js: https://nodejs.org
  goto :done
)
set /p WEB="Web stüdyoyu ac? [Y/N]: "
if /I "%WEB%"=="Y" (
  pushd demo
  if not exist node_modules call npm install
  start "AutoGBT Web" cmd /k npm run dev -- --host 127.0.0.1 --port 43123
  popd
  timeout /t 3 >nul
  start "" "http://127.0.0.1:43123"
)

:done
echo.
echo ============================================
echo  Bitti. Repo: https://github.com/Emrecanulut/Teknik
echo ============================================
pause
endlocal
