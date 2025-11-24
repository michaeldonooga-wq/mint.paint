@echo off
echo Creating self-extracting installer...
echo.

echo Step 1: Install 7-Zip if not installed
echo Download from: https://www.7-zip.org/download.html
echo.

echo Step 2: Run this after installing 7-Zip:
echo.
echo cd MintPaint.Installer\bin\Release\net8.0-windows
echo "C:\Program Files\7-Zip\7z.exe" a -sfx7z.sfx MintPaint_Setup.exe MintPaint.Installer.exe Files\
echo.

pause
