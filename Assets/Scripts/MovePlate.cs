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
    
    public bool inCheck = false;

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
        game.SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(), reference.GetComponent<Chessman>().GetYBoard());

        var piece = reference.GetComponent<Chessman>();
        
        piece.SetXBoard(matrixX);
        piece.SetYBoard(matrixY);
        piece.SetCoords();

        game.SetPosition(reference);
        cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
        
        CheckIfCreateQueenFromPawn(piece, cp, game);
        inCheck = piece.CheckIfNextAttackCanKillKing();
        
        controller.GetComponent<Game>().NextTurn();
        
        reference.GetComponent<Chessman>().DestroyMovePlates();
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

    public void CheckIfCreateQueenFromPawn(Chessman piece, GameObject cp, Game game) {
        if (matrixY == 0 && piece.name == "black_pawn")
        {
            Destroy(cp);
            game.Create("black_queen", matrixX, matrixY);
        }

        if (matrixY == 7 && piece.name == "white_pawn")
        {
            Destroy(cp);
            game.Create("white_queen", matrixX, matrixY);
        }
    }

}
