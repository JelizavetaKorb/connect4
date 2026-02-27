using BLL;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Pages;

public class ConfigureGameModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly MultiplayerSessionManager _sessionManager;
    private readonly RepositoryProvider _repoProvider;

    public ConfigureGameModel(AppDbContext context, MultiplayerSessionManager sessionManager, RepositoryProvider repoProvider)
    {
        _context = context;
        _sessionManager = sessionManager;
        _repoProvider = repoProvider;
    }

    public string? Username { get; set; }
    public GameConfiguration config { get; set; } = new GameConfiguration();
    
    // checks if user is logged in and displays the game configuration form page
    public IActionResult OnGet()
    {
        Username = HttpContext.Session.GetCurrentUsername();
        
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
        return Page();
    }

    // Validates board dimensions and win condition, determines player
    // types, creates new game with configuration, generates join code
    // if multiplayer invite, saves game, and redirects to game page
    public IActionResult OnPost(int boardWidth, int boardHeight, int winCondition,
        string p1Type, string p2Type, bool isCylindrical,
        string player1Name = "", string player2Name = "", int userPlayer = 1, bool invitePlayer2 = false)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }

        if (boardWidth < 3 || boardWidth > 37)
        {
            config = new GameConfiguration
            {
                BoardWidth = boardWidth,
                BoardHeight = boardHeight,
                WinCondition = winCondition
            };
            ModelState.AddModelError("boardWidth", "Board width must be between 3 and 37");
            return Page();
        }

        if (boardHeight < 3 || boardHeight > 37)
        {
            config = new GameConfiguration
            {
                BoardWidth = boardWidth,
                BoardHeight = boardHeight,
                WinCondition = winCondition
            };
            ModelState.AddModelError("boardHeight", "Board height must be between 3 and 37");
            return Page();
        }

        var maxWin = Math.Min(boardWidth, boardHeight);
        if (winCondition > maxWin)
        {
            config = new GameConfiguration
            {
                BoardWidth = boardWidth,
                BoardHeight = boardHeight,
                WinCondition = winCondition
            };
            ModelState.AddModelError("winCondition",
                $"Win condition must be no larger than the smallest board dimension ({maxWin})");
            return Page();
        }

        var p1IsAi = p1Type == "AI";
        var p2IsAi = p2Type == "AI";

        if (userPlayer == 1)
        {
            player1Name = Username;
            p1IsAi = false;

            if (invitePlayer2)
            {
                player2Name = "Waiting for Player 2";
                p2IsAi = false;
            }
            else if (p2IsAi)
            {
                player2Name = "AI";
            }
            else
            {
                var defaultConfig = new GameConfiguration();
                player2Name = string.IsNullOrWhiteSpace(player2Name) ? defaultConfig.Player2Name : player2Name.Trim();
            }
        }
        else
        {
            player2Name = Username;
            p2IsAi = false;

            if (invitePlayer2)
            {
                player1Name = "Waiting for Player 1";
                p1IsAi = false;
            }
            else if (p1IsAi)
            {
                player1Name = "AI";
            }
            else
            {
                var defaultConfig = new GameConfiguration();
                player1Name = string.IsNullOrWhiteSpace(player1Name) ? defaultConfig.Player1Name : player1Name.Trim();
            }
        }

        var gameConfig = new GameConfiguration
        {
            BoardWidth = boardWidth,
            BoardHeight = boardHeight,
            WinCondition = winCondition,
            P1Type = p1IsAi ? EPlayerType.Ai : EPlayerType.Human,
            P2Type = p2IsAi ? EPlayerType.Ai : EPlayerType.Human,
            IsCylindrical = isCylindrical,
            Player1Name = player1Name,
            Player2Name = player2Name
        };

        var gameBrain = new GameBrain(gameConfig);
        var gameId = Guid.NewGuid();

        var gameState = new GameState
        {
            Id = gameId,
            Name = invitePlayer2 ? "Multiplayer Game" : string.Empty,
            Configuration = gameConfig,
            Board = gameBrain.GetBoardJagged(),
            IsNextPlayerX = gameBrain.IsNextPlayerX()
        };

        var gameJson = System.Text.Json.JsonSerializer.Serialize(gameState);
        HttpContext.Session.SetString("GameBrain", gameJson);
        HttpContext.Session.SetString("CurrentGameId", gameId.ToString());

        if (invitePlayer2)
        {
            var joinCode = _sessionManager.CreateSession(gameId.ToString(), Username);

            _repoProvider.ActiveRepo.Save(gameState);
            _repoProvider.OtherRepo.Save(gameState);

            HttpContext.Session.SetString("GameName", gameState.Name);
            TempData["JoinCode"] = joinCode;
            TempData["IsWaitingForPlayer"] = true;
        }
        else
        {
            HttpContext.Session.Remove("GameName");
            HttpContext.Session.Remove("CurrentGameId");
        }
        return RedirectToPage("/Game");
    }
}