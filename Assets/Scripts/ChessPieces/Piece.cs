using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    protected Game game;
    protected SpriteManager spriteManager;
    
    private int _xBoard = -1;
    private int _yBoard = -1;
    private string _player;

    private List<Vector2Int> _moveSquares;
    private List<Vector2Int> _attackSquares;
    
    private void Start()
    {
        game = GameObject.FindGameObjectWithTag("GameController").GetComponent<Game>();
        spriteManager = Resources.Load<SpriteManager>("SpriteManager");

        // Set the piece sprite based on name
        GetComponent<SpriteRenderer>().sprite = spriteManager.GetSprite(name);

        // Determine player color based on name
        _player = name.StartsWith("black") ? "black" : "white";

        // Set position on board
        SetCoords();
    }
    
    private void OnMouseUp()
    {
        // If it's this piece's turn and the game is not over
        if (!game.IsGameOver() && game.GetCurrentPlayer() == _player)
        {
            game.DestroyMovePlates();  // Remove any existing move plates
            AllLegalMovesSpawnMovePlate(); // Generate new move plates
        }
    }

    public void AllLegalMovesSpawnMovePlate()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetPieceMoves(this, game);
        MovementPatterns.SpawnAllMovePlates(_moveSquares, _attackSquares, this, game);
    }
    
    public abstract King CanSeeKing();

    public void SetCoords()
    {
        float x = _xBoard;
        float y = _yBoard;

        // Scale position to fit the board
        x *= 0.66f;
        y *= 0.66f;

        // Offset to align with the board correctly
        x += -2.3f;
        y += -2.3f;

        // Move the piece to the calculated position
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public string GetPlayer()
    {
        return _player;
    }

    public int GetxBoard()
    {
        return _xBoard;
    }

    public int GetyBoard()
    {
        return _yBoard;
    }

    public (List<Vector2Int> movableSquares, List<Vector2Int> attackableSquares) GetAllLegalMoves()
    {
        (_moveSquares, _attackSquares) = MovementPatterns.GetPieceMoves(this, game);
        var legalMoveableSquares = new List<Vector2Int>();
        var legalAttackableSquares = new List<Vector2Int>();
        foreach (var move in _moveSquares)
        {
            if (MovementPatterns.IsMoveSafe(move.x, move.y, this, game)) legalMoveableSquares.Add(move);
        }
        foreach (var attack in _attackSquares)
        {
            if (MovementPatterns.IsMoveSafe(attack.x, attack.y, this, game)) legalAttackableSquares.Add(attack);
        }

        return (legalMoveableSquares, legalAttackableSquares);
    }

    public abstract List<Vector2Int> GetMoveSquares();
    public abstract List<Vector2Int> GetAttackSquares();
    
    public void SetXBoard(int x) { _xBoard = x; }
    public void SetYBoard(int y) { _yBoard = y; }
}