@echo off
REM Sets MSBUILD to a usable msbuild.exe path, or leaves it empty.
set "MSBUILD="

where msbuild >nul 2>&1
if not errorlevel 1 (
  for /f "delims=" %%I in ('where msbuild') do (
    set "MSBUILD=%%I"
    goto :eof
  )
)

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
  for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    set "MSBUILD=%%I"
    goto :eof
  )
)

for %%P in (
  "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
  "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
  "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
  "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
  "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
) do (
  if exist %%~P (
    set "MSBUILD=%%~P"
    goto :eof
  )
)

where dotnet >nul 2>&1
if not errorlevel 1 (
  set "MSBUILD=dotnet"
)
