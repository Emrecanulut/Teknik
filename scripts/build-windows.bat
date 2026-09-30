@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

call "%~dp0find-msbuild.bat"
if "%MSBUILD%"=="" (
  echo.
  echo [HATA] MSBuild bulunamadi.
  echo.
  echo Bu bilgisayarda henuz derleyici yok. Sunlardan BIRINI kurun:
  echo.
  echo  A^) Visual Studio 2022 Community ^(onerilen^)
  echo     https://visualstudio.microsoft.com/tr/downloads/
  echo     Kurulumda workload: ".NET masaustu gelistirme" / ".NET desktop development"
  echo.
  echo  B^) Build Tools for Visual Studio 2022 ^(daha hafif^)
  echo     https://visualstudio.microsoft.com/tr/downloads/#build-tools-for-visual-studio-2022
  echo     Ayni workload: ".NET desktop development"
  echo.
  echo  C^) .NET Framework 4.8 Developer Pack
  echo     https://dotnet.microsoft.com/download/dotnet-framework/net48
  echo.
  echo Kurulum bitince BU pencereyi kapatip scripts\kur-hepsini.bat dosyasini tekrar calistirin.
  echo Developer Command Prompt acmaniza gerek yok; script MSBuild'i kendi bulur.
  echo.
  exit /b 1
)

echo === AutoGBT Windows derleme ===
echo MSBuild: %MSBUILD%
echo.

set "DESKTOP_OK=0"
set "ADDIN_OK=0"

echo [1/2] Core + UI + Desktop...
if /I "%MSBUILD%"=="dotnet" (
  dotnet build "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" -c Release -p:Platform=x64
) else (
  "%MSBUILD%" "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" /p:Configuration=Release /p:Platform=x64 /restore /m /v:m
)
if errorlevel 1 (
  echo.
  echo [HATA] Desktop derlenemedi.
  echo .NET Framework 4.8 Developer Pack kurulu mu kontrol edin:
  echo   https://dotnet.microsoft.com/download/dotnet-framework/net48
  echo Visual Studio Installer -^> Modify -^> ".NET desktop development" isaretli olmali.
  exit /b 1
)
set "DESKTOP_OK=1"
echo Desktop hazir.

set "SWAPI=C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
if not exist "%SWAPI%\SolidWorks.Interop.sldworks.dll" (
  echo.
  echo [2/2] SolidWorks API bulunamadi — eklenti atlandi.
  echo Masaustu uygulama yine de kullanilabilir.
  echo SolidWorks kuruluysa API yolu:
  echo   %SWAPI%
  goto :summary
)

echo.
echo [2/2] SolidWorks eklentisi...
if /I "%MSBUILD%"=="dotnet" (
  dotnet build "src\AutoGBT.Addin\AutoGBT.Addin.csproj" -c Release -p:Platform=x64
) else (
  "%MSBUILD%" "src\AutoGBT.Addin\AutoGBT.Addin.csproj" /p:Configuration=Release /p:Platform=x64 /restore /m /v:m
)
if errorlevel 1 (
  echo Eklenti derlenemedi; Desktop yine kullanilabilir.
  goto :summary
)
set "ADDIN_OK=1"
echo Eklenti hazir. Kurulum: scripts\install-addin.bat ^(yonetici^)

:summary
echo.
echo === Ozet ===
if "%DESKTOP_OK%"=="1" (
  echo  Desktop: OK
  echo    %cd%\src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe
) else (
  echo  Desktop: BASARISIZ
)
if "%ADDIN_OK%"=="1" (
  echo  Add-in:  OK
) else (
  echo  Add-in:  atlandi / basarisiz
)
echo.
if "%DESKTOP_OK%"=="1" exit /b 0
exit /b 1
endlocal
