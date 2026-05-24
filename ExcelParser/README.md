# Excel Parser - WPF приложение для импорта показаний счетчиков

## Описание
Простое WPF приложение на C#, которое:
1. Читает данные из Excel файла
2. Извлекает показания счетчиков (ГВС и ХВС)
3. Генерирует SQL INSERT запросы
4. Отправляет данные в базу данных SQL Server

## Требования
- .NET 8.0 SDK
- Windows (для WPF)

## Установка

### 1. Установите .NET 8.0 SDK
Скачайте с официального сайта: https://dotnet.microsoft.com/download

### 2. Восстановите пакеты
```bash
cd ExcelParser
dotnet restore
```

### 3. Запустите приложение
```bash
dotnet run
```

Или опубликуйте как standalone приложение:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Использование

1. **Выберите Excel файл** - нажмите кнопку "Обзор..." и выберите ваш .xlsx файл
2. **Введите строку подключения** к SQL Server (по умолчанию: `Server=localhost;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True;`)
3. **Нажмите "Парсить Excel и выполнить INSERT в SQL"**

## Формат Excel файла

Приложение ожидает следующие колонки в Excel файле:
- **Первая колонка**: Номер квартиры (число)
- **"ГВС преды"**: Предыдущие показания горячей воды
- **"ГВС конеч"**: Текущие показания горячей воды  
- **"ХВС преды"**: Предыдущие показания холодной воды

Пример структуры Excel:
| Квартира | ФИО | Площадь | ГВС преды | ГВС конеч | ХВС преды | ... |
|----------|-----|---------|-----------|-----------|-----------|-----|
| 101 | Иванов А.А. | 54.5 | 120.5 | 125.3 | 80.2 | ... |
| 102 | Петров Б.Б. | 72.0 | 95.0 | 98.7 | 65.5 | ... |

## Структура проекта

```
ExcelParser/
├── AccrualChargeDto.cs      # Модель данных
├── App.xaml                  # Приложение WPF
├── App.xaml.cs
├── MainWindow.xaml           # Главный экран
├── MainWindow.xaml.cs        # Логика парсинга и SQL
└── ExcelParser.csproj        # Проект
```

## Создаваемая SQL таблица

Для работы приложения создайте таблицу в базе данных:

```sql
CREATE TABLE AccrualCharges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ApartmentNumber INT NOT NULL,
    FullName NVARCHAR(255),
    TotalArea DECIMAL(10,2),
    
    -- Показания счетчиков
    HotWaterPrevious DECIMAL(10,2),
    HotWaterCurrent DECIMAL(10,2),
    HotWaterConsumption DECIMAL(10,2),
    ColdWaterPrevious DECIMAL(10,2),
    ColdWaterCurrent DECIMAL(10,2),
    ColdWaterConsumption DECIMAL(10,2),
    
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
```

## Особенности

- Автоматический поиск колонок по частичному совпадению имен
- Вычисление потребления (разница между текущими и предыдущими показаниями)
- Пакетная обработка записей с отчетом об ошибках
- Копирование отчета в буфер обмена

## Зависимости

- [ClosedXML](https://www.nuget.org/packages/ClosedXML) - для работы с Excel
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) - для подключения к SQL Server
