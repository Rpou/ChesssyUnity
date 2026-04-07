using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A data-only snapshot of the board that can be copied without changing the live game.
/// </summary>
public class GameState
{
    private readonly PieceState[,] _positions = new PieceState[8, 8];

    public List<PieceState> PlayerWhite { get; }
    public List<PieceState> PlayerBlack { get; }
    public string CurrentPlayer { get; private set; }
    public bool GameOver { get; private set; }
    public Vector2Int? EnPassantTargetSquare { get; private set; }

    /// <summary>
    /// Creates a new game state from the current live game.
    /// </summary>
    /// <param name="game">The live game to copy.</param>
    public GameState(Game game) : this(CreatePieceStates(game.playerWhite), CreatePieceStates(game.playerBlack), game.GetCurrentPlayer(),
            game.IsGameOver(), GetEnPassantTargetSquare(game))
    {
    }

    private GameState(List<PieceState> playerWhite, List<PieceState> playerBlack, string currentPlayer,
        bool gameOver, Vector2Int? enPassantTargetSquare)
    {
        PlayerWhite = playerWhite;
        PlayerBlack = playerBlack;
        CurrentPlayer = currentPlayer;
        GameOver = gameOver;
        EnPassantTargetSquare = enPassantTargetSquare;

        RebuildPositions();
    }

    /// <summary>
    /// Gets all stored pieces for one player, including inactive captured pieces.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>The list of piece states for that player.</returns>
    public List<PieceState> GetPieces(string player)
    {
        return player == "black" ? PlayerBlack : PlayerWhite;
    }

    /// <summary>
    /// Gets only the active pieces for one player.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>The active pieces for that player.</returns>
    public List<PieceState> GetActivePieces(string player)
    {
        var activePieces = new List<PieceState>();

        foreach (var piece in GetPieces(player))
        {
            if (piece.IsActive)
            {
                activePieces.Add(piece);
            }
        }

        return activePieces;
    }

    /// <summary>
    /// Gets the piece on a board square, or null if the square is empty.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The piece on that square, or null.</returns>
    public PieceState GetPosition(int x, int y)
    {
        if (!PositionOnBoard(x, y))
        {
            return null;
        }

        return _positions[x, y];
    }

    /// <summary>
    /// Checks if a square is inside the board.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>True if the square is on the board.</returns>
    public bool PositionOnBoard(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _positions.GetLength(0) && y < _positions.GetLength(1);
    }

    /// <summary>
    /// Gets all possible moves for one player in this state.
    /// This matches the naming in Game.cs and returns legal moves.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>All legal moves for that player.</returns>
    public List<Move> GetPossibleMoves(string player)
    {
        return GetMoves(player, true);
    }

    /// <summary>
    /// Gets all possible moves for the current player in this state.
    /// </summary>
    /// <returns>All legal moves for the current player.</returns>
    public List<Move> GetPossibleMoves()
    {
        return GetPossibleMoves(CurrentPlayer);
    }

    /// <summary>
    /// Gets all raw moves for one player before king-safety filtering.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>All non-filtered moves for that player.</returns>
    public List<Move> GetAllPossibleMoves(string player)
    {
        return GetMoves(player, false);
    }

    /// <summary>
    /// Gets all raw moves for the current player before king-safety filtering.
    /// </summary>
    /// <returns>All non-filtered moves for the current player.</returns>
    public List<Move> GetAllPossibleMoves()
    {
        return GetAllPossibleMoves(CurrentPlayer);
    }

    /// <summary>
    /// Gets all raw moves for one piece in this state.
    /// </summary>
    /// <param name="piece">The piece to inspect.</param>
    /// <returns>The move squares and attack squares for that piece.</returns>
    public (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetPieceMoves(PieceState piece)
    {
        if (piece == null || !piece.IsActive)
        {
            return (new List<Vector2Int>(), new List<Vector2Int>());
        }

        if (piece.IsPawn) return GetPawnMoves(piece);
        if (piece.IsKnight) return GetKnightMoves(piece);
        if (piece.IsBishop) return GetBishopMoves(piece);
        if (piece.IsRook) return GetRookMoves(piece);
        if (piece.IsQueen) return GetQueenMoves(piece);
        return GetKingMoves(piece);
    }

    /// <summary>
    /// Gets the legal moves for one piece after king-safety filtering.
    /// </summary>
    /// <param name="piece">The piece to inspect.</param>
    /// <returns>The legal move squares and legal attack squares for that piece.</returns>
    public (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetAllLegalMoves(PieceState piece)
    {
        var (moveSquares, attackSquares) = GetPieceMoves(piece);
        var legalMoveSquares = new List<Vector2Int>();
        var legalAttackSquares = new List<Vector2Int>();

        foreach (var move in moveSquares)
        {
            if (IsMoveSafe(piece, move.x, move.y))
            {
                legalMoveSquares.Add(move);
            }
        }

        foreach (var attack in attackSquares)
        {
            if (IsMoveSafe(piece, attack.x, attack.y))
            {
                legalAttackSquares.Add(attack);
            }
        }

        return (legalMoveSquares, legalAttackSquares);
    }

    /// <summary>
    /// Checks if a player has at least one legal move in this state.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>True if that player has a legal move.</returns>
    public bool AnyLegalMoves(string player)
    {
        foreach (var piece in GetActivePieces(player))
        {
            var (moveSquares, attackSquares) = GetAllLegalMoves(piece);
            if (moveSquares.Count != 0 || attackSquares.Count != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a new copied state with the move applied.
    /// </summary>
    /// <param name="move">The move to simulate.</param>
    /// <returns>A new state after the move has been made.</returns>
    public GameState ApplyMove(Move move)
    {
        return ApplyMove(move, IsAttackMove(move));
    }

    /// <summary>
    /// Creates a new copied state with the move applied.
    /// </summary>
    /// <param name="move">The move to simulate.</param>
    /// <param name="isAttack">Whether the move captures a piece.</param>
    /// <returns>A new state after the move has been made.</returns>
    public GameState ApplyMove(Move move, bool isAttack)
    {
        var nextState = new GameState(ClonePieceStates(PlayerWhite), ClonePieceStates(PlayerBlack), CurrentPlayer,
            GameOver, EnPassantTargetSquare);

        nextState.ApplyMoveInternal(move, isAttack);
        return nextState;
    }

    /// <summary>
    /// Checks if a move should be treated as an attack in this state.
    /// </summary>
    /// <param name="move">The move to inspect.</param>
    /// <returns>True if the move captures a piece.</returns>
    public bool IsAttackMove(Move move)
    {
        if (GetPosition(move.GetMatrixX(), move.GetMatrixY()) != null)
        {
            return true;
        }

        var movingPiece = GetPosition(move.GetFromMatrixX(), move.GetFromMatrixY());
        if (movingPiece == null || !movingPiece.IsPawn || move.GetMatrixX() == move.GetFromMatrixX())
        {
            return false;
        }

        return EnPassantTargetSquare.HasValue &&
               EnPassantTargetSquare.Value.x == move.GetMatrixX() &&
               EnPassantTargetSquare.Value.y == movingPiece.MatrixY;
    }

    /// <summary>
    /// Checks if the player's king is in check in this state.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>True if the king is in check.</returns>
    public bool IsKingInCheck(string player)
    {
        PieceState king = null;
        foreach (var piece in GetActivePieces(player))
        {
            if (piece.IsKing)
            {
                king = piece;
                break;
            }
        }

        if (king == null)
        {
            return false;
        }

        var opponent = player == "white" ? "black" : "white";
        foreach (var piece in GetActivePieces(opponent))
        {
            if (piece.IsKing)
            {
                continue;
            }

            var (_, attackSquares) = GetPieceMoves(piece);
            foreach (var attack in attackSquares)
            {
                if (attack.x == king.MatrixX && attack.y == king.MatrixY)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Applies the move to this state instance.
    /// </summary>
    /// <param name="move">The move to apply.</param>
    /// <param name="isAttack">Whether the move captures a piece.</param>
    private void ApplyMoveInternal(Move move, bool isAttack)
    {
        var movingPiece = GetPosition(move.GetFromMatrixX(), move.GetFromMatrixY());
        if (movingPiece == null)
        {
            throw new InvalidOperationException("No piece found on the move's starting square.");
        }

        var beforeMoveX = movingPiece.MatrixX;
        var beforeMoveY = movingPiece.MatrixY;

        if (isAttack)
        {
            HandleAttack(movingPiece, move.GetMatrixX(), move.GetMatrixY());
        }

        _positions[beforeMoveX, beforeMoveY] = null;

        movingPiece.MatrixX = move.GetMatrixX();
        movingPiece.MatrixY = move.GetMatrixY();
        _positions[movingPiece.MatrixX, movingPiece.MatrixY] = movingPiece;

        if (movingPiece.IsKing)
        {
            movingPiece.HasMoved = true;
            if (Math.Abs(beforeMoveX - move.GetMatrixX()) == 2)
            {
                MoveRookAfterCastlingMove(beforeMoveX, beforeMoveY, move.GetMatrixX());
            }
        }

        if (movingPiece.IsRook)
        {
            movingPiece.HasMoved = true;
        }

        if (movingPiece.IsPawn && Math.Abs(beforeMoveY - move.GetMatrixY()) == 2)
        {
            EnPassantTargetSquare = new Vector2Int(movingPiece.MatrixX, movingPiece.MatrixY);
        }
        else
        {
            EnPassantTargetSquare = null;
        }

        if (movingPiece.IsPawn && (movingPiece.MatrixY == 0 || movingPiece.MatrixY == 7))
        {
            movingPiece.PromoteToQueen();
        }

        CurrentPlayer = CurrentPlayer == "white" ? "black" : "white";
    }

    /// <summary>
    /// Removes the captured piece for an attack, including en passant.
    /// </summary>
    /// <param name="movingPiece">The piece that is moving.</param>
    /// <param name="matrixX">The target x coordinate.</param>
    /// <param name="matrixY">The target y coordinate.</param>
    private void HandleAttack(PieceState movingPiece, int matrixX, int matrixY)
    {
        var capturedPiece = GetPosition(matrixX, matrixY);
        if (capturedPiece != null)
        {
            CapturePiece(capturedPiece);
            return;
        }

        if (!movingPiece.IsPawn || !EnPassantTargetSquare.HasValue)
        {
            return;
        }

        var enPassantTarget = EnPassantTargetSquare.Value;
        var direction = movingPiece.Player == "white" ? 1 : -1;
        var isDiagonalPawnMove = matrixX != movingPiece.MatrixX &&
                                 matrixY - movingPiece.MatrixY == direction;

        if (!isDiagonalPawnMove || enPassantTarget.x != matrixX || enPassantTarget.y != movingPiece.MatrixY)
        {
            return;
        }

        var enPassantPawn = GetPosition(enPassantTarget.x, enPassantTarget.y);
        if (enPassantPawn != null && enPassantPawn.IsPawn && enPassantPawn.Player != movingPiece.Player)
        {
            CapturePiece(enPassantPawn);
        }
    }

    /// <summary>
    /// Marks a piece as captured and removes it from the board.
    /// </summary>
    /// <param name="piece">The piece to capture.</param>
    private void CapturePiece(PieceState piece)
    {
        _positions[piece.MatrixX, piece.MatrixY] = null;
        piece.IsActive = false;
    }

    /// <summary>
    /// Moves the rook after a castling move.
    /// </summary>
    /// <param name="kingFromX">The king's starting x coordinate.</param>
    /// <param name="kingY">The king's row.</param>
    /// <param name="kingToX">The king's target x coordinate.</param>
    private void MoveRookAfterCastlingMove(int kingFromX, int kingY, int kingToX)
    {
        var isRightRook = kingToX > kingFromX;
        var rookFromX = isRightRook ? kingFromX + 3 : kingFromX - 4;
        var rookToX = isRightRook ? kingToX - 1 : kingToX + 1;
        var rook = GetPosition(rookFromX, kingY);

        if (rook == null)
        {
            return;
        }

        _positions[rookFromX, kingY] = null;
        rook.MatrixX = rookToX;
        rook.MatrixY = kingY;
        rook.HasMoved = true;
        _positions[rook.MatrixX, rook.MatrixY] = rook;
    }

    /// <summary>
    /// Rebuilds the board array from the stored piece lists.
    /// </summary>
    private void RebuildPositions()
    {
        foreach (var piece in PlayerWhite)
        {
            AddPieceToBoard(piece);
        }

        foreach (var piece in PlayerBlack)
        {
            AddPieceToBoard(piece);
        }
    }

    /// <summary>
    /// Places one active piece on the board array.
    /// </summary>
    /// <param name="piece">The piece to place.</param>
    private void AddPieceToBoard(PieceState piece)
    {
        if (!piece.IsActive)
        {
            return;
        }

        _positions[piece.MatrixX, piece.MatrixY] = piece;
    }

    /// <summary>
    /// Creates piece states from the live Unity pieces.
    /// </summary>
    /// <param name="pieces">The live pieces to copy.</param>
    /// <returns>The copied piece states.</returns>
    private static List<PieceState> CreatePieceStates(IEnumerable<GameObject> pieces)
    {
        var pieceStates = new List<PieceState>();

        foreach (var gameObject in pieces)
        {
            if (gameObject == null)
            {
                continue;
            }

            var piece = gameObject.GetComponent<Piece>();
            if (piece == null)
            {
                continue;
            }

            pieceStates.Add(PieceState.FromPiece(piece, gameObject.activeSelf));
        }

        return pieceStates;
    }

    /// <summary>
    /// Creates deep copies of stored piece states.
    /// </summary>
    /// <param name="pieces">The piece states to clone.</param>
    /// <returns>A new list with cloned piece states.</returns>
    private static List<PieceState> ClonePieceStates(IEnumerable<PieceState> pieces)
    {
        var clones = new List<PieceState>();

        foreach (var piece in pieces)
        {
            clones.Add(piece.Clone());
        }

        return clones;
    }

    /// <summary>
    /// Gets the en passant target square from the live game.
    /// </summary>
    /// <param name="game">The live game to inspect.</param>
    /// <returns>The en passant target square, or null.</returns>
    private static Vector2Int? GetEnPassantTargetSquare(Game game)
    {
        var enPassantTarget = game.GetEnPassentTarget();
        if (enPassantTarget == null)
        {
            return null;
        }

        return new Vector2Int(enPassantTarget.GetxBoard(), enPassantTarget.GetyBoard());
    }

    private List<Move> GetMoves(string player, bool legalOnly)
    {
        var possibleMoves = new List<Move>();

        foreach (var piece in GetActivePieces(player))
        {
            (List<Vector2Int> pieceMoves, List<Vector2Int> pieceAttacks) = legalOnly
                ? GetAllLegalMoves(piece)
                : GetPieceMoves(piece);

            foreach (var move in pieceMoves)
            {
                possibleMoves.Add(new Move(piece.MatrixX, piece.MatrixY, move.x, move.y));
            }

            foreach (var move in pieceAttacks)
            {
                possibleMoves.Add(new Move(piece.MatrixX, piece.MatrixY, move.x, move.y, true));
            }
        }

        return possibleMoves;
    }

    private bool IsMoveSafe(PieceState piece, int x, int y)
    {
        var move = new Move(piece.MatrixX, piece.MatrixY, x, y, IsAttackMove(piece, x, y));
        var nextState = ApplyMove(move, move.GetIsAttack());
        return !nextState.IsKingInCheck(piece.Player);
    }

    private bool IsAttackMove(PieceState piece, int x, int y)
    {
        if (GetPosition(x, y) != null)
        {
            return true;
        }

        if (!piece.IsPawn || x == piece.MatrixX)
        {
            return false;
        }

        return EnPassantTargetSquare.HasValue &&
               EnPassantTargetSquare.Value.x == x &&
               EnPassantTargetSquare.Value.y == piece.MatrixY;
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) CanSeeInLine(int xIncrement,
        int yIncrement, PieceState piece)
    {
        var movableSquares = new List<Vector2Int>();
        var attackableSquares = new List<Vector2Int>();

        int x = piece.MatrixX + xIncrement;
        int y = piece.MatrixY + yIncrement;

        while (PositionOnBoard(x, y) && GetPosition(x, y) == null)
        {
            movableSquares.Add(new Vector2Int(x, y));
            x += xIncrement;
            y += yIncrement;
        }

        if (PositionOnBoard(x, y))
        {
            var pieceOnSquare = GetPosition(x, y);
            if (pieceOnSquare != null && pieceOnSquare.Player != piece.Player)
            {
                attackableSquares.Add(new Vector2Int(x, y));
            }
        }

        return (movableSquares, attackableSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) CanSeePoint(int x, int y,
        PieceState piece)
    {
        var movableSquares = new List<Vector2Int>();
        var attackableSquares = new List<Vector2Int>();

        if (!PositionOnBoard(x, y))
        {
            return (movableSquares, attackableSquares);
        }

        var pieceOnSquare = GetPosition(x, y);
        if (pieceOnSquare == null)
        {
            movableSquares.Add(new Vector2Int(x, y));
        }
        else if (pieceOnSquare.Player != piece.Player)
        {
            attackableSquares.Add(new Vector2Int(x, y));
        }

        return (movableSquares, attackableSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetKingMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();
        int x = piece.MatrixX;
        int y = piece.MatrixY;

        int[][] directions =
        {
            new[] { 0, 1 }, new[] { 0, -1 }, new[] { -1, -1 }, new[] { -1, 0 },
            new[] { -1, 1 }, new[] { 1, -1 }, new[] { 1, 0 }, new[] { 1, 1 }
        };

        if (!piece.HasMoved)
        {
            PieceState rookLeft;
            PieceState rookRight;
            if (piece.Player == "white")
            {
                rookLeft = GetPosition(0, 0);
                rookRight = GetPosition(7, 0);
            }
            else
            {
                rookLeft = GetPosition(0, 7);
                rookRight = GetPosition(7, 7);
            }

            var kingStartsInCheck = IsKingInCheck(piece.Player);

            if (rookRight != null && rookRight.IsRook && !rookRight.HasMoved &&
                GetPosition(x + 1, y) == null && GetPosition(x + 2, y) == null &&
                !kingStartsInCheck &&
                IsMoveSafe(piece, x + 1, y) &&
                IsMoveSafe(piece, x + 2, y))
            {
                moveSquares.Add(new Vector2Int(x + 2, y));
            }

            if (rookLeft != null && rookLeft.IsRook && !rookLeft.HasMoved &&
                GetPosition(x - 1, y) == null && GetPosition(x - 2, y) == null && GetPosition(x - 3, y) == null &&
                !kingStartsInCheck &&
                IsMoveSafe(piece, x - 1, y) &&
                IsMoveSafe(piece, x - 2, y))
            {
                moveSquares.Add(new Vector2Int(x - 2, y));
            }
        }

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeePoint(x + dir[0], y + dir[1], piece);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetQueenMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions =
        {
            new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { -1, 0 },
            new[] { 0, -1 }, new[] { -1, -1 }, new[] { -1, 1 }, new[] { 1, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetRookMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions =
        {
            new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetBishopMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions =
        {
            new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetKnightMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions =
        {
            new[] { 1, 2 }, new[] { -1, 2 }, new[] { 1, -2 }, new[] { -1, -2 },
            new[] { 2, 1 }, new[] { -2, 1 }, new[] { 2, -1 }, new[] { -2, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeePoint(piece.MatrixX + dir[0], piece.MatrixY + dir[1], piece);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetPawnMoves(PieceState piece)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int x = piece.MatrixX;
        int y = piece.MatrixY;
        int direction = piece.Player == "white" ? 1 : -1;
        int startRow = piece.Player == "white" ? 1 : 6;

        if (PositionOnBoard(x, y + direction) && GetPosition(x, y + direction) == null)
        {
            moveSquares.Add(new Vector2Int(x, y + direction));

            if (y == startRow && GetPosition(x, y + (2 * direction)) == null)
            {
                moveSquares.Add(new Vector2Int(x, y + (2 * direction)));
            }
        }

        int attackY = y + direction;

        if (PositionOnBoard(x + 1, attackY) && GetPosition(x + 1, attackY) != null &&
            GetPosition(x + 1, attackY).Player != piece.Player)
        {
            attackSquares.Add(new Vector2Int(x + 1, attackY));
        }

        if (PositionOnBoard(x - 1, attackY) && GetPosition(x - 1, attackY) != null &&
            GetPosition(x - 1, attackY).Player != piece.Player)
        {
            attackSquares.Add(new Vector2Int(x - 1, attackY));
        }

        if (!EnPassantTargetSquare.HasValue || EnPassantTargetSquare.Value.y != y)
        {
            return (moveSquares, attackSquares);
        }

        var enPassantTarget = GetPosition(EnPassantTargetSquare.Value.x, EnPassantTargetSquare.Value.y);
        if (enPassantTarget == null || !enPassantTarget.IsPawn || enPassantTarget.Player == piece.Player)
        {
            return (moveSquares, attackSquares);
        }

        if (enPassantTarget.MatrixX == x + 1 || enPassantTarget.MatrixX == x - 1)
        {
            var enPassantAttackSquare = enPassantTarget.MatrixX > x
                ? new Vector2Int(x + 1, attackY)
                : new Vector2Int(x - 1, attackY);

            if (PositionOnBoard(enPassantAttackSquare.x, enPassantAttackSquare.y))
            {
                attackSquares.Add(enPassantAttackSquare);
            }
        }

        return (moveSquares, attackSquares);
    }
}

public class PieceState
{
    public string Name { get; private set; }
    public string Player { get; }
    public int MatrixX { get; set; }
    public int MatrixY { get; set; }
    public bool IsActive { get; set; }
    public bool HasMoved { get; set; }
    public bool IsInCheck { get; set; }

    public bool IsPawn => Name.EndsWith("pawn");
    public bool IsKnight => Name.EndsWith("knight");
    public bool IsBishop => Name.EndsWith("bishop");
    public bool IsRook => Name.EndsWith("rook");
    public bool IsQueen => Name.EndsWith("queen");
    public bool IsKing => Name.EndsWith("king");

    private PieceState(string name, string player, int matrixX, int matrixY, bool isActive, bool hasMoved,
        bool isInCheck)
    {
        Name = name;
        Player = player;
        MatrixX = matrixX;
        MatrixY = matrixY;
        IsActive = isActive;
        HasMoved = hasMoved;
        IsInCheck = isInCheck;
    }

    public static PieceState FromPiece(Piece piece, bool isActive)
    {
        return new PieceState(piece.name, piece.GetPlayer(), piece.GetxBoard(), piece.GetyBoard(), isActive,
            GetHasMoved(piece), GetIsInCheck(piece));
    }

    public PieceState Clone()
    {
        return new PieceState(Name, Player, MatrixX, MatrixY, IsActive, HasMoved, IsInCheck);
    }

    public int GetWorth()
    {
        if (IsPawn) return 1;
        if (IsKnight) return 3;
        if (IsBishop) return 3;
        if (IsRook) return 5;
        if (IsQueen) return 9;
        if (IsKing) return 100;
        return 0;
    }

    public void PromoteToQueen()
    {
        Name = Player + "_queen";
    }

    private static bool GetHasMoved(Piece piece)
    {
        if (piece is King king)
        {
            return king.GetHasMoved();
        }

        if (piece is Rook rook)
        {
            return rook.HasMoved();
        }

        return false;
    }

    private static bool GetIsInCheck(Piece piece)
    {
        if (piece is King king)
        {
            return king.GetInCheck();
        }

        return false;
    }
}
