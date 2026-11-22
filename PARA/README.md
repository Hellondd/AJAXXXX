# PARA REST API

Учебное ASP.NET Core приложение, реализующее возможности из конспекта по REST API.

## Реализовано

- REST-маршруты для ресурсов товаров, документов и платежей;
- привязка параметров из route, query, body и header;
- валидация DTO и ответы `ValidationProblem`;
- `ProblemDetails` для ошибок 404, 412 и 500;
- условный GET c `ETag` и `If-None-Match`;
- защита обновления документа через `If-Match` и ответ `412 Precondition Failed`;
- `Idempotency-Key` для повторных POST-запросов платежа;
- удаление ресурса с ответами `204 No Content` и `404 Not Found`.

## Запуск

```powershell
dotnet run --urls http://127.0.0.1:5182
```

## Примеры запросов

```http
GET /api/products
If-None-Match: "products-v1"

GET /api/documents/1

PUT /api/documents/1
If-Match: "v5"
Content-Type: application/json

{ "content": "Updated document" }

POST /api/payments
Idempotency-Key: 8d6afdf2-4552-4a0f-8390-7630b922bf8a
Content-Type: application/json

{ "amount": 100, "currency": "RUB" }
```
