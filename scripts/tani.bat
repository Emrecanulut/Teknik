@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "LOG=%USERPROFILE%\Desktop\AutoGBT-tani.txt"
if not exist "%USERPROFILE%\Desktop" set "LOG=%USERPROFILE%\AutoGBT-tani.txt"

echo AutoGBT tani raporu > "%LOG%"
echo Tarih: %DATE% %TIME% >> "%LOG%"
echo Klasor: %CD% >> "%LOG%"
echo. >> "%LOG%"

echo === Git === >> "%LOG%"
where git >nul 2>&1 && (git rev-parse --short HEAD >> "%LOG%" 2>&1) || echo git yok >> "%LOG%"
echo. >> "%LOG%"

echo === MSBuild / dotnet === >> "%LOG%"
call "%~dp0find-msbuild.bat"
echo MSBUILD=%MSBUILD% >> "%LOG%"
where dotnet >nul 2>&1 && (dotnet --info >> "%LOG%" 2>&1) || echo dotnet yok >> "%LOG%"
echo. >> "%LOG%"

echo === SolidWorks API === >> "%LOG%"
set "SWAPI=C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll"
if exist "%SWAPI%" (echo VAR: %SWAPI% >> "%LOG%") else (echo YOK: %SWAPI% >> "%LOG%")
echo. >> "%LOG%"

echo === Desktop derleme denemesi === >> "%LOG%"
if "%MSBUILD%"=="" (
  echo MSBuild bulunamadi >> "%LOG%"
) else if /I "%MSBUILD%"=="dotnet" (
  dotnet build "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" -c Release -p:Platform=AnyCPU >> "%LOG%" 2>&1
) else (
  "%MSBUILD%" "src\AutoGBT.Desktop\AutoGBT.Desktop.csproj" /p:Configuration=Release /p:Platform=AnyCPU /restore /m /v:n >> "%LOG%" 2>&1
)
echo EXITCODE=%ERRORLEVEL% >> "%LOG%"
echo. >> "%LOG%"

echo === Exe arama === >> "%LOG%"
dir /s /b "src\AutoGBT.Desktop\bin\*\AutoGBT.Desktop.exe" >> "%LOG%" 2>&1

echo.
echo Rapor yazildi:
echo   %LOG%
echo.
echo Bu dosyayi acip icerigini buraya yapistirin.
notepad "%LOG%"
pause
endlocal
