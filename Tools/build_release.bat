@echo off
rem Cuts the release archives for every platform this editor can build (Windows x64, Linux x64, WebGL and
rem Android when its module is installed): validates the project, builds each player and writes the zips and
rem the APK into Builds\Release together with VERSION.txt (versions and SHA-256 hashes).
rem
rem Usage: Tools\build_release.bat [path\to\Unity.exe]
setlocal
set "UNITY_EXE=%~1"
if "%UNITY_EXE%"=="" if defined UNITY_EXE set "UNITY_EXE=%UNITY_EXE%"
if "%UNITY_EXE%"=="" set "UNITY_EXE=D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe"
set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"
set "LOG=%TEMP%\flappycopy_release.log"
"%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT%" -executeMethod FlappyCopy.EditorTools.ReleaseBuilder.BuildAllAndExit -logFile "%LOG%" -quit
set RESULT=%ERRORLEVEL%
echo Unity exit code: %RESULT%   (log: %LOG%)
findstr /C:"Release:" /C:"Release finished" /C:"Release stopped" /C:"Build succeeded" /C:"Build failed" /C:"error CS" "%LOG%"
exit /b %RESULT%
