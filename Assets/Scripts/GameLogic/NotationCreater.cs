using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public static class NotationCreater
    {
        // worst case: (196) + 6(string made) = 202
    public static string CreateNotation(Piece piece, int xPositionBefore, int yPositionBefore, int xPositionAfter, 
        int yPositionAfter, bool putInCheck, bool killedPiece, bool castled, Game game)
    {
        piece.SetXBoard(xPositionBefore);
        piece.SetYBoard(yPositionBefore);
        piece.SetCoords();
        game.SetPosition(piece.gameObject);
        
        var letterOfSquareMovedTo = ConvertNrToChar(xPositionAfter + 1).ToString();
        var letterOfSquareBeforeMove = ConvertNrToChar(xPositionBefore + 1);
        var overlap = LegalMovesOverlapSameTypePiece(piece, game);
        
        var result = "";
        if (killedPiece) result += "x";
        if (piece is Pawn)
        {
            if (killedPiece) result = letterOfSquareBeforeMove + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = letterOfSquareMovedTo + result + (yPositionAfter+1);
            if ((game.GetCurrentPlayer() == "white" && yPositionAfter == 7) ||
                (game.GetCurrentPlayer() == "black" && yPositionAfter == 0)) result += "Q";
        }

        if (piece is Knight)
        {
            if (overlap) result = "N" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = "N" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Bishop)
        {
            result = "B" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Rook)
        {
            if (overlap) result = "R" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = "R" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Queen)
        {
            if (overlap) result = "Q" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = "Q" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is King)
        {
            var isRightRook = xPositionAfter > xPositionBefore;
            if (castled)
            {
                if (isRightRook) result = "O-O";
                else result = "O-O-O";
            }
            else result = "K" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }
        
        if (putInCheck) result += "+";
        
        game.SetPositionEmpty(xPositionBefore, yPositionBefore); // Remove our piece from the new location
        piece.SetXBoard(xPositionAfter);
        piece.SetYBoard(yPositionAfter);
        piece.SetCoords();
        game.SetPosition(piece.gameObject);
        
        return result;
    }

    // worst case: 16 + (180) = 196
    private static bool LegalMovesOverlapSameTypePiece(Piece pieceType, Game game)
    {
        var player = game.GetCurrentPlayer();
        List<GameObject> allPieces = new List<GameObject>();
        allPieces.AddRange(player == "black" ? game.playerBlack : game.playerWhite);
        
        if (pieceType is Knight)
        {
            return LegalMovesOverlap<Knight>(allPieces);
        }

        if (pieceType is Rook)
        {
            return LegalMovesOverlap<Rook>(allPieces);
        }

        if (pieceType is Queen)
        {
            return LegalMovesOverlap<Queen>(allPieces);
        }

        return false;
    }

    // worst case: 16 + 10 * 10 + 8 * 8 = 180
    private static bool LegalMovesOverlap<T>(List<GameObject> allPieces) where T : Piece
    {
        Piece piece1 = null;
        Piece piece2 = null;
        foreach (var gameObject in allPieces)
        {
            if (gameObject == null) continue;
            Piece piece = gameObject.GetComponent<Piece>();
            if (piece is T matchedPiece && piece1 == null)
            {
                piece1 = matchedPiece;
            }
            else if (piece is T matchedPiece2)
            {
                piece2 = matchedPiece2;
            }

            if (piece1 != null && piece2 != null)
            {
                (List<Vector2Int> moves1, List<Vector2Int> attacks1)
                    = piece1.GetAllLegalMoves();
                (List<Vector2Int> moves2, List<Vector2Int> attacks2) 
                    = piece2.GetAllLegalMoves();

                foreach (var move1 in moves1)
                {
                    foreach (var move2 in moves2)
                    {
                        if (move1 == move2) return true;
                    }
                }
                    
                foreach (var attack1 in attacks1)
                {
                    foreach (var attack2 in attacks2)
                    {
                        if (attack1 == attack2) return true;
                    }
                }
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Converts a nr to a letter based  on ASCII
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private static char ConvertNrToChar(int number)
    {
        return (char)('a' + number - 1);
    }
    }
}