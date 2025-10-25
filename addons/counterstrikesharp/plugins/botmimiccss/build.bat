@echo off
chcp 65001
cd /d "%~dp0"
dotnet build BotMimicCSS.csproj -c Release
pause


