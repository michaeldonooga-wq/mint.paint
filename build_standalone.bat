@echo off
echo ========================================
echo Building Standalone Mint Paint
echo (includes .NET Runtime - no installation needed)
echo ========================================
echo.

echo [1/2] Building standalone version...
cd mint.paint
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
cd ..
echo Done
echo.

echo [2/2] Copying to installer Files folder...
if exist "MintPaint.Installer\bin\Release\net8.0-windows\Files" rmdir /S /Q "MintPaint.Installer\bin\Release\net8.0-windows\Files"
mkdir "MintPaint.Installer\bin\Release\net8.0-windows\Files"
xcopy /E /I /Y "mint.paint\bin\Release\net8.0-windows\win-x64\publish\*" "MintPaint.Installer\bin\Release\net8.0-windows\Files\"
echo Done
echo.

echo [3/3] Building installer...
cd MintPaint.Installer
dotnet build -c Release
cd ..
echo Done
echo.

echo ========================================
echo SUCCESS! Standalone installer ready:
echo MintPaint.Installer\bin\Release\net8.0-windows\MintPaint.Installer.exe
echo.
echo Size: ~80-100 MB (includes .NET Runtime)
echo No .NET installation required!
echo ========================================
pause
