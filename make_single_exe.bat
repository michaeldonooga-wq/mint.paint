@echo off
echo ========================================
echo Creating single EXE installer
echo ========================================
echo.

echo [1/2] Building installer with embedded files...
cd MintPaint.Installer
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
cd ..
echo Done
echo.

echo [2/2] Copying to output...
if not exist "Output" mkdir "Output"
copy "MintPaint.Installer\bin\Release\net8.0-windows\win-x64\publish\MintPaint.Installer.exe" "Output\MintPaint_Setup.exe"
xcopy /E /I /Y "MintPaint.Installer\bin\Release\net8.0-windows\Files" "Output\Files\"
echo Done
echo.

echo ========================================
echo SUCCESS! Single installer ready:
echo Output\MintPaint_Setup.exe
echo.
echo IMPORTANT: Distribute MintPaint_Setup.exe with Files folder!
echo ========================================
pause
