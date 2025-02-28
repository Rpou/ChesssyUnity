using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    public override void GenerateMovePlates() {
        GenerateLineMovePlates(1, 1);
        GenerateLineMovePlates(1, -1);
        GenerateLineMovePlates(-1, 1);
        GenerateLineMovePlates(-1, -1);
    }
}