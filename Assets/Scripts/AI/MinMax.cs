using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


public class MinMax : AI
{

    private const int MaterialWeight = 10;
    private const int UnmovedPiecePenalty = 1;
    private const int MobilityWeight = 1;
    // 32
    private int Utility(GameState game)
    {
        int score = 0;
        foreach (var piece in game.GetPieces("white"))
        {
            if (piece.IsActive)
            {
                score += piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved && (!piece.IsKing || !piece.IsPawn)) score -= UnmovedPiecePenalty;
                if (piece.IsInCheck) score -= 8;
            }

        }

        foreach (var piece in game.GetPieces("black"))
        {
            if (piece.IsActive)
            {
                score -= piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved) score += UnmovedPiecePenalty;
                if (piece.IsInCheck) score += 8;
            }
        }

        int whiteMobility = game.GetPossibleMoveCount("white");
        int blackMobility = game.GetPossibleMoveCount("black");
        score += (int)((whiteMobility - blackMobility) * 0.2f);

        return score;
    }

    public bool isCutoff(int depth)
    {
        return depth > 3;
    }

    public EvalMove MaxValue(GameState game, int depth, int alpha, int beta)
    {
        List<Move> allmoves = game.GetPossibleMoves(); // find all legal moves.
        List<Move> moves = game.SortAttack(allmoves);

        if (isCutoff(depth) || moves.Count == 0)
        { // see if we should stop going further down.
            return new EvalMove(Utility(game), null);
        }

        int bestEval = int.MinValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move

        // look through all moves, and see which returns the highest utility.
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];

            GameState gameSim = game.ApplyMove(move);

            EvalMove ev = MinValue(gameSim, depth + 1, alpha, beta); // looks at best move for Min player
            if (ev.value > bestEval)
            { // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                alpha = Mathf.Max(alpha, ev.value);
            }
            if (alpha >= beta) return new EvalMove(bestEval, bestMove);  // makes beta cut

        }
        return new EvalMove(bestEval, bestMove);
    }


    public EvalMove MinValue(GameState game, int depth, int alpha, int beta)
    {
        List<Move> allmoves = game.GetPossibleMoves(); // find all legal moves.
        List<Move> moves = game.SortAttack(allmoves);
        if (isCutoff(depth) || moves.Count == 0)
        { // see if we should stop going further down.
            return new EvalMove(Utility(game), null);
        }

        int bestEval = int.MaxValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move

        // look through all moves, and see which returns the highest utility.
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            GameState gameSim = game.ApplyMove(move);

            EvalMove ev = MaxValue(gameSim, depth + 1, alpha, beta); // looks at best move for Min player
            if (ev.value < bestEval)
            { // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                beta = Mathf.Min(beta, ev.value);
            }
            if (beta <= alpha) return new EvalMove(bestEval, bestMove); // makes alpha cut

        }
        return new EvalMove(bestEval, bestMove);
    }

    // possiblemoves: 432. utility: 32. for loop: (432(possiblemoves)*32(gamestate))+(432*32). MakeNextMove: 1942 total: 432+32+27648+1942=30054
    /// <summary>
    /// Builds a simulated state, runs MinMax with alpha-beta pruning, and makes the chosen live move.
    /// </summary>
    /// <param name="game">The live game that the AI should move in.</param>
    /// <remarks>
    /// Runtime: Worst-case O(b^d * g), where b is the number of legal moves in a position, d is the search depth in plies,
    /// and g is the cost of one GameState.GetPossibleMoves() call.
    ///
    /// In this implementation the hot code is not the final MakeNextMove call. The expensive part is the search:
    /// every visited node calls GetPossibleMoves once, every search edge calls ApplyMove once, and every leaf calls Utility once.
    ///
    /// For the current cutoff rule (depth > 2), the search can visit at most:
    /// 1 + b + b^2 + b^3 nodes,
    /// b + b^2 + b^3 ApplyMove calls,
    /// and b^3 Utility calls.
    ///
    /// The final live MakeNextMove call happens only once, so it is not the main bottleneck.
    /// </remarks>
    public override void MakeMove(Game game)
    {

        GameState gameState = new GameState(game);
        EvalMove evmove = game.GetCurrentPlayer() == "white"
            ? MaxValue(gameState, 0, int.MinValue, int.MaxValue)
            : MinValue(gameState, 0, int.MinValue, int.MaxValue);

        if (!evmove.move.HasValue) return;

        Move move = evmove.move.Value;
        GameObject pieceObj = game.GetPosition(move.GetFromMatrixX(), move.GetFromMatrixY());
        game.MakeNextMove(pieceObj, move.GetMatrixX(), move.GetMatrixY(), move.GetIsAttack());
    }
}
