using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internal Server Error",
        Detail = exception?.Message,
        Instance = context.Request.Path
    };
    problem.Extensions["traceId"] = context.TraceIdentifier;
    problem.Extensions["timestamp"] = DateTime.UtcNow;

    context.Response.StatusCode = problem.Status.Value;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(problem);
}));

app.MapGet("/", () => Results.Ok(new
{
    message = "PARA REST API",
    endpoints = new[] { "/api/products", "/api/documents/1", "/api/payments" }
}));

// GET supports conditional requests: an unchanged resource returns 304.
app.MapGet("/api/products", (HttpContext context, [FromQuery] int page = 1) =>
{
    const string etag = "\"products-v1\"";
    if (context.Request.Headers.IfNoneMatch == etag)
    {
        context.Response.Headers.ETag = etag;
        return Results.StatusCode(StatusCodes.Status304NotModified);
    }

    context.Response.Headers.ETag = etag;
    return Results.Ok(new { page, items = Store.Products.Values.OrderBy(x => x.Id) });
});

app.MapGet("/api/products/{id:int}", (int id) =>
    Store.Products.TryGetValue(id, out var product)
        ? Results.Ok(product)
        : NotFoundProblem($"Product with id {id} was not found"));

// Route, query, body and header binding in a single endpoint.
app.MapPost("/api/categories/{categoryId:int}/products", (
    [FromRoute] int categoryId,
    [FromQuery] bool notify,
    [FromBody] CreateProductRequest request,
    [FromHeader(Name = "X-Request-Id")] string? requestId) =>
{
    var errors = ValidationErrors(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, title: "Validation failed");
    }

    var id = Store.NextProductId();
    var product = new ProductResponse(id, request.Name, request.Price, categoryId, notify, requestId);
    Store.Products[id] = product;
    return Results.Created($"/api/products/{id}", product);
});

app.MapDelete("/api/products/{id:int}", (int id) =>
    Store.Products.TryRemove(id, out _)
        ? Results.NoContent()
        : NotFoundProblem($"Product with id {id} was already removed or does not exist"));

// ETag protects a document from being overwritten by a stale client.
app.MapGet("/api/documents/{id:int}", (HttpContext context, int id) =>
{
    if (!Store.Documents.TryGetValue(id, out var document))
    {
        return NotFoundProblem($"Document with id {id} was not found");
    }

    context.Response.Headers.ETag = document.ETag;
    return Results.Ok(document);
});

app.MapPut("/api/documents/{id:int}", (HttpContext context, int id, [FromBody] UpdateDocumentRequest request) =>
{
    var errors = ValidationErrors(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, title: "Validation failed");
    }

    if (!Store.Documents.TryGetValue(id, out var current))
    {
        return NotFoundProblem($"Document with id {id} was not found");
    }

    var ifMatch = context.Request.Headers.IfMatch.ToString();
    if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch != current.ETag)
    {
        return Results.Problem(
            title: "Precondition Failed",
            detail: "The document was changed by another client. Request the current version and try again.",
            statusCode: StatusCodes.Status412PreconditionFailed,
            type: "https://httpstatuses.com/412");
    }

    var updated = current with { Content = request.Content, Version = current.Version + 1 };
    Store.Documents[id] = updated;
    context.Response.Headers.ETag = updated.ETag;
    return Results.Ok(updated);
});

// Idempotency-Key prevents accidental duplicate payment creation.
app.MapPost("/api/payments", (HttpContext context, [FromHeader(Name = "Idempotency-Key")] string? key, [FromBody] CreatePaymentRequest request) =>
{
    var errors = ValidationErrors(request);
    if (string.IsNullOrWhiteSpace(key))
    {
        errors["Idempotency-Key"] = ["The Idempotency-Key header is required."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, title: "Validation failed");
    }

    if (Store.PaymentsByKey.TryGetValue(key!, out var existing))
    {
        return Results.Ok(new { payment = existing, repeatedRequest = true });
    }

    var payment = new PaymentResponse($"pay_{Store.NextPaymentId()}", request.Amount, request.Currency.ToUpperInvariant());
    Store.PaymentsByKey[key!] = payment;
    return Results.Created($"/api/payments/{payment.Id}", payment);
});

app.Run();

static IResult NotFoundProblem(string detail) => Results.Problem(
    title: "Not Found",
    detail: detail,
    statusCode: StatusCodes.Status404NotFound,
    type: "https://httpstatuses.com/404");

static Dictionary<string, string[]> ValidationErrors(object model)
{
    var context = new ValidationContext(model);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(model, context, results, validateAllProperties: true);

    return results
        .SelectMany(result => result.MemberNames.DefaultIfEmpty("request"), (result, name) => new { name, result.ErrorMessage })
        .GroupBy(x => x.name)
        .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());
}

public sealed class CreateProductRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 1000000)]
    public decimal Price { get; init; }
}

public sealed class UpdateDocumentRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class CreatePaymentRequest
{
    [Range(1, 1000000)]
    public decimal Amount { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;
}

public record ProductResponse(int Id, string Name, decimal Price, int CategoryId, bool Notify, string? RequestId);
public record DocumentResponse(int Id, string Content, int Version)
{
    public string ETag => $"\"v{Version}\"";
}
public record PaymentResponse(string Id, decimal Amount, string Currency);

public static class Store
{
    private static int _productId = 2;
    private static int _paymentId;

    public static ConcurrentDictionary<int, ProductResponse> Products { get; } = new(
        new[] { new KeyValuePair<int, ProductResponse>(1, new ProductResponse(1, "Keyboard", 2500, 1, false, null)) });
    public static ConcurrentDictionary<int, DocumentResponse> Documents { get; } = new(
        new[] { new KeyValuePair<int, DocumentResponse>(1, new DocumentResponse(1, "Initial document", 5)) });
    public static ConcurrentDictionary<string, PaymentResponse> PaymentsByKey { get; } = new();

    public static int NextProductId() => Interlocked.Increment(ref _productId);
    public static int NextPaymentId() => Interlocked.Increment(ref _paymentId);
}
