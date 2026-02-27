namespace BLL;

public class GameConfiguration : BaseEntity
{
    public int BoardWidth { get; set; } = 7;
    public int BoardHeight { get; set; } = 6;
    public int WinCondition { get; set; } = 4;
    public bool IsCylindrical { get; set; } = true;

    public EPlayerType P1Type { get; set; } = EPlayerType.Human;
    public EPlayerType P2Type { get; set; } = EPlayerType.Human;

    public string Player1Name { get; set; } = "Player 1";
    public string Player2Name { get; set; } = "Player 2";
}