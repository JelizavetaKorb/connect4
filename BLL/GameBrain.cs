namespace BLL;

public class GameBrain
{
    private ECellState[,] GameBoard { get; set; }
    public GameConfiguration GameConfiguration { get; set; }

    public bool NextMoveByX { get; set; } = true;

    public GameBrain(GameConfiguration configuration)
    {
        GameConfiguration = configuration;
        GameBoard = new ECellState[configuration.BoardWidth, configuration.BoardHeight];
    }

    //returns a copy of board to protect the game's internal state
    public ECellState[,] GetBoard()
    {
        var gameBoardCopy = new ECellState[GameConfiguration.BoardWidth, GameConfiguration.BoardHeight];
        Array.Copy(GameBoard, gameBoardCopy, GameBoard.Length);
        return gameBoardCopy;
    }
    
    // converts 2D array into jagged for saving/loading games
    public ECellState[][] GetBoardJagged()
    {
        var jagged = new ECellState[GameConfiguration.BoardHeight][];
        for (int r = 0; r < GameConfiguration.BoardHeight; r++)
        {
            jagged[r] = new ECellState[GameConfiguration.BoardWidth];
            for (int c = 0; c < GameConfiguration.BoardWidth; c++)
                jagged[r][c] = GameBoard[c, r];
        }
        return jagged;
    }

    // vice versa. from jagged to 2D array. loads a saved game
    public void SetBoardFromJagged(ECellState[][] jagged)
    {
        GameBoard = new ECellState[GameConfiguration.BoardWidth, GameConfiguration.BoardHeight];
        for (int r = 0; r < GameConfiguration.BoardHeight; r++)
        for (int c = 0; c < GameConfiguration.BoardWidth; c++)
            GameBoard[c, r] = jagged[r][c];
    }
    
    public bool IsNextPlayerX() => NextMoveByX;
    
    public int? ProcessMove(int column, ECellState piece)
    {
        int row = GetFirstEmptyRow(column);
        if (row == -1) return null;
        GameBoard[column, row] = piece;
        NextMoveByX = !NextMoveByX;
        return row;
    }
    
    public int GetFirstEmptyRow(int column)
    {
        for (int row = GameConfiguration.BoardHeight - 1; row >= 0; row--)
            if (GameBoard[column, row] == ECellState.Empty)
                return row;
        return -1;
    }
    
    //returns direction vector
    private (int dirX, int dirY) GetDirection(int directionIndex) =>
        directionIndex switch
        {
            0 => (-1, -1), // diagonal up-left
            1 => (0, -1), // vertical
            2 => (1, -1), // diagonal up-right
            3 => (1, 0), // horizontal
            _ => (0, 0)
        };

    private (int dirX, int dirY) FlipDirection((int dirX, int dirY) direction) =>
        (-direction.dirX, -direction.dirY);
    
    // after a piece placement checks in four direction for a winning combination
    public ECellState GetWinner(int x, int y)
    {
        if (GameBoard[x, y] == ECellState.Empty) return ECellState.Empty;

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            var (dirX, dirY) = GetDirection(directionIndex);
            var count = 1;
            
            var nextX = x + dirX;
            var nextY = y + dirY;
            
            while (IsSame(nextX, nextY, x, y))
            {
                count++;
                nextX += dirX;
                nextY += dirY;
            }
            
            (dirX, dirY) = FlipDirection((dirX, dirY));
            nextX = x + dirX;
            nextY = y + dirY;

            while (IsSame(nextX, nextY, x, y))
            {
                count++;
                nextX += dirX;
                nextY += dirY;
            }

            if (count >= GameConfiguration.WinCondition)
            {
                return GameBoard[x, y] == ECellState.X ? ECellState.XWin : ECellState.OWin;
            }
        }
        return ECellState.Empty;
    }
    
    // for cylindrical board calculates opposite indexes of a piece
    private bool IsSame(int nextX, int nextY, int startX, int startY)
    {
        if (GameConfiguration.IsCylindrical)
        {
            nextX = (nextX + GameConfiguration.BoardWidth) % GameConfiguration.BoardWidth;
        }
        else
        {
            if (nextX < 0 || nextX >= GameConfiguration.BoardWidth)
                return false;
        }
        if (nextY < 0 || nextY >= GameConfiguration.BoardHeight)
            return false;
        
        return GameBoard[nextX, nextY] == GameBoard[startX, startY];
    }

    public int GetAIMove()
    {
        int bestMove = -1;
        int bestScore = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        // bigger board - less depth
        int totalCells = GameConfiguration.BoardWidth * GameConfiguration.BoardHeight;
        int depth = totalCells > 42 ? 3 : (totalCells > 30 ? 4 : 5);

        // immediate win check
        for (int col = 0; col < GameConfiguration.BoardWidth; col++)
        {
            if (GetFirstEmptyRow(col) == -1) continue;

            int row = GetFirstEmptyRow(col);
            var piece = NextMoveByX ? ECellState.X : ECellState.O;
            GameBoard[col, row] = piece;

            var winner = GetWinner(col, row);
            GameBoard[col, row] = ECellState.Empty;

            if (winner != ECellState.Empty)
                return col;
        }

        // blocks opponent's win, if possible
        bool originalTurn = NextMoveByX;
        NextMoveByX = !NextMoveByX;

        for (int col = 0; col < GameConfiguration.BoardWidth; col++)
        {
            if (GetFirstEmptyRow(col) == -1) continue;

            int row = GetFirstEmptyRow(col);
            var piece = NextMoveByX ? ECellState.X : ECellState.O;
            GameBoard[col, row] = piece;

            var winner = GetWinner(col, row);
            GameBoard[col, row] = ECellState.Empty;

            if (winner != ECellState.Empty)
            {
                NextMoveByX = originalTurn;
                return col;
            }
        }
        NextMoveByX = originalTurn;

        // minimax
        var orderedColumns = GetOrderedColumns();
        foreach (int col in orderedColumns)
        {
            if (GetFirstEmptyRow(col) == -1) continue;

            int row = GetFirstEmptyRow(col);
            var piece = NextMoveByX ? ECellState.X : ECellState.O;
            GameBoard[col, row] = piece;
            NextMoveByX = !NextMoveByX;

            int score = Minimax(depth - 1, false, alpha, beta);

            NextMoveByX = !NextMoveByX;
            GameBoard[col, row] = ECellState.Empty;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = col;
            }

            alpha = Math.Max(alpha, bestScore);
            if (beta <= alpha)
                break;
        }
        return bestMove != -1 ? bestMove : GetFirstAvailableMove();
    }

    private int GetFirstAvailableMove()
    {
        int center = GameConfiguration.BoardWidth / 2;
        if (GetFirstEmptyRow(center) != -1)
            return center;

        for (int col = 0; col < GameConfiguration.BoardWidth; col++)
        {
            if (GetFirstEmptyRow(col) != -1)
                return col;
        }

        return 0;
    }

    private int Minimax(int depth, bool isMaximizing, int alpha, int beta)
    {
        // base cases - delaying lose and preferring for faster wins
        var winner = CheckBoardForWinner();
        if (winner == ECellState.XWin)
            return 10000 - depth;
        if (winner == ECellState.OWin)
            return -10000 + depth;
        if (depth == 0 || IsBoardFullCheck())
            return EvaluateBoard();

        
        var orderedColumns = GetOrderedColumns();
        
        // maximizing for us
        if (isMaximizing)
        {
            int maxEval = int.MinValue;

            foreach (int col in orderedColumns)
            {
                if (GetFirstEmptyRow(col) == -1) continue;

                int row = GetFirstEmptyRow(col);
                var piece = NextMoveByX ? ECellState.X : ECellState.O;
                GameBoard[col, row] = piece;
                NextMoveByX = !NextMoveByX;

                int eval = Minimax(depth - 1, false, alpha, beta);

                NextMoveByX = !NextMoveByX;
                GameBoard[col, row] = ECellState.Empty;

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                    break;
            }

            return maxEval;
        }
        // minimizing from opponent's perspective
        else
        {
            int minEval = int.MaxValue;

            foreach (int col in orderedColumns)
            {
                if (GetFirstEmptyRow(col) == -1) continue;

                int row = GetFirstEmptyRow(col);
                var piece = NextMoveByX ? ECellState.X : ECellState.O;
                GameBoard[col, row] = piece;
                NextMoveByX = !NextMoveByX;

                int eval = Minimax(depth - 1, true, alpha, beta);

                NextMoveByX = !NextMoveByX;
                GameBoard[col, row] = ECellState.Empty;

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }
    
    // reorders columns for more efficient alpha-beta pruning
    private List<int> GetOrderedColumns()
    {
        var columns = new List<int>();
        int center = GameConfiguration.BoardWidth / 2;

        columns.Add(center);

        for (int offset = 1; offset <= GameConfiguration.BoardWidth / 2; offset++)
        {
            if (center + offset < GameConfiguration.BoardWidth)
                columns.Add(center + offset);
            if (center - offset >= 0)
                columns.Add(center - offset);
        }
        return columns;
    }

    // estimates current board position using heuristics
    private int EvaluateBoard()
    {
        int score = 0;
        var currentPiece = NextMoveByX ? ECellState.X : ECellState.O;
        var opponentPiece = NextMoveByX ? ECellState.O : ECellState.X;

        for (int row = 0; row < GameConfiguration.BoardHeight; row++)
        {
            for (int col = 0; col < GameConfiguration.BoardWidth; col++)
            {
                if (GameBoard[col, row] == ECellState.Empty) continue;
                score += EvaluateWindow(col, row, 1, 0, currentPiece); // horizontal
                score += EvaluateWindow(col, row, 0, 1, currentPiece); // vertical
                score += EvaluateWindow(col, row, 1, 1, currentPiece); // diagonal up-right
                score += EvaluateWindow(col, row, 1, -1, currentPiece); // diagonal up-left
            }
        }
        return score;
    }

    // estimates window based on how close it is to a win
    private int EvaluateWindow(int startX, int startY, int dirX, int dirY, ECellState player)
    {
        // counts cells in a window 
        int playerCount = 0;
        int emptyCount = 0;
        int opponentCount = 0;
        var opponent = player == ECellState.X ? ECellState.O : ECellState.X;
        
        for (int i = 0; i < GameConfiguration.WinCondition; i++)
        {
            int x = startX + (dirX * i);
            int y = startY + (dirY * i);

            if (GameConfiguration.IsCylindrical)
            {
                x = (x + GameConfiguration.BoardWidth) % GameConfiguration.BoardWidth;
            }
            else
            {
                if (x < 0 || x >= GameConfiguration.BoardWidth) return 0;
            }

            if (y < 0 || y >= GameConfiguration.BoardHeight) return 0;

            if (GameBoard[x, y] == player)
                playerCount++;
            else if (GameBoard[x, y] == opponent)
                opponentCount++;
            else
                emptyCount++;
        }

        // scores window based on cell combination
        if (playerCount == GameConfiguration.WinCondition)
            return 10000; // our win
        if (playerCount == GameConfiguration.WinCondition - 1 && emptyCount == 1)
            return 100; // possible win
        if (playerCount == GameConfiguration.WinCondition - 2 && emptyCount == 2)
            return 10; // win after two moves
        if (opponentCount == GameConfiguration.WinCondition - 1 && emptyCount == 1)
            return -100; // opponent's possible win
        if (opponentCount == GameConfiguration.WinCondition - 2 && emptyCount == 2)
            return -10; // two moves until opponent's win
        return 0;
    }

    private ECellState CheckBoardForWinner()
    {
        for (int x = 0; x < GameConfiguration.BoardWidth; x++)
        {
            for (int y = 0; y < GameConfiguration.BoardHeight; y++)
            {
                if (GameBoard[x, y] != ECellState.Empty)
                {
                    var winner = GetWinner(x, y);
                    if (winner != ECellState.Empty)
                        return winner;
                }
            }
        }
        return ECellState.Empty;
    }

    public bool IsBoardFullCheck()
    {
        for (int col = 0; col < GameConfiguration.BoardWidth; col++)
        {
            if (GetFirstEmptyRow(col) != -1)
                return false;
        }
        return true;
    }
}