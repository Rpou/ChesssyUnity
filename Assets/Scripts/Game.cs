using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class Game : MonoBehaviour
{
    private GameObject chessPiece;
    private GameObject movePlate;
    private SpriteManager spriteManager;

    private GameObject[,] positions = new GameObject[8, 8];
    public GameObject[] playerBlack = new GameObject[16];
    public GameObject[] playerWhite = new GameObject[16];
    private Vector2Int? enPassantTarget;

    private List<string> moves;
    private string currentPlayer = "white";
    private bool gameOver = false;

    void Start()
    {
        moves = new List<string>();
        movePlate = Resources.Load<GameObject>("Objects/MovePlate");
        chessPiece = Resources.Load<GameObject>("Objects/ChessPiece");
        // Load the SpriteManager from Resources
        spriteManager = Resources.Load<SpriteManager>("SpriteManager");
        if (spriteManager == null)
        {
            Debug.LogError("SpriteManager not found! Make sure it's inside the 'Resources' folder.");
        }

        // Initialize white pieces
        playerWhite = new GameObject[]
        {
            CreatePiece<Rook>("white_rook", 0, 0), CreatePiece<Knight>("white_knight", 1, 0),
            CreatePiece<Bishop>("white_bishop", 2, 0), CreatePiece<Queen>("white_queen", 3, 0),
            CreatePiece<King>("white_king", 4, 0), CreatePiece<Bishop>("white_bishop", 5, 0),
            CreatePiece<Knight>("white_knight", 6, 0), CreatePiece<Rook>("white_rook", 7, 0),
            CreatePiece<Pawn>("white_pawn", 0, 1), CreatePiece<Pawn>("white_pawn", 1, 1),
            CreatePiece<Pawn>("white_pawn", 2, 1), CreatePiece<Pawn>("white_pawn", 3, 1),
            CreatePiece<Pawn>("white_pawn", 4, 1), CreatePiece<Pawn>("white_pawn", 5, 1),
            CreatePiece<Pawn>("white_pawn", 6, 1), CreatePiece<Pawn>("white_pawn", 7, 1)
        };

        // Initialize black pieces
        playerBlack = new GameObject[]
        {
            CreatePiece<Rook>("black_rook", 0, 7), CreatePiece<Knight>("black_knight", 1, 7),
            CreatePiece<Bishop>("black_bishop", 2, 7), CreatePiece<Queen>("black_queen", 3, 7),
            CreatePiece<King>("black_king", 4, 7), CreatePiece<Bishop>("black_bishop", 5, 7),
            CreatePiece<Knight>("black_knight", 6, 7), CreatePiece<Rook>("black_rook", 7, 7),
            CreatePiece<Pawn>("black_pawn", 0, 6), CreatePiece<Pawn>("black_pawn", 1, 6),
            CreatePiece<Pawn>("black_pawn", 2, 6), CreatePiece<Pawn>("black_pawn", 3, 6),
            CreatePiece<Pawn>("black_pawn", 4, 6), CreatePiece<Pawn>("black_pawn", 5, 6),
            CreatePiece<Pawn>("black_pawn", 6, 6), CreatePiece<Pawn>("black_pawn", 7, 6)
        };
    }

    void Update()
    {
        if (IsGameOver() && Input.GetMouseButtonDown(0))
        {
            gameOver = false;
            SceneManager.LoadScene("Game");
        }
    }

    public void NextTurn()
    {
        currentPlayer = (currentPlayer == "white") ? "black" : "white";
    }

    // Generic method to create a chess piece of a specific type
    private GameObject CreatePiece<T>(string name, int x, int y) where T : Piece
    {
        GameObject obj = Instantiate(chessPiece, new Vector3(0, 0, -1), Quaternion.identity);
        T piece = obj.AddComponent<T>(); // Add the correct piece type

        // Set board position
        piece.SetXBoard(x);
        piece.SetYBoard(y);
        piece.name = name;

        // Set position in the board
        SetPosition(obj);

        return obj;
    }

    public void CheckIfCreateQueenFromPawn(int matrixX, int matrixY, GameObject cp, Game game)
    {
        var piece = cp.GetComponent<Piece>();
        if (matrixY == 0 && piece.name == "black_pawn")
        {
            Destroy(cp);
            CreatePiece<Queen>("black_queen", matrixX, matrixY);
        }

        if (matrixY == 7 && piece.name == "white_pawn")
        {
            Destroy(cp);
            CreatePiece<Queen>("white_queen", matrixX, matrixY);
        }
    }

    // worst case: 16 + 16 + 16 = 48
    public King CheckIfKingInCheck(string opponent, bool isSimulation = false)
    {
        List<GameObject> allPieces = new List<GameObject>();

        allPieces.AddRange(opponent == "black" ? playerWhite : playerBlack);

        // Check if any opponent piece can attack the king
        foreach (var gameObjectPiece in allPieces)
        {
            if (gameObjectPiece == null || !gameObjectPiece.activeSelf) continue; // Skip if the piece is destroyed

            Piece piece = gameObjectPiece.GetComponent<Piece>();
            King kingInCheck = piece.CanSeeKing();

            if (kingInCheck == null) continue;
            if (!isSimulation)
            {
                kingInCheck.SetInCheck(true); 
                Debug.Log($"{opponent} King is in Check by {piece.name}");
            }
            return kingInCheck;
        }

        if (opponent == "black")
        {
            foreach (var gameObject in playerWhite)
            {
                if (gameObject == null || !gameObject.activeSelf) continue; // Skip if the piece is destroyed
                Piece piece = gameObject.GetComponent<Piece>();
                if (piece is King king)
                {
                    king.SetInCheck(false);
                }
            }
        }
        else
        {
            foreach (var gameObject in playerBlack)
            {
                if (gameObject == null || !gameObject.activeSelf) continue; // Skip if the piece is destroyed
                Piece piece = gameObject.GetComponent<Piece>();
                if (piece is King king)
                {
                    king.SetInCheck(false);
                }
            }
        }
        return null;
    }

    public void Winner(string playerWinner)
    {
        gameOver = true;
        if (GameObject.FindGameObjectWithTag("WinnerTag").GetComponent<Text>() == null)
        {
            Console.WriteLine("ur dumb lol");
        }
        GameObject.FindGameObjectWithTag("WinnerTag").GetComponent<TextMeshProUGUI>().enabled = true;
        GameObject.FindGameObjectWithTag("WinnerTag").GetComponent<TextMeshProUGUI>().text = playerWinner + " is the winner";

        GameObject.FindGameObjectWithTag("RestartTag").GetComponent<TextMeshProUGUI>().enabled = true;
    }

    /// <summary>
    /// Spawns a single moveplate. The moveplate allows the piece to move to that square.
    /// </summary>
    /// <param name="matrixX">The x coordinate</param>
    /// <param name="matrixY">The y coordinate</param>
    /// <param name="piece">The piece in question</param>
    public void SpawnMovePlate(int matrixX, int matrixY, Piece piece)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(piece.gameObject);
        mpScript.SetCoords(matrixX, matrixY);

    }

    public void SpawnAttackMovePlate(int matrixX, int matrixY, Piece piece)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(piece.gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public bool PositionOnBoard(int x, int y)
    {
        return x >= 0 && y >= 0 && x < positions.GetLength(0) && y < positions.GetLength(1);
    }
    
    // worst case: 16
    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }

    // worst case: (196) + 6(string made) = 202
    public string CreateNotation(Piece piece, int xPositionBefore, int yPositionBefore, int xPositionAfter, 
        int yPositionAfter, bool putInCheck, bool killedPiece)
    {
        var letterOfSquareMovedTo = ConvertNrToChar(xPositionAfter + 1).ToString();
        var letterOfSquareBeforeMove = ConvertNrToChar(xPositionBefore + 1);
        var overlap = LegalMovesOverlapSameTypePiece(piece);
        
        var result = "";
        if (killedPiece) result += "x";
        if (piece is Pawn)
        {
            if (killedPiece) result = letterOfSquareBeforeMove + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = letterOfSquareMovedTo + result + (yPositionAfter+1);
        }

        if (piece is Knight)
        {
            if (overlap) result = "N" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            else result = "N" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Bishop)
        {
            result = "B" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Rook)
        {
            if (overlap) result = "R" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            result = "R" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is Queen)
        {
            if (overlap) result = "Q" + letterOfSquareBeforeMove + (yPositionBefore+1) + result + letterOfSquareMovedTo + (yPositionAfter+1);
            result = "Q" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }

        if (piece is King)
        {
            result = "K" + result + letterOfSquareMovedTo + (yPositionAfter+1);
        }
        
        if (putInCheck) result += "+";
        return result;
    }

    // worst case: 16 + (180) = 196
    private bool LegalMovesOverlapSameTypePiece(Piece pieceType)
    {
        var player = GetCurrentPlayer();
        List<GameObject> allPieces = new List<GameObject>();
        allPieces.AddRange(player == "black" ? playerBlack : playerWhite);
        
        if (pieceType is Knight)
        {
            return LegalMovesOverlap<Knight>(allPieces);
        }

        if (pieceType is Rook)
        {
            return LegalMovesOverlap<Rook>(allPieces);
        }

        if (pieceType is Queen)
        {
            return LegalMovesOverlap<Queen>(allPieces);
        }

        return false;
    }

    // worst case: 16 + 10 * 10 + 8 * 8 = 180
    private bool LegalMovesOverlap<T>(List<GameObject> allPieces) where T : Piece
    {
        Piece piece1 = null;
        Piece piece2 = null;
        foreach (var gameObject in allPieces)
        {
            if (gameObject == null) continue;
            Piece piece = gameObject.GetComponent<Piece>();
            if (piece is T matchedPiece && piece1 == null)
            {
                piece1 = matchedPiece;
            }
            else if (piece is T matchedPiece2)
            {
                piece2 = matchedPiece2;
            }
            Debug.Log("somethign is happening");

            if (piece1 != null && piece2 != null)
            {
                Debug.Log("I am inside if -- I WANT TO BE HERE");
                var moves1 = piece1.GetMoveSquares();
                var attacks1 = piece1.GetMoveSquares();
                var moves2 = piece2.GetMoveSquares();
                var attacks2 = piece2.GetMoveSquares();

                foreach (var move1 in moves1)
                {
                    foreach (var move2 in moves2)
                    {
                        Debug.Log("Move1: " + move1 + " move2: " + move2);
                        if (move1 == move2) return true;
                    }
                }
                    
                foreach (var attack1 in attacks1)
                {
                    foreach (var attack2 in attacks2)
                    {
                        if (attack1 == attack2) return true;
                    }
                }
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Converts a nr to a letter based  on ASCII
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private char ConvertNrToChar(int number)
    {
        return (char)('a' + number - 1);
    }

    public void SetPosition(GameObject obj)
    {
        Piece piece = obj.GetComponent<Piece>();

        positions[piece.GetxBoard(), piece.GetyBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }


    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void AddMove(string move)
    {
        moves.Add(move);
    }

    public List<string> GetMoves()
    {
        return moves;
    }
}
