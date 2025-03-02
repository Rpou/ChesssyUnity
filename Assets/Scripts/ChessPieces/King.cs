using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    private bool _inCheck = false;
    
    protected override void InitiateMovePlates()
    {
        (List<Vector2Int> moveSquares, List<Vector2Int> attackSquares) = MovementPatterns.GetKingMoves(this, game);

        MovementPatterns.SpawnAllMovePlates(moveSquares, attackSquares, this, game);
    }
    
    public override King CanSeeKing()
    {
        return null;
    }

    public bool CheckIfInCheck()
    {
        if (GetPlayer().Equals("white"))
        {
            foreach (var piece in game.playerWhite)
            {
                if (piece.GetComponent<Piece>().CanSeeKing().name.StartsWith("black"))
                {
                    piece.GetComponent<King>().SetInCheck(true);
                    return true;
                }
            }

            foreach (var piece in game.playerBlack)
            {
                
            }
        }
        else if (GetPlayer().Equals("black"))
        {
            
        }
        
        return false;
    }

    public bool GetInCheck()
    {
        return _inCheck;
    }
    
    public void SetInCheck(bool inCheck)
    {
        _inCheck = inCheck;
    }
}