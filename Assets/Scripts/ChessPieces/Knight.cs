using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetKnightMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
    
    public override King CanSeeKing()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetKnightMoves(this, game);

        foreach (var attack in attackSquares)
        {
            if(game.GetPosition(attack.x, attack.y).GetComponent<Piece>() is King) return game.GetPosition(attack.x, attack.y).GetComponent<King>();
        }
        return null;
    }
}