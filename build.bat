@echo off
title NOWT Proje Derleyici
echo Proje derleniyor...
echo.

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

echo.
if %errorlevel% neq 0 (
    echo [HATA] Derleme sirasinda bir sorun olustu!
) else (
    echo [BASARILI] Derleme islemi tamamlandi.
)

exit
