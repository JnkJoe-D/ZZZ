@echo off
set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Configs\Tool\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t all ^
    -d json ^
    -c cs-simple-json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputDataDir=%WORKSPACE%\Assets\Configs ^
    -x outputCodeDir=%WORKSPACE%\Assets\GameClient\Generate\Luban\Config

pause