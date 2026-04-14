using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// A data-only snapshot of the board that can be copied without changing the live game.
/// </summary>
public class GameState
{
    private static readonly Vector2Int[] OrthogonalDirections =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private static readonly Vector2Int[] KnightOffsets =
    {
        new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(1, -2),
        new Vector2Int(-1, -2), new Vector2Int(-2, -1), new Vector2Int(-2, 1), new Vector2Int(-1, 2)
    };

    private static readonly Vector2Int[] KingOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

    private readonly PieceState[,] _positions = new PieceState[8, 8];
    private PieceState _whiteKing;
    private PieceState _blackKing;
    private bool _disableMoveCache;
    private int? _whitePossibleMoveCount;
    private int? _blackPossibleMoveCount;

    private sealed class MoveUndoState
    {
        public PieceState MovingPiece { get; set; }
        public int MovingFromX { get; set; }
        public int MovingFromY { get; set; }
        public bool MovingHadMoved { get; set; }
        public string MovingOriginalName { get; set; }
        public PieceState CapturedPiece { get; set; }
        public int CapturedX { get; set; }
        public int CapturedY { get; set; }
        public bool CapturedWasActive { get; set; }
        public PieceState CastlingRook { get; set; }
        public int CastlingRookFromX { get; set; }
        public int CastlingRookFromY { get; set; }
        public bool CastlingRookHadMoved { get; set; }
        public Vector2Int? PreviousEnPassantTargetSquare { get; set; }
        public string PreviousCurrentPlayer { get; set; }
        public bool PreviousDisableMoveCache { get; set; }
    }

    public List<PieceState> PlayerWhite { get; }
    public List<PieceState> PlayerBlack { get; }
    public string CurrentPlayer { get; private set; }
    public bool GameOver { get; private set; }
    public Vector2Int? EnPassantTargetSquare { get; private set; }
    public int amountMoves { get; private set; }

    private readonly Dictionary<PieceState, (List<Vector2Int> moves, List<Vector2Int> attacks)> moveCache
    = new();


    /// <summary>
    /// Creates a new game state from the current live game.
    /// </summary>
    /// <param name="game">The live game to copy.</param>
    /// <remarks>
    /// Runtime: O(p), where p is the number of stored pieces. In a normal chess game this copies up to 32 live pieces
    /// and then places up to 32 piece states back onto the board array.
    /// </remarks>
    public GameState(Game game) : this(CreatePieceStates(game.playerWhite), CreatePieceStates(game.playerBlack), game.GetCurrentPlayer(),
            game.IsGameOver(), GetEnPassantTargetSquare(game))
    {
    }

    /// <summary>
    /// Creates a new game state from already copied piece lists.
    /// </summary>
    /// <param name="playerWhite">The stored white pieces.</param>
    /// <param name="playerBlack">The stored black pieces.</param>
    /// <param name="currentPlayer">The player whose turn it is.</param>
    /// <param name="gameOver">Whether the game is over.</param>
    /// <param name="enPassantTargetSquare">The current en passant target, if any.</param>
    /// <remarks>
    /// Runtime: O(p), where p is the number of stored pieces. RebuildPositions visits at most 32 piece states.
    /// </remarks>
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
    /// Creates a copied game state from an existing state.
    /// </summary>
    /// <param name="other">The state to copy.</param>
    /// <remarks>
    /// Runtime: O(p), where p is the number of stored pieces. This clones each stored piece once and fills the board array in
    /// the same pass, so it avoids the extra rebuild pass used by the other constructor.
    /// </remarks>
    private GameState(GameState other)
    {
        PlayerWhite = new List<PieceState>(other.PlayerWhite.Count);
        PlayerBlack = new List<PieceState>(other.PlayerBlack.Count);
        CurrentPlayer = other.CurrentPlayer;
        GameOver = other.GameOver;
        EnPassantTargetSquare = other.EnPassantTargetSquare;

        ClonePiecesIntoState(other.PlayerWhite, PlayerWhite);
        ClonePiecesIntoState(other.PlayerBlack, PlayerBlack);
    }

    /// <summary>
    /// Gets all stored pieces for one player, including inactive captured pieces.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>The list of piece states for that player.</returns>
    /// <remarks>
    /// Runtime: O(1). This only returns one of the two stored lists.
    /// </remarks>
    public List<PieceState> GetPieces(string player)
    {
        return player == "black" ? PlayerBlack : PlayerWhite;
    }

    /// <summary>
    /// Gets only the active pieces for one player.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>The active pieces for that player.</returns>
    /// <remarks>
    /// Runtime: O(p), where p is the number of pieces for that player. In chess this loops through at most 16 pieces.
    /// </remarks>
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

    public void UpdateMoves()
    {
        moveCache.Clear();

        foreach (var pieceST in PlayerWhite)
        {
            if (!pieceST.IsActive) continue;
            GetPieceMoves(pieceST);
        }

        foreach (var pieceST in PlayerBlack)
        {
            if (!pieceST.IsActive) continue;
            GetPieceMoves(pieceST);
        }
    }

    public List<Move> SortAttack(List<Move> moves)
    {
        var sorted = new List<Move>(moves.Count);

        foreach (var move in moves)
        {
            if (move.IsAttack) sorted.Add(move);
        }

        foreach (var move in moves)
        {
            if (!move.IsAttack) sorted.Add(move);
        }

        return sorted;
    }


    /// <summary>
    /// Gets the piece on a board square, or null if the square is empty.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The piece on that square, or null.</returns>
    /// <remarks>
    /// Runtime: O(1). This does one bounds check and at most one array lookup.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(1). This is just a few integer comparisons.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(p * c * safetyCheck) in the current implementation because each candidate move is simulated before it is
    /// returned as legal. In practice this loops through at most 16 active pieces, and each piece can test up to about 27
    /// candidate targets before filtering.
    /// </remarks>
    public List<Move> GetPossibleMoves(string player)
    {
        var moves = GetMoves(player, true);

        if (!_disableMoveCache)
        {
            CachePossibleMoveCount(player, moves.Count);
        }

        return moves;
    }

    public int GetPossibleMoveCount(string player)
    {
        if (_disableMoveCache)
        {
            return GetMoves(player, true).Count;
        }

        int? cachedCount = player == "white" ? _whitePossibleMoveCount : _blackPossibleMoveCount;
        if (cachedCount.HasValue)
        {
            return cachedCount.Value;
        }

        int moveCount = GetMoves(player, true).Count;
        CachePossibleMoveCount(player, moveCount);
        return moveCount;
    }

    /// <summary>
    /// Gets all possible moves for the current player in this state.
    /// </summary>
    /// <returns>All legal moves for the current player.</returns>
    /// <remarks>
    /// Runtime: Same as GetPossibleMoves(string). It still loops through at most 16 active pieces for the current player.
    /// </remarks>
    public List<Move> GetPossibleMoves()
    {
        var moves = GetPossibleMoves(CurrentPlayer);
        amountMoves = moves.Count;
        return moves;
    }

    public int GetPossibleMoveCount()
    {
        int moveCount = GetPossibleMoveCount(CurrentPlayer);
        amountMoves = moveCount;
        return moveCount;
    }

    /// <summary>
    /// Gets all raw moves for one player before king-safety filtering.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>All non-filtered moves for that player.</returns>
    /// <remarks>
    /// Runtime: O(p * c), where p is the number of active pieces and c is the number of raw targets per piece. In the loose
    /// chess worst case this loops through at most 16 active pieces and collects up to about 27 targets per piece. 432
    /// </remarks>
    public List<Move> GetAllPossibleMoves(string player)
    {
        return GetMoves(player, false);
    }

    /// <summary>
    /// Gets all raw moves for the current player before king-safety filtering.
    /// </summary>
    /// <returns>All non-filtered moves for the current player.</returns>
    /// <remarks>
    /// Runtime: Same as GetAllPossibleMoves(string). It still loops through at most 16 active pieces.
    /// </remarks>
    public List<Move> GetAllPossibleMoves()
    {
        return GetAllPossibleMoves(CurrentPlayer);
    }

    /// <summary>
    /// Gets all raw moves for one piece in this state.
    /// </summary>
    /// <param name="piece">The piece to inspect.</param>
    /// <returns>The move squares and attack squares for that piece.</returns>
    /// <remarks>
    /// Runtime: O(n) in the heaviest sliding-piece case, where n is the board width. The slowest branch is queen-style
    /// scanning, which checks up to 8 directions and at most 7 squares in each direction, so up to 56 ray steps on an 8x8
    /// board.
    /// </remarks>
    public (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetPieceMoves(PieceState piece)
    {
        if (piece == null || !piece.IsActive)
        {
            return (new List<Vector2Int>(), new List<Vector2Int>());
        }

        if (!_disableMoveCache && moveCache.TryGetValue(piece, out var cachedMoves))
        {
            return cachedMoves;
        }

        var calculatedMoves = CalculatePieceMoves(piece);

        if (!_disableMoveCache)
        {
            moveCache[piece] = calculatedMoves;
        }

        return calculatedMoves;
    }

    /// <summary>
    /// Gets the legal moves for one piece after king-safety filtering.
    /// </summary>
    /// <param name="piece">The piece to inspect.</param>
    /// <returns>The legal move squares and legal attack squares for that piece.</returns>
    /// <remarks>
    /// Runtime: O(c * safetyCheck), where c is the number of raw targets for the piece. In the heaviest case a queen can
    /// produce up to 27 candidate squares, and each one is tested with IsMoveSafe.
    /// </remarks>
    public (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetAllLegalMoves(PieceState piece)
    {
        var (moveSquares, attackSquares) = GetPieceMoves(piece);
        var legalMoveSquares = new List<Vector2Int>();
        var legalAttackSquares = new List<Vector2Int>();

        foreach (var attack in attackSquares)
        {
            if (IsMoveSafe(piece, attack.x, attack.y))
            {
                legalAttackSquares.Add(attack);
            }
        }

        foreach (var move in moveSquares)
        {
            if (IsMoveSafe(piece, move.x, move.y))
            {
                legalMoveSquares.Add(move);
            }
        }

        return (legalMoveSquares, legalAttackSquares);
    }

    /// <summary>
    /// Checks if a player has at least one legal move in this state.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <returns>True if that player has a legal move.</returns>
    /// <remarks>
    /// Runtime: O(p * legalMoveCost). In the worst case this checks all 16 active pieces before finding a legal move or
    /// deciding there are none.
    /// </remarks>
    public bool AnyLegalMoves(string player)
    {
        foreach (var piece in GetPieces(player))
        {
            if(!piece.IsActive) continue;
             
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
    /// <remarks>
    /// Runtime: O(p). It first checks whether the move is an attack, then deep-copies the state. In a normal chess game
    /// that means up to 32 piece clones and up to 32 board placements.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(p), where p is the number of stored pieces. It deep-copies up to 32 piece states and fills the board array
    /// during the same copy pass, then applies the move in constant time.
    /// </remarks>
    public GameState ApplyMove(Move move, bool isAttack)
    {
        var nextState = new GameState(this);

        nextState.ApplyMoveInternal(move, isAttack);
        return nextState;
    }

    /// <summary>
    /// Checks if a move should be treated as an attack in this state.
    /// </summary>
    /// <param name="move">The move to inspect.</param>
    /// <returns>True if the move captures a piece.</returns>
    /// <remarks>
    /// Runtime: O(1). This does at most two board lookups and a small en passant comparison.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this checks up to 2 pawn squares, 8 knight squares, 8 king
    /// squares, and at most 56 sliding ray squares, for up to 74 board checks in the worst case.
    /// </remarks>
    public bool IsKingInCheck(string player)
    {
        PieceState king = player == "white" ? _whiteKing : _blackKing;
        if (king == null)
        {
            return false;
        }

        if (!king.IsActive)
        {
            return false;
        }

        var opponent = player == "white" ? "black" : "white";

        return IsSquareAttacked(king.MatrixX, king.MatrixY, opponent);
    }

    /// <summary>
    /// Checks whether one square is attacked by the given player.
    /// </summary>
    /// <param name="x">The square x coordinate.</param>
    /// <param name="y">The square y coordinate.</param>
    /// <param name="attackingPlayer">The player whose attacks should be checked.</param>
    /// <returns>True if one of that player's pieces attacks the square.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this checks up to 2 pawn squares, 8 knight squares, 8 king
    /// squares, and at most 56 sliding ray squares, for up to 74 board checks in the worst case.
    /// </remarks>
    public bool IsSquareAttacked(int x, int y, string attackingPlayer)
    {
        if (!PositionOnBoard(x, y))
        {
            return false;
        }

        int pawnSourceY = attackingPlayer == "white" ? y - 1 : y + 1;

        PieceState pawnRight = GetPosition(x + 1, pawnSourceY);
        if (pawnRight != null && pawnRight.IsActive && pawnRight.Player == attackingPlayer && pawnRight.IsPawn)
        {
            return true;
        }

        PieceState pawnLeft = GetPosition(x - 1, pawnSourceY);
        if (pawnLeft != null && pawnLeft.IsActive && pawnLeft.Player == attackingPlayer && pawnLeft.IsPawn)
        {
            return true;
        }

        foreach (var offset in KnightOffsets)
        {
            PieceState knight = GetPosition(x + offset.x, y + offset.y);
            if (knight != null && knight.IsActive && knight.Player == attackingPlayer && knight.IsKnight)
            {
                return true;
            }
        }

        foreach (var offset in KingOffsets)
        {
            PieceState king = GetPosition(x + offset.x, y + offset.y);
            if (king != null && king.IsActive && king.Player == attackingPlayer && king.IsKing)
            {
                return true;
            }
        }

        return IsSlidingAttack(x, y, attackingPlayer, OrthogonalDirections, true) ||
               IsSlidingAttack(x, y, attackingPlayer, DiagonalDirections, false);
    }

    /// <summary>
    /// Checks whether a sliding attacker can see the target square along a set of directions.
    /// </summary>
    /// <param name="targetX">The attacked square x coordinate.</param>
    /// <param name="targetY">The attacked square y coordinate.</param>
    /// <param name="attackingPlayer">The player whose sliding pieces should be checked.</param>
    /// <param name="directions">The directions to scan.</param>
    /// <param name="rookLikeAttack">True for rook-or-queen lines, false for bishop-or-queen lines.</param>
    /// <returns>True if a matching sliding piece attacks the square.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board each call scans 4 rays and walks at most 28 squares before
    /// it either finds a blocker or leaves the board.
    /// </remarks>
    private bool IsSlidingAttack(int targetX, int targetY, string attackingPlayer, Vector2Int[] directions,
        bool rookLikeAttack)
    {
        foreach (var direction in directions)
        {
            int x = targetX + direction.x;
            int y = targetY + direction.y;

            while (PositionOnBoard(x, y))
            {
                PieceState piece = GetPosition(x, y);
                if (piece == null)
                {
                    x += direction.x;
                    y += direction.y;
                    continue;
                }

                if (!piece.IsActive || piece.Player != attackingPlayer)
                {
                    break;
                }

                if (rookLikeAttack)
                {
                    return piece.IsRook || piece.IsQueen;
                }

                return piece.IsBishop || piece.IsQueen;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies the move to this state instance.
    /// </summary>
    /// <param name="move">The move to apply.</param>
    /// <param name="isAttack">Whether the move captures a piece.</param>
    /// <remarks>
    /// Runtime: O(1). This updates a few board squares and runs only constant-size castling, en passant, and promotion checks.
    /// </remarks>
    private void ApplyMoveInternal(Move move, bool isAttack)
    {
        moveCache.Clear();
        InvalidatePossibleMoveCountCache();

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
    /// <remarks>
    /// Runtime: O(1). This checks the target square once and, for en passant, at most one extra pawn square.
    /// </remarks>
    private void HandleAttack(PieceState movingPiece, int matrixX, int matrixY)
    {
        var capturedPiece = GetCapturedPieceForMove(movingPiece, matrixX, matrixY);
        if (capturedPiece != null)
        {
            CapturePiece(capturedPiece);
        }
    }

    /// <summary>
    /// Marks a piece as captured and removes it from the board.
    /// </summary>
    /// <param name="piece">The piece to capture.</param>
    /// <remarks>
    /// Runtime: O(1). One board removal and one flag update.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(1). This does one rook lookup and one rook reposition.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(p), where p is the number of stored pieces. In this project it visits at most 32 piece states.
    /// </remarks>
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
    /// <remarks>
    /// Runtime: O(1). This only checks whether the piece is active and then assigns one array slot.
    /// </remarks>
    private void AddPieceToBoard(PieceState piece)
    {
        CacheKing(piece);

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
    /// <remarks>
    /// Runtime: O(p), where p is the number of provided GameObjects. When called from GameState(Game), this loops through at
    /// most 16 pieces for one side.
    /// </remarks>
    private static List<PieceState> CreatePieceStates(IEnumerable<GameObject> pieces)
    {
        var pieceStates = new List<PieceState>(16);

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
    /// Gets the en passant target square from the live game.
    /// </summary>
    /// <param name="game">The live game to inspect.</param>
    /// <returns>The en passant target square, or null.</returns>
    /// <remarks>
    /// Runtime: O(1). This does one getter call and one null check.
    /// </remarks>
    private static Vector2Int? GetEnPassantTargetSquare(Game game)
    {
        var enPassantTarget = game.GetEnPassentTarget();
        if (enPassantTarget == null)
        {
            return null;
        }

        return new Vector2Int(enPassantTarget.GetxBoard(), enPassantTarget.GetyBoard());
    }

    /// <summary>
    /// Clones a piece list into this state and fills the board array during the same pass.
    /// </summary>
    /// <param name="source">The source pieces to copy.</param>
    /// <param name="destination">The destination list.</param>
    /// <remarks>
    /// Runtime: O(p), where p is the number of source pieces. In this project that is at most 16 pieces per side.
    /// </remarks>
    private void ClonePiecesIntoState(IEnumerable<PieceState> source, List<PieceState> destination)
    {
        foreach (var piece in source)
        {
            var clone = piece.Clone();
            destination.Add(clone);
            AddPieceToBoard(clone);
        }
    }

    /// <summary>
    /// Caches the king reference for faster check detection.
    /// </summary>
    /// <param name="piece">The piece to inspect.</param>
    /// <remarks>
    /// Runtime: O(1). This does one piece-type check and stores one reference when the piece is a king.
    /// </remarks>
    private void CacheKing(PieceState piece)
    {
        if (!piece.IsKing)
        {
            return;
        }

        if (piece.Player == "white")
        {
            _whiteKing = piece;
            return;
        }

        _blackKing = piece;
    }

    private (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) CalculatePieceMoves(PieceState piece)
    {
        if (piece.IsPawn) return GetPawnMoves(piece);
        if (piece.IsKnight) return GetKnightMoves(piece);
        if (piece.IsBishop) return GetBishopMoves(piece);
        if (piece.IsRook) return GetRookMoves(piece);
        if (piece.IsQueen) return GetQueenMoves(piece);
        return GetKingMoves(piece);
    }

    /// <summary>
    /// Gets either raw moves or legal moves for one player.
    /// </summary>
    /// <param name="player">The player color.</param>
    /// <param name="legalOnly">True to filter out moves that leave the king in check.</param>
    /// <returns>The collected moves for that player.</returns>
    /// <remarks>
    /// Runtime: O(p * c) for raw moves and O(p * c * safetyCheck) for legal moves. In the loose chess worst case this loops
    /// through at most 16 active pieces and can inspect up to about 27 candidate targets per piece. 432
    /// </remarks>
    private List<Move> GetMoves(string player, bool legalOnly)
    {
        var possibleMoves = new List<Move>();
        var possibleAttackMoves = new List<Move>();

        foreach (var piece in GetPieces(player))
        {
            if(!piece.IsActive) continue;

            (List<Vector2Int> pieceMoves, List<Vector2Int> pieceAttacks) = legalOnly
                ? GetAllLegalMoves(piece)
                : GetPieceMoves(piece);

            foreach (var move in pieceAttacks)
            {
                possibleAttackMoves.Add(new Move(piece.MatrixX, piece.MatrixY, move.x, move.y, true));
            }

            foreach (var move in pieceMoves)
            {
                possibleMoves.Add(new Move(piece.MatrixX, piece.MatrixY, move.x, move.y));
            }
        }
        possibleAttackMoves.AddRange(possibleMoves);
        return possibleAttackMoves;
    }

    private void CachePossibleMoveCount(string player, int moveCount)
    {
        if (player == "white")
        {
            _whitePossibleMoveCount = moveCount;
            return;
        }

        _blackPossibleMoveCount = moveCount;
    }

    private void InvalidatePossibleMoveCountCache()
    {
        _whitePossibleMoveCount = null;
        _blackPossibleMoveCount = null;
        amountMoves = 0;
    }

    /// <summary>
    /// Checks whether moving one piece to a target square keeps that player's king safe.
    /// </summary>
    /// <param name="piece">The piece to test.</param>
    /// <param name="x">The target x coordinate.</param>
    /// <param name="y">The target y coordinate.</param>
    /// <returns>True if the move does not leave the king in check.</returns>
    /// <remarks>
    /// Runtime: O(checkCost). This applies the move in place, runs one IsKingInCheck call, and then undoes the move again, so
    /// it avoids cloning the whole GameState for each candidate move.
    /// </remarks>
    private bool IsMoveSafe(PieceState piece, int x, int y)
    {
        var move = new Move(piece.MatrixX, piece.MatrixY, x, y, IsAttackMove(piece, x, y));
        var undoState = ApplyTemporaryMove(move, move.GetIsAttack());
        bool isSafe = !IsKingInCheck(piece.Player);
        UndoTemporaryMove(undoState);
        return isSafe;
    }

    /// <summary>
    /// Checks whether a piece-state move should be treated as an attack.
    /// </summary>
    /// <param name="piece">The moving piece.</param>
    /// <param name="x">The target x coordinate.</param>
    /// <param name="y">The target y coordinate.</param>
    /// <returns>True if the move captures a piece.</returns>
    /// <remarks>
    /// Runtime: O(1). This does at most one board lookup and the constant-size en passant comparison.
    /// </remarks>
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

    /// <summary>
    /// Gets the piece that would be captured by a move, including en passant.
    /// </summary>
    /// <param name="movingPiece">The piece that is moving.</param>
    /// <param name="matrixX">The target x coordinate.</param>
    /// <param name="matrixY">The target y coordinate.</param>
    /// <returns>The captured piece, or null if the move is not a capture.</returns>
    /// <remarks>
    /// Runtime: O(1). This checks the target square once and, for en passant, at most one extra pawn square.
    /// </remarks>
    private PieceState GetCapturedPieceForMove(PieceState movingPiece, int matrixX, int matrixY)
    {
        var capturedPiece = GetPosition(matrixX, matrixY);
        if (capturedPiece != null)
        {
            return capturedPiece;
        }

        if (!movingPiece.IsPawn || !EnPassantTargetSquare.HasValue)
        {
            return null;
        }

        var enPassantTarget = EnPassantTargetSquare.Value;
        var direction = movingPiece.Player == "white" ? 1 : -1;
        var isDiagonalPawnMove = matrixX != movingPiece.MatrixX &&
                                 matrixY - movingPiece.MatrixY == direction;

        if (!isDiagonalPawnMove || enPassantTarget.x != matrixX || enPassantTarget.y != movingPiece.MatrixY)
        {
            return null;
        }

        var enPassantPawn = GetPosition(enPassantTarget.x, enPassantTarget.y);
        if (enPassantPawn != null && enPassantPawn.IsPawn && enPassantPawn.Player != movingPiece.Player)
        {
            return enPassantPawn;
        }

        return null;
    }

    /// <summary>
    /// Applies a move directly to this state so it can be checked and then undone.
    /// </summary>
    /// <param name="move">The move to apply temporarily.</param>
    /// <param name="isAttack">Whether the move captures a piece.</param>
    /// <returns>The information needed to undo the move again.</returns>
    /// <remarks>
    /// Runtime: O(1). This only updates the touched pieces and board squares, without cloning the whole state.
    /// </remarks>
    private MoveUndoState ApplyTemporaryMove(Move move, bool isAttack)
    {
        var movingPiece = GetPosition(move.GetFromMatrixX(), move.GetFromMatrixY());
        if (movingPiece == null)
        {
            throw new InvalidOperationException("No piece found on the move's starting square.");
        }

        PieceState capturedPiece = null;
        int capturedX = -1;
        int capturedY = -1;
        bool capturedWasActive = false;

        if (isAttack)
        {
            capturedPiece = GetCapturedPieceForMove(movingPiece, move.GetMatrixX(), move.GetMatrixY());
            if (capturedPiece != null)
            {
                capturedX = capturedPiece.MatrixX;
                capturedY = capturedPiece.MatrixY;
                capturedWasActive = capturedPiece.IsActive;
                _positions[capturedX, capturedY] = null;
                capturedPiece.IsActive = false;
            }
        }

        var undoState = new MoveUndoState
        {
            MovingPiece = movingPiece,
            MovingFromX = movingPiece.MatrixX,
            MovingFromY = movingPiece.MatrixY,
            MovingHadMoved = movingPiece.HasMoved,
            MovingOriginalName = movingPiece.Name,
            CapturedPiece = capturedPiece,
            CapturedX = capturedX,
            CapturedY = capturedY,
            CapturedWasActive = capturedWasActive,
            PreviousEnPassantTargetSquare = EnPassantTargetSquare,
            PreviousCurrentPlayer = CurrentPlayer,
            PreviousDisableMoveCache = _disableMoveCache
        };

        _disableMoveCache = true;

        _positions[undoState.MovingFromX, undoState.MovingFromY] = null;
        movingPiece.MatrixX = move.GetMatrixX();
        movingPiece.MatrixY = move.GetMatrixY();
        _positions[movingPiece.MatrixX, movingPiece.MatrixY] = movingPiece;

        PieceState castlingRook = null;
        int castlingRookFromX = -1;
        int castlingRookFromY = -1;
        bool castlingRookHadMoved = false;

        if (movingPiece.IsKing)
        {
            movingPiece.HasMoved = true;
            if (Math.Abs(undoState.MovingFromX - move.GetMatrixX()) == 2)
            {
                bool isRightRook = move.GetMatrixX() > undoState.MovingFromX;
                castlingRookFromX = isRightRook ? undoState.MovingFromX + 3 : undoState.MovingFromX - 4;
                castlingRookFromY = undoState.MovingFromY;
                int rookToX = isRightRook ? move.GetMatrixX() - 1 : move.GetMatrixX() + 1;

                castlingRook = GetPosition(castlingRookFromX, castlingRookFromY);
                if (castlingRook != null)
                {
                    castlingRookHadMoved = castlingRook.HasMoved;
                    _positions[castlingRookFromX, castlingRookFromY] = null;
                    castlingRook.MatrixX = rookToX;
                    castlingRook.MatrixY = castlingRookFromY;
                    castlingRook.HasMoved = true;
                    _positions[castlingRook.MatrixX, castlingRook.MatrixY] = castlingRook;
                }
            }
        }
        else if (movingPiece.IsRook)
        {
            movingPiece.HasMoved = true;
        }

        if (movingPiece.IsPawn && Math.Abs(undoState.MovingFromY - move.GetMatrixY()) == 2)
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

        undoState.CastlingRook = castlingRook;
        undoState.CastlingRookFromX = castlingRookFromX;
        undoState.CastlingRookFromY = castlingRookFromY;
        undoState.CastlingRookHadMoved = castlingRookHadMoved;

        return undoState;
    }

    /// <summary>
    /// Restores a temporarily applied move.
    /// </summary>
    /// <param name="undoState">The stored information from ApplyTemporaryMove.</param>
    /// <remarks>
    /// Runtime: O(1). This only restores the touched pieces and board squares.
    /// </remarks>
    private void UndoTemporaryMove(MoveUndoState undoState)
    {
        CurrentPlayer = undoState.PreviousCurrentPlayer;
        EnPassantTargetSquare = undoState.PreviousEnPassantTargetSquare;
        _disableMoveCache = undoState.PreviousDisableMoveCache;

        if (undoState.CastlingRook != null)
        {
            _positions[undoState.CastlingRook.MatrixX, undoState.CastlingRook.MatrixY] = null;
            undoState.CastlingRook.MatrixX = undoState.CastlingRookFromX;
            undoState.CastlingRook.MatrixY = undoState.CastlingRookFromY;
            undoState.CastlingRook.HasMoved = undoState.CastlingRookHadMoved;
            _positions[undoState.CastlingRookFromX, undoState.CastlingRookFromY] = undoState.CastlingRook;
        }

        _positions[undoState.MovingPiece.MatrixX, undoState.MovingPiece.MatrixY] = null;
        undoState.MovingPiece.MatrixX = undoState.MovingFromX;
        undoState.MovingPiece.MatrixY = undoState.MovingFromY;
        undoState.MovingPiece.HasMoved = undoState.MovingHadMoved;
        undoState.MovingPiece.SetName(undoState.MovingOriginalName);
        _positions[undoState.MovingFromX, undoState.MovingFromY] = undoState.MovingPiece;

        if (undoState.CapturedPiece != null)
        {
            undoState.CapturedPiece.MatrixX = undoState.CapturedX;
            undoState.CapturedPiece.MatrixY = undoState.CapturedY;
            undoState.CapturedPiece.IsActive = undoState.CapturedWasActive;
            _positions[undoState.CapturedX, undoState.CapturedY] = undoState.CapturedPiece;
        }
    }

    /// <summary>
    /// Walks in one straight direction until the path is blocked or leaves the board.
    /// </summary>
    /// <param name="xIncrement">The x step for each square.</param>
    /// <param name="yIncrement">The y step for each square.</param>
    /// <param name="piece">The piece that is looking along the line.</param>
    /// <returns>The empty move squares and the first enemy square, if one exists.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this walks at most 7 squares in one direction before it
    /// stops.
    /// </remarks>
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

    /// <summary>
    /// Checks a single target square for a normal move or attack.
    /// </summary>
    /// <param name="x">The target x coordinate.</param>
    /// <param name="y">The target y coordinate.</param>
    /// <param name="piece">The piece that is checking the square.</param>
    /// <returns>The move square if it is empty, or the attack square if it holds an enemy piece.</returns>
    /// <remarks>
    /// Runtime: O(1). This does one bounds check and one board lookup.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw king moves, including castling when allowed.
    /// </summary>
    /// <param name="piece">The king piece state.</param>
    /// <returns>The king's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(1) for the 8 neighboring squares, with extra castling checks when the king has not moved. In the castling
    /// case this also does 1 IsKingInCheck call and up to 4 IsMoveSafe simulations.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw queen moves by combining rook and bishop style lines.
    /// </summary>
    /// <param name="piece">The queen piece state.</param>
    /// <returns>The queen's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this scans 8 directions and walks at most 56 ray steps in
    /// total.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw rook moves.
    /// </summary>
    /// <param name="piece">The rook piece state.</param>
    /// <returns>The rook's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this scans 4 directions and walks at most 28 ray steps in
    /// total.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw bishop moves.
    /// </summary>
    /// <param name="piece">The bishop piece state.</param>
    /// <returns>The bishop's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(n), where n is the board width. On an 8x8 board this scans 4 diagonals and walks at most 28 ray steps in
    /// total.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw knight moves.
    /// </summary>
    /// <param name="piece">The knight piece state.</param>
    /// <returns>The knight's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(1). This checks the 8 knight jump targets once each.
    /// </remarks>
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

    /// <summary>
    /// Gets the raw pawn moves, captures, and en passant attacks.
    /// </summary>
    /// <param name="piece">The pawn piece state.</param>
    /// <returns>The pawn's move squares and attack squares.</returns>
    /// <remarks>
    /// Runtime: O(1). This checks up to 2 forward squares, 2 diagonal captures, and the constant-size en passant case.
    /// </remarks>
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

/// <summary>
/// A lightweight snapshot of one chess piece used inside GameState.
/// </summary>
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

    /// <summary>
    /// Creates one stored piece snapshot.
    /// </summary>
    /// <param name="name">The piece name.</param>
    /// <param name="player">The owning player.</param>
    /// <param name="matrixX">The board x coordinate.</param>
    /// <param name="matrixY">The board y coordinate.</param>
    /// <param name="isActive">Whether the piece is still on the board.</param>
    /// <param name="hasMoved">Whether the piece has moved before.</param>
    /// <param name="isInCheck">Whether the piece is currently in check.</param>
    /// <remarks>
    /// Runtime: O(1). This only stores the provided values.
    /// </remarks>
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

    /// <summary>
    /// Creates a PieceState from one live Unity piece.
    /// </summary>
    /// <param name="piece">The live piece to copy.</param>
    /// <param name="isActive">Whether the piece is active in the scene.</param>
    /// <returns>The copied piece state.</returns>
    /// <remarks>
    /// Runtime: O(1). This reads one live piece and does only constant-size king and rook state checks.
    /// </remarks>
    public static PieceState FromPiece(Piece piece, bool isActive)
    {
        return new PieceState(piece.name, piece.GetPlayer(), piece.GetxBoard(), piece.GetyBoard(), isActive,
            GetHasMoved(piece), GetIsInCheck(piece));
    }

    /// <summary>
    /// Creates a deep copy of this stored piece state.
    /// </summary>
    /// <returns>A copied piece state.</returns>
    /// <remarks>
    /// Runtime: O(1). This creates one new PieceState with the same values.
    /// </remarks>
    public PieceState Clone()
    {
        return new PieceState(Name, Player, MatrixX, MatrixY, IsActive, HasMoved, IsInCheck);
    }

    /// <summary>
    /// Gets the material value used by the AI evaluation.
    /// </summary>
    /// <returns>The piece value.</returns>
    /// <remarks>
    /// Runtime: O(1). This does at most 6 piece-type checks.
    /// </remarks>
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

    /// <summary>
    /// Changes this stored pawn into a queen.
    /// </summary>
    /// <remarks>
    /// Runtime: O(1). This only updates the stored name.
    /// </remarks>
    public void PromoteToQueen()
    {
        Name = Player + "_queen";
    }

    /// <summary>
    /// Restores the stored piece name.
    /// </summary>
    /// <param name="name">The name to store.</param>
    /// <remarks>
    /// Runtime: O(1). This only updates the stored name.
    /// </remarks>
    public void SetName(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Reads whether a live king or rook has moved.
    /// </summary>
    /// <param name="piece">The live piece to inspect.</param>
    /// <returns>True if the piece tracks itself as moved.</returns>
    /// <remarks>
    /// Runtime: O(1). This does at most two type checks and one getter call.
    /// </remarks>
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

    /// <summary>
    /// Reads whether a live king is currently in check.
    /// </summary>
    /// <param name="piece">The live piece to inspect.</param>
    /// <returns>True if the piece is a king in check.</returns>
    /// <remarks>
    /// Runtime: O(1). This does one type check and one getter call.
    /// </remarks>
    private static bool GetIsInCheck(Piece piece)
    {
        if (piece is King king)
        {
            return king.GetInCheck();
        }

        return false;
    }
}
