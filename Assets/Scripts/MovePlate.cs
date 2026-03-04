using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using GameLogic;
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
        Piece piece = reference.GetComponent<Piece>();
        var beforeMoveX = reference.GetComponent<Piece>().GetxBoard();
        var beforeMoveY = reference.GetComponent<Piece>().GetyBoard();
        
        if (attack)
        {
            var isEnPassantCapture = false;
            if (piece is Pawn)
            {
                int plusMinusOne = piece.GetPlayer().Equals("white") ? -1 : 1;
                int enPassantY = matrixY + plusMinusOne;

                if (game.PositionOnBoard(matrixX, enPassantY))
                {
                    var targetPosition = game.GetPosition(matrixX, enPassantY);
                    var possiblePiece = targetPosition != null ? targetPosition.GetComponent<Piece>() : null;
                    if (possiblePiece != null && possiblePiece == game.GetEnPassentTarget())
                    {
                        isEnPassantCapture = true;
                        game.SetPositionEmpty(matrixX, enPassantY);
                        Debug.Log("Destroying: " + possiblePiece.name);
                        Destroy(targetPosition);
                    }
                }
            }

            if (!isEnPassantCapture && cp != null)
            {
                Debug.Log("Destroying: " + cp.name);
                Destroy(cp);
            }
        }

        game.SetPositionEmpty(beforeMoveX, beforeMoveY);
        
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
        if (piece is Pawn pawn && Math.Abs(beforeMoveY - matrixY) == 2)
        {
            game.SetEnPassantTarget(pawn);
        }
        else
        {
            game.SetEnPassantTarget(null);
        }
        
        // Check if **opponent’s** king is in check before switching turns
        string opponent = game.GetCurrentPlayer() == "white" ? "black" : "white";
        King king = game.CheckIfKingInCheck(opponent); // 48
        var putInCheck = king != null;
        var move = NotationCreater.CreateNotation(piece, beforeMoveX, beforeMoveY, 
            matrixX, matrixY, putInCheck, attack, castled, game); // 202
        game.AddMove(move);
        GameObject.Find("SidePanelController").GetComponent<GameLogScript>().LogMove(game);

        if (!game.AnyLegalMoves(opponent))
        {
            if (putInCheck) game.Winner(game.GetCurrentPlayer());
            else game.Winner(null);
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
