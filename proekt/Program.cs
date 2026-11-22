using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Application.Common.Abstractions;
using WebApplication3.Application.Orders.CancleOrder;
using WebApplication3.Application.Orders.CreateOrder;
using WebApplication3.Application.Orders.GetOrder;
using WebApplication3.Domain;
using WebApplication3.Infrastructure;
using WebApplication3.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("OrdersDatabase"));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<GetOrderUseCase>();
builder.Services.AddScoped<CancelOrderUseCase>();
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Web/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Web/Views/Shared/{0}.cshtml");
    });
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Customers.Any())
    {
        db.Customers.Add(new Customer("student@example.com", "Учебный клиент"));
        db.Products.AddRange(
            new Product("Клавиатура", 2500m, 10),
            new Product("Мышь", 1500m, 15));
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
