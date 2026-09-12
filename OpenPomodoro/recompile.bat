@echo off
setlocal

set "PROJECT=%~dp0OpenPomodoro.csproj"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "%VSWHERE%" (
    echo ERROR: vswhere.exe was not found. Install Visual Studio with MSBuild support.
    exit /b 1
)

for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%I"

if not defined MSBUILD (
    echo ERROR: MSBuild was not found.
    exit /b 1
)

echo Recompiling OpenPomodoro in Release mode...
"%MSBUILD%" "%PROJECT%" /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /m

if errorlevel 1 (
    echo Recompile failed.
    exit /b 1
)

echo Recompile succeeded: %~dp0bin\Release\OpenPomodoro.exe
exit /b 0
