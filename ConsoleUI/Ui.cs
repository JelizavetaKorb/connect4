using BLL;
using DAL;

namespace ConsoleUI;

public static class Ui
{
    public static void ShowNextPlayer(GameConfiguration config, bool isNextPlayerX)
    {
        var nextPlayerName = isNextPlayerX ? config.Player1Name : config.Player2Name;
        var piece = isNextPlayerX ? "X" : "O";

        Console.WriteLine($"Next move by: {nextPlayerName} with {piece}");
    }

    
    public static void ShowIfCylindrical(bool isCylindrical)
    {
        Console.WriteLine(isCylindrical
            ? "Remember, that you board IS CYLINDRICAL!!"
            : "Remember, that you board IS NOT CYLINDRICAL!");
    }

    public static void DrawBoard(ECellState[,] gameBoard)
    {
        //numbers of columns
        for (int x = 0; x < gameBoard.GetLength(0); x++)
        {
            Console.Write(GetNumberRepresentation(x + 1));
            if (x < gameBoard.GetLength(0) - 1) Console.Write("|");
        }
        Console.WriteLine();

        //grid
        for (int y = 0; y < gameBoard.GetLength(1); y++)
        {
            for (int x = 0; x < gameBoard.GetLength(0); x++)
            {
                Console.Write("---");
                if (x < gameBoard.GetLength(0) - 1) Console.Write("+");
            }
            Console.WriteLine();
            
            for (int x = 0; x < gameBoard.GetLength(0); x++)
            {
                Console.Write(GetCellRepresentation(gameBoard[x, y]));
                if (x < gameBoard.GetLength(0) - 1) Console.Write("|");
            }
            Console.WriteLine();
        }
    }

    private static string GetNumberRepresentation(int number)
    {
        return " " + (number < 10 ? "0" + number : number.ToString());
    }

    private static string GetCellRepresentation(ECellState cellValue) =>
        cellValue switch
        {
            ECellState.Empty => "   ",
            ECellState.X => " X ",
            ECellState.O => " O ",
            ECellState.XWin => "XXX",
            ECellState.OWin => "OOO",
            _ => " ? "
        };
    
    public static void ShowWinner(ECellState winner, GameConfiguration config)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("🎉🎉🎉 GAME OVER  🎉🎉🎉");
        Console.WriteLine();

        if (winner == ECellState.XWin)
            Console.WriteLine($"Winner is: {config.Player1Name}");
        else if (winner == ECellState.OWin)
            Console.WriteLine($"Winner is: {config.Player2Name}");
        else
            Console.WriteLine("No winner this time.");
    }
    
    public static void AnimateDrop(ECellState[,] gameBoard, int column, ECellState piece, int finalRow)
    {
        for (int row = 0; row <= finalRow; row++)
        {
            gameBoard[column, row] = piece;

            Console.Clear();
            DrawBoard(gameBoard);
            Thread.Sleep(200);
            
            if (row != finalRow)
            {
                gameBoard[column, row] = ECellState.Empty;
            }
        }
    }

    public static GameConfiguration AskConfiguration()
    {
        string? input;
        var defaultConfig = new GameConfiguration();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("Game Configuration Settings");
            Console.WriteLine("---------------------------");

            Console.WriteLine("Default right now is...");
            Console.WriteLine($"Board Width: {defaultConfig.BoardWidth}");
            Console.WriteLine($"Board Height: {defaultConfig.BoardHeight}");
            Console.WriteLine($"Win Condition: {defaultConfig.WinCondition}");
            Console.WriteLine($"Cylindrical: {(defaultConfig.IsCylindrical ? "Yes" : "No")}\n");

            Console.Write("Do you want to customize the configuration? (y/n): ");
            input = Console.ReadLine()?.Trim().ToLower();

            if (input is "y" or "yes" or "n" or "no")
                break;

            Console.WriteLine("Invalid input. Try again.");
            Thread.Sleep(1000);
        }

        if (input is "n" or "no")
        {
            Console.WriteLine("Using default configuration then.");
            Thread.Sleep(1000);
            return defaultConfig;
        }
        
        var config = new GameConfiguration();
        
        while (true)
        {
            Console.Clear();
            Console.Write("Enter board width (between 2 and 38): ");
            if (int.TryParse(Console.ReadLine(), out int width) && width >= 3 && width < 38)
            {
                config.BoardWidth = width;
                break;
            }

            Console.WriteLine("Invalid input. Try again!");
            Thread.Sleep(1000);
        }

        while (true)
        {
            Console.Clear();
            Console.Write("Enter board height (between 2 and 38): ");
            if (int.TryParse(Console.ReadLine(), out var height) && height >= 3 && height < 38)
            {
                config.BoardHeight = height;
                break;
            }

            Console.WriteLine("Invalid input. Try again!");
            Thread.Sleep(1000);
        }

        while (true)
        {
            Console.Clear();
            var maxWin = Math.Min(config.BoardWidth, config.BoardHeight);
            Console.Write($"Enter win condition (between 2 and {maxWin + 1}): ");
            if (int.TryParse(Console.ReadLine(), out var win) && win >= 3 && win <= maxWin)
            {
                config.WinCondition = win;
                break;
            }

            Console.WriteLine(
                $"Invalid input. Win condition must be a number between 2 and no larger than the smallest board side ({maxWin}). Try again.");
            Thread.Sleep(2000);
        }

        while (true)
        {
            Console.Clear();
            Console.Write("Enable cylindrical board? (y/n): ");
            input = Console.ReadLine()?.Trim().ToLower();

            if (input is "y" or "yes")
            {
                config.IsCylindrical = true;
                break;
            }

            if (input is "n" or "no")
            {
                config.IsCylindrical = false;
                break;
            }

            Console.WriteLine("Invalid input: enter 'y' or 'n'. Try again.");
            Thread.Sleep(1000);
        }

        Console.WriteLine("Game is set! Starting...");
        Thread.Sleep(1000);
        return config;
    }
    
    public static void AskPlayerNames(GameConfiguration config)
    {
        if (config.P1Type == EPlayerType.Human)
        {
            Console.Write("Enter player 1 name: ");
            var p1 = Console.ReadLine()?.Trim();
            config.Player1Name = string.IsNullOrWhiteSpace(p1) ? "Player 1" : p1;
        }

        if (config.P2Type == EPlayerType.Human)
        {
            Console.Write("Enter player 2 name: ");
            var p2 = Console.ReadLine()?.Trim();
            config.Player2Name = string.IsNullOrWhiteSpace(p2) ? "Player 2" : p2;
        }
    }

    public static void DisplayAndDeleteSavedGames(IRepository<GameState> activeRepo, IRepository<GameState> otherRepo)
    {
        var saves = activeRepo.List();
        if (saves.Count == 0)
        {
            Console.WriteLine("No saved games to delete.");
            Thread.Sleep(1500);
            return;
        }

        Console.WriteLine("Saved games:");
        for (int i = 0; i < saves.Count; i++)
            Console.WriteLine($"{i + 1}) {saves[i].description}");

        int choice;
        while (true)
        {
            Console.Write("Enter number to delete, 0 to cancel: ");
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

        if (choice == 0) return;

        var saveId = saves[choice - 1].id;

        activeRepo.Delete(saveId);
        otherRepo.Delete(saveId);

        Console.WriteLine($"Game '{saves[choice - 1].description}' is deleted!");
        Thread.Sleep(1500);
    }
    
    public static void AskPlayerTypes(GameConfiguration config)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select Player 1 type:");
            Console.WriteLine("1) Human");
            Console.WriteLine("2) AI");
            Console.Write("Choice: ");
            var input = Console.ReadLine()?.Trim();

            if (input == "1")
            {
                config.P1Type = EPlayerType.Human;
                break;
            }
            if (input == "2")
            {
                config.P1Type = EPlayerType.Ai;
                break;
            }
        
            Console.WriteLine("Invalid input. Try again.");
            Thread.Sleep(1000);
        }

        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select Player 2 type:");
            Console.WriteLine("1) Human");
            Console.WriteLine("2) AI");
            Console.Write("Choice: ");
            var input = Console.ReadLine()?.Trim();

            if (input == "1")
            {
                config.P2Type = EPlayerType.Human;
                break;
            }
            if (input == "2")
            {
                config.P2Type = EPlayerType.Ai;
                break;
            }
        
            Console.WriteLine("Invalid input. Try again.");
            Thread.Sleep(1000);
        }
    }
}
