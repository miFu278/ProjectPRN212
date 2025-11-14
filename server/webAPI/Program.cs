using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using webAPI.Models;
using webAPI.Services;
using webAPI.Data;               // DbContext BatterySwapContext
// using webAPI.Controllers.Secure; // không bắt buộc, có cũng được

var builder = WebApplication.CreateBuilder(args);

/* ============ LOGGING sớm ============ */
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Information);

/* ============ Env info ============ */
Console.WriteLine("=== BOOT ===");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ApplicationName: {builder.Environment.ApplicationName}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

/* ============ Swagger + JWT ============ */
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BatterySwap API", Version = "v1" });

    var jwt = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste access token (không cần gõ 'Bearer ')",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwt.Reference.Id, jwt);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwt, Array.Empty<string>() }
    });
});

/* ============ CORS ============ */
builder.Services.AddCors(o => o.AddPolicy("AllowLocal", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

/* ============ Đọc cấu hình ============ */
var connStr = builder.Configuration.GetConnectionString("BatterySwapDb");
Console.WriteLine($"[CFG] ConnectionStrings:BatterySwapDb length = {(connStr?.Length ?? 0)}");
if (string.IsNullOrWhiteSpace(connStr))
{
    Console.WriteLine("[FATAL] Missing ConnectionStrings:BatterySwapDb");
    throw new InvalidOperationException("Missing Connection String 'BatterySwapDb'");
}

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "";
Console.WriteLine($"[CFG] Jwt:Secret length = {jwtSecret.Length}");

/* ============ DbContext (debug on) ============ */
builder.Services.AddDbContext<BatterySwapContext>(opt =>
{
    opt.UseSqlServer(connStr);
    opt.EnableSensitiveDataLogging();   // chỉ bật khi debug
    opt.EnableDetailedErrors();
});

/* ============ JWT Auth ============ */
var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

/* ============ DI Services (Scoped) ============ */
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<PackageService>();
builder.Services.AddScoped<DriverPackageService>();
builder.Services.AddScoped<PaymentTransactionService>();
builder.Services.AddScoped<webAPI.Services.Ado.BookingAdo>();
builder.Services.AddScoped<webAPI.Services.Ado.DriverPackageAdo>();
builder.Services.AddScoped<webAPI.Services.Ado.BatterySlotAdo>();
builder.Services.AddScoped<webAPI.Services.Ado.VehicleAdo>();
builder.Services.AddScoped<webAPI.Services.Ado.VehicleAdo>();

// Repo cho BookingController (như bạn đang dùng)
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<DriverPackageRepository>();
builder.Services.AddScoped<BatterySlotRepository>();
builder.Services.AddScoped<VehicleRepository>();

var app = builder.Build();

/* ============ Swagger Dev ============ */
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/* ============ Middleware log ConnLen mỗi request ============ */
app.Use(async (ctx, next) =>
{
    try
    {
        var db = ctx.RequestServices.GetService<BatterySwapContext>();
        if (db != null)
        {
            var cs = db.Database.GetDbConnection().ConnectionString;
            Console.WriteLine($"[DB] Runtime ConnectionString length = {(cs?.Length ?? 0)} | {ctx.Request.Method} {ctx.Request.Path}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[DB] Middleware inspect error: " + ex.Message);
    }
    await next();
});

app.UseCors("AllowLocal");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/* ============ Endpoint kiểm tra DB nhanh ============ */
app.MapGet("/_debug/db", async (BatterySwapContext db) =>
{
    var conn = db.Database.GetDbConnection();
    try
    {
        await conn.OpenAsync();
        var info = new
        {
            Provider = conn.GetType().FullName,
            DataSource = conn.DataSource,
            Database = conn.Database,
            ServerVersion = conn.ServerVersion,
            ConnectionStringLength = conn.ConnectionString?.Length ?? 0
        };
        await conn.CloseAsync();
        return Results.Ok(info);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "DB open failed", detail: ex.ToString());
    }
});

/* ============ Chặn favicon để đỡ nhiễu log (optional) ============ */
app.MapGet("/favicon.ico", () => Results.NoContent());

app.Run();
