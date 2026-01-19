using System.Reflection;
using TrainingTracker.Api.Contracts.Health;
using TrainingTracker.Api.Infrastructure.Runtime;
using TrainingTracker.Api.Middleware;
using TrainingTracker.Api.Domain.Repositories;
using TrainingTracker.Api.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingTracker.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Core services
builder.Services.AddSingleton<StartupInfo>();
// CORS for local Vite dev server (adjust in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "http://localhost:5174",
                  "https://localhost:5174",
                  "http://127.0.0.1:5173",
                  "https://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers(options =>
{
    // potential place for global filters later
})
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = context =>
        {
            var traceId = context.HttpContext.Items[CorrelationIdMiddleware.HeaderName];
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            var payload = new { traceId, message = "Validation failed", errors };
            return new BadRequestObjectResult(payload);
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks(); // readiness later (DB, etc.)
// Persistence mode toggle (InMemory vs EF) - configured via appsettings: Persistence:Mode
var persistenceMode = builder.Configuration.GetValue<string>("Persistence:Mode") ?? "InMemory";
if (string.Equals(persistenceMode, "Ef", StringComparison.OrdinalIgnoreCase))
{
    // NOTE: Schema is managed by SSDT; no migrations executed here
    var connString = builder.Configuration.GetConnectionString("TrainingTracker")
        ?? throw new InvalidOperationException("Missing connection string 'TrainingTracker'.");
    builder.Services.AddDbContext<TrainingTrackerDbContext>(opts =>
    {
        opts.UseSqlServer(connString, o => o.EnableRetryOnFailure());
    });
    builder.Services.AddScoped<ICourseRepository, EfCourseRepository>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<IEnrollmentRepository, EfEnrollmentRepository>();
}
else
{
    builder.Services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
    builder.Services.AddSingleton<IEnrollmentRepository, InMemoryEnrollmentRepository>();
}
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Observability / error pipeline
app.UseCorrelationId();
app.UseGlobalExceptionHandling();

// CORS before auth/endpoints
app.UseCors("LocalDev");

app.UseAuthorization();

app.MapControllers();

// Liveness - lightweight; always fast
app.MapGet("/health", (StartupInfo info) =>
{
    var now = DateTime.UtcNow;
    var response = new HealthResponse
    {
        Status = "ok",
        Version = assemblyVersion,
        TimestampUtc = now,
        UptimeSeconds = (long)(now - info.StartTimeUtc).TotalSeconds
    };
    return Results.Json(response);
})
   .WithName("Health")
   .WithSummary("Liveness health check")
   .WithDescription("Returns basic liveness information and uptime.")
   .WithOpenApi();

// Readiness - integrates ASP.NET Core health checks (will expand with DB)
// Readiness: add a lightweight DB connectivity probe if EF mode
app.MapGet("/health/ready", async (StartupInfo info, IServiceProvider sp, CancellationToken ct) =>
{
    var now = DateTime.UtcNow;
    var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
    var status = "ok";
    string? dbStatus = null;
    if (string.Equals(persistenceMode, "Ef", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TrainingTrackerDbContext>();
            var canConnect = await db.Database.CanConnectAsync(ct);
            dbStatus = canConnect ? "up" : "down";
            if (!canConnect) status = "degraded";
        }
        catch (Exception)
        {
            dbStatus = "error";
            status = "degraded";
            // swallow exception; middleware logging will capture if bubbled, but here we just mark degraded
        }
    }
    var payload = new
    {
        status,
        version = assemblyVersion,
        timestampUtc = now,
        uptimeSeconds = (long)(now - info.StartTimeUtc).TotalSeconds,
        dependencies = new { database = dbStatus }
    };
    return Results.Json(payload, statusCode: status == "ok" ? 200 : 503);
})
.WithName("HealthReady")
.WithSummary("Readiness health check")
.WithDescription("Indicates whether required dependencies are available (DB probed only in EF mode).")
.WithOpenApi();

app.Run();

// Expose Program class for test host
public partial class Program { }
