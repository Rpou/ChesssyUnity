using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    // References
    public GameObject controller;
    public GameObject movePlate;

    // Board positions
    protected int xBoard = -1;
    protected int yBoard = -1;

    // Which player owns this piece ("white" or "black")
    public string player;

    // The sprite for this piece – assign it via the inspector or in the subclass.
    public Sprite pieceSprite;

    // Called when the piece is created/activated.
    public virtual void Activate() {
        controller = GameObject.FindGameObjectWithTag("GameController");
        SetCoords();
        GetComponent<SpriteRenderer>().sprite = pieceSprite;
    }

    // Set the world coordinates based on the board position.
    public void SetCoords() {
        float x = xBoard;
        float y = yBoard;
        x *= 0.66f;
        y *= 0.66f;
        x += -2.3f;
        y += -2.3f;
        transform.position = new Vector3(x, y, -1.0f);
    }

    // Getters and setters for board position.
    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    // When the player clicks the piece.
    private void OnMouseUp() {
        Game game = controller.GetComponent<Game>();
        if (!game.IsGameOver() && game.GetCurrentPlayer() == player) {
            DestroyMovePlates();
            GenerateMovePlates();
        }
    }

    // Remove any existing move plates.
    public void DestroyMovePlates() {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        foreach (GameObject mp in movePlates) {
            Destroy(mp);
        }
    }

    // Each subclass must implement its own move generation.
    public abstract void GenerateMovePlates();

    // --- Helper Methods (available to all subclasses) ---

    // Spawns a non-attacking move plate.
    protected void SpawnMovePlate(int matrixX, int matrixY) {
        float x = matrixX;
        float y = matrixY;
        x *= 0.66f;
        y *= 0.66f;
        x += -2.3f;
        y += -2.3f;
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    // Spawns an attacking move plate.
    protected void SpawnAttackPlate(int matrixX, int matrixY) {
        float x = matrixX;
        float y = matrixY;
        x *= 0.66f;
        y *= 0.66f;
        x += -2.3f;
        y += -2.3f;
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    // Generates a move plate at a specific point (used by kings, knights, etc.).
    protected void GeneratePointMovePlate(int x, int y) {
        Game game = controller.GetComponent<Game>();
        if (game.PositionOnBoard(x, y)) {
            GameObject target = game.GetPosition(x, y);
            if (target == null) {
                SpawnMovePlate(x, y);
            }
            else if (target.GetComponent<Piece>().player != player) {
                SpawnAttackPlate(x, y);
            }
        }
    }

    // Generates move plates along a line (for rooks, bishops, queens).
    protected void GenerateLineMovePlates(int xIncrement, int yIncrement) {
        Game game = controller.GetComponent<Game>();
        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;
        while (game.PositionOnBoard(x, y) && game.GetPosition(x, y) == null) {
            SpawnMovePlate(x, y);
            x += xIncrement;
            y += yIncrement;
        }
        if (game.PositionOnBoard(x, y) && game.GetPosition(x, y) != null &&
            game.GetPosition(x, y).GetComponent<Piece>().player != player) {
            SpawnAttackPlate(x, y);
        }
    }
}
