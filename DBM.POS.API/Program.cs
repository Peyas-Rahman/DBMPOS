using System.Text;
using DBM.POS.API.Services;
using DBM.POS.Infrastructure.Data;
using DBM.POS.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Controllers
// =====================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =====================================================
// Swagger
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // -------------------------------------------------
    // JWT Bearer Authentication
    // -------------------------------------------------

    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter JWT token. Example: eyJhbGciOi..."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document)
            ] = []
        });

    // -------------------------------------------------
    // Fix duplicate Swagger schema names
    // -------------------------------------------------
    // Example:
    // PurchaseController.Item
    // ReturnController.Item
    //
    // Without this, Swagger sees both as "Item"
    // and throws a schemaId collision.
    // -------------------------------------------------

    options.CustomSchemaIds(type =>
        type.FullName!.Replace("+", "."));
});


// =====================================================
// Database
// =====================================================

builder.Services.AddDbContext<POSDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"))
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});


// =====================================================
// Application Services
// =====================================================

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<SyncJournalService>();

// =====================================================
// JWT Configuration
// =====================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT Issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT Audience is not configured.");


// =====================================================
// Authentication
// =====================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

        // -------------------------------------------------
        // JWT Diagnostics
        // -------------------------------------------------

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "JWT AUTHENTICATION FAILED");

                Console.WriteLine(
                    context.Exception.ToString());

                Console.WriteLine(
                    "====================================");

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    $"JWT CHALLENGE: {context.Error}");

                Console.WriteLine(
                    $"JWT ERROR DESCRIPTION: " +
                    $"{context.ErrorDescription}");

                Console.WriteLine(
                    "====================================");

                return Task.CompletedTask;
            }
        };
    });


// =====================================================
// Authorization
// =====================================================

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PosWeb", policy => policy
        .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173", "http://127.0.0.1:5174", "http://localhost:5174")
        .AllowAnyHeader()
        .AllowAnyMethod());
});


// =====================================================
// Build Application
// =====================================================

var app = builder.Build();


// =====================================================
// Database Migration + Seeder
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<POSDbContext>();

    // -------------------------------------------------
    // Apply pending EF Core migrations
    // -------------------------------------------------

    await dbContext.Database.MigrateAsync();

    // -------------------------------------------------
    // Seed initial data
    // -------------------------------------------------

    if (Guid.TryParse(app.Configuration["Sync:BranchId"], out var configuredBranchId))
        dbContext.SyncBranchId = configuredBranchId;
    await BusinessSchemaInitializer.EnsureAsync(dbContext);
    await POSDataSeeder.SeedAsync(
        dbContext,
        app.Configuration);
}


// =====================================================
// HTTP Pipeline
// =====================================================

app.UseCors("PosWeb");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "DBM POS API";
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "DBM POS API v1");
    });
}


// =====================================================
// HTTPS - development only. Branch servers intentionally expose local HTTP.
// Put production/cloud behind HTTPS reverse proxy.
// =====================================================

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();


// Branch context used by the automatic offline sync journal.
app.Use(async (context, next) =>
{
    if (Guid.TryParse(app.Configuration["Sync:BranchId"], out var branchId))
        context.RequestServices.GetRequiredService<POSDbContext>().SyncBranchId = branchId;
    await next();
});

// =====================================================
// Authentication
// =====================================================

app.UseAuthentication();


// =====================================================
// Authorization
// =====================================================

app.UseAuthorization();


// =====================================================
// Controllers
// =====================================================

app.MapControllers();


// =====================================================
// Run
// =====================================================

app.Run();