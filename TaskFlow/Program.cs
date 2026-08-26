using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskFlow.Data;
using TaskFlow.Exceptions;
using TaskFlow.Services;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
// ASP.NET Core uygulamasını oluşturur .
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection yapılandırması bulunamadı.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key yapılandırması bulunamadı.");
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();
builder.Services.AddControllers();
// Controller'ları DI sistemine ekler.
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            // Token'ın Issuer bilgisini doğrular.
            ValidateAudience = true,
            // Token'ın Audience bilgisini doğrular.
            ValidateLifetime = true,
            // Token'ın süresini kontrol eder.
            ValidateIssuerSigningKey = true,
            // Token'ın imzasını doğrular.
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            // Issuer değerini appsettings.json'dan alır.
            ValidAudience = builder.Configuration["Jwt:Audience"],
            // Audience değerini appsettings.json'dan alır.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
            // JWT gizli anahtarını alır.
        };
    });
// JWT Authentication yapılandırması.
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();
// [Authorize] endpoint'lerini korur.
builder.Services.AddProblemDetails();
// Standart hata response desteğini ekler.
builder.Services.AddScoped<ITaskService, TaskService>();
// ITaskService istendiğinde TaskService'i verir.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
// EF Core'u SQL Server ile yapılandırır.
builder.Services.AddOpenApi();
// OpenAPI desteğini ekler.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHostedService<ReminderWorker>();
// IAuthService istendiğinde AuthService'i verir.
var app = builder.Build();
// Uygulamanın çalışma pipeline'ını oluşturur.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
app.UseExceptionHandler(exceptionApp =>
{
    // Exception'ları merkezi olarak yakalar.
    exceptionApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?
            .Error;
        // Oluşan exception bilgisini alır.
        if (exception is BusinessException)
        {
            // Business Rule hatasıysa 409 Conflict döndürür.
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = exception.Message
            });
            // BusinessException mesajını Client'a gönderir.
            return;
        }
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Beklenmeyen bir hata oluştu. Path: {Path}",
            context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        // Beklenmeyen hatalar için 500 döndürür.
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Beklenmeyen bir hata oluştu."
        });
        // Gerçek exception detayını Client'a göstermez.
    });
});
app.UseHttpsRedirection();
// HTTP isteklerini HTTPS'e yönlendirir.
app.UseCors("Frontend");
app.UseAuthentication();
// Gelen JWT tokenını doğrular.
app.UseAuthorization();
// Kullanıcının endpoint'e erişim yetkisini kontrol eder.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Development ortamında OpenAPI'yi açar.
}
app.MapControllers();
// Controller endpoint'lerini aktif eder.
app.Run();
// Uygulamayı çalıştırır.
