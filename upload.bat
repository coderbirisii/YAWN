@echo off
cd /d C:\Users\Coder\Desktop\YAWN

:: Repo bazlı user/email ayarı (coderbirisii)
git config user.name "coderbirisii"
git config user.email "coderbirisii@example.com"

:: Tüm değişiklikleri ekle
git add -A

:: Commit
git commit -m "Update all changes"

:: Push
git push origin main

echo Done.
pause