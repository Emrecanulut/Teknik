@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

call "%~dp0find-msbuild.bat"
if "%MSBUILD%"=="" (
  echo.
  echo [HATA] MSBuild bulunamadi.
  echo Visual Studio Installer -^> Modify -^> ".NET desktop development" isaretleyin.
  echo Sonra scripts\tani.bat calistirip raporu paylasin.
  exit /b 1
)

echo === AutoGBT Windows derleme ===
echo MSBuild: %MSBUILD%
echo.

REM Prefer AnyCPU first (matches VS default toolbar "Any CPU")
set "PLAT=AnyCPU"
set "DESKTOP_OK=0"
set "ADDIN_OK=0"

echo [1/2] Desktop (Core + UI + WinForms)...
if /I "%MSBUILD%"=="dotnet" (
  dotnet build "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" -c Release -p:Platform=%PLAT%
) else (
  "%MSBUILD%" "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" /p:Configuration=Release /p:Platform=%PLAT% /restore /m /v:m
)
if errorlevel 1 (
  echo AnyCPU basarisiz, x64 deneniyor...
  set "PLAT=x64"
  if /I "%MSBUILD%"=="dotnet" (
    dotnet build "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" -c Release -p:Platform=x64
  ) else (
    "%MSBUILD%" "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" /p:Configuration=Release /p:Platform=x64 /restore /m /v:m
  )
)
if errorlevel 1 (
  echo.
  echo [HATA] Desktop derlenemedi. scripts\tani.bat calistirin ve raporu gonderin.
  exit /b 1
)
set "DESKTOP_OK=1"

for %%E in (
  "src\AutoGBT.Desktop\bin\Release\net48\AutoGBT.Desktop.exe"
  "src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe"
  "src\AutoGBT.Desktop\bin\AnyCPU\Release\net48\AutoGBT.Desktop.exe"
) do if exist "%%~E" set "DESKTOP_EXE=%%~fE"

echo Desktop OK: %DESKTOP_EXE%

set "SWAPI=C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
if not exist "%SWAPI%\SolidWorks.Interop.sldworks.dll" (
  echo.
  echo [2/2] SolidWorks API yok — eklenti atlandi. Desktop kullanabilirsiniz.
  goto :summary
)

echo.
echo [2/2] SolidWorks eklentisi...
if /I "%MSBUILD%"=="dotnet" (
  dotnet build "src\AutoGBT.Addin\AutoGBT.Addin.csproj" -c Release -p:Platform=x64
) else (
  "%MSBUILD%" "src\AutoGBT.Addin\AutoGBT.Addin.csproj" /p:Configuration=Release /p:Platform=x64 /restore /m /v:m
)
if not errorlevel 1 set "ADDIN_OK=1"

:summary
echo.
echo === Ozet ===
echo  Desktop: OK
if defined DESKTOP_EXE echo    %DESKTOP_EXE%
if "%ADDIN_OK%"=="1" (echo  Add-in: OK) else (echo  Add-in: atlandi)
echo.
echo Visual Studio'da: Solution Explorer -^> AutoGBT.Desktop -^> sag tik -^> Set as Startup Project -^> F5
exit /b 0
endlocal
