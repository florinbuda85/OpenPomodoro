# OpenPomodoro
simple pomodoro program

[![Build status](https://ci.appveyor.com/api/projects/status/github/florinbuda85/openpomodoro?svg=true)](https://ci.appveyor.com/project/florinbuda85/OpenPomodoro) [![Release](https://img.shields.io/github/release/florinbuda85/openpomodoro.svg?label=Release&maxAge=60)](https://github.com/florinbuda85/openpomodoro/releases/latest)

[![Screenshot](https://publicshared6.s3.amazonaws.com/printscreen.png)](https://github.com/florinbuda85/openpomodoro/releases/latest)

## Local build

Run `rebuild.bat` to rebuild Debug, or `rebuild.bat Release` for Release.
Close the running app first. The script locates the Visual Studio C# compiler
(including Insiders), uses .NET Framework MSBuild, and requires the .NET Framework
4.8 targeting pack and the restored `packages` directory.
Run `rebuild.bat /check` to check the build tools without compiling.
The executable is written to `OpenPomodoro/bin/Debug` or `OpenPomodoro/bin/Release`.

## Planned features

- Open sounds folder
- Ask what is the plan
- Display pomodoro number
