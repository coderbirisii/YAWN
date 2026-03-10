@echo off
cd /d C:\Users\Coder\Desktop\YAWN

:: -----------------------------
:: Git ayarları (coderbirisii)
:: -----------------------------
git config user.name "coderbirisii"
git config user.email "coderbirisii@example.com"

:: -----------------------------
:: Tüm değişiklikleri ekle
:: -----------------------------
git add -A

:: Commit
git commit -m "Update all changes"

:: Push main branch
git push origin main

:: -----------------------------
:: Versiyon numarasını belirle
:: -----------------------------
set VERSION=1.3.8
git tag %VERSION%
git push origin %VERSION%

:: -----------------------------
:: Installer kontrolü
:: -----------------------------
if not exist "inno\out\YAWN-Installer.exe" (
    echo Installer dosyasi bulunamadi. Lütfen Inno Setup ile derleyin.
    pause
    exit /b
)

:: -----------------------------
:: GitHub release yarat
:: -----------------------------
gh release create %VERSION% --title "YAWN v%VERSION%" --notes "YAWN application v%VERSION% release" "inno/out/YAWN-Installer.exe"

echo.
echo Tum islemler tamamlandi!
pause