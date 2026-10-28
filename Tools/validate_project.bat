@echo off
rem Validates the scene wiring, prefabs, project settings and input handler.
setlocal
set "UNITY_EXE=%~1"
if "%UNITY_EXE%"=="" if defined UNITY_EXE set "UNITY_EXE=%UNITY_EXE%"
if "%UNITY_EXE%"=="" set "UNITY_EXE=D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe"
set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"
set "LOG=%TEMP%\flappycopy_validate.log"
"%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT%" -executeMethod FlappyCopy.EditorTools.ProjectValidator.RunAndExit -logFile "%LOG%" -quit
set RESULT=%ERRORLEVEL%
findstr /C:"[FlappyCopy]" "%LOG%"
exit /b %RESULT%