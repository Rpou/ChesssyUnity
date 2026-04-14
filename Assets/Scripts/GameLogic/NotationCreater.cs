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
            var letterOfSquareMovedTo = ConvertNrToChar(xPositionAfter + 1).ToString();
            var letterOfSquareBeforeMove = ConvertNrToChar(xPositionBefore + 1);

            var result = "";
            if (killedPiece) result += "x";
            if (piece is Pawn)
            {
                if (killedPiece) result = letterOfSquareBeforeMove + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                else result = letterOfSquareMovedTo + result + (yPositionAfter + 1);
                if ((game.GetCurrentPlayer() && yPositionAfter == 7) ||
                    (!game.GetCurrentPlayer() && yPositionAfter == 0)) result += "Q";
                if (putInCheck) result += "+";
                return result;
            }

            game.SetPositionEmpty(xPositionAfter, yPositionAfter);
            piece.SetXBoard(xPositionBefore);
            piece.SetYBoard(yPositionBefore);
            piece.SetCoords();
            game.SetPosition(piece.gameObject);

            try
            {
                var overlap = LegalMovesOverlapSameTypePiece(piece, xPositionAfter, yPositionAfter, game);

                if (piece is Knight)
                {
                    if (overlap) result = "N" + letterOfSquareBeforeMove + (yPositionBefore + 1) + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                    else result = "N" + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                }

                if (piece is Bishop)
                {
                    result = "B" + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                }

                if (piece is Rook)
                {
                    if (overlap) result = "R" + letterOfSquareBeforeMove + (yPositionBefore + 1) + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                    else result = "R" + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                }

                if (piece is Queen)
                {
                    if (overlap) result = "Q" + letterOfSquareBeforeMove + (yPositionBefore + 1) + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                    else result = "Q" + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                }

                if (piece is King)
                {
                    var isRightRook = xPositionAfter > xPositionBefore;
                    if (castled)
                    {
                        if (isRightRook) result = "O-O";
                        else result = "O-O-O";
                    }
                    else result = "K" + result + letterOfSquareMovedTo + (yPositionAfter + 1);
                }

                if (putInCheck) result += "+";
                return result;
            }
            finally
            {
                game.SetPositionEmpty(xPositionBefore, yPositionBefore);
                piece.SetXBoard(xPositionAfter);
                piece.SetYBoard(yPositionAfter);
                piece.SetCoords();
                game.SetPosition(piece.gameObject);
            }
        }

        // worst case: 16 + (180) = 196
        private static bool LegalMovesOverlapSameTypePiece(Piece pieceType, int targetX, int targetY, Game game)
        {
            var player = game.GetCurrentPlayer();
            var allPieces = player ? game.playerWhite : game.playerBlack;

            if (pieceType is Knight)
            {
                return LegalMovesOverlap<Knight>(allPieces, pieceType.gameObject, targetX, targetY);
            }

            if (pieceType is Rook)
            {
                return LegalMovesOverlap<Rook>(allPieces, pieceType.gameObject, targetX, targetY);
            }

            if (pieceType is Queen)
            {
                return LegalMovesOverlap<Queen>(allPieces, pieceType.gameObject, targetX, targetY);
            }

            return false;
        }

        // worst case: 16 * 27 = 432
        private static bool LegalMovesOverlap<T>(IEnumerable<GameObject> allPieces, GameObject movedPieceObject, int targetX, int targetY) where T : Piece
        {
            foreach (var gameObject in allPieces)
            {
                if (gameObject == null || !gameObject.activeSelf || gameObject == movedPieceObject)
                {
                    continue;
                }

                if (gameObject.GetComponent<Piece>() is not T matchedPiece)
                {
                    continue;
                }

                (List<Vector2Int> moves, List<Vector2Int> attacks) = matchedPiece.GetAllLegalMoves();
                if (ContainsSquare(moves, targetX, targetY) || ContainsSquare(attacks, targetX, targetY))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSquare(IEnumerable<Vector2Int> squares, int x, int y)
        {
            foreach (var square in squares)
            {
                if (square.x == x && square.y == y)
                {
                    return true;
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
