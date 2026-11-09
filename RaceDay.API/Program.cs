using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay Event Management Platform for South African Road Events."
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// The connection string is supplied through environment-specific configuration.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/*
 * Server-side session used for login.
 * After login the API stores UserId and Role in the session and the client
 * only receives the session cookie.
 */
builder.Services.AddSingleton<IImageStore, AzureBlobImageStore>();
builder.Services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Docker serves HTTP only. Local HTTPS profiles keep the redirect.
if (app.Configuration.GetValue("UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

// Serves uploaded images from wwwroot/uploads
app.UseStaticFiles();

app.UseSession();

app.MapControllers();

await ApplyMigrationsAsync(app);

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:ApplyMigrations", false))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    for (int attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < 30)
        {
            app.Logger.LogWarning(ex, "SQL Server is not ready yet (attempt {Attempt}).", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
