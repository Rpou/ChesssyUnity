using System;
using System.Collections;
using System.Collections.Generic;
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

    // worst case: 48 + 202 + 16 = 266
    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        Game game = controller.GetComponent<Game>(); 

        GameObject cp = game.GetPosition(matrixX, matrixY);

        if (attack)
        {
            if (cp.name == "white_king") game.Winner("black");
            if (cp.name == "black_king") game.Winner("white");
            Destroy(cp);
        }

        var beforeMoveX = reference.GetComponent<Piece>().GetxBoard();
        var beforeMoveY = reference.GetComponent<Piece>().GetyBoard();

        game.SetPositionEmpty(beforeMoveX, beforeMoveY);

        Piece piece = reference.GetComponent<Piece>();
        if (piece is King movedKing) movedKing.ChangeHasMoved(true); 
        piece.SetXBoard(matrixX);
        piece.SetYBoard(matrixY);
        piece.SetCoords();

        game.SetPosition(reference);
        game.CheckIfCreateQueenFromPawn(matrixX, matrixY, reference, game);
        
        // Check if **opponent’s** king is in check before switching turns
        string opponent = game.GetCurrentPlayer() == "white" ? "black" : "white";
        King king = game.CheckIfKingInCheck(opponent); // 48
        
        var move = game.CreateNotation(piece, beforeMoveX, beforeMoveY, 
            matrixX, matrixY, king != null, attack); // 202
        game.AddMove(move);
        GameObject.Find("SidePanelController").GetComponent<GameLogScript>().LogMove(game);
        Debug.Log(move);
        
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
