# краткая инструкция по проекту SGTest:

## пакеты и сервер:

### минимум .NET 6.0 SDK
### минимум PostgreSQL 14
### dotnet установлен на пк

### сам проект был построен с использованием

### .NET 9.0 SDK

### PostgreSQL 17

## пакеты которые были скачены чередз NutGet:

### Microsoft.EntityFrameworkCore

### Microsoft.EntityFrameworkCore.Tools

### Microsoft.Extensions.Configuration.Json

### Npgsql.EntityFrameworkCore.PostgreSQL

### System.Text.Json


# Установка:

### делаем git clone в папку и открываем проект с консолью в ней

# Подключение к БД:
### 1. конфигурируем файл appsettings.json для подлючения, пишим туда свои данные

```json
{
  "json": {
    "Database": {
      "Host": "localhost",
      "Port": 5432,
      "Database": "importerdb",
      "Username": "postgres",
      "Password": "postgres"
    }
  }
}
```

# Иницилизируем базу данных есть несколько вариантов:

## 1. просто запустить скрипт sql

### psql -U postgres -f sqlScripts\baseCatalogsAndTables.sql

## 2. запускаем миграции (в папке Migration уже есть миграции но можете сгенерить новые уадлив старые или сразу сделав миграцию)

### dotnet ef migrations add "initial"

### dotnet ef database update

### 3. если у вас есть база с именем postgres то при запуске проекта таблицы создадутся сами


# Билдим проект:

### 1. dotnet build

# Запускаем проект

### 1. есть две команды import и print:

#### dotnet run import --file=<file_path> --type=<data_type> (импортирует данные)

#### dotnet run print (выводит состояние БД)

# Доступные флаги:

#### --file= путь до файла обычно папка в проекте с файлов расширения tsv (уже лежит в проекте TSVData)

#### --type= тип иморта их всего 3 (department, employee, jobtitle)

#### --print= отобращить состояние БД (--department= доп флаг чтобы отобразить конкретный депортамент)

# Примеры команд:

### dotnet run import --file=TSVData/jobtitle.tsv --type=jobtitle

### dotnet run import --file=data/departments.tsv --type=department

### dotnet run import --file=data/employees.tsv --type=employee

#### После выполнения команды будет отображенно состояние базы данных

#### так же если вы хотите узнать состояние в данный момент (Пример):

### dotnet run print --department=2

### Так же в проекте есть скомпелированный релиз чтобы сразу посмотреть работоспособность программы в нем так же лежит файл appsettings.json и папка TSVData с нужными файлами

### воспользуйтесь SGTestRunner.bat чтобь не вводить команды в косоле

# Соблюдайте порядок импорта!!!

### сначала departments, jobtitles, или наоборот и только потом employees так как их связам нужеы каталоги которых нет до импорта