@echo off
rem Runs the EditMode tests through the Unity Test Runner API and prints the summary.
rem Results: Temp\harness\editmode_results.json and .log
rem Usage: Tools\run_tests.bat [path\to\Unity.exe]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_editmode_tests.ps1" %*
exit /b %ERRORLEVEL%
