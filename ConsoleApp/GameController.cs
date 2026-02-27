using BLL;
using ConsoleUI;
using DAL;

namespace ConsoleApp;

public class GameController
{
    private GameBrain GameBrain { get; set; }

    private string? _currentSaveName;
    private Guid _currentSaveId;

    private readonly IRepository<GameState> _activeRepo;
    private readonly IRepository<GameState> _otherRepo;
    
    // for new game creates a fresh GameBrain 
    public GameController(IRepository<GameState> activeRepo, IRepository<GameState> otherRepo)
    {
        _activeRepo = activeRepo;
        _otherRepo = otherRepo;

        var config = Ui.AskConfiguration();
        Ui.AskPlayerTypes(config);
        SetDefaultPlayerNames(config);
        Ui.AskPlayerNames(config);

        GameBrain = new GameBrain(config);
    }

    // for existing game reconstructs GameBrain and game state
    public GameController(GameState savedState, IRepository<GameState> activeRepo, IRepository<GameState> otherRepo)
    {
        _activeRepo = activeRepo;
        _otherRepo = otherRepo;

        GameBrain = new GameBrain(savedState.Configuration);
        GameBrain.SetBoardFromJagged(savedState.Board);
        GameBrain.NextMoveByX = savedState.IsNextPlayerX;

        _currentSaveId = savedState.Id;
        _currentSaveName = savedState.Name;
    }

    public void GameLoop()
    {
        bool gameOver = false;

        do
        {
            Console.Clear();
            Ui.DrawBoard(GameBrain.GetBoard());
            Ui.ShowNextPlayer(GameBrain.GameConfiguration, GameBrain.IsNextPlayerX());
            Ui.ShowIfCylindrical(GameBrain.GameConfiguration.IsCylindrical);
            Console.WriteLine("Your winning condition is: " + GameBrain.GameConfiguration.WinCondition);

            var currentPlayerType = GameBrain.IsNextPlayerX()
                ? GameBrain.GameConfiguration.P1Type
                : GameBrain.GameConfiguration.P2Type;
            int column;

            if (currentPlayerType == EPlayerType.Ai)
            {
                Console.WriteLine("AI is thinking...");
                Thread.Sleep(1000);
                column = GameBrain.GetAIMove();
                Console.WriteLine($"AI chooses column {column + 1}");
                Thread.Sleep(500);
            }
            else
            {
                // exits loop if player said so
                Console.Write("Choose column (or type 'save' to save and exit): ");
                var input = Console.ReadLine();
                if (input?.Trim().ToLower() == "save")
                {
                    SaveCurrentGame();
                    return;
                }
                
                if (!int.TryParse(input, out column))
                {
                    Console.WriteLine("Please enter a number!");
                    Thread.Sleep(2000);
                    continue;
                }
                
                column--;

                if (column < 0 || column >= GameBrain.GameConfiguration.BoardWidth)
                {
                    Console.WriteLine("There is no such column. Try another one!");
                    Thread.Sleep(2000);
                    continue;
                }
            }

            var piece = GameBrain.IsNextPlayerX() ? ECellState.X : ECellState.O;
            var firstEmptyRow = GameBrain.GetFirstEmptyRow(column);

            if (firstEmptyRow == -1)
            {
                Console.WriteLine("That column is full! Try another one.");
                Thread.Sleep(2000);
                continue;
            }

            Ui.AnimateDrop(GameBrain.GetBoard(), column, piece, firstEmptyRow);
            var row = GameBrain.ProcessMove(column, piece);

            // checks again just in case
            if (row == null)
            {
                Console.WriteLine("That column is full! Try another one.");
                Thread.Sleep(2000);
                continue;
            }

            var winner = GameBrain.GetWinner(column, row.Value);
            if (winner != ECellState.Empty)
            {
                Console.Clear();
                Ui.DrawBoard(GameBrain.GetBoard());
                Ui.ShowWinner(winner, GameBrain.GameConfiguration);
                Thread.Sleep(5000);

                DeleteSaveIfExists();
                gameOver = true;
            }
            else if (GameBrain.IsBoardFullCheck())
            {
                Console.Clear();
                Ui.DrawBoard(GameBrain.GetBoard());
                Console.WriteLine("IT IS A DRAW!!! NO WINNER THIS TIME...😭😭😭");
                Thread.Sleep(5000);

                DeleteSaveIfExists();
                gameOver = true;
            }
        } while (!gameOver);
    }

    private void DeleteSaveIfExists()
    {
        if (_currentSaveId == Guid.Empty) return;

        _activeRepo.Delete(_currentSaveId.ToString());
        _otherRepo.Delete(_currentSaveId.ToString());

        _currentSaveId = Guid.Empty;
        _currentSaveName = null;
    }

    private void SetDefaultPlayerNames(GameConfiguration config)
    {
        if (config.P1Type == EPlayerType.Ai && config.P2Type == EPlayerType.Ai)
        {
            config.Player1Name = "AI1";
            config.Player2Name = "AI2";
        }
        else if (config.P1Type == EPlayerType.Ai)
        {
            config.Player1Name = "AI";
        }
        else if (config.P2Type == EPlayerType.Ai)
        {
            config.Player2Name = "AI";
        }
    }

    private void SaveCurrentGame()
    {
        if (string.IsNullOrWhiteSpace(_currentSaveName))
        {
            while (true)
            {
                Console.Write("Enter a name for this save: ");
                var saveName = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(saveName))
                {
                    _currentSaveName = saveName;
                    break;
                }
                Console.WriteLine("The game should have a name! Not empty!");
            }
        }

        if (_currentSaveId == Guid.Empty)
            _currentSaveId = Guid.NewGuid();

        var state = new GameState
        {
            Id = _currentSaveId,
            Name = _currentSaveName,
            Configuration = GameBrain.GameConfiguration,
            Board = GameBrain.GetBoardJagged(),
            IsNextPlayerX = GameBrain.NextMoveByX
        };

        _activeRepo.Save(state);
        _otherRepo.Save(state);
    
        Console.WriteLine("Game saved!");
        Thread.Sleep(1500);
    }
}
