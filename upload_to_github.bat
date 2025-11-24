@echo off
echo ========================================
echo Uploading to GitHub
echo ========================================
echo.

echo [1/6] Configuring git...
git config user.email "michaeldonooga-wq@users.noreply.github.com"
git config user.name "michaeldonooga-wq"
echo Done
echo.

echo [2/6] Initializing git...
git init
echo Done
echo.

echo [3/6] Adding files...
git add .
echo Done
echo.

echo [4/6] Creating commit...
git commit -m "EARLY-ALPHA-RELEASE"
echo Done
echo.

echo [5/6] Setting remote...
git branch -M main
git remote add origin https://github.com/michaeldonooga-wq/mint.paint.git
echo Done
echo.

echo [6/6] Pushing to GitHub...
git push -u origin main
echo Done
echo.

echo ========================================
echo SUCCESS! Code uploaded to GitHub!
echo https://github.com/michaeldonooga-wq/mint.paint
echo.
echo Next steps:
echo 1. Go to GitHub Releases
echo 2. Create new release with tag: EARLY-ALPHA-RELEASE
echo 3. Upload the installer ZIP
echo ========================================
pause
