using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class MovePlate : MonoBehaviour
{

    public GameObject controller;

    GameObject reference = null;

    //Board Position
    int matrixX;
    int matrixY;

    // False: movement, true: attacking
    public bool attack = false;

    public void Start(){
        if(attack){
            //change to red
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }

    // worst case: 202 + 48 + 16 = 266
    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        Game game = controller.GetComponent<Game>(); 

        GameObject cp = game.GetPosition(matrixX, matrixY);
        var beforeMoveX = reference.GetComponent<Piece>().GetxBoard();
        var beforeMoveY = reference.GetComponent<Piece>().GetyBoard();
        
        if (attack)
        {
            int plusMinusOne = reference.GetComponent<Piece>().GetPlayer().Equals("white") ? -1 : 1;
            try
            {
                var possiblePiece = game.GetPosition(matrixX, matrixY + plusMinusOne).GetComponent<Piece>();
                if (possiblePiece != null && possiblePiece == game.GetEnPassentTarget())
                {
                    Destroy(game.GetPosition(matrixX, matrixY + plusMinusOne));
                }
            }
            catch
            {
                Destroy(cp);
            }
        }

        game.SetPositionEmpty(beforeMoveX, beforeMoveY);

        Piece piece = reference.GetComponent<Piece>();
        
        piece.SetXBoard(matrixX);
        piece.SetYBoard(matrixY);
        piece.SetCoords();

        game.SetPosition(reference);
        game.CheckIfCreateQueenFromPawn(matrixX, matrixY, reference, game);

        var castled = false;
        if (piece is King kingMoved) kingMoved.ChangeHasMoved(true);
        if (piece is King movedKing && Math.Abs(beforeMoveX - matrixX) == 2)
        {
            castled = true;
            // if king moved right
            var isRightRook = matrixX > beforeMoveX;
            Rook rook;
            if (isRightRook)
            {
                rook = (Rook)game.GetPosition(beforeMoveX + 3, beforeMoveY).GetComponent<Piece>();
            }
            else
            {
                rook = (Rook)game.GetPosition(beforeMoveX - 4, beforeMoveY).GetComponent<Piece>();
            }
            MovementPatterns.MoveRookAfterCastlingMove(movedKing, rook, game);
            movedKing.ChangeHasMoved(true);
        }
        if (piece is Rook movedRook) movedRook.SetHasMoved(true);
        if (piece is Pawn pawn && Math.Abs(beforeMoveY - matrixY) == 2) game.SetEnPassantTarget(pawn);
        
        // Check if **opponent’s** king is in check before switching turns
        string opponent = game.GetCurrentPlayer() == "white" ? "black" : "white";
        King king = game.CheckIfKingInCheck(opponent); // 48
        var putInCheck = king != null;
        var move = game.CreateNotation(piece, beforeMoveX, beforeMoveY, 
            matrixX, matrixY, putInCheck, attack, castled); // 202
        game.AddMove(move);
        GameObject.Find("SidePanelController").GetComponent<GameLogScript>().LogMove(game);

        if (putInCheck)
        {
            Debug.Log("HE IS IN CHECK");
            var isCheckMate = game.IsCheckMate(opponent);
            Debug.Log("IS IT CHECKMATE: " + isCheckMate);
            if (isCheckMate) game.Winner(game.GetCurrentPlayer());
        }
        
        game.NextTurn();
        game.DestroyMovePlates(); // 16
    }


    public void SetCoords(int x, int y){
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj){
        reference = obj;
    }

    public GameObject GetReference(){
        return reference;
    }

}
