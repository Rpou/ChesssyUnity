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
    public bool IsRook => Name.EndsWith("rook");
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
