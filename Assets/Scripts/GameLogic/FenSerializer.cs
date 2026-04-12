using System;
using System.Text;

namespace GameLogic
{
    public static class FenSerializer
    {
        public static string Serialize(Game game)
        {
            return string.Join(" ",
                BuildBoard(game),
                game.GetCurrentPlayer() == "white" ? "w" : "b",
                BuildCastlingAvailability(game),
                BuildEnPassantSquare(game),
                Math.Max(0, game.GetHalfmoveClock()).ToString(),
                Math.Max(1, game.GetFullmoveNumber()).ToString());
        }

        public static string Serialize(GameState state, int halfmoveClock = 0, int fullmoveNumber = 1)
        {
            return string.Join(" ",
                BuildBoard(state),
                state.CurrentPlayer == "white" ? "w" : "b",
                BuildCastlingAvailability(state),
                BuildEnPassantSquare(state),
                Math.Max(0, halfmoveClock).ToString(),
                Math.Max(1, fullmoveNumber).ToString());
        }

        private static string BuildBoard(GameState state)
        {
            var fen = new StringBuilder();

            for (var y = 7; y >= 0; y--)
            {
                var emptySquares = 0;

                for (var x = 0; x < 8; x++)
                {
                    var piece = state.GetPosition(x, y);
                    if (piece == null)
                    {
                        emptySquares++;
                        continue;
                    }

                    if (emptySquares > 0)
                    {
                        fen.Append(emptySquares);
                        emptySquares = 0;
                    }

                    fen.Append(ToFenPiece(piece));
                }

                if (emptySquares > 0)
                {
                    fen.Append(emptySquares);
                }

                if (y > 0)
                {
                    fen.Append('/');
                }
            }

            return fen.ToString();
        }

        private static string BuildBoard(Game game)
        {
            var fen = new StringBuilder();

            for (var y = 7; y >= 0; y--)
            {
                var emptySquares = 0;

                for (var x = 0; x < 8; x++)
                {
                    var pieceObject = game.GetPosition(x, y);
                    var piece = pieceObject != null ? pieceObject.GetComponent<Piece>() : null;
                    if (piece == null || !pieceObject.activeSelf)
                    {
                        emptySquares++;
                        continue;
                    }

                    if (emptySquares > 0)
                    {
                        fen.Append(emptySquares);
                        emptySquares = 0;
                    }

                    fen.Append(ToFenPiece(piece));
                }

                if (emptySquares > 0)
                {
                    fen.Append(emptySquares);
                }

                if (y > 0)
                {
                    fen.Append('/');
                }
            }

            return fen.ToString();
        }

        private static string BuildCastlingAvailability(GameState state)
        {
            var castling = new StringBuilder();

            AppendCastlingRights(state, "white", 0, 'K', 'Q', castling);
            AppendCastlingRights(state, "black", 7, 'k', 'q', castling);

            return castling.Length == 0 ? "-" : castling.ToString();
        }

        private static string BuildCastlingAvailability(Game game)
        {
            var castling = new StringBuilder();

            AppendCastlingRights(game, 0, 'K', 'Q', castling);
            AppendCastlingRights(game, 7, 'k', 'q', castling);

            return castling.Length == 0 ? "-" : castling.ToString();
        }

        private static void AppendCastlingRights(GameState state, string player, int homeRank,
            char kingSideSymbol, char queenSideSymbol, StringBuilder castling)
        {
            var king = state.GetPosition(4, homeRank);
            if (!IsEligibleKing(king, player))
            {
                return;
            }

            var kingSideRook = state.GetPosition(7, homeRank);
            if (IsEligibleRook(kingSideRook, player))
            {
                castling.Append(kingSideSymbol);
            }

            var queenSideRook = state.GetPosition(0, homeRank);
            if (IsEligibleRook(queenSideRook, player))
            {
                castling.Append(queenSideSymbol);
            }
        }

        private static void AppendCastlingRights(Game game, int homeRank, char kingSideSymbol, char queenSideSymbol,
            StringBuilder castling)
        {
            var king = GetLivePiece<King>(game, 4, homeRank);
            if (king == null || king.GetHasMoved())
            {
                return;
            }

            var kingSideRook = GetLivePiece<Rook>(game, 7, homeRank);
            if (kingSideRook != null && !kingSideRook.HasMoved())
            {
                castling.Append(kingSideSymbol);
            }

            var queenSideRook = GetLivePiece<Rook>(game, 0, homeRank);
            if (queenSideRook != null && !queenSideRook.HasMoved())
            {
                castling.Append(queenSideSymbol);
            }
        }

        private static bool IsEligibleKing(PieceState piece, string player)
        {
            return piece != null &&
                   piece.IsKing &&
                   piece.Player == player &&
                   piece.IsActive &&
                   !piece.HasMoved;
        }

        private static bool IsEligibleRook(PieceState piece, string player)
        {
            return piece != null &&
                   piece.IsRook &&
                   piece.Player == player &&
                   piece.IsActive &&
                   !piece.HasMoved;
        }

        private static string BuildEnPassantSquare(GameState state)
        {
            if (!state.EnPassantTargetSquare.HasValue)
            {
                return "-";
            }

            var pawnSquare = state.EnPassantTargetSquare.Value;
            var pawn = state.GetPosition(pawnSquare.x, pawnSquare.y);
            if (pawn == null || !pawn.IsPawn)
            {
                return "-";
            }

            var targetY = pawnSquare.y + (pawn.Player == "white" ? -1 : 1);
            if (!state.PositionOnBoard(pawnSquare.x, targetY))
            {
                return "-";
            }

            return ToAlgebraic(pawnSquare.x, targetY);
        }

        private static string BuildEnPassantSquare(Game game)
        {
            var pawn = game.GetEnPassentTarget();
            if (pawn == null)
            {
                return "-";
            }

            var targetY = pawn.GetyBoard() + (pawn.GetPlayer() == "white" ? -1 : 1);
            if (!game.PositionOnBoard(pawn.GetxBoard(), targetY))
            {
                return "-";
            }

            return ToAlgebraic(pawn.GetxBoard(), targetY);
        }

        private static char ToFenPiece(PieceState piece)
        {
            char symbol;

            if (piece.IsPawn) symbol = 'p';
            else if (piece.IsKnight) symbol = 'n';
            else if (piece.IsBishop) symbol = 'b';
            else if (piece.IsRook) symbol = 'r';
            else if (piece.IsQueen) symbol = 'q';
            else symbol = 'k';

            return piece.Player == "white" ? char.ToUpperInvariant(symbol) : symbol;
        }

        private static char ToFenPiece(Piece piece)
        {
            char symbol;

            if (piece is Pawn) symbol = 'p';
            else if (piece is Knight) symbol = 'n';
            else if (piece is Bishop) symbol = 'b';
            else if (piece is Rook) symbol = 'r';
            else if (piece is Queen) symbol = 'q';
            else symbol = 'k';

            return piece.GetPlayer() == "white" ? char.ToUpperInvariant(symbol) : symbol;
        }

        private static T GetLivePiece<T>(Game game, int x, int y) where T : Piece
        {
            var pieceObject = game.GetPosition(x, y);
            if (pieceObject == null || !pieceObject.activeSelf)
            {
                return null;
            }

            return pieceObject.GetComponent<T>();
        }

        private static string ToAlgebraic(int x, int y)
        {
            return $"{(char)('a' + x)}{y + 1}";
        }
    }
}
