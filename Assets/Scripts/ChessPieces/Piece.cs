using System;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    protected Game game;
    protected SpriteManager spriteManager;
    
    private int _xBoard = -1;
    private int _yBoard = -1;
    private string _player;
    
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
            InitiateMovePlates(); // Generate new move plates
        }
    }

    protected abstract void InitiateMovePlates();
    
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
    
    public void SetXBoard(int x) { _xBoard = x; }
    public void SetYBoard(int y) { _yBoard = y; }
}