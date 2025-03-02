using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetKnightMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
}