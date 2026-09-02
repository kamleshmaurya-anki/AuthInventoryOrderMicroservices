using InventoryService.Data;
using InventoryService.Repositories;
using InventoryService.Security;
using InventoryService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDb")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductManagementService>();

// Internal API key settings (used only by reduce_stock / restore_stock)
var internalApiKeySettings = builder.Configuration.GetSection("InternalApi").Get<InternalApiKeySettings>()
    ?? throw new InvalidOperationException("InternalApi configuration section is missing.");
builder.Services.AddSingleton(internalApiKeySettings);

// Two authentication schemes live side by side:
//  - JWT Bearer: validates end-user tokens issued by Auth Service (used by
//    the public CRUD endpoints, with role-based authorization on top).
//  - InternalApiKey: validates the shared secret used by Order Service when
//    calling reduce_stock / restore_stock (see the controller for rationale).
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
        InternalApiKeyAuthenticationHandler.SchemeName, _ => { });

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
