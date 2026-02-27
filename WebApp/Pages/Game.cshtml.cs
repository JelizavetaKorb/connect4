using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL;
using DAL;
using WebApp.Services;

namespace WebApp.Pages;

public class GameModel : PageModel
{
    private readonly IRepository<GameState> _activeRepo;
    private readonly IRepository<GameState> _otherRepo;
    private readonly MultiplayerSessionManager _sessionManager;

    public GameModel(RepositoryProvider repoProvider, MultiplayerSessionManager sessionManager)
    {
        _activeRepo = repoProvider.ActiveRepo;
        _otherRepo = repoProvider.OtherRepo;
        _sessionManager = sessionManager;
    }

    public string? Username { get; set; }
    public GameBrain? GameBrain { get; set; }
    public string GameStatus { get; set; } = "Game in progress";
    public bool IsGameOver { get; set; }
    public string? GameName { get; set; }
    public bool IsMultiplayerGame { get; set; }
    public string? OpponentName { get; set; }
    public string? CurrentGameId { get; set; }

    // loads the game page, checks if user is logged in, and loads
    // the current game from either session or database depending on
    // if it's multiplayer
    public IActionResult OnGet()
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }

        CurrentGameId = HttpContext.Session.GetString("CurrentGameId");
        GameName = HttpContext.Session.GetString("GameName");

        if (!string.IsNullOrEmpty(CurrentGameId))
        {
            IsMultiplayerGame = _sessionManager.IsMultiplayerGame(CurrentGameId);
            if (IsMultiplayerGame)
            {
                var session = _sessionManager.GetSession(CurrentGameId);
                if (session != null && _sessionManager.DoesOpponentWantToExit(CurrentGameId, Username))
                {
                    TempData["SaveMessage"] = "Your opponent has exited the game.";
                    HttpContext.Session.Remove("GameBrain");
                    HttpContext.Session.Remove("CurrentGameId");
                    HttpContext.Session.Remove("GameName");
                    HttpContext.Session.Remove("LastKnownTurn");
                    return RedirectToPage("/ChooseGame");
                }
                LoadGameFromRepository(CurrentGameId);
            }
            else
            {
                LoadGameFromSession();
            }
        }
        else
        {
            LoadGameFromSession();
        }

        if (IsMultiplayerGame && GameBrain != null)
        {
            var isPlayer1 =
                GameBrain.GameConfiguration.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase);
            OpponentName = isPlayer1
                ? GameBrain.GameConfiguration.Player2Name
                : GameBrain.GameConfiguration.Player1Name;
        }

        return Page();
    }

    // checks if opponent made a move, if they left, or if game ended
    public IActionResult OnGetCheckGameState(string gameId)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return new JsonResult(new { success = false, message = "Not logged in" });
        }
        GameState? savedGame = null;
        try
        {
            savedGame = _activeRepo.Load(gameId);
        }
        catch
        {
            return new JsonResult(new { gameDeleted = true, shouldRefresh = false, isMultiplayer = false });
        }

        if (savedGame == null)
        {
            return new JsonResult(new { gameDeleted = true, shouldRefresh = false, isMultiplayer = false });
        }

        var session = _sessionManager.GetSession(gameId);

        if (session == null)
        {
            return new JsonResult(new { shouldRefresh = false, isMultiplayer = false });
        }

        _sessionManager.UpdateActivity(gameId, Username);
        bool opponentActive = _sessionManager.IsOpponentActive(gameId, Username);
        bool opponentWantsToExit = _sessionManager.DoesOpponentWantToExit(gameId, Username);
        bool shouldRefresh = false;
        bool player2Joined = false;
        bool gameOver = false;

        if (!savedGame.Configuration.Player2Name.StartsWith("[Waiting"))
        {
            player2Joined = true;
        }

        var tempBrain = new GameBrain(savedGame.Configuration);
        tempBrain.SetBoardFromJagged(savedGame.Board);
        var board = tempBrain.GetBoard();

        for (int x = 0; x < savedGame.Configuration.BoardWidth && !gameOver; x++)
        {
            for (int y = 0; y < savedGame.Configuration.BoardHeight && !gameOver; y++)
            {
                if (board[x, y] != ECellState.Empty)
                {
                    var winner = tempBrain.GetWinner(x, y);
                    if (winner != ECellState.Empty)
                    {
                        gameOver = true;
                    }
                }
            }
        }

        var gameBrainJson = HttpContext.Session.GetString("LastKnownTurn");
        if (!string.IsNullOrEmpty(gameBrainJson))
        {
            bool lastKnownIsPlayerX = bool.Parse(gameBrainJson);
            shouldRefresh = savedGame.IsNextPlayerX != lastKnownIsPlayerX;
        }
        else
        {
            shouldRefresh = true;
        }

        return new JsonResult(new
        {
            shouldRefresh,
            opponentLeft = !opponentActive,
            opponentWantsToExit,
            player2Joined,
            gameOver,
            gameDeleted = false,
            isMultiplayer = true
        });
    }

    // clears current game data and sends user to create a new game
    public IActionResult OnPostNewGame()
    {
        Username = HttpContext.Session.GetCurrentUsername();
    
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
    
        var gameId = HttpContext.Session.GetString("CurrentGameId");
        if (!string.IsNullOrEmpty(gameId))
        {
            var isMultiplayer = _sessionManager.IsMultiplayerGame(gameId);
        
            if (isMultiplayer)
            {
                _sessionManager.MarkPlayerLeft(gameId, Username);
                if (_sessionManager.BothPlayersHaveLeft(gameId))
                {
                    _activeRepo.Delete(gameId);
                    _otherRepo.Delete(gameId);
                }
            }
            else
            {
                _sessionManager.RemoveSession(gameId);
            }
        }
    
        HttpContext.Session.Remove("GameBrain");
        HttpContext.Session.Remove("CurrentGameId");
        HttpContext.Session.Remove("GameName");
        HttpContext.Session.Remove("LastKnownTurn");
        return RedirectToPage("/ConfigureGame");
    }
    
    // exits current game, marks player as left, deletes game if both players left,
    // then goes back to game selection screen
    public IActionResult OnPostLeaveGame()
    {
        Username = HttpContext.Session.GetCurrentUsername();
    
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
    
        var gameId = HttpContext.Session.GetString("CurrentGameId");
        if (!string.IsNullOrEmpty(gameId))
        {
            var isMultiplayer = _sessionManager.IsMultiplayerGame(gameId);
        
            if (isMultiplayer)
            {
                _sessionManager.MarkPlayerLeft(gameId, Username);
                if (_sessionManager.BothPlayersHaveLeft(gameId))
                {
                    _activeRepo.Delete(gameId);
                    _otherRepo.Delete(gameId);
                }
            }
        }
    
        HttpContext.Session.Remove("GameBrain");
        HttpContext.Session.Remove("CurrentGameId");
        HttpContext.Session.Remove("GameName");
        HttpContext.Session.Remove("LastKnownTurn");
    
        return RedirectToPage("/ChooseGame");
    }

    // saves the current game with a name and marks if player wants to exit multiplayer
    public IActionResult OnPostSaveGame(string saveName)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }

        var gameStateId = HttpContext.Session.GetString("CurrentGameId");
        bool isMultiplayer = false;

        if (!string.IsNullOrEmpty(gameStateId))
        {
            isMultiplayer = _sessionManager.IsMultiplayerGame(gameStateId);
        }

        if (isMultiplayer && !string.IsNullOrEmpty(gameStateId))
        {
            LoadGameFromRepository(gameStateId);
        }
        else
        {
            LoadGameFromSession();
        }

        if (GameBrain != null)
        {
            Guid currentSaveId;

            if (string.IsNullOrEmpty(gameStateId))
            {
                currentSaveId = Guid.NewGuid();
            }
            else
            {
                currentSaveId = Guid.Parse(gameStateId);
            }
            string finalName;
            if (isMultiplayer)
            {
                finalName = $"{GameBrain.GameConfiguration.Player1Name} vs {GameBrain.GameConfiguration.Player2Name}";
            }
            else
            {
                finalName = string.IsNullOrWhiteSpace(saveName) ? "Saved Game" : saveName.Trim();
            }

            var state = new GameState
            {
                Id = currentSaveId,
                Name = finalName,
                Configuration = GameBrain.GameConfiguration,
                Board = GameBrain.GetBoardJagged(),
                IsNextPlayerX = GameBrain.IsNextPlayerX()
            };

            _activeRepo.Save(state);
            _otherRepo.Save(state);

            if (isMultiplayer)
            {
                _sessionManager.SetPlayerWantsToExit(currentSaveId.ToString(), Username, true);
            }

            HttpContext.Session.SetString("GameName", finalName);
            HttpContext.Session.SetString("CurrentGameId", currentSaveId.ToString());
            HttpContext.Session.Remove("GameBrain");
            HttpContext.Session.Remove("LastKnownTurn");

            TempData["SaveMessage"] = "Game saved successfully!";
            return RedirectToPage("/ChooseGame");
        }

        return Page();
    }

    // makes AI calculate and play its move, checks if game ended and returns result as JSON.
    public IActionResult OnPostAIMove()
    {
        Username = HttpContext.Session.GetCurrentUsername();

        var gameId = HttpContext.Session.GetString("CurrentGameId");
        bool isMultiplayer = !string.IsNullOrEmpty(gameId) && _sessionManager.IsMultiplayerGame(gameId);

        if (isMultiplayer)
        {
            LoadGameFromRepository(gameId!);
        }
        else
        {
            LoadGameFromSession();
        }

        if (GameBrain != null && !IsGameOver)
        {
            var column = GameBrain.GetAIMove();
            var piece = GameBrain.IsNextPlayerX() ? ECellState.X : ECellState.O;
            var row = GameBrain.ProcessMove(column, piece);

            if (row.HasValue)
            {
                var winner = GameBrain.GetWinner(column, row.Value);
                bool gameOver = false;

                if (winner != ECellState.Empty)
                {
                    IsGameOver = true;
                    gameOver = true;
                    var winnerName = winner == ECellState.XWin
                        ? GameBrain.GameConfiguration.Player1Name
                        : GameBrain.GameConfiguration.Player2Name;
                    GameStatus = $"{winnerName} WINS! 🎉";

                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                    }
                    else
                    {
                        SaveToSession();
                        DeleteSavedGame();
                    }
                }
                else if (GameBrain.IsBoardFullCheck())
                {
                    IsGameOver = true;
                    gameOver = true;
                    GameStatus = "IT'S A DRAW! NO WINNER THIS TIME... 😭";

                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                    }
                    else
                    {
                        SaveToSession();
                        DeleteSavedGame();
                    }
                }
                else
                {
                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                        _sessionManager.UpdateActivity(gameId!, Username);
                        HttpContext.Session.SetString("LastKnownTurn", GameBrain.IsNextPlayerX().ToString());
                    }
                    else
                    {
                        SaveToSession();
                    }
                }

                return new JsonResult(new
                {
                    success = true,
                    dropColumn = column,
                    dropRow = row.Value,
                    piece = piece == ECellState.X ? "X" : "O",
                    gameOver = gameOver
                });
            }
        }

        return new JsonResult(new { success = false, message = "Error processing AI move" });
    }
    
    // processes player's move, validates turn order for multiplayer,
    // checks win/draw, and returns result as JSON
    public IActionResult OnPostMakeMoveAjax(int column, string userId)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return new JsonResult(new { success = false, message = "Not logged in" });
        }

        var gameId = HttpContext.Session.GetString("CurrentGameId");
        bool isMultiplayer = !string.IsNullOrEmpty(gameId) && _sessionManager.IsMultiplayerGame(gameId);
        if (isMultiplayer)
        {
            LoadGameFromRepository(gameId!);
        }
        else
        {
            LoadGameFromSession();
        }

        if (GameBrain != null && !IsGameOver)
        {
            if (isMultiplayer)
            {
                var isPlayer1 =
                    GameBrain.GameConfiguration.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase);
                var isMyTurn = (isPlayer1 && GameBrain.IsNextPlayerX()) || (!isPlayer1 && !GameBrain.IsNextPlayerX());

                if (!isMyTurn)
                {
                    return new JsonResult(new { success = false, message = "It's not your turn!" });
                }
            }

            int col = column - 1;

            if (col < 0 || col >= GameBrain.GameConfiguration.BoardWidth)
            {
                return new JsonResult(new { success = false, message = "No such column! Try another one." });
            }

            var firstEmptyRow = GameBrain.GetFirstEmptyRow(col);
            if (firstEmptyRow == -1)
            {
                return new JsonResult(new { success = false, message = "Column is full! Try another one." });
            }

            var piece = GameBrain.IsNextPlayerX() ? ECellState.X : ECellState.O;
            var row = GameBrain.ProcessMove(col, piece);

            if (row.HasValue)
            {
                var winner = GameBrain.GetWinner(col, row.Value);
                bool gameOver = false;

                if (winner != ECellState.Empty)
                {
                    IsGameOver = true;
                    gameOver = true;

                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                    }
                    else
                    {
                        SaveToSession();
                        DeleteSavedGame();
                    }
                }
                else if (GameBrain.IsBoardFullCheck())
                {
                    IsGameOver = true;
                    gameOver = true;

                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                    }
                    else
                    {
                        SaveToSession();
                        DeleteSavedGame();
                    }
                }
                else
                {
                    if (isMultiplayer)
                    {
                        SaveToRepository(gameId!);
                        _sessionManager.UpdateActivity(gameId!, Username);
                        HttpContext.Session.SetString("LastKnownTurn", GameBrain.IsNextPlayerX().ToString());
                    }
                    else
                    {
                        SaveToSession();
                    }
                }

                return new JsonResult(new
                {
                    success = true,
                    dropColumn = col,
                    dropRow = row.Value,
                    piece = piece == ECellState.X ? "X" : "O",
                    gameOver = gameOver
                });
            }
        }
        return new JsonResult(new { success = false, message = "Error processing move" });
    }
    
    // saves current game state to both database repositories
    private void SaveToRepository(string gameId)
    {
        if (GameBrain == null) return;
        
        var state = new GameState
        {
            Id = Guid.Parse(gameId),
            Name = GameName ?? "Multiplayer Game",
            Configuration = GameBrain.GameConfiguration,
            Board = GameBrain.GetBoardJagged(),
            IsNextPlayerX = GameBrain.IsNextPlayerX()
        };

        _activeRepo.Save(state);
        _otherRepo.Save(state);
    }
    
    // converts game to JSON and stores it in session for single-player games
    private void SaveToSession()
    {
        if (GameBrain == null) return;

        var gameState = new GameState
        {
            Name = GameName ?? string.Empty,
            Configuration = GameBrain.GameConfiguration,
            Board = GameBrain.GetBoardJagged(),
            IsNextPlayerX = GameBrain.IsNextPlayerX()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(gameState);
        HttpContext.Session.SetString("GameBrain", json);
    }

    // removes saved game from database, session manager and session storage
    private void DeleteSavedGame()
    {
        var gameStateId = HttpContext.Session.GetString("CurrentGameId");
        if (!string.IsNullOrEmpty(gameStateId))
        { 
            _activeRepo.Delete(gameStateId);
            _otherRepo.Delete(gameStateId);
            _sessionManager.RemoveSession(gameStateId);
            HttpContext.Session.Remove("CurrentGameId");
            HttpContext.Session.Remove("GameName");
            HttpContext.Session.Remove("LastKnownTurn");
        }
    }

    // gets game by ID, recreates GameBrain object, and checks if game still exists
    private void LoadGameFromRepository(string gameId)
    {
        try
        {
            var savedGame = _activeRepo.Load(gameId);
            if (savedGame != null)
            {
                GameBrain = new GameBrain(savedGame.Configuration);
                GameBrain.SetBoardFromJagged(savedGame.Board);
                GameBrain.NextMoveByX = savedGame.IsNextPlayerX;
            
                if (string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(savedGame.Name))
                {
                    GameName = savedGame.Name;
                }
                HttpContext.Session.SetString("LastKnownTurn", savedGame.IsNextPlayerX.ToString());
            
                CheckGameStatus();
            }
            else
            {
                TempData["GameDeleted"] = true;
                TempData["DeletedGameName"] = GameName ?? "This game";
                GameStatus = "This game has been deleted by another player.";
            }
        }
        catch
        {
            GameStatus = "Error loading game. Please start a new game.";
        }
    }
    
    // permanently deletes a multiplayer game
    public IActionResult OnPostDeleteMultiplayerGame(string gameId)
    {
        Username = HttpContext.Session.GetCurrentUsername();
    
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
    
        _activeRepo.Delete(gameId);
        _otherRepo.Delete(gameId);
        _sessionManager.RemoveSession(gameId);
    
        HttpContext.Session.Remove("CurrentGameId");
        HttpContext.Session.Remove("GameName");
        HttpContext.Session.Remove("GameBrain");
        HttpContext.Session.Remove("LastKnownTurn");
    
        return new JsonResult(new { success = true });
    }

    // gets game from session, deserializes it back to GameBrain object
    private void LoadGameFromSession()
    {
        var gameBrainJson = HttpContext.Session.GetString("GameBrain");
        if (!string.IsNullOrEmpty(gameBrainJson))
        {
            try
            {
                var gameState = System.Text.Json.JsonSerializer.Deserialize<GameState>(gameBrainJson);
                if (gameState != null)
                {
                    GameBrain = new GameBrain(gameState.Configuration);
                    GameBrain.SetBoardFromJagged(gameState.Board);
                    GameBrain.NextMoveByX = gameState.IsNextPlayerX;
                    
                    if (string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(gameState.Name))
                    {
                        GameName = gameState.Name;
                        HttpContext.Session.SetString("GameName", gameState.Name);
                    }
                    CheckGameStatus();
                }
            }
            catch
            {
                GameStatus = "Error loading game. Please start a new game.";
            }
        }
        else
        {
            GameStatus = "No active game. Please start a new game.";
        }
    }

    // scans entire board to see if anyone won or if it's a draw, then updates game status message
    private void CheckGameStatus()
    {
        if (GameBrain == null) return;
        var board = GameBrain.GetBoard();
        for (int x = 0; x < GameBrain.GameConfiguration.BoardWidth; x++)
        {
            for (int y = 0; y < GameBrain.GameConfiguration.BoardHeight; y++)
            {
                if (board[x, y] != ECellState.Empty)
                {
                    var winner = GameBrain.GetWinner(x, y);
                    if (winner != ECellState.Empty)
                    {
                        IsGameOver = true;
                        var winnerName = winner == ECellState.XWin 
                            ? GameBrain.GameConfiguration.Player1Name 
                            : GameBrain.GameConfiguration.Player2Name;
                        GameStatus = $"{winnerName} WINS! 🎉";
                        return;
                    }
                }
            }
        }
        if (GameBrain.IsBoardFullCheck())
        {
            IsGameOver = true;
            GameStatus = "IT'S A DRAW! NO WINNER THIS TIME... 😭";
        }
        else
        {
            GameStatus = "Game in progress";
        }
    }
}