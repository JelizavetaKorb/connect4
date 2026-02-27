using Microsoft.EntityFrameworkCore;
using DAL;
using WebApp;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var homeDirectory = DataDirectoryConfig.DataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var dbPath = Path.Combine(homeDirectory, "app.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
);

builder.Services.AddScoped<ConfigRepositoryJson>();
builder.Services.AddScoped<ConfigRepositoryEF>();
builder.Services.AddScoped<RepositoryProvider>(provider =>
{
    var context = provider.GetRequiredService<AppDbContext>();
    var jsonRepo = new ConfigRepositoryJson();
    var efRepo = new ConfigRepositoryEF(context);
    
    return DataDirectoryConfig.UseJsonRepository 
        ? new RepositoryProvider(jsonRepo, efRepo)
        : new RepositoryProvider(efRepo, jsonRepo);
});

builder.Services.AddSingleton<MultiplayerSessionManager>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Connect4.Session";
});

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();
app.Run();