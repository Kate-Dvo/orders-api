using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Orders API",
        Version = "v1",
        Description = "Web API project example"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//Configure Options Pattern
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings!.Secret);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; //true in prod with HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();