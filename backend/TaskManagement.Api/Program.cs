using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Register services (the composition root — Angular's app.config.ts providers)
// ---------------------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as strings ("InProgress"), not integers (1), so the Angular
        // client can bind them directly and the API is self-describing.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Without this, NotFound() returns an EMPTY 404 body, contradicting the contract
// which documents every 404 as carrying ProblemDetails.
builder.Services.AddProblemDetails();

// Registered Scoped by AddDbContext — one instance per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Scoped, NOT Singleton. A Singleton holding the Scoped DbContext would be a
// captive dependency: the context is never disposed and breaks on request two.
builder.Services.AddScoped<ITaskService, TaskService>();

// Swashbuckle — the single approved Article III package exception (plan.md 4.1).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The API listens on http://localhost:5178 (launchSettings.json + appsettings
// "Urls"); this policy names the ANGULAR dev server's origin, which is a different
// port and must not be confused with the API's own. See plan.md section 2.1.
const string AngularCorsPolicy = "AngularDevClient";
var angularOrigin = builder.Configuration["Cors:AngularOrigin"] ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins(angularOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// 2. Compose the middleware pipeline (Angular's HTTP interceptors — order IS
//    behaviour, not style)
// ---------------------------------------------------------------------------

app.UseExceptionHandler();      // outermost: catches anything thrown further in
app.UseStatusCodePages();       // gives bodiless error responses a ProblemDetails body

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Deliberately NOT applied in Development. The "https" launch profile listens on
// BOTH https://localhost:7047 and http://localhost:5178; with redirection on, the
// Angular client's calls to 5178 would be 307'd to 7047 and fail on the dev
// certificate and the origin change. Scoping this to non-Development means the API
// answers on 5178 under either launch profile. See plan.md section 2.1.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// MUST be after UseRouting and before the endpoints. If every endpoint works in
// Swagger but fails from Angular with a CORS error, this line is the reason:
// Swagger is same-origin with the API, so it never exercises CORS.
app.UseCors(AngularCorsPolicy);

app.MapControllers();

app.Run();
