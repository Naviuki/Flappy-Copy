@echo off
rem Builds the Windows player to Builds\Windows\FlappyCopy.exe (batchmode, no graphics).
setlocal
set "UNITY_EXE=%~1"
if "%UNITY_EXE%"=="" if defined UNITY_EXE set "UNITY_EXE=%UNITY_EXE%"
if "%UNITY_EXE%"=="" set "UNITY_EXE=D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe"
set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"
set "LOG=%TEMP%\flappycopy_build.log"
"%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT%" -executeMethod FlappyCopy.EditorTools.CiEntry.BuildWindowsCi -logFile "%LOG%" -quit
set RESULT=%ERRORLEVEL%
echo Unity exit code: %RESULT%   (log: %LOG%)
findstr /C:"Build succeeded" /C:"Build failed" /C:"error CS" "%LOG%"
exit /b %RESULT%