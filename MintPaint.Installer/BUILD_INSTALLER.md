# Как собрать установщик Mint Paint

## Шаг 1: Соберите основную программу

1. Откройте проект **mint.paint** в Visual Studio
2. Выберите **Release** (вместо Debug)
3. Нажмите **Build → Build Solution** (F6)
4. Файлы будут в: `mint.paint\bin\Release\net8.0-windows\`

## Шаг 2: Подготовьте файлы для установщика

1. Создайте папку: `MintPaint.Installer\bin\Release\net8.0-windows\Files\`
2. Скопируйте ВСЕ файлы из `mint.paint\bin\Release\net8.0-windows\` в папку `Files\`

## Шаг 3: Соберите установщик

1. Откройте проект **MintPaint.Installer** в Visual Studio
2. Выберите **Release**
3. Нажмите **Build → Build Solution**
4. Готовый установщик: `MintPaint.Installer\bin\Release\net8.0-windows\MintPaint.Installer.exe`

## Шаг 4: Распространение

Раздайте файл `MintPaint.Installer.exe` вместе с папкой `Files\` рядом с ним.

**Структура:**
```
MintPaint.Installer.exe
Files/
  ├── mint.paint.exe
  ├── mint.paint.dll
  ├── SkiaSharp.dll
  └── ... (все остальные файлы)
```

---

## Автоматизация (опционально)

Создайте BAT файл для автоматической сборки:

```batch
@echo off
echo Сборка Mint Paint...
cd mint.paint
dotnet build -c Release
cd ..

echo Копирование файлов...
xcopy /E /I /Y "mint.paint\bin\Release\net8.0-windows\*" "MintPaint.Installer\bin\Release\net8.0-windows\Files\"

echo Сборка установщика...
cd MintPaint.Installer
dotnet build -c Release
cd ..

echo Готово! Установщик: MintPaint.Installer\bin\Release\net8.0-windows\MintPaint.Installer.exe
pause
```

Сохраните как `build_all.bat` и запускайте для полной сборки.
