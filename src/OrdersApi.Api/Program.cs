using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
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
//TODO: Add other services 

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
