using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    private List<Vector2Int> _moveSquares = new List<Vector2Int>();
    private List<Vector2Int> _attackSquares = new List<Vector2Int>();
    
    public override void AllLegalMoves()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetPawnMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(_moveSquares, _attackSquares, this, game);
    }

    public override King CanSeeKing()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetPawnMoves(this, game);

        foreach (var attack in _attackSquares)
        {
            if(game.GetPosition(attack.x, attack.y).GetComponent<Piece>() is King) return game.GetPosition(attack.x, attack.y).GetComponent<King>();
        }
        return null;
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