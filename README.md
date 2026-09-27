# DevSecRepo

Решение тестового задания: REST API на .NET 10. Приложение декодирует HTML и URL из Base64, выбирает элементы через AngleSharp, извлекает email регулярным выражением, расшифровывает AES-256 ECB (PaddingMode.None) и сохраняет элементы в PostgreSQL через Dapper.

## Запуск

Нужен Docker с Docker Compose. Из корня проекта:

```bash
docker compose up -d
```

При старте контейнера API приложение собирается из исходников. Первый запуск может занять несколько минут из-за загрузки образов и NuGet-пакетов.

- Swagger: http://localhost:8090/api/swagger
- API: `POST http://localhost:8090/api/DevSec`
- pgAdmin: http://localhost:8080 (без ввода паролей)

В pgAdmin откройте сервер `TestJob PostgreSQL`, базу `testjob`, схему `public` и таблицу `elements`. PostgreSQL 18 хранит данные в Docker volume `postgres_data`. Учетные данные в Compose предназначены для локальной проверки задания.

## Тестовые запросы

```bash
curl -fsS http://localhost:8090/api/DevSec -H 'Content-Type: application/json' --data-binary @json_payload_1.txt
curl -fsS http://localhost:8090/api/DevSec -H 'Content-Type: application/json' --data-binary @json_payload_2.txt
```

Ответы сохранены в `json_result_1.txt` и `json_result_2.txt`. Первый запрос находит 238 элементов и 5 email, второй — 9 элементов и 5 email. Повторные запросы добавляют новые строки в таблицу. Элементы каждого запроса сохраняются в одной транзакции.

Текст `AES Error: Object reference not set to an instance of an object.` в поле `decrypted_plain_text` — результат расшифровки предоставленных данных; успешность запроса обозначается полем `is_error: 0`.

Некорректные входные данные возвращают HTTP 400 с полями `is_error`, `error_code` и `error_message`. Непредвиденные исключения возвращают HTTP 500 в том же формате.

## Управление контейнерами

```bash
docker compose logs api
docker compose restart api
docker compose down
```

После изменения исходников перезапустите API. Команда `docker compose down` сохраняет данные БД; добавление `-v` удаляет volume вместе с данными.

## Асинхронность

Работа с БД и парсинг HTML используют доступные асинхронные методы с передачей `CancellationToken`. Ожидание БД не удерживает поток обработки запроса. Base64, AES и регулярное выражение выполняют вычисления в памяти синхронно: `Task.Run` лишь перенес бы эту работу на другой поток. Регулярное выражение скомпилировано и ограничено таймаутом.

Исходное задание и тестовые данные: https://github.com/adm-devsec/TestJob
