@echo off
REM ===== RBLX Spoofer build script =====
REM Compiles SpooferApp.cs into RobloxSpoofer.exe using the .NET Framework
REM C# compiler that ships with every Windows 10/11 install. No SDK needed.

setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo Could not find the .NET Framework C# compiler ^(csc.exe^).
    echo It ships with Windows - make sure .NET Framework 4.x is enabled.
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /out:RobloxSpoofer.exe /win32icon:logo.ico ^
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll ^
    SpooferApp.cs

if %errorlevel%==0 (
    echo.
    echo Build OK  -^>  RobloxSpoofer.exe
) else (
    echo.
    echo Build FAILED
)
pause
