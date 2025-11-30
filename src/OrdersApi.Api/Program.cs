using FluentValidation;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.Middleware;
using OrdersApi.Application.Customers;
using OrdersApi.Application.Customers.Models;
using OrdersApi.Application.Customers.Validators;
using OrdersApi.Application.Orders;
using OrdersApi.Application.Orders.Models;
using OrdersApi.Application.Orders.Validators;
using OrdersApi.Application.Products;
using OrdersApi.Application.Products.Models;
using OrdersApi.Application.Products.Validators;
using OrdersApi.Domain.Configuration;
using OrdersApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

//Add serviced to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Configure Options Pattern
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

//Add DbContext
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//TODO: Add Authentication

//Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

//Validators
builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
builder.Services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();



var app = builder.Build();

//Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    db.Database.Migrate();
    db.SeedData(logger);
}

//Configure the HTTP request pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    var address = app.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    Console.WriteLine($"Swagger UI: {address?.FirstOrDefault()}/swagger");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
