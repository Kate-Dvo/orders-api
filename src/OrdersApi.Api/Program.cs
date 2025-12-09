using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrdersApi.Api.Helpers;
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
        Description = "Standard Orders API - Original version"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Orders API",
        Version = "v2",
        Description = "Orders API with discount support"
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

    options.DocInclusionPredicate((docName, apiDescription) => apiDescription.GroupName == docName);
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

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("API is alive"), tags: ["live"])
    .AddDbContextCheck<OrdersDbContext>("database", tags: ["ready"]);

//Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddResponseCaching();

//Validators
builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
builder.Services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();

builder.Services.AddRateLimiter(options =>
{
    //Policy 1: Fixed window (60 requests per minute) for GET requests 
    options.AddFixedWindowLimiter(Consts.FixedRateLimit, fixedOptions =>
    {
        fixedOptions.PermitLimit = 60;
        fixedOptions.Window = TimeSpan.FromMinutes(1);
        fixedOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        fixedOptions.QueueLimit = 0;
    });

    //Policy 2: Sliding window (10 request per minute) for write operations (more strict)
    options.AddSlidingWindowLimiter(Consts.SlidingRateLimit, slidingOptions =>
    {
        slidingOptions.PermitLimit = 10;
        slidingOptions.Window = TimeSpan.FromMinutes(1);
        slidingOptions.SegmentsPerWindow = 6;
        slidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        slidingOptions.QueueLimit = 0;
    });

    //Policy 3: Per-user token bucket after auth
    options.AddTokenBucketLimiter(Consts.PerUserTokenBucket, tokenOptions =>
    {
        tokenOptions.TokenLimit = 100;
        tokenOptions.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        tokenOptions.TokensPerPeriod = 50;
        tokenOptions.AutoReplenishment = true;
        tokenOptions.QueueLimit = 0;
    });

    //Global fallback
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAdress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAdress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    //Customized rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again later.",
            instance = context.HttpContext.Request.Path.ToString(),
            retryAfter = retryAfter.TotalSeconds
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    };
});

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });


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
    app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "Order API v2");
        }
    );

    var address = app.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    Console.WriteLine($"Swagger UI: {address?.FirstOrDefault()}/swagger");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseResponseCaching();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
    }
}).DisableRateLimiting();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
}).DisableRateLimiting();
app.MapControllers();

app.Run();