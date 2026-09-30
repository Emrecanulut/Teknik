@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

echo ============================================
echo  AutoGBT - Tek adim kurulum
echo ============================================
echo.

where git >nul 2>&1
if errorlevel 1 (
  echo [!] Git bulunamadi. https://git-scm.com/download/win
  echo     ZIP indirdiyseniz bu script proje kokunden calisir.
  echo.
)

echo [1/4] Derleniyor...
call "%~dp0build-windows.bat"
if errorlevel 1 (
  echo.
  echo [HATA] Derleme basarisiz. Visual Studio 2022 + .NET Framework 4.8 gerekli.
  pause
  exit /b 1
)

echo.
echo [2/4] Masaustu uygulama baslatiliyor...
start "" "%~dp0run-desktop.bat"

echo.
echo [3/4] SolidWorks eklentisi?
echo     SolidWorks kuruluysa Y, degilse N yazin.
set /p SW="SolidWorks eklentisini kur? [Y/N]: "
if /I "%SW%"=="Y" (
  echo SolidWorks'u kapatip Enter'a basin...
  pause >nul
  powershell -Command "Start-Process -FilePath '%~dp0install-addin.bat' -Verb RunAs -Wait"
  echo.
  echo Sonra: SolidWorks -^> Tools -^> Add-ins -^> AutoGBT Teknik Resim
)

echo.
echo [4/4] Web stüdyo (opsiyonel onizleme)
where npm >nul 2>&1
if errorlevel 1 (
  echo npm yok - web stüdyo atlandi. Node.js kurarsaniz: cd demo ^&^& npm install ^&^& npm run dev
) else (
  set /p WEB="Web stüdyoyu ac? [Y/N]: "
  if /I "%WEB%"=="Y" (
    pushd demo
    if not exist node_modules call npm install
    start "AutoGBT Web" cmd /k npm run dev -- --host 127.0.0.1 --port 43123
    popd
    timeout /t 3 >nul
    start "" "http://127.0.0.1:43123"
  )
)

echo.
echo ============================================
echo  Bitti.
echo  - Desktop: AutoGBT.Desktop acildi
echo  - Cizimler (eklenti): AutoGBT_Drawings klasoru
echo  - Repo: https://github.com/Emrecanulut/Teknik
echo ============================================
pause
endlocal
