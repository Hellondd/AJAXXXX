# ASPDZ — middleware и жизненный цикл ASP.NET Core

Проект создан с помощью пустого шаблона `dotnet new web`.

## Реализовано

- HTML-форма с именем и электронной почтой;
- обработка POST-запроса и вывод сообщения об успешной заявке;
- два middleware, которые записывают этапы жизненного цикла запроса;
- визуальное отображение порядка прохождения запроса: `Middleware 1 → Middleware 2 → Endpoint → Middleware 2 → Middleware 1`;
- изображение жизненного цикла в `screenshots/lifecycle-diagram.svg`.

## Запуск

```powershell
dotnet run
```
