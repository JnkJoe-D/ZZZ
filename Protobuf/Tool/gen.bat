@echo off
setlocal enabledelayedexpansion

:: 设置工作目录为脚本所在的绝对路径
cd /d "%~dp0"

:: ---------------------------------------------
:: 配置路径 (相对于本脚本所在目录 Tool)
:: ---------------------------------------------
set PROTOC=".\bin\protoc.exe"
set INCLUDE_DIR=".\include"
set PROTO_DIR="..\..\Assets\GameClient\Generate\Network\Proto"
set OUT_DIR="..\..\Assets\GameClient\Generate\Network\Protocol"

echo ===================================================
echo   [Protobuf 编译器] 开始生成 C# 协议代码...
echo ===================================================

:: 检查协议文件目录是否存在
if not exist "%PROTO_DIR%" (
    echo [错误] 找不到 proto 文件目录: %PROTO_DIR%
    pause
    exit /b 1
)

:: 确保存储生成代码的目录存在
if not exist "%OUT_DIR%" (
    mkdir "%OUT_DIR%"
    echo [提示] 创建了输出目录: %OUT_DIR%
)

:: 清空旧的生成文件，防冗余
del /q "%OUT_DIR%\*.cs" 2>nul

:: 遍历目录下的所有 .proto 文件
set count=0
for %%f in ("%PROTO_DIR%\*.proto") do (
    echo -- 正在编译: %%~nxf
    %PROTOC% --proto_path="%PROTO_DIR%" --proto_path="%INCLUDE_DIR%" --csharp_out="%OUT_DIR%" "%%f"
    
    if !errorlevel! neq 0 (
        echo [错误] 编译 %%~nxf 时发生异常！
        pause
        exit /b !errorlevel!
    )
    set /a count+=1
)

echo ===================================================
echo   恭喜！编译完成！共成功处理了 %count% 个协议文件。
echo   生成路径: Assets/GameClient/Generate/Network/Protocol
echo ===================================================
pause
