using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class MovementPatterns
{
    /// <summary>
    /// Calculates valid moves for a piece in a straight-line direction (horizontal, vertical, or diagonal).
    /// This function returns two lists:
    /// - `movableSquares`: Squares the piece can move to.
    /// - `attackableSquares`: Squares containing enemy pieces that can be captured.
    /// The movement continues in the specified direction until it encounters another piece or reaches the board edge.
    /// </summary>
    /// <param name="xIncrement">The step size in the X direction (e.g., 1 for right, -1 for left).</param>
    /// <param name="yIncrement">The step size in the Y direction (e.g., 1 for up, -1 for down).</param>
    /// <param name="piece">The piece for which movement is being calculated.</param>
    /// <param name="game">The game instance used to check board positions and determine valid moves.</param>
    /// <returns>A tuple containing two lists:
    /// - `movableSquares`: Valid squares the piece can move to.
    /// - `attackableSquares`: Valid squares where the piece can capture an enemy.</returns>
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        CanSeeInLine(int xIncrement, int yIncrement, Piece piece, Game game)
    {
        var movableSquares = new List<Vector2Int>();
        var attackableSquares = new List<Vector2Int>();

        int x = piece.GetxBoard() + xIncrement;
        int y = piece.GetyBoard() + yIncrement;

        // Continue moving until we hit a piece or go out of bounds
        while (game.PositionOnBoard(x, y) && game.GetPosition(x, y) == null)
        {
            movableSquares.Add(new Vector2Int(x, y));
            x += xIncrement;
            y += yIncrement;
        }

        // If the final position is still within the board, check if it's an enemy piece
        if (game.PositionOnBoard(x, y))
        {
            GameObject pieceOnSquare = game.GetPosition(x, y);
            if (pieceOnSquare != null && pieceOnSquare.GetComponent<Piece>().GetPlayer() != piece.GetPlayer())
            {
                attackableSquares.Add(new Vector2Int(x, y));
            }
        }

        return (movableSquares, attackableSquares);
    }
    
    /// <summary>
    /// Determines if a piece can move to or attack a specific board position.
    /// This function returns two lists:
    /// - `movableSquares`: Squares the piece can move to.
    /// - `attackableSquares`: Squares containing enemy pieces that can be captured.
    /// </summary>
    /// <param name="x">The X-coordinate of the target position.</param>
    /// <param name="y">The Y-coordinate of the target position.</param>
    /// <param name="piece">The piece checking for valid moves.</param>
    /// <param name="game">The game instance used to check board positions.</param>
    /// <returns>A tuple containing two lists:
    /// - `movableSquares`: Valid squares the piece can move to.
    /// - `attackableSquares`: Valid squares where the piece can capture an enemy.</returns>
    private static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares)  
        CanSeePoint(int x, int y, Piece piece, Game game)
    {
        var movableSquares = new List<Vector2Int>();
        var attackableSquares = new List<Vector2Int>();

        // If the target position is outside the board, return empty lists
        if (!game.PositionOnBoard(x, y)) return (movableSquares, attackableSquares);

        GameObject pieceOnSquare = game.GetPosition(x, y);

        // If the target square is empty, add it to movable squares
        if (pieceOnSquare == null)
        {
            movableSquares.Add(new Vector2Int(x, y));
        }
        // If the target square contains an enemy piece, add it to attackable squares
        else if (pieceOnSquare.GetComponent<Piece>().GetPlayer() != piece.GetPlayer())
        {
            attackableSquares.Add(new Vector2Int(x, y));
        }

        return (movableSquares, attackableSquares);
    }
    
    // King
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        GetKingMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        // Get king's current position
        int x = piece.GetxBoard();
        int y = piece.GetyBoard();

        int[][] directions = {
            new int[] { 0, 1 }, new int[] { 0, -1 }, new int[] { -1, -1 }, new int[] { -1, 0 },
            new int[] { -1, 1 }, new int[] { 1, -1 }, new int[] { 1, 0 }, new int[] { 1, 1 }
        };

        foreach (var dir in directions)
        {
            int newX = x + dir[0]; // Offset x-coordinate
            int newY = y + dir[1]; // Offset y-coordinate

            var (moves, attacks) = CanSeePoint(newX, newY, piece, game);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    
    // Queen
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        GetQueenMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions = {
            new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { 1, 1 }, new int[] { -1, 0 },
            new int[] { 0, -1 }, new int[] { -1, -1 }, new int[] { -1, 1 }, new int[] { 1, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece, game);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    // Rook
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        GetRookMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions = {
            new int[] { 1, 0 }, new int[] { -1, 0 }, new int[] { 0, 1 }, new int[] { 0, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece, game);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    // Bishop 
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        GetBishopMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int[][] directions = {
            new int[] { 1, 1 }, new int[] { -1, 1 }, new int[] { 1, -1 }, new int[] { -1, -1 }
        };

        foreach (var dir in directions)
        {
            var (moves, attacks) = CanSeeInLine(dir[0], dir[1], piece, game);
            moveSquares.AddRange(moves);
            attackSquares.AddRange(attacks);
        }

        return (moveSquares, attackSquares);
    }

    // Knight 
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) 
        GetKnightMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        // Get knight's current position
        int x = piece.GetxBoard();
        int y = piece.GetyBoard();

        int[][] moves = {
            new int[] { 1, 2 }, new int[] { -1, 2 }, new int[] { 1, -2 }, new int[] { -1, -2 },
            new int[] { 2, 1 }, new int[] { -2, 1 }, new int[] { 2, -1 }, new int[] { -2, -1 }
        };

        foreach (var move in moves)
        {
            int newX = x + move[0]; // Offset x-coordinate
            int newY = y + move[1]; // Offset y-coordinate

            var (movesList, attacksList) = CanSeePoint(newX, newY, piece, game);
            moveSquares.AddRange(movesList);
            attackSquares.AddRange(attacksList);
        }

        return (moveSquares, attackSquares);
    }

    
    // Pawn
    public static (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares)  
        GetPawnMoves(Piece piece, Game game)
    {
        var moveSquares = new List<Vector2Int>();
        var attackSquares = new List<Vector2Int>();

        int x = piece.GetxBoard();
        int y = piece.GetyBoard();
        int direction = piece.GetPlayer().Equals("white") ? 1 : -1;
        int startRow = piece.GetPlayer().Equals("white") ? 1 : 6;

        // Forward move
        if (game.PositionOnBoard(x, y + direction) && game.GetPosition(x, y + direction) == null)
        {
            moveSquares.Add(new Vector2Int(x, y + direction));
            
            // Double move from start position
            if (y == startRow && game.GetPosition(x, y + (2 * direction)) == null)
            {
                moveSquares.Add(new Vector2Int(x, y + (2 * direction)));
            }
        }

        // Diagonal attack moves
        int attackY = y + direction;

        if (game.PositionOnBoard(x + 1, attackY) && game.GetPosition(x + 1, attackY) != null &&
            game.GetPosition(x + 1, attackY).GetComponent<Piece>().GetPlayer() != piece.GetPlayer())
        {
            attackSquares.Add(new Vector2Int(x + 1, attackY));
        }

        if (game.PositionOnBoard(x - 1, attackY) && game.GetPosition(x - 1, attackY) != null &&
            game.GetPosition(x - 1, attackY).GetComponent<Piece>().GetPlayer() != piece.GetPlayer())
        {
            attackSquares.Add(new Vector2Int(x - 1, attackY));
        }

        return (moveSquares, attackSquares);
    }

    public static void SpawnAllMovePlates(List<Vector2Int> moveSquares, List<Vector2Int> attackSquares, Piece piece, Game game)
    {
        foreach (var move in moveSquares)
        {
            if (IsMoveSafe(move.x, move.y,piece, game))
            {
                game.SpawnMovePlate(move.x, move.y, piece);
            }
        }

        foreach (var attack in attackSquares)
        {
            if (IsMoveSafe(attack.x, attack.y, piece, game))
            {
                game.SpawnAttackMovePlate(attack.x,attack.y, piece);
            }
        }
    }
    
    private static bool IsMoveSafe(int x, int y, Piece piece, Game game)
    {
        // Save the current board state
        GameObject pieceOnAttackSquare = game.GetPosition(x, y);
        GameObject pieceImGonnaMove = piece.gameObject;
        int correctX = piece.GetxBoard();
        int correctY = piece.GetyBoard();

        // Simulate the move
        game.SetPositionEmpty(correctX, correctY);

        // If capturing a piece, temporarily remove it
        if (pieceOnAttackSquare != null)
        {
            game.SetPositionEmpty(x, y); // Remove the opponent's piece for simulation
            pieceOnAttackSquare.SetActive(false);
        }
        
        piece.SetXBoard(x);
        piece.SetYBoard(y);
        piece.SetCoords();
        game.SetPosition(pieceImGonnaMove);
        
        // Check if the king is now in check
        King king = game.CheckIfKingInCheck(piece.GetPlayer(), true);
        
        // Restore the board state
        game.SetPositionEmpty(x, y); // Remove our piece from the new location

        // Restore the captured piece (if there was one)
        if (pieceOnAttackSquare != null)
        {
            Piece pieceOnAttackSquarepiece = pieceOnAttackSquare.GetComponent<Piece>();
            pieceOnAttackSquarepiece.SetXBoard(x);
            pieceOnAttackSquarepiece.SetYBoard(y);
            pieceOnAttackSquarepiece.SetCoords();
            pieceOnAttackSquare.SetActive(true);
            game.SetPosition(pieceOnAttackSquare); // Restore enemy piece
        }

        // Restore our piece back to the original spot
       
        piece.SetXBoard(correctX);
        piece.SetYBoard(correctY);
        piece.SetCoords();
        game.SetPosition(piece.gameObject);

        return king == null;
    }



}
