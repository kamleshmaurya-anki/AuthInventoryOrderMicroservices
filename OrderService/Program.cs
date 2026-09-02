using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Services;
using Serilog;
using Shared.Middleware;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter the JWT token returned from Auth Service's /api/auth/login"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderManagementService>();

// Typed HttpClient for calling Inventory Service. This is the ONLY channel
// Order Service has to product/stock data - it does not reference inventory_db.
// Every request carries the shared internal API key so Inventory Service can
// authorize the reduce_stock / restore_stock calls independently of any
// end-user JWT.
var inventoryBaseUrl = builder.Configuration["InventoryService:BaseUrl"]
    ?? throw new InvalidOperationException("InventoryService:BaseUrl is not configured.");
var inventoryTimeoutSeconds = builder.Configuration.GetValue("InventoryService:TimeoutSeconds", 10);
var inventoryInternalApiKey = builder.Configuration["InventoryService:InternalApiKey"]
    ?? throw new InvalidOperationException("InventoryService:InternalApiKey is not configured.");

builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
{
    client.BaseAddress = new Uri(inventoryBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(inventoryTimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", inventoryInternalApiKey);
});

// Order Service independently validates JWTs issued by Auth Service (same
// shared key/issuer/audience) - no round trip to Auth Service is needed.
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var app = builder.Build();

// ---- Middleware pipeline ----
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
