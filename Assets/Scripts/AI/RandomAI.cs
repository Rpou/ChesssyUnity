using System;
using System.Collections.Generic;

public class RandomAI : AI
{

    public override void MakeMove(Game game)
    {
        List<Move> moves = game.GetPossibleMoves();

        if (moves == null || moves.Count == 0)
        {
            return;
        }

        int size = moves.Count;
        Random rnd = new Random();
        int number = rnd.Next(0,size);
        Move move = moves[number];
        game.MakeNextMove(move.GetPiece().gameObject, move.GetMatrixX(), move.GetMatrixY(), move.GetIsAttack());
    }


}