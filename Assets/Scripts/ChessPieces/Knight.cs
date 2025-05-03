using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    private List<Vector2Int> _moveSquares = new List<Vector2Int>();
    private List<Vector2Int> _attackSquares = new List<Vector2Int>();
    public override void AllLegalMoves()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetKnightMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(_moveSquares, _attackSquares, this, game);
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
    
    public override (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetPossibleMoves()
    {
        return MovementPatterns.GetKnightMoves(this, game);
    }
    
    public override List<Vector2Int> GetMoveSquares()
    {
        return _moveSquares;
    }
    public override List<Vector2Int> GetAttackSquares()
    {
        return _attackSquares;
    }
}