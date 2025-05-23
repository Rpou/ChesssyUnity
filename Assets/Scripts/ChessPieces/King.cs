﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public bool _inCheck;
    private bool _hasMoved;
    private List<Vector2Int> _moveSquares = new List<Vector2Int>();
    private List<Vector2Int> _attackSquares = new List<Vector2Int>();
    
    public override King CanSeeKing()
    {
        return null;
    }

    public bool GetInCheck()
    {
        return _inCheck;
    }
    
    public void SetInCheck(bool inCheck)
    {
        if (inCheck)
        {
            GetComponent<SpriteRenderer>().color = Color.red;
            _inCheck = true;
            return;
        }

        GetComponent<SpriteRenderer>();
        _inCheck = false;
        GetComponent<SpriteRenderer>().color = Color.white;
    }
    
    
    public override List<Vector2Int> GetMoveSquares()
    {
        return _moveSquares;
    }
    public override List<Vector2Int> GetAttackSquares()
    {
        return _attackSquares;
    }

    public void ChangeHasMoved(bool hasMoved)
    {
        _hasMoved = hasMoved;
    }

    public bool GetHasMoved()
    {
        return _hasMoved;
    }
}