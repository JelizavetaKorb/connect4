using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace BLL;

public class GameState : BaseEntity
{
    public string Name { get; set; } = null!;

    public Guid ConfigurationId { get; set; }

    public GameConfiguration Configuration { get; set; } = null!;

    // 2D board (not mapped directly to DB)
    [NotMapped]
    public ECellState[][] Board { get; set; } = Array.Empty<ECellState[]>();

    // Serialize/deserialize board for DB
    public string BoardSerialized
    {
        get => JsonSerializer.Serialize(Board);
        set => Board = JsonSerializer.Deserialize<ECellState[][]>(value) ?? Array.Empty<ECellState[]>();
    }

    public bool IsNextPlayerX { get; set; }
}