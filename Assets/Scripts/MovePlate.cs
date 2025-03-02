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

    public void OnMouseUp(){
        controller = GameObject.FindGameObjectWithTag("GameController");
        GameObject cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
        
        if(attack){
            if(cp.name == "white_king") controller.GetComponent<Game>().Winner("black");
            if(cp.name == "black_king") controller.GetComponent<Game>().Winner("white");
            
            Destroy(cp);
        }
        
        Game game = controller.GetComponent<Game>();
        game.SetPositionEmpty(reference.GetComponent<Piece>().GetxBoard(), reference.GetComponent<Piece>().GetyBoard());
        var piece = reference.GetComponent<Piece>();
        
        piece.SetXBoard(matrixX);
        piece.SetYBoard(matrixY);
        piece.SetCoords();

        game.SetPosition(reference);
        cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
        
        game.CheckIfCreateQueenFromPawn(matrixX, matrixY,cp, game);
        //inCheck = piece.CanSeeKing();
        //if (inCheck)
        //{
          //  Debug.Log("King in check");
        //}
        
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
