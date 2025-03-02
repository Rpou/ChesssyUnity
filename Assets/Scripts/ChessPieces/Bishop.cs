using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetBishopMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
    
    public override King CanSeeKing()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetBishopMoves(this, game);

        foreach (var attack in attackSquares)
        {
            if(game.GetPosition(attack.x, attack.y).GetComponent<Piece>() is King) return game.GetPosition(attack.x, attack.y).GetComponent<King>();
        }
        return null;
    }
}