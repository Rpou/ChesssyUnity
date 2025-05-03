using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    private List<Vector2Int> _moveSquares = new List<Vector2Int>();
    private List<Vector2Int> _attackSquares = new List<Vector2Int>();

    public override King CanSeeKing()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetPieceMoves(this, game);

        foreach (var attack in _attackSquares)
        {
            GameObject maybePiece = game.GetPosition(attack.x, attack.y);
            if (maybePiece == null) 
                continue;

            if (maybePiece.GetComponent<Piece>() is King king) return king;
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