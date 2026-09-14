@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "POMODORO_CONFIGURATION=Debug"
set "POMODORO_CHECK_ONLY="

:arguments
if "%~1"=="" goto find_tools
if /i "%~1"=="Debug" (
    set "POMODORO_CONFIGURATION=Debug"
    shift /1
    goto arguments
)
if /i "%~1"=="Release" (
    set "POMODORO_CONFIGURATION=Release"
    shift /1
    goto arguments
)
if /i "%~1"=="/check" (
    set "POMODORO_CHECK_ONLY=1"
    shift /1
    goto arguments
)
echo Usage: rebuild.bat [Debug^|Release] [/check]
exit /b 1

:find_tools
set "POMODORO_MSBUILD=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if not exist "%POMODORO_MSBUILD%" set "POMODORO_MSBUILD=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if not exist "%POMODORO_MSBUILD%" (
    echo ERROR: .NET Framework MSBuild was not found.
    exit /b 1
)

set "POMODORO_VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%POMODORO_VSWHERE%" (
    echo ERROR: Visual Studio Installer was not found. Install Visual Studio with C# build tools.
    exit /b 1
)
set "POMODORO_COMPILER_DIR="
for /f "usebackq delims=" %%I in (`"%POMODORO_VSWHERE%" -latest -prerelease -products * -find MSBuild\Current\Bin\Roslyn\csc.exe`) do set "POMODORO_COMPILER_DIR=%%~dpI"
if not defined POMODORO_COMPILER_DIR (
    echo ERROR: The Visual Studio C# compiler was not found.
    exit /b 1
)
rem Remove the trailing backslash before passing this directory as a quoted argument.
set "POMODORO_COMPILER_DIR=%POMODORO_COMPILER_DIR:~0,-1%"
set "POMODORO_FACADES=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades"
if not exist "%POMODORO_FACADES%\netstandard.dll" (
    echo ERROR: Install the .NET Framework 4.8 targeting pack.
    exit /b 1
)
if not exist "%~dp0build\FrameworkCompatibility.targets" (
    echo ERROR: build\FrameworkCompatibility.targets is missing.
    exit /b 1
)

echo MSBuild: %POMODORO_MSBUILD%
echo C# compiler: %POMODORO_COMPILER_DIR%\csc.exe
if defined POMODORO_CHECK_ONLY (
    echo Build tools are ready. No compilation was performed.
    exit /b 0
)

echo Rebuilding OpenPomodoro in %POMODORO_CONFIGURATION% mode...
"%POMODORO_MSBUILD%" "%~dp0OpenPomodoro.sln" /t:Rebuild /p:Configuration=%POMODORO_CONFIGURATION% /p:Platform="Any CPU" "/p:CscToolPath=%POMODORO_COMPILER_DIR%" /p:CscToolExe=csc.exe "/p:CustomAfterMicrosoftCommonTargets=%~dp0build\FrameworkCompatibility.targets" "/p:PomodoroFrameworkFacades=%POMODORO_FACADES%" /nologo /v:minimal
set "POMODORO_BUILD_RESULT=%ERRORLEVEL%"
if not "%POMODORO_BUILD_RESULT%"=="0" (
    echo Build failed. Review the errors above. Close OpenPomodoro if its executable is locked.
    exit /b %POMODORO_BUILD_RESULT%
)
echo Build succeeded: %~dp0OpenPomodoro\bin\%POMODORO_CONFIGURATION%\OpenPomodoro.exe
exit /b 0
