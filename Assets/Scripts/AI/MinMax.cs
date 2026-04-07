using UnityEngine;
using System.Collections.Generic;


public class MinMax : AI
{
    private int Utility(GameState game)
    {
        int score = 0;
        foreach (PieceState piece in game.GetActivePieces("white"))
        {
            score += piece.GetWorth();
        }
        foreach (PieceState piece in game.GetActivePieces("black"))
        {
            score -= piece.GetWorth(); 
        }
        return score;
    }

    public bool isCutoff(int depth){
        return depth > 2;
    }
    
    public EvalMove MaxValue(GameState game, int depth, int alpha, int beta){
        List<Move> moves = game.GetPossibleMoves(); // find all legal moves.
        if(isCutoff(depth) || moves.Count == 0){ // see if we should stop going further down.
            return new EvalMove(Utility(game),null);
        }

        int bestEval = int.MinValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move

        // look through all moves, and see which returns the highest utility.
        for(int i = 0; i < moves.Count; i++){ 
            var move = moves[i];

            GameState gameSim = game.ApplyMove(move);

            EvalMove ev = MinValue(gameSim, depth + 1, alpha, beta); // looks at best move for Min player
            if (ev.value > bestEval){ // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                alpha = Mathf.Max(alpha, ev.value);
            }
            if(alpha >= beta) return new EvalMove(bestEval, bestMove);  // makes beta cut
            
        }
        return new EvalMove(bestEval, bestMove);
    }

    public EvalMove MinValue(GameState game, int depth, int alpha, int beta){
        List<Move> moves = game.GetPossibleMoves(); // find all legal moves.
        if(isCutoff(depth) || moves.Count == 0){ // see if we should stop going further down.
            return new EvalMove(Utility(game), null);
        }

        int bestEval = int.MaxValue; // placeholder bestEval
        Move bestMove = new Move(); // placeholder move

        // look through all moves, and see which returns the highest utility.
        for(int i = 0; i < moves.Count; i++){
            var move = moves[i];
            GameState gameSim = game.ApplyMove(move);

            EvalMove ev = MaxValue(gameSim, depth + 1, alpha, beta); // looks at best move for Min player
            if (ev.value < bestEval){ // update best move if better utility
                bestEval = ev.value;
                bestMove = move;
                beta = Mathf.Min(beta, ev.value);
            }
            if (beta <= alpha) return new EvalMove(bestEval, bestMove); // makes alpha cut
        
        }
        return new EvalMove(bestEval, bestMove);
    }

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
