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

        game.SetPositionEmpty(reference.GetComponent<Piece>().GetxBoard(), reference.GetComponent<Piece>().GetyBoard());

        Piece piece = reference.GetComponent<Piece>();
        piece.SetXBoard(matrixX);
        piece.SetYBoard(matrixY);
        piece.SetCoords();

        game.SetPosition(reference);
        game.CheckIfCreateQueenFromPawn(matrixX, matrixY, reference, game);

        // Check if **opponent’s** king is in check before switching turns
        string opponent = game.GetCurrentPlayer() == "white" ? "black" : "white";
        game.CheckIfKingInCheck(opponent);

        game.NextTurn();
        game.DestroyMovePlates();
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
