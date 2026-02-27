using BLL;
using ConsoleApp;
using DAL;
using MenuSystem;
using ConsoleUI;
using Microsoft.EntityFrameworkCore;

var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
DataDirectoryConfig.DataDirectory = homeDirectory;

// Switch to EF Database
//DataDirectoryConfig.UseJsonRepository = false;

// Switch to JSON file
DataDirectoryConfig.UseJsonRepository = true;

// Initialize both repositories
var jsonRepo = new ConfigRepositoryJson();
using var db = GetDbContext();
var efRepo = new ConfigRepositoryEF(db);

IRepository<GameState> activeRepo = DataDirectoryConfig.UseJsonRepository ? jsonRepo : efRepo;
IRepository<GameState> otherRepo = DataDirectoryConfig.UseJsonRepository ? efRepo : jsonRepo;

var menu0 = new Menu("Connect4 Main Menu", EMenuLevel.Root);
menu0.AddMenuItem("N", "New game", () =>
{
    var controller = new GameController(activeRepo, otherRepo);
    controller.GameLoop();
    return "";
});

menu0.AddMenuItem("L", "Load Game", () =>
{
    var saves = activeRepo.List();
    if (saves.Count == 0)
    {
        Console.WriteLine("No saved games found.");
        Thread.Sleep(1500);
        return "";
    }

    Console.WriteLine("Saved games:");
    for (int i = 0; i < saves.Count; i++)
        Console.WriteLine($"{i + 1}) {saves[i].description}");

    int choice;
    while (true)
    {
        Console.Write("Enter number to load, 0 to cancel: ");
        var input = Console.ReadLine()?.Trim();

        if (!int.TryParse(input, out choice))
        {
            Console.WriteLine("Please enter a valid number!");
            continue;
        }

        if (choice < 0 || choice > saves.Count)
        {
            Console.WriteLine("Number out of range. Try again.");
            continue;
        }
        break;
    }

    if (choice == 0) return "";

    var savedState = activeRepo.Load(saves[choice - 1].id);
    
    var controller = new GameController(savedState, activeRepo, otherRepo);
    controller.GameLoop();
    return "";
});

var rules = new Menu("Rules", EMenuLevel.First);
menu0.AddMenuItem("R", "Rules", () =>
{
    rules.Run();
    return "";
});

menu0.AddMenuItem("D", "Delete Saved Game", () =>
{
    Ui.DisplayAndDeleteSavedGames(activeRepo, otherRepo);
    return "";
});

menu0.Run();

Console.WriteLine("Game closed...");

// db setup
AppDbContext GetDbContext()
{
    //builds connection string to the home repo of the user
    var dbPath = Path.Combine(homeDirectory, "app.db");
    var connectionString = $"Data Source={dbPath}";

    var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(connectionString)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        //.LogTo(Console.WriteLine)
        .Options;

    var dbContext = new AppDbContext(contextOptions);
    dbContext.Database.Migrate();
    return dbContext;
}
