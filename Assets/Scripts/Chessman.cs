using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Chessman : MonoBehaviour
{
    //Refrences
    public GameObject controller;
    public GameObject movePlate;

    //Positions
    private int xBoard = -1;
    private int yBoard = -1;

    //Var to keep track of player turn
    private string player;

    //Refrences for all sprites
    public Sprite black_queen, black_king, black_knight, black_bishop, black_rook, black_pawn;
    public Sprite white_queen, white_king, white_knight, white_bishop, white_rook, white_pawn;

    public void Activate(){
        // Find the game controller
        controller = GameObject.FindGameObjectWithTag("GameController");

        // Take instantiated location, and adjust the transform
        SetCoords();
        
        // Assign sprite and set player based on piece name
        switch (this.name){
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = "black"; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = "black"; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = "black"; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = "black"; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = "black"; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = "black"; break;

            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = "white"; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = "white"; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = "white"; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = "white"; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = "white"; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = "white"; break;
            
        }
    }

    public void SetCoords()
    {
        float x = xBoard;
        float y = yBoard;

        // Scale position to fit the board
        x *= 0.66f;
        y *= 0.66f;

        // Offset to align with the board correctly
        x += -2.3f;
        y += -2.3f;

        // Move the piece to the calculated position
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    // Getters and setters for board position
    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    private void OnMouseUp()
    {
        // If it's this piece's turn and the game is not over
        if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player)
        {
            DestroyMovePlates();  // Remove any existing move plates
            InitiateMovePlates(); // Generate new move plates
        }
    }

    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for(int i = 0; i < movePlates.Length; i++){
            Destroy(movePlates[i]);
        }
    }

    // Generates move plates based on the piece type
    public void InitiateMovePlates()
    {
        switch (this.name)
        {
            case "black_queen":
            case "white_queen":
                // Queens move like rooks and bishops combined
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(1, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                LineMovePlate(-1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(1, -1);
                break;

            case "black_knight":
            case "white_knight":
                // Knights move in an L shape
                LMovePlate();
                break;

            case "black_bishop":
            case "white_bishop":
                // Bishops move diagonally
                LineMovePlate(1, 1);
                LineMovePlate(1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(-1, -1);
                break;

            case "black_king":
            case "white_king":
                // Kings move one square in any direction
                SurroundMovePlate();
                break;

            case "black_rook":
            case "white_rook":
                // Rooks move in straight lines (horizontal & vertical)
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                break;

            case "black_pawn":
                PawnMovePlate(xBoard, yBoard - 1, false);
                break;
            case "white_pawn":
                PawnMovePlate(xBoard, yBoard + 1, true);
                break;
        }
    }

    // Handles straight-line movement (used for rooks, bishops, queens)
    public void LineMovePlate(int xIncrement, int yIncrement)
    {
        Game sc = controller.GetComponent<Game>();

        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;

        // Keep moving in the direction until hitting another piece or the edge
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);
            x += xIncrement;
            y += yIncrement;
        }

        // If an enemy piece is found, spawn an attack plate
        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x, y);
        }
    }

    // Handles knight's movement (L-shaped)
    public void LMovePlate()
    {
        PointMovePlate(xBoard + 1, yBoard + 2);
        PointMovePlate(xBoard - 1, yBoard + 2);
        PointMovePlate(xBoard + 2, yBoard + 1);
        PointMovePlate(xBoard + 2, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard - 2);
        PointMovePlate(xBoard - 1, yBoard - 2);
        PointMovePlate(xBoard - 2, yBoard + 1);
        PointMovePlate(xBoard - 2, yBoard - 1);
    }

    // Handles king's movement (one square in any direction)
    public void SurroundMovePlate(){
        PointMovePlate(xBoard, yBoard + 1);
        PointMovePlate(xBoard, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 0);
        PointMovePlate(xBoard - 1, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard - 0);
        PointMovePlate(xBoard + 1, yBoard + 1);
    }

    /// <summary>
    /// Spawns move plates at specific points (used by kings and knights)
    /// </summary>
    /// <param name="x">The x coordinate</param> 
    /// <param name="y">The y coordinate</param>
    public void PointMovePlate(int x, int y){
        Game sc = controller.GetComponent<Game>();
        if(sc.PositionOnBoard(x,y)){
            GameObject cp = sc.GetPosition(x,y);

            if(cp == null){
                MovePlateSpawn(x,y);
            }
            else if(cp.GetComponent<Chessman>().player != player){
                MovePlateAttackSpawn(x,y);
            }
        }
    }

    // Handles pawn movement and attacking
    public void PawnMovePlate(int x, int y, bool isWhite){
        Game sc = controller.GetComponent<Game>();
        
        // if the square in front of him exists and there is not a piece in front of him
        if(sc.PositionOnBoard(x,y) && sc.GetPosition(x,y) == null){
            MovePlateSpawn(x,y);
            
            // If bro is white and on starting square, check two steps ahead
            if (isWhite && y == 2 && sc.GetPosition(x, y + 1) == null) { 
                MovePlateSpawn(x, y + 1);
            }

            // If bro is black and on starting square, check two steps ahead
            if (!isWhite && y == 5 && sc.GetPosition(x, y - 1) == null) { 
                MovePlateSpawn(x, y - 1);
            }
        } 
        
        if(sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && 
           sc.GetPosition(x + 1,y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x + 1, y);
        }

        if(sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && 
           sc.GetPosition(x - 1,y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x - 1, y);
        }
    }
    
    /// <summary>
    /// Spawns a single moveplate. The moveplate allows the piece to move to that square.
    /// </summary>
    /// <param name="matrixX">The x coordinate</param>
    /// <param name="matrixY">The y coordinate</param>
    public void MovePlateSpawn(int matrixX, int matrixY){
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x,y,-3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);

    }

    public void MovePlateAttackSpawn(int matrixX, int matrixY){
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x,y,-3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }
    

    public bool CheckIfNextAttackCanKillKing()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        Game game = controller.GetComponent<Game>();
        if (game.GetCurrentPlayer() == "white")
        {
            
        }

        Debug.Log(name + " is checking if it can attack the king...");
        switch (this.name)
        { 
                
            case "black_queen":
            case "white_queen":
                // Queens move like rooks and bishops combined
                if(CheckLineMovePlate(1, 0)) return true;
                if(CheckLineMovePlate(0, 1)) return true;
                if(CheckLineMovePlate(1, 1)) return true;
                if(CheckLineMovePlate(-1, 0)) return true;
                if(CheckLineMovePlate(0, -1)) return true;
                if(CheckLineMovePlate(-1, -1)) return true;
                if(CheckLineMovePlate(-1, 1)) return true;
                if(CheckLineMovePlate(1, -1)) return true;
                break;

            case "black_knight":
            case "white_knight":
                // Knights move in an L shape
                if (CheckLMovePlate()) return true;
                break;

            case "black_bishop":
            case "white_bishop":
                // Bishops move diagonally
                if(CheckLineMovePlate(1, 1)) return true;
                if(CheckLineMovePlate(1, -1)) return true;
                if(CheckLineMovePlate(-1, 1)) return true;
                if(CheckLineMovePlate(-1, -1)) return true;
                break;

            case "black_king":
            case "white_king":
                // Kings move one square in any direction
                if (CheckSurroundMovePlate()) return true;
                break;

            case "black_rook":
            case "white_rook":
                // Rooks move in straight lines (horizontal & vertical)
                if(CheckLineMovePlate(1, 0)) return true;
                if(CheckLineMovePlate(0, 1)) return true;
                if(CheckLineMovePlate(-1, 0)) return true;
                if(CheckLineMovePlate(0, -1)) return true;
                break;

            case "black_pawn":
                if (CheckPawnMovePlate(xBoard, yBoard - 1, false)) return true;
                break;
            case "white_pawn":
                if (CheckPawnMovePlate(xBoard, yBoard + 1, true)) return true;
                break;
        }

        return false;
    }
    
    /// <summary>
    /// Spawns move plates at specific points (used by kings and knights)
    /// </summary>
    /// <param name="x">The x coordinate</param> 
    /// <param name="y">The y coordinate</param>
    public bool CheckPointMovePlate(int x, int y){
        Game sc = controller.GetComponent<Game>();
        
        GameObject target = sc.GetPosition(x, y);
        return sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) != null && 
               sc.GetPosition(x,y).GetComponent<Chessman>().player != player && target.name is "black_king" or "white_king";
    }

    // Handles pawn movement and attacking
    public bool CheckPawnMovePlate(int x, int y, bool isWhite){
        Game sc = controller.GetComponent<Game>();
        GameObject target = sc.GetPosition(x, y);
        if(sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && 
           sc.GetPosition(x + 1,y).GetComponent<Chessman>().player != player && target.name is "black_king" or "white_king")
        {
            return true;
        }

        return sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && 
               sc.GetPosition(x - 1,y).GetComponent<Chessman>().player != player && target.name is "black_king" or "white_king";
    }
    
    // Handles straight-line movement (used for rooks, bishops, queens)
    public bool CheckLineMovePlate(int xIncrement, int yIncrement)
    {
        Game sc = controller.GetComponent<Game>();

        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;

        // Ensure x, y stay within board limits before accessing
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            x += xIncrement;
            y += yIncrement;
        }

        

        // Before accessing the board, ensure (x, y) is still in bounds
        if (!sc.PositionOnBoard(x, y))
        {
            return false; // Don't check further, as we're out of bounds
        }

        GameObject target = sc.GetPosition(x, y);

        // Make sure the target is not null before accessing properties
        if (target != null && target.GetComponent<Chessman>().player != player)
        {
            return target.name is "black_king" or "white_king";
        }

        return false;
    }

    
    public bool CheckLMovePlate()
    {
        return CheckPointMovePlate(xBoard + 1, yBoard + 2) || CheckPointMovePlate(xBoard - 1, yBoard + 2) ||
                CheckPointMovePlate(xBoard + 2, yBoard + 1)
                || CheckPointMovePlate(xBoard + 2, yBoard - 1) || CheckPointMovePlate(xBoard + 1, yBoard - 2) ||
                CheckPointMovePlate(xBoard - 1, yBoard - 2)
                || CheckPointMovePlate(xBoard - 2, yBoard + 1) || CheckPointMovePlate(xBoard - 2, yBoard - 1);
    }

    // Handles king's movement (one square in any direction)
    public bool CheckSurroundMovePlate()
    {
        return CheckPointMovePlate(xBoard, yBoard + 1) || CheckPointMovePlate(xBoard, yBoard - 1) ||
               CheckPointMovePlate(xBoard - 1, yBoard - 1) || CheckPointMovePlate(xBoard - 1, yBoard - 0)
               || CheckPointMovePlate(xBoard - 1, yBoard + 1) || CheckPointMovePlate(xBoard + 1, yBoard - 1)
               || CheckPointMovePlate(xBoard + 1, yBoard - 0) || CheckPointMovePlate(xBoard + 1, yBoard + 1);
    }
    
    

}
