@echo off
echo ========================================
echo Build Mint Paint (requires .NET)
echo ========================================
echo.
echo NOTE: Use build_standalone.bat for version without .NET requirement
echo.

echo [1/4] Building main app...
cd mint.paint
dotnet build -c Release
cd ..
echo Done
echo.

echo [2/4] Creating installer folder...
if not exist MintPaint.Installer\bin\Release\net8.0-windows\Files mkdir MintPaint.Installer\bin\Release\net8.0-windows\Files
echo Done
echo.

echo [3/4] Copying files...
xcopy /E /I /Y mint.paint\bin\Release\net8.0-windows\* MintPaint.Installer\bin\Release\net8.0-windows\Files\
echo Done
echo.

echo [4/4] Building installer...
cd MintPaint.Installer
dotnet build -c Release
cd ..
echo Done
echo.

echo ========================================
echo SUCCESS! Installer ready:
echo MintPaint.Installer\bin\Release\net8.0-windows\MintPaint.Installer.exe
echo ========================================
pause
