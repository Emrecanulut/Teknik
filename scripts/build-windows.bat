@echo off
setlocal
cd /d "%~dp0.."

where msbuild >nul 2>&1
if errorlevel 1 (
  echo MSBuild bulunamadi. Visual Studio "Developer Command Prompt" acin.
  exit /b 1
)

echo === AutoGBT Windows derleme ===
echo.
echo [1/2] Core + UI + Desktop WinForms uygulamasi...
msbuild src\AutoGBT.Desktop\AutoGBT.Desktop.csproj /p:Configuration=Release /p:Platform=x64 /restore /m
if errorlevel 1 exit /b 1

echo.
echo Desktop hazir:
echo   %cd%\src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe
echo.
echo [2/2] SolidWorks eklentisi...
msbuild src\AutoGBT.Addin\AutoGBT.Addin.csproj /p:Configuration=Release /p:Platform=x64 /restore /m
if errorlevel 1 (
  echo Eklenti derlenemedi. SolidWorks API DLL yolu:
  echo   C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist
  exit /b 1
)

echo.
echo Eklenti hazir. Kurulum: scripts\install-addin.bat ^(yonetici^)
endlocal
