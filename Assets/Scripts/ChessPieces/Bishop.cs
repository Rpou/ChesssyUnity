using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetBishopMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
}