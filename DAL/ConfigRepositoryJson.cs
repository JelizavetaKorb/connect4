using System.Text.Json;
using BLL;

namespace DAL;

public class ConfigRepositoryJson : IRepository<GameState>
{
    // if username given, filters by it
    public List<(string id, string description)> List(string? username = null)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var allGames = Directory.EnumerateFiles(dir, "*.json")
            .Select(f =>
            {
                var id = Path.GetFileNameWithoutExtension(f);
                try
                {
                    var json = File.ReadAllText(f);
                    var state = JsonSerializer.Deserialize<GameState>(json);
                    return (id, description: state?.Name ?? id, state);
                }
                catch
                {
                    return (id: id, description: id, state: (GameState?)null);
                }
            })
            .ToList();

        if (!string.IsNullOrEmpty(username))
        {
            allGames = allGames
                .Where(g => g.state != null && 
                    (g.state.Configuration.Player1Name.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                     g.state.Configuration.Player2Name.Equals(username, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return allGames.Select(g => (g.id, g.description)).ToList();
    }

    public string Save(GameState state)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var fileName = state.Id.ToString() + ".json";
        var filePath = Path.Combine(dir, fileName);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
        return state.Id.ToString();
    }

    public GameState Load(string id)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var fullPath = Path.Combine(dir, id + ".json");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Game not found: {fullPath}");

        var json = File.ReadAllText(fullPath);
        var state = JsonSerializer.Deserialize<GameState>(json);

        if (state == null)
            throw new NullReferenceException($"Failed to load game state: {fullPath}");

        return state;
    }
    
    public void Delete(string id)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var filePath = Path.Combine(dir, id + ".json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        else
        {
            Console.WriteLine($"No file found for {id}");
        }
    }
}