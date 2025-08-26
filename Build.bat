@echo off
setlocal

rem -- ê›íË --------------------------------------------------------
set "CSproj=%~dp0LazyStateMachine\LazyStateMachine.csproj"
set "UnityProject=%~dp0..\ToReturn\ToReturn"
set "PluginDir=%UnityProject%\Assets\StateMachine"
set "Configuration=Release"
set "Framework=netstandard2.0"
set "DotNet=dotnet"
rem ---------------------------------------------------------------

if not exist "%PluginDir%" mkdir "%PluginDir%"

%DotNet% build "%CSproj%" -c %Configuration% -p:TargetFramework=%Framework% ^
    -p:GeneratePackageOnBuild=false -p:CopyLocalLockFileAssemblies=true ^
    --nologo --verbosity:minimal -o "%PluginDir%"
if errorlevel 1 (
    echo [FAILED] Build error
    exit /b 1
)

echo [SUCCESS] DLL built for Unity
endlocal
pause
