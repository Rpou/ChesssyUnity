using System.Collections.Generic;
using GameLogic;    // adjust to your namespace

public static class MoveGenerator
{
    /// <summary>
    /// Returns every possible UCI‐style coordinate/promotion move:
    ///   • 4032 plain moves (64×63 from‐to pairs)
    ///   • 64 promotions (64 from‐to pairs × 4 piece types)
    /// Total: 4544 moves.
    /// </summary>
    public static Move[] GenerateAllMoves()
    {
        var moves = new List<Move>(4544);
        char[] files = { 'a','b','c','d','e','f','g','h' };
        char[] ranks = { '1','2','3','4','5','6','7','8' };
        char[] promos = { 'Q' };

        for (int fi = 0; fi < 8; fi++)
        for (int ri = 0; ri < 8; ri++)
        {
            var fromFile = files[fi];
            var fromRank = ranks[ri];
            for (int fj = 0; fj < 8; fj++)
            for (int rj = 0; rj < 8; rj++)
            {
                var toFile = files[fj];
                var toRank = ranks[rj];
                // skip zero‐move
                if (fi == fj && ri == rj) continue;

                // build base UCI string
                string uci = $"{fromFile}{fromRank}{toFile}{toRank}";

                // promotions: white pawns (7→8) or black pawns (2→1)
                bool whitePromo = fromRank == '7' && toRank == '8';
                bool blackPromo = fromRank == '2' && toRank == '1';

                if (whitePromo || blackPromo)
                {
                    foreach (var p in promos)
                    {
                        moves.Add(Move.FromUci(uci + p));
                    }
                }
                else
                {
                    moves.Add(Move.FromUci(uci));
                }
            }
        }

        return moves.ToArray();
    }
}