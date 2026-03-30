# RfidPrintSolution
Программа для быстрой печати документов с использованием карт Mifare 1k и считывателя Acr122U.

## Установка релизной версии
1. Зайдите в релизные версии. 
2. Скачайте последнюю актуальную. 
3. Настройте appsettings.json для подключения к базе данных.
4. Запустите программу.


## Установка из 
1. Скачайте репозиторий и распакуйте.
2. Соберите проект.
3. Настройте appsettings.json для подключения к базе данных.
4. Запустите программу. 

### Скрипт создания структуры в PostgreSQL:
~~~~sql
CREATE TABLE IF NOT EXISTS card_mappings (
    id SERIAL PRIMARY KEY,
    uid TEXT NOT NULL UNIQUE,
    file_path TEXT NOT NULL,
    printer_name TEXT,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS print_log (
    id BIGSERIAL PRIMARY KEY,
    uid TEXT NOT NULL,
    computer_name TEXT NOT NULL,
    printer_name TEXT,
    file_path TEXT,
    status TEXT NOT NULL,
    error_message TEXT,
    printed_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO card_mappings (uid, file_path, description)
VALUES ('123123123123', 'C:\Test\test.pdf', 'Тестовая карта')
ON CONFLICT (uid) DO NOTHING;
~~~~
