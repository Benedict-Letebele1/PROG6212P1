using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDayApi.Data;
using RaceDayApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Logged-in state lives in a server-side session. The cookie only holds the session id.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDayApi.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddScoped<IBannerStorage, LocalBannerStorage>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Events, categories, enrolments and results for road running, walking and cycling events. "
            + "Log in with POST /api/auth/login first; the session cookie is then sent automatically from Swagger UI."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

// Creates or updates the schema and loads the demo data when the database is empty.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Database:SeedDemoData", true))
    {
        await DemoDataSeeder.SeedAsync(db);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseSession();

app.MapControllers();

app.Run();

// Exposes the entry point to the test project so WebApplicationFactory can host the API.
public partial class Program;
