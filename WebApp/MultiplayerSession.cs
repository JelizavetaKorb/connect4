namespace WebApp.Models;

public class MultiplayerSession
{
    public string GameId { get; set; } = null!;
    public string JoinCode { get; set; } = null!;
    public string Player1Name { get; set; } = null!;
    public string? Player2Name { get; set; }
    public DateTime LastActivityPlayer1 { get; set; }
    public DateTime LastActivityPlayer2 { get; set; }
    public bool Player1WantsToExit { get; set; }
    public bool Player2WantsToExit { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Player1HasLeft { get; set; } 
    public bool Player2HasLeft { get; set; } 
}