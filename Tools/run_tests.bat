@echo off
rem Runs every EditMode test headless (same checks as the Unity Test Runner) and prints the summary.
rem Results: Logs\harness\editmode_results.json and .log
rem Usage: Tools\run_tests.bat [path\to\Unity.exe]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_editmode_tests.ps1" %*
exit /b %ERRORLEVEL%
