var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

// Exposes the entry point to the test project so WebApplicationFactory can host the API.
public partial class Program;
