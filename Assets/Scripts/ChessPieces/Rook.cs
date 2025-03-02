using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetRookMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
}