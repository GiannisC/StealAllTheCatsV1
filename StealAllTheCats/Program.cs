using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StealAllTheCats.Data;
using StealAllTheCats.Services;
using System.Reflection;

// Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/StealAllTheCats_log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    // Load .env file in development
    DotNetEnv.Env.Load();

    // Load secrets in development - ApiKey
    builder.Configuration.AddUserSecrets<Program>();
}
// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog();

// DB Context
builder.Services.AddDbContext<DBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("CatDb")));

// Hangfire
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("CatDb")));

builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<CatFetcherJob>(); // Background job service
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Steal All The Cats API",
        Version = "v1",
        Description = "API for getting cat images from TheCatAPI."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // hide bottom schema
        options.DefaultModelExpandDepth(-1);   
        options.DefaultModelsExpandDepth(-1);  
    });
   
    // Run migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DBContext>();
        db.Database.Migrate();
    }
}

// Redirect root to Swagger
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger/index.html");
    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard();

app.Run();
