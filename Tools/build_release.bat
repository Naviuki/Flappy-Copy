@echo off
rem Cuts the Windows release: validates the scene/assets, builds the player, stages Builds\Release
rem (player + README + LICENSE + VERSION with SHA-256) and zips it to Builds\FlappyCopy-Windows.zip.
setlocal
set "UNITY_EXE=%~1"
if "%UNITY_EXE%"=="" if defined UNITY_EXE set "UNITY_EXE=%UNITY_EXE%"
if "%UNITY_EXE%"=="" set "UNITY_EXE=D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe"
set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"
set "LOG=%TEMP%\flappycopy_release.log"
"%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT%" -executeMethod FlappyCopy.EditorTools.ReleaseBuilder.BuildAndExit -logFile "%LOG%" -quit
set RESULT=%ERRORLEVEL%
echo Unity exit code: %RESULT%   (log: %LOG%)
findstr /C:"Release staged" /C:"Release stopped" /C:"Release:" /C:"Build succeeded" /C:"Build failed" /C:"error CS" "%LOG%"
exit /b %RESULT%
