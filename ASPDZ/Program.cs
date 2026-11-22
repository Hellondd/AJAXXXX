using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Middleware 1 starts the request lifecycle and records the incoming request.
app.Use(async (context, next) =>
{
    Lifecycle.Add(context, "Middleware 1: запрос получен сервером");
    await next();
    Lifecycle.Add(context, "Middleware 1: ответ возвращён клиенту");
});

// Middleware 2 runs between the first middleware and the endpoint.
app.Use(async (context, next) =>
{
    Lifecycle.Add(context, "Middleware 2: запрос передан обработчику маршрута");
    await next();
    Lifecycle.Add(context, "Middleware 2: обработчик завершил работу");
});

app.MapGet("/", async context =>
{
    Lifecycle.Add(context, "Endpoint GET /: отображена форма заявки");
    await WritePage(context, "", "");
});

app.MapPost("/submit", async context =>
{
    Lifecycle.Add(context, "Endpoint POST /submit: получены данные формы");
    var form = await context.Request.ReadFormAsync();
    var name = WebUtility.HtmlEncode(form["name"].ToString());
    var email = WebUtility.HtmlEncode(form["email"].ToString());

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
    {
        await WritePage(context, "Заполните имя и электронную почту.", "alert-error");
        return;
    }

    Lifecycle.Add(context, "Endpoint POST /submit: данные формы успешно обработаны");
    await WritePage(context, $"Заявка от <strong>{name}</strong> принята. Ответ будет отправлен на {email}.", "alert-success");
});

app.Run();

static async Task WritePage(HttpContext context, string message, string messageClass)
{
    context.Response.ContentType = "text/html; charset=utf-8";
    var steps = Lifecycle.Get(context);
    var lifecycleItems = string.Join("", steps.Select((step, index) =>
        $"<li><span>{index + 1}</span>{step}</li>"));

    var messageHtml = string.IsNullOrEmpty(message)
        ? ""
        : $"<p class=\"alert {messageClass}\">{message}</p>";

    await context.Response.WriteAsync($$"""
        <!doctype html>
        <html lang="ru">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>ASPDZ — middleware и форма</title>
            <style>
                body { font-family: Arial, sans-serif; background: #f3f6fb; color: #1d2939; margin: 0; }
                main { max-width: 820px; margin: 40px auto; padding: 0 20px; }
                .card { background: white; border-radius: 14px; padding: 28px; margin-bottom: 22px; box-shadow: 0 6px 20px #1d29391a; }
                h1 { color: #174ea6; margin-top: 0; }
                .subtitle { color: #667085; }
                label { display: block; font-weight: bold; margin-top: 14px; }
                input { box-sizing: border-box; width: 100%; padding: 10px; margin-top: 6px; border: 1px solid #cbd5e1; border-radius: 7px; }
                button { background: #1d4ed8; border: 0; border-radius: 7px; color: white; cursor: pointer; font-size: 16px; margin-top: 20px; padding: 11px 20px; }
                .alert { border-radius: 8px; padding: 14px; }
                .alert-success { background: #dcfce7; color: #166534; }
                .alert-error { background: #fee2e2; color: #991b1b; }
                ol { list-style: none; margin: 16px 0 0; padding: 0; }
                li { align-items: center; border-left: 2px solid #93c5fd; display: flex; gap: 12px; padding: 8px 0 8px 16px; }
                li span { background: #dbeafe; border-radius: 50%; color: #1e40af; font-weight: bold; padding: 5px 9px; }
                code { background: #eff6ff; border-radius: 4px; padding: 2px 5px; }
            </style>
        </head>
        <body>
            <main>
                <section class="card">
                    <h1>Заявка с ASP.NET Core middleware</h1>
                    <p class="subtitle">Проект создан из пустого шаблона ASP.NET Core.</p>
                    {{messageHtml}}
                    <form method="post" action="/submit">
                        <label for="name">Имя</label>
                        <input id="name" name="name" required>
                        <label for="email">Электронная почта</label>
                        <input id="email" name="email" type="email" required>
                        <button type="submit">Отправить заявку</button>
                    </form>
                </section>
                <section class="card">
                    <h2>Жизненный цикл текущего запроса</h2>
                    <p class="subtitle">Middleware выполняются до и после endpoint. Данные ниже добавлены во время обработки запроса.</p>
                    <ol>{{lifecycleItems}}</ol>
                    <p><code>Middleware 1 → Middleware 2 → Endpoint → Middleware 2 → Middleware 1</code></p>
                </section>
            </main>
        </body>
        </html>
        """);
}

static class Lifecycle
{
    private const string Key = "lifecycle";

    public static void Add(HttpContext context, string step)
    {
        var steps = Get(context);
        steps.Add(step);
    }

    public static List<string> Get(HttpContext context)
    {
        if (context.Items[Key] is not List<string> steps)
        {
            steps = [];
            context.Items[Key] = steps;
        }

        return steps;
    }
}
