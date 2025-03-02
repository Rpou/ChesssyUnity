using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetQueenMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
    
}