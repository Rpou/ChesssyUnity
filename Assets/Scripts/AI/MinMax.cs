using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine.Rendering;
using UnityEditor.Experimental.GraphView;


public class MinMax : AI
{

    private const int MaterialWeight = 100;
    private const int UnmovedPiecePenalty = 5;
    private const int MobilityWeight = 1;
    private int evaluatedMoves = 0;

    public int maxDepth = 3;
    // 32
    private int Utility(GameState game)
    {
        int score = 0;
        // white pieces
        foreach (var piece in game.GetPieces(true))
        {
            if (piece.IsActive)
            {
                score += piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved && (!piece.IsKing || !piece.IsPawn)) score -= UnmovedPiecePenalty;
                if (piece.IsInCheck) score -= 80;
            }

        }

        // black pieces.
        foreach (var piece in game.GetPieces(false))
        {
            if (piece.IsActive)
            {
                score -= piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved && (!piece.IsKing || !piece.IsPawn)) score -= UnmovedPiecePenalty;
                if (piece.IsInCheck) score += 80;
            }
        }

        //int whiteMobility = game.GetPossibleMoveCount("white");
        //int blackMobility = game.GetPossibleMoveCount("black");
        //score += (int)((whiteMobility - blackMobility) * 0.2f);

        return score;
    }

    public bool isCutoff(int depth)
    {
        return depth > maxDepth;
    }

    public EvalMove MaxValue(GameState game, int depth, int alpha, int beta)
    {
        if (isCutoff(depth))
        { // see if we should stop going further down.
            return new EvalMove(Utility(game), null);
        }

        bool movingPlayer = game.CurrentPlayerIsWhite;
        List<Move> moves = game.GetAllPossibleMoves(); // find all pseudo-legal moves.
        int bestEval = int.MinValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move
        bool foundLegalMove = false;

        // look through all moves, and see which returns the highest utility.
        for (int i = 0; i < moves.Count; i++)
        {
            evaluatedMoves += 1;
            var move = moves[i];
            game.ApplyMoveInPlace(move, move.GetIsAttack());
            if (game.IsKingInCheck(movingPlayer))
            {
                game.UndoLastMove();
                continue;
            }

            foundLegalMove = true;
            EvalMove ev = MinValue(game, depth + 1, alpha, beta); // looks at best move for Min player
            game.UndoLastMove();
            if (ev.value > bestEval)
            { // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                alpha = Mathf.Max(alpha, ev.value);
            }
            if (alpha >= beta) return new EvalMove(bestEval, bestMove);  // makes beta cut

        }

        if (!foundLegalMove)
        {
            if (game.IsKingInCheck(movingPlayer))
            {
                return new EvalMove(movingPlayer ? int.MinValue + depth : int.MaxValue - depth, null);
            }

            return new EvalMove(0, null);
        }

        return new EvalMove(bestEval, bestMove);
    }


    public EvalMove MinValue(GameState game, int depth, int alpha, int beta)
    {
        if (isCutoff(depth))
        { // see if we should stop going further down.
            return new EvalMove(Utility(game), null);
        }

        bool movingPlayer = game.CurrentPlayerIsWhite;
        List<Move> moves = game.GetAllPossibleMoves(); // find all pseudo-legal moves.
        int bestEval = int.MaxValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move
        bool foundLegalMove = false;

        // look through all moves, and see which returns the highest utility.
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            game.ApplyMoveInPlace(move, move.GetIsAttack());
            if (game.IsKingInCheck(movingPlayer))
            {
                game.UndoLastMove();
                continue;
            }

            foundLegalMove = true;
            EvalMove ev = MaxValue(game, depth + 1, alpha, beta); // looks at best move for Min player
            game.UndoLastMove();
            if (ev.value < bestEval)
            { // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                beta = Mathf.Min(beta, ev.value);
            }
            if (beta <= alpha) return new EvalMove(bestEval, bestMove); // makes alpha cut

        }

        if (!foundLegalMove)
        {
            if (game.IsKingInCheck(movingPlayer))
            {
                return new EvalMove(movingPlayer ? int.MinValue + depth : int.MaxValue - depth, null);
            }
            return new EvalMove(Utility(game), null);
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
        evaluatedMoves = 0;
        GameState gameState = new GameState(game);
        var searchTimer = System.Diagnostics.Stopwatch.StartNew();
        EvalMove evmove = game.GetCurrentPlayer()
            ? MaxValue(gameState, 0, int.MinValue, int.MaxValue)
            : MinValue(gameState, 0, int.MinValue, int.MaxValue);
        searchTimer.Stop();

        if (!evmove.move.HasValue) return;

        Move move = evmove.move.Value;
        Debug.Log($"MinMax search took {searchTimer.Elapsed.TotalMilliseconds:F2} ms and looked at {evaluatedMoves} moves.");
        GameObject pieceObj = game.GetPosition(move.GetFromMatrixX(), move.GetFromMatrixY());
        game.MakeNextMove(pieceObj, move.GetMatrixX(), move.GetMatrixY(), move.GetIsAttack());
    }
}
